namespace FamilyHub.Api.DTOs.Expenses;

/// <summary>
/// Full update of an expense. An archived category or a payer who has left the family may be
/// kept, but not newly chosen.
/// </summary>
public record UpdateExpenseRequest(
    decimal Amount,
    Guid CategoryId,
    DateOnly Date,
    string PaidByUserId,
    string? Description);
