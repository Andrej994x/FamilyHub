using FamilyHub.Api.DTOs.Push;
using FluentValidation;

namespace FamilyHub.Api.Validators;

public class CreatePushSubscriptionRequestValidator : AbstractValidator<CreatePushSubscriptionRequest>
{
    public CreatePushSubscriptionRequestValidator()
    {
        RuleFor(x => x.Endpoint)
            .NotEmpty().WithMessage("Endpoint is required.")
            .MaximumLength(2048)
            .Must(BeAnAbsoluteHttpUrl).WithMessage("Endpoint must be an absolute http(s) URL.");

        RuleFor(x => x.P256dh)
            .NotEmpty().WithMessage("The p256dh key is required.")
            .MaximumLength(255);

        RuleFor(x => x.Auth)
            .NotEmpty().WithMessage("The auth key is required.")
            .MaximumLength(255);

        RuleFor(x => x.UserAgent)
            .MaximumLength(400);
    }

    private static bool BeAnAbsoluteHttpUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}

public class UpdatePushSubscriptionRequestValidator : AbstractValidator<UpdatePushSubscriptionRequest>
{
    public UpdatePushSubscriptionRequestValidator()
    {
        RuleFor(x => x.P256dh)
            .NotEmpty().WithMessage("The p256dh key is required.")
            .MaximumLength(255);

        RuleFor(x => x.Auth)
            .NotEmpty().WithMessage("The auth key is required.")
            .MaximumLength(255);

        RuleFor(x => x.UserAgent)
            .MaximumLength(400);
    }
}
