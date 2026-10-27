using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FamilyHub.Api.Common;
using FamilyHub.Api.DTOs.Auth;
using FamilyHub.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;

namespace FamilyHub.Api.IntegrationTests.Auth;

[Collection(ApiCollection.Name)]
public class AuthenticationTests(FamilyHubApiFixture fixture)
{
    [Theory]
    [InlineData("GET", "/api/auth/me")]
    [InlineData("GET", "/api/families/current")]
    [InlineData("GET", "/api/families/7d7c5e0e-5a8f-4a50-9f2e-3f0f6c8b1a11/tasks")]
    [InlineData("POST", "/api/families")]
    [InlineData("GET", "/api/notifications")]
    public async Task Protected_endpoints_return_401_without_a_token(string method, string path)
    {
        var response = await fixture.CreateClient().SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_token_signed_with_another_key_is_rejected()
    {
        var user = await fixture.RegisterAndLoginAsync();
        var forged = MintToken(user.UserId, Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)), DateTime.UtcNow.AddMinutes(30));

        var response = await SendWithToken(forged, "/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task An_expired_token_is_rejected()
    {
        var user = await fixture.RegisterAndLoginAsync();
        var expired = MintToken(user.UserId, fixture.JwtKey, DateTime.UtcNow.AddSeconds(-5));

        var response = await SendWithToken(expired, "/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_login_and_me_return_the_same_user_without_secrets()
    {
        var user = await fixture.RegisterAndLoginAsync("Ana");

        var response = await user.Client.GetAsync("/api/auth/me");

        await response.EnsureStatusAsync(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var me = (await response.Content.ReadFromJsonAsync<UserResponse>())!;
        Assert.Equal(user.UserId, me.Id);
        Assert.Equal(user.Email, me.Email);
        Assert.Equal("Ana", me.FirstName);
        Assert.DoesNotContain("passwordHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_with_a_wrong_password_returns_401_with_an_error_body()
    {
        var user = await fixture.RegisterAndLoginAsync();

        var response = await fixture.CreateClient().PostAsJsonAsync("/api/auth/login",
            new LoginRequest(user.Email, "WrongPassword1"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var error = (await response.Content.ReadFromJsonAsync<ErrorResponse>())!;
        Assert.Equal(StatusCodes.Status401Unauthorized, error.Status);
        Assert.Equal("Invalid email or password.", error.Message);
    }

    [Fact]
    public async Task Registering_an_existing_email_is_rejected()
    {
        var user = await fixture.RegisterAndLoginAsync();

        var response = await fixture.CreateClient().PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("Copy", "Cat", user.Email, $"Tp{Guid.NewGuid():N}A1"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = (await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>())!;
        Assert.Contains("A user with this email already exists.", problem.Errors.SelectMany(e => e.Value));
    }

    private Task<HttpResponseMessage> SendWithToken(string token, string path)
    {
        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client.GetAsync(path);
    }

    private static string MintToken(string userId, string key, DateTime expires)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "FamilyHub.Api",
            audience: "FamilyHub.Client",
            claims: new[] { new Claim(JwtRegisteredClaimNames.Sub, userId) },
            notBefore: expires.AddMinutes(-60),
            expires: expires,
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
