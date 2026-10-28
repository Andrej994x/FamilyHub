using FamilyHub.Api.Common;
using FamilyHub.Api.DTOs.Expenses;
using FluentValidation;

namespace FamilyHub.Api.Validators;

public class SetExpenseBudgetsRequestValidator : AbstractValidator<SetExpenseBudgetsRequest>
{
    public const int MaxCategories = 100;

    public SetExpenseBudgetsRequestValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);

        RuleFor(x => x.Month).InclusiveBetween(1, 12);

        RuleFor(x => x.OverallAmount!.Value)
            .GreaterThan(0).WithMessage("Budget must be greater than zero.")
            .LessThanOrEqualTo(ExpenseRules.MaxAmount).WithMessage("Budget is too large.")
            .Must(ExpenseRules.HasAtMostTwoDecimals).WithMessage("Budget can have at most two decimals.")
            .OverridePropertyName(nameof(SetExpenseBudgetsRequest.OverallAmount))
            .When(x => x.OverallAmount.HasValue);

        RuleFor(x => x.Categories)
            .Must(c => c is null || c.Count <= MaxCategories).WithMessage($"At most {MaxCategories} category budgets are allowed.")
            .Must(c => c is null || c.All(b => b is not null)).WithMessage("Category budgets cannot be null.")
            .Must(c => c is null || c.Where(b => b is not null).Select(b => b.CategoryId).Distinct().Count() == c.Count(b => b is not null))
            .WithMessage("Each category can appear only once.");

        RuleForEach(x => x.Categories).ChildRules(category =>
        {
            category.RuleFor(c => c.CategoryId)
                .NotEmpty().WithMessage("A category is required.");

            category.RuleFor(c => c.Amount!.Value)
                .GreaterThan(0).WithMessage("Budget must be greater than zero.")
                .LessThanOrEqualTo(ExpenseRules.MaxAmount).WithMessage("Budget is too large.")
                .Must(ExpenseRules.HasAtMostTwoDecimals).WithMessage("Budget can have at most two decimals.")
                .OverridePropertyName(nameof(CategoryBudgetRequest.Amount))
                .When(c => c.Amount.HasValue);
        });
    }
}
