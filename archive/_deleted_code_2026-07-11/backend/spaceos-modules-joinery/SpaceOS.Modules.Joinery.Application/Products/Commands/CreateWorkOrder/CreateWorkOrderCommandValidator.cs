using FluentValidation;

namespace SpaceOS.Modules.Joinery.Application.Products.Commands.CreateWorkOrder;

public sealed class CreateWorkOrderCommandValidator : AbstractValidator<CreateWorkOrderCommand>
{
    public CreateWorkOrderCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEqual(Guid.Empty).WithMessage("TenantId cannot be empty.");

        RuleFor(x => x.ConfigurationId)
            .NotEqual(Guid.Empty).WithMessage("ConfigurationId cannot be empty.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0.")
            .LessThanOrEqualTo(1000).WithMessage("Quantity cannot exceed 1000.");

        RuleFor(x => x.DeliveryDate)
            .GreaterThan(DateOnly.FromDateTime(DateTime.UtcNow)).WithMessage("Delivery date must be in the future.");

        RuleFor(x => x.CustomerRef)
            .MaximumLength(100).WithMessage("CustomerRef cannot exceed 100 characters.");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes cannot exceed 2000 characters.");
    }
}
