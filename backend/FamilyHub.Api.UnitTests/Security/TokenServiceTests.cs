using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FamilyHub.Api.Common;
using FamilyHub.Api.Models;
using FamilyHub.Api.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FamilyHub.Api.UnitTests.Security;

public class TokenServiceTests
{
    // A throwaway key generated per test run; never a real secret.
    private static readonly string SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

    private static readonly JwtSettings Settings = new()
    {
        Issuer = "FamilyHub.Api",
        Audience = "FamilyHub.Client",
        Key = SigningKey,
        ExpiryMinutes = 30,
    };

    private static readonly ApplicationUser User = new()
    {
        Id = "user-123",
        Email = "ana@example.com",
        FirstName = "Ana",
        LastName = "Petrova",
    };

    private static TokenService CreateService() => new(Options.Create(Settings));

    /// <summary>The same validation rules the API registers in Program.cs.</summary>
    private static TokenValidationParameters ValidationParameters(string key) => new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = Settings.Issuer,
        ValidAudience = Settings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        ClockSkew = TimeSpan.Zero,
    };

    [Fact]
    public void Token_carries_the_user_identity_claims()
    {
        var token = new JwtSecurityTokenHandler().ReadJwtToken(CreateService().CreateToken(User).Token);

        Assert.Equal(User.Id, token.Subject);
        Assert.Equal(User.Email, token.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal("Ana", token.Claims.Single(c => c.Type == "firstName").Value);
        Assert.Equal("Petrova", token.Claims.Single(c => c.Type == "lastName").Value);
        Assert.False(string.IsNullOrEmpty(token.Id));
        Assert.Equal(SecurityAlgorithms.HmacSha256, token.Header.Alg);
    }

    [Fact]
    public void Each_token_gets_a_unique_jti()
    {
        var handler = new JwtSecurityTokenHandler();
        var first = handler.ReadJwtToken(CreateService().CreateToken(User).Token);
        var second = handler.ReadJwtToken(CreateService().CreateToken(User).Token);

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void Expiry_follows_the_configured_lifetime()
    {
        var before = DateTimeOffset.UtcNow;
        var result = CreateService().CreateToken(User);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);

        Assert.InRange(result.ExpiresAt, before.AddMinutes(30).AddSeconds(-1), DateTimeOffset.UtcNow.AddMinutes(30).AddSeconds(1));
        // The JWT 'exp' claim has second precision.
        Assert.Equal(result.ExpiresAt.ToUnixTimeSeconds(), new DateTimeOffset(token.ValidTo).ToUnixTimeSeconds());
    }

    [Fact]
    public void Token_validates_with_the_api_rules_and_maps_sub_to_the_user_id()
    {
        var principal = new JwtSecurityTokenHandler()
            .ValidateToken(CreateService().CreateToken(User).Token, ValidationParameters(SigningKey), out _);

        // ApiControllerBase.CurrentUserId reads NameIdentifier, which the handler maps from 'sub'.
        Assert.Equal(User.Id, principal.FindFirstValue(ClaimTypes.NameIdentifier));
    }

    [Fact]
    public void Token_is_rejected_when_signed_with_a_different_key()
    {
        var token = CreateService().CreateToken(User).Token;
        var otherKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

        Assert.ThrowsAny<SecurityTokenException>(() =>
            new JwtSecurityTokenHandler().ValidateToken(token, ValidationParameters(otherKey), out _));
    }
}
