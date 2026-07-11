using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Events;

/// <summary>
/// Raised when a <see cref="SpaceOS.Kernel.Domain.Entities.WorkStation"/> is assigned to a different facility.
/// </summary>
public readonly record struct WorkStationReassignedEvent(
    WorkStationId  WorkStationId,
    FacilityId     OldFacilityId,
    FacilityId     NewFacilityId,
    DateTimeOffset OccurredOn) : IDomainEvent;
