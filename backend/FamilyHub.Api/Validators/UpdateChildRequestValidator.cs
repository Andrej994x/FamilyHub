using FamilyHub.Api.DTOs.Children;
using FluentValidation;

namespace FamilyHub.Api.Validators;

public class UpdateChildRequestValidator : AbstractValidator<UpdateChildRequest>
{
    public UpdateChildRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Notes)
            .MaximumLength(2000);

        RuleFor(x => x.DateOfBirth)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date of birth cannot be in the future.")
            .When(x => x.DateOfBirth.HasValue);
    }
}
