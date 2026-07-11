// SpaceOS.Kernel.Application/Sync/Commands/ReceiveSignal/ReceiveSyncSignalCommandValidator.cs
using FluentValidation;
using SpaceOS.Kernel.Domain.Enums;

namespace SpaceOS.Kernel.Application.Sync.Commands.ReceiveSignal;

/// <summary>Validates <see cref="ReceiveSyncSignalCommand"/> input before the handler executes.</summary>
internal sealed class ReceiveSyncSignalCommandValidator : AbstractValidator<ReceiveSyncSignalCommand>
{
    private static readonly IReadOnlySet<string> ValidStates =
        Enum.GetNames<WorkflowPhase>().ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>Initialises validation rules for <see cref="ReceiveSyncSignalCommand"/>.</summary>
    public ReceiveSyncSignalCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty();

        RuleFor(x => x.EpicId)
            .NotEmpty();

        RuleFor(x => x.NewState)
            .NotEmpty()
            .Must(s => ValidStates.Contains(s))
            .WithMessage($"NewState must be one of: {string.Join(", ", Enum.GetNames<WorkflowPhase>())}.");

        RuleFor(x => x.ClientSignalId)
            .NotEmpty();

        RuleFor(x => x.PayloadJson)
            .NotEmpty();
    }
}
