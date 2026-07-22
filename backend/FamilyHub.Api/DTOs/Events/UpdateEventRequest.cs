using FamilyHub.Api.Models.Enums;

namespace FamilyHub.Api.DTOs.Events;

public record UpdateEventRequest(
    string Title,
    string? Description,
    EventType EventType,
    DateTimeOffset StartDateTime,
    DateTimeOffset? EndDateTime,
    string? Location,
    Guid? AssignedMemberId,
    Guid? ChildProfileId);
