// SpaceOS.Kernel.Application/Snapshots/SnapshotService.cs

using SpaceOS.Kernel.Domain.Common;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.Snapshots;

namespace SpaceOS.Kernel.Application.Snapshots;

/// <summary>
/// Default implementation of <see cref="ISnapshotService"/>.
/// Retrieves the next version number from the repository, serialises the aggregate
/// via <see cref="ISnapshotable.ToSnapshotJson"/>, computes a SHA-256 hash,
/// and persists the resulting <see cref="AggregateSnapshot"/>.
/// </summary>
internal sealed class SnapshotService : ISnapshotService
{
    private readonly IAggregateSnapshotRepository _repository;

    /// <summary>Initialises a new <see cref="SnapshotService"/>.</summary>
    /// <param name="repository">The append-only snapshot repository.</param>
    public SnapshotService(IAggregateSnapshotRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    /// <inheritdoc/>
    public async Task TakeSnapshotAsync<T>(
        T aggregate,
        AggregateType type,
        Guid? triggerEventId,
        CancellationToken ct)
        where T : AggregateRoot, ISnapshotable
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        // BE-P3B-01: use ISnapshotable.ToSnapshotJson(), NOT JsonSerializer.Serialize(aggregate)
        var stateJson    = aggregate.ToSnapshotJson();
        var snapshotHash = ComputeSha256Hex(stateJson);

        // Determine the next monotonically increasing version
        var latest      = await _repository.GetLatestAsync(GetAggregateId(aggregate), ct).ConfigureAwait(false);
        var nextVersion  = (latest?.Version ?? 0) + 1;

        var snapshot = AggregateSnapshot.Create(
            aggregateId:    GetAggregateId(aggregate),
            aggregateType:  type.ToString(),
            version:        nextVersion,
            snapshotAt:     DateTimeOffset.UtcNow,
            triggerEventId: triggerEventId ?? Guid.NewGuid(),
            stateJson:      stateJson,
            snapshotHash:   snapshotHash,
            tenantId:       GetTenantId(aggregate));

        await _repository.AddAsync(snapshot, ct).ConfigureAwait(false);
    }

    private static string ComputeSha256Hex(string input)
    {
        var bytes     = System.Text.Encoding.UTF8.GetBytes(input);
        var hashBytes = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    // Aggregate roots don't expose a universal Id property in the base class,
    // so we use dynamic dispatch via runtime reflection of the known Id types.
    private static Guid GetAggregateId(AggregateRoot aggregate) => aggregate switch
    {
        Domain.Entities.FlowEpic fe => fe.Id.Value,
        Domain.Entities.SpaceLayer sl => sl.Id.Value,
        _ => throw new InvalidOperationException(
            $"Cannot determine aggregate ID for type '{aggregate.GetType().Name}'. " +
            "Add a case to SnapshotService.GetAggregateId().")
    };

    private static Guid GetTenantId(AggregateRoot aggregate) => aggregate switch
    {
        Domain.Entities.FlowEpic fe => fe.TenantId.Value,
        Domain.Entities.SpaceLayer sl => sl.TenantId.Value,
        _ => throw new InvalidOperationException(
            $"Cannot determine tenant ID for type '{aggregate.GetType().Name}'. " +
            "Add a case to SnapshotService.GetTenantId().")
    };
}
