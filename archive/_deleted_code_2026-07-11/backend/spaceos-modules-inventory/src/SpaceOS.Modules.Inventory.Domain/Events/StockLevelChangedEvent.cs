using SpaceOS.Modules.Inventory.Domain.Common;

namespace SpaceOS.Modules.Inventory.Domain.Events;

public sealed record StockLevelChangedEvent(
    Guid PanelStockId,
    Guid TenantId,
    Guid MaterialCatalogId,
    int NewQuantity) : IDomainEvent;
