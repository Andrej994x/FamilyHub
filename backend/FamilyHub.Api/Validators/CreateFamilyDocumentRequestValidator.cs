using FamilyHub.Api.DTOs.Documents;
using FluentValidation;

namespace FamilyHub.Api.Validators;

public class CreateFamilyDocumentRequestValidator : AbstractValidator<CreateFamilyDocumentRequest>
{
    public CreateFamilyDocumentRequestValidator()
    {
        RuleFor(x => x.DocumentType)
            .IsInEnum().WithMessage("A valid document type is required.");

        // The document belongs to exactly one subject: a member or a child.
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
