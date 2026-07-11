using FluentValidation;

namespace SpaceOS.Modules.Joinery.Application.Products.Commands.ConfigureProduct;

public sealed class ConfigureProductCommandValidator : AbstractValidator<ConfigureProductCommand>
{
    public ConfigureProductCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEqual(Guid.Empty).WithMessage("TenantId cannot be empty.");

        RuleFor(x => x.ProductType)
            .NotEmpty().WithMessage("ProductType is required.")
            .MaximumLength(50).WithMessage("ProductType cannot exceed 50 characters.");

        RuleFor(x => x.Dimensions)
            .NotNull().WithMessage("Dimensions are required.");

        RuleFor(x => x.Dimensions.Width)
            .GreaterThan(0).WithMessage("Width must be greater than 0.");

        RuleFor(x => x.Dimensions.Height)
            .GreaterThan(0).WithMessage("Height must be greater than 0.");

        RuleFor(x => x.Dimensions.Thickness)
            .GreaterThan(0).WithMessage("Thickness must be greater than 0.");

        RuleFor(x => x.Materials)
            .NotNull().WithMessage("Materials are required.");

        RuleFor(x => x.Materials.Core)
            .NotEmpty().WithMessage("Core material is required.");

        RuleFor(x => x.Fittings)
            .NotNull().WithMessage("Fittings are required.");

        RuleFor(x => x.Fittings.Hinge)
            .NotEmpty().WithMessage("Hinge fitting is required.");
    }
}
