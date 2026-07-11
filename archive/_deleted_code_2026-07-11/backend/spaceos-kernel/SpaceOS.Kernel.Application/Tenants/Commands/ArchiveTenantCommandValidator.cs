// SpaceOS.Kernel.Application/Tenants/Commands/ArchiveTenantCommandValidator.cs
using FluentValidation;

namespace SpaceOS.Kernel.Application.Tenants.Commands;

/// <summary>Validates <see cref="ArchiveTenantCommand"/> input.</summary>
internal sealed class ArchiveTenantCommandValidator : AbstractValidator<ArchiveTenantCommand>
{
    /// <summary>Initialises validation rules for <see cref="ArchiveTenantCommand"/>.</summary>
    public ArchiveTenantCommandValidator() { RuleFor(x => x.Id).NotEmpty(); }
}
