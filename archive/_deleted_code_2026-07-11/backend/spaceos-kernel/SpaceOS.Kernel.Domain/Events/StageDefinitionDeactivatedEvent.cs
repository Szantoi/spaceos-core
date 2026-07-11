// SpaceOS.Kernel.Domain/Events/StageDefinitionDeactivatedEvent.cs
using System;
using SpaceOS.Kernel.Domain.Primitives;

namespace SpaceOS.Kernel.Domain.Events;

/// <summary>Raised when a <see cref="Entities.StageDefinition"/> is deactivated.</summary>
/// <param name="Id">The unique identifier of the deactivated stage definition.</param>
/// <param name="TenantId">The identifier of the owning tenant.</param>
/// <param name="StageCode">The immutable stage code of the deactivated definition.</param>
/// <param name="OccurredOn">The UTC timestamp when the event was raised.</param>
public readonly record struct StageDefinitionDeactivatedEvent(
    Guid Id,
    Guid TenantId,
    string StageCode,
    DateTimeOffset OccurredOn) : IDomainEvent;
