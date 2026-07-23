using FamilyHub.Api.Common;
using FamilyHub.Api.DTOs.Documents;

namespace FamilyHub.Api.Interfaces;

public interface IFamilyDocumentService
{
    Task<Result<FamilyDocumentResponse>> CreateDocumentAsync(
        string userId, Guid familyId, CreateFamilyDocumentRequest request);

    Task<Result<IReadOnlyList<FamilyDocumentResponse>>> GetDocumentsAsync(
        string userId, Guid familyId, DocumentFilter filter);

    Task<Result<FamilyDocumentResponse>> GetDocumentAsync(string userId, Guid familyId, Guid documentId);

    Task<Result<FamilyDocumentResponse>> UpdateDocumentAsync(
        string userId, Guid familyId, Guid documentId, UpdateFamilyDocumentRequest request);

    Task<Result> DeleteDocumentAsync(string userId, Guid familyId, Guid documentId);

    Task<Result<VaultFile>> GetAttachmentAsync(string userId, Guid familyId, Guid documentId);
}
