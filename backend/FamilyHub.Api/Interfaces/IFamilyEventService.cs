using FamilyHub.Api.Common;
using FamilyHub.Api.DTOs.Events;

namespace FamilyHub.Api.Interfaces;

public interface IFamilyEventService
{
    Task<Result<EventResponse>> CreateEventAsync(string userId, Guid familyId, CreateEventRequest request);

    Task<Result<IReadOnlyList<EventResponse>>> GetEventsAsync(string userId, Guid familyId, EventFilter filter);

    Task<Result<EventResponse>> GetEventAsync(string userId, Guid familyId, Guid eventId);

    Task<Result<EventResponse>> UpdateEventAsync(string userId, Guid familyId, Guid eventId, UpdateEventRequest request);

    Task<Result> DeleteEventAsync(string userId, Guid familyId, Guid eventId);
}
