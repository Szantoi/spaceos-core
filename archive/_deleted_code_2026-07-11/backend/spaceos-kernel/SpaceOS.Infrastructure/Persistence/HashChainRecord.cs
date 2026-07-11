// SpaceOS.Infrastructure/Persistence/HashChainRecord.cs

namespace SpaceOS.Infrastructure.Persistence;

/// <summary>
/// Relational entity representing a single entry in the append-only hash chain
/// stored in the <c>spaceos_audit_sink</c> database.
/// Each record mirrors the <c>StateHash</c> of one <see cref="SpaceOS.Kernel.Domain.AuditLog.AuditEvent"/>
/// so that divergence can be detected by comparing the two databases.
/// </summary>
/// <remarks>
/// <para>
/// The <c>bigserial</c> primary key serves as a tamper indicator: a gap in the
/// sequence means a row was deleted, which is detectable during chain verification.
/// </para>
/// <para>
/// No navigation properties are defined — this entity lives in a physically
/// separate database (<c>spaceos_audit_sink</c>) and cannot participate in
/// cross-database EF Core joins.
/// </para>
/// </remarks>
internal sealed class HashChainRecord
{
    /// <summary>
    /// Gets or sets the auto-generated surrogate primary key (bigserial).
    /// A gap in consecutive values indicates a deleted row.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the tenant that owns the originating audit event.
    /// Indexed to support tenant-scoped verification queries.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the originating <see cref="SpaceOS.Kernel.Domain.AuditLog.AuditEvent"/>.
    /// Enforced UNIQUE — one hash record per audit event.
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Gets or sets the SHA-256 hex digest mirrored from <c>AuditEvent.StateHash</c>.
    /// </summary>
    public string StateHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp at which the domain event occurred,
    /// mirrored from <c>AuditEvent.OccurredAt</c>.
    /// </summary>
    public DateTimeOffset OccurredAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp at which this record was inserted into the sink database.
    /// Defaults to <c>now()</c> at the database level.
    /// </summary>
    public DateTimeOffset InsertedAt { get; set; }
}
