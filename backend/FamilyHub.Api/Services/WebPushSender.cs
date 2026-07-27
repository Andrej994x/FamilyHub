using System.Net;
using FamilyHub.Api.Common;
using FamilyHub.Api.Interfaces;
using Microsoft.Extensions.Options;
using WebPush;
using DeviceSubscription = FamilyHub.Api.Models.PushSubscription;
using WebPushSubscription = WebPush.PushSubscription;

namespace FamilyHub.Api.Services;

/// <summary>
/// Real Web Push transport. Encrypts and signs the payload with VAPID and posts it to the
/// device's push service. Dead endpoints (404/410) are reported as <see cref="PushSendOutcome.Expired"/>.
/// </summary>
public class WebPushSender : IPushSender
{
    private readonly WebPushClient _client;
    private readonly VapidDetails _vapid;
    private readonly ILogger<WebPushSender> _logger;

    public WebPushSender(WebPushClient client, IOptions<PushSettings> settings, ILogger<WebPushSender> logger)
    {
        _client = client;
        _logger = logger;

        var s = settings.Value;
        _vapid = new VapidDetails(s.Subject, s.PublicKey, s.PrivateKey);
    }

    public async Task<PushSendOutcome> SendAsync(
        DeviceSubscription subscription,
        string payloadJson,
        CancellationToken cancellationToken = default)
    {
        var target = new WebPushSubscription(subscription.Endpoint, subscription.P256dh, subscription.Auth);

        try
        {
            await _client.SendNotificationAsync(target, payloadJson, _vapid, cancellationToken);
            return PushSendOutcome.Sent;
        }
        catch (WebPushException ex)
            when (ex.StatusCode == HttpStatusCode.NotFound || ex.StatusCode == HttpStatusCode.Gone)
        {
            // The device unsubscribed or the endpoint expired — the caller should prune it.
            return PushSendOutcome.Expired;
        }
        catch (WebPushException ex)
        {
            _logger.LogWarning(
                ex, "Push delivery failed ({Status}) for endpoint {Endpoint}.", ex.StatusCode, subscription.Endpoint);
            return PushSendOutcome.Failed;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Push delivery errored for endpoint {Endpoint}.", subscription.Endpoint);
            return PushSendOutcome.Failed;
        }
    }
}
