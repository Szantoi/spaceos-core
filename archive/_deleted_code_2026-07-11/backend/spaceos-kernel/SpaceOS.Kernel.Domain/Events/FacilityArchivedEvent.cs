// SpaceOS.Kernel.Domain/Events/FacilityArchivedEvent.cs
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Events;

/// <summary>Raised when a <see cref="Entities.Facility"/> is archived (soft-deleted).</summary>
/// <param name="FacilityId">The identifier of the archived facility.</param>
/// <param name="OccurredOn">The UTC timestamp when the archive occurred.</param>
public readonly record struct FacilityArchivedEvent(FacilityId FacilityId, DateTimeOffset OccurredOn) : IDomainEvent;
