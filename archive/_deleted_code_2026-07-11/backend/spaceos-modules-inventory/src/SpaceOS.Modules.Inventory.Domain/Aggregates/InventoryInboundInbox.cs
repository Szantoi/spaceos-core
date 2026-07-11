namespace SpaceOS.Modules.Inventory.Domain.Aggregates;

/// <summary>
/// Idempotency record for inbound deliveries received from Procurement.
/// Compound unique key: (TenantId, DeliveryLineId).
/// </summary>
public class InventoryInboundInbox
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid DeliveryLineId { get; private set; }
    public string MaterialCode { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public string UnitOfMeasure { get; private set; } = string.Empty;
    public Guid SupplierId { get; private set; }
    public DateTimeOffset ReceivedAt { get; private set; }
    public DateTimeOffset ProcessedAt { get; private set; }

    private InventoryInboundInbox() { }

    public static InventoryInboundInbox Create(
        Guid tenantId,
        Guid deliveryLineId,
        string materialCode,
        decimal quantity,
        string unitOfMeasure,
        Guid supplierId,
        DateTimeOffset receivedAt)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId required.", nameof(tenantId));
        if (deliveryLineId == Guid.Empty) throw new ArgumentException("DeliveryLineId required.", nameof(deliveryLineId));
        ArgumentException.ThrowIfNullOrWhiteSpace(materialCode);

        return new InventoryInboundInbox
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DeliveryLineId = deliveryLineId,
            MaterialCode = materialCode,
            Quantity = quantity,
            UnitOfMeasure = unitOfMeasure,
            SupplierId = supplierId,
            ReceivedAt = receivedAt,
            ProcessedAt = DateTimeOffset.UtcNow
        };
    }
}
