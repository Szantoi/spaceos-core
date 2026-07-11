using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Joinery.Application.Products.Repositories;
using SpaceOS.Modules.Joinery.Domain.Entities;

namespace SpaceOS.Modules.Joinery.Infrastructure.Persistence.Repositories;

public sealed class ProductConfigurationRepository : IProductConfigurationRepository
{
    private readonly JoineryDbContext _context;

    public ProductConfigurationRepository(JoineryDbContext context)
    {
        _context = context;
    }

    public async Task<ProductConfiguration?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct)
    {
        return await _context.ProductConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId, ct)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(ProductConfiguration config, CancellationToken ct)
    {
        await _context.ProductConfigurations.AddAsync(config, ct).ConfigureAwait(false);
        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
