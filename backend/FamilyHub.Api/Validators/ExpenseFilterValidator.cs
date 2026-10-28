using FamilyHub.Api.DTOs.Expenses;
using FluentValidation;

namespace FamilyHub.Api.Validators;

public class ExpenseFilterValidator : AbstractValidator<ExpenseFilter>
{
    public ExpenseFilterValidator()
    {
        RuleFor(x => x.Year)
            .NotNull().WithMessage("Year is required.")
            .InclusiveBetween(2000, 2100);

        RuleFor(x => x.Month)
            .NotNull().WithMessage("Month is required.")
            .InclusiveBetween(1, 12);

        RuleFor(x => x.PaidByUserId)
            .MaximumLength(450);
    }
}
