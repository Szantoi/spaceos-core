// SpaceOS.Kernel.Application/Tenants/Commands/UpdateTenantModulesCommandValidator.cs
using FluentValidation;

namespace SpaceOS.Kernel.Application.Tenants.Commands;

/// <summary>Validates <see cref="UpdateTenantModulesCommand"/> requests.</summary>
public sealed class UpdateTenantModulesCommandValidator : AbstractValidator<UpdateTenantModulesCommand>
{
    /// <summary>Initialises validation rules for <see cref="UpdateTenantModulesCommand"/>.</summary>
    public UpdateTenantModulesCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("TenantId is required.");

        RuleFor(x => x.Modules)
            .NotNull().WithMessage("Modules list is required.");
    }
}
