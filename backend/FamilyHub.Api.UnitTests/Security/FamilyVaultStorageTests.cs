using System.Text;
using FamilyHub.Api.Common;
using FamilyHub.Api.Services;
using FamilyHub.Api.UnitTests.TestSupport;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace FamilyHub.Api.UnitTests.Security;

public sealed class FamilyVaultStorageTests : IDisposable
{
    private const long MaxBytes = 5 * 1024 * 1024;

    private readonly TempContentRootEnvironment _environment = new();
    private readonly FamilyVaultStorage _storage;

    public FamilyVaultStorageTests()
    {
        _storage = new FamilyVaultStorage(_environment, NullLogger<FamilyVaultStorage>.Instance);
    }

    public void Dispose() => _environment.Dispose();

    private static FormFile File(string fileName, string contentType, long? declaredLength = null, string content = "file-content")
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return new FormFile(new MemoryStream(bytes), 0, declaredLength ?? bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType,
        };
    }

    [Fact]
    public async Task Rejects_an_empty_file()
    {
        var result = await _storage.SaveAsync(File("scan.pdf", "application/pdf", declaredLength: 0));

        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Rejects_a_file_over_the_5_mb_limit()
    {
        var result = await _storage.SaveAsync(File("scan.pdf", "application/pdf", declaredLength: MaxBytes + 1));

        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal("The attachment exceeds the 5 MB limit.", result.Error);
    }

    [Theory]
    [InlineData("script.exe", "application/pdf")]
    [InlineData("page.html", "image/png")]
    [InlineData("no-extension", "image/png")]
    public async Task Rejects_disallowed_extensions(string fileName, string contentType)
    {
        var result = await _storage.SaveAsync(File(fileName, contentType));

        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Equal("Only JPG, PNG and PDF attachments are allowed.", result.Error);
    }

    [Fact]
    public async Task Rejects_a_disallowed_content_type()
    {
        var result = await _storage.SaveAsync(File("photo.png", "text/html"));

        Assert.Equal("The attachment content type is not allowed.", result.Error);
    }

    [Fact]
    public async Task Saves_an_allowed_file_under_a_random_name_inside_the_vault()
    {
        var result = await _storage.SaveAsync(File("Passport Scan.PDF", "application/pdf"));

        Assert.True(result.Succeeded);
        var stored = result.Value!;
        Assert.StartsWith("uploads/family-vault/", stored.RelativePath);
        Assert.EndsWith(".pdf", stored.RelativePath);
        Assert.DoesNotContain("Passport", stored.RelativePath);
        Assert.Equal("Passport Scan.PDF", stored.FileName);

        var resolved = _storage.Resolve(stored.RelativePath);
        Assert.NotNull(resolved);
        Assert.Equal("application/pdf", resolved.ContentType);
        Assert.StartsWith(Path.Combine(_environment.ContentRootPath, "uploads", "family-vault"), resolved.AbsolutePath);
    }

    [Theory]
    [InlineData("../../outside.txt")]
    [InlineData("uploads/family-vault/../../outside.txt")]
    public void Resolve_and_delete_never_reach_files_outside_the_vault(string traversalPath)
    {
        var outside = Path.Combine(_environment.ContentRootPath, "outside.txt");
        System.IO.File.WriteAllText(outside, "must survive");

        Assert.Null(_storage.Resolve(traversalPath));

        _storage.Delete(traversalPath);
        Assert.True(System.IO.File.Exists(outside));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_returns_null_for_missing_paths(string? path)
    {
        Assert.Null(_storage.Resolve(path));
    }
}
