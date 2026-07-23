namespace FamilyHub.Api.DTOs.Vault;

public class CreateWarrantyRequest
{
    public string ProductName { get; set; } = string.Empty;
    public string? Store { get; set; }
    public DateTimeOffset? PurchaseDate { get; set; }
    public DateTimeOffset? WarrantyExpiryDate { get; set; }
    public string? SerialNumber { get; set; }
    public string? Notes { get; set; }
    public bool IsImportant { get; set; }
    public Guid? RelatedMemberId { get; set; }
    public List<IFormFile>? Attachments { get; set; }
}

public class UpdateWarrantyRequest
{
    public string ProductName { get; set; } = string.Empty;
    public string? Store { get; set; }
    public DateTimeOffset? PurchaseDate { get; set; }
    public DateTimeOffset? WarrantyExpiryDate { get; set; }
    public string? SerialNumber { get; set; }
    public string? Notes { get; set; }
    public bool IsImportant { get; set; }
    public Guid? RelatedMemberId { get; set; }
    /// <summary>New files to append to the warranty (e.g. receipt).</summary>
    public List<IFormFile>? Attachments { get; set; }
}

public record WarrantyResponse(
    Guid Id,
    Guid FamilyId,
    string ProductName,
    string? Store,
    DateTimeOffset? PurchaseDate,
    DateTimeOffset? WarrantyExpiryDate,
    string? SerialNumber,
    string? Notes,
    bool IsImportant,
    Guid? RelatedMemberId,
    string CreatedByUserId,
    DateTimeOffset CreatedAt,
    IReadOnlyList<VaultAttachmentResponse> Attachments);
