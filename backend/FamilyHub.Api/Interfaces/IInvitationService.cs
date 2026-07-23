using FamilyHub.Api.Common;
using FamilyHub.Api.DTOs.Invitations;

namespace FamilyHub.Api.Interfaces;

public interface IInvitationService
{
    Task<Result<CreatedInvitationResponse>> CreateInvitationAsync(
        string userId, Guid familyId, CreateInvitationRequest request);

    Task<Result<CreatedInvitationResponse>> ResendInvitationAsync(
        string userId, Guid familyId, Guid invitationId);

    Task<Result<IReadOnlyList<InvitationResponse>>> GetInvitationsAsync(string userId, Guid familyId);

    Task<Result<InvitationResponse>> AcceptInvitationAsync(string userId, string token);

    Task<Result> CancelInvitationAsync(string userId, Guid familyId, Guid invitationId);
}
