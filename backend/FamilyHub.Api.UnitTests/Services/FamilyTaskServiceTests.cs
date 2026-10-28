using FamilyHub.Api.Common;
using FamilyHub.Api.Data;
using FamilyHub.Api.DTOs.Tasks;
using FamilyHub.Api.Models.Enums;
using FamilyHub.Api.Services;
using FamilyHub.Api.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TaskStatus = FamilyHub.Api.Models.Enums.TaskStatus;

namespace FamilyHub.Api.UnitTests.Services;

public class FamilyTaskServiceTests
{
    // The real NotificationService is used so the "never notify the actor" rule is exercised;
    // only the push transport is faked.
    private static FamilyTaskService CreateService(AppDbContext db) =>
        new(db, new NotificationService(db, new RecordingPushNotificationService(), NullLogger<NotificationService>.Instance));

    private static CreateTaskRequest NewTask(string title = "Take out the bins", Guid? assignee = null, DateTimeOffset? due = null,
        TaskPriority priority = TaskPriority.Medium) => new(title, null, assignee, due, priority);

    [Fact]
    public async Task An_unassigned_task_notifies_every_member_except_the_creator()
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(
            db, ("ana", FamilyRole.Owner), ("bob", FamilyRole.Parent), ("cara", FamilyRole.Member));

        var result = await CreateService(db).CreateTaskAsync("ana", family.Id, NewTask());

        Assert.True(result.Succeeded);
        var recipients = await db.Notifications.Select(n => n.UserId).OrderBy(id => id).ToListAsync();
        Assert.Equal(new[] { "bob", "cara" }, recipients);
    }

    [Fact]
    public async Task An_assigned_task_notifies_only_the_assignee()
    {
        await using var db = TestDb.Create();
        var (family, members) = await TestDb.AddFamilyAsync(
            db, ("ana", FamilyRole.Owner), ("bob", FamilyRole.Parent), ("cara", FamilyRole.Member));

        await CreateService(db).CreateTaskAsync("ana", family.Id, NewTask(assignee: members[2].Id));

        var notification = await db.Notifications.SingleAsync();
        Assert.Equal("cara", notification.UserId);
        Assert.Equal(NotificationType.Task, notification.Type);
    }

    [Fact]
    public async Task Assigning_a_task_to_yourself_creates_no_notification()
    {
        await using var db = TestDb.Create();
        var (family, members) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner), ("bob", FamilyRole.Member));

        await CreateService(db).CreateTaskAsync("ana", family.Id, NewTask(assignee: members[0].Id));

        Assert.Empty(await db.Notifications.ToListAsync());
    }

    [Fact]
    public async Task A_task_cannot_be_assigned_to_a_member_of_another_family()
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner));
        var (_, otherMembers) = await TestDb.AddFamilyAsync(db, ("zed", FamilyRole.Owner));

        var result = await CreateService(db).CreateTaskAsync("ana", family.Id, NewTask(assignee: otherMembers[0].Id));

        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Empty(await db.FamilyTasks.ToListAsync());
    }

    [Fact]
    public async Task A_non_member_cannot_create_or_read_tasks()
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner));
        var service = CreateService(db);

        Assert.Equal(ErrorType.Forbidden, (await service.CreateTaskAsync("stranger", family.Id, NewTask())).ErrorType);
        Assert.Equal(ErrorType.Forbidden, (await service.GetTasksAsync("stranger", family.Id, new TaskFilter())).ErrorType);
        Assert.Equal(ErrorType.NotFound, (await service.GetTasksAsync("stranger", Guid.NewGuid(), new TaskFilter())).ErrorType);
    }

    [Fact]
    public async Task Completing_a_task_stamps_completed_at_and_reopening_clears_it()
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner));
        var service = CreateService(db);
        var task = (await service.CreateTaskAsync("ana", family.Id, NewTask())).Value!;

        var completed = await service.UpdateTaskStatusAsync("ana", family.Id, task.Id, new UpdateTaskStatusRequest(TaskStatus.Completed));
        var reopened = await service.UpdateTaskStatusAsync("ana", family.Id, task.Id, new UpdateTaskStatusRequest(TaskStatus.InProgress));

        Assert.Equal(TaskStatus.Pending, task.Status);
        Assert.NotNull(completed.Value!.CompletedAt);
        Assert.Null(reopened.Value!.CompletedAt);
        Assert.Equal(TaskStatus.InProgress, reopened.Value.Status);
    }

    [Fact]
    public async Task Tasks_are_listed_dated_first_then_by_due_date_then_by_priority()
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner));
        var service = CreateService(db);
        var soon = DateTimeOffset.UtcNow.AddDays(1);
        var later = DateTimeOffset.UtcNow.AddDays(5);

        await service.CreateTaskAsync("ana", family.Id, NewTask("undated-high", priority: TaskPriority.High));
        await service.CreateTaskAsync("ana", family.Id, NewTask("later-low", due: later, priority: TaskPriority.Low));
        await service.CreateTaskAsync("ana", family.Id, NewTask("soon-low", due: soon, priority: TaskPriority.Low));
        await service.CreateTaskAsync("ana", family.Id, NewTask("soon-high", due: soon, priority: TaskPriority.High));

        var titles = (await service.GetTasksAsync("ana", family.Id, new TaskFilter())).Value!.Select(t => t.Title);

        Assert.Equal(new[] { "soon-high", "soon-low", "later-low", "undated-high" }, titles);
    }

    [Fact]
    public async Task Filters_narrow_the_list()
    {
        await using var db = TestDb.Create();
        var (family, members) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner), ("bob", FamilyRole.Member));
        var service = CreateService(db);
        await service.CreateTaskAsync("ana", family.Id, NewTask("for-bob", assignee: members[1].Id, priority: TaskPriority.High));
        await service.CreateTaskAsync("ana", family.Id, NewTask("for-everyone", priority: TaskPriority.Low));

        var bobs = await service.GetTasksAsync("ana", family.Id, new TaskFilter { AssignedMemberId = members[1].Id });
        var low = await service.GetTasksAsync("ana", family.Id, new TaskFilter { Priority = TaskPriority.Low });

        Assert.Equal("for-bob", Assert.Single(bobs.Value!).Title);
        Assert.Equal("for-everyone", Assert.Single(low.Value!).Title);
    }

    [Fact]
    public async Task A_task_from_another_family_is_not_found_through_your_own_family()
    {
        await using var db = TestDb.Create();
        var (familyA, _) = await TestDb.AddFamilyAsync(db, ("ana", FamilyRole.Owner));
        var (familyB, _) = await TestDb.AddFamilyAsync(db, ("zed", FamilyRole.Owner));
        var service = CreateService(db);
        var taskOfB = (await service.CreateTaskAsync("zed", familyB.Id, NewTask())).Value!;

        Assert.Equal(ErrorType.NotFound, (await service.GetTaskAsync("ana", familyA.Id, taskOfB.Id)).ErrorType);
        Assert.Equal(ErrorType.NotFound, (await service.DeleteTaskAsync("ana", familyA.Id, taskOfB.Id)).ErrorType);
        Assert.Single(await db.FamilyTasks.ToListAsync());
    }
}
