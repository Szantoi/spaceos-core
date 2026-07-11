using SpaceOS.Modules.Contracts.Inventory.Enums;
using SpaceOS.Modules.Contracts.Shared;

namespace SpaceOS.Modules.Contracts.Inventory.Events;

/// <summary>Raised when stock has been consumed and the inventory ledger has been updated.</summary>
public sealed record StockConsumed : ModuleEvent
{
    /// <summary>Gets the material code of the consumed stock.</summary>
    public required string MaterialCode { get; init; }

    /// <summary>Gets the number of units consumed.</summary>
    public required int Quantity { get; init; }

    /// <summary>Gets the type of the document that triggered consumption.</summary>
    public required StockReferenceType ReferenceType { get; init; }

    /// <summary>Gets the identifier of the triggering document, if available.</summary>
    public required Guid? ReferenceId { get; init; }
}
