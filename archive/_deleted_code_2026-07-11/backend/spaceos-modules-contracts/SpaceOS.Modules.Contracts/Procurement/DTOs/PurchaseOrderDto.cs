using SpaceOS.Modules.Contracts.Procurement.Enums;

namespace SpaceOS.Modules.Contracts.Procurement.DTOs;

/// <summary>
/// Output DTO representing a complete purchase order.
/// TenantId present as read-only (SEC-01 output side).
/// Notes: max 2000 chars.
/// </summary>
public sealed record PurchaseOrderDto(
    Guid Id,
    Guid TenantId,
    Guid SupplierId,
    PurchaseOrderStatus Status,
    IReadOnlyList<PurchaseOrderLineDto> Lines,
    DateTimeOffset? ExpectedDelivery,
    DateTimeOffset CreatedAt,
    string? Notes);
