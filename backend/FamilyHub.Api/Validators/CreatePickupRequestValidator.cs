using FamilyHub.Api.DTOs.Pickups;
using FluentValidation;

namespace FamilyHub.Api.Validators;

public class CreatePickupRequestValidator : AbstractValidator<CreatePickupRequest>
{
    public CreatePickupRequestValidator()
    {
        RuleFor(x => x.ChildProfileId)
            .NotEmpty().WithMessage("A child must be selected.");

        RuleFor(x => x.AssignedMemberId)
            .NotEmpty().WithMessage("An assigned member is required.");

        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("Location is required.")
            .MaximumLength(300);

        RuleFor(x => x.Notes)
            .MaximumLength(2000);
    }
}
