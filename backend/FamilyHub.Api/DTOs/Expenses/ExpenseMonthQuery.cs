namespace FamilyHub.Api.DTOs.Expenses;

/// <summary>The month (query string) a budget or summary request is about. Both values are required.</summary>
public class ExpenseMonthQuery
{
    public int? Year { get; set; }

    public int? Month { get; set; }
}
