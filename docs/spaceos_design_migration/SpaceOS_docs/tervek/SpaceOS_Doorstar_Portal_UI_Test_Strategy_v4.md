# SpaceOS — Doorstar Portal UI Test Strategy
## FE teszt-rétegek + Test BFF Endpoint + E2E-L2 Playwright architektúra

> **Verzió:** v4.0 — 2026-04-16
> **Státusz:** IMPLEMENTÁCIÓRA KÉSZ — minden döntés lezárva
> **Döntéshozó:** Gábor (Architect & Founder)
> **Kontextus:** Design session 2026-04-16 — FE test strategy a `spaceos-doorstar-portal` repóhoz
> **Előzmények:** SpaceOS_Doorstar_Portal_UI_Repo_Architecture_v4.md + v4.1 Amendment
> **Kapcsolódó:** Backend E2E-L1: 214/214 teszt · Keycloak `test-runner` client · `doorstar-kft` group
> **Kumulált review:** — (v1 draft)

---

## 1. Kumulált Finding Összesítő (v1)

| Review | Finding-ek | Legfontosabb javítás | Effort delta |
|--------|------------|----------------------|--------------|
| v1 draft | — | — | FE: ~2 nap · BE: ~2.5 nap |
| v1 → `/senior-security` → v4 | 1C + 2H + 2M | SEC-TS-01: modul-szintű tenant allowlist cleanup endpointokon · SEC-TS-02: `__DANGER` env var + WARNING log · SEC-TS-05: E2E-L2 csak lokálisan, CI = unit+contract | +0.5 nap (BE) |
| **Összesen** | **1C + 2H + 2M** | | **FE: ~2 nap · BE: ~3 nap · Infra: ~1 nap** |

---

## 2. Két világ, két réteg — a teljes kép

```
┌─────────────────────────────────────────────────────────┐
│  E2E-L1 — Backend Integration (MEGLÉVŐ, NEM VÁLTOZIK)   │
│                                                          │
│  Repo: spaceos-e2e (tmux: spaceos-e2e)                   │
│  Stack: Vitest + node fetch                              │
│  Futtatás: gabor user, spaceos-e2e tmux session          │
│  Hívja: deployed BFF API (fetch, nincs browser)          │
│  Seed: Keycloak test-runner client (Direct Access Grant)  │
│  Tesztek: 214/214 ✅                                     │
│  Mit tesztel: Kernel↔Orchestrator↔Modules integráció,    │
│    FlowEpic lifecycle, B2BHandshake, RLS, audit chain,   │
│    cutting flow, nesting, cross-tenant izoláció           │
│                                                          │
│  → NEM MOZDUL, NEM MIGRÁLÓDIK, NEM FÜGG A FE-TŐL        │
└─────────────────────────────────────────────────────────┘
                         │
                         │ deployed API (közös kontraktus)
                         │
┌─────────────────────────────────────────────────────────┐
│  E2E-L2 — UI Workflow (ÚJ, ebben a dokumentumban)        │
│                                                          │
│  Repo: spaceos-doorstar-portal/tests/e2e/                │
│  Stack: Playwright + Chromium headless                    │
│  Futtatás: gabor user, spaceos-fe tmux session            │
│  Hívja: deployed BFF API (böngészőn át, valós UI)         │
│  Seed: Test BFF endpoint (ÚJ — Section 6)                │
│  Tesztek: 0 (most jön létre)                             │
│  Mit tesztel: "Szabász bemegy, belép, lát egy dashboardot,│
│    megnyitja a napi plan-t, elindít egy vágást"           │
│                                                          │
│  → ÚJ RÉTEG, a FE repo része                            │
└─────────────────────────────────────────────────────────┘
```

### Miért két réteg, miért nem egy

| Kérdés | E2E-L1 (backend) | E2E-L2 (UI) |
|---|---|---|
| Mit bizonyít? | Az API-k helyesen működnek együtt | A felhasználó el tudja végezni a feladatát |
| Browser kell? | Nem — fetch hívások | Igen — Playwright Chromium |
| Milyen gyorsan fut? | ~30s (214 teszt) | ~2-5 perc (browser startup, rendering) |
| Ki töri el? | BE kód változás | FE kód változás VAGY BE breaking change |
| Ki javítja? | BE Claude Code (spaceos-kernel/orch/modules) | FE Claude Code (spaceos-fe) |
| Dispatcher session | spaceos-e2e | spaceos-fe |

---

## 3. FE teszt piramis — három réteg

### 3.1 Unit tesztek (Vitest + React Testing Library)

**Cél:** Egyedi komponensek viselkedése izoláltan, hálózat nélkül.

```
tests/unit/
├── components/
│   ├── SheetStatusBadge.spec.tsx     ← render + snapshot
│   ├── CuttingLineTable.spec.tsx     ← props → rendered rows
│   ├── NestingVisualizer.spec.tsx    ← SVG output adott input-ra
│   ├── StockLevelCard.spec.tsx
│   ├── OrderStatusBadge.spec.tsx
│   └── PageShell.spec.tsx            ← sidebar items enabledModules-ből
├── hooks/
│   ├── useAuth.spec.ts               ← token state transitions
│   ├── useTenantConfig.spec.ts       ← enabledModules parsing
│   └── useApiQuery.spec.ts           ← loading/error/success states
└── utils/
    ├── formatDate.spec.ts
    └── formatDimension.spec.ts       ← mm formatting, edge cases
```

**Konvenciók:**

| Szabály | Példa |
|---|---|
| Fájlnév: `{Component}.spec.tsx` | `SheetStatusBadge.spec.tsx` |
| Elhelyezés: `tests/unit/` tükrözi `src/` struktúrát | `tests/unit/components/` ↔ `src/shared/components/` |
| Nincs network mock — props-ból renderel | `render(<Badge status="Validated" />)` |
| TanStack Query tesztelés: `QueryClientProvider` wrapper | `renderWithQuery(<Component />)` helper |
| Snapshot: csak stabil, determinisztikus komponensekre | Badge, formázó utils — nem animált elemek |

**Vitest konfiguráció:**

```typescript
// vitest.config.ts
import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: ['./tests/setup.ts'],
    include: ['tests/unit/**/*.spec.{ts,tsx}'],
    coverage: {
      reporter: ['text', 'lcov'],
      include: ['src/**/*.{ts,tsx}'],
      exclude: ['src/api/generated/**'],  // auto-generált kód kizárva
    },
  },
});
```

### 3.2 Contract tesztek (Vitest + MSW + Zod)

**Cél:** A FE API client és a deployed API **ugyanazt a szerződést** beszéli. Ha a backend változtat és a FE snapshotot frissíti, a contract tesztek elsőként jelzik az inkompatibilitást.

```
tests/contract/
├── setup.ts                          ← MSW server setup
├── handlers/
│   ├── cutting.handlers.ts           ← MSW mock: /bff/cutting/* 
│   ├── inventory.handlers.ts
│   ├── kernel.handlers.ts
│   ├── joinery.handlers.ts
│   └── auth.handlers.ts              ← /bff/api/auth/me mock
├── schemas/
│   ├── authMe.schema.ts              ← Zod: AuthMeResponse
│   ├── cuttingSheet.schema.ts        ← Zod: CuttingSheetResponse
│   └── dailyPlan.schema.ts           ← Zod: DailyCuttingPlanResponse
└── api-contract.spec.ts              ← a tesztek
```

**Mechanizmus:**

```typescript
// tests/contract/api-contract.spec.ts
import { describe, it, expect } from 'vitest';
import { setupServer } from 'msw/node';
import { handlers } from './handlers';
import { cuttingApi } from '../../src/api/cutting.api';
import { CuttingSheetResponseSchema } from './schemas/cuttingSheet.schema';

const server = setupServer(...handlers);

beforeAll(() => server.listen());
afterAll(() => server.close());

describe('Cutting API contract', () => {
  it('GET /cutting-sheets/{id} matches Zod schema', async () => {
    const response = await cuttingApi.getSheet('test-id');
    // Ha a Zod parse sikeres, a response megfelel a sémának
    const parsed = CuttingSheetResponseSchema.parse(response);
    expect(parsed.id).toBe('test-id');
    expect(parsed.lines).toBeInstanceOf(Array);
  });
});
```

**Az MSW handler-ek a `contract/openapi.json`-ból származó példa response-okat adnak vissza.** Ha az OpenAPI frissül és a response struktúra változik, a Zod schema eltörésével a contract teszt jelzi.

**Zod schema scope (FE-UI-05 döntés alapján):** Zod validáció csak a kritikus endpointokon:

| Endpoint | Miért kritikus |
|---|---|
| `GET /bff/api/auth/me` | enabledModules, roles — az app routing ettől függ |
| `GET /bff/cutting/cutting-sheets/{id}` | Nesting vizualizáció — rossz adat = rossz SVG |
| `GET /bff/cutting/daily-plans/{id}` | Plan management — state transitions |
| `GET /bff/inventory/panel-stocks` | Készletszint dashboard — number precision |

A többi endpointon a TypeScript compile-time típusok és az MSW mock-ek elegendő védelmet adnak.

### 3.3 E2E-L2 tesztek (Playwright)

Részletes specifikáció: Section 5 (flow-k) és Section 6 (test BFF endpoint).

---

## 4. Teszt futtatási rend

### 4.1 Fejlesztés közben (spaceos-fe tmux session)

```bash
# Komponens fejlesztés közben — gyors feedback
pnpm test:unit --watch                    # <1s per change

# Új API hívás hozzáadása után
pnpm test:contract                         # ~5s

# Feature branch kész — teljes suite
pnpm test                                  # unit + contract, ~10s
pnpm test:e2e -- --grep "login"           # egyedi E2E flow, ~30s
```

### 4.2 PR / push (CI — GitHub Actions)

```yaml
# .github/workflows/ci.yml
name: FE CI
on: [push, pull_request]
jobs:
  quality:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: pnpm/action-setup@v4
      - uses: actions/setup-node@v4
        with: { node-version: 22 }
      - run: pnpm install --frozen-lockfile
      - run: pnpm typecheck
      - run: pnpm lint
      - run: pnpm test:unit
      - run: pnpm test:contract

  e2e:
    # SEC-TS-05: E2E-L2 NEM fut CI-ban (GitHub Actions nem éri a VPS-t megbízhatóan).
    # E2E-L2 a spaceos-fe tmux session-ben fut lokálisan.
    # Ha self-hosted runner lesz a jövőben, ez a job engedélyezhető.
    if: false  # DISABLED — see SEC-TS-05
    steps:
      - uses: actions/checkout@v4
      - uses: pnpm/action-setup@v4
      - uses: actions/setup-node@v4
        with: { node-version: 22 }
      - run: pnpm install --frozen-lockfile
      - run: npx playwright install chromium
      - run: pnpm test:e2e
    env:
      VITE_API_BASE_URL: https://joinerytech.hu
      VITE_KEYCLOAK_URL: https://auth.joinerytech.hu
      VITE_KEYCLOAK_REALM: spaceos
      VITE_KEYCLOAK_CLIENT_ID: portal-app
      TEST_TENANT_ID: ${{ secrets.TEST_TENANT_ID }}
      TEST_SEEDER_SECRET: ${{ secrets.TEST_SEEDER_SECRET }}
      TEST_USER_PASSWORD: ${{ secrets.TEST_USER_PASSWORD }}
```

### 4.3 Nightly (scheduled)

```yaml
  nightly-e2e:
    runs-on: ubuntu-latest
    schedule:
      - cron: '0 3 * * *'    # 03:00 UTC, minden nap
    steps:
      # ... ugyanaz mint az e2e job, de minden branch-en
```

### 4.4 Összesített futtatási mátrix

| Trigger | Unit | Contract | E2E-L2 | E2E-L1 (BE) |
|---|---|---|---|---|
| File save (watch) | ✅ | — | — | — |
| `pnpm test` | ✅ | ✅ | — | — |
| PR / push (CI) | ✅ | ✅ | — | — |
| Push to main (CI) | ✅ | ✅ | — | — |
| **Lokális** (spaceos-fe tmux) | ✅ | ✅ | ✅ | — |
| BE deploy | — | — | — | ✅ (spaceos-e2e) |

**SEC-TS-05 döntés:** E2E-L2 **nem fut CI-ban** (GitHub Actions → VPS hálózati megbízhatóság kérdéses). Az E2E-L2 a `spaceos-fe` tmux session-ben fut lokálisan, manuális trigger vagy dispatcher task-ként.

---

## 5. E2E-L2 flow-k — lista + fixture vázlat

### 5.1 Playwright konfiguráció

```typescript
// tests/e2e/playwright.config.ts
import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests/e2e/flows',
  timeout: 60_000,
  retries: 1,
  workers: 1,                         // szekvenciális — shared test tenant
  use: {
    baseURL: process.env.VITE_API_BASE_URL || 'https://joinerytech.hu',
    screenshot: 'only-on-failure',
    trace: 'on-first-retry',
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
  ],
  globalSetup: './tests/e2e/global-setup.ts',
});
```

**`workers: 1`**: a tesztek szekvenciálisan futnak, mert egyetlen test tenantot használnak. Párhuzamos futás tenant state-conflictot okozna.

### 5.2 Global setup — test tenant reset

```typescript
// tests/e2e/global-setup.ts
import { FullConfig } from '@playwright/test';

export default async function globalSetup(config: FullConfig) {
  const apiBase = process.env.VITE_API_BASE_URL || 'https://joinerytech.hu';
  const testTenantId = process.env.TEST_TENANT_ID;
  const seederSecret = process.env.TEST_SEEDER_SECRET;

  // Test BFF reset — tiszta state minden suite futtatás előtt
  const response = await fetch(
    `${apiBase}/bff/test/tenants/${testTenantId}/reset`,
    {
      method: 'POST',
      headers: {
        'X-Test-Seeder-Secret': seederSecret!,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        seedProfile: 'doorstar-cutting-ready-v1',
        // Ez a profil létrehoz: 1 DoorOrder (Submitted),
        // 1 CuttingSheet (Received), PanelStock készlet, 1 Supplier
      }),
    }
  );

  if (!response.ok) {
    throw new Error(`Test tenant reset failed: ${response.status} ${await response.text()}`);
  }

  console.log(`Test tenant ${testTenantId} reset to doorstar-cutting-ready`);
}
```

### 5.3 Auth fixture — Playwright login

```typescript
// tests/e2e/fixtures/auth.fixture.ts
import { test as base, Page } from '@playwright/test';

type AuthFixture = {
  authenticatedPage: Page;
};

export const test = base.extend<AuthFixture>({
  authenticatedPage: async ({ page }, use) => {
    // Keycloak PKCE login flow a Playwright browserben
    await page.goto('/');

    // Keycloak login page-re redirectálódik
    await page.waitForURL(/auth\.joinerytech\.hu/);
    await page.fill('#username', process.env.TEST_USER_EMAIL || 'test-doorstar@spaceos.dev');
    await page.fill('#password', process.env.TEST_USER_PASSWORD!);
    await page.click('#kc-login');

    // Callback → app betölt
    await page.waitForURL(/portal\.joinerytech\.hu|localhost:5173/);
    await page.waitForSelector('[data-testid="app-shell"]', { timeout: 15_000 });

    await use(page);
  },
});

export { expect } from '@playwright/test';
```

### 5.4 Flow lista (tervezettje — részletes step-by-step a UI IA dokumentum után)

| # | Flow | Fájl | Mit tesztel | Precondition (seed profile) |
|---|---|---|---|---|
| 01 | Login + dashboard | `01-login.spec.ts` | PKCE login → dashboard betölt → enabledModules alapján sidebar renderelődik | Bármely |
| 02 | Cutting dashboard overview | `02-cutting-dashboard.spec.ts` | CuttingSheet lista betölt, szűrés status-ra, sheet detail navigáció | `doorstar-cutting-ready` |
| 03 | Daily plan management | `03-daily-plan.spec.ts` | Plan létrehozás → sheet assignment → plan confirm | `doorstar-cutting-ready` |
| 04 | Nesting vizualizáció | `04-nesting-view.spec.ts` | Sheet detail → nesting trigger → SVG/Canvas megjelenik | `doorstar-cutting-ready` + sheet Validated |
| 05 | Inventory overview | `05-inventory.spec.ts` | PanelStock lista, OffcutList, StockLevel kártyák | `doorstar-cutting-ready` |
| 06 | Order lifecycle | `06-order-flow.spec.ts` | Order lista → detail → submit → snapshot megjelenik | `doorstar-cutting-ready` |
| 07 | Procurement basics | `07-procurement.spec.ts` | Supplier lista, PO create → submit | `doorstar-cutting-ready` |
| 08 | Auth edge cases | `08-auth-edge.spec.ts` | Token expiry → silent renew, unauthorized → redirect, logout | Bármely |
| 09 | Responsive layout | `09-responsive.spec.ts` | Sidebar collapse mobile-on, table scroll, page shell | Bármely |

**Fontos:** a flow-k részletes step-by-step specifikációja a **UI IA dokumentum után** készül el, mert addig nem tudjuk pontosan milyen oldalak, milyen elrendezésben, milyen `data-testid`-kel lesznek. A fenti lista a **tervezett lefedettség** — nem implementációs spec.

### 5.5 Fixture vázlat

```
tests/e2e/
├── global-setup.ts                    # test tenant reset (Section 5.2)
├── fixtures/
│   ├── auth.fixture.ts                # authenticated page (Section 5.3)
│   ├── seedProfiles.ts                # seed profile nevek és leírások
│   └── testConfig.ts                  # env var-ok, URL-ek
├── flows/
│   ├── 01-login.spec.ts
│   ├── 02-cutting-dashboard.spec.ts
│   ├── 03-daily-plan.spec.ts
│   ├── 04-nesting-view.spec.ts
│   ├── 05-inventory.spec.ts
│   ├── 06-order-flow.spec.ts
│   ├── 07-procurement.spec.ts
│   ├── 08-auth-edge.spec.ts
│   └── 09-responsive.spec.ts
├── helpers/
│   ├── waitForApi.ts                  # page.waitForResponse wrapper
│   └── testIds.ts                     # data-testid konstansok
└── playwright.config.ts
```

---

## 6. Test BFF Endpoint — részletes BE specifikáció

Ez a szekció a **backend csapatnak** szól. A root koordinátor a spaceos-orch dispatcher session-be adja ki task-ként.

### 6.1 Architekturális döntés

A test BFF endpoint az **Orchestrator** rétegben (L3) él, nem a Kernel-ben. Indoklás:
- Az Orchestrator az egyetlen entry point a deployed BFF-en
- A reset logika több modult érint (Kernel tenant + Joinery order + Cutting sheet + Inventory stock) — az Orchestrator ismeri az összes downstream szolgáltatást
- A Kernel frozen rule betartva: nincs üzleti logika a Kernelben a teszteléshez

### 6.2 Endpoint specifikáció

```
POST /bff/test/tenants/{tenantId}/reset

Headers:
  X-Test-Seeder-Secret: {secret}      ← kötelező
  Content-Type: application/json

Body:
{
  "seedProfile": "doorstar-cutting-ready"   ← melyik adatprofil legyen
}

Response 200:
{
  "tenantId": "uuid",
  "seedProfile": "doorstar-cutting-ready",
  "resetAt": "2026-04-17T08:00:00Z",
  "seededEntities": {
    "orders": 1,
    "cuttingSheets": 1,
    "panelStocks": 5,
    "suppliers": 1
  }
}

Response 403:
{ "error": "Forbidden", "message": "Tenant not in test allowlist" }

Response 404:
{ "error": "Not found", "message": "Unknown seed profile" }
```

### 6.3 Security modell — négy réteg

| Réteg | Ellenőrzés | Mi történik ha fail |
|---|---|---|
| **1. Route guard** | `/bff/test/*` prefix → middleware check | 404 (a route nem is létezik ha disabled) |
| **2. Feature flag** | `SPACEOS_TEST_ENDPOINTS_ENABLED__DANGER=true` env var | 404 — prod-ban ez `false` vagy nincs beállítva |
| **3. Seeder secret** | `X-Test-Seeder-Secret` header === `TEST_SEEDER_SECRET` env var | 403 Forbidden |
| **4. Tenant allowlist** | `tenantId` benne van a `TEST_TENANT_ALLOWLIST` env var-ban (comma-separated UUID lista) | 403 Forbidden + audit log |

```typescript
// Orchestrator: middleware/testGuard.ts
// SEC-TS-02: WARNING log on startup if test endpoints are enabled
if (process.env.SPACEOS_TEST_ENDPOINTS_ENABLED__DANGER === 'true') {
  console.warn('⚠️  WARNING: Test endpoints are ENABLED. Do NOT use in production.');
}

export function testGuard(req: Request, res: Response, next: NextFunction) {
  // Layer 1: feature flag (SEC-TS-02: __DANGER suffix = explicit warning)
  if (process.env.SPACEOS_TEST_ENDPOINTS_ENABLED__DANGER !== 'true') {
    return res.status(404).json({ error: 'Not found' });
  }

  // Layer 2: seeder secret
  const secret = req.headers['x-test-seeder-secret'];
  if (!secret || secret !== process.env.TEST_SEEDER_SECRET) {
    return res.status(403).json({ error: 'Forbidden', message: 'Invalid seeder secret' });
  }

  // Layer 3: tenant allowlist
  const allowlist = (process.env.TEST_TENANT_ALLOWLIST || '').split(',').map(s => s.trim());
  const tenantId = req.params.tenantId;
  if (!allowlist.includes(tenantId)) {
    console.error(`SECURITY: Test reset attempted on non-test tenant ${tenantId}`);
    return res.status(403).json({ error: 'Forbidden', message: 'Tenant not in test allowlist' });
  }

  next();
}

// Route registration:
app.use('/bff/test/*', testGuard);
app.post('/bff/test/tenants/:tenantId/reset', testResetHandler);
```

### 6.4 Reset logika — seed profile-ok

A reset handler az alábbi szekvenciában dolgozik:

```
1. DELETE: test tenant összes adata törölve (Cutting sheets, Daily plans, Orders, Stock, POs)
   → Kernel: DELETE /api/flow-epics?tenantId={testTenantId} (internal endpoint)
   → Joinery: DELETE /internal/orders/by-tenant/{testTenantId}
   → Cutting: DELETE /internal/cutting-sheets/by-tenant/{testTenantId}
   → Inventory: DELETE /internal/panel-stocks/by-tenant/{testTenantId}
   → Procurement: DELETE /internal/purchase-orders/by-tenant/{testTenantId}

2. SEED: seed profile alapján újra létrehozás
   → a profile-specifikus fixture adatot POST-olja a modulok felé
```

**Seed profile-ok (SEC-TS-04: verzionáltak):**

| Profil | Létrehozott adat | Mire jó |
|---|---|---|
| `empty-v1` | Semmi — tiszta tenant, csak a Keycloak group + Kernel tenant rekord | Login + dashboard tesztek |
| `doorstar-cutting-ready-v1` | 1 DoorOrder (Submitted) + 1 CuttingSheet (Received) + 5 PanelStock (MDF 18/HDF 3/MDF 20 stb.) + 1 Supplier + 1 MaterialCatalog entry | Cutting dashboard, nesting, inventory, procurement tesztek |
| `doorstar-order-lifecycle-v1` | 3 DoorOrder (Draft/Submitted/Completed) + 2 CuttingSheet (Received/Validated) | Order flow tesztek, státusz szűrés |

Breaking domain change → új profil verzió (`-v2`), régi archivált. A global-setup response tartalmazza a profil verziót.

### 6.5 Internal cleanup endpointok — BE backlog

A reset logika **internal** endpointokat igényel minden modulban. Ezek nem nyilvános API-k — csak az Orchestrator `/internal/` prefix-en hívhatja.

| Modul | Internal endpoint | Művelet |
|---|---|---|
| Kernel | `DELETE /internal/flow-epics/by-tenant/{tenantId}` | FlowEpic + Snapshot + Audit törlés |
| Joinery | `DELETE /internal/orders/by-tenant/{tenantId}` | DoorOrder + CuttingListSnapshot törlés |
| Cutting | `DELETE /internal/cutting-sheets/by-tenant/{tenantId}` | CuttingSheet + DailyCuttingPlan törlés |
| Inventory | `DELETE /internal/panel-stocks/by-tenant/{tenantId}` | PanelStock + Offcut + StockMovement törlés |
| Procurement | `DELETE /internal/purchase-orders/by-tenant/{tenantId}` | PurchaseOrder + Delivery törlés |

**Biztonsági kényszer (SEC-TS-01 — CRITICAL finding):** Minden internal cleanup endpoint:
- Csak `X-SpaceOS-Internal` header-rel hívható (meglévő SEC-01 mechanizmus)
- **Modul-szintű tenant allowlist ellenőrzés** (defense in depth): minden modul a saját `TEST_TENANT_ALLOWLIST` env varjából ellenőrzi a tenantId-t. Ha nincs a listán → 403. Ez véd az Orchestrator kompromittálódás ellen is.
- Audit log bejegyzés: `"Test reset: {tenantId}, profile: {profile}"`
- A `TEST_TENANT_ALLOWLIST` env var **minden modulban** be kell legyen állítva (Kernel, Joinery, Cutting, Inventory, Procurement)

### 6.6 Implementációs effort

| Task ID | Feladat | Modul | Effort | Dispatcher target |
|---|---|---|---|---|
| BE-TEST-01 | Orchestrator: `/bff/test/*` route + testGuard middleware + reset handler | spaceos-orch | ~1 nap | spaceos-orch inbox |
| BE-TEST-02 | Kernel: `DELETE /internal/flow-epics/by-tenant/{tenantId}` | spaceos-kernel | ~0.5 nap | spaceos-kernel inbox |
| BE-TEST-03 | Joinery: `DELETE /internal/orders/by-tenant/{tenantId}` | spaceos-joinery | ~0.25 nap | spaceos-joinery inbox |
| BE-TEST-04 | Cutting: `DELETE /internal/cutting-sheets/by-tenant/{tenantId}` | spaceos-cutting (tbd) | ~0.25 nap | spaceos-cutting inbox (ha van) |
| BE-TEST-05 | Inventory: `DELETE /internal/panel-stocks/by-tenant/{tenantId}` | spaceos-inventory (tbd) | ~0.25 nap | hasonló |
| BE-TEST-06 | Procurement: `DELETE /internal/purchase-orders/by-tenant/{tenantId}` | spaceos-procurement (tbd) | ~0.25 nap | hasonló |
| BE-TEST-07 | Seed profile fixture data (JSON → API hívások az Orchestrator-ban) | spaceos-orch | ~0.5 nap | spaceos-orch inbox |
| **Összesen BE** | | | **~3 nap** | |

---

## 7. Test tenant — Keycloak + Kernel konfiguráció

### 7.1 Jelenlegi Keycloak struktúra

```
Keycloak realm: spaceos
├── Client: portal-app          (PKCE, public)
├── Client: orch-bff            (confidential)
├── Client: kernel-api          (confidential)
├── Client: test-runner         (confidential, Direct Access Grant — E2E-L1)
│
├── Group: doorstar-kft         (prod tenant)
│   ├── tenant_id: a1b2c3d4-...
│   ├── tenant_type: Producer
│   └── enabled_modules: ["door"]
│
└── Users:
    ├── test-admin@spaceos.dev       (group: doorstar-kft, role: TenantAdmin)
    ├── designer-rbac@spaceos.dev    (group: doorstar-kft, role: Designer)
    └── designer-read@spaceos.dev    (group: doorstar-kft, role: DesignerRead)
```

### 7.2 Új test tenant létrehozása

```
Keycloak:
├── Group: doorstar-e2e-test                     ← ÚJ
│   ├── tenant_id: {TEST_TENANT_UUID}            ← dedicated UUID, NEM a prod Doorstar
│   ├── tenant_type: Producer
│   └── enabled_modules: ["door", "cutting"]
│
└── Users:
    └── test-doorstar@spaceos.dev                ← ÚJ (vagy meglévő test user, e2e group-ba)
        ├── group: doorstar-e2e-test
        ├── role: TenantAdmin
        └── password: {TEST_USER_PASSWORD}       ← GitHub Secret

Kernel DB:
  INSERT INTO "Tenants" ("Id", "Name", "TenantType", "EnabledModules", ...)
  VALUES ('{TEST_TENANT_UUID}', 'Doorstar E2E Test', 'Manufacturer', '{"door","cutting"}', ...);

  INSERT INTO "TenantHandshakeAllowlist" ...    ← ha kell B2B teszthez
```

**Kritikus:** a `TEST_TENANT_UUID` soha nem egyezhet a prod `doorstar-kft` UUID-jával. A test tenant **teljesen szeparált** — saját adatok, saját életciklus, resetelhető.

### 7.3 VPS environment változók

```bash
# /etc/spaceos/orchestrator.env — kiegészítés
SPACEOS_TEST_ENDPOINTS_ENABLED__DANGER=true        # VPS dev-en true, prod-ban false/hiányzik
TEST_SEEDER_SECRET=<random-64-char-hex>    # openssl rand -hex 32
TEST_TENANT_ALLOWLIST=<TEST_TENANT_UUID>   # comma-separated, most egyetlen UUID
```

### 7.4 Infra feladatok

| Task ID | Feladat | Effort | Dispatcher target |
|---|---|---|---|
| INFRA-TEST-01 | Keycloak: `doorstar-e2e-test` group + user + claim mappers | ~0.5 nap | spaceos-infra inbox |
| INFRA-TEST-02 | Kernel DB: test tenant seed SQL | ~0.25 nap | spaceos-infra inbox |
| INFRA-TEST-03 | Orchestrator env vars (TEST_SEEDER_SECRET, TEST_TENANT_ALLOWLIST) | ~0.25 nap | spaceos-infra inbox |
| **Összesen Infra** | | **~1 nap** | |

---

## 8. A meglévő 214 E2E-L1 viszonya

| Kérdés | Válasz |
|---|---|
| Változik-e az E2E-L1? | **Nem.** Marad ahol van, ahogy van. |
| Használja-e a test BFF endpoint-ot? | **Nem.** Az L1 a saját seed mechanizmusát használja (Keycloak `test-runner` Direct Access Grant + fetch). |
| Futtatja-e a FE CI? | **Nem.** Az L1 a BE repóban fut, a spaceos-e2e tmux session-ben. |
| Ha a test BFF endpoint elromlik, az L1-et érinti? | **Nem.** Az L1 nem használja. |
| Ha a BE kódot változtatják és az L1 elhasal, az L2-t érinti? | **Csak ha breaking API change.** Az L2 a deployed API-t hívja — ha az API változik, mindkét réteg érintett. |
| Lehet-e az L1 és L2 ugyanazt a test tenantot használni? | **NEM ajánlott.** Külön tenant → nincs state ütközés. Az L1 a meglévő `doorstar-kft` tenanton fut (prod-szerű adat), az L2 a `doorstar-e2e-test`-en (resetelhető). |

---

## 9. CLAUDE.md kiegészítés a test stratégiához

A v4 + v4.1 CLAUDE.md-hez hozzáadandó:

```markdown
## Testing

### Unit Tests (tests/unit/)
- Vitest + React Testing Library
- Every component gets a .spec.tsx file
- No network calls — render from props
- TanStack Query: use renderWithQuery() helper
- Coverage: src/ except src/api/generated/

### Contract Tests (tests/contract/)
- MSW mock handlers mirror the OpenAPI spec
- Zod schemas on critical endpoints only: /auth/me, cutting-sheet detail, daily-plan detail
- If a contract test fails after pnpm contract:refresh → the API changed and you need to update
  handlers + schemas + possibly component code

### E2E Tests (tests/e2e/)
- Playwright, headless Chromium, sequential (workers: 1)
- Auth via Keycloak login page (real PKCE flow in browser)
- Test tenant reset via POST /bff/test/tenants/{id}/reset before each suite
- Do NOT hardcode test data — use the seed profile response
- Screenshots on failure (playwright/screenshots/)
- data-testid attributes on every interactive element

### Running Tests
pnpm test:unit              # unit only, fast
pnpm test:contract          # contract only
pnpm test                   # unit + contract
pnpm test:e2e               # Playwright (needs deployed API)
pnpm test:e2e -- --grep "login"  # single flow
```

---

## 10. Definition of Done

### FE teszt scaffold gates

- [ ] Vitest konfiguráció: `vitest.config.ts` (environment: jsdom)
- [ ] `tests/setup.ts` — RTL global imports, QueryClient wrapper
- [ ] Legalább 1 unit teszt futó és zöld (`pnpm test:unit`)
- [ ] MSW setup: `tests/contract/setup.ts` + legalább 1 handler
- [ ] Zod schema: `AuthMeResponseSchema` kész és validál
- [ ] Legalább 1 contract teszt futó és zöld (`pnpm test:contract`)
- [ ] Playwright konfiguráció: `playwright.config.ts`
- [ ] Auth fixture: `auth.fixture.ts` — Keycloak login flow
- [ ] Global setup: test tenant reset hívás
- [ ] Legalább 1 E2E flow futó: `01-login.spec.ts`
- [ ] `pnpm test` → unit + contract zöld
- [ ] CI workflow: `.github/workflows/ci.yml` — lint + typecheck + unit + contract

### BE test infra gates

- [ ] Orchestrator: `/bff/test/tenants/{id}/reset` endpoint deployed
- [ ] testGuard middleware: feature flag + seeder secret + tenant allowlist
- [ ] Legalább `empty` és `doorstar-cutting-ready` seed profile implementálva
- [ ] Internal cleanup endpointok: Kernel + Joinery + Cutting + Inventory + Procurement
- [ ] Test tenant: `doorstar-e2e-test` Keycloak group + Kernel seed
- [ ] VPS env vars: `SPACEOS_TEST_ENDPOINTS_ENABLED__DANGER`, `TEST_SEEDER_SECRET`, `TEST_TENANT_ALLOWLIST`
- [ ] `/bff/test/*` → prod tenantokra 403 (manuális ellenőrzés)

### Összesített

- [ ] Meglévő 214 E2E-L1 teszt ZÖLD (nem törhet el)
- [ ] Meglévő 2289 unit/integration teszt ZÖLD
- [ ] FE: `pnpm test` → 0 fail
- [ ] FE: `pnpm test:e2e` → legalább `01-login.spec.ts` zöld
- [ ] BE: új tesztek a test endpoint-okra (Orchestrator: ≥5, modulonként: ≥2)

---

## 11. Implementációs csomag

### Végrehajtási sorrend (párhuzamos trackek)

```
       FE TRACK                          BE TRACK                    INFRA TRACK
       (spaceos-fe)                      (spaceos-orch/kernel/...)   (spaceos-infra)
       ─────────────                     ────────────────────────    ──────────────
Nap 1  Vitest config                     —                          INFRA-TEST-01: KC group
       Unit test scaffold                                           INFRA-TEST-02: Kernel seed
       MSW + Zod setup                                              INFRA-TEST-03: env vars

Nap 2  Contract tests                    BE-TEST-01: Orch route     —
       Playwright config                 BE-TEST-02: Kernel cleanup
       Auth fixture                      BE-TEST-03: Joinery cleanup

Nap 3  01-login.spec.ts                 BE-TEST-04/05/06: Cutting/   —
       02-cutting-dashboard.spec.ts      Inventory/Procurement
       (ha UI pages léteznek)            cleanup

Nap 4  Remaining flows                   BE-TEST-07: Seed profiles   Deploy + verify
       CI pipeline finalize              Orch teszt
```

### Dispatcher task kiadási sorrend

A root koordinátor az alábbi sorrendben adja ki a taskokat inbox-ba:

| # | Target session | Task | Prereq |
|---|---|---|---|
| 1 | spaceos-infra | INFRA-TEST-01: KC `doorstar-e2e-test` group + user | — |
| 2 | spaceos-infra | INFRA-TEST-02: Kernel DB test tenant seed SQL | INFRA-TEST-01 DONE |
| 3 | spaceos-infra | INFRA-TEST-03: Orch env vars | — |
| 4 | spaceos-orch | BE-TEST-01: `/bff/test/*` route + testGuard + reset handler | INFRA-TEST-03 DONE |
| 5 | spaceos-kernel | BE-TEST-02: `DELETE /internal/flow-epics/by-tenant/*` | — |
| 6 | spaceos-joinery | BE-TEST-03: `DELETE /internal/orders/by-tenant/*` | — |
| 7 | spaceos-fe | FE scaffold: Vitest + MSW + Playwright config | FE repo scaffold DONE (v4) |
| 8 | spaceos-orch | BE-TEST-07: Seed profiles | BE-TEST-01..06 DONE |
| 9 | spaceos-fe | E2E flows: 01-login.spec.ts, 02-cutting-dashboard.spec.ts | BE-TEST-07 DONE + FE pages exist |

**Note:** #5 és #6 párhuzamosan kiadhatók (nincs egymásra függés). #7 párhuzamosan futhat #4-#6-tal. #9 csak akkor ha a UI pages léteznek (UI IA dokumentum utáni implementáció).

### Kockázatok

| Kockázat | Valószínűség | Hatás | Mitigáció |
|---|---|---|---|
| Test BFF reset több modulra hat — integrációs hiba | Közepes | E2E teszt nem indul | Seed profile-ok egyenként tesztelhetők curl-lel |
| Keycloak test user token expiry E2E közben | Alacsony | Auth failure mid-test | Playwright `storageState` re-auth fixture |
| GitHub Actions nem éri a VPS-t (firewall) | Közepes | CI E2E nem fut | GitHub Actions IP range → UFW allowlist VAGY self-hosted runner |
| Test tenant state corruption (párhuzamos futás) | Közepes | Flaky tesztek | `workers: 1` + global reset |
| Internal cleanup endpoint véletlenül prod tenanton | Nagyon alacsony | Adatvesztés | 4 rétegű security (feature flag + secret + allowlist + modul-szintű ellenőrzés) |

---

## 12. Mi jön utána

| # | Dokumentum | Prereq |
|---|---|---|
| ✅ | **SpaceOS_Doorstar_Portal_UI_Repo_Architecture_v4 + v4.1** | — |
| ✅ | **Ez a dokumentum (Test Strategy)** | — |
| 3 | `SpaceOS_Doorstar_Portal_UI_IA_v1.md` — UI Information Architecture | Repo scaffold DONE |
| 4 | E2E flow részletes step-by-step spec | UI IA DONE + pages implementálva |

---

*SpaceOS — Doorstar Portal UI Test Strategy v4.0 · 2026. április 16.*
*`/senior-security` reviewed · 5 finding beépítve, minden döntés lezárva*
*Státusz: IMPLEMENTÁCIÓRA KÉSZ*
