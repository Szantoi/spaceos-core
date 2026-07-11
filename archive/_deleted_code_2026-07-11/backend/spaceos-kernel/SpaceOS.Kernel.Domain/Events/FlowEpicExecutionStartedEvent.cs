using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Events;

/// <summary>
/// Raised when a <see cref="SpaceOS.Kernel.Domain.Entities.FlowEpic"/> transitions
/// from the Discovery phase to the Delivery phase.
/// </summary>
public readonly record struct FlowEpicExecutionStartedEvent(
    FlowEpicId     FlowEpicId,
    DateTimeOffset OccurredOn) : IDomainEvent;
