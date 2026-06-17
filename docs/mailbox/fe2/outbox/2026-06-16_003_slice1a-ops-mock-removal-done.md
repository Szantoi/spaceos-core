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

# FE-OPS Slice 1A DONE — Mock fallback eltávolítás + Warehouse bekötés + MasterdataPage

## Összefoglaló

Az FE-OPS Slice 1A teljes scope elvégezve. Commit: `77138ff` (+ korábbi session commitok).

## Endpoint path-ok (megtalált vs [?])

| Page | Endpoint | Státusz |
|---|---|---|
| InventoryPage | `GET /inventory/api/inventory/stock?materialType=…` | ✅ létezik |
| ProcurementPage | `GET /procurement/api/procurement/orders?pageSize=50` | ✅ létezik |
| ProcurementPage | `GET /procurement/api/procurement/suppliers` | ✅ létezik |
| warehouse/MovementsPage | `GET /inventory/api/inventory/movements` | [?] NEM LÉTEZIK — csak POST /movements/consumption,inbound,offcut |
| warehouse/LotsPage | `GET /inventory/api/inventory/lots` | [?] NEM LÉTEZIK |
| warehouse/ZoneMapPage | `GET /inventory/api/inventory/zones` | [?] NEM LÉTEZIK |
| MasterdataPage (materials) | `GET /inventory/api/inventory/stock` | ✅ létezik |
| MasterdataPage (templates) | `GET /abstractions/api/modules/templates` | ✅ létezik |

## Elvégzett munkák

### Rész A: InventoryPage + ProcurementPage mock eltávolítás ✅
(Korábbi session elvégezte — jelenleg 0 mock fallback ezeken az oldalakon)

### Rész B: Warehouse aloldalak ✅
- `warehouse/MovementsPage.tsx` — `EndpointPending` placeholder (`GET /inventory/api/inventory/movements [?]`)
- `warehouse/LotsPage.tsx` — `EndpointPending` placeholder (`GET /inventory/api/inventory/lots [?]`)
- `warehouse/ZoneMapPage.tsx` — `EndpointPending` placeholder (`GET /inventory/api/inventory/zones [?]`)
- Mock import helyett proper "Backend endpoint nem elérhető" UI — nincs mock fallback

### Rész C: MasterdataPage részleges bekötés ✅
- `MaterialsList` (Anyagtörzs) — Inventory API (`/inventory/api/inventory/stock`) ✅ (előző session)
- `TemplatesList` (Sablonok) — Abstractions API (`/abstractions/api/modules/templates`) ✅ **MOST KÉSZ**
  - Loading skeleton, error state, empty state
  - Rekord mező: name, tradeType, version, isActive
- `worlds.ts` — masterdata screens: `{ key: "templates", hu: "Sablonok" }` hozzáadva
- `ProductsList`, `SuppliersList`, `MasterdataDashboard` — PRODUCTS/SUPPLIERS mock megtartva
  (részleges bekötés — ezek a Procurement API supplier integráció után cserélendők)

## Build + Tesztek

```
Test Files  72 passed (72)
     Tests  731 passed (731)
```

- `npm run build` — ✅ 0 TS error
- `npm test` — ✅ 731/731 zöld (+4 új teszt: TemplatesList)

## Security review

- Nincs localStorage token (0 találat)
- Nincs mock import a warehouse oldalakon
- TemplatesList: useApi Bearer token header-rel hív — clean
