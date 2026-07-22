namespace FamilyHub.Api.DTOs.Auth;

/// <summary>
/// Safe representation of a user. Never includes password or security-stamp fields.
/// </summary>
public record UserResponse(
    string Id,
    string FirstName,
    string LastName,
    string Email,
    DateTimeOffset CreatedAt);
