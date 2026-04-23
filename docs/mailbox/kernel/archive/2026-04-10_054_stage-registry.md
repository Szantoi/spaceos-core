---
id: MSG-KERNEL-054
from: root
to: kernel
type: task
priority: high
status: READ
created: 2026-04-10
---

# MSG-KERNEL-054 — Kernel Stage Registry (Workflow Stage Architecture v4)

## Feladat

Implementáld a Stage Registry infrastruktúrát a `SpaceOS.Kernel` projektben.

**Architektúra dokument:** `/opt/spaceos/docs/SpaceOS_WorkflowStage_Architecture_v4.md`
**Üzleti összefoglaló:** `/opt/spaceos/docs/SpaceOS_WorkflowStage_Summary.md`

Az agent utasítás a doc Section 11-ben van. Másold át szó szerint.

---

## Scope

### Domain (Nap 1–2)

- `StageDefinition` — `static Register()`, `UpdateEndpoint()`, `Deactivate()`, StageCode immutable (no public setter)
- `StageChainTemplate` — `Create()`, `AddStep(StageDefinition, ...)` (BE-04: stageCode from entity), `RemoveStep()`, max 20 lépés
- `StageChainStep` — internal factory, compound FK a `StageDefinitionId`-ra (DB-04)
- `StageHandoff` — `static Create()`, immutable, SHA-256 hash, `IdempotencyKey` (SEC-05), `HashAlgorithm` mező (SEC-07)
- `IStageChainValidator` + `StageChainValidator` — chain-sorrend + required stage skip tiltva (BE-01/SEC-03)
- **FlowEpic bővítés** — `AssignChain()`, `AdvanceToStage()` (validator hívja, nem a domain method!), `SkipOptionalStage()`
- 6 domain event: `StageDefinitionRegisteredEvent`, `StageDefinitionUpdatedEvent`, `StageChainCreatedEvent`, `StageHandoffCreatedEvent`, `FlowEpicStageAdvancedEvent`, `FlowEpicStageSkippedEvent`
- 4 Ardalis.Specification: `ActiveStageDefinitionsSpec`, `HandoffsByFlowEpicSpec`, `LatestHandoffSpec`, `ChainStepsByTemplateSpec`

### Infrastructure (Nap 3)

- EF Core config mind a 4 új entitáshoz (OwnsOne-ok, FK-k, index-ek)
- **Migration 0027** — teljes DDL a Section 4-ből:
  - 4 tábla: `StageDefinitions`, `StageChainTemplates`, `StageChainSteps`, `StageHandoffs`
  - `FlowEpics` ALTER: `CurrentStageCode`, `StageChainTemplateId` oszlopok + compound FK DEFERRABLE
  - Indexek (Section 4.2)
  - RLS FORCE mind a 4 táblán (Section 4.3)
  - Triggers: `prevent_stage_code_change` (DB-10) + `update_updated_at` (DB-06) (Section 4.4)
  - Doorstar seed (Section 4.5): 3 stage + 1 chain template + 3 step
- `StageChainValidator` implementáció (Infrastructure/Services/)

### Application (Nap 4–6)

CQRS command/query handlers + FluentValidation validators:

**Commands:**
- `RegisterStageDefinitionCommand` handler
- `UpdateStageDefinitionCommand` handler
- `DeactivateStageDefinitionCommand` handler
- `CreateStageChainTemplateCommand` handler
- `AddStageChainStepCommand` handler
- `RemoveStageChainStepCommand` handler
- `CreateStageHandoffCommand` handler — **advisory lock + explicit tx** (BE-02/DB-02/SEC-09), idempotency (SEC-05)
- `AssignChainCommand` handler
- `AdvanceFlowEpicStageCommand` handler — IStageChainValidator inject (BE-01/SEC-03)
- `SkipOptionalStageCommand` handler

**Queries:**
- `ListStageDefinitionsQuery` handler (ActiveStageDefinitionsSpec)
- `ListStageChainTemplatesQuery` handler
- `GetStageChainTemplateQuery` handler (with steps)
- `GetStageHandoffsQuery` handler (by flowEpicId)
- `GetLatestHandoffQuery` handler

**Validators (FluentValidation):**
- `CreateStageHandoffValidator` — PayloadJson max depth 10 (SEC-04) + size < 1MB (DB-05)
- Minden command-ra saját validator

### API (Nap 6)

15 Minimal API endpoint (Section 5):

```
# Stage Registry — SystemAdmin only
POST   /api/stages
GET    /api/stages
PUT    /api/stages/{id}
DELETE /api/stages/{id}

# Stage Chains — TenantAdmin
POST   /api/stage-chains
GET    /api/stage-chains
GET    /api/stage-chains/{id}
POST   /api/stage-chains/{id}/steps
DELETE /api/stage-chains/{id}/steps/{stageCode}

# Stage Handoffs — StageOperator
POST   /api/stage-handoffs
GET    /api/stage-handoffs?flowEpicId={id}
GET    /api/stage-handoffs/{id}
GET    /api/stage-handoffs/latest?flowEpicId&src&tgt

# FlowEpic Stage Control — StageOperator
POST   /api/flow-epics/{id}/assign-chain
POST   /api/flow-epics/{id}/advance-stage
POST   /api/flow-epics/{id}/skip-stage
```

RBAC: SystemAdmin / TenantAdmin / StageOperator / TenantUser (SEC-02)

### Tests (Nap 7–8)

≥45 új teszt — **Section 8 DoD kapuk szerint**:

**Domain tesztek (≥20):**
- StageChainValidator: sorrend, backward, required skip, optional skip, no chain assigned
- StageHandoff: hash determinisztikus, IdempotencyKey unique, immutable
- StageChainTemplate: max 20 steps guard, duplicate stageCode/sortOrder guard
- FlowEpic: AssignChain csak egyszer, AdvanceToStage domain event

**Security gate tesztek (≥25):**
- RLS: cross-tenant stage definition, cross-tenant handoff — blocked
- RBAC: non-SystemAdmin → RegisterStage 403; non-StageOperator → CreateHandoff 403
- SSRF: ModuleEndpoint port outside 5000-5099 → validation error
- Replay: duplicate IdempotencyKey → return existing (nem error)
- PayloadJson > 1MB → 400; depth > 10 → 400

---

## Kritikus szabályok

| ❌ Tilos | ✅ Helyes |
|---------|---------|
| `FlowEpic.AdvanceToStage()` chain-t validál | `IStageChainValidator` validál, utána `AdvanceToStage()` hívódik (BE-01) |
| User input-ból StageCode a StepCollection-be | `AddStep(StageDefinition stageDef, ...)` — stageDef.StageCode (BE-04) |
| Handoff create advisory lock nélkül | `pg_advisory_xact_lock` + explicit `BeginTransactionAsync` (BE-02/DB-02) |
| ModuleEndpoint bármilyen URL | CHECK: `^https?://(127\.0\.0\.1\|localhost):(50[0-9]{2})$` (SEC-01) |
| `BuildServiceProvider` | Constructor injection mindenhol |

---

## Definition of Done

Section 8 teljes DoD checklist: migration gates + domain gates + API+validation gates + security gates + összesített.

**Legfontosabb kapuk:**
- [ ] Migration 0027 fut, RLS FORCE aktív mind a 4 táblán
- [ ] Doorstar seed: 3 stage + 1 chain + 3 step
- [ ] IStageChainValidator injektálva az AdvanceFlowEpicStageCommandHandler-be
- [ ] CreateStageHandoff: advisory lock + explicit tx + idempotency
- [ ] 15 endpoint + RBAC
- [ ] ≥45 új teszt
- [ ] Meglévő 1557 teszt zöld
- [ ] 0 build warning

---

## Visszajelzés

Outboxba: `MSG-KERNEL-054-DONE` státusszal, tesztszámmal és migration checklisttel.
