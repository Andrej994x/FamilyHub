using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.DTOs.Shopping;

public record UpdateShoppingItemRequest(
    string Name,
    string? Quantity,
    ItemCategory Category);
