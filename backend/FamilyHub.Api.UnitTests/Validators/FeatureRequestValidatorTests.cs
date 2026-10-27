using FamilyHub.Api.DTOs.Children;
using FamilyHub.Api.DTOs.Invitations;
using FamilyHub.Api.DTOs.Pickups;
using FamilyHub.Api.DTOs.Reminders;
using FamilyHub.Api.DTOs.Tasks;
using FamilyHub.Api.Models.Enums;
using FamilyHub.Api.Validators;

namespace FamilyHub.Api.UnitTests.Validators;

public class FeatureRequestValidatorTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Task_requires_a_title(string title)
    {
        var result = new CreateTaskRequestValidator()
            .Validate(new CreateTaskRequest(title, null, null, null, TaskPriority.Medium));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTaskRequest.Title));
    }

    [Fact]
    public void Task_title_is_limited_to_200_characters()
    {
        var validator = new CreateTaskRequestValidator();

        Assert.True(validator.Validate(new CreateTaskRequest(new string('a', 200), null, null, null, TaskPriority.Low)).IsValid);
        Assert.False(validator.Validate(new CreateTaskRequest(new string('a', 201), null, null, null, TaskPriority.Low)).IsValid);
    }

    [Fact]
    public void Task_rejects_an_undefined_priority()
    {
        var result = new CreateTaskRequestValidator()
            .Validate(new CreateTaskRequest("Title", null, null, null, (TaskPriority)99));

        Assert.Contains(result.Errors, e =>
            e.PropertyName == nameof(CreateTaskRequest.Priority) && e.ErrorMessage == "A valid priority is required.");
    }

    [Fact]
    public void Task_status_rejects_an_undefined_value()
    {
        var validator = new UpdateTaskStatusRequestValidator();

        Assert.True(validator.Validate(new UpdateTaskStatusRequest(Models.Enums.TaskStatus.Completed)).IsValid);
        Assert.False(validator.Validate(new UpdateTaskStatusRequest((Models.Enums.TaskStatus)42)).IsValid);
    }

    [Fact]
    public void Child_date_of_birth_cannot_be_in_the_future()
    {
        var validator = new CreateChildRequestValidator();
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        Assert.False(validator.Validate(new CreateChildRequest("Mia", "Test", tomorrow, null)).IsValid);
        Assert.True(validator.Validate(new CreateChildRequest("Mia", "Test", today, null)).IsValid);
        Assert.True(validator.Validate(new CreateChildRequest("Mia", "Test", null, null)).IsValid);
    }

    [Fact]
    public void Invitation_cannot_grant_the_owner_role()
    {
        var validator = new CreateInvitationRequestValidator();

        var owner = validator.Validate(new CreateInvitationRequest("new@example.com", FamilyRole.Owner));
        var parent = validator.Validate(new CreateInvitationRequest("new@example.com", FamilyRole.Parent));

        Assert.Contains(owner.Errors, e => e.ErrorMessage == "A user cannot be invited as Owner.");
        Assert.True(parent.IsValid);
    }

    [Fact]
    public void Pickup_requires_a_child_and_an_assigned_member()
    {
        var result = new CreatePickupRequestValidator().Validate(
            new CreatePickupRequest(Guid.Empty, Guid.Empty, DateTimeOffset.UtcNow.AddDays(1), "School gate", null));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePickupRequest.ChildProfileId));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePickupRequest.AssignedMemberId));
    }

    [Theory]
    [InlineData(new int[0], "Provide at least one reminder offset (or disable the category).")]
    [InlineData(new[] { 7, 7 }, "Reminder offsets must not contain duplicates.")]
    [InlineData(new[] { -1 }, "Each offset must be between 0 and 365 days.")]
    [InlineData(new[] { 366 }, "Each offset must be between 0 and 365 days.")]
    [InlineData(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, "At most 10 reminder offsets are allowed.")]
    public void Reminder_offsets_are_bounded_unique_and_non_empty(int[] offsets, string expectedMessage)
    {
        var result = new UpdateReminderPreferenceRequestValidator()
            .Validate(new UpdateReminderPreferenceRequest(true, offsets));

        Assert.Contains(result.Errors, e => e.ErrorMessage == expectedMessage);
    }
}
