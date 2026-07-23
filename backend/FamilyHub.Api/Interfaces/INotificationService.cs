using FamilyHub.Api.Common;
using FamilyHub.Api.DTOs.Notifications;
using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.Interfaces;

/// <summary>
/// Central, reusable notification service. Domain services call the creation
/// primitives when a family activity occurs; the Notification Center controller
/// uses the query/state methods. All methods scope reads and mutations to the
/// requesting user so a user can only ever touch their own notifications.
/// </summary>
public interface INotificationService
{
    // ---- Query / read-state (user-facing) ----

    Task<IReadOnlyList<NotificationResponse>> GetForUserAsync(string userId);

    Task<int> GetUnreadCountAsync(string userId);

    Task<Result> MarkAsReadAsync(string userId, Guid notificationId);

    Task MarkAllAsReadAsync(string userId);

    Task<Result> DeleteAsync(string userId, Guid notificationId);

    Task DeleteAllReadAsync(string userId);

    // ---- Creation primitives (called by other services when events occur) ----

    /// <summary>
    /// Creates a notification for a single recipient. No-op when <paramref name="userId"/>
    /// is empty or equals <paramref name="actorUserId"/> (never notify the actor).
    /// </summary>
    Task CreateForUserAsync(
        string userId,
        NotificationType type,
        string title,
        string message,
        Guid? familyId = null,
        string? relatedUrl = null,
        string? actorUserId = null);

    /// <summary>
    /// Creates a notification for each distinct recipient, skipping the actor.
    /// </summary>
    Task CreateForUsersAsync(
        IEnumerable<string> userIds,
        NotificationType type,
        string title,
        string message,
        Guid? familyId = null,
        string? relatedUrl = null,
        string? actorUserId = null);

    /// <summary>
    /// Fans a notification out to a family's members, optionally restricted to the
    /// given <paramref name="roles"/>. The actor is always excluded.
    /// </summary>
    Task CreateForFamilyAsync(
        Guid familyId,
        NotificationType type,
        string title,
        string message,
        string? relatedUrl = null,
        string? actorUserId = null,
        IReadOnlyCollection<FamilyRole>? roles = null);
}
