using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.Models;

/// <summary>
/// A stored family document (ID card, passport, …) belonging to exactly one subject —
/// either a <see cref="FamilyMember"/> or a <see cref="ChildProfile"/> in the same family.
/// </summary>
public class FamilyDocument
{
    public Guid Id { get; set; }

    public Guid FamilyId { get; set; }

    /// <summary>The member this document belongs to, when the subject is an adult member.</summary>
    public Guid? FamilyMemberId { get; set; }

    /// <summary>The child this document belongs to, when the subject is a child.</summary>
    public Guid? ChildProfileId { get; set; }

    public DocumentType DocumentType { get; set; }

    public string? DocumentNumber { get; set; }

    public DateTimeOffset? IssueDate { get; set; }

    public DateTimeOffset? ExpiryDate { get; set; }

    public string? Notes { get; set; }

    /// <summary>Relative path of the single optional attachment, e.g. "uploads/family-vault/{id}.pdf".</summary>
    public string? AttachmentPath { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public Family? Family { get; set; }

    public FamilyMember? FamilyMember { get; set; }

    public ChildProfile? ChildProfile { get; set; }
}
