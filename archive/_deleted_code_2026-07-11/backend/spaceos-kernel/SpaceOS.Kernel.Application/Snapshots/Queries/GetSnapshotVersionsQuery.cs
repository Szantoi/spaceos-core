// SpaceOS.Kernel.Application/Snapshots/Queries/GetSnapshotVersionsQuery.cs

using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.Snapshots.Queries;

/// <summary>
/// Returns all snapshot versions recorded for the given aggregate, ordered by version ascending.
/// </summary>
/// <param name="AggregateId">The aggregate whose snapshot history to retrieve.</param>
public sealed record GetSnapshotVersionsQuery(Guid AggregateId)
    : IRequest<Result<IReadOnlyList<SnapshotDto>>>;
