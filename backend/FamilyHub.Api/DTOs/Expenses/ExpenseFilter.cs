namespace FamilyHub.Api.DTOs.Expenses;

/// <summary>
/// Query-string filters for the expense history. Year and month are required so a request
/// always returns a single, bounded month. All filters are applied server-side.
/// </summary>
public class ExpenseFilter
{
    public int? Year { get; set; }

    public int? Month { get; set; }

    public Guid? CategoryId { get; set; }

    public string? PaidByUserId { get; set; }
}
