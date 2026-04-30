---
id: MSG-FE-026
from: root
to: fe
type: task
priority: high
status: READ
created: 2026-04-30
---

# FE-026 — Minden oldal átvétele mock adatokkal

> **Gábor:** "Minden oldalt át kell venni a mock adatokkal, hogy aztán rá lehessen dolgozni a valódi API-kat."
> **Design reference:** `design-reference/project/` — OLVASD EL MINDEN page-*.jsx és data*.js fájlt!
> **Cél:** Minden oldal a design reference mock adataival renderelődjön — pixel-perfect, magyar, responsive
> **Skill:** `/spaceos-terminal` szerint dolgozz
> **Használhatsz sub-agent-eket** ha szükséges

---

## Feladat

Minden design reference oldalt át kell nézni és a mock adatokat be kell építeni. A cél: a portál MINDEN oldala vizuálisan teljes legyen mock adatokkal, hogy később az API integrációt egyenként lehessen cserélni.

### Design reference fájlok → Portal oldalak mapping

Olvasd el ÚJRA az összes fájlt és implementáld ami hiányzik:

| Design fájl | Mock adat fájl | Portal oldal | Státusz |
|---|---|---|---|
| `page-home.jsx` | `data-worlds.js` | WorldHomePage | Ellenőrizd, frissítsd |
| `page-dashboard.jsx` | `data.js` | (töröltük) → WorldHomePage-be olvasztandó ha kell | Ellenőrizd |
| `page-sales.jsx` | `data.js` | SalesDashboardPage | Ellenőrizd, bővítsd |
| `page-orders.jsx` | `data.js` | OrdersPage + OrderDetailPage | Implementáld mock-kal |
| `page-design.jsx` | `data.js` | ProductConfiguratorPage | Implementáld mock-kal |
| `page-flow.jsx` | `data.js` | B2bHandshakesPage | Implementáld mock-kal |
| `page-production.jsx` | `data.js` | ProductionOverviewPage + CuttingPlanList/Detail | Ellenőrizd, bővítsd |
| `page-workflow.jsx` | `data.js` | ManufacturingFsmBoardPage | Implementáld mock-kal |
| `page-shopfloor.jsx` | `data-extra.js` | ShopFloor oldalak | Implementáld mock-kal |
| `page-extras.jsx` | `data-extra.js` | Settings: TenantInfo + AuditLog + UserList | Implementáld mock-kal |
| `page-extras-2.jsx` | `data-extra-2.js` | Settings: további oldalak | Implementáld mock-kal |
| `page-rest.jsx` | `data-extra-2.js` | Egyéb oldalak | Implementáld mock-kal |
| `page-world-pages.jsx` | `data-worlds.js` | World page templates | Ellenőrizd |
| `ui.jsx` | — | Közös UI komponensek | Implementáld |
| `tweaks-panel.jsx` | — | Design tweaks panel (ha releváns) | Opcionális |

### Mock adat stratégia

1. Hozz létre `src/mocks/` mappát
2. Minden data*.js fájlból konvertáld a mock adatokat TypeScript-be: `src/mocks/salesData.ts`, `src/mocks/productionData.ts`, stb.
3. Minden oldal importálja a mock adatokat — később az API hívásra cserélhető lesz (a hook-ban a mock → fetch csere)
4. A mock adatok MAGYAR nyelvűek legyenek (a design reference-ben is azok)

### UI komponensek (`ui.jsx`)

A design reference `ui.jsx` közös komponenseket tartalmaz (gombok, badge-ek, kártyák, ikonok). Ezeket `src/components/ui/` alá konvertáld:
- `Badge` — státusz badge színekkel
- `KpiCard` — KPI kártya ikon + érték + label
- `DataTable` — táblázat header + sorok
- `ProgressBar` — haladás sáv
- `StatusDot` — állapot jelző pont
- Bármi más ami a design-ben közös

### Minden oldal ellenőrzőlista

Minden oldalon:
- [ ] A design reference layout-ja pixel-perfect
- [ ] Mock adatok renderelődnek (nem üres lista, nem "Loading...")
- [ ] Magyar feliratok
- [ ] Responsive (375px hamburger menü, md+ sidebar)
- [ ] Klikkelhetők a gombok/linkek (ha van route, navigáljon; ha nincs, placeholder alert)

---

## Definition of Done

- [ ] MINDEN page-*.jsx design fájl implementálva vagy frissítve
- [ ] `src/mocks/` mock adat fájlok TypeScript-ben
- [ ] `src/components/ui/` közös UI komponensek
- [ ] Nincs üres oldal — mindenhol mock adat renderelődik
- [ ] Magyar UI mindenhol
- [ ] Responsive (375px+)
- [ ] `pnpm build` 0 error
- [ ] `pnpm test` ≥ 240 pass
- [ ] `pnpm lint` 0 error
- [ ] Outbox DONE — oldalonként részletezze mit implementált
