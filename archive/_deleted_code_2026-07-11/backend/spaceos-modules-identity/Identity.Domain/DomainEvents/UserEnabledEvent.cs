// Identity.Domain/DomainEvents/UserEnabledEvent.cs

using Identity.Domain.ValueObjects;

namespace Identity.Domain.DomainEvents;

public sealed record UserEnabledEvent(
    SpaceOSUserId UserId,
    Guid TenantId,
    DateTimeOffset OccurredAt);
