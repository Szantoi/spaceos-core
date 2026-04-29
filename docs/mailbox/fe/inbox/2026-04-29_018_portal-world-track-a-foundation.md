---
id: MSG-FE-018
from: root
to: fe
type: task
priority: high
status: READ
ref: SpaceOS_Portal_World_Architecture_v4_final.md
created: 2026-04-29
---

# FE-018 — Portal World Track A: Foundation (Day 1–3)

> **Tervdok:** `docs/tasks/active/SpaceOS_Portal_World_Architecture_v4_final.md` — KÖTELEZŐ olvasmány!
> **README:** `docs/tasks/active/SpaceOS_Portal_World_Architecture_v4_README.md`
> **Skill:** `/spaceos-terminal` szerint dolgozz
> **Repo:** `/opt/spaceos/spaceos-doorstar-portal/`
> **FONTOS:** React Router v7 (nem v6!) — a spec kódpéldái backwards-compatible
> **Használhatsz sub-agent-eket** ha szükséges

---

## Day 1 — Dependency setup + Auth hardening

### Új függőségek installálása

```bash
pnpm add zustand react-hook-form zod @radix-ui/react-dialog
pnpm add -D size-limit vite-plugin-remove-console
```

### Auth hardening (SEC-FE-01..06)

- SEC-FE-01: `userManager.userStore = InMemoryWebStorage()` verify (már megvan, de CI grep gate hozzáadás)
- SEC-FE-02: ESLint `no-restricted-imports: jwt-decode, jose` — minden auth state `/me/session`-ből
- SEC-FE-03: `enabledModules` URL bypass prevention — guard check
- ESLint config: `dangerouslySetInnerHTML`, `eval`, `token` grep gate-ek

---

## Day 2 — World system core

### `worldCatalog.ts`

```typescript
// 5 world definition: Home, Sales, Production, ShopFloor, Settings
// Minden world: id, label, icon, routePrefix, requiredModule, chrome
```

### `WorldShell` layout

```typescript
// chrome: 'standard' | 'minimal' | 'none'
// Standard: sidebar + header + breadcrumb
// Minimal: header only (Shop Floor)
// None: fullscreen (kiosk)
```

### `WorldGuard` + `LazyWorldRoute` HOC

- `WorldGuard`: enabledModules check → redirect ha nem elérhető
- `LazyWorldRoute`: lazy import + Suspense + error boundary per world

### Zustand store-ok

- `tenantStore` — tenant info + enabledModules (from `/me/session`)
- `authStore` — session state (isAuthenticated, user, refreshSession)

---

## Day 3 — Router restructure + size-limit

### Router

Az összes meglévő route-ot a `WorldShell` alá szervezni:
```
/ → WorldHome (auto-redirect a tenant default world-jébe)
/w/sales/* → Sales world routes
/w/production/* → Production world routes
/w/shopfloor/* → Shop Floor routes
/w/settings/* → Settings routes
```

### size-limit CI gate (FE-15)

```json
// .size-limit.json
[{ "path": "dist/assets/*.js", "limit": "350 KB" }]
```

### vite-plugin-remove-console (SEC-FE-16)

Production build-ben `console.log` strip.

---

## Tesztek (25+)

- worldCatalog: 5 world definition helyes
- WorldGuard: enabledModules check, redirect
- LazyWorldRoute: lazy load, error boundary
- tenantStore: session fetch, enabledModules cache
- authStore: login/logout state transitions
- Router: world route-ok renderelnek
- size-limit: budget check

## Definition of Done

- [ ] Új dep-ek installálva (zustand, react-hook-form, zod, radix-ui, size-limit)
- [ ] Auth hardening ESLint gates
- [ ] worldCatalog + WorldShell + WorldGuard + LazyWorldRoute
- [ ] Zustand store-ok (tenant + auth)
- [ ] Router restructure (/ → /w/sales/*, /w/production/*, etc.)
- [ ] size-limit CI gate
- [ ] `pnpm build` 0 error
- [ ] `pnpm test` ≥ 124 pass (99 + 25 új)
- [ ] Outbox DONE
