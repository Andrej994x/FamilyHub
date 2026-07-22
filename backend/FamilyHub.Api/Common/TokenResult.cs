namespace FamilyHub.Api.Common;

/// <summary>
/// A generated JWT access token and its expiry.
/// </summary>
public record TokenResult(string Token, DateTimeOffset ExpiresAt);
