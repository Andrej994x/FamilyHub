using FamilyHub.Api.Common;
using FamilyHub.Api.Models.Enums;
using FamilyHub.Api.Services;
using FamilyHub.Api.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FamilyHub.Api.UnitTests.Services;

public class NotificationServiceTests
{
    [Fact]
    public async Task Persists_one_notification_per_distinct_recipient_and_skips_the_actor()
    {
        await using var db = TestDb.Create();
        var push = new RecordingPushNotificationService();
        var service = new NotificationService(db, push, NullLogger<NotificationService>.Instance);

        await service.CreateForUsersAsync(
            new[] { "bob", "bob", "", "ana", "cara" }, NotificationType.Shopping, "Title", "Message",
            actorUserId: "ana");

        var recipients = await db.Notifications.Select(n => n.UserId).OrderBy(id => id).ToListAsync();
        Assert.Equal(new[] { "bob", "cara" }, recipients);
        Assert.Equal(new[] { "bob", "cara" }, push.Sent.Select(s => s.UserId).OrderBy(id => id));
    }

    [Fact]
    public async Task Family_notifications_can_be_limited_to_roles()
    {
        await using var db = TestDb.Create();
        var (family, _) = await TestDb.AddFamilyAsync(db,
            ("ana", FamilyRole.Owner), ("bob", FamilyRole.Parent), ("cara", FamilyRole.Member), ("dan", FamilyRole.Child));
        var service = new NotificationService(db, new RecordingPushNotificationService(), NullLogger<NotificationService>.Instance);

        await service.CreateForFamilyAsync(family.Id, NotificationType.Family, "Joined", "Someone joined",
            actorUserId: "cara", roles: new[] { FamilyRole.Owner, FamilyRole.Parent });

        var recipients = await db.Notifications.Select(n => n.UserId).OrderBy(id => id).ToListAsync();
        Assert.Equal(new[] { "ana", "bob" }, recipients);
    }

    [Fact]
    public async Task Marks_a_notification_as_pushed_only_when_a_device_accepted_it()
    {
        await using var db = TestDb.Create();
        var delivering = new NotificationService(db, new RecordingPushNotificationService { DevicesPerUser = 1 }, NullLogger<NotificationService>.Instance);
        var noDevices = new NotificationService(db, new RecordingPushNotificationService { DevicesPerUser = 0 }, NullLogger<NotificationService>.Instance);

        await delivering.CreateForUserAsync("bob", NotificationType.Task, "Pushed", "Body");
        await noDevices.CreateForUserAsync("cara", NotificationType.Task, "Not pushed", "Body");

        var bob = await db.Notifications.SingleAsync(n => n.UserId == "bob");
        var cara = await db.Notifications.SingleAsync(n => n.UserId == "cara");
        Assert.True(bob.IsPushSent);
        Assert.NotNull(bob.PushSentAt);
        Assert.False(cara.IsPushSent);
    }

    [Fact]
    public async Task Users_only_see_and_modify_their_own_notifications()
    {
        await using var db = TestDb.Create();
        var service = new NotificationService(db, new RecordingPushNotificationService(), NullLogger<NotificationService>.Instance);
        await service.CreateForUserAsync("bob", NotificationType.Task, "For bob", "Body");
        var bobsNotification = await db.Notifications.SingleAsync();

        var asCara = await service.MarkAsReadAsync("cara", bobsNotification.Id);
        var asBob = await service.MarkAsReadAsync("bob", bobsNotification.Id);

        Assert.Equal(ErrorType.NotFound, asCara.ErrorType);
        Assert.True(asBob.Succeeded);
        Assert.Empty(await service.GetForUserAsync("cara"));
        Assert.Equal(0, await service.GetUnreadCountAsync("bob"));
    }
}
