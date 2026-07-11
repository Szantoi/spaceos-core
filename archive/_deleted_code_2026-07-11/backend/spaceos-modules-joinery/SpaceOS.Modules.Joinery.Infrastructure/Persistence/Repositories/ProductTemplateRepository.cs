using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Joinery.Application.Products.Repositories;
using SpaceOS.Modules.Joinery.Domain.Entities;

namespace SpaceOS.Modules.Joinery.Infrastructure.Persistence.Repositories;

public sealed class ProductTemplateRepository : IProductTemplateRepository
{
    private readonly JoineryDbContext _context;

    public ProductTemplateRepository(JoineryDbContext context)
    {
        _context = context;
    }

    public async Task<ProductTemplate?> GetByIdAsync(string id, CancellationToken ct)
    {
        return await _context.ProductTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ProductTemplate>> GetAllAsync(CancellationToken ct)
    {
        return await _context.ProductTemplates
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
