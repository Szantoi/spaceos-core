namespace SpaceOS.Modules.Contracts.Procurement.DTOs;

/// <summary>
/// Represents a single line item within a purchase order.
/// Currency: ISO 4217, max 3 chars (CD-08).
/// </summary>
public sealed record PurchaseOrderLineDto(
    string MaterialCode,
    int Quantity,
    decimal? UnitPrice,
    string? Currency);
