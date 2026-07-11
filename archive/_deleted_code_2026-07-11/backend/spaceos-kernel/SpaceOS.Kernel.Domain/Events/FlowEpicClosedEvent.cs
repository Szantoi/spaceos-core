// SpaceOS.Kernel.Domain/Events/FlowEpicClosedEvent.cs

using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Events;

/// <summary>
/// Raised when a <see cref="Entities.FlowEpic"/> is closed with a verified proof document.
/// </summary>
/// <param name="FlowEpicId">The identifier of the closed flow epic.</param>
/// <param name="TenantId">The identifier of the owning tenant.</param>
/// <param name="ProofHash">The SHA-256 hex hash of the proof document.</param>
/// <param name="OccurredOn">The UTC timestamp when the epic was closed.</param>
public readonly record struct FlowEpicClosedEvent(
    FlowEpicId     FlowEpicId,
    TenantId       TenantId,
    string         ProofHash,
    DateTimeOffset OccurredOn) : IDomainEvent;
