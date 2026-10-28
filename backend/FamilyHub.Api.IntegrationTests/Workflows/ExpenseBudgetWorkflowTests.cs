using System.Net;
using System.Net.Http.Json;
using FamilyHub.Api.DTOs.Expenses;
using FamilyHub.Api.IntegrationTests.Infrastructure;
using FamilyHub.Api.Models.Enums;
using Microsoft.AspNetCore.Http;

namespace FamilyHub.Api.IntegrationTests.Workflows;

/// <summary>
/// Budgets and the monthly summary on SQL Server: carry-forward between months, totals summed in
/// SQL, validation, and concurrent budget changes.
/// </summary>
[Collection(ApiCollection.Name)]
public class ExpenseBudgetWorkflowTests(FamilyHubApiFixture fixture)
{
    private static string Budgets(Guid familyId) => $"/api/families/{familyId}/expense-budgets";

    private static string Summary(Guid familyId, int year, int month) =>
        $"/api/families/{familyId}/expenses/summary?year={year}&month={month}";

    [Fact]
    public async Task Budgets_carry_forward_and_the_summary_reflects_the_selected_month()
    {
        var family = await fixture.CreateFamilyWithOwnerAsync();
        var parent = await fixture.AddMemberAsync(family, FamilyRole.Parent);
        var categories = await family.Owner.GetExpenseCategoriesAsync(family.Id);
        var food = categories.Single(c => c.DefaultCategory == DefaultExpenseCategory.FoodGroceries);
        var transport = categories.Single(c => c.DefaultCategory == DefaultExpenseCategory.Transport);
        var fun = categories.Single(c => c.DefaultCategory == DefaultExpenseCategory.Entertainment);

        // October 2025: 50,000 overall, Food 15,000, Transport 6,000, Entertainment 4,000.
        var set = await parent.Client.PutAsJsonAsync(Budgets(family.Id), new SetExpenseBudgetsRequest(2025, 10, 50_000m,
            [new CategoryBudgetRequest(food.Id, 15_000m), new CategoryBudgetRequest(transport.Id, 6_000m),
             new CategoryBudgetRequest(fun.Id, 4_000m)]));
        await set.EnsureStatusAsync(HttpStatusCode.OK);
        await family.Owner.CreateExpenseAsync(family.Id, 12_000.25m, new DateOnly(2025, 10, 2), categoryId: food.Id);
        await family.Owner.CreateExpenseAsync(family.Id, 13_999.75m, new DateOnly(2025, 10, 31), parent.UserId, food.Id);
        await family.Owner.CreateExpenseAsync(family.Id, 6_000m, new DateOnly(2025, 10, 15), categoryId: transport.Id);
        await family.Owner.CreateExpenseAsync(family.Id, 750m, new DateOnly(2025, 11, 1), categoryId: food.Id);

        var october = await family.Owner.Client.GetFromJsonAsync<ExpenseSummaryResponse>(Summary(family.Id, 2025, 10));
        Assert.Equal((32_000m, 3, 50_000m, 18_000m, 64),
            (october!.TotalSpent, october.ExpenseCount, october.Budget, october.Remaining, october.UsagePercent));
        Assert.Equal([food.Id, transport.Id, fun.Id], october.Categories.Select(c => c.CategoryId));
        Assert.Equal((26_000m, 15_000m, -11_000m, 173), (october.Categories[0].Spent, october.Categories[0].Budget,
            october.Categories[0].Remaining, october.Categories[0].UsagePercent));
        Assert.Equal((6_000m, 0m, 100), (october.Categories[1].Spent, october.Categories[1].Remaining, october.Categories[1].UsagePercent));
        Assert.Equal((0m, 4_000m), (october.Categories[2].Spent, october.Categories[2].Remaining));
        Assert.Equal([13_999.75m, 6_000m, 12_000.25m], october.RecentExpenses.Select(e => e.Amount));

        // November has no budget change of its own, so October's budgets still apply.
        var november = await family.Owner.Client.GetFromJsonAsync<ExpenseSummaryResponse>(Summary(family.Id, 2025, 11));
        Assert.Equal((750m, 50_000m, 49_250m, 2), (november!.TotalSpent, november.Budget, november.Remaining, november.UsagePercent));

        // From December 2025: 60,000 overall and no Entertainment budget. Earlier months are unchanged.
        var change = await family.Owner.Client.PutAsJsonAsync(Budgets(family.Id), new SetExpenseBudgetsRequest(2025, 12, 60_000m,
            [new CategoryBudgetRequest(food.Id, 15_000m), new CategoryBudgetRequest(transport.Id, 6_000m)]));
        await change.EnsureStatusAsync(HttpStatusCode.OK);
        var december = (await change.Content.ReadFromJsonAsync<ExpenseBudgetResponse>())!;
        Assert.Equal(60_000m, december.OverallAmount);
        Assert.DoesNotContain(december.Categories, c => c.CategoryId == fun.Id);

        var stillOctober = await family.Owner.Client.GetFromJsonAsync<ExpenseBudgetResponse>($"{Budgets(family.Id)}?year=2025&month=10");
        Assert.Equal(50_000m, stillOctober!.OverallAmount);
        Assert.Contains(stillOctober.Categories, c => c.CategoryId == fun.Id && c.Amount == 4_000m);
        var march = await family.Owner.Client.GetFromJsonAsync<ExpenseSummaryResponse>(Summary(family.Id, 2026, 3));
        Assert.Equal((0m, 60_000m, 60_000m, 0), (march!.TotalSpent, march.Budget, march.Remaining, march.UsagePercent));
        // No spending yet: categories with a budget are listed in category order.
        Assert.Equal([food.Id, transport.Id], march.Categories.Select(c => c.CategoryId));
    }

    [Fact]
    public async Task A_month_without_any_budget_has_null_budget_values()
    {
        var family = await fixture.CreateFamilyWithOwnerAsync();
        await family.Owner.CreateExpenseAsync(family.Id, 500m, new DateOnly(2026, 10, 5));

        var summary = await family.Owner.Client.GetFromJsonAsync<ExpenseSummaryResponse>(Summary(family.Id, 2026, 10));

        Assert.Equal((500m, (decimal?)null, (decimal?)null, (int?)null),
            (summary!.TotalSpent, summary.Budget, summary.Remaining, summary.UsagePercent));
        var row = Assert.Single(summary.Categories);
        Assert.Null(row.Budget);
    }

    [Fact]
    public async Task Invalid_budget_requests_are_rejected_per_field_and_nothing_is_saved()
    {
        var family = await fixture.CreateFamilyWithOwnerAsync();
        var food = (await family.Owner.GetExpenseCategoriesAsync(family.Id))[0];

        var invalid = await family.Owner.Client.PutAsJsonAsync(Budgets(family.Id), new SetExpenseBudgetsRequest(2026, 13, 0m,
            [new CategoryBudgetRequest(food.Id, 12.345m), new CategoryBudgetRequest(food.Id, 1m)]));
        var noMonth = await family.Owner.Client.GetAsync($"/api/families/{family.Id}/expenses/summary?year=2026");
        var budgetsWithoutMonth = await family.Owner.Client.GetAsync($"{Budgets(family.Id)}?year=2026");

        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        var problem = (await invalid.Content.ReadFromJsonAsync<HttpValidationProblemDetails>())!;
        Assert.Contains("Month", problem.Errors.Keys);
        Assert.Contains("Budget must be greater than zero.", problem.Errors["OverallAmount"]);
        Assert.Contains("Budget can have at most two decimals.", problem.Errors["Categories[0].Amount"]);
        Assert.Contains("Each category can appear only once.", problem.Errors["Categories"]);
        Assert.Equal(HttpStatusCode.BadRequest, noMonth.StatusCode);
        Assert.Contains("Month is required.",
            (await budgetsWithoutMonth.Content.ReadFromJsonAsync<HttpValidationProblemDetails>())!.Errors["Month"]);
        var saved = await family.Owner.Client.GetFromJsonAsync<ExpenseBudgetResponse>($"{Budgets(family.Id)}?year=2026&month=12");
        Assert.Null(saved!.OverallAmount);
    }

    [Theory]
    [InlineData("expenses/summary?year=2026&month=13", "Month")]
    [InlineData("expenses/summary?year=1999&month=1", "Year")]
    [InlineData("expense-budgets?year=2026&month=0", "Month")]
    [InlineData("expense-budgets?year=2101&month=1", "Year")]
    public async Task Out_of_range_months_are_rejected(string path, string field)
    {
        var family = await fixture.CreateFamilyWithOwnerAsync();

        var response = await family.Owner.Client.GetAsync($"/api/families/{family.Id}/{path}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(field, (await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>())!.Errors.Keys);
    }

    [Fact]
    public async Task An_empty_category_id_or_more_than_100_category_budgets_are_rejected()
    {
        var family = await fixture.CreateFamilyWithOwnerAsync();
        var tooMany = Enumerable.Range(0, 101).Select(_ => new CategoryBudgetRequest(Guid.NewGuid(), 1m)).ToList();

        var empty = await family.Owner.Client.PutAsJsonAsync(Budgets(family.Id),
            new SetExpenseBudgetsRequest(2026, 10, null, [new CategoryBudgetRequest(Guid.Empty, 1m)]));
        var many = await family.Owner.Client.PutAsJsonAsync(Budgets(family.Id), new SetExpenseBudgetsRequest(2026, 10, null, tooMany));

        Assert.Contains("A category is required.",
            (await empty.Content.ReadFromJsonAsync<HttpValidationProblemDetails>())!.Errors["Categories[0].CategoryId"]);
        Assert.Contains("At most 100 category budgets are allowed.",
            (await many.Content.ReadFromJsonAsync<HttpValidationProblemDetails>())!.Errors["Categories"]);
    }

    [Fact]
    public async Task The_summary_only_includes_the_familys_own_spending()
    {
        var family = await fixture.CreateFamilyWithOwnerAsync();
        var other = await fixture.CreateFamilyWithOwnerAsync();
        var date = new DateOnly(2026, 9, 10);
        await family.Owner.CreateExpenseAsync(family.Id, 100m, date);
        await other.Owner.CreateExpenseAsync(other.Id, 9_999m, date);
        await other.Owner.CreateExpenseAsync(other.Id, 1m, date);

        var summary = await family.Owner.Client.GetFromJsonAsync<ExpenseSummaryResponse>(Summary(family.Id, 2026, 9));

        Assert.Equal((100m, 1), (summary!.TotalSpent, summary.ExpenseCount));
        Assert.Equal(100m, Assert.Single(summary.Categories).Spent);
        Assert.All(summary.RecentExpenses, e => Assert.Equal(family.Id, e.FamilyId));
    }

    [Fact]
    public async Task A_tiny_budget_with_huge_spending_does_not_break_the_summary()
    {
        var family = await fixture.CreateFamilyWithOwnerAsync();
        var food = (await family.Owner.GetExpenseCategoriesAsync(family.Id))[0];
        var set = await family.Owner.Client.PutAsJsonAsync(Budgets(family.Id),
            new SetExpenseBudgetsRequest(2026, 9, 0.01m, [new CategoryBudgetRequest(food.Id, 0.01m)]));
        await set.EnsureStatusAsync(HttpStatusCode.OK);
        await family.Owner.CreateExpenseAsync(family.Id, 10_000_000m, new DateOnly(2026, 9, 1), categoryId: food.Id);

        var response = await family.Owner.Client.GetAsync(Summary(family.Id, 2026, 9));

        await response.EnsureStatusAsync(HttpStatusCode.OK);
        var summary = (await response.Content.ReadFromJsonAsync<ExpenseSummaryResponse>())!;
        Assert.Equal((int.MaxValue, int.MaxValue), (summary.UsagePercent!.Value, summary.Categories[0].UsagePercent!.Value));
    }

    [Fact]
    public async Task An_archived_category_cannot_get_a_new_budget()
    {
        var family = await fixture.CreateFamilyWithOwnerAsync();
        var health = (await family.Owner.GetExpenseCategoriesAsync(family.Id)).Single(c => c.DefaultCategory == DefaultExpenseCategory.Health);
        var archive = await family.Owner.Client.PutAsJsonAsync($"/api/families/{family.Id}/expense-categories/{health.Id}",
            new UpdateExpenseCategoryRequest(health.Name, true));
        await archive.EnsureStatusAsync(HttpStatusCode.OK);

        var response = await family.Owner.Client.PutAsJsonAsync(Budgets(family.Id),
            new SetExpenseBudgetsRequest(2026, 10, null, [new CategoryBudgetRequest(health.Id, 2_000m)]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Concurrent_budget_changes_for_the_same_month_never_fail_with_a_server_error()
    {
        const int rounds = 10;
        var family = await fixture.CreateFamilyWithOwnerAsync();
        var parent = await fixture.AddMemberAsync(family, FamilyRole.Parent);
        var food = (await family.Owner.GetExpenseCategoriesAsync(family.Id))[0];

        for (var round = 0; round < rounds; round++)
        {
            // A month with no budget row yet, so both requests try to insert the same rows.
            var (year, month) = (2030 + round / 12, round % 12 + 1);
            var responses = await Task.WhenAll(
                family.Owner.Client.PutAsJsonAsync(Budgets(family.Id),
                    new SetExpenseBudgetsRequest(year, month, 10_000m, [new CategoryBudgetRequest(food.Id, 1_000m)])),
                parent.Client.PutAsJsonAsync(Budgets(family.Id),
                    new SetExpenseBudgetsRequest(year, month, 20_000m, [new CategoryBudgetRequest(food.Id, 2_000m)])));

            Assert.All(responses, r => Assert.Contains(r.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Conflict }));
            Assert.Contains(responses, r => r.StatusCode == HttpStatusCode.OK);
            var saved = await family.Owner.Client.GetFromJsonAsync<ExpenseBudgetResponse>($"{Budgets(family.Id)}?year={year}&month={month}");
            // Whole requests win or lose: the overall and food budgets always come from the same request.
            Assert.Contains((saved!.OverallAmount, Assert.Single(saved.Categories).Amount),
                new (decimal?, decimal)[] { (10_000m, 1_000m), (20_000m, 2_000m) });
        }
    }

    [Fact]
    public async Task Concurrent_complete_budget_sets_that_change_different_rows_never_mix()
    {
        const int rounds = 10;
        var family = await fixture.CreateFamilyWithOwnerAsync();
        var parent = await fixture.AddMemberAsync(family, FamilyRole.Parent);
        var categories = await family.Owner.GetExpenseCategoriesAsync(family.Id);
        var food = categories.Single(c => c.DefaultCategory == DefaultExpenseCategory.FoodGroceries);
        var transport = categories.Single(c => c.DefaultCategory == DefaultExpenseCategory.Transport);

        for (var round = 0; round < rounds; round++)
        {
            var (year, month) = (2040 + round / 12, round % 12 + 1);
            var setup = await family.Owner.Client.PutAsJsonAsync(Budgets(family.Id),
                new SetExpenseBudgetsRequest(year, month, 100m, [new CategoryBudgetRequest(food.Id, 50m)]));
            await setup.EnsureStatusAsync(HttpStatusCode.OK);

            // One request adds Transport (a new row); the other removes Food (an in-place update).
            var responses = await Task.WhenAll(
                family.Owner.Client.PutAsJsonAsync(Budgets(family.Id), new SetExpenseBudgetsRequest(year, month, 100m,
                    [new CategoryBudgetRequest(food.Id, 50m), new CategoryBudgetRequest(transport.Id, 20m)])),
                parent.Client.PutAsJsonAsync(Budgets(family.Id), new SetExpenseBudgetsRequest(year, month, 100m, [])));

            Assert.All(responses, r => Assert.Contains(r.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Conflict }));
            var saved = await family.Owner.Client.GetFromJsonAsync<ExpenseBudgetResponse>($"{Budgets(family.Id)}?year={year}&month={month}");
            var categoriesSaved = string.Join(",", saved!.Categories
                .OrderBy(c => c.CategoryId == food.Id ? 0 : 1)
                .Select(c => c.CategoryId == food.Id ? $"Food {c.Amount}" : $"Transport {c.Amount}"));
            // The result is always one request's complete set, never a mix of both.
            Assert.True(categoriesSaved is "" or "Food 50.00,Transport 20.00" or "Food 50,Transport 20",
                $"Round {round}: mixed result [{categoriesSaved}] ({(int)responses[0].StatusCode}, {(int)responses[1].StatusCode}).");
        }
    }

    [Fact]
    public async Task A_budget_change_racing_a_category_delete_never_fails_with_a_server_error()
    {
        const int rounds = 10;
        var family = await fixture.CreateFamilyWithOwnerAsync();
        var parent = await fixture.AddMemberAsync(family, FamilyRole.Parent);

        for (var round = 0; round < rounds; round++)
        {
            var created = await family.Owner.Client.PostAsJsonAsync($"/api/families/{family.Id}/expense-categories",
                new CreateExpenseCategoryRequest($"Short-lived {round}"));
            await created.EnsureStatusAsync(HttpStatusCode.Created);
            var category = (await created.Content.ReadFromJsonAsync<ExpenseCategoryResponse>())!;

            var responses = await Task.WhenAll(
                family.Owner.Client.DeleteAsync($"/api/families/{family.Id}/expense-categories/{category.Id}"),
                parent.Client.PutAsJsonAsync(Budgets(family.Id),
                    new SetExpenseBudgetsRequest(2026, 10, null, [new CategoryBudgetRequest(category.Id, 1_000m)])));

            Assert.True(responses.All(r => (int)r.StatusCode < 500),
                $"Round {round}: delete {(int)responses[0].StatusCode}, budget {(int)responses[1].StatusCode}.");
            await responses[0].EnsureStatusAsync(HttpStatusCode.NoContent);
            var saved = await family.Owner.Client.GetFromJsonAsync<ExpenseBudgetResponse>($"{Budgets(family.Id)}?year=2026&month=10");
            Assert.DoesNotContain(saved!.Categories, c => c.CategoryId == category.Id);
        }
    }
}
