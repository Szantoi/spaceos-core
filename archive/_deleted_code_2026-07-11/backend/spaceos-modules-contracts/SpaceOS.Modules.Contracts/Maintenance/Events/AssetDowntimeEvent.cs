using MediatR;
using SpaceOS.Modules.Contracts.Shared;

namespace SpaceOS.Modules.Contracts.Maintenance.Events;

/// <summary>
/// Raised when a production asset (machine, tool, equipment) goes down and becomes unavailable.
/// This event triggers production schedule adjustments in downstream modules.
/// </summary>
public sealed record AssetDowntimeEvent : ModuleEvent, INotification
{
    /// <summary>Gets the identifier of the asset that went down.</summary>
    public required Guid AssetId { get; init; }

    /// <summary>Gets the asset name (for logging/display).</summary>
    public required string AssetName { get; init; }

    /// <summary>Gets the reason for downtime (e.g., "Maintenance", "Breakdown", "Safety Issue").</summary>
    public required string Reason { get; init; }

    /// <summary>Gets the estimated date when the asset will be fixed and available again.</summary>
    public required DateTimeOffset? EstimatedFixDate { get; init; }

    /// <summary>Gets the identifier of the maintenance ticket or work order.</summary>
    public Guid? MaintenanceTicketId { get; init; }
}
