using FluentValidation;

namespace SpaceOS.Kernel.Application.Tenants.Commands;

/// <summary>
/// Validates <see cref="UpdateTenantNameCommand"/> requests.
/// </summary>
public class UpdateTenantNameCommandValidator : AbstractValidator<UpdateTenantNameCommand>
{
    public UpdateTenantNameCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("TenantId is required.");

        RuleFor(x => x.NewName)
            .NotEmpty().WithMessage("New name is required.")
            .MaximumLength(100).WithMessage("Tenant name cannot exceed 100 characters.");
    }
}
