using FamilyHub.Api.Models;

namespace FamilyHub.Api.Interfaces;

/// <summary>The result of attempting to deliver a push to a single subscription.</summary>
public enum PushSendOutcome
{
    /// <summary>Accepted by the push service.</summary>
    Sent,

    /// <summary>The endpoint is gone (404/410); the subscription should be deactivated.</summary>
    Expired,

    /// <summary>A transient/other failure; the subscription is left untouched.</summary>
    Failed,
}

/// <summary>
/// Low-level push transport. The real implementation delivers an encrypted Web Push via VAPID;
/// the development implementation logs the payload. Never throws — failures are reported via
/// <see cref="PushSendOutcome"/> so callers can prune dead subscriptions safely.
/// </summary>
public interface IPushSender
{
    Task<PushSendOutcome> SendAsync(
        PushSubscription subscription,
        string payloadJson,
        CancellationToken cancellationToken = default);
}
