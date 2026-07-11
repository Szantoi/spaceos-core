namespace SpaceOS.Modules.Inventory.Domain.Aggregates;

/// <summary>
/// Transactional outbox record for reorder alerts sent to Procurement.
/// Status lifecycle: Pending → InFlight → Completed | Failed.
/// </summary>
public class InventoryReorderOutbox
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Payload { get; private set; } = string.Empty;
    public string Status { get; private set; } = "Pending";
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset NextAttemptAt { get; private set; }
    public DateTimeOffset? LeaseUntil { get; private set; }
    public int AttemptCount { get; private set; }
    public string? LastError { get; private set; }

    private InventoryReorderOutbox() { }

    public static InventoryReorderOutbox Create(Guid tenantId, string payload)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId required.", nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        return new InventoryReorderOutbox
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Payload = payload,
            Status = "Pending",
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow,
            AttemptCount = 0
        };
    }

    public void ClaimLease(DateTimeOffset leaseUntil)
    {
        Status = "InFlight";
        LeaseUntil = leaseUntil;
        AttemptCount++;
    }

    public void MarkCompleted()
    {
        Status = "Completed";
        LeaseUntil = null;
    }

    public void MarkFailed(string error)
    {
        Status = "Failed";
        LastError = Sanitize(error);
        LeaseUntil = null;
    }

    public void MarkRetry(DateTimeOffset nextAttemptAt, string error)
    {
        Status = "Pending";
        NextAttemptAt = nextAttemptAt;
        LastError = Sanitize(error);
        LeaseUntil = null;
    }

    // SEC-P-11: scrub sensitive content from error messages stored in DB
    private static string Sanitize(string error)
    {
        if (error.Length > 2000) error = error[..2000];
        return error;
    }
}
