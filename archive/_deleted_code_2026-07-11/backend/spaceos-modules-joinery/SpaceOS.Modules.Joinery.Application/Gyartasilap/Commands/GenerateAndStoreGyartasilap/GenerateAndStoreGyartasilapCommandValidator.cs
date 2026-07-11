using FluentValidation;

namespace SpaceOS.Modules.Joinery.Application.Gyartasilap.Commands.GenerateAndStoreGyartasilap;

public sealed class GenerateAndStoreGyartasilapCommandValidator
    : AbstractValidator<GenerateAndStoreGyartasilapCommand>
{
    private static readonly string[] ValidVariants = { "L1", "L2", "L3", "L4" };

    public GenerateAndStoreGyartasilapCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.JoineryOrderId).NotEmpty();
        RuleFor(x => x.LabelVariant)
            .NotEmpty()
            .Must(v => ValidVariants.Contains(v, StringComparer.OrdinalIgnoreCase))
            .WithMessage("LabelVariant must be L1, L2, L3, or L4.");
    }
}
