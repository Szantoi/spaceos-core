// Identity.Domain/DomainEvents/UserProfileUpdatedEvent.cs

using Identity.Domain.ValueObjects;

namespace Identity.Domain.DomainEvents;

public sealed record UserProfileUpdatedEvent(
    SpaceOSUserId UserId,
    Guid TenantId,
    DisplayName NewDisplayName,
    DateTimeOffset OccurredAt);
