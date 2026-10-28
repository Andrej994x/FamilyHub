using FamilyHub.Api.Common;
using FamilyHub.Api.Data;
using FamilyHub.Api.DTOs.Expenses;
using FamilyHub.Api.Models;
using FamilyHub.Api.Models.Enums;
using FamilyHub.Api.Services;
using FamilyHub.Api.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FamilyHub.Api.UnitTests.Services;

public class ExpenseBudgetServiceTests
{
    private static ExpenseBudgetService Service(AppDbContext db) => new(db, NullLogger<ExpenseBudgetService>.Instance);

    private static async Task<ExpenseCategory> AddCategoryAsync(AppDbContext db, Guid familyId, string name, bool archived = false)
    {
        var category = new ExpenseCategory { Id = Guid.NewGuid(), FamilyId = familyId, Name = name, IsArchived = archived };
        db.ExpenseCategories.Add(category);
        await db.SaveChangesAsync();
        return category;
    }

    private static SetExpenseBudgetsRequest Set(int year, int month, decimal? overall, params (Guid Id, decimal? Amount)[] categories) =>
        new(year, month, overall, categories.Select(c => new CategoryBudgetRequest(c.Id, c.Amount)).ToList());

    private static async Task<ExpenseBudgetResponse> GetAsync(AppDbContext db, Guid familyId, int year, int month) =>
        (await Service(db).GetBudgetsAsync("ana", familyId, year, month)).Value!;

    [Fact]
    public async Task Without_budgets_a_month_has_no_overall_or_category_budget()
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner));

        var budgets = await GetAsync(db, family.Id, 2026, 10);

        Assert.Equal((2026, 10, (decimal?)null), (budgets.Year, budgets.Month, budgets.OverallAmount));
        Assert.Empty(budgets.Categories);
    }

    [Fact]
    public async Task A_budget_carries_forward_until_it_is_changed_and_earlier_months_keep_theirs()
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner));
        var food = await AddCategoryAsync(db, family.Id, "Food");
        var service = Service(db);

        await service.SetBudgetsAsync("ana", family.Id, Set(2026, 10, 50_000m, (food.Id, 15_000m)));
        await service.SetBudgetsAsync("ana", family.Id, Set(2027, 1, 60_000m, (food.Id, 15_000m)));

        var september = await GetAsync(db, family.Id, 2026, 9);
        var october = await GetAsync(db, family.Id, 2026, 10);
        var december = await GetAsync(db, family.Id, 2026, 12);
        var january = await GetAsync(db, family.Id, 2027, 1);
        var june = await GetAsync(db, family.Id, 2027, 6);

        Assert.Null(september.OverallAmount);
        Assert.Empty(september.Categories);
        Assert.Equal(50_000m, october.OverallAmount);
        Assert.Equal(50_000m, december.OverallAmount);
        Assert.Equal(60_000m, january.OverallAmount);
        Assert.Equal(60_000m, june.OverallAmount);
        Assert.All(new[] { october, december, january, june }, b =>
            Assert.Equal(new CategoryBudgetResponse(food.Id, 15_000m), Assert.Single(b.Categories)));
        // The unchanged food budget was not written again in January.
        Assert.Equal(1, await db.ExpenseBudgets.CountAsync(b => b.CategoryId == food.Id));
    }

    [Fact]
    public async Task Removing_a_budget_applies_from_that_month_on()
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Parent));
        var service = Service(db);

        await service.SetBudgetsAsync("ana", family.Id, Set(2026, 10, 50_000m));
        var cleared = await service.SetBudgetsAsync("ana", family.Id, Set(2026, 12, null));

        Assert.Null(cleared.Value!.OverallAmount);
        Assert.Equal(50_000m, (await GetAsync(db, family.Id, 2026, 11)).OverallAmount);
        Assert.Null((await GetAsync(db, family.Id, 2027, 3)).OverallAmount);
    }

    [Fact]
    public async Task Changing_the_same_month_again_updates_it_in_place()
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner));
        var service = Service(db);

        await service.SetBudgetsAsync("ana", family.Id, Set(2026, 10, 50_000m));
        var changed = await service.SetBudgetsAsync("ana", family.Id, Set(2026, 10, 45_500.50m));

        Assert.Equal(45_500.50m, changed.Value!.OverallAmount);
        var row = await db.ExpenseBudgets.SingleAsync();
        Assert.Equal((new DateOnly(2026, 10, 1), 45_500.50m), (row.EffectiveFrom, row.Amount));
    }

    [Fact]
    public async Task The_request_is_the_complete_set_so_an_omitted_category_loses_its_budget()
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner));
        var food = await AddCategoryAsync(db, family.Id, "Food");
        var fun = await AddCategoryAsync(db, family.Id, "Fun");
        var service = Service(db);

        await service.SetBudgetsAsync("ana", family.Id, Set(2026, 10, null, (food.Id, 15_000m), (fun.Id, 4_000m)));
        var november = await service.SetBudgetsAsync("ana", family.Id, Set(2026, 11, null, (food.Id, 16_000m)));

        Assert.Equal([new CategoryBudgetResponse(food.Id, 16_000m)], november.Value!.Categories);
        Assert.Equal(2, (await GetAsync(db, family.Id, 2026, 10)).Categories.Count);
    }

    [Fact]
    public async Task Members_can_view_budgets_but_only_owners_and_parents_can_change_them()
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner), ("bob", FamilyRole.Member));
        var service = Service(db);
        await service.SetBudgetsAsync("ana", family.Id, Set(2026, 10, 50_000m));

        var view = await service.GetBudgetsAsync("bob", family.Id, 2026, 10);
        var change = await service.SetBudgetsAsync("bob", family.Id, Set(2026, 10, 1m));

        Assert.Equal(50_000m, view.Value!.OverallAmount);
        Assert.Equal(ErrorType.Forbidden, change.ErrorType);
        Assert.Equal("Only an Owner or Parent can manage expense categories and budgets.", change.Error);
        Assert.Equal(50_000m, (await db.ExpenseBudgets.SingleAsync()).Amount);
    }

    [Fact]
    public async Task Children_and_strangers_have_no_access_and_a_missing_family_is_not_found()
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner), ("kid", FamilyRole.Child));
        var service = Service(db);

        var childView = await service.GetBudgetsAsync("kid", family.Id, 2026, 10);
        var childSet = await service.SetBudgetsAsync("kid", family.Id, Set(2026, 10, 1m));
        var stranger = await service.GetBudgetsAsync("zed", family.Id, 2026, 10);
        var missing = await service.GetBudgetsAsync("ana", Guid.NewGuid(), 2026, 10);

        Assert.Equal("Children do not have access to family expenses.", childView.Error);
        Assert.Equal(ErrorType.Forbidden, childSet.ErrorType);
        Assert.Equal(ErrorType.Forbidden, stranger.ErrorType);
        Assert.Equal(ErrorType.NotFound, missing.ErrorType);
        Assert.Empty(db.ExpenseBudgets);
    }

    [Fact]
    public async Task Another_familys_category_is_rejected_and_nothing_is_saved()
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner));
        var (other, _) = await TestDb.AddFamilyAsync(db, ("zed", FamilyRole.Owner));
        var foreign = await AddCategoryAsync(db, other.Id, "Food");

        var result = await Service(db).SetBudgetsAsync("ana", family.Id, Set(2026, 10, 50_000m, (foreign.Id, 1_000m)));

        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal("One or more categories do not exist in this family.", result.Error);
        Assert.Empty(db.ExpenseBudgets);
    }

    [Fact]
    public async Task Archived_categories_cannot_get_a_budget_but_can_have_it_removed()
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner));
        var old = await AddCategoryAsync(db, family.Id, "Old");
        var service = Service(db);
        await service.SetBudgetsAsync("ana", family.Id, Set(2026, 10, null, (old.Id, 2_000m)));
        old.IsArchived = true;
        await db.SaveChangesAsync();

        var add = await service.SetBudgetsAsync("ana", family.Id, Set(2026, 11, null, (old.Id, 3_000m)));
        var remove = await service.SetBudgetsAsync("ana", family.Id, Set(2026, 11, null, (old.Id, null)));

        Assert.Equal(ErrorType.Validation, add.ErrorType);
        Assert.Equal("Archived categories can't have a budget.", add.Error);
        Assert.True(remove.Succeeded);
        Assert.Empty(remove.Value!.Categories);
        Assert.Single((await GetAsync(db, family.Id, 2026, 10)).Categories);
    }

    [Fact]
    public async Task Budgets_are_kept_per_family()
    {
        await using var db = TestDb.Create();
        var (familyA, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner));
        var (familyB, _) = await TestDb.AddFamilyAsync(db, ("zed", FamilyRole.Owner));
        var service = Service(db);

        await service.SetBudgetsAsync("ana", familyA.Id, Set(2026, 10, 50_000m));
        await service.SetBudgetsAsync("zed", familyB.Id, Set(2026, 10, 20_000m));

        Assert.Equal(50_000m, (await GetAsync(db, familyA.Id, 2026, 10)).OverallAmount);
        Assert.Equal(20_000m, (await service.GetBudgetsAsync("zed", familyB.Id, 2026, 10)).Value!.OverallAmount);
    }
}
