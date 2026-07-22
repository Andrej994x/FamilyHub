using FamilyHub.Api.DTOs.Shopping;
using FluentValidation;

namespace FamilyHub.Api.Validators;

public class CreateShoppingListRequestValidator : AbstractValidator<CreateShoppingListRequest>
{
    public CreateShoppingListRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("List name is required.")
            .MaximumLength(200);
    }
}
