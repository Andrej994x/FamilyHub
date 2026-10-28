using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.DTOs.Expenses;

/// <summary>
/// The dashboard numbers for one month (MKD). Budget, Remaining and UsagePercent are null when no
/// budget is in effect; Remaining is negative and UsagePercent above 100 when over budget.
/// </summary>
public record ExpenseSummaryResponse(
    int Year,
    int Month,
    decimal TotalSpent,
    int ExpenseCount,
    decimal? Budget,
    decimal? Remaining,
    int? UsagePercent,
    IReadOnlyList<CategorySpendingResponse> Categories,
    IReadOnlyList<ExpenseResponse> RecentExpenses);

/// <summary>Spending in a category for the month; listed when it has spending or a budget.</summary>
public record CategorySpendingResponse(
    Guid CategoryId,
    string Name,
    DefaultExpenseCategory? DefaultCategory,
    bool IsArchived,
    decimal Spent,
    int ExpenseCount,
    decimal? Budget,
    decimal? Remaining,
    int? UsagePercent);
