using SpaceOS.Modules.Contracts.Shared;

namespace SpaceOS.Modules.Contracts.Procurement.Events;

/// <summary>Raised when a new purchase order has been successfully created.</summary>
public sealed record PurchaseOrderCreated : ModuleEvent
{
    /// <summary>Gets the identifier of the newly created purchase order.</summary>
    public required Guid OrderId { get; init; }

    /// <summary>Gets the identifier of the supplier the order was placed with.</summary>
    public required Guid SupplierId { get; init; }
}
