// SpaceOS.Kernel.Domain/Snapshots/AggregateSnapshot.cs

namespace SpaceOS.Kernel.Domain.Snapshots;

/// <summary>
/// Append-only snapshot of an aggregate's state at a specific version.
/// Snapshots are never mutated after creation — no domain events are raised.
/// </summary>
public sealed class AggregateSnapshot
{
    /// <summary>Gets the unique identifier of this snapshot record.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the identifier of the aggregate this snapshot belongs to.</summary>
    public Guid AggregateId { get; private set; }

    /// <summary>Gets the CLR type name of the aggregate.</summary>
    public string AggregateType { get; private set; } = string.Empty;

    /// <summary>Gets the aggregate version this snapshot represents.</summary>
    public int Version { get; private set; }

    /// <summary>Gets the UTC timestamp when this snapshot was taken.</summary>
    public DateTimeOffset SnapshotAt { get; private set; }

    /// <summary>Gets the identifier of the domain event that triggered this snapshot.</summary>
    public Guid TriggerEventId { get; private set; }

    /// <summary>Gets the JSON-serialised state of the aggregate at this version.</summary>
    public string StateJson { get; private set; } = string.Empty;

    /// <summary>Gets the SHA-256 hex hash of <see cref="StateJson"/> for tamper detection.</summary>
    public string SnapshotHash { get; private set; } = string.Empty;

    /// <summary>Gets the identifier of the tenant that owns the aggregate.</summary>
    public Guid TenantId { get; private set; }

    private AggregateSnapshot() { }

    /// <summary>
    /// Creates a new <see cref="AggregateSnapshot"/> record.
    /// </summary>
    /// <param name="aggregateId">The identifier of the snapshotted aggregate.</param>
    /// <param name="aggregateType">The CLR type name of the aggregate.</param>
    /// <param name="version">The aggregate version at snapshot time.</param>
    /// <param name="snapshotAt">The UTC timestamp when the snapshot was taken.</param>
    /// <param name="triggerEventId">The domain event that caused the snapshot.</param>
    /// <param name="stateJson">JSON-serialised aggregate state.</param>
    /// <param name="snapshotHash">SHA-256 hex hash of <paramref name="stateJson"/>.</param>
    /// <param name="tenantId">The owning tenant identifier.</param>
    /// <returns>A new <see cref="AggregateSnapshot"/> instance.</returns>
    public static AggregateSnapshot Create(
        Guid aggregateId,
        string aggregateType,
        int version,
        DateTimeOffset snapshotAt,
        Guid triggerEventId,
        string stateJson,
        string snapshotHash,
        Guid tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
        ArgumentException.ThrowIfNullOrWhiteSpace(stateJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(snapshotHash);

        return new AggregateSnapshot
        {
            Id             = Guid.NewGuid(),
            AggregateId    = aggregateId,
            AggregateType  = aggregateType,
            Version        = version,
            SnapshotAt     = snapshotAt,
            TriggerEventId = triggerEventId,
            StateJson      = stateJson,
            SnapshotHash   = snapshotHash,
            TenantId       = tenantId,
        };
    }
}
