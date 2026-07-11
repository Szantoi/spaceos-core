// SpaceOS.Kernel.Application/StageRegistry/Commands/RemoveStageChainStepCommandValidator.cs
using FluentValidation;

namespace SpaceOS.Kernel.Application.StageRegistry.Commands;

/// <summary>FluentValidation rules for <see cref="RemoveStageChainStepCommand"/>.</summary>
internal sealed class RemoveStageChainStepCommandValidator : AbstractValidator<RemoveStageChainStepCommand>
{
    /// <summary>Initialises validation rules for <see cref="RemoveStageChainStepCommand"/>.</summary>
    public RemoveStageChainStepCommandValidator()
    {
        RuleFor(x => x.ChainTemplateId)
            .NotEmpty().WithMessage("ChainTemplateId is required.");

        RuleFor(x => x.StageCode)
            .NotEmpty().WithMessage("StageCode is required.")
            .MaximumLength(30).WithMessage("StageCode must not exceed 30 characters.");
    }
}
