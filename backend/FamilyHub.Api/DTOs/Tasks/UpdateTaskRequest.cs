using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.DTOs.Tasks;

/// <summary>
/// Full update of a task's content. Status is changed via the dedicated status endpoint.
/// </summary>
public record UpdateTaskRequest(
    string Title,
    string? Description,
    Guid? AssignedToMemberId,
    DateTimeOffset? DueDate,
    TaskPriority Priority);
