using FluentValidation;

namespace SpaceOS.Modules.Joinery.Application.Orders.Commands.CreateOrderFromConversion;

public sealed class CreateOrderFromConversionValidator
    : AbstractValidator<CreateOrderFromConversionCommand>
{
    public CreateOrderFromConversionValidator()
    {
        RuleFor(x => x.QuoteId).NotEmpty();
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Currency).NotEmpty().Length(3)
            .Matches("^[A-Z]{3}$").WithMessage("Currency: ISO 4217 uppercase required.");
        RuleFor(x => x.TotalNet).GreaterThan(0);
        RuleFor(x => x.TotalGross).GreaterThan(0);
        RuleFor(x => x.ContentHash).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one line required.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.Description).NotEmpty().MaximumLength(500);
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.UnitPriceNet).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.VatRate).InclusiveBetween(0m, 1m);
            line.RuleFor(l => l.DiscountPercent)
                .InclusiveBetween(0m, 100m)
                .When(l => l.DiscountPercent.HasValue);
            line.RuleFor(l => l.SortOrder).GreaterThanOrEqualTo(0);
        });
    }
}
