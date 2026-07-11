// SpaceOS.Kernel.Application/Snapshots/Queries/GetSnapshotAtQuery.cs

using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.Snapshots.Queries;

/// <summary>
/// Returns the most recent snapshot for the given aggregate taken at or before the specified timestamp.
/// </summary>
/// <param name="AggregateId">The aggregate to look up.</param>
/// <param name="At">The upper-bound timestamp (inclusive).</param>
public sealed record GetSnapshotAtQuery(
    Guid AggregateId,
    DateTimeOffset At) : IRequest<Result<SnapshotDto>>;
