namespace FamilyHub.Api.DTOs.Vault;

public class CreateHomeRecordRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Provider { get; set; }
    public DateTimeOffset? IssueDate { get; set; }
    public DateTimeOffset? RenewalDate { get; set; }
    public string? Notes { get; set; }
    public bool IsImportant { get; set; }
    public Guid? RelatedMemberId { get; set; }
    public List<IFormFile>? Attachments { get; set; }
}

public class UpdateHomeRecordRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Provider { get; set; }
    public DateTimeOffset? IssueDate { get; set; }
    public DateTimeOffset? RenewalDate { get; set; }
    public string? Notes { get; set; }
    public bool IsImportant { get; set; }
    public Guid? RelatedMemberId { get; set; }
    /// <summary>New files to append to the record.</summary>
    public List<IFormFile>? Attachments { get; set; }
}

public record HomeRecordResponse(
    Guid Id,
    Guid FamilyId,
    string Title,
    string? Type,
    string? Provider,
    DateTimeOffset? IssueDate,
    DateTimeOffset? RenewalDate,
    string? Notes,
    bool IsImportant,
    Guid? RelatedMemberId,
    string CreatedByUserId,
    DateTimeOffset CreatedAt,
    IReadOnlyList<VaultAttachmentResponse> Attachments);
