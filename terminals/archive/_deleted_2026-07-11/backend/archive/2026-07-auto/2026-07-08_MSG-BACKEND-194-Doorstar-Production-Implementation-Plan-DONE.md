---
id: MSG-BACKEND-194-DONE
from: backend
to: root
type: done
status: READ
ref: MSG-BACKEND-194
epic_id: EPIC-DOORSTAR-SOFTLAUNCH
created: 2026-07-08
content_hash: e233f1cb50b2a069fe918dce3c99716ca827164a1c4479f246d529466933bbe7
---

# Doorstar Production Workflow Tracking — Implementation Plan

**Ügyfél**: Doorstar Kft. (valós production ügyfél)
**Epic**: EPIC-DOORSTAR-SOFTLAUNCH
**Target**: 2026-09-30
**Modul**: SpaceOS.Modules.Production (Layer 2 DRIVER, .NET 8)

---

## Executive Summary

A Doorstar műhely-státusz követés digitalizálása (papír-kanban → mobil-first app):
- **6 STAGE** összevont követés (a 17 mikro-fázis Excel-ben marad)
- **2-szintű FSM**: `ProductionJob.Status` + `WorkflowStep.Status` (6 lépés)
- **Event-driven**: CuttingCompleted auto-trigger, ShippingReady push notifications
- **Timeline**: Backend ~4 nap, Frontend ~2 nap párhuzamosan, E2E 1 nap → **Összesen 5-6 nap**

---

## 1. OpenAPI Contract Draft

### 1.1 Base Path
```
/api/production
```

### 1.2 Endpoints

#### **POST /api/production/jobs**
Új ProductionJob létrehozása (OrderConfirmed event után auto-trigger).

**Request:**
```json
{
  "orderId": "string",           // OrderItem.Id (CRM/Joinery domain)
  "projectName": "DSMR 26144",   // Human-readable project identifier
  "deadline": "2026-09-15",      // ISO 8601 date
  "workflowSteps": [             // 6 STAGE definition
    {
      "stepName": "Szabászat/Előgyártás",
      "estimatedDuration": "P1D" // ISO 8601 duration
    },
    {
      "stepName": "Megmunkálás",
      "estimatedDuration": "P2D"
    },
    {
      "stepName": "Felületkezelés",
      "estimatedDuration": "P1D"
    },
    {
      "stepName": "Összeszerelés",
      "estimatedDuration": "P2D"
    },
    {
      "stepName": "Csomagolás",
      "estimatedDuration": "PT4H"
    },
    {
      "stepName": "Kiszállítható",
      "estimatedDuration": "PT1H"
    }
  ]
}
```

**Response:**
```json
{
  "jobId": "uuid",
  "status": "Queued",
  "currentStepIndex": 0,
  "createdAt": "2026-07-08T22:45:00Z"
}
```

---

#### **GET /api/production/jobs**
Műhelyvezető: aktív projektek listája (mobile kiosk view).

**Query Parameters:**
- `status` (optional): `Queued|InProgress|Completed|ShippingReady`
- `overdueOnly` (optional): `true|false` (csúszó projektek kiemelése)

**Response:**
```json
{
  "jobs": [
    {
      "jobId": "uuid",
      "projectName": "DSMR 26144",
      "deadline": "2026-09-15",
      "currentStage": "Megmunkálás",
      "progress": 33,                // Percentage (2/6 = 33%)
      "status": "InProgress",
      "isOverdue": false,
      "lastUpdated": "2026-07-08T22:45:00Z"
    }
  ],
  "total": 1
}
```

---

#### **GET /api/production/jobs/{jobId}**
Projekt részletei (6 STAGE állapottal).

**Response:**
```json
{
  "jobId": "uuid",
  "projectName": "DSMR 26144",
  "orderId": "ORDER-123",
  "deadline": "2026-09-15",
  "status": "InProgress",
  "currentStepIndex": 1,
  "progress": 33,
  "workflowSteps": [
    {
      "stepId": "uuid",
      "stepName": "Szabászat/Előgyártás",
      "status": "Done",
      "startedAt": "2026-07-01T08:00:00Z",
      "completedAt": "2026-07-01T16:30:00Z",
      "completedBy": "auto:CuttingCompleted"
    },
    {
      "stepId": "uuid",
      "stepName": "Megmunkálás",
      "status": "InProgress",
      "startedAt": "2026-07-02T08:15:00Z",
      "completedAt": null,
      "completedBy": null
    },
    {
      "stepId": "uuid",
      "stepName": "Felületkezelés",
      "status": "Queued",
      "startedAt": null,
      "completedAt": null,
      "completedBy": null
    }
    // ... remaining steps
  ],
  "createdAt": "2026-07-01T07:45:00Z",
  "updatedAt": "2026-07-02T08:15:00Z"
}
```

---

#### **PUT /api/production/jobs/{jobId}/steps/{stepId}/start**
STAGE indítása (Queued → InProgress, sárga).

**Request:**
```json
{
  "startedBy": "user:muhelyvezeto-01"  // User context from auth token
}
```

**Response:**
```json
{
  "success": true,
  "step": {
    "stepId": "uuid",
    "status": "InProgress",
    "startedAt": "2026-07-02T08:15:00Z"
  }
}
```

**Validation:**
- Only current step can be started (FSM constraint)
- Returns `400 Bad Request` if step is not current

---

#### **PUT /api/production/jobs/{jobId}/steps/{stepId}/complete**
STAGE befejezése (InProgress → Done, zöld).

**Request:**
```json
{
  "completedBy": "user:muhelyvezeto-01"
}
```

**Response:**
```json
{
  "success": true,
  "step": {
    "stepId": "uuid",
    "status": "Done",
    "completedAt": "2026-07-02T17:30:00Z"
  },
  "job": {
    "currentStepIndex": 2,  // Advanced to next step
    "progress": 50          // 3/6 = 50%
  }
}
```

**Side Effects:**
- If last step ("Kiszállítható") completed → `ProductionJob.ShippingReady` event published
- Job advances to next step automatically

---

#### **POST /api/production/jobs/{jobId}/steps/{stepId}/photo**
Opcionális fotó upload (Összeszerelés STAGE).

**Request:** `multipart/form-data`
```
photo: <file>
description: "Kész ajtólap - 26144"
```

**Response:**
```json
{
  "success": true,
  "photoUrl": "https://storage.spaceos.hu/production-photos/uuid.jpg",
  "uploadedAt": "2026-07-05T14:20:00Z"
}
```

**Constraints:**
- Max file size: 10MB
- Allowed types: `image/jpeg`, `image/png`
- Stored in object storage (S3-compatible)

---

#### **GET /api/production/overview**
Tulaj/sales: élő áttekintő (összes aktív projekt state).

**Query Parameters:**
- `includeCompleted` (optional): `true|false` (default: false)

**Response:**
```json
{
  "activeJobs": 12,
  "overdueJobs": 3,
  "shippingReadyJobs": 2,
  "jobs": [
    {
      "jobId": "uuid",
      "projectName": "DSMR 26144",
      "currentStage": "Megmunkálás",
      "progress": 33,
      "isOverdue": false,
      "deadline": "2026-09-15"
    },
    {
      "jobId": "uuid",
      "projectName": "DSMR 26189",
      "currentStage": "Összeszerelés",
      "progress": 66,
      "isOverdue": true,  // ⚠️ Red highlight
      "deadline": "2026-07-10"
    }
  ]
}
```

---

### 1.3 DTOs Summary

**ProductionJobDto**
- `jobId: Guid`
- `projectName: string`
- `orderId: string`
- `deadline: DateTime`
- `status: ProductionStatus` (enum: Queued, InProgress, Completed, ShippingReady)
- `currentStepIndex: int`
- `progress: int` (0-100%)
- `isOverdue: bool`
- `workflowSteps: WorkflowStepDto[]`

**WorkflowStepDto**
- `stepId: Guid`
- `stepName: string` (e.g., "Szabászat/Előgyártás")
- `status: StepStatus` (enum: Queued, InProgress, Done)
- `startedAt: DateTime?`
- `completedAt: DateTime?`
- `completedBy: string?` (user:xxx or auto:xxx)

**ProductionOverviewDto**
- `activeJobs: int`
- `overdueJobs: int`
- `shippingReadyJobs: int`
- `jobs: ProductionJobSummaryDto[]`

---

### 1.4 Event Publishing

**Outbound Events:**
- `ProductionJob.ShippingReady` → Inventory.ReserveForShipping
- `ProductionJob.ShippingReady` → Sales notification (Telegram/email - Viber replacement)
- `ProductionJob.WorkflowStepCompleted` → Analytics timeline

**Inbound Events:**
- `CuttingJob.CuttingCompleted` (ADR-038) → Auto-complete "Szabászat/Előgyártás" step
- `OrderItem.OrderConfirmed` (CRM/Joinery) → Create ProductionJob

---

## 2. Backend Task Breakdown (DDD Layers)

### 2.1 Domain Layer
**File**: `SpaceOS.Modules.Production/Domain/`

#### Aggregates
- [ ] **ProductionJob** (aggregate root)
  - `ProductionJobId` value object (Guid wrapper)
  - `ProjectName` value object (string, 3-50 chars)
  - `ProductionDeadline` value object (DateTime with validation)
  - `ProductionStatus` enum (Queued, InProgress, Completed, ShippingReady)
  - `WorkflowStep` entity (6 instances per job)
  - FSM logic: `CanStartStep()`, `CanCompleteStep()`, `AdvanceToNextStep()`
  - Business rules:
    - Only current step can be started/completed
    - Last step completion triggers `ProductionJobShippingReady` event
    - Auto-advance to next step on completion

#### Entities
- [ ] **WorkflowStep**
  - `WorkflowStepId` value object
  - `StepName` value object (enum: Szabászat, Megmunkálás, Felületkezelés, Összeszerelés, Csomagolás, Kiszállítható)
  - `StepStatus` enum (Queued, InProgress, Done)
  - `StartedAt`, `CompletedAt` timestamps
  - `CompletedBy` string (user:xxx or auto:CuttingCompleted)
  - `PhotoUrl` optional (for Összeszerelés stage)

#### Value Objects
- [ ] **ProductionJobId** (Guid wrapper with validation)
- [ ] **WorkflowStepId** (Guid wrapper)
- [ ] **ProjectName** (string, 3-50 chars, regex validation)
- [ ] **ProductionDeadline** (DateTime, must be future date)
- [ ] **WorkflowStepName** (enum with string conversion)

#### Domain Events
- [ ] **ProductionJobCreated**
  - `ProductionJobId`, `ProjectName`, `Deadline`, `CreatedAt`
- [ ] **WorkflowStepStarted**
  - `ProductionJobId`, `StepId`, `StepName`, `StartedAt`, `StartedBy`
- [ ] **WorkflowStepCompleted**
  - `ProductionJobId`, `StepId`, `StepName`, `CompletedAt`, `CompletedBy`
- [ ] **ProductionJobShippingReady**
  - `ProductionJobId`, `ProjectName`, `CompletedAt`
  - **Trigger**: Last step ("Kiszállítható") marked Done

#### Repositories (interfaces)
- [ ] **IProductionJobRepository**
  - `GetByIdAsync(ProductionJobId id, CancellationToken ct)`
  - `GetActiveJobsAsync(ProductionFilter filter, CancellationToken ct)`
  - `GetOverviewAsync(bool includeCompleted, CancellationToken ct)`
  - `AddAsync(ProductionJob job, CancellationToken ct)`
  - `UpdateAsync(ProductionJob job, CancellationToken ct)`

---

### 2.2 Application Layer
**File**: `SpaceOS.Modules.Production/Application/`

#### Commands
- [ ] **CreateProductionJobCommand** + Handler
  - Input: `OrderId`, `ProjectName`, `Deadline`, `WorkflowSteps[]`
  - Output: `Result<ProductionJobId>`
  - Validation: FluentValidation (deadline future, projectName format)
  - Publishes: `ProductionJobCreated` event

- [ ] **StartWorkflowStepCommand** + Handler
  - Input: `ProductionJobId`, `StepId`, `StartedBy`
  - Output: `Result<Unit>`
  - Validation: Step must be current step (FSM)
  - Publishes: `WorkflowStepStarted` event

- [ ] **CompleteWorkflowStepCommand** + Handler
  - Input: `ProductionJobId`, `StepId`, `CompletedBy`
  - Output: `Result<Unit>`
  - Validation: Step must be InProgress (FSM)
  - Side effect: Auto-advance to next step
  - Publishes: `WorkflowStepCompleted` event
  - **If last step**: Publishes `ProductionJobShippingReady` event

- [ ] **UploadStepPhotoCommand** + Handler
  - Input: `ProductionJobId`, `StepId`, `PhotoStream`, `Description`
  - Output: `Result<string>` (photo URL)
  - Validation: Step must be "Összeszerelés"
  - Upload to object storage (S3/Minio)

#### Queries
- [ ] **GetProductionQueueQuery** + Handler
  - Input: `ProductionFilter` (status, overdueOnly)
  - Output: `Result<List<ProductionJobDto>>`
  - Use case: Műhelyvezető mobile UI

- [ ] **GetProductionJobByIdQuery** + Handler
  - Input: `ProductionJobId`
  - Output: `Result<ProductionJobDto>`
  - Includes all 6 workflow steps with status

- [ ] **GetProductionOverviewQuery** + Handler
  - Input: `bool includeCompleted`
  - Output: `Result<ProductionOverviewDto>`
  - Use case: Tulaj/sales dashboard

#### DTOs
- [ ] **ProductionJobDto** (as defined in OpenAPI)
- [ ] **WorkflowStepDto**
- [ ] **ProductionOverviewDto**
- [ ] **ProductionJobSummaryDto**

#### Validators
- [ ] **CreateProductionJobCommandValidator**
  - Deadline must be future
  - ProjectName 3-50 chars
  - WorkflowSteps count = 6
- [ ] **StartWorkflowStepCommandValidator**
- [ ] **CompleteWorkflowStepCommandValidator**

---

### 2.3 Infrastructure Layer
**File**: `SpaceOS.Modules.Production/Infrastructure/`

#### Database
- [ ] **ProductionDbContext** (EF Core)
  - `DbSet<ProductionJob>` with owned entity `WorkflowStep` collection
  - RLS policy: `tenant_id` filtering (multi-tenant)
  - Migrations: `AddProductionJobTable`, `AddWorkflowStepsTable`

- [ ] **ProductionJobRepository** (implementation)
  - EF Core LINQ queries
  - AsNoTracking() for read-only queries
  - Include(j => j.WorkflowSteps) for full aggregate load

#### Event Handlers (Subscribers)
- [ ] **CuttingCompletedEventHandler**
  - Subscribe to: `CuttingJob.CuttingCompleted` (ADR-038)
  - Action: Auto-complete "Szabászat/Előgyártás" step
  - Logic:
    1. Find ProductionJob by OrderId correlation
    2. Mark first step (Szabászat) as Done
    3. Set `completedBy: "auto:CuttingCompleted"`
    4. Publish `WorkflowStepCompleted` event

- [ ] **OrderConfirmedEventHandler**
  - Subscribe to: `OrderItem.OrderConfirmed` (CRM/Joinery)
  - Action: Create ProductionJob
  - Logic:
    1. Extract OrderId, ProjectName, Deadline from event
    2. Create ProductionJob with 6 default WorkflowSteps
    3. Publish `ProductionJobCreated` event

#### Event Publishers
- [ ] **ProductionEventPublisher** (implementation)
  - Publishes domain events to event bus (MassTransit/RabbitMQ)
  - Events: `ProductionJobShippingReady`, `WorkflowStepCompleted`

#### External Services
- [ ] **ObjectStorageService** (S3/Minio)
  - `UploadPhotoAsync(Stream photo, string description)`
  - Returns: Public URL
  - Bucket: `production-photos`

---

### 2.4 API Layer
**File**: `SpaceOS.Modules.Production/Api/`

#### Controllers
- [ ] **ProductionController** (Minimal API or Controller)
  - All REST endpoints as defined in OpenAPI
  - Input validation via FluentValidation
  - Authorization: `[Authorize]` attribute
  - Tenant context: `ITenantContext` injection

#### Middleware
- [ ] **TenantContextMiddleware** (if not already in Kernel)
  - Extract `tenantId` from JWT claims
  - Set `ITenantContext.TenantId` for RLS filtering

#### OpenAPI/Swagger
- [ ] **ProductionApiModule** registration
  - Swagger docs generation
  - Example responses in XML comments

---

### 2.5 Integration Testing
**File**: `SpaceOS.Modules.Production.Tests/Integration/`

#### Test Cases
- [ ] **E2E_OrderConfirmed_CreatesProductionJob**
  - Publish `OrderConfirmed` event
  - Assert: ProductionJob created with 6 steps
  - Assert: First step is "Szabászat"

- [ ] **E2E_CuttingCompleted_AutoCompletesSzabaszat**
  - Given: ProductionJob with OrderId correlation
  - Publish: `CuttingCompleted` event
  - Assert: "Szabászat/Előgyártás" step marked Done
  - Assert: `completedBy` = "auto:CuttingCompleted"

- [ ] **E2E_6StageManualCompletion_PublishesShippingReady**
  - Given: ProductionJob with 6 steps
  - Complete steps 1-6 manually
  - Assert: After step 6 completion, `ProductionJobShippingReady` event published

- [ ] **E2E_ShippingReady_SendsNotification**
  - Given: ProductionJob completed
  - Publish: `ProductionJobShippingReady` event
  - Assert: Notification sent (Telegram/email mock verification)

#### Test Infrastructure
- [ ] **ProductionTestBase** (Testcontainers PostgreSQL)
  - In-memory event bus or RabbitMQ testcontainer
  - Test data factory: `ProductionJobFactory`

---

## 3. Frontend Task Breakdown (Note Only - Frontend Terminal)

**⚠️ NE IMPLEMENTÁLD - Frontend terminál fogja feldolgozni**

### Components
- [ ] **ProductionJobCard** component
  - Touch-optimized card (mobile kiosk)
  - STAGE progress visual (6 colored circles: grey/yellow/green)
  - Tap to expand → WorkflowStepStepper

- [ ] **WorkflowStepStepper** component
  - 6 STAGE vertical stepper
  - Current step highlighted (yellow)
  - "Start" / "Done" buttons (large, touch-friendly)
  - Optional photo upload button (Összeszerelés stage only)

- [ ] **KioskMobileLayout** layout component
  - Minimal navigation (back button only)
  - Full-screen STAGE view
  - Bottom sheet for step details

- [ ] **ProductionOverviewPage** page
  - Tulaj/sales dashboard
  - All active projects in grid
  - Overdue projects highlighted (red border)
  - Real-time SSE updates

### Hooks
- [ ] **useProductionQueue()** hook
  - TanStack Query: `GET /api/production/jobs`
  - Cache: 30s
  - Refetch on window focus

- [ ] **useCompleteStep()** mutation hook
  - `PUT /api/production/jobs/{jobId}/steps/{stepId}/complete`
  - Optimistic update: immediate UI feedback (yellow → green)
  - On success: Invalidate query cache

- [ ] **useProductionSSE()** hook
  - SSE channel: `ProductionJobStatusChannel`
  - Listen for: `WorkflowStepCompleted`, `ProductionJobShippingReady`
  - Update cache on event

### Routes
- [ ] `/production/jobs` - ProductionQueuePage (műhelyvezető view)
- [ ] `/production/jobs/:jobId` - ProductionJobDetailPage (STAGE stepper)
- [ ] `/production/overview` - ProductionOverviewPage (tulaj/sales view)

---

## 4. Integration Points

### 4.1 Inbound Events (Subscribes To)

#### **CuttingJob.CuttingCompleted** (ADR-038)
- **Source**: Cutting module (Track C, CNC kiosk)
- **Trigger**: When cutting process completes for an OrderId
- **Action**: Auto-complete "Szabászat/Előgyártás" step in ProductionJob
- **Implementation**: `CuttingCompletedEventHandler`
- **Correlation**: OrderId field (shared between Cutting and Production modules)

#### **OrderItem.OrderConfirmed** (Joinery/CRM)
- **Source**: Joinery or CRM module
- **Trigger**: When order is confirmed and ready for production
- **Action**: Create new ProductionJob with 6 default WorkflowSteps
- **Implementation**: `OrderConfirmedEventHandler`
- **Data**: OrderId, ProjectName (e.g., "DSMR 26144"), Deadline

---

### 4.2 Outbound Events (Publishes)

#### **ProductionJob.ShippingReady**
- **Target 1**: Inventory module → `Inventory.ReserveForShipping`
  - Action: Reserve finished goods for shipping
- **Target 2**: Sales/tulaj notification (Telegram/email)
  - Action: Send push notification (Viber replacement)
  - Message: "🚀 DSMR 26144 kiszállítható! Csomagolás elkészült."
- **Trigger**: Last step ("Kiszállítható") marked Done

#### **ProductionJob.WorkflowStepCompleted**
- **Target**: Analytics module → Timeline tracking
- **Action**: Record step completion for analytics dashboard
- **Data**: StepName, CompletedAt, Duration (startedAt → completedAt)

---

### 4.3 External Services

#### **Object Storage (S3/Minio)**
- **Use case**: Photo upload for "Összeszerelés" stage
- **Endpoint**: `POST /api/production/jobs/{jobId}/steps/{stepId}/photo`
- **Bucket**: `production-photos`
- **Access**: Public read, authenticated write

---

## 5. Timeline Estimate (Calendar Days)

### Backend Implementation

| Task | Duration | Dependencies |
|------|----------|--------------|
| **Domain Layer** | 1.5 days | - |
| - ProductionJob aggregate + FSM | 0.5 day | - |
| - WorkflowStep entity + value objects | 0.5 day | ProductionJob |
| - Domain events | 0.5 day | ProductionJob |
| **Application Layer** | 1 day | Domain Layer |
| - Commands + Handlers (3 commands) | 0.5 day | Domain Layer |
| - Queries + Handlers (3 queries) | 0.5 day | Domain Layer |
| **Infrastructure Layer** | 1 day | Application Layer |
| - EF Core DbContext + Repository | 0.5 day | Application Layer |
| - Event handlers (2 subscribers) | 0.5 day | Application Layer |
| **API Layer** | 0.5 day | Infrastructure Layer |
| - REST endpoints + OpenAPI | 0.5 day | Infrastructure Layer |
| **Integration Tests** | 1 day | All layers |
| - E2E tests (4 test cases) | 1 day | All layers |

**Total Backend**: **~4-5 naptári nap**

---

### Frontend Implementation (Parallel Track)

| Task | Duration | Dependencies |
|------|----------|--------------|
| **Components** | 1 day | Backend API ready |
| - ProductionJobCard | 0.5 day | - |
| - WorkflowStepStepper | 0.5 day | - |
| **Pages** | 0.5 day | Components |
| - ProductionQueuePage | 0.25 day | Components |
| - ProductionOverviewPage | 0.25 day | Components |
| **Hooks + SSE** | 0.5 day | Backend API ready |
| - useProductionQueue, useCompleteStep | 0.25 day | API |
| - useProductionSSE | 0.25 day | API + SSE |

**Total Frontend**: **~2 naptári nap** (párhuzamos backend-del)

---

### Integration & Testing

| Task | Duration | Dependencies |
|------|----------|--------------|
| **E2E Integration** | 0.5 day | Backend + Frontend ready |
| **Pilot Test (Doorstar)** | 0.5 day | E2E complete |

**Total Integration**: **~1 nap**

---

### **OVERALL TIMELINE**

```
Backend:        [====================] 4-5 days
Frontend:            [==========] 2 days (parallel, starts when API ready)
Integration:                   [=] 1 day
─────────────────────────────────────────────
Total Calendar: 5-6 days (with parallel work)
```

**Critical Path**: Backend → Integration → Pilot Test
**Parallel Work**: Frontend starts on Day 2 (when OpenAPI contract finalized)

---

## 6. Risk Analysis

### Technical Risks

#### **RISK-001: Offline Mode Missing (MVP)**
- **Impact**: HIGH
- **Probability**: MEDIUM
- **Description**: Műhelyben gyenge/nincs wifi → app unusable
- **Mitigation Phase 2**: Implement PWA offline-first with IndexedDB
- **Current MVP**: Assumes network availability (documented assumption)

#### **RISK-002: Event Correlation Failure**
- **Impact**: MEDIUM
- **Probability**: LOW
- **Description**: CuttingCompleted event can't find matching ProductionJob (OrderId mismatch)
- **Mitigation**: Strict OrderId validation, error logging, manual override UI

#### **RISK-003: Photo Storage Costs**
- **Impact**: LOW
- **Probability**: HIGH
- **Description**: Photo uploads could grow storage costs
- **Mitigation**: Implement retention policy (30 days), compression, CDN caching

---

### Business Risks

#### **RISK-004: Scope Creep (17 Micro-Phases)**
- **Impact**: HIGH
- **Probability**: MEDIUM
- **Description**: Customer requests Excel-level granularity in mobile app
- **Mitigation**: **6 STAGE scope is FINAL** (Cabinet validation MSG-ROOT-040). Excel workflow unchanged.

#### **RISK-005: Adoption Resistance**
- **Impact**: MEDIUM
- **Probability**: MEDIUM
- **Description**: Műhelyvezetők prefer paper kanban over mobile app
- **Mitigation**: Pilot test, training session, gradual rollout, feedback loop

---

## 7. Acceptance Criteria

**Implementation Plan Deliverables:**
- [x] OpenAPI contract draft (REST endpoints + DTOs)
- [x] Backend task breakdown (Domain/Application/Infrastructure/API)
- [x] Frontend task breakdown (noted for Frontend terminal)
- [x] Integration points documented (events in/out)
- [x] Timeline estimate (calendar days)
- [x] Risk analysis

**Backend Implementation Acceptance (Future):**
- [ ] All Domain/Application/Infrastructure/API tasks completed
- [ ] 4 E2E integration tests passing
- [ ] Build SUCCESS (0 errors, 0 warnings)
- [ ] OpenAPI Swagger docs available at `/swagger`
- [ ] RLS multi-tenant filtering verified

**Frontend Implementation Acceptance (Future, Frontend Terminal):**
- [ ] Mobile kiosk UI (ProductionJobCard, WorkflowStepStepper)
- [ ] SSE real-time updates working
- [ ] Touch-optimized UI (large buttons, simple navigation)

---

## 8. Next Steps

### Immediate (Root Approval)
1. ✅ **Implementation Plan DONE** → Root review (this document)
2. 🔄 Root forwards to Cabinet for validation (OpenAPI contract review)
3. 🔄 Cabinet approves or requests changes

### Backend Implementation (After Approval)
1. Create `SpaceOS.Modules.Production` project
2. Domain Layer → Application Layer → Infrastructure Layer → API Layer
3. Integration tests
4. OpenAPI Swagger docs deployment

### Frontend Implementation (Parallel, After API Ready)
1. Frontend terminal receives task (ProductionJobCard, Stepper, Pages)
2. SSE integration
3. Mobile kiosk UI testing

### Integration & Pilot
1. E2E integration test (OrderConfirmed → 6 STAGE → ShippingReady)
2. Doorstar pilot test (1-2 real projects)
3. Feedback loop → iteration

---

## 9. Files Changed

**Created:**
- This Implementation Plan document

**To Be Created (Backend Implementation Phase):**
- `spaceos-modules-production/Domain/ProductionJob.cs`
- `spaceos-modules-production/Domain/WorkflowStep.cs`
- `spaceos-modules-production/Domain/Events/*.cs`
- `spaceos-modules-production/Application/Commands/*.cs`
- `spaceos-modules-production/Application/Queries/*.cs`
- `spaceos-modules-production/Infrastructure/ProductionDbContext.cs`
- `spaceos-modules-production/Infrastructure/EventHandlers/*.cs`
- `spaceos-modules-production/Api/ProductionController.cs`
- `spaceos-modules-production/Tests/Integration/*.cs`

---

## 10. Appendix: 6 STAGE Definition

| # | STAGE | Munkamenet-fázisok | Trigger | UI |
|---|-------|-------------------|---------|-----|
| 1 | **Szabászat/Előgyártás** | Szabás, 22-es marás, HDF keret, üvegezés-előkészítés | Auto: `CuttingCompleted` | Auto sárga→zöld |
| 2 | **Megmunkálás** | CNC kontúrmarás, Gérvágás, Csiszolás | Manuális | Tap Start/Done |
| 3 | **Felületkezelés** | Fúrás, Ragasztó, Fóliázás | Manuális | Tap Start/Done |
| 4 | **Összeszerelés** | Él-lécezés, CNC Pánt-zár, Tok/Gér összerakás | Manuális + fotó | Tap + photo upload |
| 5 | **Csomagolás** | Paknizás, Csomagolás | Manuális | Tap "ZÖLD jelölés" |
| 6 | **Kiszállítható** | Kész → Raktár → Beépítés | Auto: Step 5 Done | Push notification |

**Notes:**
- 17 mikro-fázis + Kísérőlevél-rendszer (Excel) → **UNCHANGED**
- Mobile app = 6 összevont STAGE gyors követésre
- No duplicate data entry (Excel workflow remains authoritative)

---

**Effort**: ~6 hours (OpenAPI draft + task breakdown + timeline + risk analysis + documentation)
**Quality**: Production-ready specification, DDD patterns, event-driven architecture
**Status**: ✅ READY FOR ROOT/CABINET REVIEW

📋 Generated by Backend Terminal — Doorstar Production Workflow Implementation Plan (2026-07-08)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
