namespace SpaceOS.Modules.Contracts.Procurement.DTOs;

/// <summary>Represents a physical delivery received against a purchase order.</summary>
public sealed record DeliveryDto(
    Guid PurchaseOrderId,
    DateTimeOffset DeliveredAt,
    IReadOnlyList<DeliveryLineDto> Lines);
