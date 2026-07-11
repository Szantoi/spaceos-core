using System;
using SpaceOS.Kernel.Domain.Primitives;

namespace SpaceOS.Kernel.Domain.Events;

/// <summary>
/// Raised when a new <see cref="SpaceOS.Kernel.Domain.Aggregates.PhysicalSpace"/> is registered.
/// </summary>
public readonly record struct PhysicalSpaceRegisteredEvent(
    Guid           PhysicalSpaceId,
    Guid           TenantId,
    Guid           FacilityId,
    string         SpaceType,
    int            WidthMm,
    int            HeightMm,
    int            DepthMm,
    DateTimeOffset OccurredOn) : IDomainEvent;
