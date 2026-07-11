using SpaceOS.Modules.Contracts.Shared;

namespace SpaceOS.Modules.Contracts.Inventory.Events;

/// <summary>Raised when inbound stock has been recorded in the inventory system.</summary>
public sealed record StockReceived : ModuleEvent
{
    /// <summary>Gets the material code of the received stock.</summary>
    public required string MaterialCode { get; init; }

    /// <summary>Gets the number of units received.</summary>
    public required int Quantity { get; init; }

    /// <summary>Gets the supplier delivery reference number, if available.</summary>
    public required string? DeliveryReference { get; init; }
}
