---
id: MSG-ARCHITECT-062
from: conductor
to: architect
type: task
priority: high
status: READ
model: opus
epic_id: EPIC-JT-MAINT
ref: MSG-ARCHITECT-047
created: 2026-07-04
estimated_nwt: 120
content_hash: b990dfc6ee5d1c40a736469e42faa2624b39f24c2fd598601028505034c68e2d
---

# JoineryTech Maintenance & Asset Management — Week 0 Contract-First OpenAPI Spec

**Epic:** EPIC-JT-MAINT (Karbantartás & Eszközgazdálkodás Modul)
**Priority:** HIGH (JoineryTech parallel development wave 2)
**Estimated:** 120 NWT (~4 hours)
**Dependency Status:** ✅ UNBLOCKED (EPIC-KERNEL-STABLE + EPIC-INVENTORY-V1 DONE)

---

## Context

EPIC-JT-MAINT (Maintenance & Asset Management modul) **párhuzamosan futhat** EPIC-JT-HR-rel és EPIC-JT-QA-vel. Domain model kész (MSG-ARCHITECT-047), most a Contract-First Week 0 OpenAPI spec következik.

**Parallel Development Wave 2:**
- 🔄 **CRM**: Backend API implementáció (MSG-BACKEND-103 in progress)
- 🔄 **Dashboard**: Frontend widgets (MSG-FRONTEND-105 in progress)
- ✅ **Kontrolling**: Week 0 DONE (OpenAPI spec locked), Week 1 queued
- ✅ **HR**: Week 0 in progress (OpenAPI spec being created)
- 🆕 **Maintenance**: Week 0 OpenAPI spec (THIS TASK) → Backend Week 1 (queued) → Frontend Week 1.5 (MSW)

**Goal:** Lock OpenAPI contract BEFORE implementation starts → prevent integration rework

---

## Task: Maintenance Module OpenAPI 3.1 Specification

**Reference:** MSG-ARCHITECT-047 (JoineryTech Maintenance Domain Model)

**Contract-First Workflow:**
- **Day 1-2:** Endpoint inventory + data model definition
- **Day 3:** Draft OpenAPI spec (Asset CRUD + WorkOrder FSM + Preventive maintenance scheduling)
- **Day 4:** Spec lock + validation + code-gen test

**ROI:** $4k Week 0 → $11-16k savings (2 weeks integration rework prevented)

---

## Deliverables

### 1. OpenAPI 3.1 Spec File

**Location:** `/opt/spaceos/docs/api/joinerytech-maintenance-v1.yaml`

**Sections:**

#### A. Asset Management (CRUD + Operating Hours + Retirement)

Based on MSG-ARCHITECT-047 "Asset Aggregate":

```yaml
# Asset CRUD endpoints:
GET    /api/maintenance/assets
GET    /api/maintenance/assets/{assetId}
POST   /api/maintenance/assets
PUT    /api/maintenance/assets/{assetId}
DELETE /api/maintenance/assets/{assetId}

# Operating hours tracking:
POST   /api/maintenance/assets/{assetId}/record-hours

# Asset lifecycle:
POST   /api/maintenance/assets/{assetId}/retire
POST   /api/maintenance/assets/{assetId}/reactivate

# Asset status query:
GET    /api/maintenance/assets/{assetId}/status
```

**Request/Response Models:**
- `CreateAssetCommand` (code, name, kind, facilityId, location, vendor, model, serialNumber, purchasedAt, purchaseValue)
- `UpdateAssetCommand` (partial updates allowed)
- `AssetDto` (full asset data with operating hours, maintenance plans, current status)
- `RecordOperatingHoursCommand` (hours, recordedAt)
- `AssetStatusDto` (status: Operational, UnderMaintenance, TemporarilyRetired, Retired)

#### B. Work Order Management (FSM + Assignment + Parts)

Based on MSG-ARCHITECT-047 "WorkOrder Aggregate":

```yaml
# WorkOrder CRUD + FSM transitions:
GET    /api/maintenance/work-orders
GET    /api/maintenance/work-orders/{workOrderId}
POST   /api/maintenance/work-orders
PUT    /api/maintenance/work-orders/{workOrderId}
DELETE /api/maintenance/work-orders/{workOrderId}

# FSM state transitions:
POST   /api/maintenance/work-orders/{workOrderId}/schedule
POST   /api/maintenance/work-orders/{workOrderId}/start
POST   /api/maintenance/work-orders/{workOrderId}/complete
POST   /api/maintenance/work-orders/{workOrderId}/postpone
POST   /api/maintenance/work-orders/{workOrderId}/reject

# Work order parts:
POST   /api/maintenance/work-orders/{workOrderId}/parts
DELETE /api/maintenance/work-orders/{workOrderId}/parts/{partId}
```

**Request/Response Models:**
- `CreateWorkOrderCommand` (assetId, type, priority, description, requiresDowntime)
- `WorkOrderDto` (workOrderId, assetId, type, priority, status, assignedTechnician, estimatedHours, parts, downtime)
- `ScheduleWorkOrderCommand` (scheduledDate, assignedTechnicianId, estimatedHours)
- `StartWorkOrderCommand` (startedAt, actualTechnicianId)
- `CompleteWorkOrderCommand` (completedAt, actualHours, workDone, nextInspectionDate)
- `PostponeWorkOrderCommand` (postponeReason, rescheduledDate)
- `RejectWorkOrderCommand` (rejectionReason)
- `AddWorkOrderPartCommand` (partCode, quantity, unitPrice, source)

**FSM Status Enum:**
```typescript
enum WorkOrderStatus {
  Reported,     // Initial state
  Scheduled,    // Manager scheduled, technician assigned
  InProgress,   // Work started (downtime begins if requiresDowntime=true)
  Completed,    // Work finished (terminal state)
  Postponed,    // Delayed, can be rescheduled
  Rejected      // Not executed, can be reopened
}
```

#### C. Preventive Maintenance Plans (Scheduling + Due Detection)

Based on MSG-ARCHITECT-047 "MaintenancePlan Entity":

```yaml
# Maintenance plans:
GET /api/maintenance/assets/{assetId}/maintenance-plans
POST /api/maintenance/assets/{assetId}/maintenance-plans
PUT /api/maintenance/assets/{assetId}/maintenance-plans/{planId}
DELETE /api/maintenance/assets/{assetId}/maintenance-plans/{planId}

# Due plans detection:
GET /api/maintenance/maintenance-plans/due
GET /api/maintenance/maintenance-plans/overdue
```

**Response Models:**
- `MaintenancePlanDto` (planId, assetId, label, kind, trigger, intervalDays, operatingHoursInterval, lastDone, lastDoneHours, nextDue, isOverdue)
- `DueMaintenancePlanDto` (planId, assetId, assetCode, assetName, kind, trigger, dueDate, overdueDays)

**Maintenance Plan Triggers:**
```typescript
enum MaintenanceTrigger {
  Interval,          // Time-based (every N days)
  OperatingHours     // Usage-based (every N hours)
}
```

#### D. Downtime Tracking (Query + Integration with Production)

Based on MSG-ARCHITECT-047 "Downtime Value Object":

```yaml
# Downtime queries:
GET /api/maintenance/downtimes
GET /api/maintenance/assets/{assetId}/downtimes
GET /api/maintenance/downtimes/{year}/{month}
```

**Response Models:**
- `DowntimeDto` (downtimeId, assetId, workOrderId, startedAt, endedAt, durationMinutes, reason, affectsProduction)
- `DowntimeSummaryDto` (assetId, assetCode, assetName, totalDowntimeHours, downtimeCount, averageDowntimeMinutes)

#### E. Data Models (Schemas)

**Domain Entities:**
- `Asset` (stored entity with maintenance plans, operating hours, retirement status)
- `WorkOrder` (stored entity with FSM status, parts, downtime)
- `MaintenancePlan` (owned by Asset, preventive scheduling)
- `WorkOrderPart` (owned by WorkOrder, parts consumed)
- `Downtime` (value object, work order downtime period)

**Enums:**
```typescript
enum AssetKind {
  Machine,        // Production machinery
  Vehicle,        // Logistics vehicles
  Tool,           // Hand tools, power tools
  Infrastructure, // Building infrastructure (HVAC, electrical)
  IT,             // Computers, servers, network equipment
  Room            // Facility rooms (meeting rooms, storage)
}

enum WorkOrderType {
  Corrective,  // Reactive maintenance (breakdown)
  Preventive,  // Scheduled preventive maintenance
  Cleaning     // Routine cleaning
}

enum WorkOrderPriority {
  Critical,    // Immediate attention (production stopped)
  High,        // Within 24 hours
  Medium,      // Within 3 days
  Low          // Within 1 week
}

enum AssignmentType {
  Internal,    // Internal technician
  External     // External contractor
}

enum MaintenancePlanKind {
  Preventive,  // Preventive maintenance
  Inspection   // Periodic inspection
}

enum AssetStatus {
  Operational,        // Available for use
  UnderMaintenance,   // Currently being maintained (InProgress WorkOrder)
  TemporarilyRetired, // Temporarily out of service
  Retired             // Permanently retired
}
```

### 2. Endpoint Inventory Matrix

Create a spreadsheet or markdown table:

| Endpoint | Method | Purpose | Request DTO | Response DTO | FSM Transition | Permission |
|----------|--------|---------|-------------|--------------|----------------|------------|
| `/assets` | GET | List assets | - | AssetDto[] | - | maintenance.view |
| `/assets/{id}` | GET | Get asset | - | AssetDto | - | maintenance.view |
| `/assets` | POST | Create asset | CreateAssetCommand | AssetDto | - | maintenance.manage |
| `/work-orders/{id}/schedule` | POST | Schedule work order | ScheduleWorkOrderCommand | WorkOrderDto | Reported → Scheduled | maintenance.manage |
| `/work-orders/{id}/start` | POST | Start work order | StartWorkOrderCommand | WorkOrderDto | Scheduled → InProgress | maintenance.technician |
| `/maintenance-plans/due` | GET | Get due plans | withinDays (query) | DueMaintenancePlanDto[] | - | maintenance.view |
| ... | ... | ... | ... | ... | ... | ... |

**Total Estimated:** 30 endpoints (9 Asset + 12 WorkOrder + 5 Maintenance Plans + 4 Downtime)

### 3. FSM Transition Rules

**WorkOrder FSM Definition:**

```yaml
components:
  schemas:
    WorkOrderFSMTransition:
      type: object
      properties:
        from:
          type: string
          enum: [Reported, Scheduled, InProgress, Postponed, Rejected, Completed]
        to:
          type: string
          enum: [Reported, Scheduled, InProgress, Postponed, Rejected, Completed]
        trigger:
          type: string
          description: API endpoint that triggers this transition
        requiredPermission:
          type: string
          description: Permission required to execute transition
        validations:
          type: array
          items:
            type: string
          description: Business rules validated during transition
```

**Transition Matrix:**
| From | To | Trigger Endpoint | Permission | Validation Rules |
|------|-----|------------------|------------|------------------|
| Reported | Scheduled | POST /work-orders/{id}/schedule | maintenance.manage | Technician exists, scheduled date >= today, estimated hours > 0 |
| Reported | Rejected | POST /work-orders/{id}/reject | maintenance.manage | Rejection reason required (min 10 chars) |
| Scheduled | InProgress | POST /work-orders/{id}/start | maintenance.technician | Scheduled date reached, technician assigned |
| InProgress | Completed | POST /work-orders/{id}/complete | maintenance.technician | Actual hours > 0, work done description required |
| InProgress | Postponed | POST /work-orders/{id}/postpone | maintenance.manage | Postpone reason required, rescheduled date > today |
| Scheduled | Postponed | POST /work-orders/{id}/postpone | maintenance.manage | Postpone reason required, rescheduled date > today |
| Postponed | Reported | - | - | Automatic reopen when rescheduled date is set |
| Rejected | Reported | - | - | Automatic reopen when rejection is reviewed |

### 4. Integration with Other Modules

**Maintenance Module provides data to:**
- **Production:** Asset downtime blocking (InProgress work orders with requiresDowntime=true block machine capacity)
- **Kontrolling:** Maintenance cost tracking (labor + parts + external contractor fees)
- **HR:** Technician workload and scheduling
- **Procurement:** Spare parts requisition (WorkOrderPart → PartCode reference)

**Integration DTOs (exported by Maintenance):**
```yaml
# AssetDowntimeData (consumed by Production)
AssetDowntimeData:
  type: object
  properties:
    assetId:
      type: string
      format: uuid
    machineId:
      type: string
      description: Reference to Production SHOPFLOOR_MACHINES
    downtimeStart:
      type: string
      format: date-time
    downtimeEnd:
      type: string
      format: date-time
      nullable: true
    isPlannedDowntime:
      type: boolean
    workOrderId:
      type: string
      format: uuid

# MaintenanceCostData (consumed by Kontrolling)
MaintenanceCostData:
  type: object
  properties:
    workOrderId:
      type: string
      format: uuid
    assetId:
      type: string
      format: uuid
    type:
      type: string
      enum: [Corrective, Preventive, Cleaning]
    laborCost:
      type: number
      description: TechnicianHourlyRate × ActualHours × 1.9
    partsCost:
      type: number
      description: Sum of (Quantity × UnitPrice) for all parts
    externalCost:
      type: number
      description: External contractor fee (if AssignmentType=External)
    totalCost:
      type: number
    completedAt:
      type: string
      format: date-time
```

### 5. Validation & Code-Gen Test

**Validation:**
```bash
# OpenAPI validation
npx @redocly/cli lint docs/api/joinerytech-maintenance-v1.yaml

# Schema validation (no $ref errors, all required fields)
```

**Code-Gen Test (Frontend):**
```bash
cd datahaven-web/client
npx orval --config orval.maintenance.config.ts

# Expected output:
# - src/api/maintenance/maintenance.ts (TanStack Query hooks)
# - src/api/maintenance/model/*.ts (TypeScript types)
```

**Code-Gen Test (Backend):**
```bash
cd spaceos-modules-maintenance/Api
dotnet add package NSwag.MSBuild

# Expected output:
# - Generated C# client (verify DTO types match domain model)
```

---

## Technical Constraints

### 1. Domain Model Compliance (MSG-ARCHITECT-047)

**MUST align with Maintenance Domain Model:**
- ✅ 2 Aggregates: Asset (CRUD + operating hours + retirement), WorkOrder (FSM workflow)
- ✅ 2 Entities: MaintenancePlan (preventive scheduling), WorkOrderPart (parts consumed)
- ✅ 7 Value Objects: Downtime, AssetKind, WorkOrderType, WorkOrderPriority, AssignmentType, MaintenancePlanKind, MaintenanceTrigger
- ✅ 3 Domain Services: AssetStatusCalculationService, PreventiveMaintenanceSchedulerService, MaintenanceCostEstimatorService
- ✅ FSM: Reported → Scheduled → InProgress → Completed (or Postponed/Rejected → Reported)
- ✅ Downtime blocking: InProgress + RequiresDowntime=true blocks production capacity

### 2. OpenAPI 3.1 Standards

```yaml
openapi: 3.1.0
info:
  title: JoineryTech Maintenance & Asset Management API
  version: 1.0.0
  description: |
    Asset registry, work order management (FSM), preventive maintenance scheduling, downtime tracking.
    Production integration: downtime blocks machine capacity.
servers:
  - url: https://api.joinerytech.local
    description: Local development
components:
  securitySchemes:
    BearerAuth:
      type: http
      scheme: bearer
      bearerFormat: JWT
security:
  - BearerAuth: []
```

### 3. Production Integration (Downtime Blocking)

**Downtime Blocking Logic:**
```yaml
# Work Order with downtime blocks production capacity
WorkOrder:
  requiresDowntime: true
  status: InProgress
  →
  Asset.MachineId (if not null) → Production capacity blocked
```

**Implementation:**
```yaml
# GET /api/maintenance/downtimes/active
ActiveDowntimesDto:
  properties:
    machineId:
      type: string
      description: Production SHOPFLOOR_MACHINES.id
    downtimeStart:
      type: string
      format: date-time
    estimatedEnd:
      type: string
      format: date-time
      nullable: true
    workOrderId:
      type: string
      format: uuid
```

### 4. RBAC Permissions

**Permission Levels:**
- `maintenance.view` — View assets, work orders, maintenance plans (read-only)
- `maintenance.manage` — Create/update/delete assets, schedule/reject work orders, manage maintenance plans
- `maintenance.technician` — Start/complete/postpone work orders (technician role)
- `maintenance.admin` — Retire assets, delete work orders, access cost data

**RLS (Row Level Security):**
- Technicians can view/edit their own assigned work orders
- Managers can schedule/reject work orders for their facility
- Maintenance admins can manage all assets across all facilities

---

## Acceptance Criteria

**DONE when:**
- [ ] OpenAPI 3.1 spec file: `/opt/spaceos/docs/api/joinerytech-maintenance-v1.yaml`
- [ ] 30 endpoints defined (9 Asset + 12 WorkOrder + 5 Maintenance Plans + 4 Downtime)
- [ ] All DTOs/schemas match MSG-ARCHITECT-047 domain model
- [ ] FSM transition rules defined (7 transitions: schedule, start, complete, postpone, reject, retire, reactivate)
- [ ] Endpoint inventory matrix created (Markdown table)
- [ ] Production integration downtime blocking defined (active downtimes endpoint)
- [ ] Integration DTOs defined (AssetDowntimeData for Production, MaintenanceCostData for Kontrolling)
- [ ] Validation passes: `npx @redocly/cli lint`
- [ ] Code-gen test passes: Orval (Frontend), NSwag (Backend)
- [ ] No $ref errors, all required fields present
- [ ] Security: Bearer JWT auth + RBAC permissions defined

**Quality Gates:**
- Spec lock commit: Tag `maintenance-spec-v1.0.0`
- Review by Conductor (contract clarity, FSM feasibility, production integration)
- Approved before Backend Week 1 starts

---

## Integration with Existing Work

**Domain Model Implementation Plan (Week 1-5):**
- ✅ **Week 0** (THIS TASK): OpenAPI spec lock
- ⏳ **Week 1**: Domain layer (Asset, WorkOrder aggregates, FSM)
- ⏳ **Week 2**: Application layer (CQRS handlers, PreventiveMaintenanceSchedulerService, MaintenanceCostEstimatorService)
- ⏳ **Week 3**: Infrastructure layer (EF Core, RLS policies, database schema)
- ⏳ **Week 4**: API layer (controllers, validation, FSM transition enforcement)
- ⏳ **Week 5**: Testing (unit, integration, FSM state machine tests, production integration tests)

**Parallel Development Unlock:**
- After Week 0 spec lock → Backend starts Week 1 → Frontend starts Week 1.5 (MSW mock API)
- No integration rework (contract locked upfront)

---

## References

- **Domain Model:** `/opt/spaceos/docs/joinerytech/domain/MAINTENANCE_DOMAIN_MODEL.md` (11,000+ words)
- **Architect Work:** `terminals/architect/outbox/2026-07-02_047_joinerytech-maintenance-domain-model-done.md`
- **Contract-First Pattern:** `/opt/spaceos/docs/knowledge/patterns/CONTRACT_FIRST_DEVELOPMENT.md`
- **HR OpenAPI Example:** Previous Week 0 work for reference pattern (MSG-ARCHITECT-061)
- **Kontrolling OpenAPI Example:** `docs/api/joinerytech-kontrolling-v1.yaml` (recently completed)
- **Code-Gen Tools:** Orval (Frontend), NSwag (Backend)

---

## Priority Rationale

**Why HIGH priority:**
- ✅ EPIC-JT-MAINT **unblocked** (EPIC-KERNEL-STABLE + EPIC-INVENTORY-V1 done)
- ✅ **Parallel development** with EPIC-JT-HR + EPIC-JT-QA (maximize throughput)
- ✅ Contract-First **prevents 2 weeks integration rework** ($11-16k savings)
- ✅ JoineryTech **top focus** (user explicit request)
- ✅ Week 0 spec **enables early Frontend mockup** (MSW parallel track)
- ✅ **Production integration critical** — downtime blocking affects scheduling
- ✅ **Pattern reuse** (same Contract-First workflow as Kontrolling + HR)

**Timeline:**
- Week 0 (4 hours) → Backend Week 1 dispatch (when CRM API completes) → Frontend Week 1.5 (MSW parallel)

---

**Next After Completion:**
When Architect completes OpenAPI spec → Conductor reviews → Backend Maintenance Week 1 task queued (wait for CRM API + Kontrolling Week 1 completion)

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
