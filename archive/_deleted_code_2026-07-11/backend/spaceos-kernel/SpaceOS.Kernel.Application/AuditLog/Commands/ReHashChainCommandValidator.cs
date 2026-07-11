// SpaceOS.Kernel.Application/AuditLog/Commands/ReHashChainCommandValidator.cs

using FluentValidation;
using SpaceOS.Kernel.Domain.AuditLog;

namespace SpaceOS.Kernel.Application.AuditLog.Commands;

/// <summary>Validates <see cref="ReHashChainCommand"/> input before the handler is invoked.</summary>
internal sealed class ReHashChainCommandValidator : AbstractValidator<ReHashChainCommand>
{
    /// <summary>Initialises validation rules for <see cref="ReHashChainCommand"/>.</summary>
    public ReHashChainCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty();

        RuleFor(x => x.TargetAlgorithm)
            .IsInEnum()
            .WithMessage("TargetAlgorithm must be a valid HashAlgorithmType value.");
    }
}
