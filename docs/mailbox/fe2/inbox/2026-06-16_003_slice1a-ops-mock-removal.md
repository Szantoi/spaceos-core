---
id: MSG-FE2-003
from: root
to: fe2
type: task
priority: high
status: READ
ref: MSG-ARCH-001-DONE
created: 2026-06-16
---

# FE-OPS Slice 1A — Mock fallback eltávolítás + Warehouse bekötés

## Kontextus

Az FE Domain Ownership Matrix (v1) alapján a FE-B terminál megkapja a **FE-OPS domain**-t:
Inventory · Procurement · Analytics · Masterdata · MfgPrep · warehouse/*

## Feladat — 2 részből áll

### Rész A: Mock fallback eltávolítás (PARTIAL page-ek)

| # | Page | Mock konstans(ok) | Backend endpoint |
|---|---|---|---|
| 1 | `InventoryPage.tsx` | `MATERIALS` | `GET /inventory/api/inventory/stock?materialType=…` |
| 2 | `ProcurementPage.tsx` | `ACTIVE_PO`, `SUPPLIERS` | `GET /procurement/api/procurement/orders?pageSize=50` + `/suppliers` |

Pattern:
```typescript
// VOLT:
const data = apiData ?? MOCK_DATA
// LESZ:
const data = apiData
// + proper loading + error state
```

### Rész B: Warehouse aloldalak bekötése (jelenleg 100% MOCK)

| # | Page | Feladat |
|---|---|---|
| 3 | `warehouse/MovementsPage.tsx` | Inline mock tömb csere → `GET /inventory/api/inventory/movements` (path ellenőrizendő!) |
| 4 | `warehouse/LotsPage.tsx` | mock import csere → `GET /inventory/api/inventory/lots` (path ellenőrizendő!) |
| 5 | `warehouse/ZoneMapPage.tsx` | zóna-struktúra → `GET /inventory/api/inventory/zones` (path ellenőrizendő!) |

> **Inventory endpoint path-ok:** Ellenőrizd a `backend/spaceos-modules-inventory/` controllereit mielőtt bekötöd. Ha az endpoint nem létezik → `[?]`-vel jelöld az outboxban és ne adj mock fallback-et — inkább "coming soon" placeholder UI.

### Rész C: MasterdataPage részleges bekötés

| # | Page | Feladat |
|---|---|---|
| 6 | `MasterdataPage.tsx` | Anyagtörzs szekció → Inventory API-ból · Sablon szekció → Abstractions API-ból (`/abstractions/api/modules/templates`) |

## Elvárások

- Loading skeleton + error state minden érintett page-en
- Mock import törlése ahol nincs más felhasználó
- Build zöld + tesztek zöldek
- Ha endpoint nem létezik: placeholder UI, NEM mock fallback

## DoD

- Inventory + Procurement mock fallback eltávolítva
- Warehouse aloldalak bekötve (vagy documented [?] ahol backend endpoint hiányzik)
- MasterdataPage anyag + sablon szekció real API-on
- Build zöld, tesztek zöldek
- Outbox: `MSG-FE2-003-DONE` — endpoint path-ok listája (megtalált vs [?])

## Skill / agent

Használd a `/spaceos-terminal` skillt. Sub-agent engedélyezett.
