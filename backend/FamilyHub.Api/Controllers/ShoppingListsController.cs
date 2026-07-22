using FamilyHub.Api.DTOs.Shopping;
using FamilyHub.Api.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FamilyHub.Api.Controllers;

[ApiController]
[Authorize]
public class ShoppingListsController : ApiControllerBase
{
    private readonly IShoppingListService _shoppingService;
    private readonly IValidator<CreateShoppingListRequest> _listValidator;
    private readonly IValidator<CreateShoppingItemRequest> _createItemValidator;
    private readonly IValidator<UpdateShoppingItemRequest> _updateItemValidator;

    public ShoppingListsController(
        IShoppingListService shoppingService,
        IValidator<CreateShoppingListRequest> listValidator,
        IValidator<CreateShoppingItemRequest> createItemValidator,
        IValidator<UpdateShoppingItemRequest> updateItemValidator)
    {
        _shoppingService = shoppingService;
        _listValidator = listValidator;
        _createItemValidator = createItemValidator;
        _updateItemValidator = updateItemValidator;
    }

    // ---- Lists (family-scoped) ----

    [HttpPost("api/families/{familyId:guid}/shopping-lists")]
    [ProducesResponseType(typeof(ShoppingListResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateList(Guid familyId, [FromBody] CreateShoppingListRequest request)
    {
        var validation = await _listValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return ValidationProblem(ToModelState(validation));
        }

        var result = await _shoppingService.CreateListAsync(CurrentUserId, familyId, request);
        return result.Succeeded
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : Error(result);
    }

    [HttpGet("api/families/{familyId:guid}/shopping-lists")]
    [ProducesResponseType(typeof(IReadOnlyList<ShoppingListResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLists(Guid familyId) =>
        HandleResult(await _shoppingService.GetListsAsync(CurrentUserId, familyId));

    // ---- Items (list-scoped) ----

    [HttpPost("api/shopping-lists/{listId:guid}/items")]
    [ProducesResponseType(typeof(ShoppingItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddItem(Guid listId, [FromBody] CreateShoppingItemRequest request)
    {
        var validation = await _createItemValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return ValidationProblem(ToModelState(validation));
        }

        var result = await _shoppingService.AddItemAsync(CurrentUserId, listId, request);
        return result.Succeeded
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : Error(result);
    }

    [HttpPut("api/shopping-lists/{listId:guid}/items/{itemId:guid}")]
    [ProducesResponseType(typeof(ShoppingItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateItem(Guid listId, Guid itemId, [FromBody] UpdateShoppingItemRequest request)
    {
        var validation = await _updateItemValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return ValidationProblem(ToModelState(validation));
        }

        return HandleResult(await _shoppingService.UpdateItemAsync(CurrentUserId, listId, itemId, request));
    }

    [HttpPatch("api/shopping-lists/{listId:guid}/items/{itemId:guid}/toggle")]
    [ProducesResponseType(typeof(ShoppingItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleItem(Guid listId, Guid itemId) =>
        HandleResult(await _shoppingService.ToggleItemAsync(CurrentUserId, listId, itemId));

    [HttpDelete("api/shopping-lists/{listId:guid}/items/{itemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteItem(Guid listId, Guid itemId)
    {
        var result = await _shoppingService.DeleteItemAsync(CurrentUserId, listId, itemId);
        return result.Succeeded ? NoContent() : Error(result);
    }

    [HttpDelete("api/shopping-lists/{listId:guid}/purchased-items")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ClearPurchased(Guid listId)
    {
        var result = await _shoppingService.ClearPurchasedItemsAsync(CurrentUserId, listId);
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
