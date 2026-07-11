using SpaceOS.Modules.Joinery.Domain.Entities;

namespace SpaceOS.Modules.Joinery.Application.Products.Repositories;

public interface IProductTemplateRepository
{
    Task<ProductTemplate?> GetByIdAsync(string id, CancellationToken ct);
    Task<IReadOnlyList<ProductTemplate>> GetAllAsync(CancellationToken ct);
}
