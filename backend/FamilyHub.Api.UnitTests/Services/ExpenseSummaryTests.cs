using FamilyHub.Api.Common;
using FamilyHub.Api.Data;
using FamilyHub.Api.DTOs.Expenses;
using FamilyHub.Api.Models;
using FamilyHub.Api.Models.Enums;
using FamilyHub.Api.Services;
using FamilyHub.Api.UnitTests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;

namespace FamilyHub.Api.UnitTests.Services;

/// <summary>The monthly dashboard summary from <see cref="ExpenseService.GetSummaryAsync"/>.</summary>
public class ExpenseSummaryTests
{
    private static ExpenseService Expenses(AppDbContext db) => new(db, NullLogger<ExpenseService>.Instance);

    private static ExpenseBudgetService Budgets(AppDbContext db) => new(db, NullLogger<ExpenseBudgetService>.Instance);

    private static async Task<ExpenseCategory> AddCategoryAsync(
        AppDbContext db, Guid familyId, string name, int sortOrder, DefaultExpenseCategory? key = null)
    {
        var category = new ExpenseCategory { Id = Guid.NewGuid(), FamilyId = familyId, Name = name, SortOrder = sortOrder, DefaultCategory = key };
        db.ExpenseCategories.Add(category);
        await db.SaveChangesAsync();
        return category;
    }

    private static async Task AddAsync(AppDbContext db, Guid familyId, Guid categoryId, decimal amount, DateOnly date) =>
        Assert.True((await Expenses(db).CreateExpenseAsync("ana", familyId,
            new CreateExpenseRequest(amount, categoryId, date, "ana", null))).Succeeded);

    [Fact]
    public async Task The_summary_shows_spending_against_the_budget_for_the_selected_month()
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner));
        var food = await AddCategoryAsync(db, family.Id, "Food & Groceries", 0, DefaultExpenseCategory.FoodGroceries);
        var transport = await AddCategoryAsync(db, family.Id, "Transport", 2, DefaultExpenseCategory.Transport);
        var fun = await AddCategoryAsync(db, family.Id, "Entertainment", 6, DefaultExpenseCategory.Entertainment);
        await Budgets(db).SetBudgetsAsync("ana", family.Id, new SetExpenseBudgetsRequest(2026, 10, 50_000m,
            [new CategoryBudgetRequest(food.Id, 15_000m), new CategoryBudgetRequest(fun.Id, 4_000m)]));
        await AddAsync(db, family.Id, food.Id, 12_000m, new DateOnly(2026, 10, 3));
        await AddAsync(db, family.Id, food.Id, 4_500.50m, new DateOnly(2026, 10, 20));
        await AddAsync(db, family.Id, transport.Id, 15_499.50m, new DateOnly(2026, 10, 31));
        await AddAsync(db, family.Id, food.Id, 9_999m, new DateOnly(2026, 9, 30));

        var summary = (await Expenses(db).GetSummaryAsync("ana", family.Id, 2026, 10)).Value!;

        Assert.Equal((32_000m, 3, 50_000m, 18_000m, 64), (summary.TotalSpent, summary.ExpenseCount, summary.Budget, summary.Remaining, summary.UsagePercent));
        Assert.Equal([food.Id, transport.Id, fun.Id], summary.Categories.Select(c => c.CategoryId));
        var foodRow = summary.Categories[0];
        Assert.Equal((16_500.50m, 2, 15_000m, -1_500.50m, 110), (foodRow.Spent, foodRow.ExpenseCount, foodRow.Budget, foodRow.Remaining, foodRow.UsagePercent));
        Assert.Equal(DefaultExpenseCategory.FoodGroceries, foodRow.DefaultCategory);
        var transportRow = summary.Categories[1];
        Assert.Equal((15_499.50m, (decimal?)null, (int?)null), (transportRow.Spent, transportRow.Budget, transportRow.UsagePercent));
        var funRow = summary.Categories[2];
        Assert.Equal((0m, 0, 4_000m, 4_000m, 0), (funRow.Spent, funRow.ExpenseCount, funRow.Budget, funRow.Remaining, funRow.UsagePercent));
    }

    [Fact]
    public async Task The_budget_carries_into_later_months_while_spending_is_per_month()
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner));
        var food = await AddCategoryAsync(db, family.Id, "Food", 0);
        await Budgets(db).SetBudgetsAsync("ana", family.Id, new SetExpenseBudgetsRequest(2026, 10, 50_000m, null));
        await AddAsync(db, family.Id, food.Id, 32_000m, new DateOnly(2026, 10, 3));
        await AddAsync(db, family.Id, food.Id, 5_000m, new DateOnly(2026, 11, 3));

        var november = (await Expenses(db).GetSummaryAsync("ana", family.Id, 2026, 11)).Value!;
        var september = (await Expenses(db).GetSummaryAsync("ana", family.Id, 2026, 9)).Value!;

        Assert.Equal((5_000m, 50_000m, 45_000m, 10), (november.TotalSpent, november.Budget, november.Remaining, november.UsagePercent));
        Assert.Equal((0m, 0, (decimal?)null, (decimal?)null, (int?)null),
            (september.TotalSpent, september.ExpenseCount, september.Budget, september.Remaining, september.UsagePercent));
        Assert.Empty(september.Categories);
        Assert.Empty(september.RecentExpenses);
    }

    [Fact]
    public async Task Over_budget_shows_a_negative_remainder_and_more_than_100_percent()
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner));
        var food = await AddCategoryAsync(db, family.Id, "Food", 0);
        await Budgets(db).SetBudgetsAsync("ana", family.Id, new SetExpenseBudgetsRequest(2026, 10, 3m, null));
        await AddAsync(db, family.Id, food.Id, 5m, new DateOnly(2026, 10, 3));

        var summary = (await Expenses(db).GetSummaryAsync("ana", family.Id, 2026, 10)).Value!;

        Assert.Equal((-2m, 167), (summary.Remaining!.Value, summary.UsagePercent!.Value));
    }

    [Fact]
    public async Task A_tiny_budget_with_huge_spending_caps_the_percentage_instead_of_failing()
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner));
        var food = await AddCategoryAsync(db, family.Id, "Food", 0);
        await Budgets(db).SetBudgetsAsync("ana", family.Id,
            new SetExpenseBudgetsRequest(2026, 10, 0.01m, [new CategoryBudgetRequest(food.Id, 0.01m)]));
        await AddAsync(db, family.Id, food.Id, 10_000_000m, new DateOnly(2026, 10, 3));

        var summary = (await Expenses(db).GetSummaryAsync("ana", family.Id, 2026, 10)).Value!;

        Assert.Equal((int.MaxValue, -9_999_999.99m), (summary.UsagePercent!.Value, summary.Remaining!.Value));
        Assert.Equal(int.MaxValue, summary.Categories[0].UsagePercent);
    }

    [Fact]
    public async Task Recent_expenses_are_the_five_newest_of_the_month()
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner));
        var food = await AddCategoryAsync(db, family.Id, "Food", 0);
        for (var day = 1; day <= 7; day++)
        {
            await AddAsync(db, family.Id, food.Id, day, new DateOnly(2026, 10, day));
        }
        await AddAsync(db, family.Id, food.Id, 100m, new DateOnly(2026, 11, 1));

        var summary = (await Expenses(db).GetSummaryAsync("ana", family.Id, 2026, 10)).Value!;

        Assert.Equal([7m, 6m, 5m, 4m, 3m], summary.RecentExpenses.Select(e => e.Amount));
        Assert.Equal((7, 28m), (summary.ExpenseCount, summary.TotalSpent));
    }

    [Fact]
    public async Task The_summary_only_counts_the_familys_own_expenses()
    {
        await using var db = TestDb.Create();
        var (familyA, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner));
        var (familyB, _) = await TestDb.AddFamilyAsync(db, ("zed", FamilyRole.Owner));
        var foodA = await AddCategoryAsync(db, familyA.Id, "Food", 0);
        var foodB = await AddCategoryAsync(db, familyB.Id, "Food", 0);
        await AddAsync(db, familyA.Id, foodA.Id, 100m, new DateOnly(2026, 10, 3));
        db.Expenses.Add(new Expense
        {
            Id = Guid.NewGuid(), FamilyId = familyB.Id, CategoryId = foodB.Id, Amount = 9_999m,
            Date = new DateOnly(2026, 10, 3), PaidByUserId = "zed", CreatedByUserId = "zed",
        });
        await db.SaveChangesAsync();

        var summary = (await Expenses(db).GetSummaryAsync("ana", familyA.Id, 2026, 10)).Value!;

        Assert.Equal(100m, summary.TotalSpent);
        Assert.Equal(foodA.Id, Assert.Single(summary.Categories).CategoryId);
    }

    [Theory]
    [InlineData(FamilyRole.Child, ErrorType.Forbidden)]
    [InlineData(FamilyRole.Member, null)]
    public async Task Members_can_see_the_summary_and_children_cannot(FamilyRole role, ErrorType? expected)
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner), ("bob", role));

        var result = await Expenses(db).GetSummaryAsync("bob", family.Id, 2026, 10);

        Assert.Equal(expected, result.ErrorType);
    }
}
