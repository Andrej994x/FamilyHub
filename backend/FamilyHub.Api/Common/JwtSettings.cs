namespace FamilyHub.Api.Common;

/// <summary>
/// Strongly-typed JWT configuration bound from the "Jwt" section of appsettings.
/// </summary>
public class JwtSettings
{
    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 60;
}
