namespace FamilyHub.Api.DTOs.Pickups;

/// <summary>
/// Full update of a pickup's content. Status is changed via the dedicated endpoints.
/// </summary>
public record UpdatePickupRequest(
    Guid ChildProfileId,
    Guid AssignedMemberId,
    DateTimeOffset PickupDateTime,
    string Location,
    string? Notes);
