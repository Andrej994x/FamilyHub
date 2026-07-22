using FamilyHub.Api.Common;
using FamilyHub.Api.DTOs.Pickups;

namespace FamilyHub.Api.Interfaces;

public interface IPickupScheduleService
{
    Task<Result<PickupResponse>> CreatePickupAsync(string userId, Guid familyId, CreatePickupRequest request);

    Task<Result<IReadOnlyList<PickupResponse>>> GetPickupsAsync(string userId, Guid familyId);

    Task<Result<PickupResponse>> GetPickupAsync(string userId, Guid familyId, Guid pickupId);

    Task<Result<PickupResponse>> UpdatePickupAsync(string userId, Guid familyId, Guid pickupId, UpdatePickupRequest request);

    Task<Result<PickupResponse>> UpdateStatusAsync(string userId, Guid familyId, Guid pickupId, UpdatePickupStatusRequest request);

    Task<Result<PickupResponse>> TakeOverAsync(string userId, Guid familyId, Guid pickupId);

    Task<Result> DeletePickupAsync(string userId, Guid familyId, Guid pickupId);
}
