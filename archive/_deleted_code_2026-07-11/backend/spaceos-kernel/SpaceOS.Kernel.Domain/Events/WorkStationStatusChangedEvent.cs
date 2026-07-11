using System;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Events;

/// <summary>
/// Event raised when a workstation's status changes.
/// </summary>
public readonly record struct WorkStationStatusChangedEvent(
    WorkStationId WorkStationId,
    WorkStationStatus OldStatus,
    WorkStationStatus NewStatus,
    DateTimeOffset OccurredOn) : IDomainEvent;
