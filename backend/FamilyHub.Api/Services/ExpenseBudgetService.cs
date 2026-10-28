using FamilyHub.Api.Common;
using FamilyHub.Api.Data;
using FamilyHub.Api.DTOs.Expenses;
using FamilyHub.Api.Interfaces;
using FamilyHub.Api.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FamilyHub.Api.Services;

/// <summary>
/// Monthly family budgets (MKD): one optional overall budget plus optional per-category budgets.
/// A budget applies from the month it is set until it is changed. Any member except a Child can
/// view budgets; only Owners and Parents can change them.
/// </summary>
public class ExpenseBudgetService : ExpenseServiceBase, IExpenseBudgetService
{
    // SQL Server: duplicate key (unique index / constraint) and deadlock victim.
    private static readonly int[] ConcurrentChangeErrors = [2601, 2627, 1205];

    public ExpenseBudgetService(AppDbContext db, ILogger<ExpenseBudgetService> logger)
        : base(db, logger)
    {
    }

    public async Task<Result<ExpenseBudgetResponse>> GetBudgetsAsync(string userId, Guid familyId, int year, int month)
    {
        var (membership, error, message) = await AuthorizeAsync(userId, familyId, requireManage: false);
        if (membership is null)
        {
            return Result<ExpenseBudgetResponse>.Failure(error!.Value, message!);
        }

        var budgets = await GetEffectiveBudgetsAsync(familyId, new DateOnly(year, month, 1));
        return Result<ExpenseBudgetResponse>.Success(ToResponse(year, month, budgets));
    }

    public async Task<Result<ExpenseBudgetResponse>> SetBudgetsAsync(
        string userId, Guid familyId, SetExpenseBudgetsRequest request)
    {
        var (membership, error, message) = await AuthorizeAsync(userId, familyId, requireManage: true);
        if (membership is null)
        {
            return Result<ExpenseBudgetResponse>.Failure(error!.Value, message!);
        }

        // Serializes budget changes with each other and with category archive/delete, so the
        // checks and the current state below can't go stale before the save.
        await using var transaction = await LockFamilyCategoriesAsync(familyId);

        var requested = request.Categories ?? [];
        var categoryIds = requested.Select(c => c.CategoryId).ToList();
        var categories = await Db.ExpenseCategories
            .Where(c => c.FamilyId == familyId && categoryIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id);

        if (categories.Count != categoryIds.Count)
        {
            return Result<ExpenseBudgetResponse>.Failure(
                ErrorType.Validation, "One or more categories do not exist in this family.");
        }

        if (requested.Any(c => c.Amount is not null && categories[c.CategoryId].IsArchived))
        {
            return Result<ExpenseBudgetResponse>.Failure(
                ErrorType.Validation, "Archived categories can't have a budget.");
        }

        var monthStart = new DateOnly(request.Year, request.Month, 1);
        var current = await GetEffectiveBudgetsAsync(familyId, monthStart);

        // The request is the complete set from this month on: a category that has a budget now
        // but is not listed loses it from this month on.
        var desired = requested.ToDictionary(c => c.CategoryId, c => c.Amount);
        foreach (var (categoryId, row) in current.Categories)
        {
            if (row.Amount is not null && !desired.ContainsKey(categoryId))
            {
                desired[categoryId] = null;
            }
        }

        var now = DateTimeOffset.UtcNow;
        ApplyChange(familyId, null, current.Overall, request.OverallAmount, monthStart, userId, now);
        foreach (var (categoryId, amount) in desired.OrderBy(d => d.Key))
        {
            ApplyChange(familyId, categoryId, current.Categories.GetValueOrDefault(categoryId), amount, monthStart, userId, now);
        }

        try
        {
            await Db.SaveChangesAsync();
            if (transaction is not null)
            {
                await transaction.CommitAsync();
            }
        }
        // The lock covers nothing for a family with no categories yet; the unique index then
        // rejects the second of two concurrent first budgets.
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && ConcurrentChangeErrors.Contains(sql.Number))
        {
            Logger.LogInformation("Concurrent budget change for family {FamilyId} rejected", familyId);
            return Result<ExpenseBudgetResponse>.Failure(
                ErrorType.Conflict, "The budgets were changed at the same time by someone else. Reload and try again.");
        }

        var saved = await GetEffectiveBudgetsAsync(familyId, monthStart);
        return Result<ExpenseBudgetResponse>.Success(ToResponse(request.Year, request.Month, saved));
    }

    /// <summary>
    /// Makes <paramref name="amount"/> the budget from <paramref name="monthStart"/> on. Nothing is
    /// written when it is already in effect; a row that starts this month is updated in place;
    /// otherwise a new row starts this month, so earlier months keep their budget.
    /// </summary>
    private void ApplyChange(Guid familyId, Guid? categoryId, ExpenseBudget? current, decimal? amount,
        DateOnly monthStart, string userId, DateTimeOffset now)
    {
        if (current?.Amount == amount)
        {
            return;
        }

        if (current is not null && current.EffectiveFrom == monthStart)
        {
            current.Amount = amount;
            current.UpdatedByUserId = userId;
            current.UpdatedAt = now;
            return;
        }

        Db.ExpenseBudgets.Add(new ExpenseBudget
        {
            Id = Guid.NewGuid(),
            FamilyId = familyId,
            CategoryId = categoryId,
            EffectiveFrom = monthStart,
            Amount = amount,
            UpdatedByUserId = userId,
            UpdatedAt = now,
        });
    }

    private static ExpenseBudgetResponse ToResponse(int year, int month, EffectiveBudgets budgets) =>
        new(year, month, budgets.OverallAmount,
            budgets.Categories
                .Where(c => c.Value.Amount is not null)
                .Select(c => new CategoryBudgetResponse(c.Key, c.Value.Amount!.Value))
                .OrderBy(c => c.CategoryId)
                .ToList());
}
