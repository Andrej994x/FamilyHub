using FamilyHub.Api.Common;
using FamilyHub.Api.DTOs.Shopping;

namespace FamilyHub.Api.Interfaces;

public interface IShoppingListService
{
    Task<Result<ShoppingListResponse>> CreateListAsync(string userId, Guid familyId, CreateShoppingListRequest request);

    Task<Result<IReadOnlyList<ShoppingListResponse>>> GetListsAsync(string userId, Guid familyId);

    Task<Result<ShoppingItemResponse>> AddItemAsync(string userId, Guid listId, CreateShoppingItemRequest request);

    Task<Result<ShoppingItemResponse>> UpdateItemAsync(string userId, Guid listId, Guid itemId, UpdateShoppingItemRequest request);

    Task<Result<ShoppingItemResponse>> ToggleItemAsync(string userId, Guid listId, Guid itemId);

    Task<Result> DeleteItemAsync(string userId, Guid listId, Guid itemId);

    Task<Result> ClearPurchasedItemsAsync(string userId, Guid listId);
}
