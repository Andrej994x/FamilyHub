using FamilyHub.Api.DTOs.Reminders;
using FluentValidation;

namespace FamilyHub.Api.Validators;

public class UpdateReminderPreferenceRequestValidator : AbstractValidator<UpdateReminderPreferenceRequest>
{
    private const int MaxOffsets = 10;
    private const int MaxOffsetDays = 365;

    public UpdateReminderPreferenceRequestValidator()
    {
        RuleFor(x => x.ReminderOffsetsDays)
            .NotNull().WithMessage("Reminder offsets are required.")
            .Must(o => o.Length >= 1).WithMessage("Provide at least one reminder offset (or disable the category).")
            .Must(o => o.Length <= MaxOffsets).WithMessage($"At most {MaxOffsets} reminder offsets are allowed.")
            .Must(o => o.All(v => v >= 0 && v <= MaxOffsetDays))
                .WithMessage($"Each offset must be between 0 and {MaxOffsetDays} days.")
            .Must(o => o.Distinct().Count() == o.Length)
                .WithMessage("Reminder offsets must not contain duplicates.");
    }
}
