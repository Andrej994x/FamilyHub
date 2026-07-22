using FamilyHub.Api.DTOs.Families;
using FluentValidation;

namespace FamilyHub.Api.Validators;

public class CreateFamilyRequestValidator : AbstractValidator<CreateFamilyRequest>
{
    public CreateFamilyRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Family name is required.")
            .MaximumLength(200);
    }
}
