using SpaceOS.Modules.Inventory.Domain.Common;

namespace SpaceOS.Modules.Inventory.Domain.Events;

public sealed record OffcutRegisteredEvent(
    Guid OffcutId,
    Guid TenantId,
    Guid MaterialCatalogId,
    decimal WidthMm,
    decimal HeightMm) : IDomainEvent;
