// SpaceOS.Kernel.Application/StageRegistry/Commands/AssignChainCommandValidator.cs
using FluentValidation;

namespace SpaceOS.Kernel.Application.StageRegistry.Commands;

/// <summary>FluentValidation rules for <see cref="AssignChainCommand"/>.</summary>
internal sealed class AssignChainCommandValidator : AbstractValidator<AssignChainCommand>
{
    /// <summary>Initialises validation rules for <see cref="AssignChainCommand"/>.</summary>
    public AssignChainCommandValidator()
    {
        RuleFor(x => x.FlowEpicId)
            .NotEmpty().WithMessage("FlowEpicId is required.");

        RuleFor(x => x.ChainTemplateId)
            .NotEmpty().WithMessage("ChainTemplateId is required.");

        RuleFor(x => x.FirstStageCode)
            .NotEmpty().WithMessage("FirstStageCode is required.")
            .MaximumLength(30).WithMessage("FirstStageCode must not exceed 30 characters.");
    }
}
