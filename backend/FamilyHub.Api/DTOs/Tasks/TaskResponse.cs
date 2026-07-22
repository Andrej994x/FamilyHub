using FamilyHub.Api.Models.Enums;
using TaskStatus = FamilyHub.Api.Models.Enums.TaskStatus;

namespace FamilyHub.Api.DTOs.Tasks;

public record TaskResponse(
    Guid Id,
    Guid FamilyId,
    string Title,
    string? Description,
    Guid? AssignedToMemberId,
    DateTimeOffset? DueDate,
    TaskPriority Priority,
    TaskStatus Status,
    string CreatedByUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);
