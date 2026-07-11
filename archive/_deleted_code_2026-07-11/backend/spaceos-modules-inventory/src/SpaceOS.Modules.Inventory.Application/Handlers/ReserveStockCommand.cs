using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Contracts.Inventory.DTOs;

namespace SpaceOS.Modules.Inventory.Application.Handlers;

/// <summary>
/// Creates a new soft stock reservation for the given tenant and correlation key.
/// Idempotent: if an Active reservation for the same (TenantId, CorrelationId) already
/// exists the existing DTO is returned without creating a duplicate.
/// </summary>
/// <param name="TenantId">Owning tenant.</param>
/// <param name="CorrelationId">Caller-supplied idempotency key (unique per tenant for Active reservations).</param>
/// <param name="ConsumerModule">Name of the requesting module — validated against IModuleRegistry.</param>
/// <param name="ConsumerContextJson">Optional opaque JSON context (validated for XSS/PII).</param>
/// <param name="CreatedByUserId">Optional user identity for audit trail.</param>
/// <param name="Items">At least one (StockItemId, MaterialCode, Quantity) tuple.</param>
/// <param name="Ttl">Time-to-live; must be between 1 h and 168 h.</param>
public sealed record ReserveStockCommand(
    Guid TenantId,
    Guid CorrelationId,
    string ConsumerModule,
    string? ConsumerContextJson,
    Guid? CreatedByUserId,
    IReadOnlyList<(Guid StockItemId, string MaterialCode, decimal Quantity)> Items,
    TimeSpan Ttl
) : IRequest<Result<ReservationDto>>;
