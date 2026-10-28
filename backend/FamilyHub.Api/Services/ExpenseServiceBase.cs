using FamilyHub.Api.Common;
using FamilyHub.Api.Data;
using FamilyHub.Api.Models;
using FamilyHub.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyHub.Api.Services;

/// <summary>
/// Shared behaviour for the Family Expenses services: family authorization (children have
/// no access) and seeding the default categories on first use.
/// </summary>
public abstract class ExpenseServiceBase
{
    protected const string ChildDeniedMessage = "Children do not have access to family expenses.";

    protected readonly AppDbContext Db;
    protected readonly ILogger Logger;

    protected ExpenseServiceBase(AppDbContext db, ILogger logger)
    {
        Db = db;
        Logger = logger;
    }

    /// <summary>
    /// Ensures the family exists, the user is a member and not a Child. When
    /// <paramref name="requireManage"/> is true, the member must be an Owner or Parent.
    /// Returns the caller's membership on success.
    /// </summary>
    protected async Task<(FamilyMember? Membership, ErrorType? Error, string? Message)> AuthorizeAsync(
        string userId, Guid familyId, bool requireManage)
    {
        var familyExists = await Db.Families.AnyAsync(f => f.Id == familyId);
        if (!familyExists)
        {
            return (null, ErrorType.NotFound, "Family not found.");
        }

        var membership = await Db.FamilyMembers
            .FirstOrDefaultAsync(m => m.FamilyId == familyId && m.UserId == userId);
        if (membership is null)
        {
            return (null, ErrorType.Forbidden, "You do not have access to this family.");
        }

        if (membership.Role == FamilyRole.Child)
        {
            return (null, ErrorType.Forbidden, ChildDeniedMessage);
        }

        if (requireManage && !CanManage(membership))
        {
            return (null, ErrorType.Forbidden, "Only an Owner or Parent can manage expense categories and budgets.");
        }

        return (membership, null, null);
    }

    protected static bool CanManage(FamilyMember membership) =>
        membership.Role is FamilyRole.Owner or FamilyRole.Parent;

    /// <summary>
    /// Creates the default categories the first time a family uses Expenses (no categories
    /// at all, archived ones included). Safe to call repeatedly and concurrently: the unique
    /// (FamilyId, Name) index rejects a second seeding, which is then ignored.
    /// </summary>
    protected async Task EnsureDefaultCategoriesAsync(Guid familyId)
    {
        if (await Db.ExpenseCategories.AnyAsync(c => c.FamilyId == familyId))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var defaults = ExpenseCategoryDefaults.All
            .Select((d, index) => new ExpenseCategory
            {
                Id = Guid.NewGuid(),
                FamilyId = familyId,
                Name = d.Name,
                DefaultCategory = d.Category,
                SortOrder = index,
                CreatedAt = now,
            })
            .ToList();

        Db.ExpenseCategories.AddRange(defaults);
        try
        {
            await Db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            foreach (var category in defaults)
            {
                Db.Entry(category).State = EntityState.Detached;
            }

            // Another request seeded the family first; anything else is a real failure.
            if (!await Db.ExpenseCategories.AnyAsync(c => c.FamilyId == familyId))
            {
                throw;
            }

            Logger.LogInformation("Default expense categories for family {FamilyId} were seeded concurrently", familyId);
        }
    }
}
