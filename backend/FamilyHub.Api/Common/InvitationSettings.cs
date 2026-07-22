namespace FamilyHub.Api.Common;

/// <summary>
/// Configuration for invitations, bound from the "Invitations" section of appsettings.
/// </summary>
public class InvitationSettings
{
    /// <summary>Front-end URL the acceptance token is appended to when building the invite link.</summary>
    public string AcceptUrlBase { get; set; } = "http://localhost:5173/invitations/accept";

    public int ExpiryDays { get; set; } = 7;
}
