namespace FamilyHub.Api.Models;

public class ShoppingList
{
    public Guid Id { get; set; }

    public Guid FamilyId { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public Family? Family { get; set; }

    public ICollection<ShoppingItem> Items { get; set; } = new List<ShoppingItem>();
}
