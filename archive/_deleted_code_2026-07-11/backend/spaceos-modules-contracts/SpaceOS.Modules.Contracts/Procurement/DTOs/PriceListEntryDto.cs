namespace SpaceOS.Modules.Contracts.Procurement.DTOs;

/// <summary>
/// A single entry from a supplier's current price list for a material.
/// Currency: ISO 4217 format (e.g. HUF, EUR, USD).
/// MinimumQuantity: null if no minimum order quantity required.
/// </summary>
public sealed record PriceListEntryDto(
    Guid SupplierId,
    string SupplierName,
    string MaterialCode,
    decimal UnitPrice,
    string Currency,
    int? MinimumQuantity,
    DateTimeOffset? ValidUntil);
