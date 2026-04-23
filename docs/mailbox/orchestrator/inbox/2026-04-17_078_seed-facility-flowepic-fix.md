---
id: MSG-ORCH-078
from: root
to: orchestrator
type: task
priority: high
status: READ
ref: MSG-INFRA-139-BLOCKED
created: 2026-04-17
---

# ORCH-078 — `doorstar-cutting-ready-v1` seed: facility-first FlowEpic fix

## Root cause

Az INFRA-139 verifikáció során kiderült: a seed `POST http://127.0.0.1:5000/api/flow-epics` hívja → **404**, mert ilyen endpoint nem létezik a Kernelben.

**Helyes path:** `POST /api/facilities/{facilityId}/flow-epics`

A FlowEpic nem közvetlenül a tenantnál él, hanem egy Facility alatt. Facility nélkül nem hozható létre.

## Fix — seed logika módosítása

A `doorstar-cutting-ready-v1` és `doorstar-smoke-v1` profil seed logikájában cseréld le a FlowEpic creation lépést:

### 1. Facility létrehozása (ÚJ lépés a seed elején)

```typescript
// Tenant facility létrehozása (vagy meglévő keresése)
const facilityRes = await kernelAxios.post(
  `/api/tenants/${tenantId}/facilities`,
  { name: 'Doorstar Gyártó' }
);
const facilityId = facilityRes.data.id ?? facilityRes.data;
```

### 2. FlowEpic a facility alatt (URL csere)

```typescript
// VOLT: POST /api/flow-epics  ← nem létezik
// ÚJ:
const epicRes = await kernelAxios.post(
  `/api/facilities/${facilityId}/flow-epics`,
  { title: 'Doorstar Seed Rendelés' }
);
const epicId = epicRes.data.id ?? epicRes.data;
```

### 3. tradeType eltávolítás

A `CreateFlowEpicRequest` csak `title`-t fogad — a `tradeType` mezőt távolítsd el a requestből ha jelen van.

## Érintett profilok

- `doorstar-cutting-ready-v1` ← elsődleges
- `doorstar-smoke-v1` ← ha ugyanezt a hibát tartalmazza, javítsd egyszerre

## Kernel endpoint referencia (megerősítve)

```
POST /api/tenants/{tenantId}/facilities     ← facility létrehozás
POST /api/facilities/{facilityId}/flow-epics ← epic létrehozás
```

(Forrás: e2e/src/chain/13-flowepic-full.chain.test.ts + FacilityEndpoints.cs)

## DoD

- [ ] `pnpm test` → 218+ zöld (regresszió nincs)
- [ ] Helyi tesztelés: seed hívás sorozat 404 nélkül lefut
- [ ] git commit + push (develop)

**Deploy:** az INFRA-139 re-verifikációhoz az ORCH-077-es deploy megvan (4497f45) — az ORCH-078 commit-ot külön deploy kell (INFRA-141 vagy az infra re-deployolja).

## Outbox

DONE: `mailbox/orchestrator/outbox/2026-04-17_078_seed-facility-flowepic-fix-done.md`

## Skillek & Agentек

- `/senior-backend` — seed profil URL fix, Kernel endpoint research
- Sub-agenteket nyugodtan indíts
