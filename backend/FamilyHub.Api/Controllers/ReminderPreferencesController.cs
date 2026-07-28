using FamilyHub.Api.DTOs.Reminders;
using FamilyHub.Api.Interfaces;
using FamilyHub.Api.Models.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/reminder-preferences")]
public class ReminderPreferencesController : ApiControllerBase
{
    private readonly IReminderPreferenceService _preferences;
    private readonly IValidator<UpdateReminderPreferenceRequest> _updateValidator;

    public ReminderPreferencesController(
        IReminderPreferenceService preferences,
        IValidator<UpdateReminderPreferenceRequest> updateValidator)
    {
        _preferences = preferences;
        _updateValidator = updateValidator;
    }

    /// <summary>All of the current user's effective reminder preferences (defaults where unset).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ReminderPreferenceResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll() =>
        Ok(await _preferences.GetAllAsync(CurrentUserId));

    /// <summary>The current user's effective preference for one category.</summary>
    [HttpGet("{category}")]
    [ProducesResponseType(typeof(ReminderPreferenceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Get(ReminderCategory category) =>
        Ok(await _preferences.GetAsync(CurrentUserId, category));

    /// <summary>Updates (or disables) the current user's preference for one category.</summary>
    [HttpPut("{category}")]
    [ProducesResponseType(typeof(ReminderPreferenceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(
        ReminderCategory category, [FromBody] UpdateReminderPreferenceRequest request)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return ToValidationProblem(validation);
        }

        return Ok(await _preferences.UpdateAsync(CurrentUserId, category, request));
    }

    /// <summary>Clears all of the current user's saved preferences, reverting to system defaults.</summary>
    [HttpPost("reset-defaults")]
    [ProducesResponseType(typeof(IReadOnlyList<ReminderPreferenceResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetDefaults()
    {
        await _preferences.ResetDefaultsAsync(CurrentUserId);
        return Ok(await _preferences.GetAllAsync(CurrentUserId));
    }
}
