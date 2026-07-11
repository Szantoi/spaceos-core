// SpaceOS.Kernel.Application/StageRegistry/Commands/CreateStageHandoffCommandValidator.cs
using System.Text.Json;
using FluentValidation;

namespace SpaceOS.Kernel.Application.StageRegistry.Commands;

/// <summary>FluentValidation rules for <see cref="CreateStageHandoffCommand"/>.</summary>
internal sealed class CreateStageHandoffCommandValidator : AbstractValidator<CreateStageHandoffCommand>
{
    private const int MaxPayloadBytes   = 1_048_576; // 1 MB — DB-05
    private const int MaxPayloadDepth   = 10;        // SEC-04

    /// <summary>Initialises validation rules for <see cref="CreateStageHandoffCommand"/>.</summary>
    public CreateStageHandoffCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("TenantId is required.");

        RuleFor(x => x.FlowEpicId)
            .NotEmpty().WithMessage("FlowEpicId is required.");

        RuleFor(x => x.SourceStageCode)
            .NotEmpty().WithMessage("SourceStageCode is required.")
            .MaximumLength(30).WithMessage("SourceStageCode must not exceed 30 characters.");

        RuleFor(x => x.TargetStageCode)
            .NotEmpty().WithMessage("TargetStageCode is required.")
            .MaximumLength(30).WithMessage("TargetStageCode must not exceed 30 characters.")
            .NotEqual(x => x.SourceStageCode).WithMessage("TargetStageCode must differ from SourceStageCode.");

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage("IdempotencyKey is required.");

        RuleFor(x => x.PayloadJson)
            .NotEmpty().WithMessage("PayloadJson is required.")
            .Must(json => System.Text.Encoding.UTF8.GetByteCount(json) < MaxPayloadBytes)
                .WithMessage($"PayloadJson must not exceed {MaxPayloadBytes} bytes.")
            .Must(BeWithinDepthLimit)
                .WithMessage($"PayloadJson nesting depth must not exceed {MaxPayloadDepth}.");
    }

    private static bool BeWithinDepthLimit(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return GetMaxDepth(doc.RootElement) <= MaxPayloadDepth;
        }
        catch (JsonException)
        {
            return false; // invalid JSON also fails
        }
    }

    private static int GetMaxDepth(JsonElement element, int currentDepth = 0)
    {
        var max = currentDepth;
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                    max = System.Math.Max(max, GetMaxDepth(property.Value, currentDepth + 1));
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                    max = System.Math.Max(max, GetMaxDepth(item, currentDepth + 1));
                break;
        }
        return max;
    }
}
