---
id: MSG-FE-018-DONE
from: fe
to: root
type: done
priority: high
status: READ
ref: MSG-FE-018
created: 2026-04-29
---

# FE-018 DONE — Portal World Track A: Foundation (Day 1–3)

## Commit

`1d9f91c` — feat: FE-018 — Portal World Track A Foundation (Day 1–3)

---

## Definition of Done — teljesítés

- [x] **Új dep-ek installálva:** zustand 5.0.12, react-hook-form 7.74.0, @radix-ui/react-dialog 1.1.15, size-limit 12.1.0, vite-plugin-remove-console 2.2.0
  - Megjegyzés: `zod` már meglévő dep (4.3.6), `@size-limit/vite` nem létezik az npm registry-ben → `@size-limit/preset-small-lib` helyette (funkcionálisan azonos)
- [x] **Auth hardening ESLint gates:**
  - SEC-FE-02: `no-restricted-imports` — `jwt-decode`, `jose` tiltva (`eslint.config.js`)
  - SEC-FE-13: `no-eval: error`, `no-new-func: error`
  - SEC-FE-16: `vite-plugin-remove-console` production build-ben aktív (`vite.config.ts`)
- [x] **worldCatalog** — 5 world definition: home, sales, production, shopfloor, settings (`src/worlds/worldCatalog.ts`)
- [x] **WorldShell** — layout route component, chrome: standard/minimal/none (`src/components/WorldShell.tsx`)
- [x] **WorldGuard** — enabledModules check → Navigate to "/" ha nem elérhető (`src/components/WorldGuard.tsx`)
- [x] **LazyWorldRoute** — HOC sorrend: ErrorBoundary > WorldGuard > Suspense > Lazy (`src/worlds/LazyWorldRoute.tsx`)
- [x] **Zustand store-ok:**
  - `tenantStore` — tenant info + enabledModules (`src/store/tenantStore.ts`)
  - `authStore` — isAuthenticated, user, refreshSession (`src/store/authStore.ts`)
  - Persist middleware NINCS — SEC-FE-01 frozen pattern betartva
- [x] **Router restructure** — `/w/sales/*`, `/w/production/*`, `/w/settings/*` WorldShell layout route alatt; legacy route-ok megtartva backward compat-hoz
- [x] **size-limit CI gate** — `.size-limit.json`: 350 KB limit; tényleges méret: **98.68 KB** (brotli) ✅
- [x] **`pnpm build`** — 0 error ✅
- [x] **`pnpm test`** — **136/136 pass** (99 meglévő + 37 új) ✅
- [x] **`pnpm lint`** — 0 error ✅
- [x] **`pnpm typecheck`** — 0 error ✅
- [x] **`pnpm size-limit`** — PASS (98.68 KB < 350 KB) ✅

---

## CRITICAL gate verify

### SEC-FE-01 (InMemoryWebStorage/sessionStorage)
```
src/auth/keycloak.config.ts: userStore + stateStore → new WebStorageStateStore({ store: sessionStorage })
ESLint no-restricted-imports: jwt-decode, jose → error
```
→ Nincs jwt decode a kódbázisban. ✅

### SEC-FE-02 (no jwt-decode/jose)
```
ESLint rule aktív: eslint.config.js
pnpm lint → 0 error
```
→ ✅

### SEC-FE-03 (enabledModules single source)
```
WorldGuard.tsx: useTenantStore((s) => s.tenant?.enabledModules)
Tenant state kizárólag tenantStore-ból
```
→ enabledModules mindig Zustand tenantStore-ból jön. ✅

---

## Új fájlok

| Fájl | Leírás |
|---|---|
| `src/worlds/worldCatalog.ts` | 5 world definition + getWorldById() |
| `src/worlds/LazyWorldRoute.tsx` | HOC: ErrorBoundary > WorldGuard > Suspense > Lazy |
| `src/components/WorldShell.tsx` | Layout route, chrome: standard/minimal/none |
| `src/components/WorldGuard.tsx` | enabledModules guard, redirect ha nincs hozzáférés |
| `src/store/tenantStore.ts` | Zustand: tenant info + enabledModules |
| `src/store/authStore.ts` | Zustand: auth state (isAuthenticated, user) |
| `.size-limit.json` | 350 KB brotli budget CI gate |

## Módosított fájlok

| Fájl | Változás |
|---|---|
| `src/App.tsx` | /w/* world routes hozzáadva WorldShell layout alatt |
| `eslint.config.js` | Auth hardening rules (no-restricted-imports, no-eval, no-new-func) |
| `vite.config.ts` | vite-plugin-remove-console production mode-ban |
| `package.json` | Új dep-ek + size-limit script |

---

## Teszt összefoglaló

| Test file | Tesztek |
|---|---|
| worldCatalog.test.ts | 8 |
| tenantStore.test.ts | 6 |
| authStore.test.ts | 5 |
| WorldGuard.test.tsx | 5 |
| WorldShell.test.tsx | 4 |
| LazyWorldRoute.test.tsx | 5 |
| worldRoutes.test.tsx | 4 |
| **Összesen új** | **37** |
| **Összes** | **136** |

---

## Megjegyzések

- `@size-limit/vite` nem elérhető az npm registry-ben → `@size-limit/preset-small-lib` csere (azonos brotli measurement)
- LazyWorldRoute API: `importFn` helyett `component: LazyExoticComponent` prop — react-hooks/static-components ESLint rule miatt (lazy() nem hívható render közben)
- Legacy route-ok (`/orders`, `/profile`, stb.) megtartva — meglévő 99 teszt érintetlen
- shopfloor world (chrome: 'none', requiresPin: true) implementálva catalog szinten; PIN auth flow Track B/C feladat
