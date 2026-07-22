namespace FamilyHub.Api.DTOs.Invitations;

/// <summary>
/// Returned to the inviter when an invitation is created. Includes the token and
/// acceptance URL so the link can be shared (until a real email provider is wired up).
/// </summary>
public record CreatedInvitationResponse(
    InvitationResponse Invitation,
    string Token,
    string AcceptUrl);
