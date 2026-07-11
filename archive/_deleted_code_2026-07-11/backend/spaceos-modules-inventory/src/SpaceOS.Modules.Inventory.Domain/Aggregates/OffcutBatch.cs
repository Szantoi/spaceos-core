namespace SpaceOS.Modules.Inventory.Domain.Aggregates;

/// <summary>
/// Idempotency record for a batch offcut registration.
/// One OffcutBatch corresponds to N Offcut records created from a single plan freeze.
/// Unique constraint: (TenantId, SourceType, SourceId) — ensures a double freeze
/// does not duplicate Offcut rows.
/// </summary>
public sealed class OffcutBatch
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string SourceType { get; private set; } = string.Empty;
    public Guid SourceId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private OffcutBatch() { }

    public static OffcutBatch Create(Guid tenantId, string sourceType, Guid sourceId)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(sourceType)) throw new ArgumentException("SourceType required.", nameof(sourceType));
        if (sourceId == Guid.Empty) throw new ArgumentException("SourceId required.", nameof(sourceId));

        return new OffcutBatch
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SourceType = sourceType,
            SourceId = sourceId,
            CreatedAt = DateTime.UtcNow
        };
    }
}
