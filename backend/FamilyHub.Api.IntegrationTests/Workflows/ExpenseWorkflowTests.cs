using System.Net;
using System.Net.Http.Json;
using FamilyHub.Api.Common;
using FamilyHub.Api.DTOs.Expenses;
using FamilyHub.Api.IntegrationTests.Infrastructure;
using FamilyHub.Api.Models.Enums;
using Microsoft.AspNetCore.Http;

namespace FamilyHub.Api.IntegrationTests.Workflows;

/// <summary>
/// Family Expenses end to end on SQL Server: default categories, monthly history, decimal
/// precision, validation, and the category archive/delete rules backed by real constraints.
/// </summary>
[Collection(ApiCollection.Name)]
public class ExpenseWorkflowTests(FamilyHubApiFixture fixture)
{
    private static string Expenses(Guid familyId) => $"/api/families/{familyId}/expenses";

    private static string Categories(Guid familyId) => $"/api/families/{familyId}/expense-categories";

    [Fact]
    public async Task Add_list_by_month_update_and_delete_an_expense()
    {
        var family = await fixture.CreateFamilyWithOwnerAsync();
        var parent = await fixture.AddMemberAsync(family, FamilyRole.Parent);

        var categories = await family.Owner.GetExpenseCategoriesAsync(family.Id);
        Assert.Equal(ExpenseCategoryDefaults.All.Select(d => d.Category), categories.Select(c => c.DefaultCategory!.Value));
        var food = categories[0];
        var transport = categories.Single(c => c.DefaultCategory == DefaultExpenseCategory.Transport);

        var create = await family.Owner.Client.PostAsJsonAsync(Expenses(family.Id),
            new CreateExpenseRequest(1234.56m, food.Id, new DateOnly(2026, 10, 15), parent.UserId, "  Market  "));
        await create.EnsureStatusAsync(HttpStatusCode.Created);
        var expense = (await create.Content.ReadFromJsonAsync<ExpenseResponse>())!;
        Assert.EndsWith($"{Expenses(family.Id)}/{expense.Id}", create.Headers.Location!.ToString(),
            StringComparison.OrdinalIgnoreCase);
        Assert.Equal((1234.56m, "Market", "Parent Tester", "Owner Tester"),
            (expense.Amount, expense.Description, expense.PaidByName, expense.CreatedByName));
        await family.Owner.CreateExpenseAsync(family.Id, 99m, new DateOnly(2026, 9, 30));

        var october = await parent.Client.GetFromJsonAsync<List<ExpenseResponse>>($"{Expenses(family.Id)}?year=2026&month=10");
        Assert.Equal([expense.Id], october!.Select(e => e.Id));
        var byPayer = await parent.Client.GetFromJsonAsync<List<ExpenseResponse>>(
            $"{Expenses(family.Id)}?year=2026&month=10&paidByUserId={family.Owner.UserId}");
        Assert.Empty(byPayer!);

        var update = await parent.Client.PutAsJsonAsync($"{Expenses(family.Id)}/{expense.Id}",
            new UpdateExpenseRequest(0.01m, transport.Id, new DateOnly(2026, 10, 1), family.Owner.UserId, null));
        await update.EnsureStatusAsync(HttpStatusCode.OK);
        var updated = (await update.Content.ReadFromJsonAsync<ExpenseResponse>())!;
        Assert.Equal((0.01m, transport.Id, (string?)null), (updated.Amount, updated.CategoryId, updated.Description));
        Assert.NotNull(updated.UpdatedAt);

        var delete = await family.Owner.Client.DeleteAsync($"{Expenses(family.Id)}/{expense.Id}");
        await delete.EnsureStatusAsync(HttpStatusCode.NoContent);
        var gone = await family.Owner.Client.GetAsync($"{Expenses(family.Id)}/{expense.Id}");
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }

    [Fact]
    public async Task Invalid_expenses_and_filters_are_rejected_per_field()
    {
        var family = await fixture.CreateFamilyWithOwnerAsync();
        var food = (await family.Owner.GetExpenseCategoriesAsync(family.Id))[0];
        var future = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2);

        var create = await family.Owner.Client.PostAsJsonAsync(Expenses(family.Id),
            new CreateExpenseRequest(12.345m, food.Id, future, family.Owner.UserId, null));
        var noMonth = await family.Owner.Client.GetAsync($"{Expenses(family.Id)}?year=2026");

        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
        var problem = (await create.Content.ReadFromJsonAsync<HttpValidationProblemDetails>())!;
        Assert.Contains("Amount can have at most two decimals.", problem.Errors["Amount"]);
        Assert.Contains("Date cannot be in the future.", problem.Errors["Date"]);
        Assert.Equal(HttpStatusCode.BadRequest, noMonth.StatusCode);
        Assert.Contains("Month is required.",
            (await noMonth.Content.ReadFromJsonAsync<HttpValidationProblemDetails>())!.Errors["Month"]);
    }

    [Fact]
    public async Task A_used_category_is_archived_instead_of_deleted_and_history_keeps_it()
    {
        var family = await fixture.CreateFamilyWithOwnerAsync();
        var created = await family.Owner.Client.PostAsJsonAsync(Categories(family.Id), new CreateExpenseCategoryRequest("Fuel"));
        await created.EnsureStatusAsync(HttpStatusCode.Created);
        var fuel = (await created.Content.ReadFromJsonAsync<ExpenseCategoryResponse>())!;
        var expense = await family.Owner.CreateExpenseAsync(family.Id, 3000m, categoryId: fuel.Id);

        var delete = await family.Owner.Client.DeleteAsync($"{Categories(family.Id)}/{fuel.Id}");
        Assert.Equal(HttpStatusCode.Conflict, delete.StatusCode);
        Assert.Equal("This category is used by expenses. Archive it instead.",
            (await delete.Content.ReadFromJsonAsync<ErrorResponse>())!.Message);

        var archive = await family.Owner.Client.PutAsJsonAsync($"{Categories(family.Id)}/{fuel.Id}",
            new UpdateExpenseCategoryRequest("Fuel", true));
        await archive.EnsureStatusAsync(HttpStatusCode.OK);

        var history = await family.Owner.Client.GetFromJsonAsync<ExpenseResponse>($"{Expenses(family.Id)}/{expense.Id}");
        Assert.Equal(("Fuel", true, 3000m), (history!.CategoryName, history.CategoryIsArchived, history.Amount));
        Assert.DoesNotContain(await family.Owner.GetExpenseCategoriesAsync(family.Id), c => c.Id == fuel.Id);
        var withArchived = await family.Owner.Client.GetFromJsonAsync<List<ExpenseCategoryResponse>>(
            $"{Categories(family.Id)}?includeArchived=true");
        Assert.Contains(withArchived!, c => c.Id == fuel.Id && c.IsArchived);
        var useArchived = await family.Owner.Client.PostAsJsonAsync(Expenses(family.Id),
            new CreateExpenseRequest(10m, fuel.Id, DateOnly.FromDateTime(DateTime.UtcNow), family.Owner.UserId, null));
        Assert.Equal(HttpStatusCode.BadRequest, useArchived.StatusCode);
    }

    [Fact]
    public async Task Category_names_are_unique_per_family_including_archived_ones()
    {
        var family = await fixture.CreateFamilyWithOwnerAsync();
        var other = await fixture.CreateFamilyWithOwnerAsync();

        var duplicateDefault = await family.Owner.Client.PostAsJsonAsync(Categories(family.Id),
            new CreateExpenseCategoryRequest("TRANSPORT"));
        var sameNameElsewhere = await other.Owner.Client.PostAsJsonAsync(Categories(other.Id),
            new CreateExpenseCategoryRequest("Pets"));
        var firstPets = await family.Owner.Client.PostAsJsonAsync(Categories(family.Id),
            new CreateExpenseCategoryRequest("Pets"));
        var secondPets = await family.Owner.Client.PostAsJsonAsync(Categories(family.Id),
            new CreateExpenseCategoryRequest("pets"));

        Assert.Equal(HttpStatusCode.Conflict, duplicateDefault.StatusCode);
        await sameNameElsewhere.EnsureStatusAsync(HttpStatusCode.Created);
        await firstPets.EnsureStatusAsync(HttpStatusCode.Created);
        Assert.Equal(HttpStatusCode.Conflict, secondPets.StatusCode);
        Assert.Equal(10, (await family.Owner.GetExpenseCategoriesAsync(family.Id)).Count);
    }

    [Fact]
    public async Task An_unused_custom_category_can_be_deleted()
    {
        var family = await fixture.CreateFamilyWithOwnerAsync();
        var created = await family.Owner.Client.PostAsJsonAsync(Categories(family.Id), new CreateExpenseCategoryRequest("Gifts"));
        var gifts = (await created.Content.ReadFromJsonAsync<ExpenseCategoryResponse>())!;

        var delete = await family.Owner.Client.DeleteAsync($"{Categories(family.Id)}/{gifts.Id}");

        await delete.EnsureStatusAsync(HttpStatusCode.NoContent);
        var missing = await family.Owner.Client.GetAsync($"{Categories(family.Id)}/{gifts.Id}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Theory]
    [InlineData("archive", "archive")]
    [InlineData("archive", "delete")]
    [InlineData("delete", "delete")]
    public async Task Concurrent_archive_or_delete_never_leaves_the_family_without_an_active_category(
        string firstAction, string secondAction)
    {
        const int rounds = 15;
        var family = await fixture.CreateFamilyWithOwnerAsync();
        var parent = await fixture.AddMemberAsync(family, FamilyRole.Parent);

        for (var round = 0; round < rounds; round++)
        {
            // Leave exactly two active (unused) categories, then remove both at the same time.
            var first = await CreateCategoryAsync(family, $"First {round}");
            var second = await CreateCategoryAsync(family, $"Second {round}");
            foreach (var category in await family.Owner.GetExpenseCategoriesAsync(family.Id))
            {
                if (category.Id != first.Id && category.Id != second.Id)
                {
                    await (await ArchiveAsync(family.Owner, family.Id, category)).EnsureStatusAsync(HttpStatusCode.OK);
                }
            }

            var responses = await Task.WhenAll(
                RemoveAsync(firstAction, family.Owner, family.Id, first),
                RemoveAsync(secondAction, parent, family.Id, second));

            var active = await family.Owner.GetExpenseCategoriesAsync(family.Id);
            Assert.True(active.Count == 1,
                $"Round {round}: {active.Count} active categories after concurrent {firstAction}/{secondAction} " +
                $"({(int)responses[0].StatusCode}, {(int)responses[1].StatusCode}).");
            Assert.Single(responses, r => r.IsSuccessStatusCode);
            var rejected = responses.Single(r => !r.IsSuccessStatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
            Assert.Equal("The family needs at least one active category.",
                (await rejected.Content.ReadFromJsonAsync<ErrorResponse>())!.Message);
        }
    }

    private async Task<ExpenseCategoryResponse> CreateCategoryAsync(TestFamily family, string name)
    {
        var response = await family.Owner.Client.PostAsJsonAsync(Categories(family.Id), new CreateExpenseCategoryRequest(name));
        await response.EnsureStatusAsync(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ExpenseCategoryResponse>())!;
    }

    private static Task<HttpResponseMessage> ArchiveAsync(TestUser user, Guid familyId, ExpenseCategoryResponse category) =>
        user.Client.PutAsJsonAsync($"{Categories(familyId)}/{category.Id}", new UpdateExpenseCategoryRequest(category.Name, true));

    private static Task<HttpResponseMessage> RemoveAsync(
        string action, TestUser user, Guid familyId, ExpenseCategoryResponse category) =>
        action == "delete"
            ? user.Client.DeleteAsync($"{Categories(familyId)}/{category.Id}")
            : ArchiveAsync(user, familyId, category);
}
