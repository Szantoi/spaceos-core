---
id: MSG-PROCUREMENT-009-DONE
from: procurement
to: root
type: done
priority: high
status: READ
ref: MSG-PROCUREMENT-009
created: 2026-04-18
---

# DONE — PROCUREMENT-009: BUG-007 GET /api/procurement/orders list endpoint

## Összefoglaló

Hozzáadva a hiányzó `GET /api/procurement/orders` lista endpoint.

| Fájl | Változás |
|---|---|
| `src/.../Application/Queries/GetOrders/GetOrdersQuery.cs` | **ÚJ** — `GetOrdersQuery` + `PurchaseOrderListResponse` DTO |
| `src/.../Application/Queries/GetOrders/GetOrdersQueryHandler.cs` | **ÚJ** — orders + supplier join in-memory |
| `src/.../Api/Endpoints/ProcurementEndpoints.cs` | `GET /orders` route + handler regisztrálva |
| `tests/.../Api/ProcurementEndpointsTests.cs` | 2 új teszt |

Commit: `308745b`

**Response shape (matches UI PurchaseOrderDto):**
```json
[{
  "id": "uuid",
  "supplierName": "Faanyag Kft.",
  "totalAmount": 200000.00,
  "expectedDelivery": null,
  "status": "Submitted",
  "createdAt": "2026-04-18T..."
}]
```

`supplierName`: aktív supplierek nevét `GetActiveSuppliersByTenantAsync`-ból lookup dict-tel; inaktív supplier esetén fallback a `supplierId.ToString()`.

## Tesztek

```
Passed!  - Failed: 0, Passed: 53, Skipped: 0, Total: 53
```

Új tesztek (2 db):
1. `GetOrders_WithAuth_Returns200WithList`
2. `GetOrders_EmptyTenant_Returns200EmptyList`

## Security review

| Pont | Státusz |
|---|---|
| JWT `tid` claim alapú tenant scope | ✅ |
| RLS a repo metódusokon át érvényes | ✅ |
| Üres lista is valid 200 (nem 404) | ✅ |

## Kockázatok / kérdések

Nincs DB migration, nincs VPS deploy szükséges — csak kód deploy kell.
