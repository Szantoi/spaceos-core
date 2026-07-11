// SpaceOS.Modules.Abstractions/Sync/SyncConstants.cs
namespace SpaceOS.Modules.Abstractions.Sync;

/// <summary>
/// Compile-time constants for the SpaceOS Inter-node Protocol (SIP) sync layer.
/// These values are part of the public module contract and must not be changed
/// without a SIP version bump.
/// </summary>
public static class SyncConstants
{
    /// <summary>
    /// The all-zeros hash used as the previous-hash value for the very first audit
    /// entry in a chain (genesis block sentinel).
    /// </summary>
    public const string GenesisHash = "0000000000000000000000000000000000000000000000000000000000000000";

    /// <summary>
    /// The current SpaceOS Inter-node Protocol (SIP) version string.
    /// </summary>
    public const string SipVersion = "1.0";

    /// <summary>
    /// Lifetime in hours of a node-to-node JWT issued by <c>INodeAuthService</c>.
    /// </summary>
    public const int NodeJwtTtlHours = 72;

    /// <summary>
    /// Maximum age in days that an undelivered offline sync command is kept in the
    /// outbox before being discarded.
    /// </summary>
    public const int OfflineQueueTtlDays = 30;
}
