namespace FamilyHub.Api.DTOs.Expenses;

/// <summary>A new expense. <see cref="PaidByUserId"/> must be a current family member.</summary>
public record CreateExpenseRequest(
    decimal Amount,
    Guid CategoryId,
    DateOnly Date,
    string PaidByUserId,
    string? Description);
