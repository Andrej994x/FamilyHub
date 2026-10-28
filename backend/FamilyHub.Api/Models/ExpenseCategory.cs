using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.Models;

/// <summary>
/// A family's expense category. Categories in use are archived rather than deleted, so
/// historical expenses keep their category.
/// </summary>
public class ExpenseCategory
{
    public Guid Id { get; set; }

    public Guid FamilyId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Set for the built-in categories; cleared when an admin renames one.</summary>
    public DefaultExpenseCategory? DefaultCategory { get; set; }

    /// <summary>Archived categories can't be used for new expenses but stay on existing ones.</summary>
    public bool IsArchived { get; set; }

    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public Family? Family { get; set; }
}
