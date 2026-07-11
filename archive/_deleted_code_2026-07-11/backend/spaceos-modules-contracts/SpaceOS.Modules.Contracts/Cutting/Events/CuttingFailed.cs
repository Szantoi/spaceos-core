using SpaceOS.Modules.Contracts.Shared;

namespace SpaceOS.Modules.Contracts.Cutting.Events;

/// <summary>
/// Raised when cutting of a sheet has failed on the shop floor.
/// Reason: max 2000 chars.
/// </summary>
public sealed record CuttingFailed : ModuleEvent
{
    /// <summary>Gets the identifier of the cutting sheet that failed.</summary>
    public required Guid SheetId { get; init; }

    /// <summary>Gets the human-readable failure reason. Max 2000 characters.</summary>
    public required string Reason { get; init; }
}
