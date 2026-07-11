using SpaceOS.Modules.Inventory.Domain.Common;

namespace SpaceOS.Modules.Inventory.Domain.Events;

public sealed record LowStockAlertEvent(
    Guid PanelStockId,
    Guid TenantId,
    Guid MaterialCatalogId,
    int CurrentQuantity) : IDomainEvent;
