namespace FamilyHub.Api.Common;

/// <summary>
/// Web Push (VAPID) configuration, bound from the "Push" section of appsettings. When the
/// key pair is not configured, pushes are logged instead of delivered (development), mirroring
/// the email sender's behaviour.
/// </summary>
public class PushSettings
{
    /// <summary>VAPID subject — a mailto: or https URL identifying the application server.</summary>
    public string Subject { get; set; } = "mailto:admin@familyhub.local";

    /// <summary>VAPID public key (base64url). Shared with the browser to create a subscription.</summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>VAPID private key (base64url). Secret — used to sign push requests.</summary>
    public string PrivateKey { get; set; } = string.Empty;

    /// <summary>True only when a usable key pair is present.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(PublicKey) && !string.IsNullOrWhiteSpace(PrivateKey);
}
