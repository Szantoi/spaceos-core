// SpaceOS.Kernel.Application/StageRegistry/Commands/UpdateStageDefinitionCommandValidator.cs
using FluentValidation;

namespace SpaceOS.Kernel.Application.StageRegistry.Commands;

/// <summary>FluentValidation rules for <see cref="UpdateStageDefinitionCommand"/>.</summary>
internal sealed class UpdateStageDefinitionCommandValidator : AbstractValidator<UpdateStageDefinitionCommand>
{
    /// <summary>Initialises validation rules for <see cref="UpdateStageDefinitionCommand"/>.</summary>
    public UpdateStageDefinitionCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Stage definition Id is required.");

        RuleFor(x => x.ModuleEndpoint)
            .NotEmpty().WithMessage("ModuleEndpoint is required.")
            .MaximumLength(500).WithMessage("ModuleEndpoint must not exceed 500 characters.");
    }
}
