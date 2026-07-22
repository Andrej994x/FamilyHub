using FamilyHub.Api.DTOs.Events;
using FamilyHub.Api.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FamilyHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/families/{familyId:guid}/events")]
public class EventsController : ApiControllerBase
{
    private readonly IFamilyEventService _eventService;
    private readonly IValidator<CreateEventRequest> _createValidator;
    private readonly IValidator<UpdateEventRequest> _updateValidator;

    public EventsController(
        IFamilyEventService eventService,
        IValidator<CreateEventRequest> createValidator,
        IValidator<UpdateEventRequest> updateValidator)
    {
        _eventService = eventService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(Guid familyId, [FromBody] CreateEventRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return ValidationProblem(ToModelState(validation));
        }

        var result = await _eventService.CreateEventAsync(CurrentUserId, familyId, request);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { familyId, eventId = result.Value!.Id }, result.Value)
            : Error(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAll(Guid familyId, [FromQuery] EventFilter filter) =>
        HandleResult(await _eventService.GetEventsAsync(CurrentUserId, familyId, filter));

    [HttpGet("{eventId:guid}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid familyId, Guid eventId) =>
        HandleResult(await _eventService.GetEventAsync(CurrentUserId, familyId, eventId));

    [HttpPut("{eventId:guid}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid familyId, Guid eventId, [FromBody] UpdateEventRequest request)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return ValidationProblem(ToModelState(validation));
        }

        return HandleResult(await _eventService.UpdateEventAsync(CurrentUserId, familyId, eventId, request));
    }

    [HttpDelete("{eventId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid familyId, Guid eventId)
    {
        var result = await _eventService.DeleteEventAsync(CurrentUserId, familyId, eventId);
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
