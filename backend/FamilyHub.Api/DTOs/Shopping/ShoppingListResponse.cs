namespace FamilyHub.Api.DTOs.Shopping;

public record ShoppingListResponse(
    Guid Id,
    Guid FamilyId,
    string Name,
    DateTimeOffset CreatedAt,
    IReadOnlyList<ShoppingItemResponse> Items);
