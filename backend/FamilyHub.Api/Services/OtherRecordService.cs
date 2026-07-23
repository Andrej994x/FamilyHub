using FamilyHub.Api.Common;
using FamilyHub.Api.Data;
using FamilyHub.Api.DTOs.Vault;
using FamilyHub.Api.Interfaces;
using FamilyHub.Api.Models;
using FamilyHub.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyHub.Api.Services;

public class OtherRecordService : VaultServiceBase, IOtherRecordService
{
    private const VaultRecordType OwnerType = VaultRecordType.Other;

    public OtherRecordService(AppDbContext db, IFamilyVaultStorage storage) : base(db, storage) { }

    public async Task<Result<OtherRecordResponse>> CreateAsync(string userId, Guid familyId, CreateOtherRecordRequest request)
    {
        var (error, message) = await AuthorizeAsync(userId, familyId, requireManage: true);
        if (error is not null) return Result<OtherRecordResponse>.Failure(error.Value, message!);

        var record = new OtherRecord
        {
            Id = Guid.NewGuid(),
            FamilyId = familyId,
            Title = request.Title.Trim(),
            Description = Clean(request.Description),
            ImportantDate = request.ImportantDate,
            ExpiryDate = request.ExpiryDate,
            CreatedByUserId = userId,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var built = await BuildAttachmentsAsync(userId, familyId, OwnerType, record.Id, request.Attachments);
        if (!built.Succeeded) return Result<OtherRecordResponse>.Failure(built.ErrorType!.Value, built.Error!);
        var attachments = built.Value!;

        Db.OtherRecords.Add(record);
        Db.VaultAttachments.AddRange(attachments);
        await SaveOrRollbackAsync(attachments);

        return Result<OtherRecordResponse>.Success(ToResponse(record, attachments));
    }

    public async Task<Result<IReadOnlyList<OtherRecordResponse>>> GetAllAsync(string userId, Guid familyId)
    {
        var (error, message) = await AuthorizeAsync(userId, familyId, requireManage: false);
        if (error is not null) return Result<IReadOnlyList<OtherRecordResponse>>.Failure(error.Value, message!);

        var records = await Db.OtherRecords
            .Where(o => o.FamilyId == familyId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var lookup = await GetAttachmentsForOwnersAsync(OwnerType, records.Select(o => o.Id).ToList());
        var list = records.Select(o => ToResponse(o, lookup[o.Id])).ToList();
        return Result<IReadOnlyList<OtherRecordResponse>>.Success(list);
    }

    public async Task<Result<OtherRecordResponse>> GetAsync(string userId, Guid familyId, Guid id)
    {
        var (error, message) = await AuthorizeAsync(userId, familyId, requireManage: false);
        if (error is not null) return Result<OtherRecordResponse>.Failure(error.Value, message!);

        var record = await GetEntityAsync(familyId, id);
        if (record is null) return Result<OtherRecordResponse>.Failure(ErrorType.NotFound, "Record not found in this family.");

        return Result<OtherRecordResponse>.Success(ToResponse(record, await GetAttachmentsAsync(OwnerType, id)));
    }

    public async Task<Result<OtherRecordResponse>> UpdateAsync(string userId, Guid familyId, Guid id, UpdateOtherRecordRequest request)
    {
        var (error, message) = await AuthorizeAsync(userId, familyId, requireManage: true);
        if (error is not null) return Result<OtherRecordResponse>.Failure(error.Value, message!);

        var record = await GetEntityAsync(familyId, id);
        if (record is null) return Result<OtherRecordResponse>.Failure(ErrorType.NotFound, "Record not found in this family.");

        record.Title = request.Title.Trim();
        record.Description = Clean(request.Description);
        record.ImportantDate = request.ImportantDate;
        record.ExpiryDate = request.ExpiryDate;

        var built = await BuildAttachmentsAsync(userId, familyId, OwnerType, record.Id, request.Attachments);
        if (!built.Succeeded) return Result<OtherRecordResponse>.Failure(built.ErrorType!.Value, built.Error!);
        Db.VaultAttachments.AddRange(built.Value!);
        await SaveOrRollbackAsync(built.Value!);

        return Result<OtherRecordResponse>.Success(ToResponse(record, await GetAttachmentsAsync(OwnerType, id)));
    }

    public async Task<Result> DeleteAsync(string userId, Guid familyId, Guid id)
    {
        var (error, message) = await AuthorizeAsync(userId, familyId, requireManage: true);
        if (error is not null) return Result.Failure(error.Value, message!);

        var record = await GetEntityAsync(familyId, id);
        if (record is null) return Result.Failure(ErrorType.NotFound, "Record not found in this family.");

        await RemoveAttachmentsForOwnerAsync(OwnerType, id);
        Db.OtherRecords.Remove(record);
        await Db.SaveChangesAsync();
        return Result.Success();
    }

    private async Task SaveOrRollbackAsync(List<VaultAttachment> newAttachments)
    {
        try { await Db.SaveChangesAsync(); }
        catch { foreach (var a in newAttachments) Storage.Delete(a.FilePath); throw; }
    }

    private Task<OtherRecord?> GetEntityAsync(Guid familyId, Guid id) =>
        Db.OtherRecords.FirstOrDefaultAsync(o => o.Id == id && o.FamilyId == familyId);

    private static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();

    private OtherRecordResponse ToResponse(OtherRecord o, IEnumerable<VaultAttachment> attachments) =>
        new(o.Id, o.FamilyId, o.Title, o.Description, o.ImportantDate, o.ExpiryDate,
            o.CreatedByUserId, o.CreatedAt, ToAttachmentResponses(attachments));
}
