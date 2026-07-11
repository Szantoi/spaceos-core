// SpaceOS.Modules.FlowManagement/Domain/OfflineSyncQueueItem.cs
namespace SpaceOS.Modules.FlowManagement.Domain;

/// <summary>
/// Represents a queued offline sync operation awaiting ingestion by the Kernel.
/// Items expire after 30 days. The (TenantId, ClientSignalId) pair is unique
/// to prevent duplicate processing of the same client-generated signal.
/// </summary>
public sealed class OfflineSyncQueueItem
{
    /// <summary>Gets the unique identifier of this queue item.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the tenant that owns this queue item.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Gets the serialised payload of the queued operation.</summary>
    public string Payload { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the client-generated idempotency key.
    /// Combined with <see cref="TenantId"/>, this must be unique in the queue.
    /// </summary>
    public Guid ClientSignalId { get; private set; }

    /// <summary>Gets the UTC timestamp after which this item may be purged.</summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>Gets the UTC timestamp when this item was created.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    private OfflineSyncQueueItem() { }

    /// <summary>
    /// Creates a new <see cref="OfflineSyncQueueItem"/> that expires 30 days from now.
    /// </summary>
    /// <param name="tenantId">The tenant that owns this queue item.</param>
    /// <param name="payload">The serialised payload. Must not be null or whitespace.</param>
    /// <param name="clientSignalId">The client-generated idempotency key.</param>
    /// <returns>A new <see cref="OfflineSyncQueueItem"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="payload"/> is null or whitespace.</exception>
    public static OfflineSyncQueueItem Create(Guid tenantId, string payload, Guid clientSignalId)
    {
        if (string.IsNullOrWhiteSpace(payload))
            throw new ArgumentException("Payload cannot be empty.", nameof(payload));

        return new OfflineSyncQueueItem
        {
            Id            = Guid.NewGuid(),
            TenantId      = tenantId,
            Payload       = payload,
            ClientSignalId = clientSignalId,
            ExpiresAt     = DateTimeOffset.UtcNow.AddDays(30),
            CreatedAt     = DateTimeOffset.UtcNow,
        };
    }
}
