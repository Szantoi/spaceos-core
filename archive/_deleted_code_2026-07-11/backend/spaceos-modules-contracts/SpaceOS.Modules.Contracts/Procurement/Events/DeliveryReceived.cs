using SpaceOS.Modules.Contracts.Shared;

namespace SpaceOS.Modules.Contracts.Procurement.Events;

/// <summary>Raised when a physical delivery has been recorded against a purchase order.</summary>
public sealed record DeliveryReceived : ModuleEvent
{
    /// <summary>Gets the identifier of the purchase order that was fulfilled.</summary>
    public required Guid OrderId { get; init; }

    /// <summary>Gets the identifier of the created delivery record.</summary>
    public required Guid DeliveryId { get; init; }
}
