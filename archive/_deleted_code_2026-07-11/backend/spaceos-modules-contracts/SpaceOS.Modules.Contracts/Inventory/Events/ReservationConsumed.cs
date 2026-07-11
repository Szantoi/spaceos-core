using SpaceOS.Modules.Contracts.Shared;

namespace SpaceOS.Modules.Contracts.Inventory.Events;

/// <summary>Published when a reservation is fully consumed via RecordConsumptionAsync.</summary>
/// <remarks>Consumers MUST verify Event.TenantId matches their JWT TenantId (SEC-03).</remarks>
public sealed record ReservationConsumed : ModuleEvent
{
    /// <summary>Gets the identifier of the consumed reservation.</summary>
    public required Guid ReservationId { get; init; }

    /// <summary>Gets the consumer-provided idempotency key of the consumed reservation.</summary>
    public required Guid CorrelationId { get; init; }

    /// <summary>Gets the registered consumer module that consumed the reservation.</summary>
    public required string ConsumerModule { get; init; }
}
