using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.DTOs.Shopping;

public record ShoppingItemResponse(
    Guid Id,
    Guid ShoppingListId,
    string Name,
    string? Quantity,
    ItemCategory Category,
    bool IsPurchased,
    bool IsImportant,
    string AddedByUserId,
    string? PurchasedByUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PurchasedAt);
