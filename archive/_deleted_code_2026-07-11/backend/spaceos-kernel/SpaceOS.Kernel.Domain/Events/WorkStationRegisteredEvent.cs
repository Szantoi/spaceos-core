using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Events;

/// <summary>
/// Raised when a new <see cref="SpaceOS.Kernel.Domain.Entities.WorkStation"/> is registered.
/// </summary>
public readonly record struct WorkStationRegisteredEvent(
    WorkStationId  WorkStationId,
    FacilityId     FacilityId,
    DateTimeOffset OccurredOn) : IDomainEvent;
