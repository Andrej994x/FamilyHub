using FamilyHub.Api.Common;
using FamilyHub.Api.Data;
using FamilyHub.Api.DTOs.Tasks;
using FamilyHub.Api.Interfaces;
using FamilyHub.Api.Models;
using FamilyHub.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;
using TaskStatus = FamilyHub.Api.Models.Enums.TaskStatus;

namespace FamilyHub.Api.Services;

public class FamilyTaskService : IFamilyTaskService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notifications;

    public FamilyTaskService(AppDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task<Result<TaskResponse>> CreateTaskAsync(string userId, Guid familyId, CreateTaskRequest request)
    {
        var access = await AuthorizeAsync(userId, familyId);
        if (access is not null)
        {
            return Result<TaskResponse>.Failure(access.Value.Error, access.Value.Message);
        }

        if (request.AssignedToMemberId is Guid assignee &&
            !await IsMemberOfFamily(assignee, familyId))
        {
            return Result<TaskResponse>.Failure(
                ErrorType.Validation, "The assigned member is not part of this family.");
        }

        var task = new FamilyTask
        {
            Id = Guid.NewGuid(),
            FamilyId = familyId,
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            AssignedToMemberId = request.AssignedToMemberId,
            DueDate = request.DueDate,
            Priority = request.Priority,
            Status = TaskStatus.Pending,
            CreatedByUserId = userId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.FamilyTasks.Add(task);
        await _db.SaveChangesAsync();

        await NotifyAssigneeAsync(userId, task.AssignedToMemberId, task.Id, task.Title);

        return Result<TaskResponse>.Success(ToResponse(task));
    }

    public async Task<Result<IReadOnlyList<TaskResponse>>> GetTasksAsync(string userId, Guid familyId, TaskFilter filter)
    {
        var access = await AuthorizeAsync(userId, familyId);
        if (access is not null)
        {
            return Result<IReadOnlyList<TaskResponse>>.Failure(access.Value.Error, access.Value.Message);
        }

        // Server-side filtering — all predicates are translated to SQL.
        var query = _db.FamilyTasks.Where(t => t.FamilyId == familyId);

        if (filter.Status is not null)
        {
            query = query.Where(t => t.Status == filter.Status);
        }

        if (filter.AssignedMemberId is not null)
        {
            query = query.Where(t => t.AssignedToMemberId == filter.AssignedMemberId);
        }

        if (filter.Priority is not null)
        {
            query = query.Where(t => t.Priority == filter.Priority);
        }

        if (filter.DueFrom is not null)
        {
            query = query.Where(t => t.DueDate >= filter.DueFrom);
        }

        if (filter.DueTo is not null)
        {
            query = query.Where(t => t.DueDate <= filter.DueTo);
        }

        // Filtering runs in SQL; ordering + projection run in memory so the
        // string-backed Priority enum sorts by its semantic value (High first),
        // not alphabetically.
        var entities = await query.ToListAsync();

        var tasks = entities
            .OrderBy(t => t.DueDate.HasValue ? 0 : 1)               // dated tasks first
            .ThenBy(t => t.DueDate ?? DateTimeOffset.MaxValue)
            .ThenByDescending(t => t.Priority)
            .Select(ToResponse)
            .ToList();

        return Result<IReadOnlyList<TaskResponse>>.Success(tasks);
    }

    public async Task<Result<TaskResponse>> GetTaskAsync(string userId, Guid familyId, Guid taskId)
    {
        var access = await AuthorizeAsync(userId, familyId);
        if (access is not null)
        {
            return Result<TaskResponse>.Failure(access.Value.Error, access.Value.Message);
        }

        var task = await _db.FamilyTasks.FirstOrDefaultAsync(t => t.Id == taskId && t.FamilyId == familyId);
        return task is null
            ? Result<TaskResponse>.Failure(ErrorType.NotFound, "Task not found in this family.")
            : Result<TaskResponse>.Success(ToResponse(task));
    }

    public async Task<Result<TaskResponse>> UpdateTaskAsync(
        string userId, Guid familyId, Guid taskId, UpdateTaskRequest request)
    {
        var access = await AuthorizeAsync(userId, familyId);
        if (access is not null)
        {
            return Result<TaskResponse>.Failure(access.Value.Error, access.Value.Message);
        }

        var task = await _db.FamilyTasks.FirstOrDefaultAsync(t => t.Id == taskId && t.FamilyId == familyId);
        if (task is null)
        {
            return Result<TaskResponse>.Failure(ErrorType.NotFound, "Task not found in this family.");
        }

        if (request.AssignedToMemberId is Guid assignee &&
            !await IsMemberOfFamily(assignee, familyId))
        {
            return Result<TaskResponse>.Failure(
                ErrorType.Validation, "The assigned member is not part of this family.");
        }

        var previousAssignee = task.AssignedToMemberId;

        task.Title = request.Title.Trim();
        task.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        task.AssignedToMemberId = request.AssignedToMemberId;
        task.DueDate = request.DueDate;
        task.Priority = request.Priority;

        await _db.SaveChangesAsync();

        // Notify only when the task is (re)assigned to a different member.
        if (task.AssignedToMemberId is not null && task.AssignedToMemberId != previousAssignee)
        {
            await NotifyAssigneeAsync(userId, task.AssignedToMemberId, task.Id, task.Title);
        }

        return Result<TaskResponse>.Success(ToResponse(task));
    }

    public async Task<Result<TaskResponse>> UpdateTaskStatusAsync(
        string userId, Guid familyId, Guid taskId, UpdateTaskStatusRequest request)
    {
        var access = await AuthorizeAsync(userId, familyId);
        if (access is not null)
        {
            return Result<TaskResponse>.Failure(access.Value.Error, access.Value.Message);
        }

        var task = await _db.FamilyTasks.FirstOrDefaultAsync(t => t.Id == taskId && t.FamilyId == familyId);
        if (task is null)
        {
            return Result<TaskResponse>.Failure(ErrorType.NotFound, "Task not found in this family.");
        }

        task.Status = request.Status;
        task.CompletedAt = request.Status == TaskStatus.Completed ? DateTimeOffset.UtcNow : null;

        await _db.SaveChangesAsync();

        return Result<TaskResponse>.Success(ToResponse(task));
    }

    public async Task<Result> DeleteTaskAsync(string userId, Guid familyId, Guid taskId)
    {
        var access = await AuthorizeAsync(userId, familyId);
        if (access is not null)
        {
            return Result.Failure(access.Value.Error, access.Value.Message);
        }

        var task = await _db.FamilyTasks.FirstOrDefaultAsync(t => t.Id == taskId && t.FamilyId == familyId);
        if (task is null)
        {
            return Result.Failure(ErrorType.NotFound, "Task not found in this family.");
        }

        _db.FamilyTasks.Remove(task);
        await _db.SaveChangesAsync();

        return Result.Success();
    }

    /// <summary>
    /// Returns an error tuple when the family is missing or the user is not a member;
    /// null when access is granted (any family member may manage tasks).
    /// </summary>
    private async Task<(ErrorType Error, string Message)?> AuthorizeAsync(string userId, Guid familyId)
    {
        var familyExists = await _db.Families.AnyAsync(f => f.Id == familyId);
        if (!familyExists)
        {
            return (ErrorType.NotFound, "Family not found.");
        }

        var isMember = await _db.FamilyMembers.AnyAsync(m => m.FamilyId == familyId && m.UserId == userId);
        if (!isMember)
        {
            return (ErrorType.Forbidden, "You do not have access to this family.");
        }

        return null;
    }

    private Task<bool> IsMemberOfFamily(Guid memberId, Guid familyId) =>
        _db.FamilyMembers.AnyAsync(m => m.Id == memberId && m.FamilyId == familyId);

    /// <summary>Notifies the assigned member's user, unless they assigned the task to themselves.</summary>
    private async Task NotifyAssigneeAsync(string actorUserId, Guid? assignedMemberId, Guid taskId, string title)
    {
        if (assignedMemberId is not Guid memberId)
        {
            return;
        }

        var assigneeUserId = await _db.FamilyMembers
            .Where(m => m.Id == memberId)
            .Select(m => m.UserId)
            .FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(assigneeUserId) || assigneeUserId == actorUserId)
        {
            return;
        }

        await _notifications.CreateAsync(
            assigneeUserId,
            "New task assigned",
            $"You have been assigned the task \"{title}\".",
            NotificationType.TaskAssigned,
            taskId);
    }

    private static TaskResponse ToResponse(FamilyTask t) =>
        new(t.Id, t.FamilyId, t.Title, t.Description, t.AssignedToMemberId, t.DueDate,
            t.Priority, t.Status, t.CreatedByUserId, t.CreatedAt, t.CompletedAt);
}
