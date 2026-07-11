// Identity.Domain/DomainEvents/PasswordResetRequestedEvent.cs

using Identity.Domain.ValueObjects;

namespace Identity.Domain.DomainEvents;

public sealed record PasswordResetRequestedEvent(
    SpaceOSUserId UserId,
    Guid TenantId,
    DateTimeOffset OccurredAt);
