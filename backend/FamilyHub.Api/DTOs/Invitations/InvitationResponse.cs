using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.DTOs.Invitations;

/// <summary>
/// Public representation of an invitation. Never includes the secret token.
/// </summary>
public record InvitationResponse(
    Guid Id,
    Guid FamilyId,
    string Email,
    FamilyRole Role,
    InvitationStatus Status,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastSentAt);
