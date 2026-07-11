namespace SpaceOS.Modules.Sales.Abstractions.Contracts;

/// <summary>
/// Payload sent to the Joinery internal endpoint for order creation (ADR-039, D-11).
/// </summary>
public sealed record OrderConversionRequest(
    Guid QuoteId,
    Guid TenantId,
    Guid CustomerId,
    Guid? LinkedTenantId,
    string Currency,
    decimal TotalNet,
    decimal TotalVat,
    decimal TotalGross,
    IReadOnlyList<OrderConversionLine> Lines,
    string ContentHash);

/// <summary>Single line item in an <see cref="OrderConversionRequest"/>.</summary>
public sealed record OrderConversionLine(
    Guid? SourceTemplateId,
    string Description,
    decimal Quantity,
    decimal UnitPriceNet,
    decimal VatRate,
    decimal? DiscountPercent,
    int SortOrder);

/// <summary>Result returned by the Joinery internal endpoint after successful order creation.</summary>
public sealed record OrderConversionResult(Guid OrderId, DateTimeOffset CreatedAt);
