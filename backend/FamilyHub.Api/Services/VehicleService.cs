using FamilyHub.Api.Common;
using FamilyHub.Api.Data;
using FamilyHub.Api.DTOs.Vault;
using FamilyHub.Api.Interfaces;
using FamilyHub.Api.Models;
using FamilyHub.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyHub.Api.Services;

public class VehicleService : VaultServiceBase, IVehicleService
{
    private const VaultRecordType OwnerType = VaultRecordType.Vehicle;

    public VehicleService(AppDbContext db, IFamilyVaultStorage storage, INotificationService notifications)
        : base(db, storage, notifications) { }

    public async Task<Result<VehicleResponse>> CreateAsync(string userId, Guid familyId, CreateVehicleRequest request)
    {
        var (error, message) = await AuthorizeAsync(userId, familyId, requireManage: true);
        if (error is not null) return Result<VehicleResponse>.Failure(error.Value, message!);

        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            FamilyId = familyId,
            Name = request.Name.Trim(),
            Make = Clean(request.Make),
            Model = Clean(request.Model),
            RegistrationNumber = Clean(request.RegistrationNumber),
            RegistrationExpiry = request.RegistrationExpiry,
            InsuranceExpiry = request.InsuranceExpiry,
            NextServiceDate = request.NextServiceDate,
            NextServiceMileage = request.NextServiceMileage,
            Notes = Clean(request.Notes),
            IsImportant = request.IsImportant,
            RelatedMemberId = request.RelatedMemberId,
            CreatedByUserId = userId,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var built = await BuildAttachmentsAsync(userId, familyId, OwnerType, vehicle.Id, request.Attachments);
        if (!built.Succeeded) return Result<VehicleResponse>.Failure(built.ErrorType!.Value, built.Error!);
        var attachments = built.Value!;

        Db.Vehicles.Add(vehicle);
        Db.VaultAttachments.AddRange(attachments);
        await SaveOrRollbackAsync(attachments);

        await NotifyImportantRecordAsync(familyId, userId, vehicle.Name, vehicle.IsImportant, vehicle.RelatedMemberId);

        return Result<VehicleResponse>.Success(ToResponse(vehicle, attachments));
    }

    public async Task<Result<IReadOnlyList<VehicleResponse>>> GetAllAsync(string userId, Guid familyId)
    {
        var (error, message) = await AuthorizeAsync(userId, familyId, requireManage: false);
        if (error is not null) return Result<IReadOnlyList<VehicleResponse>>.Failure(error.Value, message!);

        var vehicles = await Db.Vehicles
            .Where(v => v.FamilyId == familyId)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync();

        var lookup = await GetAttachmentsForOwnersAsync(OwnerType, vehicles.Select(v => v.Id).ToList());
        var list = vehicles.Select(v => ToResponse(v, lookup[v.Id])).ToList();
        return Result<IReadOnlyList<VehicleResponse>>.Success(list);
    }

    public async Task<Result<VehicleResponse>> GetAsync(string userId, Guid familyId, Guid id)
    {
        var (error, message) = await AuthorizeAsync(userId, familyId, requireManage: false);
        if (error is not null) return Result<VehicleResponse>.Failure(error.Value, message!);

        var vehicle = await GetEntityAsync(familyId, id);
        if (vehicle is null) return Result<VehicleResponse>.Failure(ErrorType.NotFound, "Vehicle not found in this family.");

        return Result<VehicleResponse>.Success(ToResponse(vehicle, await GetAttachmentsAsync(OwnerType, id)));
    }

    public async Task<Result<VehicleResponse>> UpdateAsync(string userId, Guid familyId, Guid id, UpdateVehicleRequest request)
    {
        var (error, message) = await AuthorizeAsync(userId, familyId, requireManage: true);
        if (error is not null) return Result<VehicleResponse>.Failure(error.Value, message!);

        var vehicle = await GetEntityAsync(familyId, id);
        if (vehicle is null) return Result<VehicleResponse>.Failure(ErrorType.NotFound, "Vehicle not found in this family.");

        vehicle.Name = request.Name.Trim();
        vehicle.Make = Clean(request.Make);
        vehicle.Model = Clean(request.Model);
        vehicle.RegistrationNumber = Clean(request.RegistrationNumber);
        vehicle.RegistrationExpiry = request.RegistrationExpiry;
        vehicle.InsuranceExpiry = request.InsuranceExpiry;
        vehicle.NextServiceDate = request.NextServiceDate;
        vehicle.NextServiceMileage = request.NextServiceMileage;
        vehicle.Notes = Clean(request.Notes);
        vehicle.IsImportant = request.IsImportant;
        vehicle.RelatedMemberId = request.RelatedMemberId;

        var built = await BuildAttachmentsAsync(userId, familyId, OwnerType, vehicle.Id, request.Attachments);
        if (!built.Succeeded) return Result<VehicleResponse>.Failure(built.ErrorType!.Value, built.Error!);
        Db.VaultAttachments.AddRange(built.Value!);
        await SaveOrRollbackAsync(built.Value!);

        return Result<VehicleResponse>.Success(ToResponse(vehicle, await GetAttachmentsAsync(OwnerType, id)));
    }

    public async Task<Result> DeleteAsync(string userId, Guid familyId, Guid id)
    {
        var (error, message) = await AuthorizeAsync(userId, familyId, requireManage: true);
        if (error is not null) return Result.Failure(error.Value, message!);

        var vehicle = await GetEntityAsync(familyId, id);
        if (vehicle is null) return Result.Failure(ErrorType.NotFound, "Vehicle not found in this family.");

        await RemoveAttachmentsForOwnerAsync(OwnerType, id);
        Db.Vehicles.Remove(vehicle);
        await Db.SaveChangesAsync();
        return Result.Success();
    }

    private async Task SaveOrRollbackAsync(List<VaultAttachment> newAttachments)
    {
        try { await Db.SaveChangesAsync(); }
        catch { foreach (var a in newAttachments) Storage.Delete(a.FilePath); throw; }
    }

    private Task<Vehicle?> GetEntityAsync(Guid familyId, Guid id) =>
        Db.Vehicles.FirstOrDefaultAsync(v => v.Id == id && v.FamilyId == familyId);

    private static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();

    private VehicleResponse ToResponse(Vehicle v, IEnumerable<VaultAttachment> attachments) =>
        new(v.Id, v.FamilyId, v.Name, v.Make, v.Model, v.RegistrationNumber, v.RegistrationExpiry,
            v.InsuranceExpiry, v.NextServiceDate, v.NextServiceMileage, v.Notes, v.IsImportant,
            v.RelatedMemberId, v.CreatedByUserId, v.CreatedAt, ToAttachmentResponses(attachments));
}
