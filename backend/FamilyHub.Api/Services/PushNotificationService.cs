using System.Text.Json;
using FamilyHub.Api.Common;
using FamilyHub.Api.Data;
using FamilyHub.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FamilyHub.Api.Services;

public class PushNotificationService : IPushNotificationService
{
    private static readonly JsonSerializerOptions PayloadJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext _db;
    private readonly IPushSender _sender;
    private readonly ILogger<PushNotificationService> _logger;

    public PushNotificationService(
        AppDbContext db, IPushSender sender, ILogger<PushNotificationService> logger)
    {
        _db = db;
        _sender = sender;
        _logger = logger;
    }

    public async Task<int> SendToUserAsync(
        string userId, PushPayload payload, CancellationToken cancellationToken = default)
    {
        var subscriptions = await _db.PushSubscriptions
            .Where(s => s.UserId == userId && s.IsActive)
            .ToListAsync(cancellationToken);

        if (subscriptions.Count == 0)
        {
            return 0;
        }

        var json = JsonSerializer.Serialize(payload, PayloadJsonOptions);
        var now = DateTimeOffset.UtcNow;
        var delivered = 0;
        var changed = false;

        foreach (var subscription in subscriptions)
        {
            PushSendOutcome outcome;
            try
            {
                outcome = await _sender.SendAsync(subscription, json, cancellationToken);
            }
            catch (Exception ex)
            {
                // The sender is expected not to throw, but never let one bad device abort the rest.
                _logger.LogWarning(ex, "Unexpected error sending push to endpoint {Endpoint}.", subscription.Endpoint);
                outcome = PushSendOutcome.Failed;
            }

            switch (outcome)
            {
                case PushSendOutcome.Sent:
                    subscription.LastUsedAt = now;
                    changed = true;
                    delivered++;
                    break;

                case PushSendOutcome.Expired:
                    // Dead endpoint — deactivate so it is never targeted again.
                    subscription.IsActive = false;
                    subscription.UpdatedAt = now;
                    changed = true;
                    break;

                case PushSendOutcome.Failed:
                    // Transient — leave the subscription as-is for the next attempt.
                    break;
            }
        }

        if (changed)
        {
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to persist push subscription state for user {UserId}.", userId);
            }
        }

        return delivered;
    }
}
