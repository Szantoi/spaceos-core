// SpaceOS.Kernel.Domain/Events/StageHandoffCreatedEvent.cs
using System;
using SpaceOS.Kernel.Domain.Primitives;

namespace SpaceOS.Kernel.Domain.Events;

/// <summary>Raised when a new <see cref="Entities.StageHandoff"/> data package is created.</summary>
/// <param name="Id">The unique identifier of the new handoff.</param>
/// <param name="FlowEpicId">The identifier of the associated flow epic.</param>
/// <param name="Source">The source stage code.</param>
/// <param name="Target">The target stage code.</param>
/// <param name="Version">The version number assigned to this handoff.</param>
/// <param name="OccurredOn">The UTC timestamp when the event was raised.</param>
public readonly record struct StageHandoffCreatedEvent(
    Guid Id,
    Guid FlowEpicId,
    string Source,
    string Target,
    int Version,
    DateTimeOffset OccurredOn) : IDomainEvent;
