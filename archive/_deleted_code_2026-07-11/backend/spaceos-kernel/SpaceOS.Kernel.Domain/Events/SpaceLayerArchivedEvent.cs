// SpaceOS.Kernel.Domain/Events/SpaceLayerArchivedEvent.cs
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Events;

/// <summary>Raised when a <see cref="Entities.SpaceLayer"/> is archived (soft-deleted).</summary>
/// <param name="SpaceLayerId">The identifier of the archived space layer.</param>
/// <param name="OccurredOn">The UTC timestamp when the archive occurred.</param>
public readonly record struct SpaceLayerArchivedEvent(SpaceLayerId SpaceLayerId, DateTimeOffset OccurredOn) : IDomainEvent;
