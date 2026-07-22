using TaskStatus = FamilyHub.Api.Models.Enums.TaskStatus;

namespace FamilyHub.Api.DTOs.Tasks;

public record UpdateTaskStatusRequest(TaskStatus Status);
