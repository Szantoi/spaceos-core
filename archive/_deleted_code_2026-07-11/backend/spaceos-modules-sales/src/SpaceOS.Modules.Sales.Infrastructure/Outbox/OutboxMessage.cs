using SpaceOS.Modules.Sales.Domain.Common;

namespace SpaceOS.Modules.Sales.Infrastructure.Outbox;

/// <summary>
/// Transactional outbox message entity. Drives the ADR-039 Joinery order conversion flow.
/// Lifecycle: Pending → InFlight → Completed | Failed.
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; private set; }

    /// <summary>Owning tenant.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>The aggregate this message refers to (e.g. Quote.Id).</summary>
    public Guid AggregateId { get; private set; }

    /// <summary>Domain operation name (e.g. "QuoteConversionRequested").</summary>
    public string Operation { get; private set; } = default!;

    /// <summary>JSON-serialised payload.</summary>
    public string PayloadJson { get; private set; } = default!;

    /// <summary>Idempotency key — typically the aggregate ID without hyphens.</summary>
    public string IdempotencyKey { get; private set; } = default!;

    /// <summary>Processing status: Pending, InFlight, Completed, Failed.</summary>
    public string Status { get; private set; } = "Pending";

    /// <summary>Number of delivery attempts so far.</summary>
    public int AttemptCount { get; private set; }

    /// <summary>When the worker may next attempt this message.</summary>
    public DateTimeOffset NextAttemptAt { get; private set; }

    /// <summary>When the message was created.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>When the message was successfully processed.</summary>
    public DateTimeOffset? ProcessedAt { get; private set; }

    /// <summary>Type name of the last exception (SEC-S-10: no payload, no ex.Message).</summary>
    public string? LastError { get; private set; }

    private OutboxMessage() { } // EF Core

    /// <summary>Creates a new Pending outbox message.</summary>
    public static OutboxMessage Create(
        Guid tenantId,
        Guid aggregateId,
        string operation,
        string payloadJson,
        string idempotencyKey,
        IClock clock) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        AggregateId = aggregateId,
        Operation = operation,
        PayloadJson = payloadJson,
        IdempotencyKey = idempotencyKey,
        Status = "Pending",
        AttemptCount = 0,
        NextAttemptAt = clock.UtcNow,
        CreatedAt = clock.UtcNow
    };

    /// <summary>Locks the message for processing by a single worker (lease = <paramref name="leaseSeconds"/>s).</summary>
    public void MarkInFlight(IClock clock, int leaseSeconds = 60)
    {
        Status = "InFlight";
        AttemptCount++;
        NextAttemptAt = clock.UtcNow.AddSeconds(leaseSeconds);
    }

    /// <summary>Records a successful delivery.</summary>
    public void MarkCompleted(IClock clock)
    {
        Status = "Completed";
        ProcessedAt = clock.UtcNow;
    }

    /// <summary>
    /// Records a delivery failure. Uses exponential back-off and transitions to Failed after
    /// <paramref name="maxAttempts"/> retries.
    /// </summary>
    public void RecordFailure(string errorType, IClock clock, int maxAttempts)
    {
        LastError = errorType;
        if (AttemptCount >= maxAttempts)
            Status = "Failed";
        else
        {
            Status = "Pending";
            NextAttemptAt = clock.UtcNow.AddSeconds(Math.Pow(2, AttemptCount) * 5);
        }
    }
}
