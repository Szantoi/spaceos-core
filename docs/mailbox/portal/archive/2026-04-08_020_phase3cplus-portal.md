---
id: MSG-P020
from: architect
to: portal
type: task
priority: P1
date: 2026-04-08
sprint: "Sprint D · Phase 3C+"
---

# Phase 3C+ — Portal Tasks: moduleRouter + module routes + CabinetOrdersPage

## Állapot

`@spaceos/joinery-ui` scaffold KÉSZ (architect implementálta):
- `packages/@spaceos/joinery-ui/` — package.json, tsconfig.json, vitest.config.ts
- `src/index.ts` — whitelist re-export
- `src/base/`: CuttingListBase (internal), ProductionKanban, MaterialPanel, HandshakeOrderPanel, HandshakeHostPanel
- `src/door/`: DoorSpecPanel, DoorCuttingList
- `src/cabinet/`: CabinetSpecPanel, CabinetCuttingList
- `src/window/`: WindowCuttingList (return null stub)
- `vitest.workspace.ts` frissítve ✅
- `tailwind.config.ts` content path bővítve ✅
- `vite.config.ts` alias hozzáadva ✅
- `apps/joinerytech/package.json` dependency hozzáadva ✅

## Feladatok

### T1 — Build ellenőrzés (azonnal)

```bash
cd /opt/spaceos/design-portal
/root/.npm-global/bin/pnpm turbo build --filter=joinerytech
```

0 error elvárás. Ha TypeScript hibát dob a joinery-ui package-re → fix és újra build.

### T2 — moduleRouter.tsx (Nap 7)

Létrehozni: `apps/joinerytech/src/routes/moduleRouter.tsx`

```typescript
// ⚠ NOT A SECURITY BOUNDARY — RBAC az Orchestrator-ban
import { lazy, Suspense } from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';

const DoorRoutes    = lazy(() => import('../modules/door/routes'));
const CabinetRoutes = lazy(() => import('../modules/cabinet/routes'));
const WindowRoutes  = lazy(() => import('../modules/window/routes'));

const MODULE_ROUTES = { door: DoorRoutes, cabinet: CabinetRoutes, window: WindowRoutes };

export function ModuleRouter() {
  const enabledModules = useAuthStore(s => s.enabledModules) ?? [];  // BE-P3CP-08
  const isAuthLoading  = useAuthStore(s => s.isLoading);

  if (isAuthLoading) return <PageSkeleton />;  // BE-P3CP-10

  return (
    <ErrorBoundary fallback={<ModuleLoadError />}>         {/* BE-P3CP-09 */}
      <Suspense fallback={<PageSkeleton />}>               {/* BE-P3CP-06 */}
        <Routes>
          {enabledModules.map(mod => {
            const Comp = MODULE_ROUTES[mod as keyof typeof MODULE_ROUTES];
            return Comp ? <Route key={mod} path={`/${mod}/*`} element={<Comp />} /> : null;
          })}
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </Suspense>
    </ErrorBoundary>
  );
}
```

### T3 — Module route fájlok (Nap 7)

- `apps/joinerytech/src/modules/door/routes.tsx` — /door/cutting, /door/kanban
- `apps/joinerytech/src/modules/cabinet/routes.tsx` — /cabinet/cutting, /cabinet/kanban, /cabinet/orders
- `apps/joinerytech/src/modules/window/routes.tsx` — placeholder stub (return null)
- Oldalak: DoorDashboard, DoorCuttingListPage, DoorKanbanPage, CabinetDashboard, CabinetCuttingListPage, CabinetKanbanPage, CabinetOrdersPage (stub)

### T4 — useAuthStore bővítés (Nap 9 — Orchestrator után)

`enabledModules: string[]` és `allowedHosts: AllowedHost[]` hozzáadva a store-hoz.

**SEC-P3CP-05 KRITIKUS:** `partialize`-ból KIZÁRVA:
```typescript
partialize: (state) => ({
  accessToken: state.accessToken,
  expiresAt:   state.expiresAt,
  // enabledModules: KIZÁRVA
  // allowedHosts:   KIZÁRVA
  // brandSkin:      KIZÁRVA
}),
```

### T5 — CabinetOrdersPage live (Nap 11 — api-client hookup után)

`allowedHosts` kizárólag `useAuthStore(s => s.allowedHosts)`-ból jön.

## DoD gate-ek

- [ ] `pnpm turbo build` — 0 error, 0 warning
- [ ] `pnpm turbo test` — meglévő 321 + új tesztek zöld
- [ ] `enabledModules: ['door']` → `/cabinet/*` nem létezik
- [ ] `localStorage.setItem('enabledModules', ...)` → nincs hatás
- [ ] `<Suspense>` minden lazy route körül (BE-P3CP-06)
- [ ] `<ErrorBoundary>` a Suspense körül (BE-P3CP-09)

## Válaszban kérem

Mailbox outbox: `docs/mailbox/portal/outbox/2026-04-08_020_phase3cplus-portal-done.md`
