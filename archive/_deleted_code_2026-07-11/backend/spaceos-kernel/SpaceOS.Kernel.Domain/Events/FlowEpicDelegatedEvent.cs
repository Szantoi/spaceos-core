using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Events;

/// <summary>
/// Domain event raised when a <see cref="SpaceOS.Kernel.Domain.Entities.FlowEpic"/>
/// is delegated to a guest tenant via a B2B handshake.
/// </summary>
/// <param name="FlowEpicId">The identifier of the delegated epic.</param>
/// <param name="GuestTenantId">The identifier of the guest tenant receiving the delegation.</param>
/// <param name="OccurredOn">The UTC timestamp at which the delegation occurred.</param>
public readonly record struct FlowEpicDelegatedEvent(FlowEpicId FlowEpicId, TenantId GuestTenantId, DateTimeOffset OccurredOn) : IDomainEvent;
