using FamilyHub.Api.Common;
using FamilyHub.Api.Data;
using FamilyHub.Api.DTOs.Families;
using FamilyHub.Api.Interfaces;
using FamilyHub.Api.Models;
using FamilyHub.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyHub.Api.Services;

public class FamilyService : IFamilyService
{
    private readonly AppDbContext _db;

    public FamilyService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<FamilyResponse>> CreateFamilyAsync(string userId, CreateFamilyRequest request)
    {
        // MVP rule: a user can belong to only one family.
        var alreadyInFamily = await _db.FamilyMembers.AnyAsync(m => m.UserId == userId);
        if (alreadyInFamily)
        {
            return Result<FamilyResponse>.Failure(
                ErrorType.Conflict, "You already belong to a family.");
        }

        var now = DateTimeOffset.UtcNow;
        var family = new Family
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            CreatedByUserId = userId,
            CreatedAt = now
        };

        // The creator joins as the Owner.
        family.Members.Add(new FamilyMember
        {
            Id = Guid.NewGuid(),
            FamilyId = family.Id,
            UserId = userId,
            Role = FamilyRole.Owner,
            JoinedAt = now
        });

        _db.Families.Add(family);
        await _db.SaveChangesAsync();

        return Result<FamilyResponse>.Success(
            new FamilyResponse(family.Id, family.Name, family.CreatedByUserId, family.CreatedAt, FamilyRole.Owner, 1));
    }

    public async Task<Result<FamilyResponse>> GetCurrentFamilyAsync(string userId)
    {
        var membership = await _db.FamilyMembers
            .Include(m => m.Family)
            .FirstOrDefaultAsync(m => m.UserId == userId);

        if (membership?.Family is null)
        {
            return Result<FamilyResponse>.Failure(
                ErrorType.NotFound, "You do not belong to a family.");
        }

        var memberCount = await _db.FamilyMembers.CountAsync(m => m.FamilyId == membership.FamilyId);
        return Result<FamilyResponse>.Success(ToResponse(membership.Family, membership.Role, memberCount));
    }

    public async Task<Result<FamilyResponse>> GetFamilyByIdAsync(string userId, Guid familyId)
    {
        var family = await _db.Families.FirstOrDefaultAsync(f => f.Id == familyId);
        if (family is null)
        {
            return Result<FamilyResponse>.Failure(ErrorType.NotFound, "Family not found.");
        }

        var membership = await GetMembershipAsync(userId, familyId);
        if (membership is null)
        {
            return Result<FamilyResponse>.Failure(
                ErrorType.Forbidden, "You do not have access to this family.");
        }

        var memberCount = await _db.FamilyMembers.CountAsync(m => m.FamilyId == familyId);
        return Result<FamilyResponse>.Success(ToResponse(family, membership.Role, memberCount));
    }

    public async Task<Result<FamilyResponse>> UpdateFamilyAsync(string userId, Guid familyId, UpdateFamilyRequest request)
    {
        var family = await _db.Families.FirstOrDefaultAsync(f => f.Id == familyId);
        if (family is null)
        {
            return Result<FamilyResponse>.Failure(ErrorType.NotFound, "Family not found.");
        }

        var membership = await GetMembershipAsync(userId, familyId);
        if (membership is null)
        {
            return Result<FamilyResponse>.Failure(
                ErrorType.Forbidden, "You do not have access to this family.");
        }

        if (membership.Role != FamilyRole.Owner)
        {
            return Result<FamilyResponse>.Failure(
                ErrorType.Forbidden, "Only the family owner can rename the family.");
        }

        family.Name = request.Name.Trim();
        await _db.SaveChangesAsync();

        var memberCount = await _db.FamilyMembers.CountAsync(m => m.FamilyId == familyId);
        return Result<FamilyResponse>.Success(ToResponse(family, membership.Role, memberCount));
    }

    public async Task<Result<IReadOnlyList<FamilyMemberResponse>>> GetMembersAsync(string userId, Guid familyId)
    {
        var familyExists = await _db.Families.AnyAsync(f => f.Id == familyId);
        if (!familyExists)
        {
            return Result<IReadOnlyList<FamilyMemberResponse>>.Failure(ErrorType.NotFound, "Family not found.");
        }

        var membership = await GetMembershipAsync(userId, familyId);
        if (membership is null)
        {
            return Result<IReadOnlyList<FamilyMemberResponse>>.Failure(
                ErrorType.Forbidden, "You do not have access to this family.");
        }

        var members = await _db.FamilyMembers
            .Include(m => m.User)
            .Where(m => m.FamilyId == familyId)
            .OrderBy(m => m.JoinedAt)
            .Select(m => new FamilyMemberResponse(
                m.Id,
                m.UserId,
                m.User!.FirstName,
                m.User.LastName,
                m.User.Email ?? string.Empty,
                m.Role,
                m.JoinedAt))
            .ToListAsync();

        return Result<IReadOnlyList<FamilyMemberResponse>>.Success(members);
    }

    public async Task<Result> RemoveMemberAsync(string userId, Guid familyId, Guid memberId)
    {
        var familyExists = await _db.Families.AnyAsync(f => f.Id == familyId);
        if (!familyExists)
        {
            return Result.Failure(ErrorType.NotFound, "Family not found.");
        }

        var membership = await GetMembershipAsync(userId, familyId);
        if (membership is null)
        {
            return Result.Failure(ErrorType.Forbidden, "You do not have access to this family.");
        }

        if (membership.Role != FamilyRole.Owner)
        {
            return Result.Failure(ErrorType.Forbidden, "Only the family owner can remove members.");
        }

        var target = await _db.FamilyMembers
            .FirstOrDefaultAsync(m => m.Id == memberId && m.FamilyId == familyId);
        if (target is null)
        {
            return Result.Failure(ErrorType.NotFound, "Member not found in this family.");
        }

        if (target.Role == FamilyRole.Owner)
        {
            return Result.Failure(ErrorType.Validation, "The family owner cannot be removed.");
        }

        // Unassign any tasks/events pointing at this member first — those FKs use
        // NoAction, so the member row cannot be deleted while it is still referenced.
        await _db.FamilyTasks
            .Where(t => t.AssignedToMemberId == target.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.AssignedToMemberId, (Guid?)null));

        await _db.FamilyEvents
            .Where(e => e.AssignedMemberId == target.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.AssignedMemberId, (Guid?)null));

        _db.FamilyMembers.Remove(target);
        await _db.SaveChangesAsync();

        return Result.Success();
    }

    private Task<FamilyMember?> GetMembershipAsync(string userId, Guid familyId) =>
        _db.FamilyMembers.FirstOrDefaultAsync(m => m.FamilyId == familyId && m.UserId == userId);

    private static FamilyResponse ToResponse(Family family, FamilyRole currentUserRole, int memberCount) =>
        new(family.Id, family.Name, family.CreatedByUserId, family.CreatedAt, currentUserRole, memberCount);
}
