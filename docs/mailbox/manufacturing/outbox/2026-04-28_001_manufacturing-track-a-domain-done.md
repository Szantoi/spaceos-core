---
id: MSG-MFG-OUT-001
from: manufacturing
to: root
type: done
priority: high
status: READ
ref: MSG-MFG-001
created: 2026-04-28
---

# Manufacturing Phase 1 Track A — DONE

## Összefoglaló

Track A (Scaffold + Domain + Application) teljes egészében implementálva és tesztelve.

## Build eredmény

```
dotnet build --warnaserror
→ Build succeeded. 0 Warning(s), 0 Error(s)
```

## Teszt eredmény

```
dotnet test
→ Passed! Failed: 0, Passed: 134, Skipped: 0, Total: 134
```

## DoD ellenőrzőlista

- [x] **Solution scaffold** — 5 projekt (Domain, Application, Infrastructure, Api, Tests), port 5007
- [x] **3 aggregate** — ManufacturingOrder, EdgeBandingTask, CncTask (teljes FSM)
- [x] **10+ value object** — ProcessStepSequence, TaskPriority, EdgeBandingMaterialSpec, CncOperationSpec, CncPlanIntegrityHash, WorkerAssignment, MachineAssignment, WorkerEventHmac, CompletionProof, CncFailureDetails, SignedCncPlan
- [x] **13 domain event** — ManufacturingOrderCreated, Cancelled, Completed, Failed + EdgeBanding(Scheduled/Started/Completed/Failed/Cancelled) + Cnc(Scheduled/Started/Completed/Failed)
- [x] **10 Ardalis.Specification spec** — GetOrderByIdSpec, GetOrdersByTenantSpec, GetOrderForUpdateSpec, GetOrderForCounterAdvanceSpec, GetEdgeBandingTaskByIdSpec, GetEdgeBandingTasksByOrderSpec, GetPendingEdgeBandingTasksByMachineSpec, GetEdgeBandingTasksByWorkerSpec, GetCncTaskByIdSpec, GetCncTasksByOrderSpec
- [x] **4 policy interface** — IEdgeBandingSchedulingPolicy, IWorkCellAssignmentPolicy, ICncPlanVerificationPolicy, IManufacturingOrderCancellationPolicy
- [x] **3 repository interface** — IManufacturingOrderRepository, IEdgeBandingTaskRepository, ICncTaskRepository
- [x] **12 command handler** — CreateManufacturingOrder, CancelManufacturingOrder, ScheduleEdgeBandingTask, StartEdgeBandingTask, CompleteEdgeBandingTask, FailEdgeBandingTask, CancelEdgeBandingTask, ScheduleCncTask, StartCncTask, CompleteCncTask, FailCncTask, ProcessInboxEvent
- [x] **6 query handler** — GetManufacturingOrderById, ListActiveOrdersByTenant, GetEdgeBandingTasksByOrder, GetCncTasksByOrder, GetPendingEdgeBandingTasksByMachine, GetWorkerEdgeBandingTaskQueue
- [x] **4 saga event handler** — OnPanelEdgeBandingCompleted, OnPanelEdgeBandingFailed, OnPanelCncCompleted, OnPanelCncFailed (BE-08 direct call, nem MediatR INotificationHandler)
- [x] **ResultExtensions** — ToOk(), ToCreated() type alias alapú IResult disambiguation (BE-06)
- [x] **IAuditLogger interface** — Application rétegben definiálva
- [x] **IInboxEventProcessor interface** — ProcessInboxEventCommand handler implementálja
- [x] **SEC-07 CNC plan integrity** — SHA-256 hash verification a CncTask.Schedule()-ban
- [x] **TreatWarningsAsErrors** — Directory.Build.props-ban beállítva

## Tesztek bontása

| Kategória | Tesztek |
|---|---|
| Domain — ManufacturingOrder FSM | ~25 |
| Domain — EdgeBandingTask FSM | ~22 |
| Domain — CncTask FSM + SEC-07 hash | ~20 |
| Domain — Value Objects | ~20 |
| Application — Validators | ~20 |
| Application — Command handlers (NSubstitute) | ~10 |
| Application — Saga event handlers | ~10 |
| Application — ResultExtensions | ~7 |
| **Összesen** | **134** |

## Megjegyzések

- Infrastructure projekt stub szinten van (EF Core DbContext alapvázat tartalmaz) — Track B feladata lesz a teljes persistence implementáció
- Api projekt minimal stub (Program.cs) — Track B/C során bővül endpoint-okkal
- BE-08 döntés: saga handlerek plain C# service-ek, nem INotificationHandler, mert az IDomainEvent nem implementál MediatR.INotification (Domain rétegtől MediatR-t tartjuk távol)
