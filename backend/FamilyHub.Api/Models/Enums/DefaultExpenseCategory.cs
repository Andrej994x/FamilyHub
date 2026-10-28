namespace FamilyHub.Api.Models.Enums;

/// <summary>
/// Identifies a built-in expense category so the client can show a translated label.
/// Custom categories (and renamed defaults) have no value.
/// </summary>
public enum DefaultExpenseCategory
{
    FoodGroceries,
    BillsUtilities,
    Transport,
    KidsEducation,
    Health,
    Shopping,
    Entertainment,
    Home,
    Other
}
