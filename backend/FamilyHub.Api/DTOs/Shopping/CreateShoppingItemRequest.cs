using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.DTOs.Shopping;

public record CreateShoppingItemRequest(
    string Name,
    string? Quantity,
    ItemCategory Category);
