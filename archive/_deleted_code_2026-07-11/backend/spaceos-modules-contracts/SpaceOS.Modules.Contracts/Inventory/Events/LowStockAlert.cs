using SpaceOS.Modules.Contracts.Shared;

namespace SpaceOS.Modules.Contracts.Inventory.Events;

/// <summary>Raised when a material's stock level falls at or below the configured reorder threshold.</summary>
public sealed record LowStockAlert : ModuleEvent
{
    /// <summary>Gets the material code whose stock is low.</summary>
    public required string MaterialCode { get; init; }

    /// <summary>Gets the current available quantity at the time the alert was raised.</summary>
    public required int CurrentQuantity { get; init; }

    /// <summary>Gets the configured minimum threshold that triggered the alert.</summary>
    public required int ThresholdQuantity { get; init; }
}
