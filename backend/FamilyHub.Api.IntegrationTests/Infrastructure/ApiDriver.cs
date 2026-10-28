using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FamilyHub.Api.DTOs.Auth;
using FamilyHub.Api.DTOs.Families;
using FamilyHub.Api.DTOs.Invitations;
using FamilyHub.Api.DTOs.Tasks;
using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.IntegrationTests.Infrastructure;

/// <summary>A registered, signed-in user with an HttpClient that sends their bearer token.</summary>
public sealed record TestUser(HttpClient Client, string UserId, string Email, string Token);

/// <summary>A family set up for one test, with its owner and the family id.</summary>
public sealed record TestFamily(Guid Id, TestUser Owner);

/// <summary>
/// Drives the API over HTTP the way the SPA does. Every helper creates brand-new users and
/// families with unique emails, so tests never depend on each other's data or execution order.
/// </summary>
public static class ApiDriver
{
    public static async Task<TestUser> RegisterAndLoginAsync(this FamilyHubApiFixture fixture, string firstName = "Test")
    {
        var client = fixture.CreateClient();
        var email = $"{firstName.ToLowerInvariant()}-{Guid.NewGuid():N}@test.familyhub";
        // Generated per call; satisfies the Identity password policy.
        var password = $"Tp{Guid.NewGuid():N}A1";

        var register = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(firstName, "Tester", email, password));
        await register.EnsureStatusAsync(HttpStatusCode.Created);
        var user = (await register.Content.ReadFromJsonAsync<UserResponse>())!;

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        await login.EnsureStatusAsync(HttpStatusCode.OK);
        var auth = (await login.Content.ReadFromJsonAsync<AuthResponse>())!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return new TestUser(client, user.Id, email, auth.Token);
    }

    public static async Task<TestFamily> CreateFamilyWithOwnerAsync(this FamilyHubApiFixture fixture)
    {
        var owner = await fixture.RegisterAndLoginAsync("Owner");
        var response = await owner.Client.PostAsJsonAsync("/api/families", new CreateFamilyRequest("Test family"));
        await response.EnsureStatusAsync(HttpStatusCode.Created);
        var family = (await response.Content.ReadFromJsonAsync<FamilyResponse>())!;
        return new TestFamily(family.Id, owner);
    }

    /// <summary>Registers a new user and adds them with <paramref name="role"/> via the real invitation flow.</summary>
    public static async Task<TestUser> AddMemberAsync(this FamilyHubApiFixture fixture, TestFamily family, FamilyRole role)
    {
        var member = await fixture.RegisterAndLoginAsync(role.ToString());

        var invite = await family.Owner.Client.PostAsJsonAsync(
            $"/api/families/{family.Id}/invitations", new CreateInvitationRequest(member.Email, role));
        await invite.EnsureStatusAsync(HttpStatusCode.Created);
        var created = (await invite.Content.ReadFromJsonAsync<CreatedInvitationResponse>())!;

        var accept = await member.Client.PostAsync($"/api/invitations/{created.Token}/accept", content: null);
        await accept.EnsureStatusAsync(HttpStatusCode.OK);

        return member;
    }

    public static async Task<IReadOnlyList<FamilyMemberResponse>> GetMembersAsync(this TestUser user, Guid familyId)
    {
        var response = await user.Client.GetAsync($"/api/families/{familyId}/members");
        await response.EnsureStatusAsync(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<List<FamilyMemberResponse>>())!;
    }

    public static async Task<TaskResponse> CreateTaskAsync(this TestUser user, Guid familyId, string title = "Water the plants",
        Guid? assignee = null)
    {
        var response = await user.Client.PostAsJsonAsync($"/api/families/{familyId}/tasks",
            new CreateTaskRequest(title, null, assignee, null, TaskPriority.Medium));
        await response.EnsureStatusAsync(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<TaskResponse>())!;
    }

    /// <summary>Asserts the status code, including the response body in the failure message.</summary>
    public static async Task EnsureStatusAsync(this HttpResponseMessage response, HttpStatusCode expected)
    {
        if (response.StatusCode != expected)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"{response.RequestMessage?.Method} {response.RequestMessage?.RequestUri?.PathAndQuery}: " +
                        $"expected {(int)expected} {expected}, got {(int)response.StatusCode} {response.StatusCode}. Body: {body}");
        }
    }
}
