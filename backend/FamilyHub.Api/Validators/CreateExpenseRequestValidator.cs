using FamilyHub.Api.Common;
using FamilyHub.Api.DTOs.Expenses;
using FluentValidation;

namespace FamilyHub.Api.Validators;

public class CreateExpenseRequestValidator : AbstractValidator<CreateExpenseRequest>
{
    public CreateExpenseRequestValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.")
            .LessThanOrEqualTo(ExpenseRules.MaxAmount).WithMessage("Amount is too large.")
            .Must(ExpenseRules.HasAtMostTwoDecimals).WithMessage("Amount can have at most two decimals.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("A category is required.");

        RuleFor(x => x.Date)
            .GreaterThanOrEqualTo(ExpenseRules.MinDate).WithMessage("Date is too far in the past.")
            .Must(d => d <= ExpenseRules.LatestAllowedDate()).WithMessage("Date cannot be in the future.");

        RuleFor(x => x.PaidByUserId)
            .NotEmpty().WithMessage("Choose who paid.")
            .MaximumLength(450);

        RuleFor(x => x.Description)
            .MaximumLength(500);
    }
}
