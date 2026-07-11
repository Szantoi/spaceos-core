// SpaceOS.Modules.Abstractions/Catalog/ISupplierCatalog.cs
namespace SpaceOS.Modules.Abstractions.Catalog;

/// <summary>
/// Provides real-time material availability and pricing data from a supplier's
/// catalogue.
/// </summary>
public interface ISupplierCatalog
{
    /// <summary>
    /// Returns all materials listed in the catalogue of <paramref name="supplierId"/>.
    /// </summary>
    /// <param name="supplierId">The supplier identifier to query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of material specs; never <c>null</c>, may be empty.</returns>
    Task<IReadOnlyList<IMaterialSpec>> GetMaterialsAsync(Guid supplierId, CancellationToken ct = default);

    /// <summary>
    /// Returns the current unit price for <paramref name="materialCode"/> from
    /// <paramref name="supplierId"/>, or <c>null</c> when the material is not priced.
    /// </summary>
    /// <param name="supplierId">The supplier to query.</param>
    /// <param name="materialCode">The supplier material code to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Unit price as a <see cref="decimal"/>, or <c>null</c>.</returns>
    Task<decimal?> GetPriceAsync(Guid supplierId, string materialCode, CancellationToken ct = default);

    /// <summary>
    /// Returns the current stock quantity for <paramref name="materialCode"/> held
    /// by <paramref name="supplierId"/>, or <c>null</c> when stock data is unavailable.
    /// </summary>
    /// <param name="supplierId">The supplier to query.</param>
    /// <param name="materialCode">The supplier material code to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Available stock count as an <see cref="int"/>, or <c>null</c>.</returns>
    Task<int?> GetStockAsync(Guid supplierId, string materialCode, CancellationToken ct = default);
}
