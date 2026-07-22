using FamilyHub.Api.DTOs.Events;
using FluentValidation;

namespace FamilyHub.Api.Validators;

public class UpdateEventRequestValidator : AbstractValidator<UpdateEventRequest>
{
    public UpdateEventRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(2000);

        RuleFor(x => x.Location)
            .MaximumLength(300);

        RuleFor(x => x.EventType)
            .IsInEnum().WithMessage("A valid event type is required.");

        RuleFor(x => x.EndDateTime)
            .Must((req, end) => end > req.StartDateTime)
            .WithMessage("End date/time must be after the start date/time.")
            .When(x => x.EndDateTime.HasValue);
    }
}
