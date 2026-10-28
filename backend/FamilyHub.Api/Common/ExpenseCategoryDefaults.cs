using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.Common;

/// <summary>
/// The built-in expense categories every family starts with, in display order. The English
/// name is stored; the client shows a translated label based on the category key.
/// </summary>
public static class ExpenseCategoryDefaults
{
    public static IReadOnlyList<(DefaultExpenseCategory Category, string Name)> All { get; } =
    [
        (DefaultExpenseCategory.FoodGroceries, "Food & Groceries"),
        (DefaultExpenseCategory.BillsUtilities, "Bills & Utilities"),
        (DefaultExpenseCategory.Transport, "Transport"),
        (DefaultExpenseCategory.KidsEducation, "Kids & Education"),
        (DefaultExpenseCategory.Health, "Health"),
        (DefaultExpenseCategory.Shopping, "Shopping"),
        (DefaultExpenseCategory.Entertainment, "Entertainment"),
        (DefaultExpenseCategory.Home, "Home"),
        (DefaultExpenseCategory.Other, "Other"),
    ];
}
