// SpaceOS.Kernel.Domain/Sync/SyncConstants.cs
namespace SpaceOS.Kernel.Domain.Sync;

/// <summary>
/// Domain constants governing offline synchronisation behaviour.
/// </summary>
public static class SyncConstants
{
    /// <summary>Number of days an unprocessed sync signal remains valid in the offline queue.</summary>
    public const int OfflineQueueTtlDays = 30;

    /// <summary>Sentinel hash value used when no prior signal exists in the chain.</summary>
    public const string GenesisHash = "GENESIS";
}
