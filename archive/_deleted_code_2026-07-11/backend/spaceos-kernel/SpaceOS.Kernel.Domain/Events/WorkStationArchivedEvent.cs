// SpaceOS.Kernel.Domain/Events/WorkStationArchivedEvent.cs
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Events;

/// <summary>Raised when a <see cref="Entities.WorkStation"/> is archived (soft-deleted).</summary>
/// <param name="WorkStationId">The identifier of the archived workstation.</param>
/// <param name="OccurredOn">The UTC timestamp when the archive occurred.</param>
public readonly record struct WorkStationArchivedEvent(WorkStationId WorkStationId, DateTimeOffset OccurredOn) : IDomainEvent;
