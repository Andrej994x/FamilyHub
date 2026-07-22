using FamilyHub.Api.Common;
using FamilyHub.Api.DTOs.Tasks;

namespace FamilyHub.Api.Interfaces;

public interface IFamilyTaskService
{
    Task<Result<TaskResponse>> CreateTaskAsync(string userId, Guid familyId, CreateTaskRequest request);

    Task<Result<IReadOnlyList<TaskResponse>>> GetTasksAsync(string userId, Guid familyId, TaskFilter filter);

    Task<Result<TaskResponse>> GetTaskAsync(string userId, Guid familyId, Guid taskId);

    Task<Result<TaskResponse>> UpdateTaskAsync(string userId, Guid familyId, Guid taskId, UpdateTaskRequest request);

    Task<Result<TaskResponse>> UpdateTaskStatusAsync(string userId, Guid familyId, Guid taskId, UpdateTaskStatusRequest request);

    Task<Result> DeleteTaskAsync(string userId, Guid familyId, Guid taskId);
}
