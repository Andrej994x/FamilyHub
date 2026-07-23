using FamilyHub.Api.Common;
using FamilyHub.Api.Data;
using FamilyHub.Api.DTOs.Vault;
using FamilyHub.Api.Interfaces;
using FamilyHub.Api.Models;
using FamilyHub.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyHub.Api.Services;

public class WarrantyService : VaultServiceBase, IWarrantyService
{
    private const VaultRecordType OwnerType = VaultRecordType.Warranty;

    public WarrantyService(AppDbContext db, IFamilyVaultStorage storage, INotificationService notifications)
        : base(db, storage, notifications) { }

    public async Task<Result<WarrantyResponse>> CreateAsync(string userId, Guid familyId, CreateWarrantyRequest request)
    {
        var (error, message) = await AuthorizeAsync(userId, familyId, requireManage: true);
        if (error is not null) return Result<WarrantyResponse>.Failure(error.Value, message!);

        var warranty = new Warranty
        {
            Id = Guid.NewGuid(),
            FamilyId = familyId,
            ProductName = request.ProductName.Trim(),
            Store = Clean(request.Store),
            PurchaseDate = request.PurchaseDate,
            WarrantyExpiryDate = request.WarrantyExpiryDate,
            SerialNumber = Clean(request.SerialNumber),
            Notes = Clean(request.Notes),
            IsImportant = request.IsImportant,
            RelatedMemberId = request.RelatedMemberId,
            CreatedByUserId = userId,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var built = await BuildAttachmentsAsync(userId, familyId, OwnerType, warranty.Id, request.Attachments);
        if (!built.Succeeded) return Result<WarrantyResponse>.Failure(built.ErrorType!.Value, built.Error!);
        var attachments = built.Value!;

        Db.Warranties.Add(warranty);
        Db.VaultAttachments.AddRange(attachments);
        await SaveOrRollbackAsync(attachments);

        await NotifyImportantRecordAsync(familyId, userId, warranty.ProductName, warranty.IsImportant, warranty.RelatedMemberId);

        return Result<WarrantyResponse>.Success(ToResponse(warranty, attachments));
    }

    public async Task<Result<IReadOnlyList<WarrantyResponse>>> GetAllAsync(string userId, Guid familyId)
    {
        var (error, message) = await AuthorizeAsync(userId, familyId, requireManage: false);
        if (error is not null) return Result<IReadOnlyList<WarrantyResponse>>.Failure(error.Value, message!);

        var warranties = await Db.Warranties
            .Where(w => w.FamilyId == familyId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();

        var lookup = await GetAttachmentsForOwnersAsync(OwnerType, warranties.Select(w => w.Id).ToList());
        var list = warranties.Select(w => ToResponse(w, lookup[w.Id])).ToList();
        return Result<IReadOnlyList<WarrantyResponse>>.Success(list);
    }

    public async Task<Result<WarrantyResponse>> GetAsync(string userId, Guid familyId, Guid id)
    {
        var (error, message) = await AuthorizeAsync(userId, familyId, requireManage: false);
        if (error is not null) return Result<WarrantyResponse>.Failure(error.Value, message!);

        var warranty = await GetEntityAsync(familyId, id);
        if (warranty is null) return Result<WarrantyResponse>.Failure(ErrorType.NotFound, "Warranty not found in this family.");

        return Result<WarrantyResponse>.Success(ToResponse(warranty, await GetAttachmentsAsync(OwnerType, id)));
    }

    public async Task<Result<WarrantyResponse>> UpdateAsync(string userId, Guid familyId, Guid id, UpdateWarrantyRequest request)
    {
        var (error, message) = await AuthorizeAsync(userId, familyId, requireManage: true);
        if (error is not null) return Result<WarrantyResponse>.Failure(error.Value, message!);

        var warranty = await GetEntityAsync(familyId, id);
        if (warranty is null) return Result<WarrantyResponse>.Failure(ErrorType.NotFound, "Warranty not found in this family.");

        warranty.ProductName = request.ProductName.Trim();
        warranty.Store = Clean(request.Store);
        warranty.PurchaseDate = request.PurchaseDate;
        warranty.WarrantyExpiryDate = request.WarrantyExpiryDate;
        warranty.SerialNumber = Clean(request.SerialNumber);
        warranty.Notes = Clean(request.Notes);
        warranty.IsImportant = request.IsImportant;
        warranty.RelatedMemberId = request.RelatedMemberId;

        var built = await BuildAttachmentsAsync(userId, familyId, OwnerType, warranty.Id, request.Attachments);
        if (!built.Succeeded) return Result<WarrantyResponse>.Failure(built.ErrorType!.Value, built.Error!);
        Db.VaultAttachments.AddRange(built.Value!);
        await SaveOrRollbackAsync(built.Value!);

        return Result<WarrantyResponse>.Success(ToResponse(warranty, await GetAttachmentsAsync(OwnerType, id)));
    }

    public async Task<Result> DeleteAsync(string userId, Guid familyId, Guid id)
    {
        var (error, message) = await AuthorizeAsync(userId, familyId, requireManage: true);
        if (error is not null) return Result.Failure(error.Value, message!);

        var warranty = await GetEntityAsync(familyId, id);
        if (warranty is null) return Result.Failure(ErrorType.NotFound, "Warranty not found in this family.");

        await RemoveAttachmentsForOwnerAsync(OwnerType, id);
        Db.Warranties.Remove(warranty);
        await Db.SaveChangesAsync();
        return Result.Success();
    }

    private async Task SaveOrRollbackAsync(List<VaultAttachment> newAttachments)
    {
        try { await Db.SaveChangesAsync(); }
        catch { foreach (var a in newAttachments) Storage.Delete(a.FilePath); throw; }
    }

    private Task<Warranty?> GetEntityAsync(Guid familyId, Guid id) =>
        Db.Warranties.FirstOrDefaultAsync(w => w.Id == id && w.FamilyId == familyId);

    private static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();

    private WarrantyResponse ToResponse(Warranty w, IEnumerable<VaultAttachment> attachments) =>
        new(w.Id, w.FamilyId, w.ProductName, w.Store, w.PurchaseDate, w.WarrantyExpiryDate, w.SerialNumber,
            w.Notes, w.IsImportant, w.RelatedMemberId, w.CreatedByUserId, w.CreatedAt, ToAttachmentResponses(attachments));
}
