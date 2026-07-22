using FamilyHub.Api.DTOs.Families;
using FamilyHub.Api.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FamilyHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/families")]
public class FamiliesController : ApiControllerBase
{
    private readonly IFamilyService _familyService;
    private readonly IValidator<CreateFamilyRequest> _createValidator;
    private readonly IValidator<UpdateFamilyRequest> _updateValidator;

    public FamiliesController(
        IFamilyService familyService,
        IValidator<CreateFamilyRequest> createValidator,
        IValidator<UpdateFamilyRequest> updateValidator)
    {
        _familyService = familyService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(FamilyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateFamilyRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return ValidationProblem(ToModelState(validation));
        }

        var result = await _familyService.CreateFamilyAsync(CurrentUserId, request);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { familyId = result.Value!.Id }, result.Value)
            : Error(result);
    }

    [HttpGet("current")]
    [ProducesResponseType(typeof(FamilyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrent() =>
        HandleResult(await _familyService.GetCurrentFamilyAsync(CurrentUserId));

    [HttpGet("{familyId:guid}")]
    [ProducesResponseType(typeof(FamilyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid familyId) =>
        HandleResult(await _familyService.GetFamilyByIdAsync(CurrentUserId, familyId));

    [HttpPut("{familyId:guid}")]
    [ProducesResponseType(typeof(FamilyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid familyId, [FromBody] UpdateFamilyRequest request)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return ValidationProblem(ToModelState(validation));
        }

        return HandleResult(await _familyService.UpdateFamilyAsync(CurrentUserId, familyId, request));
    }

    [HttpGet("{familyId:guid}/members")]
    [ProducesResponseType(typeof(IReadOnlyList<FamilyMemberResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMembers(Guid familyId) =>
        HandleResult(await _familyService.GetMembersAsync(CurrentUserId, familyId));

    [HttpDelete("{familyId:guid}/members/{memberId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMember(Guid familyId, Guid memberId)
    {
        var result = await _familyService.RemoveMemberAsync(CurrentUserId, familyId, memberId);
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
