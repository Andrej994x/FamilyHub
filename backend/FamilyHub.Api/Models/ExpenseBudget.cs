namespace FamilyHub.Api.Models;

/// <summary>
/// A monthly budget that applies from <see cref="EffectiveFrom"/> until a later row for the
/// same family and category replaces it, so past months keep the budget they had.
/// </summary>
public class ExpenseBudget
{
    public Guid Id { get; set; }

    public Guid FamilyId { get; set; }

    /// <summary>Null for the overall family budget; otherwise a per-category budget.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>The first day of the month the budget starts applying.</summary>
    public DateOnly EffectiveFrom { get; set; }

    /// <summary>Budget in MKD; null means "no budget from this month on".</summary>
    public decimal? Amount { get; set; }

    public string UpdatedByUserId { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public Family? Family { get; set; }

    public ExpenseCategory? Category { get; set; }
}
