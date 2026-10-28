using FamilyHub.Api.DTOs.Expenses;
using FluentValidation;

namespace FamilyHub.Api.Validators;

public class UpdateExpenseCategoryRequestValidator : AbstractValidator<UpdateExpenseCategoryRequest>
{
    public UpdateExpenseCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("Name is required.")
            .Must(n => n is null || n.Trim().Length <= 50).WithMessage("Name can be at most 50 characters.");
    }
}
