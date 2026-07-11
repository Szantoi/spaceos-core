namespace SpaceOS.Modules.Joinery.Domain.Entities;

/// <summary>
/// Transactional outbox entry for reliable domain event publication.
/// Written in the same DB transaction as the originating aggregate mutation,
/// then dispatched asynchronously by a background worker.
/// </summary>
public sealed class JoineryOutboxEntry
{
    private const int MaxRetryCount = 3;
    private const int MaxErrorLength = 2000;

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public DateTimeOffset? FailedAt { get; private set; }
    public string? Error { get; private set; }
    public int RetryCount { get; private set; }

    private JoineryOutboxEntry() { } // EF Core

    /// <summary>
    /// Creates a new pending outbox entry.
    /// </summary>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="eventType">Fully-qualified domain event type name.</param>
    /// <param name="payloadJson">JSON-serialized event payload.</param>
    /// <param name="createdAt">UTC creation timestamp.</param>
    public static JoineryOutboxEntry Create(
        Guid tenantId,
        string eventType,
        string payloadJson,
        DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadJson);

        return new JoineryOutboxEntry
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EventType = eventType,
            PayloadJson = payloadJson,
            CreatedAt = createdAt,
            RetryCount = 0
        };
    }

    /// <summary>
    /// Marks this entry as successfully processed.
    /// </summary>
    /// <param name="processedAt">UTC timestamp of successful processing.</param>
    public void MarkProcessed(DateTimeOffset processedAt) => ProcessedAt = processedAt;

    /// <summary>
    /// Increments the retry counter and, when <see cref="MaxRetryCount"/> is reached,
    /// marks the entry as permanently failed.
    /// </summary>
    /// <param name="error">Error message describing the failure.</param>
    /// <param name="failedAt">UTC timestamp used when marking permanently failed.</param>
    public void IncrementRetry(string error, DateTimeOffset failedAt)
    {
        RetryCount++;

        if (RetryCount >= MaxRetryCount)
        {
            FailedAt = failedAt;
            Error = error.Length > MaxErrorLength
                ? error[..MaxErrorLength]
                : error;
        }
    }
}
