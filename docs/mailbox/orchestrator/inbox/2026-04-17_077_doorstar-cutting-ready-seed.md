---
id: MSG-ORCH-077
from: root
to: orchestrator
type: task
priority: high
status: READ
ref: FE-TEST-STRATEGY_doorstar-portal-test-infra
created: 2026-04-17
---

# ORCH-077 — `doorstar-cutting-ready-v1` seed profil implementáció

## Kontextus

Az ORCH-076-ban elkészült `empty-v1` és `doorstar-smoke-v1` profilok. Az E2E-L2 Playwright flow-khoz (02–07) egy részletesebb seed profil kell, amely valós UI-ra alkalmas adatot teremt:
- 1 DoorOrder (Submitted állapotban)
- 1 CuttingSheet (Received állapotban, a DoorOrder-hez kötve)
- 5 PanelStock (különböző méretű panelek)
- 1 Supplier (Faanyag Kft.)

## Feladat

Az `src/seed/` (vagy ahol az ORCH-076 seed profileok élnek) mappában add hozzá a `doorstar-cutting-ready-v1` profilt.

### Elvárt seededEntities response

```json
{
  "tenantId": "...",
  "seedProfile": "doorstar-cutting-ready-v1",
  "resetAt": "...",
  "seededEntities": {
    "orders": 1,
    "cuttingSheets": 1,
    "panelStocks": 5,
    "suppliers": 1
  }
}
```

### Seed logika elvárások

1. **Kernel FlowEpic + DoorOrder:** Hívd a Kernel `POST /internal/flow-epics` (vagy meglévő seed endpoint) a test tenant JWT-jével. A DoorOrder legyen `Submitted` állapotban.

2. **CuttingSheet:** Hívd a Cutting modul `POST /internal/cutting-sheets` (ha van) vagy direkt a modul seed endpointját. Status: `Received`. Kösd az 1-es DoorOrder-hez.

3. **PanelStock:** 5 panel (pl.: 2440×1220mm, 2 db; 1830×610mm, 3 db) — Inventory módulban.

4. **Supplier:** 1 supplier ("Faanyag Kft.") — Procurement modulban.

**Fontos:** Ha a moduloknak nincs közvetlen seed endpointjuk, az ORCH-076-ban megvalósított reset pipeline már tud törölni — de a seeding részét implementálnod kell. Használd a belső `X-SpaceOS-Internal: true` headeres endpointokat ahol elérhetők, vagy a test tenant JWT-vel hívd az API-kat közvetlenül.

## Technikai kontextus

Az ORCH reset route (`/bff/test/tenants/:id/reset`) már éles. Az ORCH-076 commitja: `e500a4f`. A seed profilok az Orchestratorban vannak implementálva.

Az aktuális reset endpoint struktúra (INFRA-137 verif):
```json
{
  "tenantId": "...",
  "seedProfile": "empty-v1",
  "resetAt": "...",
  "deletedCounts": { ... },
  "seededEntities": {"orders": 0, "panelStocks": 0, "suppliers": 0}
}
```

A `seededEntities` mező már benne van — csak a `doorstar-cutting-ready-v1` logikát kell feltölteni.

## DoD

- [ ] `pnpm test` → 217+ zöld (regresszió nincs)
- [ ] `POST /bff/test/tenants/{TEST_TENANT_ID}/reset` + body `{"seedProfile":"doorstar-cutting-ready-v1"}` → HTTP 200, `seededEntities.orders=1, cuttingSheets=1, panelStocks=5, suppliers=1`
- [ ] Kétszeri hívás is helyes: reset törli az előző seed adatot, majd újra seedel
- [ ] git commit + push (develop)

## Outbox

DONE: `mailbox/orchestrator/outbox/2026-04-17_077_doorstar-cutting-ready-seed-done.md`

## Skillek & Agentек

- `/senior-backend` — seed profil logika, belső API hívások test context-ben
- Sub-agenteket nyugodtan indíts
