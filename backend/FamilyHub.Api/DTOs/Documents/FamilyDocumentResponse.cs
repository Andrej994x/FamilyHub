using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.DTOs.Documents;

public record FamilyDocumentResponse(
    Guid Id,
    Guid FamilyId,
    Guid? FamilyMemberId,
    Guid? ChildProfileId,
    DocumentType DocumentType,
    string? DocumentNumber,
    DateTimeOffset? IssueDate,
    DateTimeOffset? ExpiryDate,
    string? Notes,
    string? AttachmentPath,
    bool HasAttachment,
    string CreatedByUserId,
    DateTimeOffset CreatedAt);
