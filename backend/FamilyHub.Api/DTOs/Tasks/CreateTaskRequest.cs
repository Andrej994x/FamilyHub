using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.DTOs.Tasks;

public record CreateTaskRequest(
    string Title,
    string? Description,
    Guid? AssignedToMemberId,
    DateTimeOffset? DueDate,
    TaskPriority Priority);
