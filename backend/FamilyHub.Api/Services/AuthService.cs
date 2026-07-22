using FamilyHub.Api.Common;
using FamilyHub.Api.DTOs.Auth;
using FamilyHub.Api.Interfaces;
using FamilyHub.Api.Models;
using Microsoft.AspNetCore.Identity;

namespace FamilyHub.Api.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;

    public AuthService(UserManager<ApplicationUser> userManager, ITokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            return AuthResult.Failure(new[] { "A user with this email already exists." });
        }

        var user = new ApplicationUser
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email,
            UserName = request.Email,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Identity hashes the password before persisting.
        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return AuthResult.Failure(result.Errors.Select(e => e.Description));
        }

        return AuthResult.Success(ToResponse(user));
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return null;
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            return null;
        }

        var token = _tokenService.CreateToken(user);
        return new AuthResponse(token.Token, token.ExpiresAt, ToResponse(user));
    }

    public async Task<UserResponse?> GetUserByIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user is null ? null : ToResponse(user);
    }

    private static UserResponse ToResponse(ApplicationUser user) =>
        new(user.Id, user.FirstName, user.LastName, user.Email ?? string.Empty, user.CreatedAt);
}
