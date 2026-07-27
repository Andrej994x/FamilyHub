using FamilyHub.Api.Common;

namespace FamilyHub.Api.Interfaces;

/// <summary>
/// Delivers a push payload to a user's active devices. Used by the notification service when an
/// in-app notification is created. Implementations never throw and prune dead subscriptions.
/// </summary>
public interface IPushNotificationService
{
    /// <summary>
    /// Sends <paramref name="payload"/> to every active device of <paramref name="userId"/>.
    /// Returns the number of devices the push was accepted by (0 when the user has none).
    /// </summary>
    Task<int> SendToUserAsync(string userId, PushPayload payload, CancellationToken cancellationToken = default);
}
