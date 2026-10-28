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

public class ExpenseCategoryServiceTests
{
    private static ExpenseCategoryService Service(AppDbContext db) => new(db, NullLogger<ExpenseCategoryService>.Instance);

    private static async Task<IReadOnlyList<ExpenseCategoryResponse>> ListAsync(
        AppDbContext db, string userId, Guid familyId, bool includeArchived = false) =>
        (await Service(db).GetCategoriesAsync(userId, familyId, includeArchived)).Value!;

    [Fact]
    public async Task The_default_categories_are_created_once_on_first_use()
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Member));

        var first = await ListAsync(db, "ana", family.Id);
        var second = await ListAsync(db, "ana", family.Id);

        Assert.Equal(ExpenseCategoryDefaults.All.Select(d => d.Category), first.Select(c => c.DefaultCategory!.Value));
        Assert.Equal(["Food & Groceries", "Bills & Utilities", "Transport", "Kids & Education", "Health",
            "Shopping", "Entertainment", "Home", "Other"], first.Select(c => c.Name));
        Assert.Equal(first.Select(c => c.Id), second.Select(c => c.Id));
        Assert.Equal(9, await db.ExpenseCategories.CountAsync());
    }

    [Fact]
    public async Task Defaults_are_seeded_per_family()
    {
        await using var db = TestDb.Create();
        var (familyA, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner));
        var (familyB, _) = await TestDb.AddFamilyAsync(db, ("zed", FamilyRole.Owner));

        var a = await ListAsync(db, "ana", familyA.Id);
        var b = await ListAsync(db, "zed", familyB.Id);

        Assert.Equal(9, a.Count);
        Assert.Equal(9, b.Count);
        Assert.Empty(a.Select(c => c.Id).Intersect(b.Select(c => c.Id)));
    }

    [Fact]
    public async Task Creating_a_custom_category_first_still_creates_the_defaults()
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner));

        var created = await Service(db).CreateCategoryAsync("ana", family.Id, new CreateExpenseCategoryRequest("  Fuel  "));

        Assert.True(created.Succeeded);
        Assert.Equal(("Fuel", (DefaultExpenseCategory?)null, 9), (created.Value!.Name, created.Value.DefaultCategory, created.Value.SortOrder));
        Assert.Equal(10, (await ListAsync(db, "ana", family.Id)).Count);
    }

    [Fact]
    public async Task Children_have_no_access_and_members_can_only_read()
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db,
            ("ana", FamilyRole.Owner), ("bob", FamilyRole.Member), ("kid", FamilyRole.Child));
        var service = Service(db);
        var category = (await ListAsync(db, "ana", family.Id))[0];

        var childList = await service.GetCategoriesAsync("kid", family.Id, includeArchived: false);
        var memberList = await service.GetCategoriesAsync("bob", family.Id, includeArchived: false);
        var memberCreate = await service.CreateCategoryAsync("bob", family.Id, new CreateExpenseCategoryRequest("Fuel"));
        var memberUpdate = await service.UpdateCategoryAsync("bob", family.Id, category.Id, new UpdateExpenseCategoryRequest("X", true));
        var memberDelete = await service.DeleteCategoryAsync("bob", family.Id, category.Id);

        Assert.Equal(ErrorType.Forbidden, childList.ErrorType);
        Assert.Equal("Children do not have access to family expenses.", childList.Error);
        Assert.True(memberList.Succeeded);
        Assert.All(new Result[] { memberCreate, memberUpdate, memberDelete }, r =>
        {
            Assert.Equal(ErrorType.Forbidden, r.ErrorType);
            Assert.Equal("Only an Owner or Parent can manage expense categories and budgets.", r.Error);
        });
        Assert.Equal(9, await db.ExpenseCategories.CountAsync(c => !c.IsArchived && c.DefaultCategory != null));
    }

    [Theory]
    [InlineData("Transport")]
    [InlineData("transport")]
    public async Task Duplicate_names_conflict_regardless_of_case(string name)
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Parent));
        var categories = await ListAsync(db, "ana", family.Id);
        var health = categories.Single(c => c.DefaultCategory == DefaultExpenseCategory.Health);

        var create = await Service(db).CreateCategoryAsync("ana", family.Id, new CreateExpenseCategoryRequest(name));
        var rename = await Service(db).UpdateCategoryAsync("ana", family.Id, health.Id, new UpdateExpenseCategoryRequest(name, false));

        Assert.Equal(ErrorType.Conflict, create.ErrorType);
        Assert.Equal(ErrorType.Conflict, rename.ErrorType);
    }

    [Fact]
    public async Task Renaming_a_default_makes_it_custom_and_keeping_the_name_keeps_it_default()
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner));
        var categories = await ListAsync(db, "ana", family.Id);
        var transport = categories.Single(c => c.DefaultCategory == DefaultExpenseCategory.Transport);
        var health = categories.Single(c => c.DefaultCategory == DefaultExpenseCategory.Health);
        var service = Service(db);

        var renamed = await service.UpdateCategoryAsync("ana", family.Id, transport.Id, new UpdateExpenseCategoryRequest("Car & fuel", false));
        var archived = await service.UpdateCategoryAsync("ana", family.Id, health.Id, new UpdateExpenseCategoryRequest("Health", true));

        Assert.Equal(("Car & fuel", (DefaultExpenseCategory?)null), (renamed.Value!.Name, renamed.Value.DefaultCategory));
        Assert.Equal((DefaultExpenseCategory.Health, true), (archived.Value!.DefaultCategory, archived.Value.IsArchived));
    }

    [Fact]
    public async Task Archived_categories_are_listed_only_on_request_and_can_be_restored()
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner));
        var other = (await ListAsync(db, "ana", family.Id)).Single(c => c.DefaultCategory == DefaultExpenseCategory.Other);
        var service = Service(db);

        await service.UpdateCategoryAsync("ana", family.Id, other.Id, new UpdateExpenseCategoryRequest("Other", true));
        var active = await ListAsync(db, "ana", family.Id);
        var all = await ListAsync(db, "ana", family.Id, includeArchived: true);
        var restored = await service.UpdateCategoryAsync("ana", family.Id, other.Id, new UpdateExpenseCategoryRequest("Other", false));

        Assert.Equal(8, active.Count);
        Assert.Equal(9, all.Count);
        Assert.False(restored.Value!.IsArchived);
        // Archiving everything must never trigger a second round of default seeding.
        Assert.Equal(9, await db.ExpenseCategories.CountAsync());
    }

    [Fact]
    public async Task A_category_used_by_an_expense_cannot_be_deleted()
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner));
        var food = (await ListAsync(db, "ana", family.Id))[0];
        await new ExpenseService(db, NullLogger<ExpenseService>.Instance).CreateExpenseAsync("ana", family.Id,
            new CreateExpenseRequest(100m, food.Id, new DateOnly(2026, 10, 1), "ana", null));

        var delete = await Service(db).DeleteCategoryAsync("ana", family.Id, food.Id);

        Assert.Equal(ErrorType.Conflict, delete.ErrorType);
        Assert.Equal("This category is used by expenses. Archive it instead.", delete.Error);
        Assert.True(await db.ExpenseCategories.AnyAsync(c => c.Id == food.Id));
    }

    [Fact]
    public async Task Deleting_an_unused_category_also_removes_its_budgets()
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner));
        var fun = (await ListAsync(db, "ana", family.Id)).Single(c => c.DefaultCategory == DefaultExpenseCategory.Entertainment);
        db.ExpenseBudgets.Add(new ExpenseBudget
        {
            Id = Guid.NewGuid(), FamilyId = family.Id, CategoryId = fun.Id,
            EffectiveFrom = new DateOnly(2026, 10, 1), Amount = 4000m, UpdatedByUserId = "ana",
        });
        await db.SaveChangesAsync();

        var delete = await Service(db).DeleteCategoryAsync("ana", family.Id, fun.Id);

        Assert.True(delete.Succeeded);
        Assert.False(await db.ExpenseCategories.AnyAsync(c => c.Id == fun.Id));
        Assert.Empty(db.ExpenseBudgets);
    }

    [Fact]
    public async Task The_last_active_category_cannot_be_archived_or_deleted()
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner));
        var categories = await ListAsync(db, "ana", family.Id);
        var service = Service(db);
        foreach (var category in categories.Skip(1))
        {
            await service.UpdateCategoryAsync("ana", family.Id, category.Id, new UpdateExpenseCategoryRequest(category.Name, true));
        }

        var last = categories[0];
        var archive = await service.UpdateCategoryAsync("ana", family.Id, last.Id, new UpdateExpenseCategoryRequest(last.Name, true));
        var delete = await service.DeleteCategoryAsync("ana", family.Id, last.Id);

        Assert.Equal(ErrorType.Validation, archive.ErrorType);
        Assert.Equal(ErrorType.Validation, delete.ErrorType);
        Assert.Single(await ListAsync(db, "ana", family.Id));
    }

    [Fact]
    public async Task A_category_is_looked_up_by_id_and_family()
    {
        await using var db = TestDb.Create();
        var (familyA, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner));
        var (familyB, _) = await TestDb.AddFamilyAsync(db, ("zed", FamilyRole.Owner));
        var categoryOfB = (await ListAsync(db, "zed", familyB.Id))[0];
        var service = Service(db);

        var get = await service.GetCategoryAsync("ana", familyA.Id, categoryOfB.Id);
        var update = await service.UpdateCategoryAsync("ana", familyA.Id, categoryOfB.Id, new UpdateExpenseCategoryRequest("Hacked", true));
        var delete = await service.DeleteCategoryAsync("ana", familyA.Id, categoryOfB.Id);

        Assert.All(new Result[] { get, update, delete }, r => Assert.Equal(ErrorType.NotFound, r.ErrorType));
        var untouched = await db.ExpenseCategories.SingleAsync(c => c.Id == categoryOfB.Id);
        Assert.Equal((categoryOfB.Name, false), (untouched.Name, untouched.IsArchived));
    }
}
