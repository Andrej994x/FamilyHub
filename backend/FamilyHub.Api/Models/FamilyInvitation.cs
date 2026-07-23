using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.Models;

public class FamilyInvitation
{
    public Guid Id { get; set; }

    public Guid FamilyId { get; set; }

    public string Email { get; set; } = string.Empty;

    public FamilyRole Role { get; set; }

    /// <summary>Opaque token used to accept the invitation.</summary>
    public string Token { get; set; } = string.Empty;

    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>When the invitation email was last sent (on create or resend).</summary>
    public DateTimeOffset? LastSentAt { get; set; }

    // Navigation properties
    public Family? Family { get; set; }
}
