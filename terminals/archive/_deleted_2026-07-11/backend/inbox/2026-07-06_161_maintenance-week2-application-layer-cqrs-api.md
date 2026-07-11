---
id: MSG-BACKEND-161
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
epic_id: EPIC-JT-MAINTENANCE
checkpoint_id: CP-MAINT-APPLICATION
estimated_nwt: 180
created: 2026-07-06
ref: MSG-CONDUCTOR-094
content_hash: 64cb1efd01f75e13245ded4d72a40593c5c9abc2b39fd194343f5842f1eff24e
---

# Maintenance Week 2 Application Layer — CQRS + API

## Context

**Domain Layer:** ✅ DONE (MSG-BACKEND-145, 100 tests PASS)

**Maintenance modul domain:**
- 2 aggregates: Asset (8 methods), WorkOrder (11 methods)
- FSM: WorkOrderStatus (7 states, 7 valid transitions)
- Enums: AssetKind, AssetStatus (COMPUTED!), WorkOrderType, WorkOrderPriority
- Value Objects: MaintenancePlan, WorkOrderPart, Downtime
- Domain Services: AssetStatusCalculation, PreventiveMaintenanceScheduler, MaintenanceCostEstimator
- Repository Contracts: IAssetRepository, IWorkOrderRepository (GetInProgressWithDowntimeAsync)

**Production Integration:** WorkOrderStartedEvent.RequiresDowntime → Production module capacity planning

**Pattern:** Follow DMS/HR Week 2 CQRS pattern (MediatR + FluentValidation + Minimal API)

---

## Task: Application Layer Implementation

### Phase 1: Commands (Asset) — 6 Commands + 6 Handlers

**AssetId Commands:**
```csharp
// 1. CreateAssetCommand
public record CreateAssetCommand(
    AssetKind Kind,
    string Code,           // max 50 chars
    string Name,           // max 200 chars
    string Location,       // max 200 chars
    Guid TenantId
) : IRequest<Result<AssetId>>;

// Handler: Asset.CreateAsync(kind, code, name, location, tenantId)

// 2. RecordOperatingHoursCommand
public record RecordOperatingHoursCommand(
    AssetId AssetId,
    decimal Hours,         // must be positive
    Guid TenantId
) : IRequest<Result>;

// Handler: asset.RecordOperatingHours(hours)
// Validation: Only Machine/Vehicle can record hours, not Retired assets

// 3. RetireAssetCommand
public record RetireAssetCommand(
    AssetId AssetId,
    string Reason,         // optional
    Guid TenantId
) : IRequest<Result>;

// Handler: asset.Retire()

// 4. ReactivateAssetCommand
public record ReactivateAssetCommand(
    AssetId AssetId,
    Guid TenantId
) : IRequest<Result>;

// Handler: asset.Reactivate()

// 5. AddMaintenancePlanCommand
public record AddMaintenancePlanCommand(
    AssetId AssetId,
    MaintenanceTrigger Trigger,     // Interval | OperatingHours
    int? IntervalDays,              // required if Trigger == Interval
    decimal? OperatingHoursThreshold, // required if Trigger == OperatingHours
    string? Description,
    Guid TenantId
) : IRequest<Result>;

// Handler: asset.AddMaintenancePlan(new MaintenancePlan(...))

// 6. RemoveMaintenancePlanCommand
public record RemoveMaintenancePlanCommand(
    AssetId AssetId,
    int PlanIndex,        // index in MaintenancePlans collection
    Guid TenantId
) : IRequest<Result>;

// Handler: asset.RemoveMaintenancePlan(plan)
```

### Phase 2: Commands (WorkOrder) — 10 Commands + 10 Handlers

```csharp
// 7. ReportWorkOrderCommand
public record ReportWorkOrderCommand(
    AssetId AssetId,
    WorkOrderType Type,     // Corrective | Preventive | Cleaning
    WorkOrderPriority Priority,
    string Title,           // max 200 chars
    string Description,     // max 2000 chars
    Guid TenantId
) : IRequest<Result<WorkOrderId>>;

// Handler: WorkOrder.ReportAsync(assetId, type, priority, title, description, tenantId)

// 8. ScheduleWorkOrderCommand
public record ScheduleWorkOrderCommand(
    WorkOrderId WorkOrderId,
    DateTime ScheduledStart,
    decimal EstimatedHours,  // must be positive
    Guid TenantId
) : IRequest<Result>;

// Handler: workOrder.Schedule(scheduledStart, estimatedHours)

// 9. AssignWorkOrderCommand
public record AssignWorkOrderCommand(
    WorkOrderId WorkOrderId,
    Guid AssignedTo,         // UserId
    AssignmentType AssignmentType, // Internal | External
    Guid TenantId
) : IRequest<Result>;

// Handler: workOrder.Assign(assignedTo, assignmentType)

// 10. StartWorkOrderCommand
public record StartWorkOrderCommand(
    WorkOrderId WorkOrderId,
    bool RequiresDowntime,   // CRITICAL: Production integration flag
    Guid TenantId
) : IRequest<Result>;

// Handler: workOrder.StartWork(requiresDowntime)
// Raises WorkOrderStartedEvent with RequiresDowntime flag

// 11. CompleteWorkOrderCommand
public record CompleteWorkOrderCommand(
    WorkOrderId WorkOrderId,
    decimal ActualHours,     // must be positive
    string? CompletionNote,  // optional
    Guid TenantId
) : IRequest<Result>;

// Handler: workOrder.Complete(actualHours, completionNote)

// 12. PostponeWorkOrderCommand
public record PostponeWorkOrderCommand(
    WorkOrderId WorkOrderId,
    string Reason,           // max 500 chars
    Guid TenantId
) : IRequest<Result>;

// Handler: workOrder.Postpone(reason)

// 13. RejectWorkOrderCommand
public record RejectWorkOrderCommand(
    WorkOrderId WorkOrderId,
    string Reason,           // max 500 chars
    Guid TenantId
) : IRequest<Result>;

// Handler: workOrder.Reject(reason)

// 14. ReopenWorkOrderCommand
public record ReopenWorkOrderCommand(
    WorkOrderId WorkOrderId,
    string Reason,           // max 500 chars
    Guid TenantId
) : IRequest<Result>;

// Handler: workOrder.Reopen(reason)

// 15. AddWorkOrderPartCommand
public record AddWorkOrderPartCommand(
    WorkOrderId WorkOrderId,
    string PartName,         // max 200 chars
    int Quantity,            // must be positive
    decimal UnitPrice,       // must be positive
    Guid TenantId
) : IRequest<Result>;

// Handler: workOrder.AddPart(new WorkOrderPart(partName, quantity, unitPrice))

// 16. RemoveWorkOrderPartCommand
public record RemoveWorkOrderPartCommand(
    WorkOrderId WorkOrderId,
    int PartIndex,           // index in Parts collection
    Guid TenantId
) : IRequest<Result>;

// Handler: workOrder.RemovePart(part)
```

### Phase 3: Queries (Asset) — 4 Queries + 4 Handlers

```csharp
// 1. GetAssetQuery
public record GetAssetQuery(
    AssetId AssetId,
    Guid TenantId
) : IRequest<Result<AssetDto>>;

// Handler: IAssetRepository.GetByIdAsync(assetId, tenantId)

// 2. GetAssetsQuery
public record GetAssetsQuery(
    AssetKind? Kind,         // optional filter
    AssetStatus? Status,     // optional filter (COMPUTED!)
    int Page = 1,
    int PageSize = 20,
    Guid TenantId
) : IRequest<Result<AssetListDto[]>>;

// Handler: IAssetRepository + IAssetStatusCalculationService
// Note: AssetStatus is COMPUTED from WorkOrders, not stored

// 3. GetAssetMaintenanceHistoryQuery
public record GetAssetMaintenanceHistoryQuery(
    AssetId AssetId,
    Guid TenantId
) : IRequest<Result<WorkOrderDto[]>>;

// Handler: IWorkOrderRepository.GetByAssetIdAsync(assetId, tenantId)

// 4. GetAssetsRequiringMaintenanceQuery
public record GetAssetsRequiringMaintenanceQuery(
    Guid TenantId
) : IRequest<Result<AssetDto[]>>;

// Handler: IPreventiveMaintenanceSchedulerService.GetAssetsRequiringMaintenance(assets)
// Logic: Check MaintenancePlan intervals/hours thresholds
```

### Phase 4: Queries (WorkOrder) — 5 Queries + 5 Handlers

```csharp
// 5. GetWorkOrderQuery
public record GetWorkOrderQuery(
    WorkOrderId WorkOrderId,
    Guid TenantId
) : IRequest<Result<WorkOrderDto>>;

// Handler: IWorkOrderRepository.GetByIdAsync(workOrderId, tenantId)

// 6. GetWorkOrdersQuery
public record GetWorkOrdersQuery(
    WorkOrderStatus? Status,   // optional filter
    WorkOrderType? Type,       // optional filter
    int Page = 1,
    int PageSize = 20,
    Guid TenantId
) : IRequest<Result<WorkOrderListDto[]>>;

// Handler: IWorkOrderRepository with filters

// 7. GetPendingWorkOrdersQuery
public record GetPendingWorkOrdersQuery(
    Guid TenantId
) : IRequest<Result<WorkOrderDto[]>>;

// Handler: IWorkOrderRepository where Status == Reported

// 8. GetAssetCurrentWorkOrdersQuery
public record GetAssetCurrentWorkOrdersQuery(
    AssetId AssetId,
    Guid TenantId
) : IRequest<Result<WorkOrderDto[]>>;

// Handler: IWorkOrderRepository where AssetId = X AND Status IN (Reported, Scheduled, InProgress, Postponed)

// 9. GetInProgressWithDowntimeQuery (CRITICAL: Production Integration)
public record GetInProgressWithDowntimeQuery(
    Guid TenantId
) : IRequest<Result<WorkOrderDto[]>>;

// Handler: IWorkOrderRepository.GetInProgressWithDowntimeAsync(tenantId)
// Used by Production module for capacity planning
```

### Phase 5: FluentValidation Validators — 16 Validators

**Asset Validators:**
```csharp
// 1. CreateAssetValidator
RuleFor(x => x.Code).NotEmpty().Length(1, 50);
RuleFor(x => x.Name).NotEmpty().Length(1, 200);
RuleFor(x => x.Location).NotEmpty().Length(1, 200);
RuleFor(x => x.Kind).IsInEnum();

// 2. RecordOperatingHoursValidator
RuleFor(x => x.Hours).GreaterThan(0).WithMessage("Operating hours must be positive");

// 3. RetireAssetValidator
RuleFor(x => x.Reason).MaximumLength(500);

// 4. ReactivateAssetValidator
// No special validation (AssetId + TenantId only)

// 5. AddMaintenancePlanValidator
RuleFor(x => x.Trigger).IsInEnum();
RuleFor(x => x.IntervalDays)
    .GreaterThan(0)
    .When(x => x.Trigger == MaintenanceTrigger.Interval)
    .WithMessage("Interval days must be positive when using Interval trigger");
RuleFor(x => x.OperatingHoursThreshold)
    .GreaterThan(0)
    .When(x => x.Trigger == MaintenanceTrigger.OperatingHours)
    .WithMessage("Operating hours threshold must be positive when using OperatingHours trigger");

// 6. RemoveMaintenancePlanValidator
RuleFor(x => x.PlanIndex).GreaterThanOrEqualTo(0);
```

**WorkOrder Validators:**
```csharp
// 7. ReportWorkOrderValidator
RuleFor(x => x.Title).NotEmpty().Length(1, 200);
RuleFor(x => x.Description).NotEmpty().Length(1, 2000);
RuleFor(x => x.Type).IsInEnum();
RuleFor(x => x.Priority).IsInEnum();

// 8. ScheduleWorkOrderValidator
RuleFor(x => x.ScheduledStart).GreaterThanOrEqualTo(DateTime.Today).WithMessage("Scheduled start cannot be in the past");
RuleFor(x => x.EstimatedHours).GreaterThan(0);

// 9. AssignWorkOrderValidator
RuleFor(x => x.AssignedTo).NotEmpty();
RuleFor(x => x.AssignmentType).IsInEnum();

// 10. StartWorkOrderValidator
// No special validation (RequiresDowntime is bool)

// 11. CompleteWorkOrderValidator
RuleFor(x => x.ActualHours).GreaterThan(0);
RuleFor(x => x.CompletionNote).MaximumLength(1000);

// 12. PostponeWorkOrderValidator
RuleFor(x => x.Reason).NotEmpty().Length(1, 500);

// 13. RejectWorkOrderValidator
RuleFor(x => x.Reason).NotEmpty().Length(1, 500);

// 14. ReopenWorkOrderValidator
RuleFor(x => x.Reason).NotEmpty().Length(1, 500);

// 15. AddWorkOrderPartValidator
RuleFor(x => x.PartName).NotEmpty().Length(1, 200);
RuleFor(x => x.Quantity).GreaterThan(0);
RuleFor(x => x.UnitPrice).GreaterThan(0);

// 16. RemoveWorkOrderPartValidator
RuleFor(x => x.PartIndex).GreaterThanOrEqualTo(0);
```

### Phase 6: DTOs — 18 DTOs

**Request DTOs (16):**
- CreateAssetDto
- RecordOperatingHoursDto
- RetireAssetDto
- ReactivateAssetDto
- AddMaintenancePlanDto
- RemoveMaintenancePlanDto
- ReportWorkOrderDto
- ScheduleWorkOrderDto
- AssignWorkOrderDto
- StartWorkOrderDto
- CompleteWorkOrderDto
- PostponeWorkOrderDto
- RejectWorkOrderDto
- ReopenWorkOrderDto
- AddWorkOrderPartDto
- RemoveWorkOrderPartDto

**Response DTOs (8):**
```csharp
// AssetDto
public record AssetDto(
    Guid Id,
    AssetKind Kind,
    string Code,
    string Name,
    string Location,
    AssetStatus Status,           // COMPUTED from WorkOrders!
    decimal? OperatingHours,      // only for Machine/Vehicle
    bool Retired,
    MaintenancePlanDto[] MaintenancePlans,
    DateTime CreatedAt
);

// AssetListDto (lighter for pagination)
public record AssetListDto(
    Guid Id,
    AssetKind Kind,
    string Code,
    string Name,
    AssetStatus Status,           // COMPUTED
    bool Retired
);

// MaintenancePlanDto
public record MaintenancePlanDto(
    MaintenanceTrigger Trigger,
    int? IntervalDays,
    decimal? OperatingHoursThreshold,
    string? Description
);

// WorkOrderDto
public record WorkOrderDto(
    Guid Id,
    Guid AssetId,
    string AssetCode,            // denormalized for convenience
    WorkOrderType Type,
    WorkOrderPriority Priority,
    WorkOrderStatus Status,
    string Title,
    string Description,
    DateTime? ScheduledStart,
    decimal? EstimatedHours,
    decimal? ActualHours,
    Guid? AssignedTo,
    AssignmentType? AssignmentType,
    bool RequiresDowntime,       // CRITICAL: Production integration
    WorkOrderPartDto[] Parts,
    decimal TotalPartsCost,
    string? CompletionNote,
    DateTime CreatedAt
);

// WorkOrderListDto (lighter for pagination)
public record WorkOrderListDto(
    Guid Id,
    Guid AssetId,
    string AssetCode,
    WorkOrderType Type,
    WorkOrderPriority Priority,
    WorkOrderStatus Status,
    string Title,
    DateTime CreatedAt
);

// WorkOrderPartDto
public record WorkOrderPartDto(
    string PartName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice
);

// EmployeeCapacityDto (if needed for Production integration)
// DepartmentCapacityDto (if needed for Production integration)
```

### Phase 7: API Endpoints — 20 Endpoints (Deferred to Host)

**Asset Endpoints (10):**
```
POST   /api/maintenance/assets                         CreateAsset
GET    /api/maintenance/assets/{id}                    GetAsset
GET    /api/maintenance/assets                         GetAssets (pagination + filters)
POST   /api/maintenance/assets/{id}/operating-hours    RecordOperatingHours
POST   /api/maintenance/assets/{id}/retire             RetireAsset
POST   /api/maintenance/assets/{id}/reactivate         ReactivateAsset
POST   /api/maintenance/assets/{id}/maintenance-plans  AddMaintenancePlan
DELETE /api/maintenance/assets/{id}/maintenance-plans/{index} RemoveMaintenancePlan
GET    /api/maintenance/assets/{id}/history            GetAssetMaintenanceHistory
GET    /api/maintenance/assets/requiring-maintenance   GetAssetsRequiringMaintenance
```

**WorkOrder Endpoints (10):**
```
POST   /api/maintenance/work-orders                    ReportWorkOrder
GET    /api/maintenance/work-orders/{id}               GetWorkOrder
GET    /api/maintenance/work-orders                    GetWorkOrders (pagination + filters)
GET    /api/maintenance/work-orders/pending            GetPendingWorkOrders
GET    /api/maintenance/work-orders/in-progress-downtime GetInProgressWithDowntime (Production!)
POST   /api/maintenance/work-orders/{id}/schedule      ScheduleWorkOrder
POST   /api/maintenance/work-orders/{id}/assign        AssignWorkOrder
POST   /api/maintenance/work-orders/{id}/start         StartWorkOrder
POST   /api/maintenance/work-orders/{id}/complete      CompleteWorkOrder
POST   /api/maintenance/work-orders/{id}/postpone      PostponeWorkOrder
POST   /api/maintenance/work-orders/{id}/reject        RejectWorkOrder
POST   /api/maintenance/work-orders/{id}/reopen        ReopenWorkOrder
POST   /api/maintenance/work-orders/{id}/parts         AddWorkOrderPart
DELETE /api/maintenance/work-orders/{id}/parts/{index} RemoveWorkOrderPart
```

**Note:** API endpoints deferred to host project (like HR Week 2 pattern).

### Phase 8: Integration Tests — 40+ Tests (Deferred to Host)

**Test Categories:**
- Asset lifecycle tests (create, retire, reactivate)
- Operating hours tracking tests (Machine/Vehicle only)
- Maintenance plan tests (interval/hours based)
- WorkOrder FSM tests (7 transitions)
- WorkOrder assignment tests (internal/external)
- WorkOrder parts tests (add/remove, cost calculation)
- Production integration test (GetInProgressWithDowntime)
- Validation tests (FluentValidation rules)

**Note:** Integration tests with Testcontainers deferred to host project.

---

## Acceptance Criteria

✅ **50 CQRS handlers implemented** (16 commands + 9 queries × 2 handlers each = 50 files)
✅ **16 FluentValidation validators**
✅ **18 DTOs** (16 request + 8 response, including nested DTOs)
⚠️ **20 API endpoints** (deferred to host)
⚠️ **40+ integration tests** (deferred to host)
✅ **Build: 0 errors, 0 warnings**
⚠️ **OpenAPI spec** (requires API endpoints in host)

---

## NuGet Dependencies

```xml
<PackageReference Include="MediatR" Version="12.4.1" />
<PackageReference Include="Ardalis.Result" Version="10.1.0" />
<PackageReference Include="FluentValidation" Version="11.10.0" />
```

---

## Critical Implementation Notes

### 1. AssetStatus COMPUTED Pattern

**AssetStatus NEVER stored in database!** Always computed from WorkOrders:

```csharp
// In GetAssetQuery handler:
var asset = await _assetRepository.GetByIdAsync(assetId, tenantId);
var activeWorkOrders = await _workOrderRepository.GetAssetCurrentWorkOrdersAsync(assetId, tenantId);
var computedStatus = _assetStatusService.GetAssetStatus(asset, activeWorkOrders);

return new AssetDto(..., Status: computedStatus, ...);
```

### 2. Production Integration (CRITICAL)

**GetInProgressWithDowntimeQuery** must return WorkOrders with `RequiresDowntime = true`:

```csharp
// Production module uses this to calculate available machine capacity
var blockedWorkOrders = await mediator.Send(new GetInProgressWithDowntimeQuery(tenantId));
var blockedAssetIds = blockedWorkOrders.Select(wo => wo.AssetId).ToHashSet();
var availableAssets = allAssets.Where(a => !blockedAssetIds.Contains(a.Id));
```

### 3. FSM Enforcement

**WorkOrderStatusTransitions validator** ensures FSM rules:

```csharp
// Valid transitions (from Week 1 Domain Layer):
Reported → Scheduled | InProgress | Rejected
Scheduled → InProgress | Postponed | Rejected
InProgress → Completed | Postponed
Postponed → Reported
Rejected → Reported
Completed → ∅ (terminal state)

// Handler must check before state change:
if (!WorkOrderStatusTransitions.IsValidTransition(workOrder.Status, newStatus))
    return Result.Error($"Invalid transition from {workOrder.Status} to {newStatus}");
```

### 4. Preventive Maintenance Scheduling

**IPreventiveMaintenanceSchedulerService** logic (from Week 1):

```csharp
// Interval-based:
if (plan.Trigger == Interval && DateTime.Today >= asset.LastMaintenanceDate + plan.IntervalDays)
    → Schedule preventive maintenance

// OperatingHours-based:
if (plan.Trigger == OperatingHours && asset.OperatingHours >= asset.LastMaintenanceHours + plan.OperatingHoursThreshold)
    → Schedule preventive maintenance
```

### 5. Cost Calculation

**WorkOrder.CalculateCost()** (from Week 1):

```csharp
decimal totalPartsCost = Parts.Sum(p => p.TotalPrice);
decimal laborCost = ActualHours * laborRate; // laborRate from IMaintenanceCostEstimatorService
decimal totalCost = totalPartsCost + laborCost;
```

---

## Security Patterns

✅ **FluentValidation** input validation on all commands
✅ **Domain invariants** enforced through aggregate methods
✅ **FSM state transitions** validated (WorkOrderStatusTransitions)
✅ **Repository interfaces** for data access (RLS ready)
✅ **Result pattern** for error handling (Ardalis.Result)
✅ **TenantId** mandatory in all commands/queries

---

## Build & Test

```bash
cd /opt/spaceos/backend/spaceos-modules/spaceos-modules-maintenance
dotnet build

# Expected:
# Build succeeded.
#     0 Warning(s)
#     0 Error(s)
```

**Note:** Integration tests deferred to host project (like HR Week 2 pattern).

---

## Next Steps (Week 3)

1. **Infrastructure Layer** — EF Core repositories, DbContext, migrations
2. **API Layer** — Minimal API endpoints in host project
3. **Integration Tests** — E2E tests with Testcontainers

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
