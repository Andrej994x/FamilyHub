using FamilyHub.Api.DTOs.Families;
using FluentValidation;

namespace FamilyHub.Api.Validators;

public class UpdateFamilyRequestValidator : AbstractValidator<UpdateFamilyRequest>
{
    public UpdateFamilyRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Family name is required.")
            .MaximumLength(200);
    }
}
