namespace FamilyHub.Api.DTOs.Expenses;

/// <summary>
/// The complete set of budgets from the given month on: the overall budget (null = none) and the
/// category budgets. A category that currently has a budget but is not listed has it removed from
/// this month on. Earlier months keep the budgets they had.
/// </summary>
public record SetExpenseBudgetsRequest(
    int Year,
    int Month,
    decimal? OverallAmount,
    IReadOnlyList<CategoryBudgetRequest>? Categories);

/// <summary>A category budget; a null amount removes the category's budget from the month on.</summary>
public record CategoryBudgetRequest(Guid CategoryId, decimal? Amount);
