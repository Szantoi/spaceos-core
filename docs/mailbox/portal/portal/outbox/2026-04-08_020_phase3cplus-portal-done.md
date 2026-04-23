# MSG-P020 — Phase 3C+ Portal: moduleRouter + module routes + useAuthStore (Day 7 + T4)

**Date:** 2026-04-08
**Status:** DONE
**Agent:** Portal

---

## T1 — Build ellenőrzés

- `tsc --noEmit` → **0 error** (tsconfig.json paths frissítve a @spaceos/* workspace csomagokhoz)
- Pre-existing test failures (204) nem az én változtatásom — git stash ellenőrzéssel igazolva

## T2 — moduleRouter.tsx

Létrehozva: `apps/joinerytech/src/routes/moduleRouter.tsx`

- Lazy load: `DoorRoutes`, `CabinetRoutes`, `WindowRoutes`
- `useAuthStore(s => s.enabledModules)` — BE-P3CP-08
- `useAuthStore(s => s.isLoading)` — BE-P3CP-10 (skeleton during auth)
- `<ErrorBoundary>` a `<Suspense>` körül — BE-P3CP-09
- `<Suspense>` minden lazy route körül — BE-P3CP-06
- `<Navigate to="/" replace />` fallback a nem engedélyezett modulokhoz

## T3 — Module route fájlok

### door/routes.tsx
- `/` → DoorDashboard (index)
- `/cutting` → DoorCuttingListPage
- `/kanban` → DoorKanbanPage

### cabinet/routes.tsx
- `/` → CabinetDashboard (index)
- `/cutting` → CabinetCuttingListPage
- `/kanban` → CabinetKanbanPage
- `/orders` → CabinetOrdersPage (stub — Day 11 hookup)

### window/routes.tsx
- `return null` — SEC-P3CP-04 stub, no runtime error

## T4 — useAuthStore bővítés

Frissítve: `apps/joinerytech/src/store/auth.store.ts`

Új fields az `AuthState`-ben:
- `isLoading: boolean` (alapértelmezett: false)
- `enabledModules: string[]` (alapértelmezett: [])
- `allowedHosts: AllowedHost[]` (alapértelmezett: [])

**SEC-P3CP-05 TELJESÍTVE:**
```typescript
partialize: (state) => ({ token: state.token, refreshToken: state.refreshToken })
// enabledModules: KIZÁRVA
// allowedHosts:   KIZÁRVA
// brandSkin:      KIZÁRVA
```

`login()` most parszolja a JWT-ből:
- `enabled_modules` → `enabledModules`
- `allowed_hosts` → `allowedHosts`

## ErrorBoundary

Létrehozva: `apps/joinerytech/src/components/common/ErrorBoundary.tsx`
- Class component, `getDerivedStateFromError` + `componentDidCatch`

## tsconfig.json frissítés

`paths` bővítve az összes `@spaceos/*` workspace csomaggal:
```json
"@spaceos/joinery-ui": ["../../packages/@spaceos/joinery-ui/src/index.ts"],
"@spaceos/api-client": ["../../packages/@spaceos/api-client/src/index.ts"],
...
```

## DoD gate-ek

- [x] `tsc --noEmit` — 0 error
- [x] `enabledModules: ['door']` → `/cabinet/*` nem létezik (Navigate fallback)
- [x] `localStorage.setItem('enabledModules', ...)` → nincs hatás (partialize kizárva)
- [x] `<Suspense>` minden lazy route körül (BE-P3CP-06)
- [x] `<ErrorBoundary>` a Suspense körül (BE-P3CP-09)

## Pending (T5 — Day 11)

CabinetOrdersPage live B2BHandshake integráció — vár T1 Kernel + api-client hookup után.
