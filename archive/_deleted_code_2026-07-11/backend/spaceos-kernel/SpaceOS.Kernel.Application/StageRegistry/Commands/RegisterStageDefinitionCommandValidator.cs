// SpaceOS.Kernel.Application/StageRegistry/Commands/RegisterStageDefinitionCommandValidator.cs
using FluentValidation;

namespace SpaceOS.Kernel.Application.StageRegistry.Commands;

/// <summary>FluentValidation rules for <see cref="RegisterStageDefinitionCommand"/>.</summary>
internal sealed class RegisterStageDefinitionCommandValidator : AbstractValidator<RegisterStageDefinitionCommand>
{
    private const string StageCodePattern = @"^[a-z][a-z0-9_]{1,28}[a-z0-9]$";

    /// <summary>Initialises validation rules for <see cref="RegisterStageDefinitionCommand"/>.</summary>
    public RegisterStageDefinitionCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("TenantId is required.");

        RuleFor(x => x.StageCode)
            .NotEmpty().WithMessage("StageCode is required.")
            .MaximumLength(30).WithMessage("StageCode must not exceed 30 characters.")
            .Matches(StageCodePattern).WithMessage("StageCode must match ^[a-z][a-z0-9_]{1,28}[a-z0-9]$");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("DisplayName is required.")
            .MaximumLength(100).WithMessage("DisplayName must not exceed 100 characters.");

        RuleFor(x => x.ModuleEndpoint)
            .NotEmpty().WithMessage("ModuleEndpoint is required.")
            .MaximumLength(500).WithMessage("ModuleEndpoint must not exceed 500 characters.");
    }
}
