using FamilyHub.Api.Models.Enums;
using TaskStatus = FamilyHub.Api.Models.Enums.TaskStatus;

namespace FamilyHub.Api.DTOs.Tasks;

/// <summary>
/// Optional query-string filters for listing tasks. All filters are applied server-side.
/// </summary>
public class TaskFilter
{
    public TaskStatus? Status { get; set; }

    public Guid? AssignedMemberId { get; set; }

    public DateTimeOffset? DueFrom { get; set; }

    public DateTimeOffset? DueTo { get; set; }

    public TaskPriority? Priority { get; set; }
}
