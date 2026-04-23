---
id: MSG-ORCH-060
from: root
to: orchestrator
type: task
priority: critical
status: READ
ref: MSG-E2E-014
created: 2026-04-14
---

# MSG-ORCH-060 — proof.route.ts path fix (`/api/tasks/` → `/api/flow-epics/`)

## Kontextus

Az E2E-014 (36-proof.chain.test.ts) a Doorstar Q2 proof chain-t teszteli.
Diagnosztika során kiderült: a `proof.route.ts` rossz Kernel útvonalra proxyzik.

## Bug

```typescript
// proof.route.ts:46 — JELENLEG (hibás):
`${env.KERNEL_BASE_URL}/api/tasks/${taskId}/proof`

// A Kernel FlowEpicEndpoints.cs-ben ez a valódi endpoint:
POST /api/flow-epics/:id/proof
```

A Kernelben **nincs `/api/tasks/*` route** — minden proof upload 404-et kap.

## Feladat

Javítsd a `proof.route.ts` fájlt:

**Fájl:** `src/routes/proof.route.ts`

Változtatások:
1. **Line 4 komment:** `/bff/tasks/:taskId/proof → POST /api/tasks/:taskId/proof`
   → `/bff/tasks/:taskId/proof → POST /api/flow-epics/:taskId/proof`
2. **Line 21 komment:** ugyanaz
3. **Line 46 axios URL:** `/api/tasks/${taskId}/proof`
   → `/api/flow-epics/${taskId}/proof`

## Tesztek

- `proof.route.test.ts` létezik — frissítsd az URL mockokat ha szükséges
- `npm run build` → 0 TS error
- `npm test` → minden teszt zöld

## Definition of Done

- [ ] `proof.route.ts` javítva (3 helyen)
- [ ] `proof.route.test.ts` zöld
- [ ] 183 meglévő teszt változatlan
- [ ] DONE outbox: `MSG-ORCH-060-DONE`

## Visszajelzés

Outboxba: `MSG-ORCH-060-DONE`

## Megjegyzés

Deploy (INFRA-071) a DONE outbox elfogadása után következik — nem kell megvárnod,
a commit + DONE elég.
