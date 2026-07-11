// SpaceOS.Modules.Abstractions/Products/IPricingResolver.cs
namespace SpaceOS.Modules.Abstractions.Products;

/// <summary>
/// Resolves the sell price for a fully-configured parametric product, incorporating
/// both the supplier's material cost and an optional dealer margin.
/// </summary>
public interface IPricingResolver
{
    /// <summary>
    /// Resolves the price for a product, including supplier cost and, when
    /// <paramref name="dealerId"/> is provided, the dealer's margin.
    /// </summary>
    /// <param name="productId">The identifier of the product to price.</param>
    /// <param name="supplierId">
    /// The identifier of the supplier whose material pricing is used as the
    /// cost base.
    /// </param>
    /// <param name="dealerId">
    /// The optional identifier of the dealer whose margin is applied to the
    /// supplier price. Pass <c>null</c> to return the supplier price only.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The resolved price as a non-negative <see cref="decimal"/>.</returns>
    Task<decimal> ResolvePriceAsync(
        Guid productId,
        Guid supplierId,
        Guid? dealerId,
        CancellationToken ct = default);
}
