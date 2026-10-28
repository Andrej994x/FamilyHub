using System.Net;
using System.Net.Http.Json;
using FamilyHub.Api.Common;
using FamilyHub.Api.DTOs.Auth;
using FamilyHub.Api.DTOs.Invitations;
using FamilyHub.Api.DTOs.Tasks;
using FamilyHub.Api.IntegrationTests.Infrastructure;
using FamilyHub.Api.Models.Enums;
using Microsoft.AspNetCore.Http;

namespace FamilyHub.Api.IntegrationTests.Validation;

[Collection(ApiCollection.Name)]
public class InputValidationTests(FamilyHubApiFixture fixture)
{
    [Fact]
    public async Task Register_with_invalid_input_returns_a_validation_problem_per_field()
    {
        var response = await fixture.CreateClient().PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("", "Tester", "not-an-email", "short"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = (await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>())!;
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Contains("FirstName", problem.Errors.Keys);
        Assert.Contains("A valid email address is required.", problem.Errors["Email"]);
        Assert.Contains("Password must be at least 8 characters long.", problem.Errors["Password"]);
    }

    [Fact]
    public async Task Creating_a_task_without_a_title_is_rejected_and_nothing_is_saved()
    {
        var family = await fixture.CreateFamilyWithOwnerAsync();

        var response = await family.Owner.Client.PostAsJsonAsync($"/api/families/{family.Id}/tasks",
            new CreateTaskRequest("  ", null, null, null, TaskPriority.High));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = (await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>())!;
        Assert.Contains("Title is required.", problem.Errors["Title"]);

        var tasks = await family.Owner.Client.GetFromJsonAsync<List<TaskResponse>>($"/api/families/{family.Id}/tasks");
        Assert.Empty(tasks!);
    }

    [Fact]
    public async Task A_task_cannot_be_assigned_to_a_member_of_another_family()
    {
        var family = await fixture.CreateFamilyWithOwnerAsync();
        var other = await fixture.CreateFamilyWithOwnerAsync();
        var outsider = (await other.Owner.GetMembersAsync(other.Id)).Single();

        var response = await family.Owner.Client.PostAsJsonAsync($"/api/families/{family.Id}/tasks",
            new CreateTaskRequest("Sneaky", null, outsider.Id, null, TaskPriority.Low));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = (await response.Content.ReadFromJsonAsync<ErrorResponse>())!;
        Assert.Equal("The assigned member is not part of this family.", error.Message);
    }

    [Fact]
    public async Task An_invitation_cannot_grant_the_owner_role()
    {
        var family = await fixture.CreateFamilyWithOwnerAsync();

        var response = await family.Owner.Client.PostAsJsonAsync($"/api/families/{family.Id}/invitations",
            new CreateInvitationRequest($"{Guid.NewGuid():N}@test.familyhub", FamilyRole.Owner));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = (await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>())!;
        Assert.Contains("A user cannot be invited as Owner.", problem.Errors["Role"]);
    }

    [Fact]
    public async Task Reminder_offsets_out_of_range_are_rejected()
    {
        var user = await fixture.RegisterAndLoginAsync();

        var response = await user.Client.PutAsJsonAsync("/api/reminder-preferences/Tasks",
            new { isEnabled = true, reminderOffsetsDays = new[] { 400 } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = (await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>())!;
        Assert.Contains("Each offset must be between 0 and 365 days.", problem.Errors.SelectMany(e => e.Value));
    }

    [Fact]
    public async Task Null_reminder_offsets_are_rejected_before_the_validator_runs()
    {
        var user = await fixture.RegisterAndLoginAsync();

        // The FluentValidation rule itself throws on null (documented in docs/TESTING.md); the API is
        // protected because [ApiController] treats the non-nullable array as required and returns 400.
        var response = await user.Client.PutAsJsonAsync("/api/reminder-preferences/Tasks",
            new { isEnabled = true, reminderOffsetsDays = (int[]?)null });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
