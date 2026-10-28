using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.DTOs.Expenses;

/// <summary>
/// An expense category. When <see cref="DefaultCategory"/> is set, the client shows its
/// translated label instead of <see cref="Name"/>.
/// </summary>
public record ExpenseCategoryResponse(
    Guid Id,
    string Name,
    DefaultExpenseCategory? DefaultCategory,
    bool IsArchived,
    int SortOrder);
