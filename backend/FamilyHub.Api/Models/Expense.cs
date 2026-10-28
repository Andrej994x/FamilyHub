namespace FamilyHub.Api.Models;

/// <summary>
/// A shared family expense, in MKD. People are referenced by user id (not membership), so an
/// expense keeps its payer and creator after they leave the family.
/// </summary>
public class Expense
{
    public Guid Id { get; set; }

    public Guid FamilyId { get; set; }

    public Guid CategoryId { get; set; }

    /// <summary>Amount in MKD, always positive, at most two decimals.</summary>
    public decimal Amount { get; set; }

    public DateOnly Date { get; set; }

    /// <summary>The user who paid; a family member when the expense was saved.</summary>
    public string PaidByUserId { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation properties
    public Family? Family { get; set; }

    public ExpenseCategory? Category { get; set; }
}
