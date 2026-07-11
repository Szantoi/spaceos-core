// SpaceOS.Kernel.Domain/AuditLog/AuditEventCreatedEvent.cs

using SpaceOS.Kernel.Domain.Primitives;

namespace SpaceOS.Kernel.Domain.AuditLog;

/// <summary>
/// Raised when a new <see cref="AuditEvent"/> is created.
/// </summary>
/// <param name="AuditEventId">The unique identifier of the newly created audit event.</param>
/// <param name="OccurredOn">The UTC timestamp at which the audit event was recorded.</param>
public readonly record struct AuditEventCreatedEvent(
    Guid           AuditEventId,
    DateTimeOffset OccurredOn) : IDomainEvent;
