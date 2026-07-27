namespace FamilyHub.Api.Models;

/// <summary>
/// A Web Push subscription for one browser/device belonging to an <see cref="ApplicationUser"/>.
/// The three transport fields (<see cref="Endpoint"/>, <see cref="P256dh"/>, <see cref="Auth"/>)
/// come from the browser's PushManager and are everything needed to deliver an encrypted push.
/// </summary>
public class PushSubscription
{
    public Guid Id { get; set; }

    /// <summary>Owner — the <see cref="ApplicationUser"/> this device belongs to.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>The push service URL that identifies this device (globally unique).</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// SHA-256 (hex) of <see cref="Endpoint"/>. Endpoints can be long — too long for a SQL
    /// unique index key — so uniqueness is enforced on this fixed-length hash instead.
    /// </summary>
    public string EndpointHash { get; set; } = string.Empty;

    /// <summary>Client public key (ECDH P-256) used to encrypt the payload.</summary>
    public string P256dh { get; set; } = string.Empty;

    /// <summary>Client auth secret used to encrypt the payload.</summary>
    public string Auth { get; set; } = string.Empty;

    /// <summary>Optional device/browser description, for the user to recognise it.</summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// False once the endpoint is known to be dead (the push service returned 404/410).
    /// Only active subscriptions are targeted when sending.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>When a push was last delivered to this device, if ever.</summary>
    public DateTimeOffset? LastUsedAt { get; set; }

    // Navigation
    public ApplicationUser? User { get; set; }
}
