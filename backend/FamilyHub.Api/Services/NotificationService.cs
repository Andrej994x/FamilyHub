using FamilyHub.Api.Common;
using FamilyHub.Api.Data;
using FamilyHub.Api.DTOs.Notifications;
using FamilyHub.Api.Interfaces;
using FamilyHub.Api.Models;
using FamilyHub.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyHub.Api.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    private readonly IPushNotificationService _push;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        AppDbContext db, IPushNotificationService push, ILogger<NotificationService> logger)
    {
        _db = db;
        _push = push;
        _logger = logger;
    }

    // ---- Query / read-state ----

    public async Task<IReadOnlyList<NotificationResponse>> GetForUserAsync(string userId)
    {
        return await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationResponse(
                n.Id, n.FamilyId, n.Type, n.Title, n.Message, n.RelatedUrl, n.IsRead, n.CreatedAt))
            .ToListAsync();
    }

    public Task<int> GetUnreadCountAsync(string userId) =>
        _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

    public async Task<Result> MarkAsReadAsync(string userId, Guid notificationId)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
        if (notification is null)
        {
            return Result.Failure(ErrorType.NotFound, "Notification not found.");
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await _db.SaveChangesAsync();
        }

        return Result.Success();
    }

    public Task MarkAllAsReadAsync(string userId) =>
        _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

    public async Task<Result> DeleteAsync(string userId, Guid notificationId)
    {
        var deleted = await _db.Notifications
            .Where(n => n.Id == notificationId && n.UserId == userId)
            .ExecuteDeleteAsync();

        return deleted == 0
            ? Result.Failure(ErrorType.NotFound, "Notification not found.")
            : Result.Success();
    }

    public Task DeleteAllReadAsync(string userId) =>
        _db.Notifications
            .Where(n => n.UserId == userId && n.IsRead)
            .ExecuteDeleteAsync();

    // ---- Creation primitives ----

    public Task CreateForUserAsync(
        string userId,
        NotificationType type,
        string title,
        string message,
        Guid? familyId = null,
        string? relatedUrl = null,
        string? actorUserId = null) =>
        PersistAsync(new[] { userId }, type, title, message, familyId, relatedUrl, actorUserId);

    public Task CreateForUsersAsync(
        IEnumerable<string> userIds,
        NotificationType type,
        string title,
        string message,
        Guid? familyId = null,
        string? relatedUrl = null,
        string? actorUserId = null) =>
        PersistAsync(userIds, type, title, message, familyId, relatedUrl, actorUserId);

    public async Task CreateForFamilyAsync(
        Guid familyId,
        NotificationType type,
        string title,
        string message,
        string? relatedUrl = null,
        string? actorUserId = null,
        IReadOnlyCollection<FamilyRole>? roles = null)
    {
        var query = _db.FamilyMembers.Where(m => m.FamilyId == familyId);
        if (roles is { Count: > 0 })
        {
            query = query.Where(m => roles.Contains(m.Role));
        }

        var recipientIds = await query.Select(m => m.UserId).ToListAsync();

        await PersistAsync(recipientIds, type, title, message, familyId, relatedUrl, actorUserId);
    }

    /// <summary>
    /// Persists one notification per distinct recipient in a single save, skipping
    /// empty ids and the actor. Central choke point for the "never notify the actor" rule.
    /// </summary>
    private async Task PersistAsync(
        IEnumerable<string> userIds,
        NotificationType type,
        string title,
        string message,
        Guid? familyId,
        string? relatedUrl,
        string? actorUserId)
    {
        var recipients = userIds
            .Where(id => !string.IsNullOrEmpty(id) && id != actorUserId)
            .Distinct()
            .ToList();

        if (recipients.Count == 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var created = new List<Notification>(recipients.Count);
        foreach (var recipient in recipients)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = recipient,
                FamilyId = familyId,
                Type = type,
                Title = title,
                Message = message,
                RelatedUrl = relatedUrl,
                IsRead = false,
                CreatedAt = now,
                IsPushSent = false,
            };
            _db.Notifications.Add(notification);
            created.Add(notification);
        }

        // Persist the in-app notifications first; these are the source of truth and must not be
        // lost if a subsequent push send fails.
        await _db.SaveChangesAsync();

        await DispatchPushAsync(created);
    }

    /// <summary>
    /// Best-effort push fan-out for freshly created notifications. Delivery runs inline and is
    /// deliberately non-fatal: a failure here never affects the saved in-app notifications.
    /// (A production system would offload this to a background queue.)
    /// </summary>
    private async Task DispatchPushAsync(List<Notification> notifications)
    {
        var anyPushed = false;

        foreach (var notification in notifications)
        {
            try
            {
                var payload = new PushPayload(
                    notification.Title,
                    notification.Message,
                    notification.RelatedUrl,
                    notification.Id.ToString());

                var delivered = await _push.SendToUserAsync(notification.UserId, payload);
                if (delivered > 0)
                {
                    notification.IsPushSent = true;
                    notification.PushSentAt = DateTimeOffset.UtcNow;
                    anyPushed = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Push dispatch failed for notification {NotificationId}.", notification.Id);
            }
        }

        if (anyPushed)
        {
            await _db.SaveChangesAsync();
        }
    }
}
