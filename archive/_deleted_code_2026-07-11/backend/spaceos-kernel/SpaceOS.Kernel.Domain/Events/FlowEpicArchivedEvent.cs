// SpaceOS.Kernel.Domain/Events/FlowEpicArchivedEvent.cs
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Events;

/// <summary>Raised when a <see cref="Entities.FlowEpic"/> is archived (soft-deleted).</summary>
/// <param name="FlowEpicId">The identifier of the archived flow epic.</param>
/// <param name="OccurredOn">The UTC timestamp when the archive occurred.</param>
public readonly record struct FlowEpicArchivedEvent(FlowEpicId FlowEpicId, DateTimeOffset OccurredOn) : IDomainEvent;
