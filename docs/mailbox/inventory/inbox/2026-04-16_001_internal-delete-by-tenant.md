---
id: MSG-INVENTORY-001
from: root
to: inventory
type: task
priority: high
status: READ
ref: SpaceOS_Doorstar_Portal_UI_Test_Strategy_v4
created: 2026-04-16
---

# INVENTORY-001 — BE-TEST-05: `DELETE /internal/panel-stocks/by-tenant/{tenantId}`

## Kontextus

Ref: `docs/tasks/active/FE-TEST-STRATEGY_doorstar-portal-test-infra.md` §6.5
Párhuzamos task. Első feladat ebbe a mailboxba.

A Test BFF reset az Orchestrator-ból hívja ezt az endpointot.

## Endpoint

```
DELETE /internal/panel-stocks/by-tenant/{tenantId}?confirm=true
Headers: X-SpaceOS-Internal: true
Response 200: { tenantId, deletedCounts: { panelStocks, offcuts, stockMovements } }
Response 400: confirm=true hiánya
Response 403: header hiánya VAGY tenant nem allowlistban
```

## Security (ugyanaz mint JOINERY-010 — másolható pattern)

1. `X-SpaceOS-Internal: true` header kötelező → hiány: 403
2. `?confirm=true` kötelező → hiány: 400
3. GUID formátum ellenőrzés → invalid: 400
4. `TEST_TENANT_ALLOWLIST` env var allowlist lookup → nincs benne: 403 + LogWarning

## Implementáció

Kövesd a JOINERY-010 mintát. CLAUDE.md: WD = `/opt/spaceos/spaceos-modules-inventory/`.
Törölt entitások (sorrendben, FK miatt): `StockMovements` → `Offcuts` → `PanelStocks` (tenant szerint).

## Tesztek (kötelező)

- Header hiánya → 403
- confirm hiánya → 400
- Tenant nem allowlistban → 403
- Sikeres törlés → 200 + counts
- **≥4 új teszt**

## DoD

- [ ] `dotnet build` → 0 error
- [ ] `dotnet test` → ≥4 új teszt zöld
- [ ] 4 réteg security implementálva
- [ ] git commit + push

## Outbox

DONE: `mailbox/inventory/outbox/2026-04-16_001_internal-delete-by-tenant-done.md`

## Skillек & Agentек

- `/senior-backend` — Minimal API, FK delete sorrend (StockMovements→Offcuts→PanelStocks)
- Agent: `se-security-reviewer` — allowlist, defense in depth
- Sub-agenteket nyugodtan indíts
