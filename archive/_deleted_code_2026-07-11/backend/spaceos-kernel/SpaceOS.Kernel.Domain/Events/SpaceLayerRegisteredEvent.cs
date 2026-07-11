using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Events;

/// <summary>
/// Raised when a new <see cref="SpaceOS.Kernel.Domain.Entities.SpaceLayer"/> is registered.
/// </summary>
public readonly record struct SpaceLayerRegisteredEvent(
    SpaceLayerId   SpaceLayerId,
    FacilityId     FacilityId,
    TradeType      TradeType,
    bool           IsExternalNode,
    DateTimeOffset OccurredOn) : IDomainEvent;
