// SpaceOS.Kernel.Domain/Events/StageChainStepAddedEvent.cs
using SpaceOS.Kernel.Domain.Primitives;

namespace SpaceOS.Kernel.Domain.Events;

/// <summary>
/// Raised when a <see cref="Entities.StageChainStep"/> is added to a
/// <see cref="Entities.StageChainTemplate"/>.
/// </summary>
/// <param name="ChainTemplateId">The identifier of the chain template.</param>
/// <param name="TenantId">The owning tenant identifier.</param>
/// <param name="StageCode">The stage code of the step that was added.</param>
/// <param name="SortOrder">The sort order assigned to the new step.</param>
/// <param name="OccurredOn">The UTC timestamp when the event was raised.</param>
public readonly record struct StageChainStepAddedEvent(
    Guid ChainTemplateId,
    Guid TenantId,
    string StageCode,
    int SortOrder,
    DateTimeOffset OccurredOn) : IDomainEvent;
