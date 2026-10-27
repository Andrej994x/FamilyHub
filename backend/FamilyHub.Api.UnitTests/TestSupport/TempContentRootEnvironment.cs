using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace FamilyHub.Api.UnitTests.TestSupport;

/// <summary>
/// A web host environment rooted in a fresh temporary directory, deleted on dispose, so file
/// storage tests never touch the API project's real uploads folder.
/// </summary>
internal sealed class TempContentRootEnvironment : IWebHostEnvironment, IDisposable
{
    public TempContentRootEnvironment()
    {
        ContentRootPath = Path.Combine(Path.GetTempPath(), $"familyhub-unit-{Guid.NewGuid():N}");
        Directory.CreateDirectory(ContentRootPath);
        ContentRootFileProvider = new NullFileProvider();
        WebRootPath = Path.Combine(ContentRootPath, "wwwroot");
        WebRootFileProvider = new NullFileProvider();
    }

    public string ApplicationName { get; set; } = "FamilyHub.Api.UnitTests";

    public IFileProvider ContentRootFileProvider { get; set; }

    public string ContentRootPath { get; set; }

    public string EnvironmentName { get; set; } = "Testing";

    public IFileProvider WebRootFileProvider { get; set; }

    public string WebRootPath { get; set; }

    public void Dispose()
    {
        if (Directory.Exists(ContentRootPath))
        {
            Directory.Delete(ContentRootPath, recursive: true);
        }
    }
}
