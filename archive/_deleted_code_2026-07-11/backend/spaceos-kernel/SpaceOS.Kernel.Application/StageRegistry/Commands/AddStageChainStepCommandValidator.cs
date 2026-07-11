// SpaceOS.Kernel.Application/StageRegistry/Commands/AddStageChainStepCommandValidator.cs
using FluentValidation;

namespace SpaceOS.Kernel.Application.StageRegistry.Commands;

/// <summary>FluentValidation rules for <see cref="AddStageChainStepCommand"/>.</summary>
internal sealed class AddStageChainStepCommandValidator : AbstractValidator<AddStageChainStepCommand>
{
    /// <summary>Initialises validation rules for <see cref="AddStageChainStepCommand"/>.</summary>
    public AddStageChainStepCommandValidator()
    {
        RuleFor(x => x.ChainTemplateId)
            .NotEmpty().WithMessage("ChainTemplateId is required.");

        RuleFor(x => x.StageDefinitionId)
            .NotEmpty().WithMessage("StageDefinitionId is required.");

        RuleFor(x => x.SortOrder)
            .GreaterThan(0).WithMessage("SortOrder must be greater than 0.");
    }
}
