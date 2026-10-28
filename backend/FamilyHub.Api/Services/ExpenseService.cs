using FamilyHub.Api.Common;
using FamilyHub.Api.Data;
using FamilyHub.Api.DTOs.Expenses;
using FamilyHub.Api.Interfaces;
using FamilyHub.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FamilyHub.Api.Services;

/// <summary>
/// Shared family expenses. Every member except a Child can view and add expenses; Owners and
/// Parents can change any expense, other members only the ones they created.
/// </summary>
public class ExpenseService : ExpenseServiceBase, IExpenseService
{
    private const string NotFoundMessage = "Expense not found in this family.";
    private const int RecentExpenseCount = 5;

    public ExpenseService(AppDbContext db, ILogger<ExpenseService> logger)
        : base(db, logger)
    {
    }

    public async Task<Result<ExpenseResponse>> CreateExpenseAsync(string userId, Guid familyId, CreateExpenseRequest request)
    {
        var (membership, error, message) = await AuthorizeAsync(userId, familyId, requireManage: false);
        if (membership is null)
        {
            return Result<ExpenseResponse>.Failure(error!.Value, message!);
        }

        // Serializes with category delete/archive so the category check below still holds at save time.
        await using var transaction = await LockFamilyCategoriesAsync(familyId);

        var category = await Db.ExpenseCategories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId && c.FamilyId == familyId);
        var invalid = ValidateCategory(category, currentCategoryId: null)
                      ?? await ValidatePayerAsync(familyId, request.PaidByUserId, currentPayerId: null);
        if (invalid is not null)
        {
            return Result<ExpenseResponse>.Failure(ErrorType.Validation, invalid);
        }

        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            FamilyId = familyId,
            CategoryId = category!.Id,
            Amount = request.Amount,
            Date = request.Date,
            PaidByUserId = request.PaidByUserId,
            Description = Normalize(request.Description),
            CreatedByUserId = userId,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        Db.Expenses.Add(expense);
        await Db.SaveChangesAsync();
        if (transaction is not null)
        {
            await transaction.CommitAsync();
        }

        return Result<ExpenseResponse>.Success(await ToResponseAsync(expense, category));
    }

    public async Task<Result<IReadOnlyList<ExpenseResponse>>> GetExpensesAsync(
        string userId, Guid familyId, ExpenseFilter filter)
    {
        var (membership, error, message) = await AuthorizeAsync(userId, familyId, requireManage: false);
        if (membership is null)
        {
            return Result<IReadOnlyList<ExpenseResponse>>.Failure(error!.Value, message!);
        }

        // The validator guarantees a year and month; one month is always a bounded result.
        var monthStart = new DateOnly(filter.Year!.Value, filter.Month!.Value, 1);
        var nextMonthStart = monthStart.AddMonths(1);

        var query = Db.Expenses
            .Include(e => e.Category)
            .Where(e => e.FamilyId == familyId && e.Date >= monthStart && e.Date < nextMonthStart);

        if (filter.CategoryId is not null)
        {
            query = query.Where(e => e.CategoryId == filter.CategoryId);
        }

        if (!string.IsNullOrEmpty(filter.PaidByUserId))
        {
            query = query.Where(e => e.PaidByUserId == filter.PaidByUserId);
        }

        var expenses = await query
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.CreatedAt)
            .ToListAsync();

        var names = await GetUserNamesAsync(expenses.SelectMany(e => new[] { e.PaidByUserId, e.CreatedByUserId }));
        IReadOnlyList<ExpenseResponse> responses = expenses
            .Select(e => ToResponse(e, e.Category!, names))
            .ToList();

        return Result<IReadOnlyList<ExpenseResponse>>.Success(responses);
    }

    public async Task<Result<ExpenseResponse>> GetExpenseAsync(string userId, Guid familyId, Guid expenseId)
    {
        var (membership, error, message) = await AuthorizeAsync(userId, familyId, requireManage: false);
        if (membership is null)
        {
            return Result<ExpenseResponse>.Failure(error!.Value, message!);
        }

        var expense = await FindAsync(familyId, expenseId);
        return expense is null
            ? Result<ExpenseResponse>.Failure(ErrorType.NotFound, NotFoundMessage)
            : Result<ExpenseResponse>.Success(await ToResponseAsync(expense, expense.Category!));
    }

    public async Task<Result<ExpenseResponse>> UpdateExpenseAsync(
        string userId, Guid familyId, Guid expenseId, UpdateExpenseRequest request)
    {
        var (membership, error, message) = await AuthorizeAsync(userId, familyId, requireManage: false);
        if (membership is null)
        {
            return Result<ExpenseResponse>.Failure(error!.Value, message!);
        }

        // Serializes with category delete/archive so the category check below still holds at save time.
        await using var transaction = await LockFamilyCategoriesAsync(familyId);

        var expense = await FindAsync(familyId, expenseId);
        if (expense is null)
        {
            return Result<ExpenseResponse>.Failure(ErrorType.NotFound, NotFoundMessage);
        }

        if (!CanChange(membership, expense))
        {
            return Result<ExpenseResponse>.Failure(ErrorType.Forbidden, "You can only change expenses you added.");
        }

        var category = request.CategoryId == expense.CategoryId
            ? expense.Category
            : await Db.ExpenseCategories.FirstOrDefaultAsync(c => c.Id == request.CategoryId && c.FamilyId == familyId);
        var invalid = ValidateCategory(category, expense.CategoryId)
                      ?? await ValidatePayerAsync(familyId, request.PaidByUserId, expense.PaidByUserId);
        if (invalid is not null)
        {
            return Result<ExpenseResponse>.Failure(ErrorType.Validation, invalid);
        }

        expense.CategoryId = category!.Id;
        expense.Category = category;
        expense.Amount = request.Amount;
        expense.Date = request.Date;
        expense.PaidByUserId = request.PaidByUserId;
        expense.Description = Normalize(request.Description);
        expense.UpdatedAt = DateTimeOffset.UtcNow;

        await Db.SaveChangesAsync();
        if (transaction is not null)
        {
            await transaction.CommitAsync();
        }

        return Result<ExpenseResponse>.Success(await ToResponseAsync(expense, category));
    }

    public async Task<Result> DeleteExpenseAsync(string userId, Guid familyId, Guid expenseId)
    {
        var (membership, error, message) = await AuthorizeAsync(userId, familyId, requireManage: false);
        if (membership is null)
        {
            return Result.Failure(error!.Value, message!);
        }

        var expense = await Db.Expenses.FirstOrDefaultAsync(e => e.Id == expenseId && e.FamilyId == familyId);
        if (expense is null)
        {
            return Result.Failure(ErrorType.NotFound, NotFoundMessage);
        }

        if (!CanChange(membership, expense))
        {
            return Result.Failure(ErrorType.Forbidden, "You can only delete expenses you added.");
        }

        Db.Expenses.Remove(expense);
        await Db.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<ExpenseSummaryResponse>> GetSummaryAsync(string userId, Guid familyId, int year, int month)
    {
        var (membership, error, message) = await AuthorizeAsync(userId, familyId, requireManage: false);
        if (membership is null)
        {
            return Result<ExpenseSummaryResponse>.Failure(error!.Value, message!);
        }

        var monthStart = new DateOnly(year, month, 1);
        var nextMonthStart = monthStart.AddMonths(1);
        var monthExpenses = Db.Expenses
            .Where(e => e.FamilyId == familyId && e.Date >= monthStart && e.Date < nextMonthStart);

        // Totals are summed in SQL; only one row per category comes back.
        var spending = await monthExpenses
            .GroupBy(e => e.CategoryId)
            .Select(g => new { CategoryId = g.Key, Spent = g.Sum(e => e.Amount), Count = g.Count() })
            .ToListAsync();

        var recent = await monthExpenses
            .Include(e => e.Category)
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.CreatedAt)
            .Take(RecentExpenseCount)
            .ToListAsync();

        var budgets = await GetEffectiveBudgetsAsync(familyId, monthStart);
        var budgetedCategoryIds = budgets.Categories
            .Where(c => c.Value.Amount is not null)
            .Select(c => c.Key);
        var categoryIds = spending.Select(s => s.CategoryId).Union(budgetedCategoryIds).ToList();
        var categories = await Db.ExpenseCategories
            .Where(c => c.FamilyId == familyId && categoryIds.Contains(c.Id))
            .ToListAsync();

        var byCategory = categories
            .Select(c =>
            {
                var spent = spending.FirstOrDefault(s => s.CategoryId == c.Id);
                var amount = spent?.Spent ?? 0m;
                var budget = budgets.AmountFor(c.Id);
                return (Category: c, Row: new CategorySpendingResponse(c.Id, c.Name, c.DefaultCategory, c.IsArchived,
                    amount, spent?.Count ?? 0, budget, budget - amount, UsagePercent(amount, budget)));
            })
            .OrderByDescending(x => x.Row.Spent)
            .ThenBy(x => x.Category.SortOrder)
            .ThenBy(x => x.Category.Name)
            .Select(x => x.Row)
            .ToList();

        var total = spending.Sum(s => s.Spent);
        var overallBudget = budgets.OverallAmount;
        var names = await GetUserNamesAsync(recent.SelectMany(e => new[] { e.PaidByUserId, e.CreatedByUserId }));

        return Result<ExpenseSummaryResponse>.Success(new ExpenseSummaryResponse(
            year, month, total, spending.Sum(s => s.Count),
            overallBudget, overallBudget - total, UsagePercent(total, overallBudget),
            byCategory,
            recent.Select(e => ToResponse(e, e.Category!, names)).ToList()));
    }

    /// <summary>
    /// Share of the budget spent, as a whole percentage (above 100 when over budget). Capped at
    /// int.MaxValue: a tiny budget with large spending would otherwise overflow the cast.
    /// </summary>
    private static int? UsagePercent(decimal spent, decimal? budget) =>
        budget is > 0
            ? (int)Math.Min(Math.Round(spent * 100m / budget.Value, MidpointRounding.AwayFromZero), int.MaxValue)
            : null;

    private static bool CanChange(FamilyMember membership, Expense expense) =>
        CanManage(membership) || expense.CreatedByUserId == membership.UserId;

    private Task<Expense?> FindAsync(Guid familyId, Guid expenseId) =>
        Db.Expenses
            .Include(e => e.Category)
            .FirstOrDefaultAsync(e => e.Id == expenseId && e.FamilyId == familyId);

    /// <summary>
    /// The category must belong to the family. An archived category can't be newly chosen, but an
    /// expense may keep the one it already has. Returns an error message, or null when valid.
    /// </summary>
    private static string? ValidateCategory(ExpenseCategory? category, Guid? currentCategoryId)
    {
        if (category is null)
        {
            return "The category does not exist in this family.";
        }

        if (category.IsArchived && category.Id != currentCategoryId)
        {
            return "This category is archived and can't be used for new expenses.";
        }

        return null;
    }

    /// <summary>
    /// The payer must be a current family member, unless an existing expense keeps a payer who
    /// has since left. Returns an error message, or null when valid.
    /// </summary>
    private async Task<string?> ValidatePayerAsync(Guid familyId, string paidByUserId, string? currentPayerId)
    {
        if (paidByUserId == currentPayerId)
        {
            return null;
        }

        var isMember = await Db.FamilyMembers.AnyAsync(m => m.FamilyId == familyId && m.UserId == paidByUserId);
        return isMember ? null : "The payer must be a member of this family.";
    }

    private async Task<Dictionary<string, string>> GetUserNamesAsync(IEnumerable<string> userIds)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<string, string>();
        }

        var users = await Db.Users
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName })
            .ToListAsync();

        return users.ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}".Trim());
    }

    private async Task<ExpenseResponse> ToResponseAsync(Expense expense, ExpenseCategory category) =>
        ToResponse(expense, category, await GetUserNamesAsync([expense.PaidByUserId, expense.CreatedByUserId]));

    private static ExpenseResponse ToResponse(Expense e, ExpenseCategory category, IReadOnlyDictionary<string, string> names) =>
        new(e.Id, e.FamilyId, e.Amount, e.Date, category.Id, category.Name, category.DefaultCategory, category.IsArchived,
            e.PaidByUserId, names.GetValueOrDefault(e.PaidByUserId, string.Empty), e.Description,
            e.CreatedByUserId, names.GetValueOrDefault(e.CreatedByUserId, string.Empty), e.CreatedAt, e.UpdatedAt);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
