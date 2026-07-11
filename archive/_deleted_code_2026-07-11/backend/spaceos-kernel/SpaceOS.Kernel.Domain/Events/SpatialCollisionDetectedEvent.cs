using System;
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Events;

/// <summary>
/// Raised when a spatial collision is detected between two elements in the BVH tree.
/// </summary>
public readonly record struct SpatialCollisionDetectedEvent(
    Guid           ElementIdA,
    Guid           ElementIdB,
    BoundingBox    IntersectionVolume,
    DateTimeOffset OccurredOn) : IDomainEvent;
