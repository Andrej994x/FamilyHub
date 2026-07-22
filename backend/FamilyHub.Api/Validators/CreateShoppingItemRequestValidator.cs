using FamilyHub.Api.DTOs.Shopping;
using FluentValidation;

namespace FamilyHub.Api.Validators;

public class CreateShoppingItemRequestValidator : AbstractValidator<CreateShoppingItemRequest>
{
    public CreateShoppingItemRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Item name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Quantity)
            .MaximumLength(50);

        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("A valid category is required.");
    }
}
