using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.Tenants.Queries;

/// <summary>
/// Handles <see cref="GetTenantByIdQuery"/>: loads a Tenant by its identifier
/// and projects it to a <see cref="TenantDto"/>.
/// </summary>
public class GetTenantByIdQueryHandler : IRequestHandler<GetTenantByIdQuery, Result<TenantDto>>
{
    private readonly ITenantRepository _tenantRepository;

    /// <summary>
    /// Initialises a new <see cref="GetTenantByIdQueryHandler"/>.
    /// </summary>
    /// <param name="tenantRepository">Repository for tenant lookups.</param>
    public GetTenantByIdQueryHandler(ITenantRepository tenantRepository)
    {
        ArgumentNullException.ThrowIfNull(tenantRepository);
        _tenantRepository = tenantRepository;
    }

    /// <inheritdoc/>
    public async Task<Result<TenantDto>> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken)
    {
        var id = TenantId.From(request.TenantId);
        var tenant = await _tenantRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);

        if (tenant is null)
            return Result.NotFound();

        return Result.Success(new TenantDto(
            tenant.Id.Value,
            tenant.Name.Value,
            tenant.TenantType.ToString(),
            tenant.EnabledModules));
    }
}
