// Identity.Domain/DomainEvents/UserKcSyncFailedEvent.cs

using Identity.Domain.ValueObjects;

namespace Identity.Domain.DomainEvents;

public sealed record UserKcSyncFailedEvent(
    SpaceOSUserId UserId,
    Guid TenantId,
    string Reason,
    DateTimeOffset OccurredAt);
