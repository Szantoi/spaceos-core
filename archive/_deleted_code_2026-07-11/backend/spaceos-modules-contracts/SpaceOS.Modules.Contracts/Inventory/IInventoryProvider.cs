using Ardalis.Result;
using SpaceOS.Modules.Contracts.Cutting.DTOs;
using SpaceOS.Modules.Contracts.Inventory.DTOs;
using SpaceOS.Modules.Contracts.Inventory.Requests;
using SpaceOS.Modules.Contracts.Shared;

namespace SpaceOS.Modules.Contracts.Inventory;

/// <summary>
/// Contract for inventory/stock management.
/// v1.2.0 — Reservation capability added (<see cref="ProviderCapability.InventoryReservation"/>).
/// TenantId resolved from JWT — not in request parameters (SEC-01).
/// </summary>
public interface IInventoryProvider : IModuleProvider
{
    /// <summary>
    /// Returns all stock items for the given material code.
    /// Requires <see cref="ProviderCapability.InventoryStock"/> capability.
    /// </summary>
    /// <param name="materialCode">Material code to query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of matching stock items.</returns>
    Task<Result<IReadOnlyList<StockItemDto>>> GetStockAsync(string materialCode, CancellationToken ct);

    /// <summary>
    /// Returns offcut stock items that meet minimum size requirements.
    /// Requires <see cref="ProviderCapability.InventoryOffcut"/> capability.
    /// </summary>
    /// <param name="materialCode">Material code to filter offcuts.</param>
    /// <param name="minWidth">Minimum required width in millimetres.</param>
    /// <param name="minHeight">Minimum required height in millimetres.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of usable offcut items meeting the size constraints.</returns>
    Task<Result<IReadOnlyList<StockItemDto>>> GetUsableOffcutsAsync(
        string materialCode, decimal minWidth, decimal minHeight, CancellationToken ct);

    /// <summary>
    /// Records one or more stock consumption movements.
    /// </summary>
    /// <param name="movements">List of stock movements to record.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Success or failure result.</returns>
    Task<Result> RecordConsumptionAsync(IReadOnlyList<StockMovementDto> movements, CancellationToken ct);

    /// <summary>
    /// Registers a cutting offcut as a reusable stock item.
    /// </summary>
    /// <param name="offcut">Offcut data produced by the cutting provider.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The new stock item identifier on success.</returns>
    Task<Result<Guid>> RecordOffcutAsync(CuttingOffcutResultDto offcut, CancellationToken ct);

    /// <summary>
    /// Records one or more inbound deliveries.
    /// </summary>
    /// <param name="items">List of items received from a delivery.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Success or failure result.</returns>
    Task<Result> RecordInboundAsync(IReadOnlyList<InboundReceiptDto> items, CancellationToken ct);

    /// <summary>
    /// Returns consumption trend data for a material over a time window.
    /// Requires <see cref="ProviderCapability.InventoryTrend"/> capability.
    /// </summary>
    /// <param name="materialCode">Material code to analyse.</param>
    /// <param name="from">Start of the analysis window (inclusive).</param>
    /// <param name="to">End of the analysis window (inclusive).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Consumption trend data on success.</returns>
    Task<Result<ConsumptionTrendDto>> GetConsumptionTrendAsync(
        string materialCode, DateTimeOffset from, DateTimeOffset to, CancellationToken ct);

    /// <summary>
    /// Creates a soft stock reservation with a TTL. Idempotent on (TenantId, CorrelationId)
    /// for <c>Active</c> reservations only — terminal state reservations do not block new ones.
    /// Requires <see cref="ProviderCapability.InventoryReservation"/> capability.
    /// </summary>
    /// <remarks>
    /// <para>TTL range: 1 hour to 168 hours (7 days). Default enforced by tenant configuration.</para>
    /// <para><b>SEC-07 / SEC-09 — ConsumerContextJson policy:</b> TRUSTED ONLY.
    /// NO user input, NO PII, NO secrets. Schema validated on ingest.</para>
    /// <para><b>SEC-13 — ConsumerModule allowlist:</b> Must be registered via IModuleRegistry.</para>
    /// <para>Rate limit: 100 requests per minute per tenant. Exceeding returns 429 with Retry-After.</para>
    /// </remarks>
    /// <param name="request">Reservation request containing items, TTL, and correlation ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created (or idempotently returned) reservation on success.</returns>
    Task<Result<ReservationDto>> ReserveAsync(ReserveStockRequest request, CancellationToken ct);

    /// <summary>
    /// Releases an active reservation identified by its correlation ID.
    /// No-op if the reservation is already Released, Expired, or Consumed.
    /// Rate limit: 100 requests per minute per tenant.
    /// </summary>
    /// <param name="correlationId">Consumer-provided correlation ID of the reservation to release.</param>
    /// <param name="reason">Optional reason for releasing the reservation.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Success result. NotFound if no active reservation matches the correlation ID.</returns>
    Task<Result> ReleaseReservationAsync(Guid correlationId, string? reason, CancellationToken ct);

    /// <summary>
    /// Queries reservations by filter criteria. At least one filter property must be set (DoS guard).
    /// Maximum Take is 500. Rate limit: 60 requests per minute per tenant.
    /// </summary>
    /// <param name="filter">Filter criteria. Must have at least one non-default property set.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Matching reservations with their items.</returns>
    Task<Result<IReadOnlyList<ReservationDto>>> GetReservationsAsync(
        ReservationFilter filter, CancellationToken ct);
}
