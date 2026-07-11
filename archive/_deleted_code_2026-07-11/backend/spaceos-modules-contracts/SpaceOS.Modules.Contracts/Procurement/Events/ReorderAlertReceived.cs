using SpaceOS.Modules.Contracts.Shared;

namespace SpaceOS.Modules.Contracts.Procurement.Events;

/// <summary>Raised when the procurement system has detected that a material needs to be reordered.</summary>
public sealed record ReorderAlertReceived : ModuleEvent
{
    /// <summary>Gets the material code that needs reordering.</summary>
    public required string MaterialCode { get; init; }

    /// <summary>Gets the current stock quantity at the time the alert was generated.</summary>
    public required int CurrentQuantity { get; init; }

    /// <summary>Gets the suggested order quantity based on consumption trends and lead time.</summary>
    public required int SuggestedQuantity { get; init; }
}
