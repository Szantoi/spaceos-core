using FluentValidation;
using SpaceOS.Kernel.Application.Common;

namespace SpaceOS.Kernel.Application.SpaceLayers.Commands;

/// <summary>
/// Validates <see cref="UpdateSpaceLayerIntentDataCommand"/> input, including per-<see cref="SpaceOS.Kernel.Domain.Enums.TradeType"/>
/// structural JSON schema validation via <see cref="IntentDataSchemaValidator"/>.
/// </summary>
public class UpdateSpaceLayerIntentDataCommandValidator : AbstractValidator<UpdateSpaceLayerIntentDataCommand>
{
    /// <summary>Initialises a new <see cref="UpdateSpaceLayerIntentDataCommandValidator"/>.</summary>
    public UpdateSpaceLayerIntentDataCommandValidator()
    {
        RuleFor(x => x.SpaceLayerId)
            .NotEmpty().WithMessage("SpaceLayerId is required.");

        RuleFor(x => x.IntentDataJson)
            .NotEmpty().WithMessage("IntentDataJson is required.");

        RuleFor(x => x)
            .Must(cmd => IntentDataSchemaValidator.Validate(cmd.IntentDataJson, cmd.TradeType) is null)
            .When(cmd => !string.IsNullOrWhiteSpace(cmd.IntentDataJson))
            .WithName("IntentDataJson")
            .WithMessage(cmd => IntentDataSchemaValidator.Validate(cmd.IntentDataJson, cmd.TradeType)
                                ?? "IntentDataJson schema validation failed.");
    }
}
