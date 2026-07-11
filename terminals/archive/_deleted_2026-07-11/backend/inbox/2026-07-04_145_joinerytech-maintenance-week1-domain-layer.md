---
processed: 2026-07-06
id: MSG-BACKEND-145
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
epic_id: EPIC-JT-MAINT
checkpoint_id: CP-MAINT-BACKEND
ref: MSG-ARCHITECT-062-DONE
created: 2026-07-04
unblocked_at: 2026-07-06
unblocked_by: conductor
# blocked_by: MSG-BACKEND-143 (DONE 2026-07-06 14:20, unblocked)
estimated_nwt: 150
content_hash: 43d01e669b6ae261335f0626b9b1d1c5646c1744d85cab1f68df4320fa7be6e5
---

# JoineryTech Maintenance Week 1 — Domain Layer Implementation

**Epic:** EPIC-JT-MAINT (Maintenance & Asset Management)
**Checkpoint:** CP-MAINT-BACKEND
**Estimated:** 150 NWT (~5 hours)
**Blocked by:** MSG-BACKEND-143 (Kontrolling Week 2 — finish first to avoid context switching)

---

## Context

Az Architect elkészítette a Maintenance & Asset Management Week 0 OpenAPI specifikációt ✅:
- **File:** `/opt/spaceos/docs/api/joinerytech-maintenance-v1.yaml` (2,500+ lines, 31 endpoints)
- **Validation:** Redocly lint 0 errors, Orval code-gen test passed ✅
- **MSG:** MSG-ARCHITECT-062-DONE

A Domain Model specifikáció készen áll:
- **File:** `/opt/spaceos/docs/joinerytech/domain/MAINTENANCE_DOMAIN_MODEL.md`
- **Aggregates:** Asset, WorkOrder
- **Domain Services:** AssetStatusCalculationService, PreventiveMaintenanceSchedulerService, MaintenanceCostEstimatorService
- **FSM:** WorkOrder (7 transitions: schedule, assign, start, complete, postpone, reject, reopen)
- **Production Integration:** Downtime blocking API (`/api/maintenance/downtimes/active`)

**Most a Week 1 Domain Layer-t kell implementálni.**

---

## Deliverables

### 1. Projekt struktúra
```
spaceos-modules-maintenance/
  Domain/
    Aggregates/
      Asset.cs
      WorkOrder.cs
    ValueObjects/
      AssetId.cs
      WorkOrderId.cs
      MaintenancePlan.cs
      WorkOrderPart.cs
      Downtime.cs
    Enums/
      AssetKind.cs
      AssetStatus.cs
      WorkOrderStatus.cs
      WorkOrderType.cs
      WorkOrderPriority.cs
      AssignmentType.cs
      MaintenanceTrigger.cs
    Services/
      AssetStatusCalculationService.cs
      PreventiveMaintenanceSchedulerService.cs
      MaintenanceCostEstimatorService.cs
      IAssetStatusCalculationService.cs
      IPreventiveMaintenanceSchedulerService.cs
      IMaintenanceCostEstimatorService.cs
    Events/
      AssetCreatedEvent.cs
      AssetOperatingHoursRecordedEvent.cs
      AssetRetiredEvent.cs
      AssetReactivatedEvent.cs
      AssetLinkedToMachineEvent.cs
      AssetLinkedToVehicleEvent.cs
      MaintenancePlanAddedEvent.cs
      MaintenancePlanRemovedEvent.cs
      WorkOrderReportedEvent.cs
      WorkOrderScheduledEvent.cs
      WorkOrderAssignedEvent.cs
      WorkOrderStartedEvent.cs
      WorkOrderCompletedEvent.cs
      WorkOrderPostponedEvent.cs
      WorkOrderRejectedEvent.cs
      WorkOrderReopenedEvent.cs
      WorkOrderPartAddedEvent.cs
      WorkOrderPartRemovedEvent.cs
    Repositories/
      IAssetRepository.cs
      IWorkOrderRepository.cs
    FSM/
      WorkOrderStatusTransitions.cs
  Tests/
    Domain/
      AssetTests.cs
      WorkOrderTests.cs
      AssetStatusCalculationServiceTests.cs
      PreventiveMaintenanceSchedulerServiceTests.cs
      MaintenanceCostEstimatorServiceTests.cs
      WorkOrderFsmTests.cs
```

### 2. Asset Aggregate (Asset.cs)

**Invariants:**
- Code must be unique per tenant
- Name must not be empty
- Retired assets cannot have operating hours recorded
- Only Machine assets can have MachineId
- Only Vehicle assets can have VehicleId

**Factory method:**
```csharp
public static Asset Create(
    TenantId tenantId,
    string code,
    string name,
    AssetKind kind,
    FacilityId facilityId,
    string location,
    string vendor = null,
    string model = null)
```

**Methods:**
- `RecordOperatingHours(decimal hours)` — increment operating hours (for hour-based maintenance plans)
- `Retire()` — soft delete, block future maintenance
- `Reactivate()` — reactivate retired asset
- `AddMaintenancePlan(MaintenancePlan plan)` — attach preventive maintenance plan
- `RemoveMaintenancePlan(string planId)` — remove maintenance plan
- `LinkToMachine(string machineId)` — Production integration (only for AssetKind.Machine)
- `LinkToVehicle(string vehicleId)` — Logistics integration (only for AssetKind.Vehicle)

**Properties:**
- Maintenance plans: `private readonly List<MaintenancePlan> _maintenancePlans = new();`
- ReadOnly exposure: `public IReadOnlyList<MaintenancePlan> MaintenancePlans => _maintenancePlans.AsReadOnly();`
- OperatingHours: `public decimal OperatingHours { get; private set; }`
- MachineId (Production): `public string MachineId { get; private set; }`
- VehicleId (Logistics): `public string VehicleId { get; private set; }`

**Domain Events:**
- AssetCreatedEvent
- AssetOperatingHoursRecordedEvent
- AssetRetiredEvent, AssetReactivatedEvent
- AssetLinkedToMachineEvent, AssetLinkedToVehicleEvent
- MaintenancePlanAddedEvent, MaintenancePlanRemovedEvent

### 3. WorkOrder Aggregate (WorkOrder.cs)

**Invariants:**
- Title must not be empty
- Actual hours must be > 0 when completing
- Rejection/postponement reason required when rejecting/postponing
- Internal assignments require AssignedEmployeeId
- External assignments require AssignedPartnerId
- Cannot add/remove parts on Completed work orders
- FSM transition validation (WorkOrderStatusTransitions)

**Factory method:**
```csharp
public static WorkOrder Create(
    TenantId tenantId,
    AssetId assetId,
    WorkOrderType type,
    WorkOrderPriority priority,
    string title,
    string description,
    decimal estimatedHours = 0,
    bool requiresDowntime = false)
```

**FSM Transitions (7):**
- `Schedule(DateTime scheduledAt, decimal estimatedHours)` — Reported → Scheduled
- `AssignInternalTechnician(EmployeeId employeeId)` — Assign internal tech (Reported/Scheduled only)
- `AssignExternalContractor(PartnerId partnerId)` — Assign external contractor (Reported/Scheduled only)
- `StartWork()` — Scheduled → InProgress (requires assignment)
- `Complete(decimal actualHours)` — InProgress → Completed
- `Postpone(string reason)` — InProgress/Scheduled → Postponed
- `Reject(string reason)` — Reported/Scheduled → Rejected
- `Reopen()` — Postponed/Rejected → Reported

**Parts Management:**
- `AddPart(string catalogCode, int quantity, Money unitPrice)` — add part (blocked if Completed)
- `RemovePart(string partId)` — remove part (blocked if Completed)

**Properties:**
- Parts list: `private readonly List<WorkOrderPart> _parts = new();`
- ReadOnly exposure: `public IReadOnlyList<WorkOrderPart> Parts => _parts.AsReadOnly();`
- RequiresDowntime: `public bool RequiresDowntime { get; private set; }` — **CRITICAL for Production integration!**

**Domain Events:**
- WorkOrderReportedEvent
- WorkOrderScheduledEvent, WorkOrderAssignedEvent
- WorkOrderStartedEvent (includes RequiresDowntime for Production integration)
- WorkOrderCompletedEvent
- WorkOrderPostponedEvent, WorkOrderRejectedEvent, WorkOrderReopenedEvent
- WorkOrderPartAddedEvent, WorkOrderPartRemovedEvent

### 4. Value Objects

**MaintenancePlan:**
- Id (string, GUID)
- Label (string, e.g., "Havi kenőanyag csere")
- Trigger (MaintenanceTrigger: Interval or OperatingHours)
- IntervalDays (int?, for interval-based plans)
- IntervalHours (decimal?, for hour-based plans)
- EstimatedHours (decimal)
- LastDone (DateOnly?, last execution date)
- LastDoneHours (decimal?, asset hours at last execution)
- AssigneeType (AssignmentType: Internal/External)
- AssigneeEmployeeId (EmployeeId?, default technician)

**WorkOrderPart:**
- Id (string, GUID)
- CatalogCode (string, reference to Catalog)
- Quantity (int)
- UnitPrice (Money)
- TotalPrice (Money, calculated)

**Downtime (for Production integration):**
- AssetId
- MachineId (string, reference to Production SHOPFLOOR_MACHINES)
- WorkOrderId
- StartedAt (DateTime)
- EstimatedEndAt (DateTime?)
- Type (WorkOrderType)

### 5. Enums

**AssetKind:**
- Machine (Production CNC/Lathe/etc.)
- Vehicle (Logistics vans/trucks)
- Tool (Hand tools, power tools)
- Infrastructure (HVAC, electrical)
- IT (Computers, servers, network)
- Room (Facility spaces)

**AssetStatus (computed, NEVER stored):**
- Operational — asset működik
- Maintenance — karbantartás alatt (InProgress WO with RequiresDowntime=true)
- Breakdown — géptörés (InProgress corrective WO with RequiresDowntime=true)
- Retired — selejtezve

**WorkOrderStatus:**
- Reported
- Scheduled
- InProgress
- Completed
- Postponed
- Rejected

**WorkOrderType:**
- Corrective (breakdown repair)
- Preventive (scheduled maintenance)
- Cleaning

**WorkOrderPriority:**
- Critical
- High
- Medium
- Low

**AssignmentType:**
- Internal (employee)
- External (contractor/partner)

**MaintenanceTrigger:**
- Interval (days-based, e.g., every 30 days)
- OperatingHours (hours-based, e.g., every 500 hours)

### 6. Domain Services

**AssetStatusCalculationService:**
```csharp
public interface IAssetStatusCalculationService
{
    AssetStatus CalculateStatus(Asset asset, IEnumerable<WorkOrder> workOrders);
}
```

**Implementation:**
- Priority 1: Retired → AssetStatus.Retired
- Priority 2: Breakdown → InProgress corrective WO with RequiresDowntime=true
- Priority 3: Maintenance → InProgress WO with RequiresDowntime=true
- Default: Operational

**⚠️ CRITICAL:** Status is NEVER stored! Always computed from Asset.Retired + WorkOrders.

**PreventiveMaintenanceSchedulerService:**
```csharp
public interface IPreventiveMaintenanceSchedulerService
{
    bool IsPlanDue(MaintenancePlan plan, Asset asset, DateOnly today, int withinDays = 7);
    IEnumerable<MaintenancePlan> GetDuePlans(IEnumerable<Asset> assets, DateOnly today, int withinDays = 7);
    WorkOrder CreateWorkOrderFromPlan(MaintenancePlan plan, Asset asset);
}
```

**Implementation:**
- **Interval-based plans:** Check LastDone + IntervalDays vs. today
- **Hour-based plans:** Check (OperatingHours - LastDoneHours) vs. IntervalHours
- Due threshold: withinDays for interval, 50 hours for operating hours

**MaintenanceCostEstimatorService:**
```csharp
public interface IMaintenanceCostEstimatorService
{
    Money CalculateEstimatedCost(WorkOrder workOrder, decimal hourlyRate);
    Money CalculateActualCost(WorkOrder workOrder, decimal hourlyRate);
}
```

**Implementation:**
- EstimatedCost = (EstimatedHours × hourlyRate) + sum(Parts.TotalPrice)
- ActualCost = (ActualHours × hourlyRate) + sum(Parts.TotalPrice)

### 7. FSM Transition Validator

**WorkOrderStatusTransitions:**
```csharp
public static class WorkOrderStatusTransitions
{
    public static bool IsValidTransition(WorkOrderStatus from, WorkOrderStatus to);
    public static IReadOnlyList<WorkOrderStatus> GetAllowedTransitions(WorkOrderStatus from);
}
```

**Transition Rules:**
- Reported → Scheduled, Rejected
- Scheduled → InProgress, Postponed, Rejected
- InProgress → Completed, Postponed
- Postponed → Reported (reopen)
- Rejected → Reported (reopen)
- No backwards transitions (immutable audit trail)

### 8. Repository Contracts

**IAssetRepository:**
```csharp
public interface IAssetRepository
{
    Task<Asset?> GetByIdAsync(AssetId id, CancellationToken ct = default);
    Task<Asset?> GetByCodeAsync(TenantId tenantId, string code, CancellationToken ct = default);
    Task<IEnumerable<Asset>> GetActiveByKindAsync(TenantId tenantId, AssetKind kind, CancellationToken ct = default);
    Task<IEnumerable<Asset>> GetActiveByFacilityAsync(TenantId tenantId, FacilityId facilityId, CancellationToken ct = default);
    Task<IEnumerable<Asset>> GetByMachineIdAsync(TenantId tenantId, string machineId, CancellationToken ct = default);
    Task AddAsync(Asset asset, CancellationToken ct = default);
    Task UpdateAsync(Asset asset, CancellationToken ct = default);
}
```

**IWorkOrderRepository:**
```csharp
public interface IWorkOrderRepository
{
    Task<WorkOrder?> GetByIdAsync(WorkOrderId id, CancellationToken ct = default);
    Task<IEnumerable<WorkOrder>> GetActiveByAssetAsync(AssetId assetId, CancellationToken ct = default);
    Task<IEnumerable<WorkOrder>> GetByStatusAsync(TenantId tenantId, WorkOrderStatus status, CancellationToken ct = default);
    Task<IEnumerable<WorkOrder>> GetInProgressWithDowntimeAsync(TenantId tenantId, CancellationToken ct = default); // PRODUCTION INTEGRATION
    Task<IEnumerable<WorkOrder>> GetDuePreventiveAsync(TenantId tenantId, DateOnly today, CancellationToken ct = default);
    Task AddAsync(WorkOrder workOrder, CancellationToken ct = default);
    Task UpdateAsync(WorkOrder workOrder, CancellationToken ct = default);
}
```

**⚠️ CRITICAL Production Integration:**
`GetInProgressWithDowntimeAsync()` returns all WorkOrders where:
- Status = InProgress
- RequiresDowntime = true

This is used by Production module to query `/api/maintenance/downtimes/active` and block machine capacity.

### 9. Unit Tests

**Test coverage minimum: 80%**

**AssetTests.cs (15+ tests):**
- Asset creation validation (code, name)
- Operating hours recording (positive hours only, blocked if retired)
- Retire/Reactivate
- Maintenance plan add/remove (duplicate detection, not found errors)
- LinkToMachine (only for AssetKind.Machine, validation)
- LinkToVehicle (only for AssetKind.Vehicle, validation)
- Domain event assertions

**WorkOrderTests.cs (25+ tests):**
- WorkOrder creation validation (title, estimatedHours)
- FSM transitions: Schedule, AssignInternalTechnician, AssignExternalContractor, StartWork, Complete, Postpone, Reject, Reopen
- Invalid transitions (exception assertions)
- Assignment validation (cannot start without assignment)
- Postponement/rejection reason validation
- Parts management: AddPart, RemovePart (blocked if Completed)
- Domain event assertions

**AssetStatusCalculationServiceTests.cs (8+ tests):**
- Retired asset → AssetStatus.Retired
- InProgress corrective WO with downtime → AssetStatus.Breakdown
- InProgress preventive WO with downtime → AssetStatus.Maintenance
- InProgress WO without downtime → AssetStatus.Operational
- No active WOs → AssetStatus.Operational
- Multiple InProgress WOs → priority order (Breakdown > Maintenance)

**PreventiveMaintenanceSchedulerServiceTests.cs (12+ tests):**
- Interval-based plan due calculation (never executed, within days, overdue)
- Hour-based plan due calculation (never executed, threshold, overdue)
- GetDuePlans aggregation (exclude retired assets)
- CreateWorkOrderFromPlan (link to plan, assign technician if specified)

**MaintenanceCostEstimatorServiceTests.cs (6+ tests):**
- CalculateEstimatedCost: labor only, labor + parts
- CalculateActualCost: labor only, labor + parts
- Edge cases: 0 hours, no parts, multiple parts

**WorkOrderFsmTests.cs (15+ tests):**
- IsValidTransition for all valid paths
- IsValidTransition rejects invalid paths
- GetAllowedTransitions for each status
- Edge cases: Reopen from Postponed/Rejected

**Test pattern (EXAMPLE):**
```csharp
[Fact]
public void Create_ValidAsset_ShouldSucceed()
{
    // Arrange
    var tenantId = TenantId.New();
    var facilityId = FacilityId.New();

    // Act
    var asset = Asset.Create(
        tenantId,
        "CNC-001",
        "Haas VF-2",
        AssetKind.Machine,
        facilityId,
        "Műhely 1, Bal sarok",
        "Haas Automation",
        "VF-2");

    // Assert
    asset.Should().NotBeNull();
    asset.Code.Should().Be("CNC-001");
    asset.Name.Should().Be("Haas VF-2");
    asset.Kind.Should().Be(AssetKind.Machine);
    asset.Retired.Should().BeFalse();
    asset.OperatingHours.Should().Be(0);
    asset.DomainEvents.Should().ContainSingle(e => e is AssetCreatedEvent);
}

[Fact]
public void CalculateStatus_InProgressCorrectiveWithDowntime_ShouldReturnBreakdown()
{
    // Arrange
    var service = new AssetStatusCalculationService();
    var asset = CreateAsset();
    var workOrder = CreateWorkOrder(WorkOrderType.Corrective, WorkOrderStatus.InProgress, requiresDowntime: true);

    // Act
    var status = service.CalculateStatus(asset, new[] { workOrder });

    // Assert
    status.Should().Be(AssetStatus.Breakdown);
}

[Fact]
public void GetInProgressWithDowntimeAsync_ShouldOnlyReturnDowntimeBlockingWorkOrders()
{
    // This test validates Production integration!
    // Arrange
    var repo = CreateMockRepository();
    var wo1 = CreateWorkOrder(status: WorkOrderStatus.InProgress, requiresDowntime: true); // INCLUDED
    var wo2 = CreateWorkOrder(status: WorkOrderStatus.InProgress, requiresDowntime: false); // EXCLUDED
    var wo3 = CreateWorkOrder(status: WorkOrderStatus.Completed, requiresDowntime: true); // EXCLUDED

    // Act
    var result = await repo.GetInProgressWithDowntimeAsync(tenantId);

    // Assert
    result.Should().ContainSingle();
    result.First().Id.Should().Be(wo1.Id);
}
```

---

## .csproj Dependencies

**Add to spaceos-modules-maintenance.csproj:**
```xml
<PackageReference Include="FluentAssertions" Version="6.12.0" />
<PackageReference Include="xunit" Version="2.9.0" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
```

---

## Security & Validation Considerations

1. **Production Integration (CRITICAL!):**
   - `GetInProgressWithDowntimeAsync()` must be RLS-aware (tenant isolation)
   - Production queries this endpoint to block machine capacity
   - If broken, entire factory capacity planning fails!

2. **Asset Code Uniqueness:**
   - Repository must enforce unique constraint per tenant
   - Application layer validates before calling Create

3. **FSM Immutability:**
   - No backwards transitions (except reopen from Postponed/Rejected)
   - Domain Events create audit trail (never delete events)

4. **Input Validation:**
   - All factory methods validate inputs (ArgumentException on invalid)
   - Operating hours: must be > 0
   - Assignment: internal requires EmployeeId, external requires PartnerId
   - Rejection/postponement: reason required

5. **AssetStatus Calculation:**
   - NEVER store AssetStatus in database
   - Always compute from Asset.Retired + WorkOrders
   - Application layer must enforce this (Week 2)

---

## Build & Test Gate

**Before submitting DONE:**
```bash
cd /opt/spaceos/spaceos-modules-maintenance
dotnet build
dotnet test --verbosity normal

# Expected:
# Build: 0 warnings, 0 errors
# Tests: 81+ tests, all green ✅
```

**Test coverage minimum:** 80% for domain layer

---

## Acceptance Criteria

- [ ] Asset aggregate implemented with all methods (Create, RecordOperatingHours, Retire, Reactivate, AddMaintenancePlan, RemoveMaintenancePlan, LinkToMachine, LinkToVehicle)
- [ ] WorkOrder aggregate implemented with all FSM transitions (Schedule, AssignInternalTechnician, AssignExternalContractor, StartWork, Complete, Postpone, Reject, Reopen)
- [ ] Parts management implemented (AddPart, RemovePart)
- [ ] 7 enums implemented (AssetKind, AssetStatus, WorkOrderStatus, WorkOrderType, WorkOrderPriority, AssignmentType, MaintenanceTrigger)
- [ ] 3 value objects implemented (MaintenancePlan, WorkOrderPart, Downtime)
- [ ] AssetStatusCalculationService implemented (Retired → Breakdown → Maintenance → Operational priority)
- [ ] PreventiveMaintenanceSchedulerService implemented (interval-based + hour-based plans)
- [ ] MaintenanceCostEstimatorService implemented (estimated + actual cost calculation)
- [ ] WorkOrderStatusTransitions FSM validator implemented
- [ ] 17 domain events implemented
- [ ] 2 repository contracts implemented (IAssetRepository, IWorkOrderRepository)
- [ ] **Production integration query:** `GetInProgressWithDowntimeAsync()` — CRITICAL!
- [ ] **81+ unit tests** — all green ✅
- [ ] Build: 0 warnings, 0 errors
- [ ] Test coverage: ≥80%

---

## DONE Outbox Format

**File:** `2026-07-04_NNN_msg-145-maintenance-week1-domain-done.md`

**Frontmatter:**
```yaml
---
id: MSG-BACKEND-145-DONE
from: backend
to: conductor
type: done
status: UNREAD
ref: MSG-BACKEND-145
epic_id: EPIC-JT-MAINT
checkpoint_id: CP-MAINT-BACKEND
created: YYYY-MM-DD
---
```

**Tartalom:**
- Összefoglaló: Mi készült el?
- Unit test eredmények (hány teszt, coverage %)
- Build státusz (warnings/errors)
- Files changed lista
- **Production integration validation:** GetInProgressWithDowntimeAsync() teszt eredmény
- Következő lépés: Week 2 Application Layer (DTOs, Query/Command handlers, Integration Data Provider for Kontrolling)

---

## References

- **Domain Model:** `/opt/spaceos/docs/joinerytech/domain/MAINTENANCE_DOMAIN_MODEL.md`
- **OpenAPI Spec:** `/opt/spaceos/docs/api/joinerytech-maintenance-v1.yaml`
- **Architect DONE:** MSG-ARCHITECT-062-DONE
- **Production Integration:** `/api/maintenance/downtimes/active` endpoint (downtime blocking)

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
