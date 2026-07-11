using Ardalis.Result;
using SpaceOS.Modules.Sales.Domain.Enums;
using SpaceOS.Modules.Sales.Domain.ValueObjects;

namespace SpaceOS.Modules.Sales.Domain.Entities;

/// <summary>
/// Owned line item on a Quote. All totals are Domain-computed (RULE 1).
/// Immutable after the parent Quote leaves Draft status (enforced by DB trigger DB-S-02).
/// </summary>
public sealed class QuoteLine
{
    /// <summary>Line identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Parent quote reference.</summary>
    public Guid QuoteId { get; private set; }

    /// <summary>Owning tenant (cross-tenant FK guard).</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Nature of the line item.</summary>
    public QuoteLineType LineType { get; private set; }

    /// <summary>Optional soft reference to a product template in Abstractions.</summary>
    public Guid? SourceTemplateId { get; private set; }

    /// <summary>Frozen snapshot description (max 500 chars).</summary>
    public string Description { get; private set; } = default!;

    /// <summary>Quantity (must be > 0).</summary>
    public decimal Quantity { get; private set; }

    /// <summary>Unit price (currency-tagged).</summary>
    public Money UnitPrice { get; internal set; }

    /// <summary>VAT rate as a fraction (0..1, e.g. 0.27 for HU 27%).</summary>
    public decimal VatRate { get; private set; }

    /// <summary>Optional discount fraction (0..1). Null means no discount.</summary>
    public decimal? DiscountPercent { get; private set; }

    /// <summary>Display order within the quote.</summary>
    public int SortOrder { get; private set; }

    /// <summary>Net line total (after discount, before VAT) — Domain computed.</summary>
    public Money LineNet { get; internal set; }

    /// <summary>VAT amount — Domain computed.</summary>
    public Money LineVat { get; internal set; }

    /// <summary>Gross line total — Domain computed.</summary>
    public Money LineGross { get; internal set; }

    /// <summary>ISO 4217 currency (derived from UnitPrice).</summary>
    public string Currency => UnitPrice.Currency;

    private QuoteLine() { } // EF Core

    /// <summary>
    /// BE-S-04: Restores Currency into UnitPrice, LineNet, LineVat, LineGross Money structs
    /// after EF Core materialises this entity. Called by Quote.FixMoneyCurrency().
    /// </summary>
    public void FixUnitPriceCurrency(string currency)
    {
        UnitPrice = new Money(UnitPrice.Amount, currency);
        LineNet   = new Money(LineNet.Amount,   currency);
        LineVat   = new Money(LineVat.Amount,   currency);
        LineGross = new Money(LineGross.Amount, currency);
    }

    /// <summary>
    /// Creates a new QuoteLine, computing all totals in the Domain layer (RULE 1).
    /// </summary>
    public static Result<QuoteLine> Create(
        Guid tenantId,
        QuoteLineType type,
        Guid? sourceTemplateId,
        string description,
        decimal quantity,
        Money unitPrice,
        decimal vatRate,
        decimal? discountPercent,
        int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(description) || description.Length > 500)
            return Result.Invalid(new ValidationError("Description: 1..500 char."));
        if (quantity <= 0m)
            return Result.Invalid(new ValidationError("Quantity must be > 0."));
        if (unitPrice.Amount < 0m)
            return Result.Invalid(new ValidationError("UnitPrice must be >= 0."));
        if (vatRate < 0m || vatRate > 1m)
            return Result.Invalid(new ValidationError("VatRate must be 0..1."));
        if (discountPercent.HasValue && (discountPercent < 0m || discountPercent > 1m))
            return Result.Invalid(new ValidationError("DiscountPercent must be 0..1."));

        var grossBeforeDiscount = unitPrice.Amount * quantity;
        var discountAmount = grossBeforeDiscount * (discountPercent ?? 0m);
        var net = Math.Round(grossBeforeDiscount - discountAmount, 2, MidpointRounding.AwayFromZero);
        var vat = Math.Round(net * vatRate, 2, MidpointRounding.AwayFromZero);
        var gross = net + vat;

        return Result.Success(new QuoteLine
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LineType = type,
            SourceTemplateId = sourceTemplateId,
            Description = description,
            Quantity = quantity,
            UnitPrice = unitPrice,
            VatRate = vatRate,
            DiscountPercent = discountPercent,
            SortOrder = sortOrder,
            LineNet = new Money(net, unitPrice.Currency),
            LineVat = new Money(vat, unitPrice.Currency),
            LineGross = new Money(gross, unitPrice.Currency)
        });
    }
}
