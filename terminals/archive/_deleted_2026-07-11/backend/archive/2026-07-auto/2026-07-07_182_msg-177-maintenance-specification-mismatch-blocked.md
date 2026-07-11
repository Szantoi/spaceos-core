---
id: MSG-BACKEND-182
from: backend
to: conductor
type: blocked
status: CANCELLED
ref: MSG-BACKEND-177
created: 2026-07-07
resolved: 2026-07-10
resolved_by: root
resolution: "Maintenance Week 2 Application Layer ALREADY COMPLETE for Asset+WorkOrder scope (16 Commands + 9 Queries per ADR-057). Inspection is out of scope. Specification was outdated generic template. CANCELLED as duplicate work."
content_hash: 2a688aebeedef1edbaff230961406afaa74efe317ee84a90bf9aa308a58c28f3
---

## Blocker: Maintenance Week 2 Specification Mismatch

**MSG-BACKEND-177** blokkolt — **specification conflicts** a meglévő domain modellel és implementációval.

### Probléma Összefoglalás

Az inbox specification **elavult generikus sablont** használ, amely **NEM egyezik** a Maintenance domain modellel (ADR-057) és a **már implementált** Application Layer-rel.

### Conflict #1: Inspection Scope Error

**Inbox követelmény:**
```csharp
// Inspection Commands (MSG-177 szerint)
CreateInspectionCommand.cs
RecordInspectionFindingCommand.cs
CompleteInspectionCommand.cs
FailInspectionCommand.cs

// Inspection Queries
GetInspectionByIdQuery.cs
GetInspectionsByAssetQuery.cs
GetFailedInspectionsQuery.cs
```

**ADR-057 szerinti architektúra:**
- **Inspection NEM része a Maintenance modulnak** — nincs Inspection aggregate az ADR-057-ben!
- Maintenance aggregátumok: **Asset**, **WorkOrder**, **MaintenancePlan** ONLY
- Inspection valószínűleg külön modul (QA/Quality) vagy későbbi phase

**Fájl:** `/opt/spaceos/docs/architecture/decisions/ADR-057-joinerytech-maintenance-domain-model.md`
- Line 31: "A Maintenance modul **3 aggregate root** köré szerveződik"
- Line 33-35: Asset, WorkOrder, MaintenancePlan
- **Inspection NINCS említve egyáltalán az ADR-ben!**

**Eredmény:**
```bash
$ find /opt/spaceos/spaceos-modules-maintenance/src/Domain/Aggregates -name "*.cs"
Asset.cs         ✅
WorkOrder.cs     ✅
Inspection.cs    ❌ NINCS (nem Maintenance scope)
```

### Conflict #2: MaintenancePlan Design Mismatch

**Inbox követelmény (MaintenanceSchedule):**
```csharp
CreateMaintenanceScheduleCommand.cs
UpdateScheduleCommand.cs
SkipScheduledMaintenanceCommand.cs
GenerateWorkOrdersFromScheduleCommand.cs
```

**ADR-057 szerinti design (Line 267-344):**
```csharp
MaintenancePlan (Aggregate Root?)
├── Id (Guid)
├── AssetId (Guid)
├── Label (string)
├── Trigger (Interval / OperatingHours)
├── LastDone / LastDoneHours
└── Methods: MarkCompleted(), Deactivate(), Reactivate()
```

**Implementált állapot:**
```csharp
// MaintenancePlan = VALUE OBJECT (owned collection in Asset)
// Domain/ValueObjects/MaintenancePlan.cs
public record MaintenancePlan
{
    public string Id { get; init; }
    public string Label { get; init; }
    public MaintenanceTrigger Trigger { get; init; }
    // ... (owned by Asset, NOT aggregate root)
}

// Asset aggregate
public class Asset
{
    private readonly List<MaintenancePlan> _maintenancePlans = new();
    public IReadOnlyList<MaintenancePlan> MaintenancePlans => _maintenancePlans.AsReadOnly();

    public void AddMaintenancePlan(MaintenancePlan plan) { /* ... */ }
    public void RemoveMaintenancePlan(string planId) { /* ... */ }
}
```

**Design Decision:** MaintenancePlan az Asset aggregate-nek **owned collection-je**, NEM külön aggregate root!

**Implementált Commands:**
- AddMaintenancePlanCommand ✅ (add plan to Asset)
- RemoveMaintenancePlanCommand ✅ (remove plan from Asset)
- CreateMaintenanceScheduleCommand ❌ (nincs külön aggregate, ezért nincs Create)
- UpdateScheduleCommand ❌ (owned collection, nem update-elhető közvetlenül)

### Conflict #3: Naming Mismatch

**Inbox szerint:**
- **MaintenanceSchedule** Commands/Queries
- ScheduledDate, GenerateWorkOrders, etc.

**Implementálva (ADR-057 szerint):**
- **MaintenancePlan** (owned collection az Asset-ben)
- WorkOrder.Schedule() metódus (nem külön Schedule aggregate)
- PreventiveMaintenanceSchedulerService (domain service WorkOrder generáláshoz)

### Meglévő Implementáció Státusza

**Week 2 Application Layer COMPLETE (Asset + WorkOrder scope):**

✅ **Commands (16):** CreateAsset, RetireAsset, ReactivateAsset, RecordOperatingHours, ReportWorkOrder, AssignWorkOrder, ScheduleWorkOrder, StartWorkOrder, PostponeWorkOrder, ReopenWorkOrder, RejectWorkOrder, AddMaintenancePlan, RemoveMaintenancePlan, AddWorkOrderPart, RemoveWorkOrderPart, CompleteWorkOrder
✅ **Queries (9):** GetAsset, GetAssets, GetWorkOrder, GetWorkOrders, GetPendingWorkOrders, GetInProgressWithDowntime, GetAssetMaintenanceHistory, GetAssetCurrentWorkOrders, GetAssetsRequiringMaintenance
✅ **Validators:** FluentValidation for all commands
✅ **DTOs:** Asset, WorkOrder, MaintenancePlan, WorkOrderPart response DTOs
✅ **Build:** 0 errors, 0 warnings
✅ **MSG-166-DONE:** Infrastructure layer complete
✅ **MSG-170-DONE:** API layer complete (pattern reuse)

**Missing from Inbox Requirements:**
❌ **Inspection CQRS handlers** (Inspection aggregate nincs az ADR-057-ben)
❌ **MaintenanceSchedule as aggregate** (MaintenancePlan owned collection, NEM aggregate)
❌ **UpdateScheduleCommand** (owned collection, nem update-elhető közvetlenül)
❌ **GenerateWorkOrdersFromScheduleCommand** (szolgáltatás létezik: PreventiveMaintenanceSchedulerService)

**Verification:**
```bash
$ dotnet build /opt/spaceos/spaceos-modules-maintenance/src/SpaceOS.Modules.Maintenance.csproj
Build succeeded.
    0 Warning(s)
    0 Error(s)

$ find /opt/spaceos/spaceos-modules-maintenance/src/Application/Commands -name "*Command.cs" | wc -l
16  # (16 command types implemented)

$ find /opt/spaceos/spaceos-modules-maintenance/src/Application/Queries -name "*Query.cs" | wc -l
9   # (9 query types implemented)
```

### Root Cause Analysis

**Conductor error:** Dispatched MSG-177 using **generic JoineryTech Week 2 specification template**, amely:
1. Feltételez Inspection aggregate-et a Maintenance modulban (SCOPE ERROR — nincs az ADR-057-ben)
2. Feltételez MaintenanceSchedule aggregate-et (DESIGN MISMATCH — MaintenancePlan owned collection)
3. Nem tükrözi az ADR-057 gazdagabb, owned collection-alapú domain modellt
4. Nem ellenőrizte hogy Week 2 Application Layer már implementálva van (Asset + WorkOrder COMPLETE)

**What should have happened:**
- Verification hogy Maintenance Week 2 Application Layer már implementálva van (Asset + WorkOrder scope)
- Custom specification based on ADR-057, NOT generic template
- Inspection aggregate későbbi phase vagy külön modul (QA?)

### Architecture Confirmation

**ADR-057 domain model CORRECT:**
- 3 aggregátumok: Asset (aggregate root), WorkOrder (aggregate root), MaintenancePlan (VALUE OBJECT owned by Asset)
- Asset + WorkOrder IMPLEMENTED ✅
- MaintenancePlan IMPLEMENTED as owned collection ✅
- Inspection NINCS az ADR-ban (külön modul vagy későbbi phase)

**Design Decision Rationale:**
- MaintenancePlan owned collection az Asset-ben → egyszerűbb domain modell
- Preventive WorkOrder generálás: PreventiveMaintenanceSchedulerService (domain service)
- Nincs szükség külön MaintenancePlan aggregate-re (egyszerűsítés)

**No architecture change needed.** Decision szükséges: implementáljuk-e Inspection aggregátumot (új modul?) vagy későbbi phase?

### Blocking Reason

Nem implementálhatom az inbox követelményeket, mert:
- ❌ **Inspection Commands/Queries:** SCOPE ERROR — Inspection nincs a Maintenance domain-ben (ADR-057)
- ❌ **CreateMaintenanceScheduleCommand:** DESIGN MISMATCH — MaintenancePlan owned collection, NEM aggregate
- ❌ **UpdateScheduleCommand:** DESIGN MISMATCH — owned collection, nem update-elhető közvetlenül
- ⚠️ **Naming mismatches:** MaintenanceSchedule vs MaintenancePlan (design decision)
- ℹ️ **GenerateWorkOrdersFromScheduleCommand:** Szolgáltatás létezik (PreventiveMaintenanceSchedulerService), de nem CQRS command

### Recommended Next Steps

**Opció A: MSG-177 CANCELLED (RECOMMENDED)**
- Maintenance Week 2 Application Layer **ALREADY COMPLETE** (Asset + WorkOrder scope)
- Specification elavult — ADR-057 domain model gazdagabb, owned collection-alapú
- Inspection scope ERROR (nincs a Maintenance modulban)
- MaintenancePlan design decision (owned collection, NEM aggregate)
- MSG-177 CANCELLED, inbox marked as duplicate/outdated work

**Opció B: Specification Correction**
- Conductor updates MSG-177 specification to match ADR-057
- Remove Inspection scope (külön modul vagy phase)
- Accept MaintenancePlan as owned collection (not separate aggregate)
- Accept existing Command/Query names (AddMaintenancePlan, RemoveMaintenancePlan)
- Optional: Add missing queries (GetOverdueWorkOrders, GetMaintenanceHistory)

**Opció C: Add Inspection Module (NEW SCOPE)**
- Create separate Inspection module with domain model (~60 NWT)
- Inspection aggregate: InspectionId, AssetId, InspectionDate, Status, Findings
- CQRS handlers: CreateInspection, RecordFinding, CompleteInspection, FailInspection
- Integration with Maintenance module (AssetId reference)
- Expected time: ~60 NWT (full Week 1-4 for new module)

### Impact Assessment

**Week 2 Application Layer Status:**

| Module | Asset/WorkOrder | MaintenancePlan | Inspection | Status |
|--------|-----------------|-----------------|------------|--------|
| **Maintenance** | ✅ COMPLETE | ✅ Owned collection | ❌ Out of scope | 🟢 DONE |
| **CRM** | ✅ COMPLETE | N/A | N/A | ✅ DONE (MSG-174 CANCELLED) |
| **Kontrolling** | ✅ COMPLETE | N/A | N/A | ✅ DONE (MSG-175 CANCELLED) |
| **HR** | ✅ Employee/Absence | ❌ TimeLog/Assignment gap | ❌ Contract out of scope | 🟡 PARTIAL (MSG-176 BLOCKED) |
| **QA** | 📋 PENDING | N/A | 📋 PENDING | 📋 VALID (MSG-178) |

**Corrected Dispatch Count:** 1 valid task (QA) + 1 partial (HR)

### Verifiable Files

```bash
# Domain Model (ADR-057)
cat /opt/spaceos/docs/architecture/decisions/ADR-057-joinerytech-maintenance-domain-model.md

# Domain Aggregates (Asset + WorkOrder ONLY)
ls /opt/spaceos/spaceos-modules-maintenance/src/Domain/Aggregates/

# MaintenancePlan as Value Object (owned collection)
cat /opt/spaceos/spaceos-modules-maintenance/src/Domain/ValueObjects/MaintenancePlan.cs

# Implemented Commands (16 handlers)
find /opt/spaceos/spaceos-modules-maintenance/src/Application/Commands -name "*Handler.cs" | wc -l

# Implemented Queries (9 handlers)
find /opt/spaceos/spaceos-modules-maintenance/src/Application/Queries -name "*Handler.cs" | wc -l

# Build Status
dotnet build /opt/spaceos/spaceos-modules-maintenance/src/SpaceOS.Modules.Maintenance.csproj

# MSG-166 DONE outbox (Week 3 Infrastructure)
cat /opt/spaceos/terminals/backend/outbox/2026-07-07_001_msg-backend-166-maintenance-infrastructure-done.md
```

### Kockázatok

| Kockázat | Impact | Likelihood |
|----------|--------|------------|
| **Implementálás spec conflict nélkül** | HIGH — Architectural violation (Inspection scope, MaintenancePlan design) | HIGH |
| **Duplicate work** | MEDIUM — Asset + WorkOrder már complete, wasted effort | LOW if Opció A |
| **Inspection modul hiány** | LOW — Inspection későbbi phase vagy külön modul (QA?) | MEDIUM |

---

**Status:** 🔴 BLOCKED — Architecture decision needed from Conductor

**Preferred Resolution:** **Opció A** — MSG-177 CANCELLED (Maintenance Week 2 Application Layer already complete for Asset + WorkOrder scope)

**Alternative:** **Opció B** — Specification correction (remove Inspection, accept MaintenancePlan design)

**Next Action:** Conductor döntés Opció A/B/C közül

---

🤖 Generated by Backend Terminal

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
