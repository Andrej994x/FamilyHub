using FamilyHub.Api.DTOs.Tasks;
using FluentValidation;

namespace FamilyHub.Api.Validators;

public class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(2000);

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("A valid priority is required.");
    }
}
