// SpaceOS.Kernel.Domain/Sync/SyncSignal.cs
using SpaceOS.Kernel.Domain.Events;
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Domain.Sync;

/// <summary>
/// Aggregate root representing a state-change signal emitted by an offline module
/// and queued for eventual synchronisation to the Kernel.
/// Carries a hash-chained state fingerprint to guarantee ordered delivery and tamper-detection.
/// </summary>
public sealed class SyncSignal : AggregateRoot
{
    /// <summary>Gets the unique identifier of this signal.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the identifier of the flow epic this signal belongs to.</summary>
    public FlowEpicId EpicId { get; init; }

    /// <summary>Gets the identifier of the tenant that emitted this signal.</summary>
    public TenantId TenantId { get; init; }

    /// <summary>Gets the target workflow phase encoded as a string.</summary>
    public string NewState { get; private set; } = string.Empty;

    /// <summary>Gets the HMAC-SHA256 fingerprint of the new state.</summary>
    public string StateHash { get; private set; } = string.Empty;

    /// <summary>Gets the hash of the preceding signal in the chain, enabling ordered validation.</summary>
    public string PreviousHash { get; private set; } = string.Empty;

    /// <summary>Gets the client-generated UUID used for idempotent delivery.</summary>
    public Guid ClientSignalId { get; private set; }

    /// <summary>Gets a value indicating whether this signal has been acknowledged by the Kernel.</summary>
    public bool IsSyncedToKernel { get; private set; }

    /// <summary>Gets the UTC timestamp after which this signal is considered expired and may be discarded.</summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>Gets the UTC timestamp at which the signal was originally raised on the client.</summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>EF Core parameterless constructor — not for application use.</summary>
    private SyncSignal() { }

    /// <summary>
    /// Creates a new <see cref="SyncSignal"/> for offline state synchronisation.
    /// </summary>
    /// <param name="epicId">The flow epic this signal targets.</param>
    /// <param name="tenantId">The owning tenant.</param>
    /// <param name="newState">The target workflow phase as a string.</param>
    /// <param name="stateHash">HMAC-SHA256 fingerprint of the new state.</param>
    /// <param name="previousHash">Hash of the preceding signal in the chain.</param>
    /// <param name="clientSignalId">Client-generated UUID for idempotent delivery.</param>
    /// <returns>A new, unsynced <see cref="SyncSignal"/> instance.</returns>
    public static SyncSignal Create(
        FlowEpicId epicId,
        TenantId tenantId,
        string newState,
        string stateHash,
        string previousHash,
        Guid clientSignalId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newState);
        ArgumentException.ThrowIfNullOrWhiteSpace(stateHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(previousHash);

        var now = DateTimeOffset.UtcNow;
        var signal = new SyncSignal
        {
            Id = Guid.NewGuid(),
            EpicId = epicId,
            TenantId = tenantId,
            NewState = newState,
            StateHash = stateHash,
            PreviousHash = previousHash,
            ClientSignalId = clientSignalId,
            IsSyncedToKernel = false,
            ExpiresAt = now.AddDays(SyncConstants.OfflineQueueTtlDays),
            OccurredAt = now
        };

        signal.AddDomainEvent(new SyncSignalReceivedEvent(
            signal.Id, epicId, tenantId, newState, clientSignalId, now));

        return signal;
    }

    /// <summary>
    /// Marks this signal as acknowledged by the Kernel.
    /// </summary>
    public void MarkSynced()
    {
        IsSyncedToKernel = true;
    }
}
