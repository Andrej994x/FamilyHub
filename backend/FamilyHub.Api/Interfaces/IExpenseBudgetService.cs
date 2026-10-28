using FamilyHub.Api.Common;
using FamilyHub.Api.DTOs.Expenses;

namespace FamilyHub.Api.Interfaces;

public interface IExpenseBudgetService
{
    Task<Result<ExpenseBudgetResponse>> GetBudgetsAsync(string userId, Guid familyId, int year, int month);

    Task<Result<ExpenseBudgetResponse>> SetBudgetsAsync(string userId, Guid familyId, SetExpenseBudgetsRequest request);
}
