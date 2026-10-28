using FamilyHub.Api.DTOs.Expenses;
using FamilyHub.Api.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/families/{familyId:guid}/expense-categories")]
public class ExpenseCategoriesController : ApiControllerBase
{
    private readonly IExpenseCategoryService _service;
    private readonly IValidator<CreateExpenseCategoryRequest> _createValidator;
    private readonly IValidator<UpdateExpenseCategoryRequest> _updateValidator;

    public ExpenseCategoriesController(
        IExpenseCategoryService service,
        IValidator<CreateExpenseCategoryRequest> createValidator,
        IValidator<UpdateExpenseCategoryRequest> updateValidator)
    {
        _service = service;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ExpenseCategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAll(Guid familyId, [FromQuery] bool includeArchived = false) =>
        HandleResult(await _service.GetCategoriesAsync(CurrentUserId, familyId, includeArchived));

    [HttpGet("{categoryId:guid}")]
    [ProducesResponseType(typeof(ExpenseCategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid familyId, Guid categoryId) =>
        HandleResult(await _service.GetCategoryAsync(CurrentUserId, familyId, categoryId));

    [HttpPost]
    [ProducesResponseType(typeof(ExpenseCategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(Guid familyId, [FromBody] CreateExpenseCategoryRequest request)
    {
        var validation = await _createValidator.ValidateAsync(request);
        if (!validation.IsValid) return ToValidationProblem(validation);

        var result = await _service.CreateCategoryAsync(CurrentUserId, familyId, request);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { familyId, categoryId = result.Value!.Id }, result.Value)
            : Error(result);
    }

    [HttpPut("{categoryId:guid}")]
    [ProducesResponseType(typeof(ExpenseCategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid familyId, Guid categoryId, [FromBody] UpdateExpenseCategoryRequest request)
    {
        var validation = await _updateValidator.ValidateAsync(request);
        if (!validation.IsValid) return ToValidationProblem(validation);

        return HandleResult(await _service.UpdateCategoryAsync(CurrentUserId, familyId, categoryId, request));
    }

    [HttpDelete("{categoryId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid familyId, Guid categoryId)
    {
        var result = await _service.DeleteCategoryAsync(CurrentUserId, familyId, categoryId);
        return result.Succeeded ? NoContent() : Error(result);
    }
}
