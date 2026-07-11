// SpaceOS.Kernel.Application/Tools/Queries/ListWorkStationsQuery.cs
using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;

namespace SpaceOS.Kernel.Application.Tools.Queries;

/// <summary>Returns a paginated list of WorkStations for the authenticated tenant.</summary>
/// <param name="TenantId">Tenant identifier from JWT claim.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Items per page (max 50).</param>
public sealed record ListWorkStationsQuery(Guid TenantId, int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedList<WorkStationSummaryDto>>>;
