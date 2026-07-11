# SpaceOS.Modules.Contracts

Shared module provider contracts for the SpaceOS ecosystem.

**Current version:** `1.2.0`

## Contents

| Interface | Methods | Domain |
|---|---|---|
| `ICuttingProvider` | 6 | Cutting sheets, nesting, execution, waste |
| `IInventoryProvider` | 9 | Stock, offcuts, consumption, reservation |
| `IProcurementProvider` | 5 | Purchase orders, pricing, deliveries |

## SemVer strategy

| Change | Version bump |
|---|---|
| Breaking interface change (removed/renamed method) | MAJOR |
| New method added to an interface | MINOR |
| DTO field added / XML doc update | PATCH |

## Usage

### Checking capabilities before calling optional methods

```csharp
if (!provider.Capabilities.HasFlag(ProviderCapability.InventoryReservation))
    throw new NotSupportedException("Provider does not support reservations.");

var result = await provider.ReserveAsync(request, ct);
```

## Reservation API (v1.2.0)

### Overview

Soft stock reservation with TTL. Reservations are created by consumer modules (e.g., Cutting Planning) and later consumed or released.

```csharp
var request = new ReserveStockRequest(
    CorrelationId: Guid.NewGuid(),        // consumer-generated idempotency key
    ConsumerModule: "CuttingPlanning",    // must be in the allowlist (SEC-13)
    ConsumerContextJson: null,            // TRUSTED ONLY — no user input (SEC-07)
    Items: new[]
    {
        new ReserveItemRequest(stockItemId, "MDF18", quantity: 3.5m)
    },
    Ttl: TimeSpan.FromHours(24)
);

var result = await inventoryProvider.ReserveAsync(request, ct);
```

### Idempotency

`ReserveAsync` is idempotent on `(TenantId, CorrelationId)` **for Active reservations only**.
Terminal state reservations (Expired, Released, Consumed) with the same CorrelationId do not block new ones.

### ConsumerContextJson policy (SEC-07, SEC-09)

`ConsumerContextJson` is **TRUSTED ONLY**:
- NO user-supplied content
- NO PII (email addresses, bearer tokens)
- NO HTML / JavaScript

Use `ConsumerContextJsonSchema.IsValid()` before passing to the provider:

```csharp
if (!ConsumerContextJsonSchema.IsValid(rawJson, out var violation))
    return Result.Invalid(new ValidationError($"ConsumerContextJson rejected: {violation}"));
```

### Events

All 4 reservation events extend `ModuleEvent`. Consumers **MUST** verify `Event.TenantId` matches their JWT TenantId (SEC-03):

```csharp
// CORRECT:
if (reservedEvent.TenantId != jwtTenantId)
    throw new SecurityException("TenantId mismatch — event discarded.");

// WRONG — never trust event data without verification:
// ProcessReservation(reservedEvent); // ← SPOOFABLE
```

### Filter queries

At least one filter property must be set (DoS guard). Maximum `Take` is 500.

```csharp
var filter = new ReservationFilter(
    ConsumerModule: "CuttingPlanning",
    Status: ReservationStatus.Active,
    CorrelationId: null,
    CreatedAfter: null,
    CreatedBefore: null,
    Skip: 0,
    Take: 100
);

var reservations = await provider.GetReservationsAsync(filter, ct);
```

## Security notes

| Rule | Description |
|---|---|
| SEC-01 | TenantId never in request DTOs — resolved from JWT by implementation |
| SEC-03 | All domain events: verify `Event.TenantId` matches JWT TenantId |
| SEC-07 | ConsumerContextJson: schema + XSS regex validation required |
| SEC-09 | ConsumerContextJson: PII patterns rejected |
| SEC-13 | ConsumerModule must be in the registered allowlist |
