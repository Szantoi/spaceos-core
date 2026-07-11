namespace SpaceOS.Modules.Contracts.Procurement.DTOs;

/// <summary>
/// Represents a single line of an actual delivery against a purchase order line.
/// QualityNote: max 2000 chars.
/// </summary>
public sealed record DeliveryLineDto(
    string MaterialCode,
    int QuantityOrdered,
    int QuantityReceived,
    string? QualityNote);
