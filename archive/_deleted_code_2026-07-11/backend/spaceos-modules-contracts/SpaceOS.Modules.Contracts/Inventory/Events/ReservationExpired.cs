using SpaceOS.Modules.Contracts.Shared;

namespace SpaceOS.Modules.Contracts.Inventory.Events;

/// <summary>Published when a reservation expires because its TTL elapsed without consumption.</summary>
/// <remarks>Consumers MUST verify Event.TenantId matches their JWT TenantId (SEC-03).</remarks>
public sealed record ReservationExpired : ModuleEvent
{
    /// <summary>Gets the identifier of the expired reservation.</summary>
    public required Guid ReservationId { get; init; }

    /// <summary>Gets the consumer-provided idempotency key of the expired reservation.</summary>
    public required Guid CorrelationId { get; init; }

    /// <summary>Gets the UTC timestamp at which the reservation expired.</summary>
    public required DateTimeOffset ExpiredAt { get; init; }
}
