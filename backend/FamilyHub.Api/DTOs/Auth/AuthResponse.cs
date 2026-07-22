namespace FamilyHub.Api.DTOs.Auth;

/// <summary>
/// Returned after a successful login: the JWT access token, its expiry, and the user.
/// </summary>
public record AuthResponse(
    string Token,
    DateTimeOffset ExpiresAt,
    UserResponse User);
