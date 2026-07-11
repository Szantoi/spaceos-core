using SpaceOS.Modules.Contracts.Shared;

namespace SpaceOS.Modules.Contracts.Inventory.Events;

/// <summary>Raised when a cutting offcut has been registered as a reusable stock item.</summary>
public sealed record OffcutRegistered : ModuleEvent
{
    /// <summary>Gets the new stock item identifier assigned to the offcut.</summary>
    public required Guid OffcutId { get; init; }

    /// <summary>Gets the material code of the offcut panel.</summary>
    public required string MaterialCode { get; init; }

    /// <summary>Gets the width of the offcut in millimetres.</summary>
    public required decimal Width { get; init; }

    /// <summary>Gets the height of the offcut in millimetres.</summary>
    public required decimal Height { get; init; }
}
