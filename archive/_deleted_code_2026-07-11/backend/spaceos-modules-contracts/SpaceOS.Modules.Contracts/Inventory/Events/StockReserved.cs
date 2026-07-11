using SpaceOS.Modules.Contracts.Inventory.DTOs;
using SpaceOS.Modules.Contracts.Shared;

namespace SpaceOS.Modules.Contracts.Inventory.Events;

/// <summary>Published when a soft stock reservation is successfully created.</summary>
/// <remarks>Consumers MUST verify Event.TenantId matches their JWT TenantId (SEC-03).</remarks>
public sealed record StockReserved : ModuleEvent
{
    /// <summary>Gets the user identity that created the reservation, if available.</summary>
    public required Guid? ActorUserId { get; init; }

    /// <summary>Gets the consumer-provided idempotency key.</summary>
    public required Guid CorrelationId { get; init; }

    /// <summary>Gets the identifier of the new reservation.</summary>
    public required Guid ReservationId { get; init; }

    /// <summary>Gets the registered consumer module name.</summary>
    public required string ConsumerModule { get; init; }

    /// <summary>Gets the UTC timestamp after which the reservation will expire.</summary>
    public required DateTimeOffset ExpiresAt { get; init; }

    /// <summary>Gets the reserved line items.</summary>
    public required IReadOnlyList<ReservationItemDto> Items { get; init; }
}
