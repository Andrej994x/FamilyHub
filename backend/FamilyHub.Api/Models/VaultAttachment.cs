using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.Models;

/// <summary>
/// A file attached to a Family Vault record. The owner is referenced polymorphically via
/// (<see cref="OwnerType"/>, <see cref="OwnerId"/>) so a single table serves vehicles, pets,
/// home records, warranties and other records. Scoped to a family for access control and so
/// deleting a family cascades its attachments in a single path.
/// </summary>
public class VaultAttachment
{
    public Guid Id { get; set; }

    public Guid FamilyId { get; set; }

    public VaultRecordType OwnerType { get; set; }

    public Guid OwnerId { get; set; }

    /// <summary>Relative storage path, e.g. "uploads/family-vault/{id}.pdf".</summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>Original upload file name, for a friendlier download.</summary>
    public string? FileName { get; set; }

    public string ContentType { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public Family? Family { get; set; }
}
