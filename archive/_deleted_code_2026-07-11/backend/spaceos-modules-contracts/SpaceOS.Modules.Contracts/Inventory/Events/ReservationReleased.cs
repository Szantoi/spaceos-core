using SpaceOS.Modules.Contracts.Shared;

namespace SpaceOS.Modules.Contracts.Inventory.Events;

/// <summary>Published when an active reservation is explicitly released by the consumer.</summary>
/// <remarks>Consumers MUST verify Event.TenantId matches their JWT TenantId (SEC-03).</remarks>
public sealed record ReservationReleased : ModuleEvent
{
    /// <summary>Gets the identifier of the released reservation.</summary>
    public required Guid ReservationId { get; init; }

    /// <summary>Gets the consumer-provided idempotency key of the released reservation.</summary>
    public required Guid CorrelationId { get; init; }

    /// <summary>Gets the optional reason provided by the consumer at release time.</summary>
    public required string? Reason { get; init; }
}
