// SpaceOS.Modules.Abstractions/Catalog/IProductCatalog.cs
using SpaceOS.Modules.Abstractions.Products;

namespace SpaceOS.Modules.Abstractions.Catalog;

/// <summary>
/// Read-only query surface for a tenant's parametric product catalogue.
/// </summary>
public interface IProductCatalog
{
    /// <summary>
    /// Returns all parametric products belonging to <paramref name="tenantId"/>.
    /// </summary>
    /// <param name="tenantId">The tenant whose catalogue is being queried.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of products; never <c>null</c>, may be empty.</returns>
    Task<IReadOnlyList<IParametricProduct>> GetProductsAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>
    /// Returns the parametric product with the given identifier, or <c>null</c> when
    /// no product with that identifier exists in the catalogue.
    /// </summary>
    /// <param name="productId">The product identifier to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching product, or <c>null</c>.</returns>
    Task<IParametricProduct?> GetByIdAsync(Guid productId, CancellationToken ct = default);
}
