using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.DTOs.Documents;

/// <summary>
/// Multipart/form-data payload for updating a document. Provide <see cref="Attachment"/>
/// to replace the current file, or set <see cref="RemoveAttachment"/> to clear it. When
/// neither is set the existing attachment is kept.
/// </summary>
public class UpdateFamilyDocumentRequest
{
    public Guid? FamilyMemberId { get; set; }

    public Guid? ChildProfileId { get; set; }

    public DocumentType DocumentType { get; set; }

    public string? DocumentNumber { get; set; }

    public DateTimeOffset? IssueDate { get; set; }

    public DateTimeOffset? ExpiryDate { get; set; }

    public string? Notes { get; set; }

    public IFormFile? Attachment { get; set; }

    public bool RemoveAttachment { get; set; }
}
