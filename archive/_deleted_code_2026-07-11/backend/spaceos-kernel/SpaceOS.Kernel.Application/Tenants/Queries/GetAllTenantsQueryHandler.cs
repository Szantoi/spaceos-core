// SpaceOS.Kernel.Application/Tenants/Queries/GetAllTenantsQueryHandler.cs
using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.Specifications;

namespace SpaceOS.Kernel.Application.Tenants.Queries;

/// <summary>
/// Handles <see cref="GetAllTenantsQuery"/>: returns a paged list of tenants projected to <see cref="TenantDto"/>,
/// optionally filtered by <c>TenantType</c>.
/// </summary>
public sealed class GetAllTenantsQueryHandler
    : IRequestHandler<GetAllTenantsQuery, Result<PagedList<TenantDto>>>
{
    private readonly ITenantRepository _tenantRepository;

    /// <summary>Initialises a new <see cref="GetAllTenantsQueryHandler"/>.</summary>
    /// <param name="tenantRepository">The tenant repository.</param>
    public GetAllTenantsQueryHandler(ITenantRepository tenantRepository)
    {
        ArgumentNullException.ThrowIfNull(tenantRepository);
        _tenantRepository = tenantRepository;
    }

    /// <summary>Executes the query and returns a paged result of tenant DTOs.</summary>
    public async Task<Result<PagedList<TenantDto>>> Handle(
        GetAllTenantsQuery request,
        CancellationToken ct)
    {
        int totalCount;
        IReadOnlyList<Domain.Entities.Tenant> tenants;

        if (request.TenantTypeFilter.HasValue)
        {
            var type = request.TenantTypeFilter.Value;
            totalCount = await _tenantRepository
                .CountAsync(new AllTenantsByTypeSpec(type), ct)
                .ConfigureAwait(false);
            tenants = await _tenantRepository
                .ListAsync(new AllTenantsByTypePagedSpec(type, request.Page, request.PageSize), ct)
                .ConfigureAwait(false);
        }
        else
        {
            totalCount = await _tenantRepository
                .CountAsync(new AllTenantsSpec(), ct)
                .ConfigureAwait(false);
            tenants = await _tenantRepository
                .ListAsync(new AllTenantsPagedSpec(request.Page, request.PageSize), ct)
                .ConfigureAwait(false);
        }

        var items = tenants
            .Select(t => new TenantDto(
                t.Id.Value,
                t.Name.Value,
                t.TenantType.ToString(),
                t.EnabledModules))
            .ToList()
            .AsReadOnly();

        return Result.Success(new PagedList<TenantDto>(items, request.Page, request.PageSize, totalCount));
    }
}
