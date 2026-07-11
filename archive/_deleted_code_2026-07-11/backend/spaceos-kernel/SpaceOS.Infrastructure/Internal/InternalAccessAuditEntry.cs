namespace SpaceOS.Infrastructure.Internal;

/// <summary>
/// Append-only record for every GET /api/internal/tenants/{id} call (SEC-S-09).
/// </summary>
public sealed class InternalAccessAuditEntry
{
    /// <summary>Gets the surrogate primary key (GENERATED ALWAYS AS IDENTITY).</summary>
    public long Id { get; private init; }

    /// <summary>Gets the tenant that performed the lookup.</summary>
    public Guid RequesterTenantId { get; private init; }

    /// <summary>Gets the tenant that was looked up.</summary>
    public Guid TargetTenantId { get; private init; }

    /// <summary>Gets the lookup outcome: "Found" or "NotFound".</summary>
    public string Result { get; private init; } = default!;

    /// <summary>Gets the UTC instant at which the lookup was recorded.</summary>
    public DateTimeOffset OccurredAt { get; private init; }

    /// <summary>Parameterless constructor reserved for EF Core materialisation.</summary>
    private InternalAccessAuditEntry() { }

    /// <summary>
    /// Creates a new <see cref="InternalAccessAuditEntry"/>.
    /// </summary>
    /// <param name="requesterTenantId">The tenant that performed the lookup.</param>
    /// <param name="targetTenantId">The tenant that was looked up.</param>
    /// <param name="result">"Found" or "NotFound".</param>
    /// <param name="occurredAt">The UTC instant of the lookup.</param>
    public static InternalAccessAuditEntry Create(
        Guid requesterTenantId,
        Guid targetTenantId,
        string result,
        DateTimeOffset occurredAt)
        => new()
        {
            RequesterTenantId = requesterTenantId,
            TargetTenantId    = targetTenantId,
            Result            = result,
            OccurredAt        = occurredAt,
        };
}
