using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Abstractions.Application;
using SpaceOS.Modules.Abstractions.Domain.Aggregates;
using SpaceOS.Modules.Abstractions.Domain.Entities;

namespace SpaceOS.Modules.Abstractions.Infrastructure.Persistence;

public sealed class AbstractionsRepository : IAbstractionsRepository
{
    private readonly AbstractionsDbContext _db;

    public AbstractionsRepository(AbstractionsDbContext db)
    {
        _db = db;
    }

    public async Task<ProductTemplate?> GetTemplateAsync(Guid id, Guid tenantId, CancellationToken ct = default) =>
        await _db.ProductTemplates.AsNoTracking()
                 .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId, ct).ConfigureAwait(false);

    public async Task<ProductTemplate?> GetTemplateWithAllAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        var template = await _db.ProductTemplates
                                .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId, ct).ConfigureAwait(false);
        if (template == null) return null;

        var slots = await _db.ComponentSlots
                             .Where(s => s.TemplateId == id)
                             .OrderBy(s => s.SortOrder)
                             .ToListAsync(ct).ConfigureAwait(false);
        var connections = await _db.SlotConnections
                                   .Where(c => c.TemplateId == id)
                                   .ToListAsync(ct).ConfigureAwait(false);
        var parameters = await _db.TemplateParameters
                                  .Where(p => p.TemplateId == id)
                                  .ToListAsync(ct).ConfigureAwait(false);

        template.LoadCollections(slots, connections, parameters);
        return template;
    }

    public async Task<IReadOnlyList<ProductTemplate>> ListTemplatesAsync(
        Guid tenantId, string? tradeTypeFilter, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.ProductTemplates.AsNoTracking()
                       .Where(t => t.TenantId == tenantId && t.IsActive && !t.IsArchived);
        if (!string.IsNullOrWhiteSpace(tradeTypeFilter))
            query = query.Where(t => t.TradeType == tradeTypeFilter.ToLowerInvariant());
        return await query.OrderBy(t => t.Name)
                          .Skip((page - 1) * pageSize).Take(pageSize)
                          .ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<ProductTemplate?> GetTemplateByNameWithAllAsync(string name, Guid tenantId, CancellationToken ct = default)
    {
        var template = await _db.ProductTemplates
                                .FirstOrDefaultAsync(
                                    t => t.Name == name && t.TenantId == tenantId && t.IsActive && !t.IsArchived,
                                    ct).ConfigureAwait(false);
        if (template == null) return null;

        var slots = await _db.ComponentSlots
                             .Where(s => s.TemplateId == template.Id)
                             .OrderBy(s => s.SortOrder)
                             .ToListAsync(ct).ConfigureAwait(false);
        var connections = await _db.SlotConnections
                                   .Where(c => c.TemplateId == template.Id)
                                   .ToListAsync(ct).ConfigureAwait(false);
        var parameters = await _db.TemplateParameters
                                  .Where(p => p.TemplateId == template.Id)
                                  .ToListAsync(ct).ConfigureAwait(false);

        template.LoadCollections(slots, connections, parameters);
        return template;
    }

    public async Task<int> GetMaxVersionAsync(Guid tenantId, string name, CancellationToken ct = default)
    {
        var max = await _db.ProductTemplates.AsNoTracking()
                           .Where(t => t.TenantId == tenantId && t.Name == name)
                           .MaxAsync(t => (int?)t.Version, ct).ConfigureAwait(false);
        return max ?? 0;
    }

    public async Task AddTemplateAsync(ProductTemplate template, CancellationToken ct = default) =>
        await _db.ProductTemplates.AddAsync(template, ct).ConfigureAwait(false);

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
}
