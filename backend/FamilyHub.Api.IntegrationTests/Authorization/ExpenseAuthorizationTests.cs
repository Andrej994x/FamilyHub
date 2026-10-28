using System.Net;
using System.Net.Http.Json;
using FamilyHub.Api.Common;
using FamilyHub.Api.DTOs.Expenses;
using FamilyHub.Api.IntegrationTests.Infrastructure;
using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.IntegrationTests.Authorization;

/// <summary>
/// Family Expenses access rules: Owner/Parent manage everything, Members view and add and change
/// only their own expenses, Children have no access, and nothing crosses family boundaries.
/// </summary>
[Collection(ApiCollection.Name)]
public class ExpenseAuthorizationTests(FamilyHubApiFixture fixture)
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static string Expenses(Guid familyId) => $"/api/families/{familyId}/expenses";

    private static string Categories(Guid familyId) => $"/api/families/{familyId}/expense-categories";

    private static string Budgets(Guid familyId) => $"/api/families/{familyId}/expense-budgets";

    [Theory]
    [InlineData(FamilyRole.Parent, HttpStatusCode.Created)]
    [InlineData(FamilyRole.Member, HttpStatusCode.Created)]
    [InlineData(FamilyRole.Child, HttpStatusCode.Forbidden)]
    public async Task Every_adult_can_add_an_expense_but_a_child_cannot(FamilyRole role, HttpStatusCode expected)
    {
        var family = await fixture.CreateFamilyWithOwnerAsync();
        var user = await fixture.AddMemberAsync(family, role);
        var category = (await family.Owner.GetExpenseCategoriesAsync(family.Id))[0];

        var response = await user.Client.PostAsJsonAsync(Expenses(family.Id),
            new CreateExpenseRequest(1500m, category.Id, Today, family.Owner.UserId, null));

        Assert.Equal(expected, response.StatusCode);
    }

    [Theory]
    [InlineData("/expenses?year=2026&month=10")]
    [InlineData("/expense-categories")]
    [InlineData("/expenses/summary?year=2026&month=10")]
    [InlineData("/expense-budgets?year=2026&month=10")]
    public async Task A_child_cannot_even_read_family_expenses(string suffix)
    {
        var family = await fixture.CreateFamilyWithOwnerAsync();
        var child = await fixture.AddMemberAsync(family, FamilyRole.Child);
        var expense = await family.Owner.CreateExpenseAsync(family.Id);

        var list = await child.Client.GetAsync($"/api/families/{family.Id}{suffix}");
        var single = await child.Client.GetAsync($"{Expenses(family.Id)}/{expense.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, single.StatusCode);
        Assert.Equal("Children do not have access to family expenses.",
            (await list.Content.ReadFromJsonAsync<ErrorResponse>())!.Message);
    }

    [Fact]
    public async Task A_member_changes_only_their_own_expenses()
    {
        var family = await fixture.CreateFamilyWithOwnerAsync();
        var member = await fixture.AddMemberAsync(family, FamilyRole.Member);
        var ownersExpense = await family.Owner.CreateExpenseAsync(family.Id, 1000m);
        var ownExpense = await member.CreateExpenseAsync(family.Id, 200m);
        var change = new UpdateExpenseRequest(1m, ownersExpense.CategoryId, Today, member.UserId, "changed");

        var updateOthers = await member.Client.PutAsJsonAsync($"{Expenses(family.Id)}/{ownersExpense.Id}", change);
        var deleteOthers = await member.Client.DeleteAsync($"{Expenses(family.Id)}/{ownersExpense.Id}");
        var updateOwn = await member.Client.PutAsJsonAsync($"{Expenses(family.Id)}/{ownExpense.Id}", change);
        var deleteOwn = await member.Client.DeleteAsync($"{Expenses(family.Id)}/{ownExpense.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, updateOthers.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deleteOthers.StatusCode);
        await updateOwn.EnsureStatusAsync(HttpStatusCode.OK);
        await deleteOwn.EnsureStatusAsync(HttpStatusCode.NoContent);
        var untouched = await family.Owner.Client.GetFromJsonAsync<ExpenseResponse>($"{Expenses(family.Id)}/{ownersExpense.Id}");
        Assert.Equal((1000m, null), (untouched!.Amount, untouched.Description));
    }

    [Fact]
    public async Task A_parent_can_change_and_delete_a_members_expense()
    {
        var family = await fixture.CreateFamilyWithOwnerAsync();
        var parent = await fixture.AddMemberAsync(family, FamilyRole.Parent);
        var member = await fixture.AddMemberAsync(family, FamilyRole.Member);
        var expense = await member.CreateExpenseAsync(family.Id, 300m);

        var update = await parent.Client.PutAsJsonAsync($"{Expenses(family.Id)}/{expense.Id}",
            new UpdateExpenseRequest(350m, expense.CategoryId, Today, member.UserId, "Corrected"));
        var delete = await parent.Client.DeleteAsync($"{Expenses(family.Id)}/{expense.Id}");

        await update.EnsureStatusAsync(HttpStatusCode.OK);
        await delete.EnsureStatusAsync(HttpStatusCode.NoContent);
        var gone = await member.Client.GetAsync($"{Expenses(family.Id)}/{expense.Id}");
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }

    [Theory]
    [InlineData(FamilyRole.Parent, HttpStatusCode.Created)]
    [InlineData(FamilyRole.Member, HttpStatusCode.Forbidden)]
    [InlineData(FamilyRole.Child, HttpStatusCode.Forbidden)]
    public async Task Only_owners_and_parents_manage_categories(FamilyRole role, HttpStatusCode expected)
    {
        var family = await fixture.CreateFamilyWithOwnerAsync();
        var user = await fixture.AddMemberAsync(family, role);
        var health = (await family.Owner.GetExpenseCategoriesAsync(family.Id)).Single(c => c.DefaultCategory == DefaultExpenseCategory.Health);

        var create = await user.Client.PostAsJsonAsync(Categories(family.Id), new CreateExpenseCategoryRequest("Fuel"));
        var archive = await user.Client.PutAsJsonAsync($"{Categories(family.Id)}/{health.Id}",
            new UpdateExpenseCategoryRequest("Health", true));

        Assert.Equal(expected, create.StatusCode);
        Assert.Equal(expected == HttpStatusCode.Created ? HttpStatusCode.OK : HttpStatusCode.Forbidden, archive.StatusCode);
    }

    [Theory]
    [InlineData(FamilyRole.Parent, HttpStatusCode.OK)]
    [InlineData(FamilyRole.Member, HttpStatusCode.Forbidden)]
    [InlineData(FamilyRole.Child, HttpStatusCode.Forbidden)]
    public async Task Only_owners_and_parents_set_budgets(FamilyRole role, HttpStatusCode expected)
    {
        var family = await fixture.CreateFamilyWithOwnerAsync();
        var user = await fixture.AddMemberAsync(family, role);
        var set = await family.Owner.Client.PutAsJsonAsync(Budgets(family.Id), new SetExpenseBudgetsRequest(2026, 10, 50_000m, null));
        await set.EnsureStatusAsync(HttpStatusCode.OK);

        var response = await user.Client.PutAsJsonAsync(Budgets(family.Id), new SetExpenseBudgetsRequest(2026, 10, 1m, null));

        Assert.Equal(expected, response.StatusCode);
        var budget = await family.Owner.Client.GetFromJsonAsync<ExpenseBudgetResponse>($"{Budgets(family.Id)}?year=2026&month=10");
        Assert.Equal(expected == HttpStatusCode.OK ? 1m : 50_000m, budget!.OverallAmount);
    }

    [Fact]
    public async Task Budgets_and_the_summary_of_an_unknown_family_return_404()
    {
        var user = await fixture.RegisterAndLoginAsync();
        var familyId = Guid.NewGuid();

        var summary = await user.Client.GetAsync($"{Expenses(familyId)}/summary?year=2026&month=10");
        var view = await user.Client.GetAsync($"{Budgets(familyId)}?year=2026&month=10");
        var set = await user.Client.PutAsJsonAsync(Budgets(familyId), new SetExpenseBudgetsRequest(2026, 10, 1m, null));

        Assert.All([summary, view, set], r => Assert.Equal(HttpStatusCode.NotFound, r.StatusCode));
        Assert.Equal("Family not found.", (await set.Content.ReadFromJsonAsync<ErrorResponse>())!.Message);
    }

    [Fact]
    public async Task Members_can_view_budgets_and_the_summary()
    {
        var family = await fixture.CreateFamilyWithOwnerAsync();
        var member = await fixture.AddMemberAsync(family, FamilyRole.Member);
        var set = await family.Owner.Client.PutAsJsonAsync(Budgets(family.Id), new SetExpenseBudgetsRequest(2026, 10, 50_000m, null));
        await set.EnsureStatusAsync(HttpStatusCode.OK);

        var budget = await member.Client.GetFromJsonAsync<ExpenseBudgetResponse>($"{Budgets(family.Id)}?year=2026&month=10");
        var summary = await member.Client.GetFromJsonAsync<ExpenseSummaryResponse>($"{Expenses(family.Id)}/summary?year=2026&month=10");

        Assert.Equal(50_000m, budget!.OverallAmount);
        Assert.Equal((0m, 50_000m), (summary!.TotalSpent, summary.Budget));
    }

    [Fact]
    public async Task Another_familys_budgets_cannot_be_changed_and_its_categories_cannot_be_budgeted()
    {
        var victim = await fixture.CreateFamilyWithOwnerAsync();
        var attacker = await fixture.CreateFamilyWithOwnerAsync();
        var victimCategory = (await victim.Owner.GetExpenseCategoriesAsync(victim.Id))[0];
        var set = await victim.Owner.Client.PutAsJsonAsync(Budgets(victim.Id), new SetExpenseBudgetsRequest(2026, 10, 50_000m, null));
        await set.EnsureStatusAsync(HttpStatusCode.OK);

        var overwrite = await attacker.Owner.Client.PutAsJsonAsync(Budgets(victim.Id), new SetExpenseBudgetsRequest(2026, 10, 1m, null));
        var foreignCategory = await attacker.Owner.Client.PutAsJsonAsync(Budgets(attacker.Id),
            new SetExpenseBudgetsRequest(2026, 10, 10m, [new CategoryBudgetRequest(victimCategory.Id, 5m)]));

        Assert.Equal(HttpStatusCode.Forbidden, overwrite.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, foreignCategory.StatusCode);
        var victimBudget = await victim.Owner.Client.GetFromJsonAsync<ExpenseBudgetResponse>($"{Budgets(victim.Id)}?year=2026&month=10");
        Assert.Equal((50_000m, 0), (victimBudget!.OverallAmount!.Value, victimBudget.Categories.Count));
        var attackerBudget = await attacker.Owner.Client.GetFromJsonAsync<ExpenseBudgetResponse>($"{Budgets(attacker.Id)}?year=2026&month=10");
        Assert.Null(attackerBudget!.OverallAmount);
    }

    [Fact]
    public async Task Another_familys_expense_and_category_are_not_reachable_through_your_own_family_route()
    {
        var victim = await fixture.CreateFamilyWithOwnerAsync();
        var attacker = await fixture.CreateFamilyWithOwnerAsync();
        var expense = await victim.Owner.CreateExpenseAsync(victim.Id, 4200m);
        var attackerCategory = (await attacker.Owner.GetExpenseCategoriesAsync(attacker.Id))[0];

        var read = await attacker.Owner.Client.GetAsync($"{Expenses(attacker.Id)}/{expense.Id}");
        var update = await attacker.Owner.Client.PutAsJsonAsync($"{Expenses(attacker.Id)}/{expense.Id}",
            new UpdateExpenseRequest(1m, attackerCategory.Id, Today, attacker.Owner.UserId, null));
        var delete = await attacker.Owner.Client.DeleteAsync($"{Expenses(attacker.Id)}/{expense.Id}");
        var archiveCategory = await attacker.Owner.Client.PutAsJsonAsync($"{Categories(attacker.Id)}/{expense.CategoryId}",
            new UpdateExpenseCategoryRequest("Hacked", true));
        var deleteCategory = await attacker.Owner.Client.DeleteAsync($"{Categories(attacker.Id)}/{expense.CategoryId}");

        Assert.All([read, update, delete, archiveCategory, deleteCategory],
            r => Assert.Equal(HttpStatusCode.NotFound, r.StatusCode));
        var stillThere = await victim.Owner.Client.GetFromJsonAsync<ExpenseResponse>($"{Expenses(victim.Id)}/{expense.Id}");
        Assert.Equal((4200m, expense.CategoryName, false), (stillThere!.Amount, stillThere.CategoryName, stillThere.CategoryIsArchived));
    }

    [Fact]
    public async Task Another_familys_category_or_payer_cannot_be_used()
    {
        var family = await fixture.CreateFamilyWithOwnerAsync();
        var other = await fixture.CreateFamilyWithOwnerAsync();
        var ownCategory = (await family.Owner.GetExpenseCategoriesAsync(family.Id))[0];
        var foreignCategory = (await other.Owner.GetExpenseCategoriesAsync(other.Id))[0];

        var withForeignCategory = await family.Owner.Client.PostAsJsonAsync(Expenses(family.Id),
            new CreateExpenseRequest(10m, foreignCategory.Id, Today, family.Owner.UserId, null));
        var withForeignPayer = await family.Owner.Client.PostAsJsonAsync(Expenses(family.Id),
            new CreateExpenseRequest(10m, ownCategory.Id, Today, other.Owner.UserId, null));

        Assert.Equal(HttpStatusCode.BadRequest, withForeignCategory.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, withForeignPayer.StatusCode);
        var month = await family.Owner.Client.GetFromJsonAsync<List<ExpenseResponse>>(
            $"{Expenses(family.Id)}?year={Today.Year}&month={Today.Month}");
        Assert.Empty(month!);
    }

    [Fact]
    public async Task Removing_a_member_keeps_their_expenses_and_revokes_access()
    {
        var family = await fixture.CreateFamilyWithOwnerAsync();
        var member = await fixture.AddMemberAsync(family, FamilyRole.Member);
        var membership = (await family.Owner.GetMembersAsync(family.Id)).Single(m => m.UserId == member.UserId);
        var expense = await member.CreateExpenseAsync(family.Id, 650m);

        var remove = await family.Owner.Client.DeleteAsync($"/api/families/{family.Id}/members/{membership.Id}");

        await remove.EnsureStatusAsync(HttpStatusCode.NoContent);
        var kept = await family.Owner.Client.GetFromJsonAsync<ExpenseResponse>($"{Expenses(family.Id)}/{expense.Id}");
        Assert.Equal((650m, member.UserId, "Member Tester"), (kept!.Amount, kept.PaidByUserId, kept.PaidByName));
        var afterRemoval = await member.Client.GetAsync($"{Expenses(family.Id)}?year={Today.Year}&month={Today.Month}");
        Assert.Equal(HttpStatusCode.Forbidden, afterRemoval.StatusCode);
        // The expense can still be corrected while keeping its former payer.
        var correct = await family.Owner.Client.PutAsJsonAsync($"{Expenses(family.Id)}/{expense.Id}",
            new UpdateExpenseRequest(600m, expense.CategoryId, expense.Date, member.UserId, null));
        await correct.EnsureStatusAsync(HttpStatusCode.OK);
    }
}
