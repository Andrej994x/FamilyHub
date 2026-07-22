using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.DTOs.Invitations;

public record CreateInvitationRequest(string Email, FamilyRole Role);
