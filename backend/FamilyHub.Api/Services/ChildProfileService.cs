using FamilyHub.Api.Common;
using FamilyHub.Api.Data;
using FamilyHub.Api.DTOs.Children;
using FamilyHub.Api.Interfaces;
using FamilyHub.Api.Models;
using FamilyHub.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyHub.Api.Services;

public class ChildProfileService : IChildProfileService
{
    private readonly AppDbContext _db;

    public ChildProfileService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<ChildResponse>> CreateChildAsync(string userId, Guid familyId, CreateChildRequest request)
    {
        var (_, error, message) = await AuthorizeAsync(userId, familyId, requireManage: true);
        if (error is not null)
        {
            return Result<ChildResponse>.Failure(error.Value, message!);
        }

        var child = new ChildProfile
        {
            Id = Guid.NewGuid(),
            FamilyId = familyId,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            DateOfBirth = request.DateOfBirth,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.ChildProfiles.Add(child);
        await _db.SaveChangesAsync();

        return Result<ChildResponse>.Success(ToResponse(child));
    }

    public async Task<Result<IReadOnlyList<ChildResponse>>> GetChildrenAsync(string userId, Guid familyId)
    {
        var (_, error, message) = await AuthorizeAsync(userId, familyId, requireManage: false);
        if (error is not null)
        {
            return Result<IReadOnlyList<ChildResponse>>.Failure(error.Value, message!);
        }

        var children = await _db.ChildProfiles
            .Where(c => c.FamilyId == familyId)
            .OrderBy(c => c.FirstName).ThenBy(c => c.LastName)
            .Select(c => new ChildResponse(
                c.Id, c.FamilyId, c.FirstName, c.LastName, c.DateOfBirth, c.Notes, c.CreatedAt))
            .ToListAsync();

        return Result<IReadOnlyList<ChildResponse>>.Success(children);
    }

    public async Task<Result<ChildResponse>> GetChildAsync(string userId, Guid familyId, Guid childId)
    {
        var (_, error, message) = await AuthorizeAsync(userId, familyId, requireManage: false);
        if (error is not null)
        {
            return Result<ChildResponse>.Failure(error.Value, message!);
        }

        var child = await _db.ChildProfiles
            .FirstOrDefaultAsync(c => c.Id == childId && c.FamilyId == familyId);
        if (child is null)
        {
            return Result<ChildResponse>.Failure(ErrorType.NotFound, "Child profile not found in this family.");
        }

        return Result<ChildResponse>.Success(ToResponse(child));
    }

    public async Task<Result<ChildResponse>> UpdateChildAsync(
        string userId, Guid familyId, Guid childId, UpdateChildRequest request)
    {
        var (_, error, message) = await AuthorizeAsync(userId, familyId, requireManage: true);
        if (error is not null)
        {
            return Result<ChildResponse>.Failure(error.Value, message!);
        }

        var child = await _db.ChildProfiles
            .FirstOrDefaultAsync(c => c.Id == childId && c.FamilyId == familyId);
        if (child is null)
        {
            return Result<ChildResponse>.Failure(ErrorType.NotFound, "Child profile not found in this family.");
        }

        child.FirstName = request.FirstName.Trim();
        child.LastName = request.LastName.Trim();
        child.DateOfBirth = request.DateOfBirth;
        child.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

        await _db.SaveChangesAsync();

        return Result<ChildResponse>.Success(ToResponse(child));
    }

    public async Task<Result> DeleteChildAsync(string userId, Guid familyId, Guid childId)
    {
        var (_, error, message) = await AuthorizeAsync(userId, familyId, requireManage: true);
        if (error is not null)
        {
            return Result.Failure(error.Value, message!);
        }

        var child = await _db.ChildProfiles
            .FirstOrDefaultAsync(c => c.Id == childId && c.FamilyId == familyId);
        if (child is null)
        {
            return Result.Failure(ErrorType.NotFound, "Child profile not found in this family.");
        }

        // Clear this child from any events first — the event FK uses NoAction, so the
        // child row cannot be deleted while it is still referenced.
        await _db.FamilyEvents
            .Where(e => e.ChildProfileId == child.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.ChildProfileId, (Guid?)null));

        // Pickups require a child (non-nullable FK, NoAction), so remove any pickups
        // for this child before deleting it.
        await _db.PickupSchedules
            .Where(p => p.ChildProfileId == child.Id)
            .ExecuteDeleteAsync();

        // Vault documents for this child (ChildProfileId FK, NoAction) must go too.
        await _db.FamilyDocuments
            .Where(d => d.ChildProfileId == child.Id)
            .ExecuteDeleteAsync();

        _db.ChildProfiles.Remove(child);
        await _db.SaveChangesAsync();

        return Result.Success();
    }

    /// <summary>
    /// Ensures the family exists and the user is a member. When <paramref name="requireManage"/>
    /// is true, the member must be an Owner or Parent.
    /// </summary>
    private async Task<(FamilyMember? Membership, ErrorType? Error, string? Message)> AuthorizeAsync(
        string userId, Guid familyId, bool requireManage)
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

        if (requireManage && membership.Role is not (FamilyRole.Owner or FamilyRole.Parent))
        {
            return (null, ErrorType.Forbidden, "Only an Owner or Parent can manage child profiles.");
        }

        return (membership, null, null);
    }

    private static ChildResponse ToResponse(ChildProfile c) =>
        new(c.Id, c.FamilyId, c.FirstName, c.LastName, c.DateOfBirth, c.Notes, c.CreatedAt);
}
