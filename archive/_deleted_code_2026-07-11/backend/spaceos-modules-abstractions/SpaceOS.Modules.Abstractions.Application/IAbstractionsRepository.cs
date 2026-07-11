using SpaceOS.Modules.Abstractions.Domain.Aggregates;

namespace SpaceOS.Modules.Abstractions.Application;

public interface IAbstractionsRepository
{
    Task<ProductTemplate?> GetTemplateAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<ProductTemplate?> GetTemplateWithAllAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<ProductTemplate>> ListTemplatesAsync(
        Guid tenantId, string? tradeTypeFilter, int page, int pageSize, CancellationToken ct = default);
    Task<ProductTemplate?> GetTemplateByNameWithAllAsync(string name, Guid tenantId, CancellationToken ct = default);
    Task<int> GetMaxVersionAsync(Guid tenantId, string name, CancellationToken ct = default);
    Task AddTemplateAsync(ProductTemplate template, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
