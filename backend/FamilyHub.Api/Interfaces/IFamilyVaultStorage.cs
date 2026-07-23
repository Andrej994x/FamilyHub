using FamilyHub.Api.Common;

namespace FamilyHub.Api.Interfaces;

/// <summary>A file that was just saved to the vault, with its stored metadata.</summary>
public record StoredFile(string RelativePath, string FileName, string ContentType, long SizeBytes);

/// <summary>A resolved attachment ready to be streamed back to the client.</summary>
public record VaultFile(string AbsolutePath, string ContentType, string FileName);

/// <summary>
/// Stores family-vault attachments on the local filesystem under uploads/family-vault.
/// Enforces the allowed types (jpg, png, pdf) and the 5 MB size limit.
/// </summary>
public interface IFamilyVaultStorage
{
    /// <summary>Validates and saves the file, returning its stored metadata on success.</summary>
    Task<Result<StoredFile>> SaveAsync(IFormFile file);

    /// <summary>Deletes the file at the given relative path if it exists. Safe to call with null.</summary>
    void Delete(string? relativePath);

    /// <summary>Resolves a stored relative path to a physical file, or null if it is missing.</summary>
    VaultFile? Resolve(string? relativePath);
}
