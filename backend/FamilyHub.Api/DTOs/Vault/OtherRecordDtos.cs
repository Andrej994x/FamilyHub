namespace FamilyHub.Api.DTOs.Vault;

public class CreateOtherRecordRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset? ImportantDate { get; set; }
    public DateTimeOffset? ExpiryDate { get; set; }
    public List<IFormFile>? Attachments { get; set; }
}

public class UpdateOtherRecordRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset? ImportantDate { get; set; }
    public DateTimeOffset? ExpiryDate { get; set; }
    /// <summary>New files to append to the record.</summary>
    public List<IFormFile>? Attachments { get; set; }
}

public record OtherRecordResponse(
    Guid Id,
    Guid FamilyId,
    string Title,
    string? Description,
    DateTimeOffset? ImportantDate,
    DateTimeOffset? ExpiryDate,
    string CreatedByUserId,
    DateTimeOffset CreatedAt,
    IReadOnlyList<VaultAttachmentResponse> Attachments);
