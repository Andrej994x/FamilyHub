using FamilyHub.Api.Common;
using FamilyHub.Api.DTOs.Expenses;

namespace FamilyHub.Api.Interfaces;

public interface IExpenseService
{
    Task<Result<ExpenseResponse>> CreateExpenseAsync(string userId, Guid familyId, CreateExpenseRequest request);

    Task<Result<IReadOnlyList<ExpenseResponse>>> GetExpensesAsync(string userId, Guid familyId, ExpenseFilter filter);

    Task<Result<ExpenseResponse>> GetExpenseAsync(string userId, Guid familyId, Guid expenseId);

    Task<Result<ExpenseResponse>> UpdateExpenseAsync(string userId, Guid familyId, Guid expenseId, UpdateExpenseRequest request);

    Task<Result> DeleteExpenseAsync(string userId, Guid familyId, Guid expenseId);

    Task<Result<ExpenseSummaryResponse>> GetSummaryAsync(string userId, Guid familyId, int year, int month);
}
