using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Events;

/// <summary>
/// Raised when a <see cref="SpaceOS.Kernel.Domain.Entities.WorkStation"/> display name is updated.
/// </summary>
public readonly record struct WorkStationRenamedEvent(
    WorkStationId  WorkStationId,
    string         OldName,
    string         NewName,
    DateTimeOffset OccurredOn) : IDomainEvent;
