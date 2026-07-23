using FamilyHub.Api.DTOs.Documents;
using FluentValidation;

namespace FamilyHub.Api.Validators;

public class UpdateFamilyDocumentRequestValidator : AbstractValidator<UpdateFamilyDocumentRequest>
{
    public UpdateFamilyDocumentRequestValidator()
    {
        RuleFor(x => x.DocumentType)
            .IsInEnum().WithMessage("A valid document type is required.");

        RuleFor(x => x)
            .Must(x => (x.FamilyMemberId is null) ^ (x.ChildProfileId is null))
            .WithMessage("Provide either a family member or a child profile, but not both.");

        RuleFor(x => x.DocumentNumber)
            .MaximumLength(100);

        RuleFor(x => x.Notes)
            .MaximumLength(2000);

        RuleFor(x => x.ExpiryDate)
            .Must((req, expiry) => expiry > req.IssueDate)
            .WithMessage("Expiry date must be after the issue date.")
            .When(x => x.IssueDate.HasValue && x.ExpiryDate.HasValue);
    }
}
