using SpaceOS.Modules.Contracts.Shared;

namespace SpaceOS.Modules.Contracts.Cutting.Events;

/// <summary>Raised when nesting optimization has completed for a cutting sheet.</summary>
public sealed record NestingCompleted : ModuleEvent
{
    /// <summary>Gets the identifier of the cutting sheet that was nested.</summary>
    public required Guid SheetId { get; init; }

    /// <summary>Gets the number of stock panels consumed by the nesting run.</summary>
    public required int PanelsUsed { get; init; }

    /// <summary>Gets the overall waste percentage (0–100) achieved by nesting.</summary>
    public required decimal WastePercentage { get; init; }
}
