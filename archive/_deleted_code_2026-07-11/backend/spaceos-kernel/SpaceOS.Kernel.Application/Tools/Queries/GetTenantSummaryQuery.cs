// SpaceOS.Kernel.Application/Tools/Queries/GetTenantSummaryQuery.cs
using Ardalis.Result;
using MediatR;

namespace SpaceOS.Kernel.Application.Tools.Queries;

/// <summary>Returns aggregate counts for a tenant — used by the LLM Tool Registry.</summary>
/// <param name="TenantId">The tenant whose summary is requested. Sourced from JWT claim.</param>
public sealed record GetTenantSummaryQuery(Guid TenantId) : IRequest<Result<TenantSummaryDto>>;
