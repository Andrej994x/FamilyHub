using System.Net;
using System.Net.Http.Json;
using FamilyHub.Api.DTOs.Notifications;
using FamilyHub.Api.DTOs.Tasks;
using FamilyHub.Api.IntegrationTests.Infrastructure;
using FamilyHub.Api.Models.Enums;
using TaskStatus = FamilyHub.Api.Models.Enums.TaskStatus;

namespace FamilyHub.Api.IntegrationTests.Workflows;

[Collection(ApiCollection.Name)]
public class FamilyTaskWorkflowTests(FamilyHubApiFixture fixture)
{
    [Fact]
    public async Task A_family_can_join_create_complete_and_delete_a_task_with_notifications()
    {
        // Join: the owner invites a member, who accepts; the owner is told about the new member.
        var family = await fixture.CreateFamilyWithOwnerAsync();
        var member = await fixture.AddMemberAsync(family, FamilyRole.Member);

        var ownerNotifications = await family.Owner.Client.GetFromJsonAsync<List<NotificationResponse>>("/api/notifications");
        Assert.Contains(ownerNotifications!, n => n.Type == NotificationType.Family && n.FamilyId == family.Id);

        // Create: an unassigned task notifies everyone except its creator.
        var created = await family.Owner.Client.PostAsJsonAsync($"/api/families/{family.Id}/tasks",
            new CreateTaskRequest("  Plan the weekend  ", "Ideas welcome", null, DateTimeOffset.UtcNow.AddDays(2), TaskPriority.High));
        await created.EnsureStatusAsync(HttpStatusCode.Created);
        var task = (await created.Content.ReadFromJsonAsync<TaskResponse>())!;
        Assert.Equal($"/api/families/{family.Id}/tasks/{task.Id}", created.Headers.Location!.AbsolutePath,
            ignoreCase: true);
        Assert.Equal("Plan the weekend", task.Title);
        Assert.Equal(TaskStatus.Pending, task.Status);
        Assert.Equal(family.Owner.UserId, task.CreatedByUserId);

        var memberNotifications = await member.Client.GetFromJsonAsync<List<NotificationResponse>>("/api/notifications");
        Assert.Contains(memberNotifications!, n => n.Type == NotificationType.Task && n.Message.Contains("Plan the weekend"));
        ownerNotifications = await family.Owner.Client.GetFromJsonAsync<List<NotificationResponse>>("/api/notifications");
        Assert.DoesNotContain(ownerNotifications!, n => n.Type == NotificationType.Task);

        // Complete: the member marks it done.
        var completed = await member.Client.PatchAsJsonAsync($"/api/families/{family.Id}/tasks/{task.Id}/status",
            new UpdateTaskStatusRequest(TaskStatus.Completed));
        await completed.EnsureStatusAsync(HttpStatusCode.OK);
        Assert.NotNull((await completed.Content.ReadFromJsonAsync<TaskResponse>())!.CompletedAt);

        var done = await family.Owner.Client.GetFromJsonAsync<List<TaskResponse>>(
            $"/api/families/{family.Id}/tasks?status={(int)TaskStatus.Completed}");
        Assert.Equal(task.Id, Assert.Single(done!).Id);

        // Delete.
        var deleted = await family.Owner.Client.DeleteAsync($"/api/families/{family.Id}/tasks/{task.Id}");
        await deleted.EnsureStatusAsync(HttpStatusCode.NoContent);
        var gone = await member.Client.GetAsync($"/api/families/{family.Id}/tasks/{task.Id}");
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }
}
