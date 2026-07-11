// SpaceOS.Kernel.Domain/Events/StageChainCreatedEvent.cs
using System;
using SpaceOS.Kernel.Domain.Primitives;

namespace SpaceOS.Kernel.Domain.Events;

/// <summary>Raised when a new <see cref="Entities.StageChainTemplate"/> is created for a tenant.</summary>
/// <param name="Id">The unique identifier of the new chain template.</param>
/// <param name="TenantId">The identifier of the owning tenant.</param>
/// <param name="Name">The name of the chain template.</param>
/// <param name="OccurredOn">The UTC timestamp when the event was raised.</param>
public readonly record struct StageChainCreatedEvent(
    Guid Id,
    Guid TenantId,
    string Name,
    DateTimeOffset OccurredOn) : IDomainEvent;
