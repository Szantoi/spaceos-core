// SpaceOS.Kernel.Domain/Events/AggregateSnapshotCreatedEvent.cs

using SpaceOS.Kernel.Domain.Primitives;

namespace SpaceOS.Kernel.Domain.Events;

/// <summary>
/// Raised when an <c>AggregateSnapshot</c> is successfully created.
/// SEC-P3B-04: <see cref="SnapshotHash"/> is included in the event payload so that
/// the hash enters the AuditEvent chain, providing a tamper-evident link between
/// the snapshot record and the audit trail.
/// </summary>
/// <param name="SnapshotId">The unique identifier of the newly created snapshot.</param>
/// <param name="TenantId">The identifier of the tenant that owns the aggregate.</param>
/// <param name="AggregateId">The identifier of the snapshotted aggregate.</param>
/// <param name="AggregateType">The string name of the aggregate type.</param>
/// <param name="Version">The version number of this snapshot.</param>
/// <param name="SnapshotHash">The SHA-256 hex hash of the snapshot state JSON.</param>
/// <param name="OccurredOn">The UTC timestamp when the snapshot was created.</param>
public readonly record struct AggregateSnapshotCreatedEvent(
    Guid   SnapshotId,
    Guid   TenantId,
    Guid   AggregateId,
    string AggregateType,
    int    Version,
    string SnapshotHash,
    DateTimeOffset OccurredOn) : IDomainEvent;
