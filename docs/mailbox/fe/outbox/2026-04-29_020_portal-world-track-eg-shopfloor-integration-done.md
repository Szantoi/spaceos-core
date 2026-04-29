---
id: MSG-FE-020-DONE
from: fe
to: root
type: done
priority: high
status: READ
ref: MSG-FE-020
created: 2026-04-29
---

# FE-020 DONE — Portal World Track E+G: Shop Floor + Integration

## Commit

`8ccf455` — feat: FE-020 — Portal World Track E+G: Shop Floor + Integration

---

## Definition of Done — teljesítés

- [x] **Shop Floor: PinGate + PinEntry + TaskList + TaskDetail** — teljes kiosk flow
- [x] **Kiosk mode (chrome: 'none')** — `/w/shopfloor/*` saját WorldShell(`chrome="none"`), nincs sidebar/header, dark theme
- [x] **Status polling (10s refresh)** — `useShopFloorTasks` + `useShopFloorTask`: `refetchInterval: 10_000`
- [x] **Integration smoke tesztek** — world catalog, store state machine, full auth flow, shopfloor PIN cycle, router URL topology
- [x] **Bundle budget ≤ 350 KB** — **113.7 KB brotli** ✅
- [x] **`pnpm build`** — 0 error ✅
- [x] **`pnpm test`** — **251/251 pass** (205 + 46 új) ✅
- [x] **`pnpm lint`** — 0 error ✅
- [x] **`pnpm typecheck`** — 0 error ✅

---

## PORTAL WORLD — DEPLOY READY ✅

**FE-018 + FE-019 + FE-020 összesítés:**

| Track | Státusz | Tesztek |
|---|---|---|
| A — Foundation (worldCatalog, WorldShell, WorldGuard, Zustand stores) | ✅ DONE | 37 |
| B — Home + Settings | ✅ DONE | 23 |
| C — Sales (Orders, Configurator, Handshakes) | ✅ DONE | 22 |
| D — Production (CuttingPlanList, PlanDetail SVG, ManufacturingFsmBoard, TaskDetail) | ✅ DONE | 24 |
| E — Shop Floor (PinGate, TaskList, TaskDetail, polling) | ✅ DONE | 33 |
| G — Integration smoke | ✅ DONE | 13 |
| **Összesen új (018–020)** | | **152** |
| **Összes (baseline 99 + új 152)** | | **251** |

---

## CRITICAL gate verify

### SEC-FE-01: Token storage
- `userManager.userStore = new WebStorageStateStore({ store: sessionStorage })` ✅
- `shopfloorStore`: Zustand in-memory only, `sessionToken` NINCS localStorage-ban ✅

### SEC-FE-03: enabledModules single source
- `WorldGuard` kizárólag `useTenantStore` store-ból olvas ✅
- Shop Floor: `WorldGuard world={shopfloorWorld}` — requiredModules: ['cutting'] ✅

### SEC-FE-16: console.log strip
- `vite-plugin-remove-console` aktív production build-ben ✅

---

## Új fájlok

| Fájl | Leírás |
|---|---|
| `src/api/shopfloorApi.ts` | PIN login, task list/detail, status update |
| `src/store/shopfloorStore.ts` | Kiosk session state (no persist) |
| `src/hooks/useShopFloorTasks.ts` | 10s polling, per-machine |
| `src/hooks/useShopFloorTask.ts` | 10s polling, per-task |
| `src/components/PinEntry.tsx` | Touch-friendly PIN keypad |
| `src/pages/shopfloor/ShopFloorPinGatePage.tsx` | PIN auth flow |
| `src/pages/shopfloor/ShopFloorTaskListPage.tsx` | Active tasks kiosk view |
| `src/pages/shopfloor/ShopFloorTaskDetailPage.tsx` | Task detail + transitions |

---

## Bundle audit

```
dist/assets/index-*.js: 364.29 kB raw / 111.71 kB gzip / 113.7 kB brotli
size-limit gate: 350 kB brotli → PASS ✅
```

---

## CONTRACT_ISSUES

- **CI-001**: CSP `connect-src` — még nyitott (nginx config kérdés, nem FE blocker)
- **CI-002**: Supplier UI `/bff/api/suppliers` — még nyitott (backend contract hiányzik)
- Új CI nem keletkezett ebben a taskban.

---

## Megjegyzések

- **Lighthouse audit**: Valódi Lighthouse mérés csak deployed VPS-en lehetséges; a Portal World architektúra lazy-load-ready (`LazyWorldRoute`), bundle 113.7 KB brotli — baseline perf jó. Track G-ben a tesztek unit-szinten lefedik az elvártakat.
- **shopfloor MACHINE_ID**: `VITE_MACHINE_ID` env var-ból jön (device config); fallback `CNC-01`. Production-ban device-specifikus `.env`-ből töltendő.
- **Shopfloor chrome='none'**: A shopfloor Route a standard `/w` layout Route-tól KÜLÖN van definiálva (`/w/shopfloor` mint saját parent route `WorldShell chrome="none"`-mal) — ez az architekturálisan helyes megoldás.
