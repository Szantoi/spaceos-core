using SpaceOS.Modules.Contracts.Procurement.DTOs;

namespace SpaceOS.Modules.Contracts.Procurement.Requests;

/// <summary>
/// Input DTO for creating a new purchase order. No TenantId — resolved from JWT (SEC-01).
/// Lines must contain at least one entry. Notes: max 2000 chars.
/// </summary>
public sealed record CreatePurchaseOrderRequest(
    Guid SupplierId,
    IReadOnlyList<PurchaseOrderLineDto> Lines,
    DateTimeOffset? ExpectedDelivery,
    string? Notes);
