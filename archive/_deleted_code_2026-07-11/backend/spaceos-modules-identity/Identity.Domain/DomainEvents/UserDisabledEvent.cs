// Identity.Domain/DomainEvents/UserDisabledEvent.cs

using Identity.Domain.ValueObjects;

namespace Identity.Domain.DomainEvents;

public sealed record UserDisabledEvent(
    SpaceOSUserId UserId,
    Guid TenantId,
    DateTimeOffset OccurredAt);
