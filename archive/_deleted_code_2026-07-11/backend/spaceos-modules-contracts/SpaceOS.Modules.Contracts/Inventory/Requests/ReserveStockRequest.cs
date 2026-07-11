using System.Text.Json;

namespace SpaceOS.Modules.Contracts.Inventory.Requests;

/// <summary>
/// Request to create a soft stock reservation with a TTL.
/// TenantId is NOT included — it is resolved from the JWT by the implementation (SEC-01).
/// ConsumerContextJson MUST NOT contain PII or user-supplied content (SEC-07, SEC-09).
/// CorrelationId: consumer-generated idempotency key; unique per active reservation per tenant.
/// ConsumerModule: registered consumer module name; must be in the allowlist (SEC-13); max 50 characters.
/// ConsumerContextJson: optional structured metadata from the consumer — TRUSTED ONLY, no PII, no user input.
/// Items: line items to reserve; must contain at least one item (I-01); max 200 items.
/// Ttl: reservation lifetime; must be between 1 hour and 168 hours (I-02, I-03).
/// </summary>
public sealed record ReserveStockRequest(
    Guid CorrelationId,
    string ConsumerModule,
    JsonDocument? ConsumerContextJson,
    IReadOnlyList<ReserveItemRequest> Items,
    TimeSpan Ttl
);

/// <summary>
/// A single line item within a <see cref="ReserveStockRequest"/>.
/// StockItemId: identifier of the stock item to reserve.
/// MaterialCode: material code of the stock item; max 20 characters.
/// QuantityReserved: quantity to reserve; must be greater than zero (I-06).
/// </summary>
public sealed record ReserveItemRequest(
    Guid StockItemId,
    string MaterialCode,
    decimal QuantityReserved
);
