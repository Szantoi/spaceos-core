// SpaceOS.Kernel.Application/StageRegistry/Commands/CreateStageChainTemplateCommandValidator.cs
using FluentValidation;

namespace SpaceOS.Kernel.Application.StageRegistry.Commands;

/// <summary>FluentValidation rules for <see cref="CreateStageChainTemplateCommand"/>.</summary>
internal sealed class CreateStageChainTemplateCommandValidator : AbstractValidator<CreateStageChainTemplateCommand>
{
    /// <summary>Initialises validation rules for <see cref="CreateStageChainTemplateCommand"/>.</summary>
    public CreateStageChainTemplateCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("TenantId is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Chain template name is required.")
            .MaximumLength(100).WithMessage("Chain template name must not exceed 100 characters.");
    }
}
