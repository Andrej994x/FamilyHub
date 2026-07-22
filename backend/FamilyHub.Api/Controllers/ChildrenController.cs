using FamilyHub.Api.DTOs.Children;
using FamilyHub.Api.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FamilyHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/families/{familyId:guid}/children")]
public class ChildrenController : ApiControllerBase
{
    private readonly IChildProfileService _childService;
    private readonly IValidator<CreateChildRequest> _createValidator;
    private readonly IValidator<UpdateChildRequest> _updateValidator;

    public ChildrenController(
        IChildProfileService childService,
        IValidator<CreateChildRequest> createValidator,
        IValidator<UpdateChildRequest> updateValidator)
    {
        _childService = childService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ChildResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(Guid familyId, [FromBody] CreateChildRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return ValidationProblem(ToModelState(validation));
        }

        var result = await _childService.CreateChildAsync(CurrentUserId, familyId, request);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { familyId, childId = result.Value!.Id }, result.Value)
            : Error(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ChildResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAll(Guid familyId) =>
        HandleResult(await _childService.GetChildrenAsync(CurrentUserId, familyId));

    [HttpGet("{childId:guid}")]
    [ProducesResponseType(typeof(ChildResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid familyId, Guid childId) =>
        HandleResult(await _childService.GetChildAsync(CurrentUserId, familyId, childId));

    [HttpPut("{childId:guid}")]
    [ProducesResponseType(typeof(ChildResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid familyId, Guid childId, [FromBody] UpdateChildRequest request)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return ValidationProblem(ToModelState(validation));
        }

        return HandleResult(await _childService.UpdateChildAsync(CurrentUserId, familyId, childId, request));
    }

    [HttpDelete("{childId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid familyId, Guid childId)
    {
        var result = await _childService.DeleteChildAsync(CurrentUserId, familyId, childId);
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
