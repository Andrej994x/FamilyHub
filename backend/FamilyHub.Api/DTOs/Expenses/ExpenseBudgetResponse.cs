namespace FamilyHub.Api.DTOs.Expenses;

/// <summary>
/// The budgets in effect for a month (MKD). A budget applies from the month it was set until it
/// is changed, so a month without its own change shows the latest earlier one. Null means no
/// overall budget; categories without a budget are not listed.
/// </summary>
public record ExpenseBudgetResponse(
    int Year,
    int Month,
    decimal? OverallAmount,
    IReadOnlyList<CategoryBudgetResponse> Categories);

public record CategoryBudgetResponse(Guid CategoryId, decimal Amount);
