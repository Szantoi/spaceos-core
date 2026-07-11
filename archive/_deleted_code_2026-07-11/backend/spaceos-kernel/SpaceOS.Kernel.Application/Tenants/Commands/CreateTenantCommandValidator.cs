using FluentValidation;
using SpaceOS.Kernel.Domain.Enums;

namespace SpaceOS.Kernel.Application.Tenants.Commands;

/// <summary>
/// Validates <see cref="CreateTenantCommand"/> requests.
/// </summary>
public class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    /// <summary>Initialises validation rules for <see cref="CreateTenantCommand"/>.</summary>
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tenant name is required.")
            .MaximumLength(100).WithMessage("Tenant name cannot exceed 100 characters.");

        RuleFor(x => x.TenantType)
            .IsInEnum().WithMessage("TenantType must be a valid ecosystem actor type.");
    }
}
