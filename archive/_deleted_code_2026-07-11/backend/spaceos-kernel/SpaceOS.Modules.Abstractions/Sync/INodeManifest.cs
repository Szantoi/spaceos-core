// SpaceOS.Modules.Abstractions/Sync/INodeManifest.cs
namespace SpaceOS.Modules.Abstractions.Sync;

/// <summary>
/// Describes a registered SpaceOS node as seen by the Kernel during discovery
/// and heartbeat exchanges.
/// </summary>
public interface INodeManifest
{
    /// <summary>Gets the tenant identifier that owns this node.</summary>
    Guid TenantId { get; }

    /// <summary>Gets the base HTTPS URL at which this node's public API is reachable.</summary>
    string ServerUrl { get; }

    /// <summary>Gets the SIP API version advertised by this node (e.g. <c>"1.0"</c>).</summary>
    string PublicApiVersion { get; }

    /// <summary>
    /// Gets the UTC timestamp of the last heartbeat received from this node, or
    /// <c>null</c> when no heartbeat has been received yet.
    /// </summary>
    DateTimeOffset? LastHeartbeatAt { get; }

    /// <summary>
    /// Gets the maximum level-of-detail (LOD) that guest callers may request from
    /// this node. A value of <c>0</c> means guests receive no data.
    /// </summary>
    int MaxGuestLod { get; }
}
