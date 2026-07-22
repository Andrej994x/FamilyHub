using FamilyHub.Api.DTOs.Invitations;
using FamilyHub.Api.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FamilyHub.Api.Controllers;

[ApiController]
[Authorize]
public class InvitationsController : ApiControllerBase
{
    private readonly IInvitationService _invitationService;
    private readonly IValidator<CreateInvitationRequest> _createValidator;

    public InvitationsController(
        IInvitationService invitationService,
        IValidator<CreateInvitationRequest> createValidator)
    {
        _invitationService = invitationService;
        _createValidator = createValidator;
    }

    [HttpPost("api/families/{familyId:guid}/invitations")]
    [ProducesResponseType(typeof(CreatedInvitationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(Guid familyId, [FromBody] CreateInvitationRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return ValidationProblem(ToModelState(validation));
        }

        var result = await _invitationService.CreateInvitationAsync(CurrentUserId, familyId, request);
        return result.Succeeded
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : Error(result);
    }

    [HttpGet("api/families/{familyId:guid}/invitations")]
    [ProducesResponseType(typeof(IReadOnlyList<InvitationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForFamily(Guid familyId) =>
        HandleResult(await _invitationService.GetInvitationsAsync(CurrentUserId, familyId));

    [HttpPost("api/invitations/{token}/accept")]
    [ProducesResponseType(typeof(InvitationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Accept(string token) =>
        HandleResult(await _invitationService.AcceptInvitationAsync(CurrentUserId, token));

    [HttpDelete("api/families/{familyId:guid}/invitations/{invitationId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid familyId, Guid invitationId)
    {
        var result = await _invitationService.CancelInvitationAsync(CurrentUserId, familyId, invitationId);
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
