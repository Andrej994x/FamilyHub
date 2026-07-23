using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.Models;

public class ShoppingItem
{
    public Guid Id { get; set; }

    public Guid ShoppingListId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Free-form quantity, e.g. "2", "500g", "3 packs". Optional.</summary>
    public string? Quantity { get; set; }

    public ItemCategory Category { get; set; }

    public bool IsPurchased { get; set; }

    /// <summary>When true, adding or updating the item notifies the whole family.</summary>
    public bool IsImportant { get; set; }

    public string AddedByUserId { get; set; } = string.Empty;

    public string? PurchasedByUserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? PurchasedAt { get; set; }

    // Navigation properties
    public ShoppingList? ShoppingList { get; set; }
}
