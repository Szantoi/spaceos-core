// SpaceOS.Kernel.Application/AuditLog/AuditEventDto.cs

namespace SpaceOS.Kernel.Application.AuditLog;

/// <summary>
/// Read-only projection of an <see cref="SpaceOS.Kernel.Domain.AuditLog.AuditEvent"/> for API consumers.
/// </summary>
/// <remarks>
/// <c>Payload</c> is intentionally excluded — it contains internal serialised domain event data
/// and must never be exposed via the public API surface (security requirement).
/// </remarks>
/// <param name="Id">The unique identifier of the audit event record.</param>
/// <param name="EventType">The fully-qualified domain event type name (e.g. "TenantCreatedEvent").</param>
/// <param name="AggregateId">The identifier of the aggregate that raised the domain event.</param>
/// <param name="StateHash">The SHA-256 hex digest of the domain state at the time of the event.</param>
/// <param name="OccurredAt">The UTC timestamp at which the domain event occurred.</param>
/// <param name="ActorId">The JWT <c>sub</c> claim of the user who triggered the event, or <c>null</c> for system events.</param>
/// <param name="SourceIp">The remote IP address of the caller, or <c>null</c> for system events.</param>
/// <param name="SourceBrand">The validated brand identifier, or <c>null</c>.</param>
public sealed record AuditEventDto(
    Guid Id,
    string EventType,
    Guid AggregateId,
    string StateHash,
    DateTimeOffset OccurredAt,
    string? ActorId,
    string? SourceIp,
    string? SourceBrand);
