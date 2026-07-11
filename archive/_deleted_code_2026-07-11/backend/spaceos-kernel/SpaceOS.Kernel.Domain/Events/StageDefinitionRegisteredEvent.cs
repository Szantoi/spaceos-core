// SpaceOS.Kernel.Domain/Events/StageDefinitionRegisteredEvent.cs
using System;
using SpaceOS.Kernel.Domain.Primitives;

namespace SpaceOS.Kernel.Domain.Events;

/// <summary>Raised when a new <see cref="Entities.StageDefinition"/> is registered for a tenant.</summary>
/// <param name="Id">The unique identifier of the new stage definition.</param>
/// <param name="TenantId">The identifier of the owning tenant.</param>
/// <param name="StageCode">The immutable stage code.</param>
/// <param name="OccurredOn">The UTC timestamp when the event was raised.</param>
public readonly record struct StageDefinitionRegisteredEvent(
    Guid Id,
    Guid TenantId,
    string StageCode,
    DateTimeOffset OccurredOn) : IDomainEvent;
