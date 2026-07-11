// SpaceOS.Kernel.Domain/AuditLog/AuditEvent.cs

using SpaceOS.Kernel.Domain.Primitives;

namespace SpaceOS.Kernel.Domain.AuditLog;

/// <summary>
/// Aggregate root representing an immutable audit log entry for a domain event.
/// Audit events are append-only — no mutations after creation.
/// </summary>
public sealed class AuditEvent : AggregateRoot
{
    /// <summary>Gets the unique identifier of this audit event record.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the identifier of the tenant this audit event belongs to.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Gets the fully-qualified name of the domain event type (e.g. "TenantCreatedEvent").</summary>
    public string EventType { get; private set; } = string.Empty;

    /// <summary>Gets the identifier of the aggregate that raised the domain event.</summary>
    public Guid AggregateId { get; private set; }

    /// <summary>Gets the serialised JSON representation of the domain event payload.</summary>
    public string Payload { get; private set; } = string.Empty;

    /// <summary>Gets the hex digest of the domain event state at the time of recording.</summary>
    public string StateHash { get; private set; } = string.Empty;

    /// <summary>Gets the hash of the preceding audit event in the chain, or <c>"GENESIS"</c> for the first event of a tenant.</summary>
    public string PreviousHash { get; private set; } = "GENESIS";

    /// <summary>
    /// Gets the algorithm used to compute <see cref="StateHash"/>.
    /// Stored per-record to support incremental algorithm migration without ambiguity.
    /// </summary>
    public HashAlgorithmType HashAlgorithm { get; private set; } = HashAlgorithmType.SHA256;

    /// <summary>Gets the UTC timestamp at which the domain event occurred.</summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>
    /// Gets the authenticated user identifier (JWT <c>sub</c> claim) who triggered this event,
    /// or <c>null</c> for system-initiated events.
    /// </summary>
    public string? ActorId { get; private set; }

    /// <summary>
    /// Gets the source IP address of the caller (respecting <c>X-Forwarded-For</c>),
    /// or <c>null</c> for system-initiated events.
    /// </summary>
    public string? SourceIp { get; private set; }

    /// <summary>
    /// Gets the brand identifier extracted from the <c>X-SpaceOS-Brand</c> header,
    /// or <c>null</c> when the header is missing or the value is not in the allowlist.
    /// </summary>
    public string? SourceBrand { get; private set; }

    /// <summary>
    /// Gets the monotone sequence number assigned by the database on insert.
    /// Used as a tiebreaker in <c>GetChainAsync</c> when two events share the same
    /// <see cref="OccurredAt"/> clock tick.  Value is DB-generated (<c>GENERATED ALWAYS AS IDENTITY</c>)
    /// and must never be set by application code.
    /// </summary>
    public long Sequence { get; private set; }

    /// <summary>
    /// Parameterless constructor reserved for EF Core materialisation.
    /// </summary>
    private AuditEvent() { }

    private AuditEvent(
        Guid              id,
        Guid              tenantId,
        string            eventType,
        Guid              aggregateId,
        string            payload,
        string            stateHash,
        string?           previousHash,
        DateTimeOffset    occurredAt,
        string?           actorId,
        string?           sourceIp,
        HashAlgorithmType hashAlgorithm,
        string?           sourceBrand)
    {
        Id            = id;
        TenantId      = tenantId;
        EventType     = eventType;
        AggregateId   = aggregateId;
        Payload       = payload;
        StateHash     = stateHash;
        PreviousHash  = previousHash ?? "GENESIS";
        OccurredAt    = occurredAt;
        ActorId       = actorId;
        SourceIp      = sourceIp;
        HashAlgorithm = hashAlgorithm;
        SourceBrand   = sourceBrand;
    }

    /// <summary>
    /// Creates a new <see cref="AuditEvent"/> with a freshly generated identifier and the current UTC time.
    /// Raises an <see cref="AuditEventCreatedEvent"/>.
    /// </summary>
    /// <param name="tenantId">The identifier of the owning tenant.</param>
    /// <param name="eventType">The fully-qualified domain event type name.</param>
    /// <param name="aggregateId">The identifier of the aggregate that raised the event.</param>
    /// <param name="payload">The serialised JSON payload of the domain event.</param>
    /// <param name="stateHash">The hex digest of the domain state at the time of the event.</param>
    /// <param name="previousHash">The <see cref="StateHash"/> of the preceding audit event, or <c>null</c> to default to <c>"GENESIS"</c>.</param>
    /// <param name="actorId">The JWT <c>sub</c> claim of the caller (pseudonymized), or <c>null</c> for system events.</param>
    /// <param name="sourceIp">The remote IP of the caller, or <c>null</c> for system events.</param>
    /// <param name="hashAlgorithm">The algorithm used to compute <paramref name="stateHash"/>. Defaults to <see cref="HashAlgorithmType.SHA256"/>.</param>
    /// <param name="sourceBrand">The validated brand from the <c>X-SpaceOS-Brand</c> header, or <c>null</c>.</param>
    /// <returns>A newly created, immutable <see cref="AuditEvent"/> instance.</returns>
    public static AuditEvent Create(
        Guid              tenantId,
        string            eventType,
        Guid              aggregateId,
        string            payload,
        string            stateHash,
        string?           previousHash  = null,
        string?           actorId       = null,
        string?           sourceIp      = null,
        HashAlgorithmType hashAlgorithm = HashAlgorithmType.SHA256,
        string?           sourceBrand   = null)
    {
        var occurredAt = DateTimeOffset.UtcNow;
        var auditEvent = new AuditEvent(
            id:            Guid.NewGuid(),
            tenantId:      tenantId,
            eventType:     eventType,
            aggregateId:   aggregateId,
            payload:       payload,
            stateHash:     stateHash,
            previousHash:  previousHash,
            occurredAt:    occurredAt,
            actorId:       actorId,
            sourceIp:      sourceIp,
            hashAlgorithm: hashAlgorithm,
            sourceBrand:   sourceBrand);

        auditEvent.AddDomainEvent(new AuditEventCreatedEvent(auditEvent.Id, occurredAt));

        return auditEvent;
    }
}
