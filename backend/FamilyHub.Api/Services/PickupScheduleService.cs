using FamilyHub.Api.Common;
using FamilyHub.Api.Data;
using FamilyHub.Api.DTOs.Pickups;
using FamilyHub.Api.Interfaces;
using FamilyHub.Api.Models;
using FamilyHub.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyHub.Api.Services;

public class PickupScheduleService : IPickupScheduleService
{
    private const string PickupsUrl = "/pickups";

    private readonly AppDbContext _db;
    private readonly INotificationService _notifications;

    public PickupScheduleService(AppDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task<Result<PickupResponse>> CreatePickupAsync(string userId, Guid familyId, CreatePickupRequest request)
    {
        var (membership, error, message) = await GetMembershipAsync(userId, familyId);
        if (membership is null)
        {
            return Result<PickupResponse>.Failure(error!.Value, message!);
        }

        if (membership.Role is not (FamilyRole.Owner or FamilyRole.Parent))
        {
            return Result<PickupResponse>.Failure(
                ErrorType.Forbidden, "Only an Owner or Parent can create a pickup.");
        }

        var referenceError = await ValidateReferencesAsync(familyId, request.ChildProfileId, request.AssignedMemberId);
        if (referenceError is not null)
        {
            return Result<PickupResponse>.Failure(ErrorType.Validation, referenceError);
        }

        var pickup = new PickupSchedule
        {
            Id = Guid.NewGuid(),
            FamilyId = familyId,
            ChildProfileId = request.ChildProfileId,
            AssignedMemberId = request.AssignedMemberId,
            PickupDateTime = request.PickupDateTime,
            Location = request.Location.Trim(),
            Notes = Clean(request.Notes),
            Status = PickupStatus.Pending,
            CreatedByUserId = userId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.PickupSchedules.Add(pickup);
        await _db.SaveChangesAsync();

        await NotifyAssignedMemberAsync(userId, pickup.AssignedMemberId, pickup.Id, pickup.Location);

        return Result<PickupResponse>.Success(ToResponse(pickup));
    }

    public async Task<Result<IReadOnlyList<PickupResponse>>> GetPickupsAsync(string userId, Guid familyId)
    {
        var (membership, error, message) = await GetMembershipAsync(userId, familyId);
        if (membership is null)
        {
            return Result<IReadOnlyList<PickupResponse>>.Failure(error!.Value, message!);
        }

        var pickups = await _db.PickupSchedules
            .Where(p => p.FamilyId == familyId)
            .OrderBy(p => p.PickupDateTime)
            .ToListAsync();

        return Result<IReadOnlyList<PickupResponse>>.Success(pickups.Select(ToResponse).ToList());
    }

    public async Task<Result<PickupResponse>> GetPickupAsync(string userId, Guid familyId, Guid pickupId)
    {
        var (membership, error, message) = await GetMembershipAsync(userId, familyId);
        if (membership is null)
        {
            return Result<PickupResponse>.Failure(error!.Value, message!);
        }

        var pickup = await GetPickupEntityAsync(familyId, pickupId);
        return pickup is null
            ? Result<PickupResponse>.Failure(ErrorType.NotFound, "Pickup not found in this family.")
            : Result<PickupResponse>.Success(ToResponse(pickup));
    }

    public async Task<Result<PickupResponse>> UpdatePickupAsync(
        string userId, Guid familyId, Guid pickupId, UpdatePickupRequest request)
    {
        var (membership, error, message) = await GetMembershipAsync(userId, familyId);
        if (membership is null)
        {
            return Result<PickupResponse>.Failure(error!.Value, message!);
        }

        if (membership.Role is not (FamilyRole.Owner or FamilyRole.Parent))
        {
            return Result<PickupResponse>.Failure(
                ErrorType.Forbidden, "Only an Owner or Parent can edit a pickup.");
        }

        var pickup = await GetPickupEntityAsync(familyId, pickupId);
        if (pickup is null)
        {
            return Result<PickupResponse>.Failure(ErrorType.NotFound, "Pickup not found in this family.");
        }

        var referenceError = await ValidateReferencesAsync(familyId, request.ChildProfileId, request.AssignedMemberId);
        if (referenceError is not null)
        {
            return Result<PickupResponse>.Failure(ErrorType.Validation, referenceError);
        }

        var previousAssignee = pickup.AssignedMemberId;

        pickup.ChildProfileId = request.ChildProfileId;
        pickup.AssignedMemberId = request.AssignedMemberId;
        pickup.PickupDateTime = request.PickupDateTime;
        pickup.Location = request.Location.Trim();
        pickup.Notes = Clean(request.Notes);

        await _db.SaveChangesAsync();

        // Notify only when the pickup is reassigned to a different member.
        if (pickup.AssignedMemberId != previousAssignee)
        {
            await NotifyAssignedMemberAsync(userId, familyId, pickup.AssignedMemberId, pickup.Location);
        }

        return Result<PickupResponse>.Success(ToResponse(pickup));
    }

    public async Task<Result<PickupResponse>> UpdateStatusAsync(
        string userId, Guid familyId, Guid pickupId, UpdatePickupStatusRequest request)
    {
        var (membership, error, message) = await GetMembershipAsync(userId, familyId);
        if (membership is null)
        {
            return Result<PickupResponse>.Failure(error!.Value, message!);
        }

        var pickup = await GetPickupEntityAsync(familyId, pickupId);
        if (pickup is null)
        {
            return Result<PickupResponse>.Failure(ErrorType.NotFound, "Pickup not found in this family.");
        }

        // Only the assigned member may confirm, reject or complete the pickup.
        if (pickup.AssignedMemberId != membership.Id)
        {
            return Result<PickupResponse>.Failure(
                ErrorType.Forbidden, "Only the assigned member can change the pickup status.");
        }

        pickup.Status = request.Status;
        pickup.CompletedAt = request.Status == PickupStatus.Completed ? DateTimeOffset.UtcNow : null;

        await _db.SaveChangesAsync();

        // Notify the pickup's creator when the assigned member rejects it.
        if (request.Status == PickupStatus.CannotAttend)
        {
            await _notifications.CreateForUserAsync(
                pickup.CreatedByUserId,
                NotificationType.Calendar,
                "Pickup rejected",
                $"The assigned member cannot attend the pickup at {pickup.Location}.",
                familyId,
                PickupsUrl,
                actorUserId: userId);
        }

        return Result<PickupResponse>.Success(ToResponse(pickup));
    }

    public async Task<Result<PickupResponse>> TakeOverAsync(string userId, Guid familyId, Guid pickupId)
    {
        var (membership, error, message) = await GetMembershipAsync(userId, familyId);
        if (membership is null)
        {
            return Result<PickupResponse>.Failure(error!.Value, message!);
        }

        var pickup = await GetPickupEntityAsync(familyId, pickupId);
        if (pickup is null)
        {
            return Result<PickupResponse>.Failure(ErrorType.NotFound, "Pickup not found in this family.");
        }

        // Only a rejected pickup can be taken over.
        if (pickup.Status != PickupStatus.CannotAttend)
        {
            return Result<PickupResponse>.Failure(
                ErrorType.Validation, "Only a rejected pickup can be taken over.");
        }

        // The taker must be another Owner/Parent (not the member who rejected it).
        if (membership.Role is not (FamilyRole.Owner or FamilyRole.Parent))
        {
            return Result<PickupResponse>.Failure(
                ErrorType.Forbidden, "Only an Owner or Parent can take over a pickup.");
        }

        if (pickup.AssignedMemberId == membership.Id)
        {
            return Result<PickupResponse>.Failure(
                ErrorType.Validation, "You cannot take over a pickup you are already assigned to.");
        }

        pickup.AssignedMemberId = membership.Id;
        pickup.Status = PickupStatus.Pending;
        pickup.CompletedAt = null;

        await _db.SaveChangesAsync();

        // Notify the pickup's creator that another member took it over.
        await _notifications.CreateForUserAsync(
            pickup.CreatedByUserId,
            NotificationType.Calendar,
            "Pickup taken over",
            $"Another member has taken over the pickup at {pickup.Location}.",
            familyId,
            PickupsUrl,
            actorUserId: userId);

        return Result<PickupResponse>.Success(ToResponse(pickup));
    }

    public async Task<Result> DeletePickupAsync(string userId, Guid familyId, Guid pickupId)
    {
        var (membership, error, message) = await GetMembershipAsync(userId, familyId);
        if (membership is null)
        {
            return Result.Failure(error!.Value, message!);
        }

        if (membership.Role is not (FamilyRole.Owner or FamilyRole.Parent))
        {
            return Result.Failure(ErrorType.Forbidden, "Only an Owner or Parent can delete a pickup.");
        }

        var pickup = await GetPickupEntityAsync(familyId, pickupId);
        if (pickup is null)
        {
            return Result.Failure(ErrorType.NotFound, "Pickup not found in this family.");
        }

        _db.PickupSchedules.Remove(pickup);
        await _db.SaveChangesAsync();

        return Result.Success();
    }

    private async Task<(FamilyMember? Membership, ErrorType? Error, string? Message)> GetMembershipAsync(
        string userId, Guid familyId)
    {
        var familyExists = await _db.Families.AnyAsync(f => f.Id == familyId);
        if (!familyExists)
        {
            return (null, ErrorType.NotFound, "Family not found.");
        }

        var membership = await _db.FamilyMembers
            .FirstOrDefaultAsync(m => m.FamilyId == familyId && m.UserId == userId);
        if (membership is null)
        {
            return (null, ErrorType.Forbidden, "You do not have access to this family.");
        }

        return (membership, null, null);
    }

    /// <summary>Ensures the child and assigned member both belong to the family.</summary>
    private async Task<string?> ValidateReferencesAsync(Guid familyId, Guid childId, Guid memberId)
    {
        if (!await _db.ChildProfiles.AnyAsync(c => c.Id == childId && c.FamilyId == familyId))
        {
            return "The selected child is not part of this family.";
        }

        if (!await _db.FamilyMembers.AnyAsync(m => m.Id == memberId && m.FamilyId == familyId))
        {
            return "The assigned member is not part of this family.";
        }

        return null;
    }

    private Task<PickupSchedule?> GetPickupEntityAsync(Guid familyId, Guid pickupId) =>
        _db.PickupSchedules.FirstOrDefaultAsync(p => p.Id == pickupId && p.FamilyId == familyId);

    /// <summary>Notifies the assigned member's user, unless they assigned the pickup to themselves.</summary>
    private async Task NotifyAssignedMemberAsync(string actorUserId, Guid familyId, Guid assignedMemberId, string location)
    {
        var assigneeUserId = await _db.FamilyMembers
            .Where(m => m.Id == assignedMemberId)
            .Select(m => m.UserId)
            .FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(assigneeUserId))
        {
            return;
        }

        await _notifications.CreateForUserAsync(
            assigneeUserId,
            NotificationType.Calendar,
            "Pickup assigned",
            $"You have been assigned a pickup at {location}.",
            familyId,
            PickupsUrl,
            actorUserId);
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static PickupResponse ToResponse(PickupSchedule p) =>
        new(p.Id, p.FamilyId, p.ChildProfileId, p.AssignedMemberId, p.PickupDateTime, p.Location,
            p.Notes, p.Status, p.CreatedByUserId, p.CreatedAt, p.CompletedAt);
}
