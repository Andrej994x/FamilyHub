using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.DTOs.Families;

public record FamilyResponse(
    Guid Id,
    string Name,
    string CreatedByUserId,
    DateTimeOffset CreatedAt,
    FamilyRole CurrentUserRole,
    int MemberCount);
