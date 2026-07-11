// SpaceOS.Kernel.Domain/Events/StageChainStepRemovedEvent.cs
using SpaceOS.Kernel.Domain.Primitives;

namespace SpaceOS.Kernel.Domain.Events;

/// <summary>
/// Raised when a <see cref="Entities.StageChainStep"/> is removed from a
/// <see cref="Entities.StageChainTemplate"/>.
/// </summary>
/// <param name="ChainTemplateId">The identifier of the chain template.</param>
/// <param name="TenantId">The owning tenant identifier.</param>
/// <param name="StageCode">The stage code of the step that was removed.</param>
/// <param name="OccurredOn">The UTC timestamp when the event was raised.</param>
public readonly record struct StageChainStepRemovedEvent(
    Guid ChainTemplateId,
    Guid TenantId,
    string StageCode,
    DateTimeOffset OccurredOn) : IDomainEvent;
