using System.Security.Cryptography;
using DotNet.Testcontainers.Configurations;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace FamilyHub.Api.IntegrationTests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<FamilyHubApiFixture>
{
    public const string Name = "FamilyHub API on a disposable SQL Server";
}

/// <summary>
/// One disposable SQL Server container and one in-memory API host for the whole test run.
/// Order of operations (each step must pass before the next):
///   1. the Docker endpoint is local;
///   2. the container starts from a pinned SQL Server image with a random per-run password;
///   3. its endpoint is a loopback host on a random (non-1433) port;
///   4. the server answering on that endpoint is the container (machine name = container id);
///   5. the connection string passes <see cref="TestDatabaseGuard"/>;
///   6. the API host is built, its DbContext connection string passes the guard again, and only
///      then are the existing EF Core migrations applied to the new FamilyHubTest_* database.
/// The container (and with it the database) is removed on dispose, and by the Testcontainers
/// resource reaper if the test process dies.
/// </summary>
public sealed class FamilyHubApiFixture : IAsyncLifetime
{
    /// <summary>Same tag as SQL_SERVER_IMAGE in .github/workflows/ci.yml.</summary>
    public const string DefaultSqlServerImage = "mcr.microsoft.com/mssql/server:2022-CU27-ubuntu-22.04";

    private const string AllowedImagePrefix = "mcr.microsoft.com/mssql/server:";

    private MsSqlContainer? _container;
    private FamilyHubApiFactory? _factory;

    public string ConnectionString { get; private set; } = string.Empty;

    /// <summary>The container's <c>host,mappedPort</c>; the only data source tests may use.</summary>
    public string ExpectedDataSource { get; private set; } = string.Empty;

    /// <summary>The container id; SQL Server inside the container reports its prefix as machine name.</summary>
    public string ContainerId { get; private set; } = string.Empty;

    /// <summary>A throwaway per-run JWT signing key, so tests can mint tokens (e.g. expired ones).</summary>
    public string JwtKey { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

    public FamilyHubApiFactory Factory =>
        _factory ?? throw new InvalidOperationException("The API fixture is not initialized.");

    public HttpClient CreateClient() => Factory.CreateClient();

    public async ValueTask InitializeAsync()
    {
        DockerEndpointGuard.AssertLocalEngine(TestcontainersSettings.OS.DockerEndpointAuthConfig?.Endpoint);

        var image = Environment.GetEnvironmentVariable("FAMILYHUB_TEST_SQL_IMAGE");
        image = string.IsNullOrWhiteSpace(image) ? DefaultSqlServerImage : image.Trim();
        if (!image.StartsWith(AllowedImagePrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"FAMILYHUB_TEST_SQL_IMAGE must be a '{AllowedImagePrefix}*' image, but was '{image}'.");
        }

        // Generated per run, satisfies SQL Server's complexity policy, never written anywhere.
        var saPassword = $"Tc1!{Convert.ToHexString(RandomNumberGenerator.GetBytes(16))}";

        _container = new MsSqlBuilder(image)
            .WithPassword(saPassword)
            .WithLabel("org.familyhub.purpose", "integration-tests")
            .Build();

        try
        {
            await _container.StartAsync();
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                "Could not start the disposable SQL Server container. Docker (Linux containers) is required " +
                "for the integration tests; they never fall back to a local or shared SQL Server.", ex);
        }

        var host = _container.Hostname;
        var port = _container.GetMappedPublicPort(MsSqlBuilder.MsSqlPort);
        DockerEndpointGuard.AssertLocalContainerEndpoint(host, port);

        ContainerId = _container.Id;
        ExpectedDataSource = $"{host},{port}";

        var builder = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            DataSource = ExpectedDataSource,
            InitialCatalog = $"{TestDatabaseGuard.DatabasePrefix}{Guid.NewGuid():N}",
            IntegratedSecurity = false,
            TrustServerCertificate = true,
        };
        ConnectionString = builder.ConnectionString;

        TestDatabaseGuard.AssertIsolated(ConnectionString, ExpectedDataSource);
        await AssertServerIsTheContainerAsync(builder);

        _factory = new FamilyHubApiFactory(ConnectionString, ExpectedDataSource, JwtKey);
        await _factory.MigrateIsolatedDatabaseAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    /// <summary>
    /// Read-only identity check on the container's master database (the test database does not
    /// exist yet): the server must report the container's own host name and run on Linux.
    /// </summary>
    private async Task AssertServerIsTheContainerAsync(SqlConnectionStringBuilder testDatabase)
    {
        var master = new SqlConnectionStringBuilder(testDatabase.ConnectionString) { InitialCatalog = "master" };

        await using var connection = new SqlConnection(master.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(
            "SELECT CAST(SERVERPROPERTY('MachineName') AS nvarchar(128)), " +
            "(SELECT TOP (1) host_platform FROM sys.dm_os_host_info)", connection);
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();

        var machineName = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
        var platform = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);

        if (!ContainerId.StartsWith(machineName, StringComparison.OrdinalIgnoreCase) || machineName.Length < 12 ||
            !string.Equals(platform, "Linux", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"The SQL Server on {ExpectedDataSource} reports machine '{machineName}' ({platform}), which is not " +
                $"test container {ContainerId[..12]}. Refusing to continue.");
        }
    }
}
