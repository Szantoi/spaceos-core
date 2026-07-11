// SpaceOS.Kernel.Application/Snapshots/Queries/GetSnapshotVersionsQueryHandler.cs

using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Domain.Snapshots;

namespace SpaceOS.Kernel.Application.Snapshots.Queries;

/// <summary>
/// Handles <see cref="GetSnapshotVersionsQuery"/>: returns all snapshot versions for an aggregate.
/// </summary>
internal sealed class GetSnapshotVersionsQueryHandler
    : IRequestHandler<GetSnapshotVersionsQuery, Result<IReadOnlyList<SnapshotDto>>>
{
    private readonly IAggregateSnapshotRepository _repository;

    /// <summary>Initialises a new <see cref="GetSnapshotVersionsQueryHandler"/>.</summary>
    /// <param name="repository">The snapshot repository.</param>
    public GetSnapshotVersionsQueryHandler(IAggregateSnapshotRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    /// <summary>Executes the query and returns all snapshot DTOs for the aggregate.</summary>
    public async Task<Result<IReadOnlyList<SnapshotDto>>> Handle(
        GetSnapshotVersionsQuery request,
        CancellationToken ct)
    {
        var snapshots = await _repository
            .ListByAggregateAsync(request.AggregateId, ct)
            .ConfigureAwait(false);

        var dtos = snapshots
            .Select(static s => new SnapshotDto(
                s.Id, s.AggregateId, s.AggregateType, s.Version,
                s.SnapshotAt, s.StateJson, s.SnapshotHash, s.TenantId))
            .ToList()
            .AsReadOnly();

        return Result.Success<IReadOnlyList<SnapshotDto>>(dtos);
    }
}
