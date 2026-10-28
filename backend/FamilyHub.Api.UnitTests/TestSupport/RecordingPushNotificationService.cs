using FamilyHub.Api.Common;
using FamilyHub.Api.Interfaces;

namespace FamilyHub.Api.UnitTests.TestSupport;

/// <summary>Fake push fan-out that records every send instead of contacting a push service.</summary>
internal sealed class RecordingPushNotificationService : IPushNotificationService
{
    public List<(string UserId, PushPayload Payload)> Sent { get; } = new();

    /// <summary>The number of devices each send reports as reached.</summary>
    public int DevicesPerUser { get; set; }

    public Task<int> SendToUserAsync(string userId, PushPayload payload, CancellationToken cancellationToken = default)
    {
        Sent.Add((userId, payload));
        return Task.FromResult(DevicesPerUser);
    }
}
