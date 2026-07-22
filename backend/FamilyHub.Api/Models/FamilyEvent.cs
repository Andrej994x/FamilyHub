using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.Models;

public class FamilyEvent
{
    public Guid Id { get; set; }

    public Guid FamilyId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public EventType EventType { get; set; }

    public DateTimeOffset StartDateTime { get; set; }

    public DateTimeOffset? EndDateTime { get; set; }

    public string? Location { get; set; }

    /// <summary>Optional related member — a <see cref="FamilyMember"/> in the same family.</summary>
    public Guid? AssignedMemberId { get; set; }

    /// <summary>Optional related child — a <see cref="ChildProfile"/> in the same family.</summary>
    public Guid? ChildProfileId { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public Family? Family { get; set; }

    public FamilyMember? AssignedMember { get; set; }

    public ChildProfile? ChildProfile { get; set; }
}
