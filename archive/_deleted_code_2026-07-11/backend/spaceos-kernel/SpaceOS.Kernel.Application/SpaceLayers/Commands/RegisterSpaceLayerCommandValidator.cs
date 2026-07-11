using FluentValidation;
using SpaceOS.Kernel.Application.Common;

namespace SpaceOS.Kernel.Application.SpaceLayers.Commands;

/// <summary>
/// FluentValidation rules for <see cref="RegisterSpaceLayerCommand"/>.
/// Enforces conditional URL / JSON requirements based on <c>IsExternalNode</c>.
/// </summary>
public class RegisterSpaceLayerCommandValidator : AbstractValidator<RegisterSpaceLayerCommand>
{
    public RegisterSpaceLayerCommandValidator()
    {
        RuleFor(x => x.FacilityId)
            .NotEmpty().WithMessage("FacilityId is required.");

        RuleFor(x => x.TradeType)
            .IsInEnum().WithMessage("TradeType must be a valid value (Joinery, Plumbing, Electrical, Architecture, Mep).");

        // When IsExternalNode = true, ExternalSourceUrl is mandatory and must be a valid absolute URI.
        When(x => x.IsExternalNode, () =>
        {
            RuleFor(x => x.ExternalSourceUrl)
                .NotEmpty().WithMessage("ExternalSourceUrl is required for a federated (external) SpaceLayer.")
                .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
                .WithMessage("ExternalSourceUrl must be a valid absolute URI.");
        });

        // When IsExternalNode = false, IntentDataJson is mandatory, must be valid JSON,
        // and must pass the per-trade structural schema (scalar parameters, maxProperties: 10).
        When(x => !x.IsExternalNode, () =>
        {
            // Rule 1: presence check — separate RuleFor so When() below does not suppress this.
            // Cascade.Stop prevents NotEmpty firing (and adding a duplicate key) when NotNull already failed.
            RuleFor(x => x.IntentDataJson)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage("IntentDataJson is required for a local SpaceLayer.")
                .NotEmpty().WithMessage("IntentDataJson must not be empty for a local SpaceLayer.");

            // Rule 2: JSON syntax — only when non-empty (avoids duplicate error on null)
            RuleFor(x => x.IntentDataJson)
                .Must(json => JsonValidationHelper.IsValidJson(json))
                .When(x => !string.IsNullOrEmpty(x.IntentDataJson))
                .WithMessage("IntentDataJson must be valid JSON.");

            // Rule 3: structural schema (scalar parameters, maxProperties: 10, size limit)
            RuleFor(x => x)
                .Must(cmd => IntentDataSchemaValidator.Validate(cmd.IntentDataJson!, cmd.TradeType) is null)
                .When(cmd => !string.IsNullOrEmpty(cmd.IntentDataJson) && JsonValidationHelper.IsValidJson(cmd.IntentDataJson))
                .WithName("IntentDataJson")
                .WithMessage(cmd => IntentDataSchemaValidator.Validate(cmd.IntentDataJson!, cmd.TradeType)
                                    ?? "IntentDataJson schema validation failed.");
        });
    }
}
