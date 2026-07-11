using FluentValidation;

namespace SpaceOS.Modules.Inventory.Application.Commands.RecordConsumption;

public sealed class RecordConsumptionValidator : AbstractValidator<RecordConsumptionCommand>
{
    public RecordConsumptionValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.MaterialType).NotEmpty();
        RuleFor(x => x.Area).GreaterThan(0);
        RuleFor(x => x.PanelCount).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty();
        RuleFor(x => x.OccurredAt).NotEqual(default(DateTime));
    }
}
