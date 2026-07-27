namespace FamilyHub.Api.DTOs.Push;

/// <summary>Registers a browser push subscription for the current user.</summary>
public record CreatePushSubscriptionRequest(
    string Endpoint,
    string P256dh,
    string Auth,
    string? UserAgent);

/// <summary>Updates an existing subscription's keys (e.g. after the browser rotates them).</summary>
public record UpdatePushSubscriptionRequest(
    string P256dh,
    string Auth,
    string? UserAgent);

public record PushSubscriptionResponse(
    Guid Id,
    string Endpoint,
    string? UserAgent,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LastUsedAt);

/// <summary>The VAPID public key the client needs to create a subscription.</summary>
public record VapidPublicKeyResponse(string PublicKey);
