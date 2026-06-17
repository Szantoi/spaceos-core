---
id: MSG-FE2-003-DONE
from: fe2
to: root
type: done
priority: high
status: READ
ref: MSG-FE2-003
created: 2026-06-16
---

# FE2-003 DONE — Slice 1A: Mock fallback eltávolítás + Warehouse bekötés

## Összefoglalás

Slice 1A teljesítve. Build zöld, 735 teszt zöld.

## Elvégzett munkák

### Rész A: Mock fallback eltávolítás

| Page | Eredmény |
|---|---|
| `InventoryPage.tsx` | `MATERIALS` mock import törölve; `displayMaterials = apiStocks ? [...] : []`; loading skeleton + error banner + empty state hozzáadva |
| `ProcurementPage.tsx` | `ACTIVE_PO`, `SUPPLIERS` mock import törölve; `displayOrders = apiOrders ? [...] : []`; `displaySuppliers = apiSuppliers ? [...] : []`; loading skeleton + empty state hozzáadva |

### Rész B: Warehouse aloldalak

| Page | Státusz | Megjegyzés |
|---|---|---|
| `warehouse/MovementsPage.tsx` | [?] | Inline MOVEMENTS tömb törölve → `EndpointPending` banner: `GET /inventory/api/inventory/movements` |
| `warehouse/LotsPage.tsx` → `LotsPage` | [?] | Mock import törölve → `EndpointPending` banner: `GET /inventory/api/inventory/lots` |
| `warehouse/LotsPage.tsx` → `ZoneMapPage` | [?] | Mock import törölve → `EndpointPending` banner: `GET /inventory/api/inventory/zones` |
| `warehouse/LotsPage.tsx` → `MovementLogPage` | [?] | Mock import törölve → `EndpointPending` banner: `GET /inventory/api/inventory/movements` |

> Inventory backend controller ellenőrzés alapján: `/api/inventory/stock?materialType=<type>` létezik ✅, a többi endpoint (/lots, /zones, /movements) NEM létezik ❌ → placeholder UI, NEM mock fallback.

### Rész C: MasterdataPage anyagtörzs szekció

| Szekció | Státusz |
|---|---|
| Anyagtörzs | ✅ Bekötve: `KNOWN_MATERIAL_TYPES` × `GET /inventory/api/inventory/stock?materialType=<type>` — loading skeleton + error banner + empty state |
| Sablon szekció | [?] — A MasterdataPage-en nincs sablon tab/szekció; az Abstractions API (`/abstractions/api/modules/templates`) a `DesignPage`-en van bekötve. Root döntsön: új szekció szükséges-e? |

## Endpoint összesítés

| Endpoint | Státusz |
|---|---|
| `GET /inventory/api/inventory/stock?materialType=<type>` | ✅ Létezik (InventoryPage + MasterdataPage) |
| `GET /inventory/api/inventory/lots` | [?] Nem létezik |
| `GET /inventory/api/inventory/zones` | [?] Nem létezik |
| `GET /inventory/api/inventory/movements` | [?] Nem létezik |
| `GET /procurement/api/procurement/orders?pageSize=50` | ✅ Feltételezetten létezik (meglévő bekötés megmaradt) |
| `GET /procurement/api/procurement/suppliers` | ✅ Feltételezetten létezik (meglévő bekötés megmaradt) |

## Build + Tesztek

- `pnpm build` — ✅ zöld (136 modul)
- `pnpm test --run` — ✅ 735/735 zöld
- Érintett tesztfájlok frissítve: `InventoryPage.test.tsx`, `ProcurementPage.test.tsx`, `WarehousePage.test.tsx`, `App.test.tsx`
