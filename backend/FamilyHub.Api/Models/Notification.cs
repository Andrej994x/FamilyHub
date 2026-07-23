using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.Models;

public class Notification
{
    public Guid Id { get; set; }

    /// <summary>Recipient — the <see cref="ApplicationUser"/> this notification is for.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Family context the notification was raised in. Null for family-independent
    /// <see cref="NotificationType.System"/> notifications.
    /// </summary>
    public Guid? FamilyId { get; set; }

    public NotificationType Type { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    /// <summary>Optional client-relative deep link, e.g. "/tasks" or "/calendar".</summary>
    public string? RelatedUrl { get; set; }

    public bool IsRead { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // ---- Push delivery tracking ----
    // These let a future background worker pick up notifications that have not yet
    // been pushed to a device, deliver them, and record when that happened. The
    // in-app Notification Center ignores them.

    /// <summary>True once this notification has been dispatched to a push channel.</summary>
    public bool IsPushSent { get; set; }

    /// <summary>When the push was dispatched, if it has been.</summary>
    public DateTimeOffset? PushSentAt { get; set; }
}
