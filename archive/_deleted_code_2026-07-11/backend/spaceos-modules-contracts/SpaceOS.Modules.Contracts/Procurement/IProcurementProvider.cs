using Ardalis.Result;
using SpaceOS.Modules.Contracts.Procurement.DTOs;
using SpaceOS.Modules.Contracts.Procurement.Requests;
using SpaceOS.Modules.Contracts.Shared;

namespace SpaceOS.Modules.Contracts.Procurement;

/// <summary>
/// Contract for procurement/purchasing.
/// TenantId resolved from JWT — not in request parameters (SEC-01).
/// </summary>
public interface IProcurementProvider : IModuleProvider
{
    /// <summary>
    /// Creates a new purchase order.
    /// Requires <see cref="ProviderCapability.ProcurementOrder"/> capability.
    /// </summary>
    /// <param name="request">Purchase order details.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The new purchase order identifier on success.</returns>
    Task<Result<Guid>> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request, CancellationToken ct);

    /// <summary>
    /// Retrieves a purchase order by its identifier.
    /// </summary>
    /// <param name="orderId">The purchase order identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The purchase order DTO on success.</returns>
    Task<Result<PurchaseOrderDto>> GetPurchaseOrderAsync(Guid orderId, CancellationToken ct);

    /// <summary>
    /// Returns the current price list entries for a material across all suppliers.
    /// Requires <see cref="ProviderCapability.ProcurementPricing"/> capability.
    /// </summary>
    /// <param name="materialCode">Material code to query prices for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>All active price list entries for the material.</returns>
    Task<Result<IReadOnlyList<PriceListEntryDto>>> GetPricesAsync(string materialCode, CancellationToken ct);

    /// <summary>
    /// Records a physical delivery against an existing purchase order.
    /// </summary>
    /// <param name="delivery">Delivery details including line-by-line received quantities.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Success or failure result.</returns>
    Task<Result> RecordDeliveryAsync(DeliveryDto delivery, CancellationToken ct);

    /// <summary>
    /// Returns the aggregated performance rating for a supplier.
    /// Requires <see cref="ProviderCapability.ProcurementRating"/> capability.
    /// </summary>
    /// <param name="supplierId">Identifier of the supplier to rate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The supplier rating on success.</returns>
    Task<Result<SupplierRatingDto>> GetSupplierRatingAsync(Guid supplierId, CancellationToken ct);
}
