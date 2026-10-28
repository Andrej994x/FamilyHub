using FamilyHub.Api.DTOs.Expenses;
using FamilyHub.Api.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/families/{familyId:guid}/expense-budgets")]
public class ExpenseBudgetsController : ApiControllerBase
{
    private readonly IExpenseBudgetService _service;
    private readonly IValidator<ExpenseMonthQuery> _monthValidator;
    private readonly IValidator<SetExpenseBudgetsRequest> _setValidator;

    public ExpenseBudgetsController(
        IExpenseBudgetService service,
        IValidator<ExpenseMonthQuery> monthValidator,
        IValidator<SetExpenseBudgetsRequest> setValidator)
    {
        _service = service;
        _monthValidator = monthValidator;
        _setValidator = setValidator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ExpenseBudgetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid familyId, [FromQuery] ExpenseMonthQuery query)
    {
        var validation = await _monthValidator.ValidateAsync(query);
        if (!validation.IsValid) return ToValidationProblem(validation);

        return HandleResult(await _service.GetBudgetsAsync(CurrentUserId, familyId, query.Year!.Value, query.Month!.Value));
    }

    [HttpPut]
    [ProducesResponseType(typeof(ExpenseBudgetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Set(Guid familyId, [FromBody] SetExpenseBudgetsRequest request)
    {
        var validation = await _setValidator.ValidateAsync(request);
        if (!validation.IsValid) return ToValidationProblem(validation);

        return HandleResult(await _service.SetBudgetsAsync(CurrentUserId, familyId, request));
    }
}
