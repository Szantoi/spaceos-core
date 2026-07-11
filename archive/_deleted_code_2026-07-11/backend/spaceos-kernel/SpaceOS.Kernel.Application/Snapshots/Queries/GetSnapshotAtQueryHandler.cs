// SpaceOS.Kernel.Application/Snapshots/Queries/GetSnapshotAtQueryHandler.cs

using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Domain.Snapshots;

namespace SpaceOS.Kernel.Application.Snapshots.Queries;

/// <summary>
/// Handles <see cref="GetSnapshotAtQuery"/>: returns the aggregate snapshot at or before
/// the requested timestamp.
/// </summary>
internal sealed class GetSnapshotAtQueryHandler
    : IRequestHandler<GetSnapshotAtQuery, Result<SnapshotDto>>
{
    private readonly IAggregateSnapshotRepository _repository;

    /// <summary>Initialises a new <see cref="GetSnapshotAtQueryHandler"/>.</summary>
    /// <param name="repository">The snapshot repository.</param>
    public GetSnapshotAtQueryHandler(IAggregateSnapshotRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    /// <summary>Executes the query and returns the matching snapshot DTO, or NotFound.</summary>
    public async Task<Result<SnapshotDto>> Handle(GetSnapshotAtQuery request, CancellationToken ct)
    {
        var snapshot = await _repository
            .GetAtTimestampAsync(request.AggregateId, request.At, ct)
            .ConfigureAwait(false);

        if (snapshot is null)
            return Result<SnapshotDto>.NotFound();

        return Result.Success(ToDto(snapshot));
    }

    private static SnapshotDto ToDto(AggregateSnapshot s) =>
        new(s.Id, s.AggregateId, s.AggregateType, s.Version, s.SnapshotAt, s.StateJson, s.SnapshotHash, s.TenantId);
}
