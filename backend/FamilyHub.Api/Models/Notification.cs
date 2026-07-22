using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.Models;

public class Notification
{
    public Guid Id { get; set; }

    /// <summary>Recipient — the <see cref="ApplicationUser"/> this notification is for.</summary>
    public string UserId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public NotificationType Type { get; set; }

    /// <summary>Optional id of the entity the notification refers to (task, pickup, family, …).</summary>
    public Guid? RelatedEntityId { get; set; }

    public bool IsRead { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
