# JoineryTech Maintenance & Asset Management — Endpoint Inventory Matrix

**OpenAPI Spec:** `/opt/spaceos/docs/api/joinerytech-maintenance-v1.yaml`
**Version:** 1.0.0
**Domain Model Reference:** MSG-ARCHITECT-047
**Epic:** EPIC-JT-MAINT

---

## Endpoint Overview

| Category | Endpoints | Read/Write |
|----------|-----------|------------|
| **Asset Management** | 9 | 2 GET, 1 POST, 1 PUT, 1 DELETE, 4 POST (lifecycle) |
| **Work Order Management** | 12 | 2 GET, 1 POST, 1 PUT, 1 DELETE, 5 POST (FSM), 2 parts mgmt |
| **Maintenance Plans** | 6 | 3 GET, 1 POST, 1 PUT, 1 DELETE |
| **Downtime Tracking** | 4 | 4 GET |
| **TOTAL** | **31** | **11 GET, 3 POST (create), 2 PUT, 2 DELETE, 13 POST (actions)** |

---

## Asset Management Endpoints (9)

### CRUD Operations

| Endpoint | Method | Purpose | Request DTO | Response DTO | FSM Transition | Permission |
|----------|--------|---------|-------------|--------------|----------------|------------|
| `/api/maintenance/assets` | GET | List all assets with filters | query params (kind, facilityId, status) | `AssetDto[]` | - | `maintenance.view` |
| `/api/maintenance/assets/{assetId}` | GET | Get asset by ID | - | `AssetDto` | - | `maintenance.view` |
| `/api/maintenance/assets` | POST | Create new asset | `CreateAssetCommand` | `AssetDto` | - | `maintenance.manage` |
| `/api/maintenance/assets/{assetId}` | PUT | Update asset data | `UpdateAssetCommand` | `AssetDto` | - | `maintenance.manage` |
| `/api/maintenance/assets/{assetId}` | DELETE | Soft-delete asset | - | `204 No Content` | - | `maintenance.admin` |

### Operating Hours Tracking

| Endpoint | Method | Purpose | Request DTO | Response DTO | FSM Transition | Permission |
|----------|--------|---------|-------------|--------------|----------------|------------|
| `/api/maintenance/assets/{assetId}/record-hours` | POST | Record operating hours | `RecordOperatingHoursCommand` | `AssetDto` | - | `maintenance.manage` |

### Asset Lifecycle Management

| Endpoint | Method | Purpose | Request DTO | Response DTO | FSM Transition | Permission |
|----------|--------|---------|-------------|--------------|----------------|------------|
| `/api/maintenance/assets/{assetId}/retire` | POST | Retire asset permanently | - | `AssetDto` | **Operational -> Retired** | `maintenance.admin` |
| `/api/maintenance/assets/{assetId}/reactivate` | POST | Bring retired asset back to operational | - | `AssetDto` | **Retired -> Operational** | `maintenance.admin` |

### Asset Status Query

| Endpoint | Method | Purpose | Request DTO | Response DTO | FSM Transition | Permission |
|----------|--------|---------|-------------|--------------|----------------|------------|
| `/api/maintenance/assets/{assetId}/status` | GET | Get current operational status | - | `AssetStatusDto` | - | `maintenance.view` |

---

## Work Order Management Endpoints (12)

### CRUD Operations

| Endpoint | Method | Purpose | Request DTO | Response DTO | FSM Transition | Permission |
|----------|--------|---------|-------------|--------------|----------------|------------|
| `/api/maintenance/work-orders` | GET | List work orders with filters | query params (assetId, type, status, priority, assignedTechnicianId, startDate, endDate) | `WorkOrderDto[]` | - | `maintenance.view` |
| `/api/maintenance/work-orders/{workOrderId}` | GET | Get work order by ID | - | `WorkOrderDto` | - | `maintenance.view` |
| `/api/maintenance/work-orders` | POST | Create work order | `CreateWorkOrderCommand` | `WorkOrderDto` | - | `maintenance.manage` |
| `/api/maintenance/work-orders/{workOrderId}` | PUT | Update work order details | `UpdateWorkOrderCommand` | `WorkOrderDto` | - | `maintenance.manage` |
| `/api/maintenance/work-orders/{workOrderId}` | DELETE | Delete work order | - | `204 No Content` | - | `maintenance.admin` |

### FSM State Transitions

| Endpoint | Method | Purpose | Request DTO | Response DTO | FSM Transition | Permission |
|----------|--------|---------|-------------|--------------|----------------|------------|
| `/api/maintenance/work-orders/{workOrderId}/schedule` | POST | Schedule work order and assign technician | `ScheduleWorkOrderCommand` | `WorkOrderDto` | **Reported -> Scheduled** | `maintenance.manage` |
| `/api/maintenance/work-orders/{workOrderId}/start` | POST | Start work on scheduled work order | `StartWorkOrderCommand` | `WorkOrderDto` | **Scheduled -> InProgress** | `maintenance.technician` |
| `/api/maintenance/work-orders/{workOrderId}/complete` | POST | Complete work order and log actual hours | `CompleteWorkOrderCommand` | `WorkOrderDto` | **InProgress -> Completed** | `maintenance.technician` |
| `/api/maintenance/work-orders/{workOrderId}/postpone` | POST | Postpone work order (reschedule needed) | `PostponeWorkOrderCommand` | `WorkOrderDto` | **Scheduled/InProgress -> Postponed** | `maintenance.manage` |
| `/api/maintenance/work-orders/{workOrderId}/reject` | POST | Reject work order (not executed) | `RejectWorkOrderCommand` | `WorkOrderDto` | **Reported/Scheduled -> Rejected** | `maintenance.manage` |

### Work Order Parts Management

| Endpoint | Method | Purpose | Request DTO | Response DTO | FSM Transition | Permission |
|----------|--------|---------|-------------|--------------|----------------|------------|
| `/api/maintenance/work-orders/{workOrderId}/parts` | POST | Add part to work order | `AddWorkOrderPartCommand` | `WorkOrderDto` | - | `maintenance.technician` |
| `/api/maintenance/work-orders/{workOrderId}/parts/{partId}` | DELETE | Remove part from work order | - | `WorkOrderDto` | - | `maintenance.technician` |

---

## Maintenance Plan Endpoints (6)

| Endpoint | Method | Purpose | Request DTO | Response DTO | FSM Transition | Permission |
|----------|--------|---------|-------------|--------------|----------------|------------|
| `/api/maintenance/assets/{assetId}/maintenance-plans` | GET | List maintenance plans for asset | - | `MaintenancePlanDto[]` | - | `maintenance.view` |
| `/api/maintenance/assets/{assetId}/maintenance-plans` | POST | Create maintenance plan | `CreateMaintenancePlanCommand` | `MaintenancePlanDto` | - | `maintenance.manage` |
| `/api/maintenance/assets/{assetId}/maintenance-plans/{planId}` | PUT | Update maintenance plan | `UpdateMaintenancePlanCommand` | `MaintenancePlanDto` | - | `maintenance.manage` |
| `/api/maintenance/assets/{assetId}/maintenance-plans/{planId}` | DELETE | Delete maintenance plan | - | `204 No Content` | - | `maintenance.admin` |
| `/api/maintenance/maintenance-plans/due` | GET | Get due maintenance plans (within N days) | query param: `withinDays` (default: 7) | `DueMaintenancePlanDto[]` | - | `maintenance.view` |
| `/api/maintenance/maintenance-plans/overdue` | GET | Get overdue maintenance plans | - | `DueMaintenancePlanDto[]` | - | `maintenance.view` |

---

## Downtime Tracking Endpoints (4)

| Endpoint | Method | Purpose | Request DTO | Response DTO | FSM Transition | Permission |
|----------|--------|---------|-------------|--------------|----------------|------------|
| `/api/maintenance/downtimes` | GET | List all downtimes with filters | query params (assetId, startDate, endDate) | `DowntimeDto[]` | - | `maintenance.view` |
| `/api/maintenance/assets/{assetId}/downtimes` | GET | List downtimes for specific asset | - | `DowntimeDto[]` | - | `maintenance.view` |
| `/api/maintenance/downtimes/{year}/{month}` | GET | Get downtime summary for month (all assets) | - | `DowntimeSummaryDto[]` | - | `maintenance.view` |
| `/api/maintenance/downtimes/active` | GET | Get active downtimes (Production integration) | - | `ActiveDowntimeDto[]` | - | `maintenance.view` |

---

## Data Models

### Request DTOs (Commands)

| DTO | Used By | Key Fields |
|-----|---------|------------|
| **CreateAssetCommand** | POST /assets | code, name, kind, facilityId, location, vendor, model, serialNumber, purchasedAt, purchaseValue, machineId, vehicleId |
| **UpdateAssetCommand** | PUT /assets/{id} | name, location, vendor, model, serialNumber (partial updates) |
| **RecordOperatingHoursCommand** | POST /assets/{id}/record-hours | hours, recordedAt |
| **CreateWorkOrderCommand** | POST /work-orders | assetId, type, priority, description, requiresDowntime |
| **UpdateWorkOrderCommand** | PUT /work-orders/{id} | description, priority, requiresDowntime (Reported only) |
| **ScheduleWorkOrderCommand** | POST /work-orders/{id}/schedule | scheduledDate, assignedTechnicianId, estimatedHours |
| **StartWorkOrderCommand** | POST /work-orders/{id}/start | startedAt, actualTechnicianId |
| **CompleteWorkOrderCommand** | POST /work-orders/{id}/complete | completedAt, actualHours, workDone, nextInspectionDate |
| **PostponeWorkOrderCommand** | POST /work-orders/{id}/postpone | postponeReason, rescheduledDate |
| **RejectWorkOrderCommand** | POST /work-orders/{id}/reject | rejectionReason |
| **AddWorkOrderPartCommand** | POST /work-orders/{id}/parts | partCode, quantity, unitPrice, source |
| **CreateMaintenancePlanCommand** | POST /assets/{id}/maintenance-plans | label, kind, trigger, intervalDays, operatingHoursInterval |
| **UpdateMaintenancePlanCommand** | PUT /assets/{id}/maintenance-plans/{planId} | label, intervalDays, operatingHoursInterval |

### Response DTOs

| DTO | Used By | Key Fields |
|-----|---------|------------|
| **AssetDto** | GET /assets, POST/PUT /assets | assetId, code, name, kind, facilityId, location, vendor, model, serialNumber, purchasedAt, purchaseValue, operatingHours, machineId, vehicleId, status, maintenancePlans |
| **AssetStatusDto** | GET /assets/{id}/status | assetId, status, activeWorkOrderId, estimatedAvailableAt |
| **WorkOrderDto** | GET /work-orders, POST/PUT /work-orders, FSM transitions | workOrderId, assetId, type, priority, status, description, requiresDowntime, assignedTechnicianId, scheduledDate, estimatedHours, startedAt, completedAt, actualHours, workDone, parts, downtime |
| **WorkOrderPartDto** | Nested in WorkOrderDto | partId, partCode, quantity, unitPrice, source |
| **MaintenancePlanDto** | GET /maintenance-plans | planId, assetId, label, kind, trigger, intervalDays, operatingHoursInterval, lastDone, lastDoneHours, nextDue, isOverdue |
| **DueMaintenancePlanDto** | GET /maintenance-plans/due, /overdue | planId, assetId, assetCode, assetName, kind, trigger, dueDate, overdueDays |
| **DowntimeDto** | GET /downtimes | downtimeId, assetId, workOrderId, startedAt, endedAt, durationMinutes, reason, affectsProduction |
| **DowntimeSummaryDto** | GET /downtimes/{year}/{month} | assetId, assetCode, assetName, totalDowntimeHours, downtimeCount, averageDowntimeMinutes |
| **ActiveDowntimeDto** | GET /downtimes/active | workOrderId, assetId, machineId, downtimeStart, estimatedEnd, isPlannedDowntime |

### Enums

| Enum | Values | Usage |
|------|--------|-------|
| **AssetKind** | Machine, Vehicle, Tool, Infrastructure, IT, Room | Asset categorization |
| **AssetStatus** | Operational, UnderMaintenance, TemporarilyRetired, Retired | Asset operational status (calculated) |
| **WorkOrderType** | Corrective, Preventive, Cleaning | Work order category |
| **WorkOrderPriority** | Critical, High, Medium, Low | Work order urgency level |
| **WorkOrderStatus** | Reported, Scheduled, InProgress, Completed, Postponed, Rejected | FSM state machine |
| **MaintenancePlanKind** | Preventive, Inspection | Maintenance plan category |
| **MaintenanceTrigger** | Interval, OperatingHours | Maintenance plan trigger type |

---

## FSM Transition Rules (WorkOrder Workflow)

| From | To | Trigger Endpoint | Permission | Validation Rules |
|------|-----|------------------|------------|------------------|
| **Reported** | **Scheduled** | POST /work-orders/{id}/schedule | `maintenance.manage` | Technician exists, scheduled date >= today, estimated hours > 0 |
| **Reported** | **Rejected** | POST /work-orders/{id}/reject | `maintenance.manage` | Rejection reason required (min 10 chars) |
| **Scheduled** | **InProgress** | POST /work-orders/{id}/start | `maintenance.technician` | Scheduled date reached, technician assigned |
| **Scheduled** | **Postponed** | POST /work-orders/{id}/postpone | `maintenance.manage` | Postpone reason required (min 10 chars), rescheduled date > today |
| **Scheduled** | **Rejected** | POST /work-orders/{id}/reject | `maintenance.manage` | Rejection reason required (min 10 chars) |
| **InProgress** | **Completed** | POST /work-orders/{id}/complete | `maintenance.technician` | Actual hours > 0, work done description required |
| **InProgress** | **Postponed** | POST /work-orders/{id}/postpone | `maintenance.manage` | Postpone reason required (min 10 chars), rescheduled date > today |
| **Postponed** | **Reported** | - | - | Automatic reopen when rescheduled date is set |
| **Rejected** | **Reported** | - | - | Automatic reopen (manual review required) |

**Terminal States:** Completed

**Blocking Statuses:** InProgress (if requiresDowntime=true) — blocks production capacity on linked machine

**Auto-Reopen:** Postponed and Rejected work orders automatically return to Reported status

---

## Integration Contracts

### Exported to Production Module

| DTO | Purpose | Key Fields |
|-----|---------|------------|
| **AssetDowntimeData** | Downtime blocking integration | assetId, machineId (Production SHOPFLOOR_MACHINES reference), downtimeStart, downtimeEnd, isPlannedDowntime, workOrderId |

**Integration Pattern:**
- Work Order with `requiresDowntime=true` + `status=InProgress` → Production blocks machine capacity for `machineId`
- Work Order completion → Production releases capacity
- `GET /downtimes/active` provides list of active downtimes with machineId references

### Exported to Kontrolling Module

| DTO | Purpose | Key Fields |
|-----|---------|------------|
| **MaintenanceCostData** | Maintenance cost tracking | workOrderId, assetId, type, laborCost (TechnicianHourlyRate × ActualHours × 1.9), partsCost (sum of Quantity × UnitPrice), externalCost (contractor fee), totalCost, completedAt |

**Integration Pattern:**
- Work Order completion → Kontrolling reads cost breakdown
- Labor cost calculation: `TechnicianHourlyRate × ActualHours × 1.9`
- Parts cost: Sum of (Quantity × UnitPrice) for all WorkOrderParts
- Kontrolling integrates maintenance costs into project cost tracking

### Integration with Procurement Module

| Reference | Purpose |
|-----------|---------|
| **WorkOrderPart.partCode** | References Procurement.PartCode |

**Integration Pattern:**
- Work Order completion → Stock decremented for each WorkOrderPart
- Parts with `source=FromStock` decrease inventory
- Parts with `source=Purchased` or `source=FromSupplier` bypass inventory

### Integration with HR Module

| Reference | Purpose |
|-----------|---------|
| **WorkOrder.assignedTechnicianId** | References HR Employee UUID |

**Integration Pattern:**
- Work Order scheduling → HR CapacityCalculationService includes maintenance assignments in daily load
- Technician availability query before scheduling: `HR.GetTechnicianAvailabilityAsync`
- HR provides technician skills for matching to work order requirements

---

## Database Schema (Maintenance owns)

| Table | Purpose |
|-------|---------|
| `maintenance.assets` | Asset registry (code, name, kind, facilityId, location, vendor, model, serialNumber, purchasedAt, purchaseValue, operatingHours, machineId, vehicleId, retired) |
| `maintenance.work_orders` | Work order records (assetId, type, priority, status, description, requiresDowntime, assignedTechnicianId, scheduledDate, estimatedHours, startedAt, completedAt, actualHours, workDone, postponeReason, rescheduledDate, rejectionReason) |
| `maintenance.maintenance_plans` | Preventive maintenance plans (assetId, label, kind, trigger, intervalDays, operatingHoursInterval, lastDone, lastDoneHours) |
| `maintenance.work_order_parts` | Parts consumed in work orders (workOrderId, partCode, quantity, unitPrice, source) |
| `maintenance.downtimes` | Downtime records (assetId, workOrderId, startedAt, endedAt, durationMinutes, reason, affectsProduction) |

**Readonly joins to other modules:**
- Production module (SHOPFLOOR_MACHINES for downtime blocking)
- HR module (Employees for technician assignment)
- Procurement module (PartCode for parts reference)
- Logistics module (VEHICLES for vehicle assets)

---

## Security & Permissions

### Authentication
- **Bearer JWT** (HttpOnly cookie in production)
- **Token expiry:** 1 hour (access token), 7 days (refresh token)

### RBAC Permissions

| Permission | Scope | Allowed Operations |
|------------|-------|-------------------|
| **maintenance.view** | Read-only access to assets, work orders, maintenance plans, downtimes | GET endpoints |
| **maintenance.manage** | Manager access | Create/update assets, schedule/reject work orders, manage maintenance plans |
| **maintenance.technician** | Technician access | Start/complete/postpone work orders, add/remove parts |
| **maintenance.admin** | Admin access | Retire/reactivate assets, delete work orders, delete maintenance plans, access cost data |

### Row Level Security (RLS)

- **Technicians can view/edit their own assigned work orders**
- **Managers can schedule/reject work orders** for their facility
- **Maintenance admins can manage all assets** across all facilities
- **Asset operational status** visible to all users with `maintenance.view`

---

## Error Responses (Standardized)

| Status Code | Error Code | Description |
|-------------|------------|-------------|
| **400** | VALIDATION_FAILED | Validation error (e.g., invalid date range, missing required field) |
| **401** | UNAUTHORIZED | Token expired (refresh token required) |
| **403** | FORBIDDEN | Permission denied (e.g., non-manager trying to schedule work order) |
| **404** | NOT_FOUND | Resource not found (asset, work order, maintenance plan) |
| **409** | CONFLICT | Asset code already exists |
| **422** | STATE_INVALID | Invalid FSM transition (e.g., complete work order that is not InProgress) |
| **500** | INTERNAL_ERROR | Server error |

### Special: StateError (422)

**When:** Invalid FSM transition attempted

**Response Body:**
```json
{
  "code": "STATE_INVALID",
  "message": "Cannot transition from Scheduled to Completed",
  "currentStatus": "Scheduled",
  "allowedTransitions": ["InProgress", "Postponed", "Rejected"],
  "timestamp": "2026-07-04T10:00:00Z"
}
```

---

## Production Integration Details

### Downtime Blocking Logic

**Trigger:** Work Order with `requiresDowntime=true` + `status=InProgress`

**Action:**
1. Maintenance publishes `MachineDowntimeStartedEvent` with `machineId` (if Asset.machineId is not null)
2. Production blocks capacity for `machineId` during downtime period
3. Production scheduling skips downtime-blocked machines

**Completion:**
1. Work Order completion publishes `MachineDowntimeEndedEvent`
2. Production releases capacity for `machineId`

**Active Downtimes Endpoint:**
```http
GET /api/maintenance/downtimes/active

Response:
[
  {
    "workOrderId": "123e4567-e89b-12d3-a456-426614174000",
    "assetId": "789e4567-e89b-12d3-a456-426614174000",
    "machineId": "456e4567-e89b-12d3-a456-426614174000",
    "downtimeStart": "2026-07-04T08:00:00Z",
    "estimatedEnd": "2026-07-04T12:00:00Z",
    "isPlannedDowntime": true
  }
]
```

**Production Module Integration:**
- Production reads active downtimes before scheduling jobs
- Production capacity planner excludes downtime-blocked machines
- Production dashboard shows machines under maintenance

---

## Preventive Maintenance Scheduling Logic

### Interval Trigger (Time-Based)

**Formula:**
```
isDue = (LastDone + IntervalDays) <= (Today + withinDays)
```

**Example:**
- Last done: 2026-06-01
- Interval: 30 days
- Today: 2026-07-01
- Within days: 7
- Due date: 2026-07-01 (LastDone + Interval = 2026-06-01 + 30 = 2026-07-01)
- Is due? Yes (2026-07-01 <= 2026-07-08)

### OperatingHours Trigger (Usage-Based)

**Formula:**
```
isDue = (CurrentHours - LastDoneHours) >= (OperatingHoursInterval - threshold)
```

**Example:**
- Last done hours: 1000
- Operating hours interval: 500
- Current hours: 1480
- Threshold: 50 hours
- Hours since last maintenance: 1480 - 1000 = 480
- Is due? Yes (480 >= 500 - 50 = 450)

---

## Next Steps (After Week 0)

### Backend Implementation (Week 1-5)

**Week 1: Domain Layer**
- Asset aggregate (CRUD, operating hours, retirement lifecycle)
- WorkOrder aggregate (FSM state machine, parts management, downtime)
- MaintenancePlan entity (preventive scheduling)
- WorkOrderPart entity (parts consumption)

**Week 2: Application Layer**
- CQRS command handlers (Create, Update, Schedule, Start, Complete, Postpone, Reject)
- CQRS query handlers (ListAssets, ListWorkOrders, GetDueMaintenance Plans, GetActiveDowntimes)
- AssetStatusCalculationService, PreventiveMaintenanceSchedulerService, MaintenanceCostEstimatorService

**Week 3: Infrastructure Layer**
- EF Core configuration (assets, work_orders, maintenance_plans, work_order_parts, downtimes tables)
- RLS policies (facility-based, technician-based)
- Integration queries (Production machine downtime, HR technician availability, Procurement part prices)
- Database migrations

**Week 4: API Layer**
- ASP.NET Core controllers (31 endpoints)
- Validation attributes (dates, FSM transitions, downtime logic)
- Authorization policies (maintenance.view, maintenance.manage, maintenance.technician, maintenance.admin)
- Swagger documentation

**Week 5: Testing**
- Unit tests (FSM state machine, downtime calculation, preventive maintenance scheduling)
- Integration tests (CRUD, FSM transitions, production integration)
- E2E test: Create asset -> Create work order -> Schedule -> Start -> Complete -> Check downtime

### Frontend Implementation (Week 1.5+)

**Week 1.5: MSW Mock API Setup**
- Mock API handlers (31 endpoints)
- Feature flag for mock/real API swap
- React Query hooks integration (Orval generated)

**Week 2.5: UI Components**
- Asset list & detail view (asset card, operating hours chart)
- Work order list & detail view (FSM status indicator, Gantt timeline)
- Maintenance plan calendar (due/overdue indicator)
- Downtime dashboard (active downtimes, production impact)

---

**Status:** Endpoint inventory matrix completed
**Validation:** Pending (redocly lint)
**Code Generation:** Pending (Orval test)
**Next:** Spec validation and code-gen test

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>
