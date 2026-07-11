// SpaceOS.Kernel.Application/Tenants/Queries/GetAllTenantsQuery.cs
using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Enums;

namespace SpaceOS.Kernel.Application.Tenants.Queries;

/// <summary>Returns a single page of tenants, optionally filtered by ecosystem actor type.</summary>
/// <param name="Page">1-based page number. Defaults to 1.</param>
/// <param name="PageSize">Maximum items per page. Defaults to 20, maximum 100.</param>
/// <param name="TenantTypeFilter">Optional <see cref="TenantType"/> filter. <c>null</c> returns all tenants.</param>
public sealed record GetAllTenantsQuery(
    int Page = 1,
    int PageSize = 20,
    TenantType? TenantTypeFilter = null) : IRequest<Result<PagedList<TenantDto>>>;
