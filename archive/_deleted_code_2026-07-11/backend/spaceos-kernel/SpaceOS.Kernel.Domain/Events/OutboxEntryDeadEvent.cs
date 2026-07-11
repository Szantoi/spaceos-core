// SpaceOS.Kernel.Domain/Events/OutboxEntryDeadEvent.cs

using SpaceOS.Kernel.Domain.Primitives;

namespace SpaceOS.Kernel.Domain.Events;

/// <summary>
/// Raised when an outbox entry is permanently moved to the <c>Dead</c> status
/// because it has exceeded the maximum retry count and will no longer be retried.
/// Consumers of this event should alert on-call engineers or forward to a dead-letter queue.
/// </summary>
/// <param name="EntryId">The unique identifier of the dead outbox entry.</param>
/// <param name="TenantId">The identifier of the owning tenant.</param>
/// <param name="EventType">The event type discriminator of the dead entry.</param>
/// <param name="RetryCount">The total number of processing attempts that were made.</param>
/// <param name="OccurredOn">The UTC timestamp when the entry was declared dead.</param>
public readonly record struct OutboxEntryDeadEvent(
    Guid   EntryId,
    Guid   TenantId,
    string EventType,
    int    RetryCount,
    DateTimeOffset OccurredOn) : IDomainEvent;
