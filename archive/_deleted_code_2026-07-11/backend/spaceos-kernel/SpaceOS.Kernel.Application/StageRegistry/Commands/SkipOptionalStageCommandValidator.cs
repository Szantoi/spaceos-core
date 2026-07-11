// SpaceOS.Kernel.Application/StageRegistry/Commands/SkipOptionalStageCommandValidator.cs
using FluentValidation;

namespace SpaceOS.Kernel.Application.StageRegistry.Commands;

/// <summary>FluentValidation rules for <see cref="SkipOptionalStageCommand"/>.</summary>
internal sealed class SkipOptionalStageCommandValidator : AbstractValidator<SkipOptionalStageCommand>
{
    /// <summary>Initialises validation rules for <see cref="SkipOptionalStageCommand"/>.</summary>
    public SkipOptionalStageCommandValidator()
    {
        RuleFor(x => x.FlowEpicId)
            .NotEmpty().WithMessage("FlowEpicId is required.");

        RuleFor(x => x.StageCode)
            .NotEmpty().WithMessage("StageCode is required.")
            .MaximumLength(30).WithMessage("StageCode must not exceed 30 characters.");
    }
}
