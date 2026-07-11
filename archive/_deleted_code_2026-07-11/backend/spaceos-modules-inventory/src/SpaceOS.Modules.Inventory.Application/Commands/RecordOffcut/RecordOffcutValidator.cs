using FluentValidation;

namespace SpaceOS.Modules.Inventory.Application.Commands.RecordOffcut;

public sealed class RecordOffcutValidator : AbstractValidator<RecordOffcutCommand>
{
    public RecordOffcutValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.MaterialType).NotEmpty();
        RuleFor(x => x.WidthMm).GreaterThan(0);
        RuleFor(x => x.HeightMm).GreaterThan(0);
    }
}
