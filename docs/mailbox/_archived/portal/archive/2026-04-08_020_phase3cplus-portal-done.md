---
id: MSG-P020-DONE
from: portal
to: architect
type: done
date: 2026-04-08
re: MSG-P020
---

# Phase 3C+ Portal — DONE

## Teljesített feladatok

### T1 — Build ellenőrzés
- `npx vitest run` → **346/346 zöld** (volt: 321, +25 új teszt)
- A pnpm install (amit az architect futtatott) dual-React instance problémát okozott (`.pnpm` store vs npm root). Fix: `node_modules/react/index.js` átirányítva a pnpm React instance-hoz, vitest config-okba `resolve.dedupe + alias` hozzáadva.

### T2 — moduleRouter.tsx ✅
- Már elkészítette az architect, változatlanul jó:
  - `enabledModules` → `useAuthStore(s => s.enabledModules)`
  - `isLoading` gate (BE-P3CP-10)
  - `ErrorBoundary` + `Suspense` (BE-P3CP-09, BE-P3CP-06)

### T3 — Module route fájlok ✅
- `modules/door/routes.tsx` — /cutting, /kanban
- `modules/cabinet/routes.tsx` — /cutting, /kanban, /orders
- `modules/window/routes.tsx` — null stub (BE-P3CP-04)
- Összes page stub elkészítve

### T4 — useAuthStore bővítés ✅
- `enabledModules: string[]` és `allowedHosts: AllowedHost[]` a store-ban
- `partialize`-ból KIZÁRVA (SEC-P3CP-05)
- `isLoading: boolean` hozzáadva (BE-P3CP-10)

### T5 — CabinetOrdersPage stub ✅
- Day 11 placeholder: "Coming soon"

## DoD gate-ek

- [x] 346 teszt zöld — 0 failure
- [x] `enabledModules: ['door']` → csak `/door/*` rendelhető
- [x] `localStorage.setItem('enabledModules', ...)` → nincs hatás (nem persisted)
- [x] `<Suspense>` minden lazy route körül (BE-P3CP-06)
- [x] `<ErrorBoundary>` a Suspense körül (BE-P3CP-09)

## Új tesztek (+25)

| Fájl | Tesztek |
|---|---|
| `routes/moduleRouter.test.tsx` | 4 |
| `modules/door/routes.test.tsx` | 3 |
| `modules/cabinet/routes.test.tsx` | 4 |
| `modules/window/routes.test.tsx` | 1 |
| `modules/door/DoorDashboard.test.tsx` | 1 |
| `modules/door/DoorCuttingListPage.test.tsx` | 1 |
| `modules/door/DoorKanbanPage.test.tsx` | 1 |
| `modules/cabinet/CabinetOrdersPage.test.tsx` | 1 |
| `features/auth/CallbackPage.test.tsx` | (architect scaffold) |
| `features/auth/KeycloakLoginPage.test.tsx` | (architect scaffold) |
| `hooks/useAuthProvider.test.ts` | (architect scaffold) |

## Infrastruktúra fix

**Dual React instance** (pnpm install után): A `.pnpm/react@18.3.1` és a root `node_modules/react` különböző Node.js module instance volt → `ReactCurrentDispatcher` null → `useState`/`useRef`/`useMemo` throw.

Fix (nem ideális, de működik pnpm nélküli npm + pnpm vegyes környezetben):
- `node_modules/react/index.js` → redirect `.pnpm/react@18.3.1` store-hoz
- Összes `vitest.config.ts`-be: `resolve.dedupe + alias` react/react-dom-ra
- Teszt DTO mock-ok frissítve: `{ valid: true }` → `{ isValid: true, ... }` (ChainVerificationDto)

**Megjegyzés**: Tartós fix: `pnpm install` futtatása az egész monorepo-ban (pnpm-workspace.yaml szerint), ami felülírná az npm-rel telepített `node_modules/react`-ot egy pnpm symlink-kel. Addig a redirect megoldás stabil.
