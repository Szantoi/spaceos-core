---
id: MSG-CUTTING-048
from: root
to: cutting
type: task
priority: high
status: READ
ref: SpaceOS_Modules_Cutting_Phase4_Execution_Architecture_v4.md
created: 2026-04-27
---

# CUTTING-048 — Phase 4 Track A: Domain + Application (Nap 1–13)

> **Tervdok:** `docs/tasks/new/SpaceOS_Modules_Cutting_Phase4_Execution_Architecture_v4.md` — KÖTELEZŐ olvasmány!
> **README:** `docs/tasks/new/PHASE_4_README.md` — agent context
> **Skill:** `/spaceos-terminal` szerint dolgozz
> **Előfeltétel:** Cutting Phase 3 DEPLOYED (303 teszt)
> **Párhuzamosan fut:** KERNEL-103 (Outbox Extension) — a domain ettől független
> **Használhatsz sub-agent-eket** ha szükséges

---

## Scope

### Új projekt: `SpaceOS.Modules.Cutting.Execution.Domain`

**CuttingExecution aggregate** (spec §4):
- 7-state FSM: Scheduled → Started → InProgress → Completed / Cancelled / Failed
- `Schedule()`, `Start(workerId, badgeHmac)`, `RecordProgress(panelEvent)`, `RecordOffcut(offcutEvent)`, `Complete(proof)`, `Cancel(reason)`, `EvaluateMilestones(predicates)`
- Result<T> mindenhol (nem throw!)
- Domain events: ExecutionScheduled, Started, ProgressRecorded, OffcutRecorded, Completed, Cancelled, MilestoneReached

**Value Objects** (spec §4.2–4.5):
- `WorkerEventHmac` — SEC-02: FixedTimeEquals
- `CompletionProof` — 3 level: HashOnly, SignedEvidence, PhotoEvidence
- `MilestoneStatus` — Pending/Met/Expired
- `ProgressEvent` — panel-szintű, UUID v7 EventId (A4-7 idempotens)
- `OffcutEvent` — atomic Cutting+Inventory (A4-13)
- `WorkerAssignment` — workerId + enrollmentId
- `ScheduleWindow` — start/end timestamps
- `ExecutionKey` — AES-256 per-execution (A4-9 crypto-shredding)

**IMilestonePredicate** (spec §5):
- `IPanelCompletionPredicate` — 7/7 panel kész?
- `ITimeWindowPredicate` — schedule window-n belül?
- `IQualityCheckPredicate` — offcut ratio elfogadható?
- `IWorkerConsentPredicate` — worker consent aktív?

**Phase 3 CuttingExecution stub:** `[Obsolete]` attribútum + komment

### Új projekt: `SpaceOS.Modules.Cutting.Execution.Application`

- 8 command handler (MediatR): Schedule, Start, RecordProgress, RecordOffcut, Complete, Cancel, EvaluateMilestones, WithdrawWorkerConsent
- 6 query handler: GetExecution, ListExecutions, GetProgress, GetMilestones, GetCompletionProof, GetWorkerConsent
- 5 event handler: ProgressRecorded→SignalR, Completed→HashChain, MilestoneReached→StageRegistry
- FluentValidation validators (BE-A07: shape validation, domain rule a Result<T>-ben)
- ConfigureAwait(false) minden async-ban (BE-A03)
- 9 Specification (BE-A03): ExecutionsBySheetSpec, ExecutionsByWorkerSpec, PendingMilestonesSpec, stb.

---

## Tesztek (95+)

A spec §9.1–9.5 alapján — domain unit tesztek.

## Definition of Done

- [ ] CuttingExecution aggregate + 7-state FSM + Result<T>
- [ ] 8 VO + 4 IMilestonePredicate + 3 Policy interface
- [ ] 12 domain event
- [ ] 8 command + 6 query + 5 event handler
- [ ] 9 Specification (Ardalis.Specification)
- [ ] FluentValidation + ConfigureAwait(false)
- [ ] Phase 3 stub `[Obsolete]`
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` ≥ 398 pass (303 előző + 95 új)
- [ ] Outbox DONE
