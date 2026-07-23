using FamilyHub.Api.Common;
using FamilyHub.Api.Data;
using FamilyHub.Api.DTOs.Vault;
using FamilyHub.Api.Interfaces;
using FamilyHub.Api.Models;
using FamilyHub.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyHub.Api.Services;

public class HomeRecordService : VaultServiceBase, IHomeRecordService
{
    private const VaultRecordType OwnerType = VaultRecordType.Home;

    public HomeRecordService(AppDbContext db, IFamilyVaultStorage storage, INotificationService notifications)
        : base(db, storage, notifications) { }

    public async Task<Result<HomeRecordResponse>> CreateAsync(string userId, Guid familyId, CreateHomeRecordRequest request)
    {
        var (error, message) = await AuthorizeAsync(userId, familyId, requireManage: true);
        if (error is not null) return Result<HomeRecordResponse>.Failure(error.Value, message!);

        var record = new HomeRecord
        {
            Id = Guid.NewGuid(),
            FamilyId = familyId,
            Title = request.Title.Trim(),
            Type = Clean(request.Type),
            Provider = Clean(request.Provider),
            IssueDate = request.IssueDate,
            RenewalDate = request.RenewalDate,
            Notes = Clean(request.Notes),
            IsImportant = request.IsImportant,
            RelatedMemberId = request.RelatedMemberId,
            CreatedByUserId = userId,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var built = await BuildAttachmentsAsync(userId, familyId, OwnerType, record.Id, request.Attachments);
        if (!built.Succeeded) return Result<HomeRecordResponse>.Failure(built.ErrorType!.Value, built.Error!);
        var attachments = built.Value!;

        Db.HomeRecords.Add(record);
        Db.VaultAttachments.AddRange(attachments);
        await SaveOrRollbackAsync(attachments);

        await NotifyImportantRecordAsync(familyId, userId, record.Title, record.IsImportant, record.RelatedMemberId);

        return Result<HomeRecordResponse>.Success(ToResponse(record, attachments));
    }

    public async Task<Result<IReadOnlyList<HomeRecordResponse>>> GetAllAsync(string userId, Guid familyId)
    {
        var (error, message) = await AuthorizeAsync(userId, familyId, requireManage: false);
        if (error is not null) return Result<IReadOnlyList<HomeRecordResponse>>.Failure(error.Value, message!);

        var records = await Db.HomeRecords
            .Where(h => h.FamilyId == familyId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();

        var lookup = await GetAttachmentsForOwnersAsync(OwnerType, records.Select(h => h.Id).ToList());
        var list = records.Select(h => ToResponse(h, lookup[h.Id])).ToList();
        return Result<IReadOnlyList<HomeRecordResponse>>.Success(list);
    }

    public async Task<Result<HomeRecordResponse>> GetAsync(string userId, Guid familyId, Guid id)
    {
        var (error, message) = await AuthorizeAsync(userId, familyId, requireManage: false);
        if (error is not null) return Result<HomeRecordResponse>.Failure(error.Value, message!);

        var record = await GetEntityAsync(familyId, id);
        if (record is null) return Result<HomeRecordResponse>.Failure(ErrorType.NotFound, "Home record not found in this family.");

        return Result<HomeRecordResponse>.Success(ToResponse(record, await GetAttachmentsAsync(OwnerType, id)));
    }

    public async Task<Result<HomeRecordResponse>> UpdateAsync(string userId, Guid familyId, Guid id, UpdateHomeRecordRequest request)
    {
        var (error, message) = await AuthorizeAsync(userId, familyId, requireManage: true);
        if (error is not null) return Result<HomeRecordResponse>.Failure(error.Value, message!);

        var record = await GetEntityAsync(familyId, id);
        if (record is null) return Result<HomeRecordResponse>.Failure(ErrorType.NotFound, "Home record not found in this family.");

        record.Title = request.Title.Trim();
        record.Type = Clean(request.Type);
        record.Provider = Clean(request.Provider);
        record.IssueDate = request.IssueDate;
        record.RenewalDate = request.RenewalDate;
        record.Notes = Clean(request.Notes);
        record.IsImportant = request.IsImportant;
        record.RelatedMemberId = request.RelatedMemberId;

        var built = await BuildAttachmentsAsync(userId, familyId, OwnerType, record.Id, request.Attachments);
        if (!built.Succeeded) return Result<HomeRecordResponse>.Failure(built.ErrorType!.Value, built.Error!);
        Db.VaultAttachments.AddRange(built.Value!);
        await SaveOrRollbackAsync(built.Value!);

        return Result<HomeRecordResponse>.Success(ToResponse(record, await GetAttachmentsAsync(OwnerType, id)));
    }

    public async Task<Result> DeleteAsync(string userId, Guid familyId, Guid id)
    {
        var (error, message) = await AuthorizeAsync(userId, familyId, requireManage: true);
        if (error is not null) return Result.Failure(error.Value, message!);

        var record = await GetEntityAsync(familyId, id);
        if (record is null) return Result.Failure(ErrorType.NotFound, "Home record not found in this family.");

        await RemoveAttachmentsForOwnerAsync(OwnerType, id);
        Db.HomeRecords.Remove(record);
        await Db.SaveChangesAsync();
        return Result.Success();
    }

    private async Task SaveOrRollbackAsync(List<VaultAttachment> newAttachments)
    {
        try { await Db.SaveChangesAsync(); }
        catch { foreach (var a in newAttachments) Storage.Delete(a.FilePath); throw; }
    }

    private Task<HomeRecord?> GetEntityAsync(Guid familyId, Guid id) =>
        Db.HomeRecords.FirstOrDefaultAsync(h => h.Id == id && h.FamilyId == familyId);

    private static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();

    private HomeRecordResponse ToResponse(HomeRecord h, IEnumerable<VaultAttachment> attachments) =>
        new(h.Id, h.FamilyId, h.Title, h.Type, h.Provider, h.IssueDate, h.RenewalDate, h.Notes,
            h.IsImportant, h.RelatedMemberId, h.CreatedByUserId, h.CreatedAt, ToAttachmentResponses(attachments));
}
