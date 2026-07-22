namespace FamilyHub.Api.DTOs.Pickups;

public record CreatePickupRequest(
    Guid ChildProfileId,
    Guid AssignedMemberId,
    DateTimeOffset PickupDateTime,
    string Location,
    string? Notes);
