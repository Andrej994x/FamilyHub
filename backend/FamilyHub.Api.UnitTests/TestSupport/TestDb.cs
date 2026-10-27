using FamilyHub.Api.Data;
using FamilyHub.Api.Models;
using FamilyHub.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyHub.Api.UnitTests.TestSupport;

/// <summary>
/// Creates an isolated EF Core InMemory <see cref="AppDbContext"/> per test. InMemory does not
/// enforce relational constraints or support ExecuteUpdate/ExecuteDelete, so service paths that
/// rely on those are covered by the SQL Server integration tests instead.
/// </summary>
internal static class TestDb
{
    public static AppDbContext Create() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"familyhub-unit-{Guid.NewGuid():N}")
            .Options);

    /// <summary>Adds a family with the given members (user id + role) and returns it with its members.</summary>
    public static async Task<(Family Family, FamilyMember[] Members)> AddFamilyAsync(
        AppDbContext db, params (string UserId, FamilyRole Role)[] members)
    {
        var family = new Family
        {
            Id = Guid.NewGuid(),
            Name = "Test family",
            CreatedByUserId = members.Length > 0 ? members[0].UserId : "creator",
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var memberships = members
            .Select(m => new FamilyMember
            {
                Id = Guid.NewGuid(),
                FamilyId = family.Id,
                UserId = m.UserId,
                Role = m.Role,
                JoinedAt = DateTimeOffset.UtcNow,
            })
            .ToArray();

        db.Families.Add(family);
        db.FamilyMembers.AddRange(memberships);
        await db.SaveChangesAsync();

        return (family, memberships);
    }
}
