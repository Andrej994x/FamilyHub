using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.Models;

public class PickupSchedule
{
    public Guid Id { get; set; }

    public Guid FamilyId { get; set; }

    public Guid ChildProfileId { get; set; }

    /// <summary>The <see cref="FamilyMember"/> currently responsible for the pickup.</summary>
    public Guid AssignedMemberId { get; set; }

    public DateTimeOffset PickupDateTime { get; set; }

    public string Location { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public PickupStatus Status { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? CompletedAt { get; set; }

    // Navigation properties
    public Family? Family { get; set; }

    public ChildProfile? ChildProfile { get; set; }

    public FamilyMember? AssignedMember { get; set; }
}
