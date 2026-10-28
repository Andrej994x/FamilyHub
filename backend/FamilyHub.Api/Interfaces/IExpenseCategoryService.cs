using FamilyHub.Api.Common;
using FamilyHub.Api.DTOs.Expenses;

namespace FamilyHub.Api.Interfaces;

public interface IExpenseCategoryService
{
    Task<Result<IReadOnlyList<ExpenseCategoryResponse>>> GetCategoriesAsync(string userId, Guid familyId, bool includeArchived);

    Task<Result<ExpenseCategoryResponse>> GetCategoryAsync(string userId, Guid familyId, Guid categoryId);

    Task<Result<ExpenseCategoryResponse>> CreateCategoryAsync(string userId, Guid familyId, CreateExpenseCategoryRequest request);

    Task<Result<ExpenseCategoryResponse>> UpdateCategoryAsync(
        string userId, Guid familyId, Guid categoryId, UpdateExpenseCategoryRequest request);

    Task<Result> DeleteCategoryAsync(string userId, Guid familyId, Guid categoryId);
}
