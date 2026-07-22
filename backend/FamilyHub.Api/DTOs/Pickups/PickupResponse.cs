using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.DTOs.Pickups;

public record PickupResponse(
    Guid Id,
    Guid FamilyId,
    Guid ChildProfileId,
    Guid AssignedMemberId,
    DateTimeOffset PickupDateTime,
    string Location,
    string? Notes,
    PickupStatus Status,
    string CreatedByUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);
