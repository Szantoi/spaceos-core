// SpaceOS.Kernel.Domain/Events/FlowEpicStageAdvancedEvent.cs
using System;
using SpaceOS.Kernel.Domain.Primitives;

namespace SpaceOS.Kernel.Domain.Events;

/// <summary>Raised when a <see cref="Entities.FlowEpic"/> advances to a new stage (or is assigned its first stage).</summary>
/// <param name="FlowEpicId">The identifier of the flow epic.</param>
/// <param name="TenantId">The identifier of the owning tenant.</param>
/// <param name="From">The previous stage code, or <see langword="null"/> when the chain is first assigned.</param>
/// <param name="To">The new current stage code.</param>
/// <param name="OccurredOn">The UTC timestamp when the event was raised.</param>
public readonly record struct FlowEpicStageAdvancedEvent(
    Guid FlowEpicId,
    Guid TenantId,
    string? From,
    string To,
    DateTimeOffset OccurredOn) : IDomainEvent;
