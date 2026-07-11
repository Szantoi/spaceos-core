using SpaceOS.Modules.Contracts.Shared;

namespace SpaceOS.Modules.Contracts.Cutting.Events;

/// <summary>Raised when physical cutting of a sheet has been completed on the shop floor.</summary>
public sealed record CuttingCompleted : ModuleEvent
{
    /// <summary>Gets the identifier of the completed cutting sheet.</summary>
    public required Guid SheetId { get; init; }

    /// <summary>Gets the identifier of the operator who completed the cutting.</summary>
    public required Guid OperatorId { get; init; }

    /// <summary>Gets the identifiers of offcut stock items registered after cutting.</summary>
    public required IReadOnlyList<Guid> OffcutIds { get; init; }
}
