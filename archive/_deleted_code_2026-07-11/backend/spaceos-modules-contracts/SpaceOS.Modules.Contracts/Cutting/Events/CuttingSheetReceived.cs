using SpaceOS.Modules.Contracts.Shared;

namespace SpaceOS.Modules.Contracts.Cutting.Events;

/// <summary>Raised when a cutting sheet has been successfully received and accepted by the provider.</summary>
public sealed record CuttingSheetReceived : ModuleEvent
{
    /// <summary>Gets the identifier of the accepted cutting sheet.</summary>
    public required Guid SheetId { get; init; }

    /// <summary>Gets the identifier of the source entity that submitted the sheet.</summary>
    public required Guid SourceEntityId { get; init; }

    /// <summary>Gets the name of the module type that submitted the sheet.</summary>
    public required string SourceModuleType { get; init; }
}
