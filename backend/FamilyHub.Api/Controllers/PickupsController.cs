using FamilyHub.Api.DTOs.Pickups;
using FamilyHub.Api.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FamilyHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/families/{familyId:guid}/pickups")]
public class PickupsController : ApiControllerBase
{
    private readonly IPickupScheduleService _pickupService;
    private readonly IValidator<CreatePickupRequest> _createValidator;
    private readonly IValidator<UpdatePickupRequest> _updateValidator;
    private readonly IValidator<UpdatePickupStatusRequest> _statusValidator;

    public PickupsController(
        IPickupScheduleService pickupService,
        IValidator<CreatePickupRequest> createValidator,
        IValidator<UpdatePickupRequest> updateValidator,
        IValidator<UpdatePickupStatusRequest> statusValidator)
    {
        _pickupService = pickupService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _statusValidator = statusValidator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(PickupResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(Guid familyId, [FromBody] CreatePickupRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return ValidationProblem(ToModelState(validation));
        }

        var result = await _pickupService.CreatePickupAsync(CurrentUserId, familyId, request);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { familyId, pickupId = result.Value!.Id }, result.Value)
            : Error(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PickupResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAll(Guid familyId) =>
        HandleResult(await _pickupService.GetPickupsAsync(CurrentUserId, familyId));

    [HttpGet("{pickupId:guid}")]
    [ProducesResponseType(typeof(PickupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid familyId, Guid pickupId) =>
        HandleResult(await _pickupService.GetPickupAsync(CurrentUserId, familyId, pickupId));

    [HttpPut("{pickupId:guid}")]
    [ProducesResponseType(typeof(PickupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid familyId, Guid pickupId, [FromBody] UpdatePickupRequest request)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return ValidationProblem(ToModelState(validation));
        }

        return HandleResult(await _pickupService.UpdatePickupAsync(CurrentUserId, familyId, pickupId, request));
    }

    [HttpPatch("{pickupId:guid}/status")]
    [ProducesResponseType(typeof(PickupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid familyId, Guid pickupId, [FromBody] UpdatePickupStatusRequest request)
    {
        var validation = await _statusValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return ValidationProblem(ToModelState(validation));
        }

        return HandleResult(await _pickupService.UpdateStatusAsync(CurrentUserId, familyId, pickupId, request));
    }

    [HttpPatch("{pickupId:guid}/take-over")]
    [ProducesResponseType(typeof(PickupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TakeOver(Guid familyId, Guid pickupId) =>
        HandleResult(await _pickupService.TakeOverAsync(CurrentUserId, familyId, pickupId));

    [HttpDelete("{pickupId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid familyId, Guid pickupId)
    {
        var result = await _pickupService.DeletePickupAsync(CurrentUserId, familyId, pickupId);
        return result.Succeeded ? NoContent() : Error(result);
    }

    private ModelStateDictionary ToModelState(FluentValidation.Results.ValidationResult validation)
    {
        foreach (var error in validation.Errors)
        {
            ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }

        return ModelState;
    }
}
