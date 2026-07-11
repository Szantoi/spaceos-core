// SpaceOS.Kernel.Application/StageRegistry/Commands/AdvanceFlowEpicStageCommandValidator.cs
using FluentValidation;

namespace SpaceOS.Kernel.Application.StageRegistry.Commands;

/// <summary>FluentValidation rules for <see cref="AdvanceFlowEpicStageCommand"/>.</summary>
internal sealed class AdvanceFlowEpicStageCommandValidator : AbstractValidator<AdvanceFlowEpicStageCommand>
{
    /// <summary>Initialises validation rules for <see cref="AdvanceFlowEpicStageCommand"/>.</summary>
    public AdvanceFlowEpicStageCommandValidator()
    {
        RuleFor(x => x.FlowEpicId)
            .NotEmpty().WithMessage("FlowEpicId is required.");

        RuleFor(x => x.TargetStageCode)
            .NotEmpty().WithMessage("TargetStageCode is required.")
            .MaximumLength(30).WithMessage("TargetStageCode must not exceed 30 characters.");
    }
}
