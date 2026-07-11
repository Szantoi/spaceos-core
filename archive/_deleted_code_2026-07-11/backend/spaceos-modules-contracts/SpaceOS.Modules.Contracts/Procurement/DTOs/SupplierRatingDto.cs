namespace SpaceOS.Modules.Contracts.Procurement.DTOs;

/// <summary>
/// Aggregated performance rating for a supplier based on historical order data.
/// DeliveryAccuracy, QualityScore, PriceCompetitiveness: scores in range 0 to 1.
/// </summary>
public sealed record SupplierRatingDto(
    Guid SupplierId,
    decimal DeliveryAccuracy,
    decimal QualityScore,
    decimal PriceCompetitiveness,
    int TotalOrders,
    int LateDeliveries);
