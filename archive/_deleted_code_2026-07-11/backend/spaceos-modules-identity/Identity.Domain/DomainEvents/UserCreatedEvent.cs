// Identity.Domain/DomainEvents/UserCreatedEvent.cs

using Identity.Domain.ValueObjects;

namespace Identity.Domain.DomainEvents;

public sealed record UserCreatedEvent(
    SpaceOSUserId UserId,
    Guid TenantId,
    Email Email,
    DisplayName DisplayName,
    DateTimeOffset OccurredAt);
