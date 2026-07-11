namespace SpaceOS.Modules.Contracts.Inventory.DTOs;

/// <summary>
/// Consumption trend analysis for a single material over a given time window.
/// EstimatedDaysUntilStockout is nullable — computed value, may be unavailable (CD-10).
/// Null when insufficient data or stock is at zero.
/// </summary>
public sealed record ConsumptionTrendDto(
    string MaterialCode,
    DateTimeOffset From,
    DateTimeOffset To,
    decimal TotalConsumed,
    decimal AverageDaily,
    int? EstimatedDaysUntilStockout);
