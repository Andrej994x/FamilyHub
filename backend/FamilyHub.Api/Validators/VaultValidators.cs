using FamilyHub.Api.DTOs.Vault;
using FluentValidation;

namespace FamilyHub.Api.Validators;

public class CreateVehicleRequestValidator : AbstractValidator<CreateVehicleRequest>
{
    public CreateVehicleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(200);
        RuleFor(x => x.Make).MaximumLength(100);
        RuleFor(x => x.Model).MaximumLength(100);
        RuleFor(x => x.RegistrationNumber).MaximumLength(50);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.NextServiceMileage).GreaterThanOrEqualTo(0).When(x => x.NextServiceMileage.HasValue);
    }
}

public class UpdateVehicleRequestValidator : AbstractValidator<UpdateVehicleRequest>
{
    public UpdateVehicleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(200);
        RuleFor(x => x.Make).MaximumLength(100);
        RuleFor(x => x.Model).MaximumLength(100);
        RuleFor(x => x.RegistrationNumber).MaximumLength(50);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.NextServiceMileage).GreaterThanOrEqualTo(0).When(x => x.NextServiceMileage.HasValue);
    }
}

public class CreatePetRequestValidator : AbstractValidator<CreatePetRequest>
{
    public CreatePetRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(200);
        RuleFor(x => x.Type).MaximumLength(100);
        RuleFor(x => x.Breed).MaximumLength(100);
        RuleFor(x => x.MicrochipNumber).MaximumLength(100);
        RuleFor(x => x.VaccinationName).MaximumLength(200);
        RuleFor(x => x.Veterinarian).MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public class UpdatePetRequestValidator : AbstractValidator<UpdatePetRequest>
{
    public UpdatePetRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.").MaximumLength(200);
        RuleFor(x => x.Type).MaximumLength(100);
        RuleFor(x => x.Breed).MaximumLength(100);
        RuleFor(x => x.MicrochipNumber).MaximumLength(100);
        RuleFor(x => x.VaccinationName).MaximumLength(200);
        RuleFor(x => x.Veterinarian).MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public class CreateHomeRecordRequestValidator : AbstractValidator<CreateHomeRecordRequest>
{
    public CreateHomeRecordRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Title is required.").MaximumLength(200);
        RuleFor(x => x.Type).MaximumLength(100);
        RuleFor(x => x.Provider).MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public class UpdateHomeRecordRequestValidator : AbstractValidator<UpdateHomeRecordRequest>
{
    public UpdateHomeRecordRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Title is required.").MaximumLength(200);
        RuleFor(x => x.Type).MaximumLength(100);
        RuleFor(x => x.Provider).MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public class CreateWarrantyRequestValidator : AbstractValidator<CreateWarrantyRequest>
{
    public CreateWarrantyRequestValidator()
    {
        RuleFor(x => x.ProductName).NotEmpty().WithMessage("Product name is required.").MaximumLength(200);
        RuleFor(x => x.Store).MaximumLength(200);
        RuleFor(x => x.SerialNumber).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.WarrantyExpiryDate)
            .Must((req, expiry) => expiry > req.PurchaseDate)
            .WithMessage("Warranty expiry must be after the purchase date.")
            .When(x => x.PurchaseDate.HasValue && x.WarrantyExpiryDate.HasValue);
    }
}

public class UpdateWarrantyRequestValidator : AbstractValidator<UpdateWarrantyRequest>
{
    public UpdateWarrantyRequestValidator()
    {
        RuleFor(x => x.ProductName).NotEmpty().WithMessage("Product name is required.").MaximumLength(200);
        RuleFor(x => x.Store).MaximumLength(200);
        RuleFor(x => x.SerialNumber).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.WarrantyExpiryDate)
            .Must((req, expiry) => expiry > req.PurchaseDate)
            .WithMessage("Warranty expiry must be after the purchase date.")
            .When(x => x.PurchaseDate.HasValue && x.WarrantyExpiryDate.HasValue);
    }
}

public class CreateOtherRecordRequestValidator : AbstractValidator<CreateOtherRecordRequest>
{
    public CreateOtherRecordRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Title is required.").MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}

public class UpdateOtherRecordRequestValidator : AbstractValidator<UpdateOtherRecordRequest>
{
    public UpdateOtherRecordRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().WithMessage("Title is required.").MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}
