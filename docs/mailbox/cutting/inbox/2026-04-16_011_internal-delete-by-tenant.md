---
id: MSG-CUTTING-011
from: root
to: cutting
type: task
priority: high
status: READ
ref: SpaceOS_Doorstar_Portal_UI_Test_Strategy_v4
created: 2026-04-16
---

# CUTTING-011 — BE-TEST-04: `DELETE /internal/cutting-sheets/by-tenant/{tenantId}`

## Kontextus

Ref: `docs/tasks/active/FE-TEST-STRATEGY_doorstar-portal-test-infra.md` §6.5
Párhuzamos KERNEL-085 és JOINERY-010-zel (JOINERY már DONE, KERNEL fut).

A Test BFF reset az Orchestrator-ból hívja ezt az endpointot a test tenant adatainak törlésére.

## Endpoint

```
DELETE /internal/cutting-sheets/by-tenant/{tenantId}?confirm=true
Headers: X-SpaceOS-Internal: true
Response 200: { tenantId, deletedCounts: { cuttingSheets, dailyCuttingPlans } }
Response 400: confirm=true hiánya
Response 403: header hiánya VAGY tenant nem allowlistban
```

## Security (ugyanaz mint JOINERY-010 — másolható pattern)

1. `X-SpaceOS-Internal: true` header kötelező → hiány: 403
2. `?confirm=true` kötelező → hiány: 400
3. GUID formátum ellenőrzés → invalid: 400
4. `TEST_TENANT_ALLOWLIST` env var allowlist lookup → nincs benne: 403 + LogWarning

## Implementáció

Kövesd a JOINERY-010 mintát (`InternalEndpoints.cs` + repository extension).
Törölt entitások: `DailyCuttingPlans` (tenant szerint), `NestingResults` (ha van), `CuttingSheets` (tenant szerint).

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

DONE: `mailbox/cutting/outbox/2026-04-16_011_internal-delete-by-tenant-done.md`

## Skillек & Agentек

- `/senior-backend` — Minimal API, pattern: JOINERY-010 `InternalEndpoints.cs` másolható
- Agent: `se-security-reviewer` — defense in depth, allowlist
- Sub-agenteket nyugodtan indíts
