using FamilyHub.Api.Common;
using FamilyHub.Api.Data;
using FamilyHub.Api.DTOs.Documents;
using FamilyHub.Api.Interfaces;
using FamilyHub.Api.Models;
using FamilyHub.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyHub.Api.Services;

public class FamilyDocumentService : IFamilyDocumentService
{
    private readonly AppDbContext _db;
    private readonly IFamilyVaultStorage _storage;

    public FamilyDocumentService(AppDbContext db, IFamilyVaultStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<Result<FamilyDocumentResponse>> CreateDocumentAsync(
        string userId, Guid familyId, CreateFamilyDocumentRequest request)
    {
        var (_, error, message) = await AuthorizeAsync(userId, familyId, requireManage: true);
        if (error is not null)
        {
            return Result<FamilyDocumentResponse>.Failure(error.Value, message!);
        }

        var subjectError = await ValidateSubjectAsync(familyId, request.FamilyMemberId, request.ChildProfileId);
        if (subjectError is not null)
        {
            return Result<FamilyDocumentResponse>.Failure(ErrorType.Validation, subjectError);
        }

        // Persist the attachment first (if any) so a validation failure short-circuits
        // before we touch the database.
        string? attachmentPath = null;
        if (request.Attachment is not null)
        {
            var saved = await _storage.SaveAsync(request.Attachment);
            if (!saved.Succeeded)
            {
                return Result<FamilyDocumentResponse>.Failure(saved.ErrorType!.Value, saved.Error!);
            }
            attachmentPath = saved.Value!.RelativePath;
        }

        var document = new FamilyDocument
        {
            Id = Guid.NewGuid(),
            FamilyId = familyId,
            FamilyMemberId = request.FamilyMemberId,
            ChildProfileId = request.ChildProfileId,
            DocumentType = request.DocumentType,
            DocumentNumber = Clean(request.DocumentNumber),
            IssueDate = request.IssueDate,
            ExpiryDate = request.ExpiryDate,
            Notes = Clean(request.Notes),
            AttachmentPath = attachmentPath,
            CreatedByUserId = userId,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _db.FamilyDocuments.Add(document);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch
        {
            _storage.Delete(attachmentPath); // avoid orphaning the just-saved file
            throw;
        }

        return Result<FamilyDocumentResponse>.Success(ToResponse(document));
    }

    public async Task<Result<IReadOnlyList<FamilyDocumentResponse>>> GetDocumentsAsync(
        string userId, Guid familyId, DocumentFilter filter)
    {
        var (_, error, message) = await AuthorizeAsync(userId, familyId, requireManage: false);
        if (error is not null)
        {
            return Result<IReadOnlyList<FamilyDocumentResponse>>.Failure(error.Value, message!);
        }

        var query = _db.FamilyDocuments.Where(d => d.FamilyId == familyId);

        if (filter.DocumentType is not null)
        {
            query = query.Where(d => d.DocumentType == filter.DocumentType);
        }

        if (filter.FamilyMemberId is not null)
        {
            query = query.Where(d => d.FamilyMemberId == filter.FamilyMemberId);
        }

        if (filter.ChildProfileId is not null)
        {
            query = query.Where(d => d.ChildProfileId == filter.ChildProfileId);
        }

        var documents = await query
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        return Result<IReadOnlyList<FamilyDocumentResponse>>.Success(
            documents.Select(ToResponse).ToList());
    }

    public async Task<Result<FamilyDocumentResponse>> GetDocumentAsync(
        string userId, Guid familyId, Guid documentId)
    {
        var (_, error, message) = await AuthorizeAsync(userId, familyId, requireManage: false);
        if (error is not null)
        {
            return Result<FamilyDocumentResponse>.Failure(error.Value, message!);
        }

        var document = await GetEntityAsync(familyId, documentId);
        return document is null
            ? Result<FamilyDocumentResponse>.Failure(ErrorType.NotFound, "Document not found in this family.")
            : Result<FamilyDocumentResponse>.Success(ToResponse(document));
    }

    public async Task<Result<FamilyDocumentResponse>> UpdateDocumentAsync(
        string userId, Guid familyId, Guid documentId, UpdateFamilyDocumentRequest request)
    {
        var (_, error, message) = await AuthorizeAsync(userId, familyId, requireManage: true);
        if (error is not null)
        {
            return Result<FamilyDocumentResponse>.Failure(error.Value, message!);
        }

        var document = await GetEntityAsync(familyId, documentId);
        if (document is null)
        {
            return Result<FamilyDocumentResponse>.Failure(ErrorType.NotFound, "Document not found in this family.");
        }

        var subjectError = await ValidateSubjectAsync(familyId, request.FamilyMemberId, request.ChildProfileId);
        if (subjectError is not null)
        {
            return Result<FamilyDocumentResponse>.Failure(ErrorType.Validation, subjectError);
        }

        var oldAttachmentPath = document.AttachmentPath;
        var newAttachmentPath = oldAttachmentPath;

        if (request.Attachment is not null)
        {
            var saved = await _storage.SaveAsync(request.Attachment);
            if (!saved.Succeeded)
            {
                return Result<FamilyDocumentResponse>.Failure(saved.ErrorType!.Value, saved.Error!);
            }
            newAttachmentPath = saved.Value!.RelativePath;
        }
        else if (request.RemoveAttachment)
        {
            newAttachmentPath = null;
        }

        document.FamilyMemberId = request.FamilyMemberId;
        document.ChildProfileId = request.ChildProfileId;
        document.DocumentType = request.DocumentType;
        document.DocumentNumber = Clean(request.DocumentNumber);
        document.IssueDate = request.IssueDate;
        document.ExpiryDate = request.ExpiryDate;
        document.Notes = Clean(request.Notes);
        document.AttachmentPath = newAttachmentPath;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch
        {
            // If we saved a replacement file, remove it so it does not leak.
            if (newAttachmentPath != oldAttachmentPath)
            {
                _storage.Delete(newAttachmentPath);
            }
            throw;
        }

        // The old file is only removed once the new state is safely persisted.
        if (oldAttachmentPath is not null && oldAttachmentPath != newAttachmentPath)
        {
            _storage.Delete(oldAttachmentPath);
        }

        return Result<FamilyDocumentResponse>.Success(ToResponse(document));
    }

    public async Task<Result> DeleteDocumentAsync(string userId, Guid familyId, Guid documentId)
    {
        var (_, error, message) = await AuthorizeAsync(userId, familyId, requireManage: true);
        if (error is not null)
        {
            return Result.Failure(error.Value, message!);
        }

        var document = await GetEntityAsync(familyId, documentId);
        if (document is null)
        {
            return Result.Failure(ErrorType.NotFound, "Document not found in this family.");
        }

        var attachmentPath = document.AttachmentPath;

        _db.FamilyDocuments.Remove(document);
        await _db.SaveChangesAsync();

        _storage.Delete(attachmentPath);

        return Result.Success();
    }

    public async Task<Result<VaultFile>> GetAttachmentAsync(string userId, Guid familyId, Guid documentId)
    {
        var (_, error, message) = await AuthorizeAsync(userId, familyId, requireManage: false);
        if (error is not null)
        {
            return Result<VaultFile>.Failure(error.Value, message!);
        }

        var document = await GetEntityAsync(familyId, documentId);
        if (document is null)
        {
            return Result<VaultFile>.Failure(ErrorType.NotFound, "Document not found in this family.");
        }

        if (document.AttachmentPath is null)
        {
            return Result<VaultFile>.Failure(ErrorType.NotFound, "This document has no attachment.");
        }

        var file = _storage.Resolve(document.AttachmentPath);
        return file is null
            ? Result<VaultFile>.Failure(ErrorType.NotFound, "The attachment file could not be found.")
            : Result<VaultFile>.Success(file);
    }

    /// <summary>
    /// Ensures the family exists and the user is a member. When <paramref name="requireManage"/>
    /// is true, the member must be an Owner or Parent.
    /// </summary>
    private async Task<(FamilyMember? Membership, ErrorType? Error, string? Message)> AuthorizeAsync(
        string userId, Guid familyId, bool requireManage)
    {
        var familyExists = await _db.Families.AnyAsync(f => f.Id == familyId);
        if (!familyExists)
        {
            return (null, ErrorType.NotFound, "Family not found.");
        }

        var membership = await _db.FamilyMembers
            .FirstOrDefaultAsync(m => m.FamilyId == familyId && m.UserId == userId);
        if (membership is null)
        {
            return (null, ErrorType.Forbidden, "You do not have access to this family.");
        }

        if (requireManage && membership.Role is not (FamilyRole.Owner or FamilyRole.Parent))
        {
            return (null, ErrorType.Forbidden, "Only an Owner or Parent can manage documents.");
        }

        return (membership, null, null);
    }

    /// <summary>Ensures the referenced member/child (whichever is set) belongs to the family.</summary>
    private async Task<string?> ValidateSubjectAsync(Guid familyId, Guid? memberId, Guid? childId)
    {
        if (memberId is Guid m &&
            !await _db.FamilyMembers.AnyAsync(x => x.Id == m && x.FamilyId == familyId))
        {
            return "The selected family member is not part of this family.";
        }

        if (childId is Guid c &&
            !await _db.ChildProfiles.AnyAsync(x => x.Id == c && x.FamilyId == familyId))
        {
            return "The selected child is not part of this family.";
        }

        return null;
    }

    private Task<FamilyDocument?> GetEntityAsync(Guid familyId, Guid documentId) =>
        _db.FamilyDocuments.FirstOrDefaultAsync(d => d.Id == documentId && d.FamilyId == familyId);

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static FamilyDocumentResponse ToResponse(FamilyDocument d) =>
        new(d.Id, d.FamilyId, d.FamilyMemberId, d.ChildProfileId, d.DocumentType,
            d.DocumentNumber, d.IssueDate, d.ExpiryDate, d.Notes, d.AttachmentPath,
            d.AttachmentPath is not null, d.CreatedByUserId, d.CreatedAt);
}
