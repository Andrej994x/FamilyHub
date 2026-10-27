using System.Text.Json;
using FamilyHub.Api.IntegrationTests.Infrastructure;

namespace FamilyHub.Api.IntegrationTests.Safety;

/// <summary>
/// Pure checks of the isolation guards. They need no Docker, so they also run (and must pass)
/// on machines where the container tests cannot start.
/// </summary>
public class TestDatabaseGuardTests
{
    private const string Container = "127.0.0.1,49153";
    private const string ValidDatabase = "FamilyHubTest_0123456789abcdef";

    private static string ContainerString(string database = ValidDatabase, string server = Container) =>
        $"Server={server};Database={database};User Id=sa;Password=placeholder;TrustServerCertificate=True";

    [Fact]
    public void Accepts_the_container_test_database()
    {
        TestDatabaseGuard.AssertIsolated(ContainerString(), Container);
    }

    [Theory]
    [InlineData("Server=(localdb)\\MSSQLLocalDB;Database=FamilyHubTest_x;Trusted_Connection=True")]
    [InlineData("Server=.;Database=FamilyHubTest_x;User Id=sa;Password=placeholder")]
    [InlineData("Server=(local);Database=FamilyHubTest_x;User Id=sa;Password=placeholder")]
    [InlineData("Server=localhost\\SQLEXPRESS;Database=FamilyHubTest_x;User Id=sa;Password=placeholder")]
    [InlineData("Server=np:\\\\.\\pipe\\sql\\query;Database=FamilyHubTest_x;User Id=sa;Password=placeholder")]
    [InlineData("Server=localhost,1433;Database=FamilyHubCi;User Id=sa;Password=placeholder")]
    [InlineData("Server=127.0.0.1,49154;Database=FamilyHubTest_x;User Id=sa;Password=placeholder")]
    [InlineData("Server=db.example.com,49153;Database=FamilyHubTest_x;User Id=sa;Password=placeholder")]
    public void Rejects_servers_other_than_the_container(string connectionString)
    {
        Assert.Throws<InvalidOperationException>(() => TestDatabaseGuard.AssertIsolated(connectionString, Container));
    }

    [Theory]
    [InlineData("FamilyHub")]
    [InlineData("FamilyHubCi")]
    [InlineData("master")]
    [InlineData("tempdb")]
    [InlineData("FamilyHubTest_")]
    [InlineData("SomethingElse")]
    public void Rejects_databases_without_the_test_prefix(string database)
    {
        Assert.Throws<InvalidOperationException>(() =>
            TestDatabaseGuard.AssertIsolated(ContainerString(database), Container));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a connection string")]
    public void Rejects_missing_or_unparseable_connection_strings(string? connectionString)
    {
        Assert.Throws<InvalidOperationException>(() => TestDatabaseGuard.AssertIsolated(connectionString, Container));
    }

    [Fact]
    public void Rejects_integrated_security_and_attached_files()
    {
        Assert.Throws<InvalidOperationException>(() => TestDatabaseGuard.AssertIsolated(
            $"Server={Container};Database={ValidDatabase};Integrated Security=True", Container));
        Assert.Throws<InvalidOperationException>(() => TestDatabaseGuard.AssertIsolated(
            ContainerString() + ";AttachDbFilename=C:\\data\\FamilyHub.mdf", Container));
    }

    [Fact]
    public void Rejects_the_connection_string_committed_in_appsettings()
    {
        var appSettings = FindApiFile("appsettings.json");
        using var json = JsonDocument.Parse(File.ReadAllText(appSettings));
        var committed = json.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString();

        Assert.Throws<InvalidOperationException>(() => TestDatabaseGuard.AssertIsolated(committed, Container));
    }

    [Theory]
    [InlineData("npipe://./pipe/docker_engine")]
    [InlineData("unix:///var/run/docker.sock")]
    [InlineData("tcp://localhost:2375")]
    [InlineData("tcp://127.0.0.1:2375")]
    public void Accepts_local_docker_endpoints(string endpoint)
    {
        DockerEndpointGuard.AssertLocalEngine(new Uri(endpoint));
    }

    [Theory]
    [InlineData("tcp://docker.example.com:2376")]
    [InlineData("tcp://10.0.0.5:2375")]
    [InlineData("ssh://user@host")]
    public void Rejects_remote_docker_endpoints(string endpoint)
    {
        Assert.Throws<InvalidOperationException>(() => DockerEndpointGuard.AssertLocalEngine(new Uri(endpoint)));
    }

    [Fact]
    public void Rejects_a_missing_docker_endpoint()
    {
        Assert.Throws<InvalidOperationException>(() => DockerEndpointGuard.AssertLocalEngine(null));
    }

    [Theory]
    [InlineData("db.example.com", 49153)]
    [InlineData("192.168.1.10", 49153)]
    [InlineData("127.0.0.1", 1433)]
    public void Rejects_non_loopback_or_default_port_container_endpoints(string host, int port)
    {
        Assert.Throws<InvalidOperationException>(() => DockerEndpointGuard.AssertLocalContainerEndpoint(host, port));
    }

    private static string FindApiFile(string fileName)
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "FamilyHub.Api", fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException($"Could not locate FamilyHub.Api/{fileName} above {AppContext.BaseDirectory}.");
    }
}
