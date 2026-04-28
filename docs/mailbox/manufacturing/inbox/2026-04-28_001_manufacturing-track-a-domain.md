---
id: MSG-MFG-001
from: root
to: manufacturing
type: task
priority: high
status: UNREAD
ref: SpaceOS_Modules_Manufacturing_Phase1_Architecture_v4.md
created: 2026-04-28
---

# MFG-001 — Manufacturing Phase 1 Track A: Domain + Application (Day 1–17)

> **Tervdok:** `docs/tasks/active/SpaceOS_Modules_Manufacturing_Phase1_Architecture_v4.md` — KÖTELEZŐ olvasmány!
> **README:** `docs/tasks/active/SpaceOS_Modules_Manufacturing_Phase1_Architecture_v4_README.md`
> **Skill:** `/spaceos-terminal` szerint dolgozz
> **Repo:** `/opt/spaceos/spaceos-modules-manufacturing/`
> **Port:** 5007 (NEM 5006 — az Procurement!)
> **Használhatsz sub-agent-eket** ha szükséges

---

## Scope

### Solution scaffold (Day 1)

```bash
dotnet new sln -n SpaceOS.Modules.Manufacturing
```

5 projekt (Domain, Application, Infrastructure, Api, Tests) + Directory.Build.props + .gitignore

### Domain (Day 2–10)

**3 aggregate:**
- `ManufacturingOrder` — saga koordinátor, counter columns (pending/completed EdgeBanding + CNC)
- `EdgeBandingTask` — FSM (Scheduled→InProgress→Completed/Failed), line items
- `CncTask` — FSM, PlanIntegrityHash (SEC-07)

**Value Objects:** TaskPriority, MaterialSpec, EdgeBandSpec, CncOperationRef, PlanIntegrityHash

**Domain Events:** OrderCreated, TaskScheduled, TaskStarted, TaskCompleted, TaskFailed, OrderCompleted

**Specifications (10+):** Ardalis.Specification, AsSplitQuery

**Inbox processor interface:** `ICuttingPanelCompletedInboxProcessor`

### Application (Day 11–17)

- 12 command handler (MediatR): CreateOrder, ScheduleEdgeBanding, StartEdgeBanding, CompleteEdgeBanding, ScheduleCnc, StartCnc, CompleteCnc, FailTask, CancelOrder, ProcessInboxEvent, etc.
- 6 query handler
- FluentValidation (shape-only, BE-07)
- ConfigureAwait(false)
- ResultExtensions (BE-06)

---

## Tesztek (100+)

**Domain (50+):** FSM transitions, saga counter, PlanIntegrityHash, specifications
**Application (50+):** handler success/failure, inbox processing, validation

## Definition of Done

- [ ] Solution scaffold (5 projekt, port 5007)
- [ ] 3 aggregate (ManufacturingOrder, EdgeBandingTask, CncTask)
- [ ] 12+ command handler + 6 query handler
- [ ] Inbox processor interface
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` ≥ 100 pass
- [ ] Outbox DONE
