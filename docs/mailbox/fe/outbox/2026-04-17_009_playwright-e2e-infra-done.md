---
id: MSG-FE-009-DONE
from: fe
to: root
type: done
priority: high
status: READ
ref: MSG-FE-009
created: 2026-04-17
---

# FE-009 DONE — Playwright E2E-L2 infrastruktúra bővítés

## Commit

`789e498` — feat: FE-009 — Playwright E2E-L2 infra expansion

## Elvégzett munka

### 1. Új fixture/helper fájlok

| Fájl | Tartalom |
|---|---|
| `tests/e2e/fixtures/seedProfiles.ts` | `SEED_PROFILES` konstansok: `empty-v1`, `doorstar-smoke-v1` |
| `tests/e2e/fixtures/testConfig.ts` | Env var helpers: `tenantId`, `userEmail`, `portalBase`, `apiBase`, `bffEnabled` |
| `tests/e2e/helpers/testIds.ts` | Central `TEST_IDS` registry: `APP_SHELL`, `APP_HEADER`, `NAV_SIDEBAR`, `USER_MENU`, `PROFILE_NAME`, `LOGIN_BUTTON` |
| `tests/e2e/helpers/waitForApi.ts` | `waitForApiResponse()` — URL pattern match + trigger kombinálva |

### 2. global-setup.ts — reset endpoint javítva

- `X-SpaceOS-Internal: true` header (volt: `X-Test-Seeder-Secret`)
- `?confirm=true` query paraméter
- `seedProfile: 'empty-v1'` (INFRA-137 verifikálta)
- `testConfig.tenantId` = `2c84d541-4ccf-4b3a-a932-aca21c43a99e`

### 3. playwright.config.ts — URL szétválasztás

- `baseURL`: `TEST_PORTAL_URL || 'https://portal.joinerytech.hu'` (portal UI)
- A `VITE_API_BASE_URL` csak a reset endpoint számára — a `testConfig.ts`-ben
- Volt: `VITE_API_BASE_URL`-t használta baseURL-ként (rossz!)

### 4. data-testid bővítések

| Komponens | data-testid |
|---|---|
| `AppHeader` `<nav>` | `nav-sidebar` |
| Profile NavLink | `user-menu` (volt: `profile-nav-link`) |

### 5. Flow 01 — 01-login.spec.ts (6 teszt)

- Meglévő 4 teszt megmarad
- Új: `nav-sidebar` legalább 1 link látható
- Új: `user-menu` látható bejelentkezett állapotban

### 6. Flow 08 — 08-auth-edge.spec.ts (5 teszt)

- Meglévő 3 unauthenticated teszt megmarad
- Új: logout flow (authenticatedPage fixture) → Keycloak redirect
- Új: `/dashboard` ismeretlen route → NotFoundPage vagy auth redirect

### 7. pnpm scripts

```json
"test:e2e": "playwright test --config tests/e2e/playwright.config.ts",
"test:e2e:headed": "playwright test --config tests/e2e/playwright.config.ts --headed",
"test:e2e:ui": "playwright test --config tests/e2e/playwright.config.ts --ui"
```

## DoD ellenőrzés

- [x] `pnpm test` (unit + contract) → 90/90 zöld (regresszió nincs)
- [x] `pnpm build` → 0 error
- [x] `pnpm lint` → 0 hiba
- [x] `pnpm typecheck` → 0 hiba
- [x] global-setup.ts: X-SpaceOS-Internal + ?confirm=true + empty-v1
- [x] seedProfiles.ts + testConfig.ts + testIds.ts + waitForApi.ts létrejött
- [x] nav-sidebar + user-menu data-testid hozzáadva AppHeader-hez
- [x] 01-login.spec.ts: 6 teszt (nav-sidebar + user-menu ellenőrzés)
- [x] 08-auth-edge.spec.ts: 5 teszt (logout flow + /dashboard 404)
- [x] test:e2e:headed + test:e2e:ui scripts hozzáadva
- [x] git commit `789e498` + push (main)

## Megjegyzés — CSP blokkolás (CI-001)

A `pnpm test:e2e` E2E futtatás live portal ellen még mindig CI-001 (nginx CSP) blokkolástól függ.
Az E2E tesztek a helyes kóddal rendelkeznek, de az OIDC discovery/token exchange addig nem tud
lefutni, amíg az infra nem adja hozzá `https://joinerytech.hu`-t a `connect-src` CSP direktívához.
Ez az FE scope-on kívüli blocker — külön infra feladat.

## Scope

Flow 02–07 (seed-dependent) → FE-010 (ORCH BE-TEST seed profilok után)
