namespace SpaceOS.Modules.Contracts.Inventory.Requests;

/// <summary>
/// Input DTO for recording inbound stock from a supplier delivery.
/// No TenantId — resolved from JWT (SEC-01).
/// MaterialCode: max 20 chars. QualityNote: max 2000 chars.
/// </summary>
public sealed record InboundReceiptDto(
    string MaterialCode,
    int Quantity,
    decimal Width,
    decimal Height,
    decimal Thickness,
    string? DeliveryReference,
    string? QualityNote);
