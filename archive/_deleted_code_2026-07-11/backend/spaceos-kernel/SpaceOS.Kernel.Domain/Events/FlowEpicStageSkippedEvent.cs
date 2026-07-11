// SpaceOS.Kernel.Domain/Events/FlowEpicStageSkippedEvent.cs
using System;
using SpaceOS.Kernel.Domain.Primitives;

namespace SpaceOS.Kernel.Domain.Events;

/// <summary>Raised when an optional stage is explicitly skipped on a <see cref="Entities.FlowEpic"/>.</summary>
/// <param name="FlowEpicId">The identifier of the flow epic.</param>
/// <param name="TenantId">The identifier of the owning tenant.</param>
/// <param name="Skipped">The stage code of the skipped stage.</param>
/// <param name="OccurredOn">The UTC timestamp when the event was raised.</param>
public readonly record struct FlowEpicStageSkippedEvent(
    Guid FlowEpicId,
    Guid TenantId,
    string Skipped,
    DateTimeOffset OccurredOn) : IDomainEvent;
