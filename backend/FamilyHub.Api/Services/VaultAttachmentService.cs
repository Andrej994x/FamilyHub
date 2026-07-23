using FamilyHub.Api.Common;
using FamilyHub.Api.Data;
using FamilyHub.Api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FamilyHub.Api.Services;

/// <summary>
/// Serves and removes individual vault attachments across all record types. Attachments are
/// scoped to a family, so access is validated against the caller's membership in that family.
/// </summary>
public class VaultAttachmentService : VaultServiceBase, IVaultAttachmentService
{
    public VaultAttachmentService(AppDbContext db, IFamilyVaultStorage storage) : base(db, storage) { }

    public async Task<Result<VaultFile>> GetForDownloadAsync(string userId, Guid familyId, Guid attachmentId)
    {
        var (error, message) = await AuthorizeAsync(userId, familyId, requireManage: false);
        if (error is not null) return Result<VaultFile>.Failure(error.Value, message!);

        var attachment = await Db.VaultAttachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.FamilyId == familyId);
        if (attachment is null)
        {
            return Result<VaultFile>.Failure(ErrorType.NotFound, "Attachment not found in this family.");
        }

        var file = Storage.Resolve(attachment.FilePath);
        if (file is null)
        {
            return Result<VaultFile>.Failure(ErrorType.NotFound, "The attachment file could not be found.");
        }

        // Serve with the stored content type and original file name for a friendlier download.
        return Result<VaultFile>.Success(
            new VaultFile(file.AbsolutePath, attachment.ContentType, attachment.FileName ?? file.FileName));
    }

    public async Task<Result> DeleteAsync(string userId, Guid familyId, Guid attachmentId)
    {
        var (error, message) = await AuthorizeAsync(userId, familyId, requireManage: true);
        if (error is not null) return Result.Failure(error.Value, message!);

        var attachment = await Db.VaultAttachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.FamilyId == familyId);
        if (attachment is null)
        {
            return Result.Failure(ErrorType.NotFound, "Attachment not found in this family.");
        }

        Storage.Delete(attachment.FilePath);
        Db.VaultAttachments.Remove(attachment);
        await Db.SaveChangesAsync();
        return Result.Success();
    }
}
