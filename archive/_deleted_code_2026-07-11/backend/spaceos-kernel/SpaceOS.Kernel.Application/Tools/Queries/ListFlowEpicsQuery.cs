// SpaceOS.Kernel.Application/Tools/Queries/ListFlowEpicsQuery.cs
using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;

namespace SpaceOS.Kernel.Application.Tools.Queries;

/// <summary>Returns a paginated list of FlowEpics for the authenticated tenant.</summary>
/// <param name="TenantId">Tenant identifier from JWT claim.</param>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Items per page (max 50).</param>
public sealed record ListFlowEpicsQuery(Guid TenantId, int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedList<FlowEpicSummaryDto>>>;
