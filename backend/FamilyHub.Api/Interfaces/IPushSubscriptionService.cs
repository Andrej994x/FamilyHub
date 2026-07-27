using FamilyHub.Api.Common;
using FamilyHub.Api.DTOs.Push;

namespace FamilyHub.Api.Interfaces;

/// <summary>
/// Manages the current user's push subscriptions (devices). All operations are scoped to the
/// requesting user, so a user can only ever see or change their own devices.
/// </summary>
public interface IPushSubscriptionService
{
    Task<IReadOnlyList<PushSubscriptionResponse>> GetForUserAsync(string userId);

    /// <summary>Registers a device, upserting on the endpoint (re-registration is idempotent).</summary>
    Task<Result<PushSubscriptionResponse>> RegisterAsync(string userId, CreatePushSubscriptionRequest request);

    Task<Result<PushSubscriptionResponse>> UpdateAsync(string userId, Guid id, UpdatePushSubscriptionRequest request);

    Task<Result> RemoveAsync(string userId, Guid id);
}
