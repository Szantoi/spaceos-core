using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Events;

/// <summary>
/// Raised when a <see cref="SpaceOS.Kernel.Domain.Entities.Facility"/> is renamed.
/// </summary>
public readonly record struct FacilityRenamedEvent(
    FacilityId     FacilityId,
    string         OldName,
    string         NewName,
    DateTimeOffset OccurredOn) : IDomainEvent;
