using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.DTOs.Families;

public record FamilyMemberResponse(
    Guid Id,
    string UserId,
    string FirstName,
    string LastName,
    string Email,
    FamilyRole Role,
    DateTimeOffset JoinedAt);
