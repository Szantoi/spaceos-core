using SpaceOS.Modules.Inventory.Domain.Common;
using SpaceOS.Modules.Inventory.Domain.Enums;

namespace SpaceOS.Modules.Inventory.Domain.Aggregates;

// Append-only — no UPDATE, no DELETE
public class StockMovement : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public MovementType MovementType { get; private set; }
    public Guid MaterialCatalogId { get; private set; }
    public decimal Quantity { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public string Reference { get; private set; } = string.Empty;

    private StockMovement() { }

    public static StockMovement Record(
        Guid tenantId,
        MovementType movementType,
        Guid materialCatalogId,
        decimal quantity,
        DateTime occurredAt,
        string reference)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId required.", nameof(tenantId));
        if (materialCatalogId == Guid.Empty) throw new ArgumentException("MaterialCatalogId required.", nameof(materialCatalogId));
        if (quantity <= 0) throw new ArgumentException("Quantity must be positive.", nameof(quantity));

        return new StockMovement
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            MovementType = movementType,
            MaterialCatalogId = materialCatalogId,
            Quantity = quantity,
            OccurredAt = occurredAt,
            Reference = reference
        };
    }
}
