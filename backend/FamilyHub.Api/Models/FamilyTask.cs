using FamilyHub.Api.Models.Enums;
using TaskStatus = FamilyHub.Api.Models.Enums.TaskStatus;

namespace FamilyHub.Api.Models;

public class FamilyTask
{
    public Guid Id { get; set; }

    public Guid FamilyId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Optional assignee — a <see cref="FamilyMember"/> in the same family.</summary>
    public Guid? AssignedToMemberId { get; set; }

    public DateTimeOffset? DueDate { get; set; }

    public TaskPriority Priority { get; set; }

    public TaskStatus Status { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? CompletedAt { get; set; }

    // Navigation properties
    public Family? Family { get; set; }

    public FamilyMember? AssignedToMember { get; set; }
}
