using FamilyHub.Api.Common;
using FamilyHub.Api.Data;
using FamilyHub.Api.Models;
using FamilyHub.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

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
    /// Locks the family's category rows until the returned transaction ends. Category archive,
    /// restore, rename and delete, budget changes, and expense create and update all take this
    /// lock first, so for one family they run one after another and each sees the committed
    /// result of the previous one (the "at least one active category" rule, budget sets that are
    /// never mixed, and budgets or expenses that never race their category being deleted or
    /// archived). The no-op update takes row locks held
    /// until commit or rollback (writers block writers under both READ COMMITTED and
    /// read-committed snapshot); every caller locks the same rows through the same index, so they
    /// queue instead of deadlocking. Returns null on non-relational providers (EF InMemory in unit
    /// tests), which support neither transactions nor ExecuteUpdate.
    /// </summary>
    protected async Task<IDbContextTransaction?> LockFamilyCategoriesAsync(Guid familyId)
    {
        if (!Db.Database.IsRelational())
        {
            return null;
        }

        var transaction = await Db.Database.BeginTransactionAsync();
        await Db.ExpenseCategories
            .Where(c => c.FamilyId == familyId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.SortOrder, c => c.SortOrder));
        return transaction;
    }

    /// <summary>
    /// The budget rows in effect for the month starting at <paramref name="monthStart"/>: per budget
    /// (overall, or one category) the latest row that started on or before that month. A row's
    /// Amount may be null, meaning the budget was removed. A family has one row per budget change,
    /// so the rows are resolved in memory.
    /// </summary>
    protected async Task<EffectiveBudgets> GetEffectiveBudgetsAsync(Guid familyId, DateOnly monthStart)
    {
        var rows = await Db.ExpenseBudgets
            .Where(b => b.FamilyId == familyId && b.EffectiveFrom <= monthStart)
            .ToListAsync();

        var overall = rows
            .Where(b => b.CategoryId is null)
            .MaxBy(b => b.EffectiveFrom);
        var categories = rows
            .Where(b => b.CategoryId is not null)
            .GroupBy(b => b.CategoryId!.Value)
            .ToDictionary(g => g.Key, g => g.MaxBy(b => b.EffectiveFrom)!);

        return new EffectiveBudgets(overall, categories);
    }

    protected sealed record EffectiveBudgets(ExpenseBudget? Overall, IReadOnlyDictionary<Guid, ExpenseBudget> Categories)
    {
        public decimal? OverallAmount => Overall?.Amount;

        public decimal? AmountFor(Guid categoryId) => Categories.GetValueOrDefault(categoryId)?.Amount;
    }

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
