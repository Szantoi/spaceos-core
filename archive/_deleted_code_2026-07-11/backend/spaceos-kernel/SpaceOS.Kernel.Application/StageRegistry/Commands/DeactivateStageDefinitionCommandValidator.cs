// SpaceOS.Kernel.Application/StageRegistry/Commands/DeactivateStageDefinitionCommandValidator.cs
using FluentValidation;

namespace SpaceOS.Kernel.Application.StageRegistry.Commands;

/// <summary>FluentValidation rules for <see cref="DeactivateStageDefinitionCommand"/>.</summary>
internal sealed class DeactivateStageDefinitionCommandValidator : AbstractValidator<DeactivateStageDefinitionCommand>
{
    /// <summary>Initialises validation rules for <see cref="DeactivateStageDefinitionCommand"/>.</summary>
    public DeactivateStageDefinitionCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Stage definition Id is required.");
    }
}
