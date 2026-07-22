using FamilyHub.Api.DTOs.Invitations;
using FamilyHub.Api.Models.Enums;
using FluentValidation;

namespace FamilyHub.Api.Validators;

public class CreateInvitationRequestValidator : AbstractValidator<CreateInvitationRequest>
{
    public CreateInvitationRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256);

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("A valid role is required.")
            .NotEqual(FamilyRole.Owner).WithMessage("A user cannot be invited as Owner.");
    }
}
