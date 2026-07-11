using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Joinery.Application.Orders.DTOs;

namespace SpaceOS.Modules.Joinery.Application.Orders.Queries.GetSnapshots;

/// <summary>
/// Returns the latest cutting list snapshots for the given order.
/// </summary>
/// <param name="OrderId">The door order identifier.</param>
/// <param name="TenantId">The requesting tenant.</param>
public sealed record GetSnapshotsQuery(Guid OrderId, Guid TenantId) : IRequest<Result<IReadOnlyList<SnapshotSummaryDto>>>;
