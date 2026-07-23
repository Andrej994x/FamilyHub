using FamilyHub.Api.Common;
using FamilyHub.Api.DTOs.Vault;

namespace FamilyHub.Api.Interfaces;

public interface IVehicleService
{
    Task<Result<VehicleResponse>> CreateAsync(string userId, Guid familyId, CreateVehicleRequest request);
    Task<Result<IReadOnlyList<VehicleResponse>>> GetAllAsync(string userId, Guid familyId);
    Task<Result<VehicleResponse>> GetAsync(string userId, Guid familyId, Guid id);
    Task<Result<VehicleResponse>> UpdateAsync(string userId, Guid familyId, Guid id, UpdateVehicleRequest request);
    Task<Result> DeleteAsync(string userId, Guid familyId, Guid id);
}

public interface IPetService
{
    Task<Result<PetResponse>> CreateAsync(string userId, Guid familyId, CreatePetRequest request);
    Task<Result<IReadOnlyList<PetResponse>>> GetAllAsync(string userId, Guid familyId);
    Task<Result<PetResponse>> GetAsync(string userId, Guid familyId, Guid id);
    Task<Result<PetResponse>> UpdateAsync(string userId, Guid familyId, Guid id, UpdatePetRequest request);
    Task<Result> DeleteAsync(string userId, Guid familyId, Guid id);
}

public interface IHomeRecordService
{
    Task<Result<HomeRecordResponse>> CreateAsync(string userId, Guid familyId, CreateHomeRecordRequest request);
    Task<Result<IReadOnlyList<HomeRecordResponse>>> GetAllAsync(string userId, Guid familyId);
    Task<Result<HomeRecordResponse>> GetAsync(string userId, Guid familyId, Guid id);
    Task<Result<HomeRecordResponse>> UpdateAsync(string userId, Guid familyId, Guid id, UpdateHomeRecordRequest request);
    Task<Result> DeleteAsync(string userId, Guid familyId, Guid id);
}

public interface IWarrantyService
{
    Task<Result<WarrantyResponse>> CreateAsync(string userId, Guid familyId, CreateWarrantyRequest request);
    Task<Result<IReadOnlyList<WarrantyResponse>>> GetAllAsync(string userId, Guid familyId);
    Task<Result<WarrantyResponse>> GetAsync(string userId, Guid familyId, Guid id);
    Task<Result<WarrantyResponse>> UpdateAsync(string userId, Guid familyId, Guid id, UpdateWarrantyRequest request);
    Task<Result> DeleteAsync(string userId, Guid familyId, Guid id);
}

public interface IOtherRecordService
{
    Task<Result<OtherRecordResponse>> CreateAsync(string userId, Guid familyId, CreateOtherRecordRequest request);
    Task<Result<IReadOnlyList<OtherRecordResponse>>> GetAllAsync(string userId, Guid familyId);
    Task<Result<OtherRecordResponse>> GetAsync(string userId, Guid familyId, Guid id);
    Task<Result<OtherRecordResponse>> UpdateAsync(string userId, Guid familyId, Guid id, UpdateOtherRecordRequest request);
    Task<Result> DeleteAsync(string userId, Guid familyId, Guid id);
}

public interface IVaultAttachmentService
{
    Task<Result<VaultFile>> GetForDownloadAsync(string userId, Guid familyId, Guid attachmentId);
    Task<Result> DeleteAsync(string userId, Guid familyId, Guid attachmentId);
}
