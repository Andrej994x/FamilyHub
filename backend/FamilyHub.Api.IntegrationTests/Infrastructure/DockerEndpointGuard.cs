using System.Net;

namespace FamilyHub.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Checks, before any container starts, that Testcontainers talks to a Docker engine on this
/// machine and that the SQL Server endpoint it hands back is a loopback, non-default port.
/// </summary>
public static class DockerEndpointGuard
{
    /// <summary>The default SQL Server port. A test container is never reached through it.</summary>
    public const int DefaultSqlServerPort = 1433;

    public static void AssertLocalEngine(Uri? endpoint)
    {
        if (endpoint is null)
        {
            throw new InvalidOperationException(
                "Docker is required for the integration tests, but no Docker endpoint was found. " +
                "Start Docker Desktop (Linux containers) or the Docker engine. Tests never fall back to another SQL Server.");
        }

        var isLocal = endpoint.Scheme switch
        {
            "npipe" or "unix" => true,
            "tcp" or "http" or "https" => IsLoopback(endpoint.Host),
            _ => false,
        };

        if (!isLocal)
        {
            throw new InvalidOperationException(
                $"Refusing to run integration tests against the non-local Docker endpoint '{endpoint}'. " +
                "Use a local Docker engine.");
        }
    }

    public static void AssertLocalContainerEndpoint(string host, int mappedPort)
    {
        if (!IsLoopback(host))
        {
            throw new InvalidOperationException(
                $"Refusing to use test container host '{host}': only a loopback address on this machine is allowed.");
        }

        if (mappedPort == DefaultSqlServerPort)
        {
            throw new InvalidOperationException(
                $"Refusing to use port {DefaultSqlServerPort}: it could be a locally installed SQL Server. " +
                "Test containers must use a random mapped port.");
        }
    }

    private static bool IsLoopback(string host) =>
        string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
        (IPAddress.TryParse(host, out var address) && IPAddress.IsLoopback(address));
}
