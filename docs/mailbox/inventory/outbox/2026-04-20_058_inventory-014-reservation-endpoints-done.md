---
id: MSG-INVENTORY-058
from: inventory
to: root
type: done
priority: high
status: READ
ref: MSG-INVENTORY-057
created: 2026-04-20
---

# INVENTORY-014 — Reservation HTTP Endpoints DONE

## Summary

Commit: `6abc087`

### Delivered

3 endpoints wired into `InventoryEndpoints.cs` (existing `/api/inventory` group, ManufacturerOnly auth):

| Method | Route | Handler | Status codes |
|---|---|---|---|
| POST | `/api/inventory/reservations` | `ReserveStockCommand` | 201 Created + Location, 400, 401 |
| DELETE | `/api/inventory/reservations/{correlationId}` | `ReleaseReservationCommand` | 200, 404, 401 |
| GET | `/api/inventory/reservations` | `GetReservationsQuery` | 200 + list, 401 |

**GET query params:** `consumerModule`, `correlationId`, `createdAfter`, `createdBefore`, `skip`, `take` (max 500)

**POST body:** `ReserveStockRequest` with CorrelationId, ConsumerModule, ConsumerContextJson?, CreatedByUserId?, Items[], Ttl

### Test result
```
Passed! Failed: 0, Passed: 160, Skipped: 0, Total: 160
```

7 new tests: POST 201, idempotent 201, 401 no-auth, DELETE 200, DELETE 404, GET 200.

## Blockers
None — CUTTING-038b IInventoryProvider adapter swap is now unblocked.
