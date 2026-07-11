// SpaceOS.Modules.Abstractions/Sync/ISyncable.cs
namespace SpaceOS.Modules.Abstractions.Sync;

/// <summary>
/// Marks an entity as eligible for two-way synchronisation with the SpaceOS Kernel.
/// </summary>
public interface ISyncable
{
    /// <summary>Gets the unique identifier of this entity.</summary>
    Guid Id { get; }

    /// <summary>
    /// Gets a value indicating whether this entity has been successfully synchronised
    /// with the Kernel at least once.
    /// </summary>
    bool IsSyncedToKernel { get; }

    /// <summary>
    /// Gets the UTC timestamp of the most recent successful sync, or <c>null</c> if the
    /// entity has never been synchronised.
    /// </summary>
    DateTimeOffset? LastSyncAt { get; }
}
