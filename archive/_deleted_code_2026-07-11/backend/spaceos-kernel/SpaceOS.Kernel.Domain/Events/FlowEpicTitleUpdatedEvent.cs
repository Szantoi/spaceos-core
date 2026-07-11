using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Events;

/// <summary>
/// Raised when the title of a <see cref="SpaceOS.Kernel.Domain.Entities.FlowEpic"/> is updated.
/// </summary>
public readonly record struct FlowEpicTitleUpdatedEvent(
    FlowEpicId     FlowEpicId,
    string         OldTitle,
    string         NewTitle,
    DateTimeOffset OccurredOn) : IDomainEvent;
