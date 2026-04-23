---
id: MSG-ORCH-074
from: root
to: orchestrator
type: task
priority: high
status: READ
ref: SpaceOS_Doorstar_Portal_UI_Test_Strategy_v4
created: 2026-04-16
---

# ORCH-074 — BE-TEST-01: `/bff/test/*` route + testGuard + reset handler

## Kontextus

Ref: `docs/tasks/active/FE-TEST-STRATEGY_doorstar-portal-test-infra.md` §6.2–§6.4
INFRA-126 DONE: env vars beállítva.

```
SPACEOS_TEST_ENDPOINTS_ENABLED__DANGER=true   ← .env fájlban él (PM2 + dotenv)
TEST_SEEDER_SECRET=e1880a84d03814d584f51c311896b4bf30bbc7e1ce68eb1bd577deadff9c3229
TEST_TENANT_ALLOWLIST=2c84d541-4ccf-4b3a-a932-aca21c43a99e
```

**Fontos:** Az orchestrator PM2 + dotenv (`dotenv/config`) kombót használ. A `.env` fájlból töltődnek be ezek az értékek — NE az `/etc/spaceos/` path-ot használd.

## Implementálandó

### 1. `testGuard` middleware (`src/middleware/testGuard.ts`)

```typescript
import { Request, Response, NextFunction } from 'express';

// SEC-TS-02: WARNING log startup-on ha enabled
if (process.env.SPACEOS_TEST_ENDPOINTS_ENABLED__DANGER === 'true') {
  console.warn('⚠️  WARNING: Test endpoints are ENABLED. Do NOT use in production.');
}

export function testGuard(req: Request, res: Response, next: NextFunction): void {
  // Layer 1: feature flag
  if (process.env.SPACEOS_TEST_ENDPOINTS_ENABLED__DANGER !== 'true') {
    res.status(404).json({ error: 'Not found' });
    return;
  }

  // Layer 2: seeder secret
  const secret = req.headers['x-test-seeder-secret'] as string;
  if (!secret || secret !== process.env.TEST_SEEDER_SECRET) {
    res.status(403).json({ error: 'Forbidden', message: 'Invalid seeder secret' });
    return;
  }

  // Layer 3: tenant allowlist
  const allowlist = (process.env.TEST_TENANT_ALLOWLIST || '').split(',').map(s => s.trim());
  const tenantId = req.params.tenantId;
  if (tenantId && !allowlist.includes(tenantId)) {
    console.error(`SECURITY: Test reset attempted on non-test tenant ${tenantId}`);
    res.status(403).json({ error: 'Forbidden', message: 'Tenant not in test allowlist' });
    return;
  }

  next();
}
```

### 2. Reset handler (`src/routes/test.route.ts`)

```typescript
import { Router, Request, Response } from 'express';
import axios from 'axios';
import { testGuard } from '../middleware/testGuard';

const router = Router();

// Seed profile-ok (§6.4)
const SEED_PROFILES: Record<string, object> = {
  'empty-v1': {},
  'doorstar-cutting-ready-v1': {
    orders: [{ reference: 'E2E-CUTTING-001', status: 'Submitted' }],
    panelStocks: [
      { material: 'MDF-18', widthMm: 2800, heightMm: 2070, quantity: 10 },
      { material: 'HDF-3', widthMm: 2800, heightMm: 2070, quantity: 5 },
    ],
    suppliers: [{ name: 'E2E Test Supplier' }],
  },
  'doorstar-order-lifecycle-v1': {
    orders: [
      { reference: 'E2E-DRAFT-001', status: 'Draft' },
      { reference: 'E2E-SUBMITTED-001', status: 'Submitted' },
      { reference: 'E2E-DONE-001', status: 'Done' },
    ],
  },
};

router.use('/:tenantId/*', testGuard);
router.use('/:tenantId', testGuard);

router.post('/tenants/:tenantId/reset', async (req: Request, res: Response) => {
  const { tenantId } = req.params;
  const { seedProfile } = req.body as { seedProfile: string };

  // Profil validáció
  if (!SEED_PROFILES[seedProfile]) {
    res.status(404).json({ error: 'Not found', message: `Unknown seed profile: ${seedProfile}` });
    return;
  }

  const internalHeader = { 'X-SpaceOS-Internal': 'true' };
  const baseUrls = {
    kernel: process.env.KERNEL_URL || 'http://127.0.0.1:5000',
    joinery: process.env.JOINERY_URL || 'http://127.0.0.1:5002',
    cutting: process.env.CUTTING_URL || 'http://127.0.0.1:5005',
    inventory: process.env.INVENTORY_URL || 'http://127.0.0.1:5004',
    procurement: process.env.PROCUREMENT_URL || 'http://127.0.0.1:5006',
  };

  const deletedCounts: Record<string, unknown> = {};

  // 1. TÖRLÉS — minden modul (graceful: ha nincs endpoint, folytat)
  const deleteModules = [
    { name: 'joinery', url: `${baseUrls.joinery}/internal/orders/by-tenant/${tenantId}?confirm=true` },
    { name: 'cutting', url: `${baseUrls.cutting}/internal/cutting-sheets/by-tenant/${tenantId}?confirm=true` },
    { name: 'inventory', url: `${baseUrls.inventory}/internal/panel-stocks/by-tenant/${tenantId}?confirm=true` },
    { name: 'procurement', url: `${baseUrls.procurement}/internal/purchase-orders/by-tenant/${tenantId}?confirm=true` },
    { name: 'kernel', url: `${baseUrls.kernel}/internal/flow-epics/by-tenant/${tenantId}?confirm=true` },
  ];

  for (const mod of deleteModules) {
    try {
      const r = await axios.delete(mod.url, { headers: internalHeader });
      deletedCounts[mod.name] = r.data.deletedCounts;
    } catch (err: unknown) {
      // Graceful degradation: ha az endpoint még nincs (404) vagy modul le van állva
      const status = axios.isAxiosError(err) ? err.response?.status : null;
      console.warn(`Test reset: ${mod.name} cleanup failed (${status ?? 'network error'}) — continuing`);
      deletedCounts[mod.name] = { error: `cleanup failed: ${status ?? 'network'}` };
    }
  }

  // 2. SEED — profil alapján (TODO: implementáld a profile-specifikus fixture hívásokat BE-TEST-07-ben)
  const seededEntities: Record<string, number> = { orders: 0, panelStocks: 0, suppliers: 0 };
  // BE-TEST-07 (ORCH-075) bővíti ki a tényleges seed hívásokkal

  res.json({
    tenantId,
    seedProfile,
    resetAt: new Date().toISOString(),
    deletedCounts,
    seededEntities,
  });
});

export default router;
```

### 3. Route regisztráció (`src/app.ts` vagy `src/index.ts`)

```typescript
import testRouter from './routes/test.route';
app.use('/bff/test', testRouter);
```

### 4. Startup warning

A `testGuard.ts` importálása app induláskor (hogy a WARNING log megjelenjen).

## Tesztek (kötelező)

- `POST /bff/test/tenants/{id}/reset` — feature flag disabled → 404
- Rossz `X-Test-Seeder-Secret` → 403
- Tenant nem allowlistban → 403
- Ismeretlen seedProfile → 404
- Sikeres reset → 200, deletedCounts + seededEntities struktúra
- **≥5 új teszt**

## DoD

- [ ] `pnpm build` → 0 error
- [ ] `pnpm test` → ≥5 új teszt zöld
- [ ] `POST /bff/test/tenants/2c84d541-.../reset` → 200 (graceful, ha modul endpoint hiányzik)
- [ ] Rossz secret → 403, disabled flag → 404
- [ ] Startup WARNING log ha `DANGER=true`
- [ ] git commit + push (develop branch)

## Outbox

DONE: `mailbox/orchestrator/outbox/2026-04-16_074_test-bff-endpoint-done.md`

## Skillек & Agentек

- `/senior-fullstack` — Express middleware chain, axios graceful degradation, async reset handler
- `/senior-security` — feature flag guard, secret comparison, startup warning
- `/javascript-typescript-jest` — middleware unit tesztek, axios mock, reset endpoint integration
- Agent: `se-security-reviewer` — testGuard 3 réteg, secret leakage, audit log
- Sub-agenteket nyugodtan indíts párhuzamosan (pl. testGuard middleware + reset handler + tesztek egyszerre)
