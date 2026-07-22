using FamilyHub.Api.DTOs.Tasks;
using FluentValidation;

namespace FamilyHub.Api.Validators;

public class UpdateTaskStatusRequestValidator : AbstractValidator<UpdateTaskStatusRequest>
{
    public UpdateTaskStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("A valid status is required.");
    }
}
