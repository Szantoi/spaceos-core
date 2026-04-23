---
id: MSG-FE-007
from: root
to: fe
type: task
priority: high
status: READ
ref: FE-PORTAL
created: 2026-04-16
---

# FE-007 — Contract Tests (MSW + Zod) + Playwright E2E scaffold

## Kontextus

WD: `/opt/spaceos/spaceos-doorstar-portal/`. CLAUDE.md kötelező olvasás.
Pipeline: INBOX READ → CODE → BUILD → TEST → REVIEW → SECURITY → OUTBOX

## Előzmény

FE-006 DONE: ErrorBoundary + NotFoundPage + ProfilePage · 87 teszt · commit `9b6bd61`.
Soft Launch MVP FE kész. Ref: `SpaceOS_Doorstar_Portal_UI_Test_Strategy_v4.md`.

## Feladat

A meglévő unit tesztek (87) mellé: Contract tesztréteg (MSW + Zod) + Playwright E2E infrastruktúra scaffold.
A Playwright **flow tesztek** (01-login stb.) later jönnek — most csak a konfig + auth fixture + 01-login skelton kell.

---

## A — Contract tesztek (MSW + Zod)

### A1. Függőségek

```bash
pnpm add -D msw zod @types/msw
```

### A2. Vitest konfig bővítés

```typescript
// vitest.config.ts — bővítsd ki a meglévőt
test: {
  include: [
    'src/**/*.test.{ts,tsx}',         // meglévő unit tesztek
    'tests/contract/**/*.spec.{ts,tsx}' // ÚJ contract tesztek
  ],
}
```

### A3. MSW setup + handlers

```
tests/contract/
├── setup.ts                   ← MSW server (setupServer)
├── handlers/
│   └── auth.handlers.ts       ← /bff/auth/me mock
└── schemas/
    └── authMe.schema.ts       ← Zod: AuthMeResponse
```

**authMe.schema.ts:**
```typescript
import { z } from 'zod';

export const AuthMeResponseSchema = z.object({
  sub: z.string().uuid(),
  email: z.string().email().optional(),
  name: z.string().optional(),
  tenantId: z.string().uuid(),
  tenantName: z.string().optional(),
  roles: z.array(z.string()),
});

export type AuthMeResponse = z.infer<typeof AuthMeResponseSchema>;
```

**auth.handlers.ts:**
```typescript
import { http, HttpResponse } from 'msw';

export const authHandlers = [
  http.get('/bff/auth/me', () =>
    HttpResponse.json({
      sub: '00000000-0000-0000-0000-000000000001',
      email: 'test@spaceos.dev',
      name: 'Test User',
      tenantId: '00000000-0000-0000-0000-000000000002',
      tenantName: 'Test Tenant',
      roles: ['TenantAdmin'],
    })
  ),
];
```

### A4. Contract teszt

```typescript
// tests/contract/auth-contract.spec.ts
import { describe, it, expect, beforeAll, afterAll } from 'vitest';
import { setupServer } from 'msw/node';
import { authHandlers } from './handlers/auth.handlers';
import { AuthMeResponseSchema } from './schemas/authMe.schema';
import { apiClient } from '../../src/api/client';

const server = setupServer(...authHandlers);
beforeAll(() => server.listen());
afterAll(() => server.close());

describe('GET /bff/auth/me contract', () => {
  it('response matches AuthMeResponseSchema', async () => {
    const { data } = await apiClient.get('/auth/me');
    const parsed = AuthMeResponseSchema.safeParse(data);
    expect(parsed.success).toBe(true);
  });

  it('roles is an array', async () => {
    const { data } = await apiClient.get('/auth/me');
    expect(Array.isArray(data.roles)).toBe(true);
  });

  it('tenantId is a valid UUID', async () => {
    const { data } = await apiClient.get('/auth/me');
    const result = AuthMeResponseSchema.pick({ tenantId: true }).safeParse({ tenantId: data.tenantId });
    expect(result.success).toBe(true);
  });
});
```

---

## B — Playwright scaffold

### B1. Függőségek

```bash
pnpm add -D @playwright/test
npx playwright install chromium --with-deps
```

### B2. playwright.config.ts

```typescript
// tests/e2e/playwright.config.ts
import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests/e2e/flows',
  timeout: 60_000,
  retries: 1,
  workers: 1,
  use: {
    baseURL: process.env.VITE_API_BASE_URL || 'https://portal.joinerytech.hu',
    screenshot: 'only-on-failure',
    trace: 'on-first-retry',
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
  ],
  globalSetup: './tests/e2e/global-setup.ts',
});
```

### B3. global-setup.ts (stub — test BFF még nem él)

```typescript
// tests/e2e/global-setup.ts
import { FullConfig } from '@playwright/test';

export default async function globalSetup(_config: FullConfig) {
  const testEndpointsEnabled = process.env.TEST_BFF_ENABLED === 'true';

  if (!testEndpointsEnabled) {
    console.log('ℹ️  Test BFF endpoint not yet available — skipping tenant reset');
    return;
  }

  // TODO: POST /bff/test/tenants/{TEST_TENANT_ID}/reset — aktiválni, ha BE-TEST-01 DONE
  const apiBase = process.env.VITE_API_BASE_URL || 'https://portal.joinerytech.hu';
  const response = await fetch(`${apiBase}/bff/test/tenants/${process.env.TEST_TENANT_ID}/reset`, {
    method: 'POST',
    headers: {
      'X-Test-Seeder-Secret': process.env.TEST_SEEDER_SECRET!,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ seedProfile: 'doorstar-order-lifecycle-v1' }),
  });
  if (!response.ok) throw new Error(`Test tenant reset failed: ${response.status}`);
}
```

### B4. Auth fixture

```typescript
// tests/e2e/fixtures/auth.fixture.ts
import { test as base, Page } from '@playwright/test';

export const test = base.extend<{ authenticatedPage: Page }>({
  authenticatedPage: async ({ page }, use) => {
    await page.goto('/');
    await page.waitForURL(/auth\.joinerytech\.hu/);
    await page.fill('#username', process.env.TEST_USER_EMAIL || 'test-doorstar@spaceos.dev');
    await page.fill('#password', process.env.TEST_USER_PASSWORD!);
    await page.click('#kc-login');
    await page.waitForURL(/portal\.joinerytech\.hu/);
    await page.waitForSelector('[data-testid="app-shell"]', { timeout: 15_000 });
    await use(page);
  },
});

export { expect } from '@playwright/test';
```

### B5. 01-login.spec.ts (első flow — most implementálandó)

```typescript
// tests/e2e/flows/01-login.spec.ts
import { test, expect } from '../fixtures/auth.fixture';

test.describe('Login flow', () => {
  test('successful PKCE login shows orders dashboard', async ({ authenticatedPage: page }) => {
    await expect(page).toHaveURL(/\/orders/);
    await expect(page.getByRole('heading', { name: /rendelések/i })).toBeVisible();
  });

  test('logout redirects to Keycloak login page', async ({ authenticatedPage: page }) => {
    await page.getByRole('link', { name: /profil/i }).click();
    await page.getByRole('button', { name: /kijelentkezés/i }).click();
    await page.waitForURL(/auth\.joinerytech\.hu/);
    await expect(page).toHaveURL(/auth\.joinerytech\.hu/);
  });
});
```

### B6. package.json scripts bővítés

```json
{
  "scripts": {
    "test:unit": "vitest run src",
    "test:contract": "vitest run tests/contract",
    "test": "vitest run",
    "test:e2e": "playwright test --config tests/e2e/playwright.config.ts"
  }
}
```

### B7. CI workflow

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
```

**Megjegyzés (SEC-TS-05):** `test:e2e` NEM fut CI-ban — csak lokálisan a spaceos-fe session-ben.

---

## DoD

- [ ] `pnpm add -D msw zod` — függőségek telepítve
- [ ] `tests/contract/setup.ts` + `auth.handlers.ts` + `authMe.schema.ts` létrejön
- [ ] `auth-contract.spec.ts` — 3 contract teszt futó és zöld
- [ ] `pnpm test` → unit + contract együtt zöld (vitest include mindkettőt)
- [ ] `@playwright/test` telepítve, `playwright.config.ts` kész
- [ ] `tests/e2e/global-setup.ts` + `auth.fixture.ts` + `01-login.spec.ts` létrejön
- [ ] `pnpm test:e2e` parancs konfigurált (fut, akár skip-el ha nincs deployed API)
- [ ] `.github/workflows/ci.yml` létrehozva (unit + contract fut CI-ban)
- [ ] `pnpm build` → 0 error
- [ ] `pnpm test` (unit + contract) → mind zöld, ≥3 új contract teszt
- [ ] `pnpm lint` + `pnpm typecheck` → 0 hiba
- [ ] git commit + push

## Outbox

DONE: `mailbox/fe/outbox/2026-04-16_007_contract-tests-playwright-scaffold-done.md`

## Skillек & Agentек

- `/senior-frontend` — MSW v2 API (http.get syntax), Zod schema design, Vitest multi-include
- `/javascript-typescript-jest` — MSW setupServer, Zod safeParse, contract test pattern
- `/senior-qa` — contract test coverage döntés, Playwright fixture lifecycle
- Agent: `expert-react-frontend-engineer` — MSW + TanStack Query kombinálás, Playwright auth fixture
- Agent: `playwright-tester` — Playwright config, global-setup, PKCE login flow Playwright-ben
- Agent: `se-security-reviewer` — CI workflow secrets, test endpoint feature flag
- Sub-agenteket nyugodtan indíts párhuzamosan (pl. MSW setup + Playwright config egyszerre)
