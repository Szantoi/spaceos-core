using SpaceOS.Modules.Sales.Domain.Enums;

namespace SpaceOS.Modules.Sales.Application.DTOs;

/// <summary>Sales funnel aggregate — count and gross amount per status.</summary>
public sealed record SalesFunnelResponse(IReadOnlyList<FunnelStageDto> Stages);

/// <summary>One stage in the funnel.</summary>
public sealed record FunnelStageDto(QuoteStatus Status, int Count, decimal TotalGrossAmount);

/// <summary>Quote acceptance and conversion rate metrics.</summary>
public sealed record ConversionRateResponse(
    int TotalSent,
    int TotalAccepted,
    int TotalConverted,
    decimal SentToAcceptedRate,
    decimal AcceptedToConvertedRate);
