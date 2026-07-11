using System;
using SpaceOS.Kernel.Domain.Primitives;

namespace SpaceOS.Kernel.Domain.Events;

/// <summary>
/// Raised when a new <see cref="SpaceOS.Kernel.Domain.Entities.SpatialElement"/> is registered
/// within a physical space.
/// </summary>
public readonly record struct SpatialElementRegisteredEvent(
    Guid           ElementId,
    Guid           PhysicalSpaceId,
    Guid           FlowEpicId,
    string         TradeType,
    DateTimeOffset OccurredOn) : IDomainEvent;
