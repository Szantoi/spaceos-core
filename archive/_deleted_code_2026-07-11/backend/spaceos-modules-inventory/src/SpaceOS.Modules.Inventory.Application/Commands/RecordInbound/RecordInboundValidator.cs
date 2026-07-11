using FluentValidation;

namespace SpaceOS.Modules.Inventory.Application.Commands.RecordInbound;

public sealed class RecordInboundValidator : AbstractValidator<RecordInboundCommand>
{
    public RecordInboundValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.MaterialType).NotEmpty();
        RuleFor(x => x.Area).GreaterThan(0);
        RuleFor(x => x.PanelCount).GreaterThan(0);
        RuleFor(x => x.Reference).NotEmpty();
        RuleFor(x => x.OccurredAt).NotEqual(default(DateTime));
    }
}
