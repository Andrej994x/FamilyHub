using FamilyHub.Api.Interfaces;
using FamilyHub.Api.Models;

namespace FamilyHub.Api.Services;

/// <summary>
/// Development push sender. Instead of delivering an encrypted push it logs the payload, so the
/// notification → push flow can be exercised without VAPID keys or a real push service.
/// </summary>
public class LoggingPushSender : IPushSender
{
    private readonly ILogger<LoggingPushSender> _logger;

    public LoggingPushSender(ILogger<LoggingPushSender> logger)
    {
        _logger = logger;
    }

    public Task<PushSendOutcome> SendAsync(
        PushSubscription subscription,
        string payloadJson,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "PUSH (dev, not delivered)\n  Endpoint: {Endpoint}\n  Payload: {Payload}",
            subscription.Endpoint,
            payloadJson);

        return Task.FromResult(PushSendOutcome.Sent);
    }
}
