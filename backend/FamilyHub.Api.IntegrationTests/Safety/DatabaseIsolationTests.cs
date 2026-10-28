using FamilyHub.Api.Data;
using FamilyHub.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FamilyHub.Api.IntegrationTests.Safety;

/// <summary>Evidence, from inside the running API host, that it only ever talks to the test container.</summary>
[Collection(ApiCollection.Name)]
public class DatabaseIsolationTests(FamilyHubApiFixture fixture, ITestOutputHelper output)
{
    [Fact]
    public async Task The_api_uses_the_disposable_container_database()
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var connectionString = db.Database.GetConnectionString();

        TestDatabaseGuard.AssertIsolated(connectionString, fixture.ExpectedDataSource);
        var builder = new SqlConnectionStringBuilder(connectionString);
        Assert.Equal(fixture.ExpectedDataSource, builder.DataSource);
        Assert.False(builder.IntegratedSecurity);

        var identity = await db.Database
            .SqlQueryRaw<ServerIdentity>(
                "SELECT DB_NAME() AS DatabaseName, CAST(SERVERPROPERTY('MachineName') AS nvarchar(128)) AS MachineName")
            .SingleAsync();

        Assert.Equal(builder.InitialCatalog, identity.DatabaseName);
        Assert.StartsWith(identity.MachineName, fixture.ContainerId, StringComparison.OrdinalIgnoreCase);

        // Printed (without credentials) as verification evidence in the test log.
        output.WriteLine($"Data source: {builder.DataSource}; database: {identity.DatabaseName}; " +
                         $"server machine: {identity.MachineName}; container: {fixture.ContainerId[..12]}");
    }

    [Fact]
    public async Task All_existing_migrations_are_applied_and_no_development_seed_data_exists()
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.Equal(db.Database.GetMigrations(), await db.Database.GetAppliedMigrationsAsync());
        // DbSeeder only runs in Development; its sample family and users must not be here.
        Assert.False(await db.Users.AnyAsync(u => u.Email == DbSeeder.Parent1Email || u.Email == DbSeeder.Parent2Email));
    }

    [Fact]
    public void The_host_runs_in_the_testing_environment()
    {
        var environment = fixture.Factory.Services.GetRequiredService<IWebHostEnvironment>();

        Assert.Equal(FamilyHubApiFactory.EnvironmentName, environment.EnvironmentName);
        Assert.False(environment.IsDevelopment());
    }

    private sealed record ServerIdentity(string DatabaseName, string MachineName);
}
