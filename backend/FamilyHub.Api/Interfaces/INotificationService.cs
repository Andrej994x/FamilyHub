using FamilyHub.Api.Common;
using FamilyHub.Api.DTOs.Notifications;
using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.Interfaces;

public interface INotificationService
{
    // ---- Query / read-state (user-facing) ----

    Task<IReadOnlyList<NotificationResponse>> GetForUserAsync(string userId);

    Task<int> GetUnreadCountAsync(string userId);

    Task<Result> MarkAsReadAsync(string userId, Guid notificationId);

    Task MarkAllAsReadAsync(string userId);

    // ---- Creation (called by other services when events occur) ----

    Task CreateAsync(string userId, string title, string message, NotificationType type, Guid? relatedEntityId = null);
}
