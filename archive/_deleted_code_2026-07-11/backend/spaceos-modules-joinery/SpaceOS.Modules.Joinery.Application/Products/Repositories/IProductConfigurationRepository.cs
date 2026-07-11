using SpaceOS.Modules.Joinery.Domain.Entities;

namespace SpaceOS.Modules.Joinery.Application.Products.Repositories;

public interface IProductConfigurationRepository
{
    Task<ProductConfiguration?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct);
    Task AddAsync(ProductConfiguration config, CancellationToken ct);
}
