---
id: MSG-FE-009
from: root
to: fe
type: task
priority: high
status: READ
ref: FE-TEST-STRATEGY_doorstar-portal-test-infra
created: 2026-04-17
---

# FE-009 — Playwright E2E-L2 infrastruktúra + Flow 01 + Flow 08

## Kontextus

A reset pipeline éles és működik — INFRA-137 verifikálta:
- `POST /bff/test/tenants/{id}/reset` → HTTP 200, `deletedCounts` mind szám, nincs "error"
- `empty-v1` és `doorstar-smoke-v1` seed profilok elérhetők

A test strategy (FE-TEST-STRATEGY_doorstar-portal-test-infra.md) alapján most az E2E-L2 Playwright infrastruktúrát kell felépíteni, és a seed-independent flow-kat implementálni.

## Feladat

### 1. Playwright infrastruktúra

A test strategy Section 5.1–5.5 alapján:

```
tests/e2e/
├── playwright.config.ts        # workers:1, baseURL, screenshot on failure
├── global-setup.ts             # reset endpoint hívás (empty-v1 profillal)
├── fixtures/
│   ├── auth.fixture.ts         # authenticatedPage fixture (PKCE login)
│   ├── seedProfiles.ts         # profil nevek konstansok
│   └── testConfig.ts           # env var-ok (TEST_TENANT_ID, TEST_USER_EMAIL, stb.)
└── helpers/
    ├── waitForApi.ts
    └── testIds.ts              # data-testid konstansok
```

**Env változók** (`.env.test.local` példa, `.gitignore`-ban):
```
TEST_TENANT_ID=2c84d541-4ccf-4b3a-a932-aca21c43a99e
TEST_USER_EMAIL=test-doorstar@spaceos.dev
TEST_USER_PASSWORD=<Keycloak test user jelszava>
VITE_API_BASE_URL=https://joinerytech.hu
```

**Fontos — global-setup.ts:** A reset endpoint a test strategy-ben `POST` + `X-Test-Seeder-Secret` headerrel van specifikálva, de az implementált endpoint az `X-SpaceOS-Internal: true` headert + `?confirm=true` query paraméterként kapta. Ellenőrizd az aktuális ORCH reset implementációt és igazítsd hozzá.

### 2. Flow 01 — Login + Dashboard (`01-login.spec.ts`)

**Precondition:** Bármely seed profil (empty-v1 OK)

**Steps:**
1. `page.goto('/')` → PKCE redirect Keycloak-ra
2. `waitForURL(/auth\.joinerytech\.hu/)` → login form megjelenik
3. Kitölti username + password → submit
4. `waitForURL(/portal\.joinerytech\.hu/)` → callback → app betölt
5. `waitForSelector('[data-testid="app-shell"]')` → dashboard renderelve
6. Sidebar: legalább 1 nav item látható
7. `expect(page.url()).toContain('portal.joinerytech.hu')` — nem Keycloak oldalon vagyunk

**data-testid elvárások** (ha nincs meg a portálban, add hozzá a releváns komponenshez):
- `app-shell` — a fő wrapper div
- `nav-sidebar` — a sidebar navigáció
- `user-menu` — a bejelentkezett user neve/menü

### 3. Flow 08 — Auth edge cases (`08-auth-edge.spec.ts`)

**Precondition:** Bármely seed profil

**Steps:**
1. **Logout flow:** Bejelentkezett page → logout gomb → Keycloak logout → `/` redirect → login page
2. **Unauthorized redirect:** Töröld a session storage-ból a tokent → navigálj `page.goto('/dashboard')` → redirect login page-re
3. **Invalid tenant:** Ha van olyan env var hogy TEST_INVALID_USER → 403/401 → hibakezelő UI megjelenik

**Megjegyzés:** Ha a portal logout gombja nincs data-testid-del ellátva, add hozzá.

### 4. pnpm scripts frissítés

`package.json`-ban:
```json
"test:e2e": "playwright test",
"test:e2e:headed": "playwright test --headed",
"test:e2e:ui": "playwright test --ui"
```

### 5. `@playwright/test` csomag hozzáadása

```bash
pnpm add -D @playwright/test
npx playwright install chromium
```

## DoD

- [ ] `pnpm test` (unit + contract) → zöld (regresszió nincs)
- [ ] `pnpm test:e2e` → 01-login.spec.ts + 08-auth-edge.spec.ts fut, min. 3 teszt zöld
- [ ] global-setup.ts reset endpoint sikeres (HTTP 200)
- [ ] git commit + push (develop)

## Scope határ

**NEM kell ebben a taskban:**
- Flow 02–07 (seed-dependent, `doorstar-cutting-ready-v1` profil kell → ORCH-077-ben jön)
- UI IA dokumentum (külön task)
- CI integráció (SEC-TS-05: E2E-L2 nem fut CI-ban)

## Outbox

DONE: `mailbox/fe/outbox/2026-04-17_009_playwright-e2e-infra-done.md`

## Skillek & Agentек

- `/senior-frontend` — Playwright setup, PKCE auth flow tesztelése
- Sub-agenteket nyugodtan indíts
