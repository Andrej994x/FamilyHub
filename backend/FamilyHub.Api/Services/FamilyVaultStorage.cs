using FamilyHub.Api.Common;
using FamilyHub.Api.Interfaces;

namespace FamilyHub.Api.Services;

public class FamilyVaultStorage : IFamilyVaultStorage
{
    // Relative to the application content root.
    private const string RelativeFolder = "uploads/family-vault";
    private const long MaxBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".pdf" };

    private static readonly Dictionary<string, string> ContentTypeByExtension =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".pdf"] = "application/pdf",
        };

    private static readonly HashSet<string> AllowedContentTypes =
        new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "application/pdf" };

    private readonly string _rootPath;
    private readonly ILogger<FamilyVaultStorage> _logger;

    public FamilyVaultStorage(IWebHostEnvironment environment, ILogger<FamilyVaultStorage> logger)
    {
        _rootPath = Path.Combine(environment.ContentRootPath, RelativeFolder);
        _logger = logger;
    }

    public async Task<Result<StoredFile>> SaveAsync(IFormFile file)
    {
        if (file.Length == 0)
        {
            return Result<StoredFile>.Failure(ErrorType.Validation, "The uploaded file is empty.");
        }

        if (file.Length > MaxBytes)
        {
            return Result<StoredFile>.Failure(ErrorType.Validation, "The attachment exceeds the 5 MB limit.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            return Result<StoredFile>.Failure(
                ErrorType.Validation, "Only JPG, PNG and PDF attachments are allowed.");
        }

        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            return Result<StoredFile>.Failure(
                ErrorType.Validation, "The attachment content type is not allowed.");
        }

        Directory.CreateDirectory(_rootPath);

        var storedName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var absolutePath = Path.Combine(_rootPath, storedName);

        await using (var stream = new FileStream(absolutePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Stored with forward slashes so the value is portable across platforms.
        var relativePath = $"{RelativeFolder}/{storedName}";
        var originalName = Path.GetFileName(file.FileName);
        return Result<StoredFile>.Success(
            new StoredFile(relativePath, originalName, file.ContentType, file.Length));
    }

    public void Delete(string? relativePath)
    {
        var absolutePath = ToSafeAbsolutePath(relativePath);
        if (absolutePath is null)
        {
            return;
        }

        try
        {
            if (File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
            }
        }
        catch (IOException ex)
        {
            // A failed cleanup should not fail the request; log and move on.
            _logger.LogWarning(ex, "Could not delete vault attachment at {Path}.", absolutePath);
        }
    }

    public VaultFile? Resolve(string? relativePath)
    {
        var absolutePath = ToSafeAbsolutePath(relativePath);
        if (absolutePath is null || !File.Exists(absolutePath))
        {
            return null;
        }

        var extension = Path.GetExtension(absolutePath);
        var contentType = ContentTypeByExtension.TryGetValue(extension, out var type)
            ? type
            : "application/octet-stream";

        return new VaultFile(absolutePath, contentType, Path.GetFileName(absolutePath));
    }

    /// <summary>
    /// Maps a stored relative path to an absolute path, guarding against traversal outside
    /// the vault folder. Returns null for anything missing or outside the folder.
    /// </summary>
    private string? ToSafeAbsolutePath(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        var fileName = Path.GetFileName(relativePath);
        if (string.IsNullOrEmpty(fileName))
        {
            return null;
        }

        var absolutePath = Path.GetFullPath(Path.Combine(_rootPath, fileName));
        var rootFull = Path.GetFullPath(_rootPath);

        return absolutePath.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase)
            ? absolutePath
            : null;
    }
}
