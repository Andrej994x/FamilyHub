using FamilyHub.Api.Common;
using FamilyHub.Api.Data;
using FamilyHub.Api.DTOs.Vault;
using FamilyHub.Api.Interfaces;
using FamilyHub.Api.Models;
using FamilyHub.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyHub.Api.Services;

public class PetService : VaultServiceBase, IPetService
{
    private const VaultRecordType OwnerType = VaultRecordType.Pet;

    public PetService(AppDbContext db, IFamilyVaultStorage storage) : base(db, storage) { }

    public async Task<Result<PetResponse>> CreateAsync(string userId, Guid familyId, CreatePetRequest request)
    {
        var (error, message) = await AuthorizeAsync(userId, familyId, requireManage: true);
        if (error is not null) return Result<PetResponse>.Failure(error.Value, message!);

        var pet = new Pet
        {
            Id = Guid.NewGuid(),
            FamilyId = familyId,
            Name = request.Name.Trim(),
            Type = Clean(request.Type),
            Breed = Clean(request.Breed),
            DateOfBirth = request.DateOfBirth,
            MicrochipNumber = Clean(request.MicrochipNumber),
            VaccinationName = Clean(request.VaccinationName),
            LastVaccinationDate = request.LastVaccinationDate,
            NextVaccinationDate = request.NextVaccinationDate,
            Veterinarian = Clean(request.Veterinarian),
            Notes = Clean(request.Notes),
            CreatedByUserId = userId,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var built = await BuildAttachmentsAsync(userId, familyId, OwnerType, pet.Id, request.Attachments);
        if (!built.Succeeded) return Result<PetResponse>.Failure(built.ErrorType!.Value, built.Error!);
        var attachments = built.Value!;

        Db.Pets.Add(pet);
        Db.VaultAttachments.AddRange(attachments);
        await SaveOrRollbackAsync(attachments);

        return Result<PetResponse>.Success(ToResponse(pet, attachments));
    }

    public async Task<Result<IReadOnlyList<PetResponse>>> GetAllAsync(string userId, Guid familyId)
    {
        var (error, message) = await AuthorizeAsync(userId, familyId, requireManage: false);
        if (error is not null) return Result<IReadOnlyList<PetResponse>>.Failure(error.Value, message!);

        var pets = await Db.Pets
            .Where(p => p.FamilyId == familyId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var lookup = await GetAttachmentsForOwnersAsync(OwnerType, pets.Select(p => p.Id).ToList());
        var list = pets.Select(p => ToResponse(p, lookup[p.Id])).ToList();
        return Result<IReadOnlyList<PetResponse>>.Success(list);
    }

    public async Task<Result<PetResponse>> GetAsync(string userId, Guid familyId, Guid id)
    {
        var (error, message) = await AuthorizeAsync(userId, familyId, requireManage: false);
        if (error is not null) return Result<PetResponse>.Failure(error.Value, message!);

        var pet = await GetEntityAsync(familyId, id);
        if (pet is null) return Result<PetResponse>.Failure(ErrorType.NotFound, "Pet not found in this family.");

        return Result<PetResponse>.Success(ToResponse(pet, await GetAttachmentsAsync(OwnerType, id)));
    }

    public async Task<Result<PetResponse>> UpdateAsync(string userId, Guid familyId, Guid id, UpdatePetRequest request)
    {
        var (error, message) = await AuthorizeAsync(userId, familyId, requireManage: true);
        if (error is not null) return Result<PetResponse>.Failure(error.Value, message!);

        var pet = await GetEntityAsync(familyId, id);
        if (pet is null) return Result<PetResponse>.Failure(ErrorType.NotFound, "Pet not found in this family.");

        pet.Name = request.Name.Trim();
        pet.Type = Clean(request.Type);
        pet.Breed = Clean(request.Breed);
        pet.DateOfBirth = request.DateOfBirth;
        pet.MicrochipNumber = Clean(request.MicrochipNumber);
        pet.VaccinationName = Clean(request.VaccinationName);
        pet.LastVaccinationDate = request.LastVaccinationDate;
        pet.NextVaccinationDate = request.NextVaccinationDate;
        pet.Veterinarian = Clean(request.Veterinarian);
        pet.Notes = Clean(request.Notes);

        var built = await BuildAttachmentsAsync(userId, familyId, OwnerType, pet.Id, request.Attachments);
        if (!built.Succeeded) return Result<PetResponse>.Failure(built.ErrorType!.Value, built.Error!);
        Db.VaultAttachments.AddRange(built.Value!);
        await SaveOrRollbackAsync(built.Value!);

        return Result<PetResponse>.Success(ToResponse(pet, await GetAttachmentsAsync(OwnerType, id)));
    }

    public async Task<Result> DeleteAsync(string userId, Guid familyId, Guid id)
    {
        var (error, message) = await AuthorizeAsync(userId, familyId, requireManage: true);
        if (error is not null) return Result.Failure(error.Value, message!);

        var pet = await GetEntityAsync(familyId, id);
        if (pet is null) return Result.Failure(ErrorType.NotFound, "Pet not found in this family.");

        await RemoveAttachmentsForOwnerAsync(OwnerType, id);
        Db.Pets.Remove(pet);
        await Db.SaveChangesAsync();
        return Result.Success();
    }

    private async Task SaveOrRollbackAsync(List<VaultAttachment> newAttachments)
    {
        try { await Db.SaveChangesAsync(); }
        catch { foreach (var a in newAttachments) Storage.Delete(a.FilePath); throw; }
    }

    private Task<Pet?> GetEntityAsync(Guid familyId, Guid id) =>
        Db.Pets.FirstOrDefaultAsync(p => p.Id == id && p.FamilyId == familyId);

    private static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();

    private PetResponse ToResponse(Pet p, IEnumerable<VaultAttachment> attachments) =>
        new(p.Id, p.FamilyId, p.Name, p.Type, p.Breed, p.DateOfBirth, p.MicrochipNumber, p.VaccinationName,
            p.LastVaccinationDate, p.NextVaccinationDate, p.Veterinarian, p.Notes, p.CreatedByUserId,
            p.CreatedAt, ToAttachmentResponses(attachments));
}
