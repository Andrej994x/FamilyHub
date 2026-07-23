using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.DTOs.Documents;

/// <summary>
/// Multipart/form-data payload for creating a document. The subject is exactly one of
/// <see cref="FamilyMemberId"/> or <see cref="ChildProfileId"/>. <see cref="Attachment"/>
/// is the single optional image/PDF file.
/// </summary>
public class CreateFamilyDocumentRequest
{
    public Guid? FamilyMemberId { get; set; }

    public Guid? ChildProfileId { get; set; }

    public DocumentType DocumentType { get; set; }

    public string? DocumentNumber { get; set; }

    public DateTimeOffset? IssueDate { get; set; }

    public DateTimeOffset? ExpiryDate { get; set; }

    public string? Notes { get; set; }

    public IFormFile? Attachment { get; set; }
}
