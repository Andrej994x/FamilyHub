using FamilyHub.Api.DTOs.Auth;

namespace FamilyHub.Api.Common;

/// <summary>
/// Outcome of a registration attempt. Carries the created user on success,
/// or a list of human-readable errors on failure.
/// </summary>
public record AuthResult(bool Succeeded, UserResponse? User, IReadOnlyList<string> Errors)
{
    public static AuthResult Success(UserResponse user) =>
        new(true, user, Array.Empty<string>());

    public static AuthResult Failure(IEnumerable<string> errors) =>
        new(false, null, errors.ToArray());
}
