using SpaceOS.Modules.Sales.Domain.Enums;

namespace SpaceOS.Modules.Sales.Application.DTOs;

/// <summary>Full quote detail response including lines.</summary>
public sealed record QuoteResponse(
    Guid Id,
    Guid TenantId,
    Guid CustomerId,
    string QuoteNumber,
    QuoteStatus Status,
    string Currency,
    DateTimeOffset? ValidUntil,
    string? Notes,
    decimal TotalNetAmount,
    decimal TotalVatAmount,
    decimal TotalGrossAmount,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? SentAt,
    DateTimeOffset? AcceptedAt,
    DateTimeOffset? RejectedAt,
    string? RejectionReason,
    DateTimeOffset? ConvertedAt,
    Guid? ConvertedOrderId,
    DateTimeOffset? ConversionRequestedAt,
    string? ConversionFailureReason,
    string? ContentHash,
    bool IsArchived,
    IReadOnlyList<QuoteLineResponse> Lines);

/// <summary>Summary version for list responses.</summary>
public sealed record QuoteSummary(
    Guid Id,
    Guid CustomerId,
    string QuoteNumber,
    QuoteStatus Status,
    string Currency,
    decimal TotalGrossAmount,
    DateTimeOffset CreatedAt,
    bool IsArchived);

/// <summary>Quote line item response.</summary>
public sealed record QuoteLineResponse(
    Guid Id,
    QuoteLineType LineType,
    Guid? SourceTemplateId,
    string Description,
    decimal Quantity,
    decimal UnitPriceAmount,
    string UnitPriceCurrency,
    decimal VatRate,
    decimal? DiscountPercent,
    decimal LineNetAmount,
    decimal LineVatAmount,
    decimal LineGrossAmount,
    int SortOrder);
