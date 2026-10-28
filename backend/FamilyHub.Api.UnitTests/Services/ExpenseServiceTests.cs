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

public class ExpenseServiceTests
{
    private static readonly DateOnly Oct10 = new(2026, 10, 10);

    private static ExpenseService Service(AppDbContext db) => new(db, NullLogger<ExpenseService>.Instance);

    private static async Task<ExpenseCategory> AddCategoryAsync(
        AppDbContext db, Guid familyId, string name = "Food", bool archived = false)
    {
        var category = new ExpenseCategory { Id = Guid.NewGuid(), FamilyId = familyId, Name = name, IsArchived = archived };
        db.ExpenseCategories.Add(category);
        await db.SaveChangesAsync();
        return category;
    }

    private static CreateExpenseRequest NewExpense(Guid categoryId, string paidBy, decimal amount = 1500m, DateOnly? date = null) =>
        new(amount, categoryId, date ?? Oct10, paidBy, "  Weekly groceries  ");

    private static UpdateExpenseRequest Change(Guid categoryId, string paidBy, decimal amount = 2000m) =>
        new(amount, categoryId, Oct10, paidBy, null);

    [Theory]
    [InlineData(FamilyRole.Owner)]
    [InlineData(FamilyRole.Parent)]
    [InlineData(FamilyRole.Member)]
    public async Task Adults_can_add_expenses_paid_by_any_family_member(FamilyRole role)
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner), ("bob", role), ("kid", FamilyRole.Child));
        db.Users.Add(new ApplicationUser { Id = "kid", FirstName = "Mia", LastName = "Test" });
        await db.SaveChangesAsync();
        var category = await AddCategoryAsync(db, family.Id);

        var result = await Service(db).CreateExpenseAsync("bob", family.Id, NewExpense(category.Id, paidBy: "kid", amount: 1234.56m));

        Assert.True(result.Succeeded);
        Assert.Equal(1234.56m, result.Value!.Amount);
        Assert.Equal(("kid", "Mia Test", "bob"), (result.Value.PaidByUserId, result.Value.PaidByName, result.Value.CreatedByUserId));
        Assert.Equal(("Food", "Weekly groceries"), (result.Value.CategoryName, result.Value.Description));
    }

    [Fact]
    public async Task Children_cannot_read_or_add_expenses()
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner), ("kid", FamilyRole.Child));
        var category = await AddCategoryAsync(db, family.Id);
        var service = Service(db);
        var expense = (await service.CreateExpenseAsync("ana", family.Id, NewExpense(category.Id, "ana"))).Value!;

        var list = await service.GetExpensesAsync("kid", family.Id, new ExpenseFilter { Year = 2026, Month = 10 });
        var get = await service.GetExpenseAsync("kid", family.Id, expense.Id);
        var create = await service.CreateExpenseAsync("kid", family.Id, NewExpense(category.Id, "kid"));

        Assert.All(new Result[] { list, get, create }, r =>
        {
            Assert.Equal(ErrorType.Forbidden, r.ErrorType);
            Assert.Equal("Children do not have access to family expenses.", r.Error);
        });
        Assert.Single(db.Expenses);
    }

    [Fact]
    public async Task A_missing_family_is_not_found_and_a_stranger_is_forbidden()
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner));
        var filter = new ExpenseFilter { Year = 2026, Month = 10 };

        var missing = await Service(db).GetExpensesAsync("ana", Guid.NewGuid(), filter);
        var stranger = await Service(db).GetExpensesAsync("stranger", family.Id, filter);

        Assert.Equal(ErrorType.NotFound, missing.ErrorType);
        Assert.Equal(ErrorType.Forbidden, stranger.ErrorType);
    }

    [Fact]
    public async Task Members_can_change_only_the_expenses_they_added()
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner), ("bob", FamilyRole.Member));
        var category = await AddCategoryAsync(db, family.Id);
        var service = Service(db);
        var anas = (await service.CreateExpenseAsync("ana", family.Id, NewExpense(category.Id, "ana"))).Value!;
        var bobs = (await service.CreateExpenseAsync("bob", family.Id, NewExpense(category.Id, "ana"))).Value!;

        var updateOthers = await service.UpdateExpenseAsync("bob", family.Id, anas.Id, Change(category.Id, "bob"));
        var deleteOthers = await service.DeleteExpenseAsync("bob", family.Id, anas.Id);
        var updateOwn = await service.UpdateExpenseAsync("bob", family.Id, bobs.Id, Change(category.Id, "bob"));
        var deleteOwn = await service.DeleteExpenseAsync("bob", family.Id, bobs.Id);

        Assert.Equal(ErrorType.Forbidden, updateOthers.ErrorType);
        Assert.Equal(ErrorType.Forbidden, deleteOthers.ErrorType);
        Assert.True(updateOwn.Succeeded);
        Assert.True(deleteOwn.Succeeded);
        var remaining = await db.Expenses.SingleAsync();
        Assert.Equal((anas.Id, 1500m), (remaining.Id, remaining.Amount));
    }

    [Theory]
    [InlineData(FamilyRole.Owner)]
    [InlineData(FamilyRole.Parent)]
    public async Task Owners_and_parents_can_change_anyones_expenses(FamilyRole role)
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db, ("ana", role), ("bob", FamilyRole.Member));
        var category = await AddCategoryAsync(db, family.Id);
        var service = Service(db);
        var first = (await service.CreateExpenseAsync("bob", family.Id, NewExpense(category.Id, "bob"))).Value!;
        var second = (await service.CreateExpenseAsync("bob", family.Id, NewExpense(category.Id, "bob"))).Value!;

        var update = await service.UpdateExpenseAsync("ana", family.Id, first.Id, Change(category.Id, "ana", 99m));
        var delete = await service.DeleteExpenseAsync("ana", family.Id, second.Id);

        Assert.True(update.Succeeded);
        Assert.Equal((99m, "ana"), (update.Value!.Amount, update.Value.PaidByUserId));
        Assert.NotNull(update.Value.UpdatedAt);
        Assert.True(delete.Succeeded);
        Assert.Single(db.Expenses);
    }

    [Fact]
    public async Task An_expense_is_looked_up_by_id_and_family()
    {
        await using var db = TestDb.Create();
        var (familyA, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner));
        var (familyB, _) = await TestDb.AddFamilyAsync(db, ("zed", FamilyRole.Owner));
        var categoryB = await AddCategoryAsync(db, familyB.Id);
        var categoryA = await AddCategoryAsync(db, familyA.Id);
        var service = Service(db);
        var expenseOfB = (await service.CreateExpenseAsync("zed", familyB.Id, NewExpense(categoryB.Id, "zed"))).Value!;

        // Ana is a member of A but targets B's expense through A's route.
        var get = await service.GetExpenseAsync("ana", familyA.Id, expenseOfB.Id);
        var update = await service.UpdateExpenseAsync("ana", familyA.Id, expenseOfB.Id, Change(categoryA.Id, "ana"));
        var delete = await service.DeleteExpenseAsync("ana", familyA.Id, expenseOfB.Id);

        Assert.All(new Result[] { get, update, delete }, r => Assert.Equal(ErrorType.NotFound, r.ErrorType));
        Assert.Equal(1500m, (await db.Expenses.SingleAsync()).Amount);
    }

    [Fact]
    public async Task Another_familys_category_or_payer_is_rejected()
    {
        await using var db = TestDb.Create();
        var (familyA, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner));
        var (familyB, _) = await TestDb.AddFamilyAsync(db, ("zed", FamilyRole.Owner));
        var categoryA = await AddCategoryAsync(db, familyA.Id);
        var categoryB = await AddCategoryAsync(db, familyB.Id);
        var service = Service(db);

        var foreignCategory = await service.CreateExpenseAsync("ana", familyA.Id, NewExpense(categoryB.Id, "ana"));
        var foreignPayer = await service.CreateExpenseAsync("ana", familyA.Id, NewExpense(categoryA.Id, "zed"));

        Assert.Equal(ErrorType.Validation, foreignCategory.ErrorType);
        Assert.Equal("The category does not exist in this family.", foreignCategory.Error);
        Assert.Equal(ErrorType.Validation, foreignPayer.ErrorType);
        Assert.Equal("The payer must be a member of this family.", foreignPayer.Error);
        Assert.Empty(db.Expenses);
    }

    [Fact]
    public async Task An_archived_category_cannot_be_chosen_but_existing_expenses_keep_it()
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner));
        var food = await AddCategoryAsync(db, family.Id, "Food");
        var old = await AddCategoryAsync(db, family.Id, "Old", archived: true);
        var service = Service(db);
        var expense = (await service.CreateExpenseAsync("ana", family.Id, NewExpense(food.Id, "ana"))).Value!;
        food.IsArchived = true;
        await db.SaveChangesAsync();

        var create = await service.CreateExpenseAsync("ana", family.Id, NewExpense(old.Id, "ana"));
        var keep = await service.UpdateExpenseAsync("ana", family.Id, expense.Id, Change(food.Id, "ana", 1800m));
        var switchToArchived = await service.UpdateExpenseAsync("ana", family.Id, expense.Id, Change(old.Id, "ana"));

        Assert.Equal(ErrorType.Validation, create.ErrorType);
        Assert.True(keep.Succeeded);
        Assert.True(keep.Value!.CategoryIsArchived);
        Assert.Equal(ErrorType.Validation, switchToArchived.ErrorType);
    }

    [Fact]
    public async Task An_expense_can_keep_a_payer_who_left_the_family_but_not_switch_to_one()
    {
        await using var db = TestDb.Create();
        var (family, members) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner), ("bob", FamilyRole.Member));
        var category = await AddCategoryAsync(db, family.Id);
        var service = Service(db);
        var bobPaid = (await service.CreateExpenseAsync("ana", family.Id, NewExpense(category.Id, "bob"))).Value!;
        var anaPaid = (await service.CreateExpenseAsync("ana", family.Id, NewExpense(category.Id, "ana"))).Value!;
        db.FamilyMembers.Remove(members[1]);
        await db.SaveChangesAsync();

        var keep = await service.UpdateExpenseAsync("ana", family.Id, bobPaid.Id, Change(category.Id, "bob", 700m));
        var switchToFormer = await service.UpdateExpenseAsync("ana", family.Id, anaPaid.Id, Change(category.Id, "bob"));

        Assert.True(keep.Succeeded);
        Assert.Equal(ErrorType.Validation, switchToFormer.ErrorType);
    }

    [Fact]
    public async Task History_returns_one_month_newest_first_with_category_and_payer_filters()
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner), ("bob", FamilyRole.Parent));
        var food = await AddCategoryAsync(db, family.Id, "Food");
        var fuel = await AddCategoryAsync(db, family.Id, "Fuel");
        var service = Service(db);
        async Task<Guid> Add(Guid category, string paidBy, DateOnly date) =>
            (await service.CreateExpenseAsync("ana", family.Id, NewExpense(category, paidBy, date: date))).Value!.Id;

        var oct1 = await Add(food.Id, "ana", new DateOnly(2026, 10, 1));
        var oct31 = await Add(fuel.Id, "bob", new DateOnly(2026, 10, 31));
        var oct15 = await Add(food.Id, "bob", new DateOnly(2026, 10, 15));
        await Add(food.Id, "ana", new DateOnly(2026, 9, 30));
        await Add(food.Id, "ana", new DateOnly(2026, 11, 1));

        var october = await service.GetExpensesAsync("ana", family.Id, new ExpenseFilter { Year = 2026, Month = 10 });
        var octoberFood = await service.GetExpensesAsync("ana", family.Id,
            new ExpenseFilter { Year = 2026, Month = 10, CategoryId = food.Id });
        var octoberByBob = await service.GetExpensesAsync("ana", family.Id,
            new ExpenseFilter { Year = 2026, Month = 10, PaidByUserId = "bob" });

        Assert.Equal([oct31, oct15, oct1], october.Value!.Select(e => e.Id));
        Assert.Equal([oct15, oct1], octoberFood.Value!.Select(e => e.Id));
        Assert.Equal([oct31, oct15], octoberByBob.Value!.Select(e => e.Id));
    }
}
