// SpaceOS.Kernel.Domain/Outbox/OutboxMessage.cs

namespace SpaceOS.Kernel.Domain.Outbox;

/// <summary>
/// Represents an outgoing integration message stored in the transactional outbox.
/// Messages are written atomically with the business operation and processed asynchronously.
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>Gets the unique identifier of this outbox message.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the message type discriminator (e.g. "EscrowTrigger").</summary>
    public string Type { get; private set; } = string.Empty;

    /// <summary>Gets the JSON-serialised message payload.</summary>
    public string Payload { get; private set; } = string.Empty;

    /// <summary>Gets the UTC timestamp when this message was created.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Gets the UTC timestamp when this message was successfully processed, or <see langword="null"/> if still pending.</summary>
    public DateTimeOffset? ProcessedAt { get; private set; }

    /// <summary>Gets the identifier of the tenant that owns this message.</summary>
    public Guid TenantId { get; private set; }

    // ── Phase 4 extension fields (nullable for backward compatibility with Phase 3 records) ──

    /// <summary>Gets the batch identifier grouping related messages, or <see langword="null"/> for unbatched messages.</summary>
    public Guid? BatchId { get; private set; }

    /// <summary>Gets the zero-based sequence number within <see cref="BatchId"/>, or <see langword="null"/> for unbatched messages.</summary>
    public int? BatchSequenceNumber { get; private set; }

    /// <summary>Gets the identifier of the aggregate that produced this message, or <see langword="null"/> if not tracked.</summary>
    public Guid? AggregateId { get; private set; }

    /// <summary>Gets the type name of the aggregate (e.g. "CuttingSheet"), or <see langword="null"/> if not tracked.</summary>
    public string? AggregateType { get; private set; }

    /// <summary>Gets the domain event type name (e.g. "CuttingSheetSubmitted"), or <see langword="null"/> if not tracked.</summary>
    public string? EventType { get; private set; }

    /// <summary>Gets the current processing status of this message.</summary>
    public OutboxStatus Status { get; private set; } = OutboxStatus.Pending;

    /// <summary>Gets the number of dispatch attempts made for this message.</summary>
    public int Attempts { get; private set; }

    /// <summary>Gets the last error message if dispatch failed, or <see langword="null"/> if no failure occurred.</summary>
    public string? LastError { get; private set; }

    // EF Core parameterless constructor
    private OutboxMessage() { }

    /// <summary>
    /// Creates a new pending <see cref="OutboxMessage"/>.
    /// </summary>
    /// <param name="type">The message type discriminator.</param>
    /// <param name="payload">The JSON-serialised payload.</param>
    /// <param name="tenantId">The owning tenant identifier.</param>
    /// <returns>A new <see cref="OutboxMessage"/> with <see cref="Status"/> set to <see cref="OutboxStatus.Pending"/>.</returns>
    public static OutboxMessage Create(string type, string payload, Guid tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        return new OutboxMessage
        {
            Id        = Guid.NewGuid(),
            Type      = type,
            Payload   = payload,
            CreatedAt = DateTimeOffset.UtcNow,
            TenantId  = tenantId,
            Status    = OutboxStatus.Pending,
            Attempts  = 0,
        };
    }

    /// <summary>
    /// Creates a new pending <see cref="OutboxMessage"/> with Phase 4 batch tracking fields.
    /// </summary>
    /// <param name="type">The message type discriminator.</param>
    /// <param name="payload">The JSON-serialised payload.</param>
    /// <param name="tenantId">The owning tenant identifier.</param>
    /// <param name="batchId">The batch grouping identifier.</param>
    /// <param name="batchSequenceNumber">The zero-based sequence number within the batch.</param>
    /// <param name="aggregateId">The identifier of the source aggregate.</param>
    /// <param name="aggregateType">The type name of the source aggregate.</param>
    /// <param name="eventType">The domain event type name.</param>
    /// <returns>A new <see cref="OutboxMessage"/> with batch tracking fields set.</returns>
    public static OutboxMessage Create(
        string type,
        string payload,
        Guid tenantId,
        Guid batchId,
        int batchSequenceNumber,
        Guid aggregateId,
        string aggregateType,
        string eventType)
    {
        var message = Create(type, payload, tenantId);
        message.BatchId             = batchId;
        message.BatchSequenceNumber = batchSequenceNumber;
        message.AggregateId         = aggregateId;
        message.AggregateType       = aggregateType;
        message.EventType           = eventType;
        return message;
    }

    /// <summary>
    /// Marks this message as successfully processed.
    /// </summary>
    /// <param name="processedAt">The UTC timestamp of successful processing.</param>
    public void MarkProcessed(DateTimeOffset processedAt)
    {
        ProcessedAt = processedAt;
        Status      = OutboxStatus.Processed;
    }

    /// <summary>
    /// Marks this message as failed, incrementing the attempt counter and recording the error.
    /// </summary>
    /// <param name="errorMessage">The error message to record.</param>
    public void MarkFailed(string errorMessage)
    {
        Attempts++;
        LastError = errorMessage;
        Status    = OutboxStatus.Failed;
    }

    /// <summary>
    /// Resets a failed message back to <see cref="OutboxStatus.Pending"/> for retry.
    /// </summary>
    public void ResetToPending()
    {
        Status    = OutboxStatus.Pending;
        LastError = null;
    }
}
