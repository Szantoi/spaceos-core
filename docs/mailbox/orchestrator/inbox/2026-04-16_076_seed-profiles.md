---
id: MSG-ORCH-076
from: root
to: orchestrator
type: task
priority: high
status: READ
ref: SpaceOS_Doorstar_Portal_UI_Test_Strategy_v4 §6.3
created: 2026-04-16
---

# ORCH-076 — BE-TEST-07: Seed profiles implementáció

## Kontextus

Ref: `docs/tasks/new/SpaceOS_Doorstar_Portal_UI_Test_Strategy_v4.md` §6.3

A `/bff/test/tenants/:tenantId/reset` endpoint már él (ORCH-074 ✅, INFRA-129 ✅).
Jelenleg a `seedProfile` paramétert nem dolgozza fel — a response-ban `seededEntities: {orders: 0, ...}`.
Ez a task a seed logikát implementálja.

## Implementálandó

### Seed profilok

```typescript
// src/routes/test.route.ts — extend meglévő reset handler

interface SeedProfile {
  name: string;
  seed: (tenantId: string) => Promise<SeededEntities>;
}

interface SeededEntities {
  orders: number;
  panelStocks: number;
  suppliers: number;
}
```

### `empty-v1` profil (kötelező)

```typescript
const emptyV1: SeedProfile = {
  name: 'empty-v1',
  async seed(_tenantId) {
    // Nincs seedelés — tiszta állapot
    return { orders: 0, panelStocks: 0, suppliers: 0 };
  }
};
```

### `doorstar-smoke-v1` profil (kötelező)

```typescript
const doorstarSmokeV1: SeedProfile = {
  name: 'doorstar-smoke-v1',
  async seed(tenantId) {
    // 1 aktív megrendelés a Joinery-n keresztül BFF API-n
    // POST /bff/api/orders  (X-SpaceOS-Brand: doorstar-kft header-rel)
    // A belső BFF híváshoz: használd a meglévő axiosKernel/axiosJoinery klienst
    // seed token: Keycloak Direct Access Grant — test-runner client
    // (lásd ORCH-074 testGuard.ts mintát a secret ellenőrzésre)
    
    // Minimális seed: 1 DoorOrder (InProgress state)
    // Response: { orders: 1, panelStocks: 0, suppliers: 0 }
    return { orders: 1, panelStocks: 0, suppliers: 0 };
  }
};
```

### Ismeretlen profil → 400

```typescript
if (!profiles[seedProfile]) {
  return res.status(400).json({
    error: 'Unknown seed profile',
    available: Object.keys(profiles)
  });
}
```

### Reset handler frissítés

```typescript
// A cleanup után (ha sikeres vagy graceful degradation):
const seededEntities = await profiles[seedProfile].seed(tenantId);

res.json({
  tenantId,
  seedProfile,
  resetAt: new Date().toISOString(),
  deletedCounts,
  seededEntities
});
```

## Tesztek (kötelező)

```typescript
// src/routes/test.route.test.ts — extend meglévő tesztek

// Új tesztek (≥3):
// - empty-v1 profil → seededEntities { orders:0, panelStocks:0, suppliers:0 }
// - doorstar-smoke-v1 profil → seededEntities.orders === 1
// - ismeretlen profil → 400 + available list
```

## DoD

- [ ] `pnpm build` (tsc) → 0 error
- [ ] `pnpm test` → mind zöld, ≥3 új teszt
- [ ] `empty-v1` → seededEntities all zeros
- [ ] `doorstar-smoke-v1` → 1 DoorOrder létrehozva
- [ ] ismeretlen profil → 400
- [ ] git commit + push (develop branch)

## Outbox

DONE: `mailbox/orchestrator/outbox/2026-04-16_076_seed-profiles-done.md`

## Skillek & Agentек

- `/senior-backend` — seed profile pattern, Keycloak Direct Access Grant token
- `/javascript-typescript-jest` — unit tesztek a seed logikára (mock axiosJoinery)
- Sub-agenteket nyugodtan indíts párhuzamosan
