using FamilyHub.Api.Common;
using FamilyHub.Api.DTOs.Families;

namespace FamilyHub.Api.Interfaces;

public interface IFamilyService
{
    Task<Result<FamilyResponse>> CreateFamilyAsync(string userId, CreateFamilyRequest request);

    Task<Result<FamilyResponse>> GetCurrentFamilyAsync(string userId);

    Task<Result<FamilyResponse>> GetFamilyByIdAsync(string userId, Guid familyId);

    Task<Result<FamilyResponse>> UpdateFamilyAsync(string userId, Guid familyId, UpdateFamilyRequest request);

    Task<Result<IReadOnlyList<FamilyMemberResponse>>> GetMembersAsync(string userId, Guid familyId);

    Task<Result> RemoveMemberAsync(string userId, Guid familyId, Guid memberId);
}
