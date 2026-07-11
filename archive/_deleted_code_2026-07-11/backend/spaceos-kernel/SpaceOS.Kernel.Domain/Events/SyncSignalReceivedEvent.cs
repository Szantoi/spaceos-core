// SpaceOS.Kernel.Domain/Events/SyncSignalReceivedEvent.cs
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Events;

/// <summary>
/// Raised when a new <see cref="SpaceOS.Kernel.Domain.Sync.SyncSignal"/> is received and appended to the hash chain.
/// </summary>
public readonly record struct SyncSignalReceivedEvent(
    Guid SyncSignalId,
    FlowEpicId EpicId,
    TenantId TenantId,
    string NewState,
    Guid ClientSignalId,
    DateTimeOffset OccurredOn) : IDomainEvent;
