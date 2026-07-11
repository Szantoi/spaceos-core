// SpaceOS.Kernel.Domain/Events/NodeHeartbeatRecordedEvent.cs
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Events;

/// <summary>
/// Raised when a node heartbeat is recorded and the online status changes.
/// </summary>
public readonly record struct NodeHeartbeatRecordedEvent(
    Guid NodeManifestId,
    TenantId TenantId,
    bool IsOnlineChanged,
    DateTimeOffset OccurredOn) : IDomainEvent;
