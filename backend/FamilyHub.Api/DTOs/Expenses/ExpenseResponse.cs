using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.DTOs.Expenses;

/// <summary>An expense with the names the UI needs. Amounts are in MKD.</summary>
public record ExpenseResponse(
    Guid Id,
    Guid FamilyId,
    decimal Amount,
    DateOnly Date,
    Guid CategoryId,
    string CategoryName,
    DefaultExpenseCategory? CategoryDefault,
    bool CategoryIsArchived,
    string PaidByUserId,
    string PaidByName,
    string? Description,
    string CreatedByUserId,
    string CreatedByName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
