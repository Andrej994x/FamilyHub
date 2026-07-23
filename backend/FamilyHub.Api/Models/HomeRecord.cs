namespace FamilyHub.Api.Models;

public class HomeRecord
{
    public Guid Id { get; set; }

    public Guid FamilyId { get; set; }

    public string Title { get; set; } = string.Empty;

    /// <summary>Free-form kind, e.g. "Electricity", "Internet", "Mortgage".</summary>
    public string? Type { get; set; }

    public string? Provider { get; set; }

    public DateTimeOffset? IssueDate { get; set; }

    /// <summary>Expiry or renewal date, whichever applies to the record.</summary>
    public DateTimeOffset? RenewalDate { get; set; }

    public string? Notes { get; set; }

    /// <summary>When true, creating the record notifies Owner/Parent members (and the related person).</summary>
    public bool IsImportant { get; set; }

    /// <summary>Optional <see cref="FamilyMember"/> this record concerns (the "related person").</summary>
    public Guid? RelatedMemberId { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public Family? Family { get; set; }
}
