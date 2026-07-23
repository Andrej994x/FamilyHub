using FamilyHub.Api.Common;
using FamilyHub.Api.Data;
using FamilyHub.Api.DTOs.Events;
using FamilyHub.Api.Interfaces;
using FamilyHub.Api.Models;
using FamilyHub.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyHub.Api.Services;

public class FamilyEventService : IFamilyEventService
{
    private const string CalendarUrl = "/calendar";

    private readonly AppDbContext _db;
    private readonly INotificationService _notifications;

    public FamilyEventService(AppDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task<Result<EventResponse>> CreateEventAsync(string userId, Guid familyId, CreateEventRequest request)
    {
        var access = await AuthorizeAsync(userId, familyId);
        if (access is not null)
        {
            return Result<EventResponse>.Failure(access.Value.Error, access.Value.Message);
        }

        var referenceError = await ValidateReferencesAsync(familyId, request.AssignedMemberId, request.ChildProfileId);
        if (referenceError is not null)
        {
            return Result<EventResponse>.Failure(ErrorType.Validation, referenceError);
        }

        var ev = new FamilyEvent
        {
            Id = Guid.NewGuid(),
            FamilyId = familyId,
            Title = request.Title.Trim(),
            Description = Clean(request.Description),
            EventType = request.EventType,
            StartDateTime = request.StartDateTime,
            EndDateTime = request.EndDateTime,
            Location = Clean(request.Location),
            AssignedMemberId = request.AssignedMemberId,
            ChildProfileId = request.ChildProfileId,
            CreatedByUserId = userId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.FamilyEvents.Add(ev);
        await _db.SaveChangesAsync();

        await NotifyEventCreatedAsync(userId, ev);

        return Result<EventResponse>.Success(ToResponse(ev));
    }

    /// <summary>
    /// Calendar rule: when an event names a participant (the assigned member) notify
    /// only that member; otherwise notify the whole family. The creator is never notified.
    /// </summary>
    private async Task NotifyEventCreatedAsync(string actorUserId, FamilyEvent ev)
    {
        var message = $"A new event \"{ev.Title}\" was scheduled for {ev.StartDateTime:yyyy-MM-dd HH:mm}.";

        if (ev.AssignedMemberId is Guid memberId)
        {
            var participantUserId = await _db.FamilyMembers
                .Where(m => m.Id == memberId)
                .Select(m => m.UserId)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrEmpty(participantUserId))
            {
                await _notifications.CreateForUserAsync(
                    participantUserId, NotificationType.Calendar, "New event for you", message,
                    ev.FamilyId, CalendarUrl, actorUserId);
            }

            return;
        }

        await _notifications.CreateForFamilyAsync(
            ev.FamilyId, NotificationType.Calendar, "New family event", message,
            CalendarUrl, actorUserId);
    }

    public async Task<Result<IReadOnlyList<EventResponse>>> GetEventsAsync(string userId, Guid familyId, EventFilter filter)
    {
        var access = await AuthorizeAsync(userId, familyId);
        if (access is not null)
        {
            return Result<IReadOnlyList<EventResponse>>.Failure(access.Value.Error, access.Value.Message);
        }

        // Server-side filtering — all predicates are translated to SQL.
        var query = _db.FamilyEvents.Where(e => e.FamilyId == familyId);

        if (filter.DateFrom is not null)
        {
            query = query.Where(e => e.StartDateTime >= filter.DateFrom);
        }

        if (filter.DateTo is not null)
        {
            query = query.Where(e => e.StartDateTime <= filter.DateTo);
        }

        if (filter.MemberId is not null)
        {
            query = query.Where(e => e.AssignedMemberId == filter.MemberId);
        }

        if (filter.ChildId is not null)
        {
            query = query.Where(e => e.ChildProfileId == filter.ChildId);
        }

        if (filter.EventType is not null)
        {
            query = query.Where(e => e.EventType == filter.EventType);
        }

        var events = await query
            .OrderBy(e => e.StartDateTime)
            .ToListAsync();

        return Result<IReadOnlyList<EventResponse>>.Success(events.Select(ToResponse).ToList());
    }

    public async Task<Result<EventResponse>> GetEventAsync(string userId, Guid familyId, Guid eventId)
    {
        var access = await AuthorizeAsync(userId, familyId);
        if (access is not null)
        {
            return Result<EventResponse>.Failure(access.Value.Error, access.Value.Message);
        }

        var ev = await _db.FamilyEvents.FirstOrDefaultAsync(e => e.Id == eventId && e.FamilyId == familyId);
        return ev is null
            ? Result<EventResponse>.Failure(ErrorType.NotFound, "Event not found in this family.")
            : Result<EventResponse>.Success(ToResponse(ev));
    }

    public async Task<Result<EventResponse>> UpdateEventAsync(
        string userId, Guid familyId, Guid eventId, UpdateEventRequest request)
    {
        var access = await AuthorizeAsync(userId, familyId);
        if (access is not null)
        {
            return Result<EventResponse>.Failure(access.Value.Error, access.Value.Message);
        }

        var ev = await _db.FamilyEvents.FirstOrDefaultAsync(e => e.Id == eventId && e.FamilyId == familyId);
        if (ev is null)
        {
            return Result<EventResponse>.Failure(ErrorType.NotFound, "Event not found in this family.");
        }

        var referenceError = await ValidateReferencesAsync(familyId, request.AssignedMemberId, request.ChildProfileId);
        if (referenceError is not null)
        {
            return Result<EventResponse>.Failure(ErrorType.Validation, referenceError);
        }

        ev.Title = request.Title.Trim();
        ev.Description = Clean(request.Description);
        ev.EventType = request.EventType;
        ev.StartDateTime = request.StartDateTime;
        ev.EndDateTime = request.EndDateTime;
        ev.Location = Clean(request.Location);
        ev.AssignedMemberId = request.AssignedMemberId;
        ev.ChildProfileId = request.ChildProfileId;

        await _db.SaveChangesAsync();

        return Result<EventResponse>.Success(ToResponse(ev));
    }

    public async Task<Result> DeleteEventAsync(string userId, Guid familyId, Guid eventId)
    {
        var access = await AuthorizeAsync(userId, familyId);
        if (access is not null)
        {
            return Result.Failure(access.Value.Error, access.Value.Message);
        }

        var ev = await _db.FamilyEvents.FirstOrDefaultAsync(e => e.Id == eventId && e.FamilyId == familyId);
        if (ev is null)
        {
            return Result.Failure(ErrorType.NotFound, "Event not found in this family.");
        }

        _db.FamilyEvents.Remove(ev);
        await _db.SaveChangesAsync();

        return Result.Success();
    }

    /// <summary>
    /// Returns an error tuple when the family is missing or the user is not a member;
    /// null when access is granted (any family member may manage events).
    /// </summary>
    private async Task<(ErrorType Error, string Message)?> AuthorizeAsync(string userId, Guid familyId)
    {
        var familyExists = await _db.Families.AnyAsync(f => f.Id == familyId);
        if (!familyExists)
        {
            return (ErrorType.NotFound, "Family not found.");
        }

        var isMember = await _db.FamilyMembers.AnyAsync(m => m.FamilyId == familyId && m.UserId == userId);
        if (!isMember)
        {
            return (ErrorType.Forbidden, "You do not have access to this family.");
        }

        return null;
    }

    /// <summary>Ensures any referenced member/child belongs to the family. Returns an error message or null.</summary>
    private async Task<string?> ValidateReferencesAsync(Guid familyId, Guid? memberId, Guid? childId)
    {
        if (memberId is Guid m &&
            !await _db.FamilyMembers.AnyAsync(x => x.Id == m && x.FamilyId == familyId))
        {
            return "The assigned member is not part of this family.";
        }

        if (childId is Guid c &&
            !await _db.ChildProfiles.AnyAsync(x => x.Id == c && x.FamilyId == familyId))
        {
            return "The referenced child is not part of this family.";
        }

        return null;
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static EventResponse ToResponse(FamilyEvent e) =>
        new(e.Id, e.FamilyId, e.Title, e.Description, e.EventType, e.StartDateTime, e.EndDateTime,
            e.Location, e.AssignedMemberId, e.ChildProfileId, e.CreatedByUserId, e.CreatedAt);
}
