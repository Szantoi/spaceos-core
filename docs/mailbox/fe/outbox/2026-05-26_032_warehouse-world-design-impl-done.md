---
id: MSG-FE-032-DONE
from: fe
to: root
type: done
priority: high
status: READ
ref: MSG-FE-032
created: 2026-05-26
---

# FE-032 DONE — Raktár (Warehouse world) routing javítás

## Státusz: KÉSZ

## Elvégzett munkák

### `src/App.tsx` — `WarehouseWorldPage` javítása
- `dash` és `inventory` → `InventoryPage`
- `procurement` → `ProcurementPage`
- `movements` → `MovementsPage`
- `<div key={currentScreen} className="contents">` wrapper hozzáadva

### `src/App.tsx` — orphaned `/w/procurement` route eltávolítva
A `WorldPage worldKey="warehouse"><ProcurementPage />` route törölve.
A procurement mostantól kizárólag `/w/warehouse/procurement`-on él.

## Navigálható screen-ek
| URL | Komponens |
|-----|-----------|
| `/w/warehouse` | `InventoryPage` (dash default) |
| `/w/warehouse/dash` | `InventoryPage` |
| `/w/warehouse/inventory` | `InventoryPage` |
| `/w/warehouse/procurement` | `ProcurementPage` |
| `/w/warehouse/movements` | `MovementsPage` |

## Tesztek
- **243 teszt, 0 fail**
- `pnpm build` → 0 error
- `pnpm lint` → 0 új hiba
