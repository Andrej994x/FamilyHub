using FamilyHub.Api.Common;
using FamilyHub.Api.DTOs.Children;

namespace FamilyHub.Api.Interfaces;

public interface IChildProfileService
{
    Task<Result<ChildResponse>> CreateChildAsync(string userId, Guid familyId, CreateChildRequest request);

    Task<Result<IReadOnlyList<ChildResponse>>> GetChildrenAsync(string userId, Guid familyId);

    Task<Result<ChildResponse>> GetChildAsync(string userId, Guid familyId, Guid childId);

    Task<Result<ChildResponse>> UpdateChildAsync(string userId, Guid familyId, Guid childId, UpdateChildRequest request);

    Task<Result> DeleteChildAsync(string userId, Guid familyId, Guid childId);
}
