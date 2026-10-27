using FamilyHub.Api.Data;
using FamilyHub.Api.Interfaces;
using FamilyHub.Api.Models;
using FamilyHub.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace FamilyHub.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Hosts the real API pipeline in memory (TestServer) against the isolated container database.
/// </summary>
public sealed class FamilyHubApiFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// Not "Development": user-secrets are not loaded (so no real secrets), DbSeeder does not run
    /// and Swagger is off.
    /// </summary>
    public const string EnvironmentName = "Testing";

    private readonly string _connectionString;
    private readonly string _expectedDataSource;
    private readonly string _jwtKey;
    private readonly string _logDirectory =
        Path.Combine(Path.GetTempPath(), $"familyhub-integration-logs-{Guid.NewGuid():N}");

    public FamilyHubApiFactory(string connectionString, string expectedDataSource, string jwtKey)
    {
        TestDatabaseGuard.AssertIsolated(connectionString, expectedDataSource);
        _connectionString = connectionString;
        _expectedDataSource = expectedDataSource;
        _jwtKey = jwtKey;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);

        // Program.cs reads Jwt:Key before the host is built, so it is supplied as a host setting.
        builder.UseSetting("Jwt:Key", _jwtKey);

        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = _jwtKey,
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
                // Keep Serilog's rolling file out of the API project's logs/ folder.
                ["Serilog:WriteTo:1:Args:path"] = Path.Combine(_logDirectory, "familyhub-.log"),
                ["Email:Smtp:Host"] = string.Empty,
                ["Push:PublicKey"] = string.Empty,
                ["Push:PrivateKey"] = string.Empty,
            }));

        builder.ConfigureTestServices(services =>
        {
            // Replace the DbContext options outright, so no appsettings value, environment variable
            // or user-secret can redirect the API to another database.
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlServer(_connectionString));

            // The daily reminder pass is a background timer; it is not part of these tests.
            var reminderService = services.SingleOrDefault(d =>
                d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(ReminderBackgroundService));
            if (reminderService is not null)
            {
                services.Remove(reminderService);
            }

            // Nothing leaves the process, and invitation links (bearer tokens) are not logged.
            services.RemoveAll<IEmailSender>();
            services.AddScoped<IEmailSender, NoOpEmailSender>();
            services.RemoveAll<IPushSender>();
            services.AddScoped<IPushSender, NoOpPushSender>();
        });
    }

    /// <summary>
    /// Builds the host, re-checks the connection string the API's DbContext really uses, and only
    /// then applies the existing migrations to the isolated test database.
    /// </summary>
    public async Task MigrateIsolatedDatabaseAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        TestDatabaseGuard.AssertIsolated(db.Database.GetConnectionString(), _expectedDataSource);

        await db.Database.MigrateAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();

        try
        {
            if (Directory.Exists(_logDirectory))
            {
                Directory.Delete(_logDirectory, recursive: true);
            }
        }
        catch (IOException)
        {
            // A log file can still be held briefly by the sink; the temp folder is harmless.
        }
    }

    private sealed class NoOpEmailSender : IEmailSender
    {
        public Task SendEmailAsync(string toEmail, string subject, string htmlBody, string? textBody = null,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NoOpPushSender : IPushSender
    {
        public Task<PushSendOutcome> SendAsync(PushSubscription subscription, string payloadJson,
            CancellationToken cancellationToken = default) => Task.FromResult(PushSendOutcome.Sent);
    }
}
