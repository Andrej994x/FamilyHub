using FamilyHub.Api.Common;
using FamilyHub.Api.Data;
using FamilyHub.Api.DTOs.Vault;
using FamilyHub.Api.Interfaces;
using FamilyHub.Api.Models;
using FamilyHub.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyHub.Api.Services;

/// <summary>
/// Shared behaviour for the Family Vault record services: family authorization and the
/// coordination of polymorphic <see cref="VaultAttachment"/> files.
/// </summary>
public abstract class VaultServiceBase
{
    protected readonly AppDbContext Db;
    protected readonly IFamilyVaultStorage Storage;

    protected VaultServiceBase(AppDbContext db, IFamilyVaultStorage storage)
    {
        Db = db;
        Storage = storage;
    }

    /// <summary>
    /// Ensures the family exists and the user is a member. When <paramref name="requireManage"/>
    /// is true, the member must be an Owner or Parent.
    /// </summary>
    protected async Task<(ErrorType? Error, string? Message)> AuthorizeAsync(
        string userId, Guid familyId, bool requireManage)
    {
        var familyExists = await Db.Families.AnyAsync(f => f.Id == familyId);
        if (!familyExists)
        {
            return (ErrorType.NotFound, "Family not found.");
        }

        var membership = await Db.FamilyMembers
            .FirstOrDefaultAsync(m => m.FamilyId == familyId && m.UserId == userId);
        if (membership is null)
        {
            return (ErrorType.Forbidden, "You do not have access to this family.");
        }

        if (requireManage && membership.Role is not (FamilyRole.Owner or FamilyRole.Parent))
        {
            return (ErrorType.Forbidden, "Only an Owner or Parent can manage vault records.");
        }

        return (null, null);
    }

    /// <summary>
    /// Validates and stores the uploaded files as attachment entities for the given owner.
    /// On any validation failure, files already saved in this call are rolled back.
    /// The returned entities are not yet added to the context.
    /// </summary>
    protected async Task<Result<List<VaultAttachment>>> BuildAttachmentsAsync(
        string userId, Guid familyId, VaultRecordType ownerType, Guid ownerId, List<IFormFile>? files)
    {
        var attachments = new List<VaultAttachment>();
        if (files is null || files.Count == 0)
        {
            return Result<List<VaultAttachment>>.Success(attachments);
        }

        foreach (var file in files)
        {
            var saved = await Storage.SaveAsync(file);
            if (!saved.Succeeded)
            {
                foreach (var a in attachments)
                {
                    Storage.Delete(a.FilePath);
                }
                return Result<List<VaultAttachment>>.Failure(saved.ErrorType!.Value, saved.Error!);
            }

            var info = saved.Value!;
            attachments.Add(new VaultAttachment
            {
                Id = Guid.NewGuid(),
                FamilyId = familyId,
                OwnerType = ownerType,
                OwnerId = ownerId,
                FilePath = info.RelativePath,
                FileName = info.FileName,
                ContentType = info.ContentType,
                FileSizeBytes = info.SizeBytes,
                CreatedByUserId = userId,
                CreatedAt = DateTimeOffset.UtcNow,
            });
        }

        return Result<List<VaultAttachment>>.Success(attachments);
    }

    protected Task<List<VaultAttachment>> GetAttachmentsAsync(VaultRecordType ownerType, Guid ownerId) =>
        Db.VaultAttachments
            .Where(a => a.OwnerType == ownerType && a.OwnerId == ownerId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();

    /// <summary>Attachments for many owners at once, grouped by owner id (avoids N+1 in lists).</summary>
    protected async Task<ILookup<Guid, VaultAttachment>> GetAttachmentsForOwnersAsync(
        VaultRecordType ownerType, IReadOnlyCollection<Guid> ownerIds)
    {
        if (ownerIds.Count == 0)
        {
            return Array.Empty<VaultAttachment>().ToLookup(a => a.OwnerId);
        }

        var attachments = await Db.VaultAttachments
            .Where(a => a.OwnerType == ownerType && ownerIds.Contains(a.OwnerId))
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();

        return attachments.ToLookup(a => a.OwnerId);
    }

    /// <summary>Deletes the files and attachment rows for an owner. Caller saves changes.</summary>
    protected async Task RemoveAttachmentsForOwnerAsync(VaultRecordType ownerType, Guid ownerId)
    {
        var attachments = await GetAttachmentsAsync(ownerType, ownerId);
        foreach (var attachment in attachments)
        {
            Storage.Delete(attachment.FilePath);
        }
        Db.VaultAttachments.RemoveRange(attachments);
    }

    protected static VaultAttachmentResponse ToAttachmentResponse(VaultAttachment a) =>
        new(a.Id, a.FileName, a.ContentType, a.FileSizeBytes, a.CreatedAt);

    protected static IReadOnlyList<VaultAttachmentResponse> ToAttachmentResponses(
        IEnumerable<VaultAttachment> attachments) =>
        attachments.Select(ToAttachmentResponse).ToList();
}
