using FamilyHub.Api.Common;
using FamilyHub.Api.Data;
using FamilyHub.Api.DTOs.Expenses;
using FamilyHub.Api.Interfaces;
using FamilyHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FamilyHub.Api.Services;

/// <summary>
/// Expense categories. Any member except a Child can read them; only Owners and Parents can
/// manage them. A category used by an expense can only be archived, never deleted.
/// </summary>
public class ExpenseCategoryService : ExpenseServiceBase, IExpenseCategoryService
{
    private const string NotFoundMessage = "Category not found in this family.";
    private const string DuplicateNameMessage = "A category with this name already exists (it may be archived).";

    public ExpenseCategoryService(AppDbContext db, ILogger<ExpenseCategoryService> logger)
        : base(db, logger)
    {
    }

    public async Task<Result<IReadOnlyList<ExpenseCategoryResponse>>> GetCategoriesAsync(
        string userId, Guid familyId, bool includeArchived)
    {
        var (membership, error, message) = await AuthorizeAsync(userId, familyId, requireManage: false);
        if (membership is null)
        {
            return Result<IReadOnlyList<ExpenseCategoryResponse>>.Failure(error!.Value, message!);
        }

        await EnsureDefaultCategoriesAsync(familyId);

        var query = Db.ExpenseCategories.Where(c => c.FamilyId == familyId);
        if (!includeArchived)
        {
            query = query.Where(c => !c.IsArchived);
        }

        IReadOnlyList<ExpenseCategoryResponse> categories = await query
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new ExpenseCategoryResponse(c.Id, c.Name, c.DefaultCategory, c.IsArchived, c.SortOrder))
            .ToListAsync();

        return Result<IReadOnlyList<ExpenseCategoryResponse>>.Success(categories);
    }

    public async Task<Result<ExpenseCategoryResponse>> GetCategoryAsync(string userId, Guid familyId, Guid categoryId)
    {
        var (membership, error, message) = await AuthorizeAsync(userId, familyId, requireManage: false);
        if (membership is null)
        {
            return Result<ExpenseCategoryResponse>.Failure(error!.Value, message!);
        }

        var category = await FindAsync(familyId, categoryId);
        return category is null
            ? Result<ExpenseCategoryResponse>.Failure(ErrorType.NotFound, NotFoundMessage)
            : Result<ExpenseCategoryResponse>.Success(ToResponse(category));
    }

    public async Task<Result<ExpenseCategoryResponse>> CreateCategoryAsync(
        string userId, Guid familyId, CreateExpenseCategoryRequest request)
    {
        var (membership, error, message) = await AuthorizeAsync(userId, familyId, requireManage: true);
        if (membership is null)
        {
            return Result<ExpenseCategoryResponse>.Failure(error!.Value, message!);
        }

        // A custom category must not stop the defaults from being created.
        await EnsureDefaultCategoriesAsync(familyId);

        var name = request.Name.Trim();
        if (await NameExistsAsync(familyId, name, exceptId: null))
        {
            return Result<ExpenseCategoryResponse>.Failure(ErrorType.Conflict, DuplicateNameMessage);
        }

        var maxSortOrder = await Db.ExpenseCategories
            .Where(c => c.FamilyId == familyId)
            .MaxAsync(c => (int?)c.SortOrder) ?? -1;

        var category = new ExpenseCategory
        {
            Id = Guid.NewGuid(),
            FamilyId = familyId,
            Name = name,
            SortOrder = maxSortOrder + 1,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        Db.ExpenseCategories.Add(category);
        return await SaveAsync(category);
    }

    public async Task<Result<ExpenseCategoryResponse>> UpdateCategoryAsync(
        string userId, Guid familyId, Guid categoryId, UpdateExpenseCategoryRequest request)
    {
        var (membership, error, message) = await AuthorizeAsync(userId, familyId, requireManage: true);
        if (membership is null)
        {
            return Result<ExpenseCategoryResponse>.Failure(error!.Value, message!);
        }

        await using var transaction = await LockFamilyCategoriesAsync(familyId);

        var category = await FindAsync(familyId, categoryId);
        if (category is null)
        {
            return Result<ExpenseCategoryResponse>.Failure(ErrorType.NotFound, NotFoundMessage);
        }

        var name = request.Name.Trim();
        if (name != category.Name)
        {
            if (await NameExistsAsync(familyId, name, exceptId: category.Id))
            {
                return Result<ExpenseCategoryResponse>.Failure(ErrorType.Conflict, DuplicateNameMessage);
            }

            // A renamed default is a custom category: its name is shown as typed in every language.
            category.Name = name;
            category.DefaultCategory = null;
        }

        if (request.IsArchived && !category.IsArchived && await IsLastActiveAsync(category))
        {
            return Result<ExpenseCategoryResponse>.Failure(
                ErrorType.Validation, "The family needs at least one active category.");
        }

        category.IsArchived = request.IsArchived;

        var result = await SaveAsync(category);
        if (result.Succeeded && transaction is not null)
        {
            await transaction.CommitAsync();
        }

        return result;
    }

    public async Task<Result> DeleteCategoryAsync(string userId, Guid familyId, Guid categoryId)
    {
        var (membership, error, message) = await AuthorizeAsync(userId, familyId, requireManage: true);
        if (membership is null)
        {
            return Result.Failure(error!.Value, message!);
        }

        await using var transaction = await LockFamilyCategoriesAsync(familyId);

        var category = await FindAsync(familyId, categoryId);
        if (category is null)
        {
            return Result.Failure(ErrorType.NotFound, NotFoundMessage);
        }

        if (await Db.Expenses.AnyAsync(e => e.CategoryId == category.Id))
        {
            return Result.Failure(ErrorType.Conflict, "This category is used by expenses. Archive it instead.");
        }

        if (!category.IsArchived && await IsLastActiveAsync(category))
        {
            return Result.Failure(ErrorType.Validation, "The family needs at least one active category.");
        }

        // Budget rows reference the category with Restrict, so remove them in the same save.
        var budgets = await Db.ExpenseBudgets.Where(b => b.CategoryId == category.Id).ToListAsync();
        Db.ExpenseBudgets.RemoveRange(budgets);
        Db.ExpenseCategories.Remove(category);
        await Db.SaveChangesAsync();

        if (transaction is not null)
        {
            await transaction.CommitAsync();
        }

        return Result.Success();
    }

    private Task<ExpenseCategory?> FindAsync(Guid familyId, Guid categoryId) =>
        Db.ExpenseCategories.FirstOrDefaultAsync(c => c.Id == categoryId && c.FamilyId == familyId);

    // ToLower keeps the check case-insensitive on every provider (SQL Server's collation
    // already is); the unique index is the final guard.
    private Task<bool> NameExistsAsync(Guid familyId, string name, Guid? exceptId) =>
        Db.ExpenseCategories.AnyAsync(c =>
            c.FamilyId == familyId && c.Id != exceptId && c.Name.ToLower() == name.ToLower());

    private async Task<bool> IsLastActiveAsync(ExpenseCategory category) =>
        !await Db.ExpenseCategories.AnyAsync(c =>
            c.FamilyId == category.FamilyId && c.Id != category.Id && !c.IsArchived);

    /// <summary>Saves the category, mapping a unique-name race to 409 instead of a 500.</summary>
    private async Task<Result<ExpenseCategoryResponse>> SaveAsync(ExpenseCategory category)
    {
        try
        {
            await Db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            if (!await NameTakenByAnotherAsync(category))
            {
                throw;
            }

            Db.Entry(category).State = EntityState.Detached;
            return Result<ExpenseCategoryResponse>.Failure(ErrorType.Conflict, DuplicateNameMessage);
        }

        return Result<ExpenseCategoryResponse>.Success(ToResponse(category));
    }

    private Task<bool> NameTakenByAnotherAsync(ExpenseCategory category) =>
        Db.ExpenseCategories.AsNoTracking().AnyAsync(c =>
            c.FamilyId == category.FamilyId && c.Id != category.Id && c.Name == category.Name);

    private static ExpenseCategoryResponse ToResponse(ExpenseCategory c) =>
        new(c.Id, c.Name, c.DefaultCategory, c.IsArchived, c.SortOrder);
}
