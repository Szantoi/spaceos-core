using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Events;

/// <summary>
/// Raised when a new <see cref="SpaceOS.Kernel.Domain.Entities.FlowEpic"/> is created.
/// </summary>
public readonly record struct FlowEpicCreatedEvent(
    FlowEpicId     FlowEpicId,
    FacilityId     TargetFacilityId,
    DateTimeOffset OccurredOn) : IDomainEvent;
