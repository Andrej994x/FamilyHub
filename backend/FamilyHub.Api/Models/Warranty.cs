namespace FamilyHub.Api.Models;

public class Warranty
{
    public Guid Id { get; set; }

    public Guid FamilyId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string? Store { get; set; }

    public DateTimeOffset? PurchaseDate { get; set; }

    public DateTimeOffset? WarrantyExpiryDate { get; set; }

    public string? SerialNumber { get; set; }

    public string? Notes { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public Family? Family { get; set; }
}
