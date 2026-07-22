using FamilyHub.Api.DTOs.Pickups;
using FamilyHub.Api.Models.Enums;
using FluentValidation;

namespace FamilyHub.Api.Validators;

public class UpdatePickupStatusRequestValidator : AbstractValidator<UpdatePickupStatusRequest>
{
    public UpdatePickupStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("A valid status is required.")
            .NotEqual(PickupStatus.Pending)
            .WithMessage("Status must be Confirmed, CannotAttend or Completed.");
    }
}
