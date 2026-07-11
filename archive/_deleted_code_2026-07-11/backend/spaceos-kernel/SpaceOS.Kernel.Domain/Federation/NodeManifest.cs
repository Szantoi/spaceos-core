// SpaceOS.Kernel.Domain/Federation/NodeManifest.cs
using SpaceOS.Kernel.Domain.Events;
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Federation;

/// <summary>
/// Aggregate root representing a remote SpaceOS node's federation manifest.
/// Tracks the node's public URL, API version, and heartbeat state for cross-node communication.
/// </summary>
public sealed class NodeManifest : AggregateRoot
{
    /// <summary>Gets the unique identifier of this manifest.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the identifier of the tenant that owns this node.</summary>
    public TenantId TenantId { get; init; }

    /// <summary>Gets the public base URL of the remote node.</summary>
    public string ServerUrl { get; private set; } = string.Empty;

    /// <summary>Gets the public API version advertised by this node.</summary>
    public string PublicApiVersion { get; private set; } = "1.0";

    /// <summary>Gets the UTC timestamp of the last received heartbeat, or <see langword="null"/> if never received.</summary>
    public DateTimeOffset? LastHeartbeatAt { get; private set; }

    /// <summary>Gets the UTC timestamp at which this manifest was first created.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Gets the UTC timestamp of the last update to this manifest.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Gets the optimistic concurrency token incremented on every mutation.</summary>
    public int Version { get; private set; } = 1;

    /// <summary>Gets the maximum guest Level-of-Detail the node is willing to serve. Defaults to 3.</summary>
    public int MaxGuestLod { get; private set; } = 3;

    /// <summary>EF Core parameterless constructor — not for application use.</summary>
    private NodeManifest() { }

    /// <summary>
    /// Creates a new <see cref="NodeManifest"/> for the given tenant and server URL.
    /// URL format validation is the responsibility of the Application layer (<c>INodeUrlValidator</c>).
    /// </summary>
    /// <param name="tenantId">The identifier of the owning tenant.</param>
    /// <param name="serverUrl">The public base URL of the remote node (max 2048 chars).</param>
    /// <returns>A freshly created <see cref="NodeManifest"/> instance.</returns>
    public static NodeManifest Create(TenantId tenantId, string serverUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverUrl);

        var now = DateTimeOffset.UtcNow;
        var manifest = new NodeManifest
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ServerUrl = serverUrl,
            CreatedAt = now,
            UpdatedAt = now
        };

        manifest.AddDomainEvent(new NodeRegisteredEvent(
            manifest.Id, tenantId, serverUrl, manifest.PublicApiVersion, now));

        return manifest;
    }

    /// <summary>
    /// Records a heartbeat, updating <see cref="LastHeartbeatAt"/> and incrementing the concurrency <see cref="Version"/>.
    /// Raises <see cref="NodeHeartbeatRecordedEvent"/> only when the online status changes (i.e. the node was
    /// previously silent for more than the threshold determined by the caller).
    /// </summary>
    /// <param name="isOnlineChanged">
    /// <see langword="true"/> when the node transitioned from offline/unknown to online; the Application layer
    /// is responsible for computing this flag before calling this method.
    /// </param>
    public void RecordHeartbeat(bool isOnlineChanged)
    {
        var now = DateTimeOffset.UtcNow;
        LastHeartbeatAt = now;
        UpdatedAt = now;
        Version++;

        if (isOnlineChanged)
            AddDomainEvent(new NodeHeartbeatRecordedEvent(Id, TenantId, true, now));
    }
}
