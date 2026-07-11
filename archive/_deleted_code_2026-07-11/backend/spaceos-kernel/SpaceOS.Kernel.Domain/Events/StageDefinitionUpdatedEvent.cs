// SpaceOS.Kernel.Domain/Events/StageDefinitionUpdatedEvent.cs
using System;
using SpaceOS.Kernel.Domain.Primitives;

namespace SpaceOS.Kernel.Domain.Events;

/// <summary>Raised when a <see cref="Entities.StageDefinition"/> module endpoint is updated.</summary>
/// <param name="Id">The unique identifier of the updated stage definition.</param>
/// <param name="TenantId">The identifier of the owning tenant.</param>
/// <param name="StageCode">The immutable stage code of the updated definition.</param>
/// <param name="OccurredOn">The UTC timestamp when the event was raised.</param>
public readonly record struct StageDefinitionUpdatedEvent(
    Guid Id,
    Guid TenantId,
    string StageCode,
    DateTimeOffset OccurredOn) : IDomainEvent;
