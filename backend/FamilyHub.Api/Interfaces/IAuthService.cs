using FamilyHub.Api.Common;
using FamilyHub.Api.DTOs.Auth;

namespace FamilyHub.Api.Interfaces;

public interface IAuthService
{
    /// <summary>Registers a new user. Passwords are hashed by ASP.NET Core Identity.</summary>
    Task<AuthResult> RegisterAsync(RegisterRequest request);

    /// <summary>Validates credentials and returns a token; null when credentials are invalid.</summary>
    Task<AuthResponse?> LoginAsync(LoginRequest request);

    /// <summary>Returns the user for the given id; null when not found.</summary>
    Task<UserResponse?> GetUserByIdAsync(string userId);
}
