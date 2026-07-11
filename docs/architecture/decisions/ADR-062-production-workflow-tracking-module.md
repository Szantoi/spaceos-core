# ADR-062: Production Workflow Tracking Module Architecture

**Status:** DRAFT v1 (No review yet)
**Date:** 2026-07-10
**Epic:** EPIC-DOORSTAR-SOFTLAUNCH
**Author:** Architect Terminal
**Review References:** MSG-ARCHITECT-074, MSG-BACKEND-194, MSG-BACKEND-196

---

## Context

### Business Problem

**Client:** Doorstar Kft. (real production customer, target: 2026-09-30 soft launch)

**Current State (Pain Points):**
- Workshop status tracking is **paper-based** (Munkamenet.pdf printed, manually marked, photo sent via Viber to owner)
- **No real-time visibility** for owners/sales on production progress
- **Coordination chaos** — multiple jobs in different workflow stages, no central tracking
- **Deadlines missed** — no proactive alert system for overdue jobs
- **Communication bottleneck** — Viber photos = asynchronous, error-prone

**Business Requirements:**
1. **Mobile-first UI** for workshop managers (touch-optimized, kiosk mode)
2. **Real-time status push** to owner/sales (Viber replacement)
3. **Workflow transparency** — every job's current stage visible
4. **Deadline tracking** — highlight overdue jobs
5. **Photo documentation** (assembly stage optional photo upload)
6. **Excel workflow preservation** — do NOT replace detailed Kísérőlevél tracking (17 micro-phases + Folyamatok.xlsm), COMPLEMENT it

### Domain Model Requirements

**6 STAGE Workflow** (Condensed from 17 micro-phases in Munkamenet.pdf):

| # | STAGE | Covers (Munkamenet phases) | Trigger | Mobile UI |
|---|-------|---------------------------|---------|-----------|
| 1 | **Szabászat/Előgyártás** | Szabás, 22-es marás, HDF keret, üvegezés-előkészítés | Auto: `CuttingJob.CuttingCompleted` | Auto yellow→green |
| 2 | **Megmunkálás** | CNC kontúrmarás, Gérvágás, Csiszolás (variants) | Manual | Tap Start/Done |
| 3 | **Felületkezelés** | Fúrás, Ragasztó, Fóliázás | Manual | Tap Start/Done |
| 4 | **Összeszerelés** | Él-lécezés/Kivágás, CNC Pánt-zár, Tok összerakás, Gér összerakás | Manual + optional photo | Tap + photo upload |
| 5 | **Csomagolás** | Paknizás, Csomagolás | Manual | "GREEN mark" → Ready |
| 6 | **Kiszállítható** | Finished product → Warehouse → Installation | Auto: Csomagolás=Done → push | Push owner/sales |

**Key Constraint:** The 6 STAGE is a **summary view** only. The detailed 17-phase Munkamenet.pdf + Folyamatok.xlsm Kísérőlevél-system **remains unchanged** in Excel. The mobile app does NOT replace it, it **complements** it with:
- Fast STAGE-level status updates
- Real-time visibility for owner/sales
- Photo documentation (assembly stage)

### Integration Requirements

**Inbound Events:**
1. **OrderConfirmed** (from Joinery/CRM) → Creates `ProductionJob` aggregate
2. **CuttingCompleted** (from Cutting module, ADR-038) → Auto-completes "Szabászat" stage

**Outbound Events:**
1. **ProductionJobShippingReady** → Triggers:
   - Inventory reservation (`Inventory.ReserveForShipping`)
   - Owner/sales notification (Telegram/email — Viber replacement)

**Frontend Integration:**
- React 18 UI (mobile-first, touch-optimized)
- SSE (Server-Sent Events) for real-time status push
- TypeScript DTOs must match Backend API contract

---

## Decision

### Architecture Layer

**Layer 2 DRIVER** — `spaceos-modules-production` (.NET 8, DDD/CQRS/FSM)

**Why Layer 2:**
- Domain logic is **industry-specific** (woodworking production workflow)
- Kernel (Layer 1) provides infrastructure (auth, multi-tenancy, audit, event bus)
- Production module is a **pluggable driver** — does NOT pollute Kernel with domain logic

### Domain Model

**Aggregate Root:** `ProductionJob`

```csharp
public class ProductionJob : AggregateRoot<ProductionJobId>
{
    // Aggregate Identity
    public ProductionJobId Id { get; private set; }
    public TenantId TenantId { get; private set; }

    // Business Data
    public OrderId OrderId { get; private set; }
    public CustomerId CustomerId { get; private set; }
    public string ProjectName { get; private set; }
    public ProductionDeadline Deadline { get; private set; }
    public ProductionStatus Status { get; private set; }  // Aggregate-level FSM

    // Owned Entities (6 STAGE)
    public List<WorkflowStep> Steps { get; private set; }

    // Metadata
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
}
```

**Entity (Owned by Aggregate):** `WorkflowStep`

```csharp
public class WorkflowStep : Entity<WorkflowStepId>
{
    public WorkflowStepId Id { get; private set; }
    public WorkflowStepName Name { get; private set; }      // Value Object: Szabászat, Megmunkálás, etc.
    public int StepIndex { get; private set; }              // 0-5 (6 STAGE)
    public WorkflowStepStatus Status { get; private set; }  // Entity-level FSM

    // Tracking
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? CompletedBy { get; private set; }
    public PhotoUrl? PhotoUrl { get; private set; }         // Optional (Összeszerelés only)
}
```

**Value Objects:**
- `ProductionJobId` — Guid-based strongly-typed ID
- `WorkflowStepId` — Guid-based strongly-typed ID
- `WorkflowStepName` — Enum: Szabaszat, Megmunkalas, Felületkezeles, Osszeszereles, Csomagolas, Kiszallithato
- `ProductionDeadline` — DateTimeOffset with validation (must be future)
- `PhotoUrl` — URL validation + max length

**Domain Events:**
```csharp
public record ProductionJobStarted(ProductionJobId JobId, OrderId OrderId, TenantId TenantId, DateTimeOffset CreatedAt);

public record WorkflowStepStarted(ProductionJobId JobId, WorkflowStepId StepId, WorkflowStepName StepName, string StartedBy, DateTimeOffset StartedAt, TenantId TenantId);

public record WorkflowStepCompleted(ProductionJobId JobId, WorkflowStepId StepId, WorkflowStepName StepName, string CompletedBy, DateTimeOffset CompletedAt, PhotoUrl? PhotoUrl, TenantId TenantId);

public record ProductionJobShippingReady(ProductionJobId JobId, string ProjectName, DateTimeOffset ReadyAt, TenantId TenantId);
```

### 2-Level FSM (Finite State Machine)

**Level 1: Aggregate-Level FSM** (`ProductionJob.Status`)

```
Queued → InProgress → Completed → ShippingReady
```

**Transitions:**
- `Queued → InProgress`: When first step starts
- `InProgress → Completed`: When last step (Csomagolás) completes
- `Completed → ShippingReady`: Automatically (domain event raised)

**Level 2: Entity-Level FSM** (`WorkflowStep.Status`)

```
Queued → InProgress → Done
```

**Transitions:**
- `Queued → InProgress`: Manual (workshop manager taps "Start")
- `InProgress → Done`: Manual (workshop manager taps "Complete")

**Exception:** Szabászat step:
- Auto-transitions `Queued → Done` when `CuttingJob.CuttingCompleted` event received

**FSM Business Rules:**
1. **IN-ORDER:** Steps must complete sequentially (Step N+1 can only start if Step N is Done)
2. **SINGLE ACTIVE:** Only ONE step can be InProgress at a time
3. **PHOTO REQUIRED:** Összeszerelés step completion requires photo upload (optional in v1, enforced in v2)

### API Contract (REST + SSE)

**REST Endpoints:**

```yaml
POST   /api/production/jobs                                    # Create job (OrderConfirmed event handler)
GET    /api/production/jobs                                    # List active jobs (workshop view)
GET    /api/production/jobs/{jobId}                            # Job details (6 STAGE status)
PUT    /api/production/jobs/{jobId}/steps/{stepId}/start       # Start step (manual trigger)
PUT    /api/production/jobs/{jobId}/steps/{stepId}/complete    # Complete step (manual trigger)
POST   /api/production/jobs/{jobId}/steps/{stepId}/photo       # Upload photo (optional)
GET    /api/production/overview                                # Owner/sales dashboard (all jobs)
```

**SSE Channel:** `/api/sse/production`

**SSE Events:**
- `WorkflowStepCompletedEvent` — Real-time step completion notification
- `ProductionJobShippingReadyEvent` — Job ready for shipping notification

**DTOs:** See Section 1 (OpenAPI Contract Validation) for complete DTO definitions.

### Integration Pattern

**Event-Driven Integration:**

1. **Inbound: OrderConfirmed → ProductionJob**
   - Handler: `OrderConfirmedEventHandler`
   - Action: Create `ProductionJob` aggregate with 6 steps (all Queued)
   - Idempotency: Check if job already exists for OrderId

2. **Inbound: CuttingCompleted → Szabászat Auto-Complete**
   - Handler: `CuttingCompletedEventHandler`
   - **Correlation:** Event MUST include `OrderId` (see Finding 3.2)
   - Action: Find ProductionJob by OrderId, complete Szabászat step
   - Idempotency: Check if Szabászat already Done

3. **Outbound: ShippingReady → Inventory + Notification**
   - Event: `ProductionJobShippingReady`
   - Handlers:
     - `InventoryReservationHandler` → `IInventoryReservationService.ReserveForShippingAsync()`
     - `NotificationHandler` → `INotificationService.NotifyShippingReadyAsync()` (Telegram/Email)

### CQRS Pattern

**Commands (Write Operations):**
- `StartProductionJobCommand` + `StartProductionJobCommandHandler`
- `StartWorkflowStepCommand` + `StartWorkflowStepCommandHandler`
- `CompleteWorkflowStepCommand` + `CompleteWorkflowStepCommandHandler`
- `UploadStepPhotoCommand` + `UploadStepPhotoCommandHandler`

**Queries (Read Operations):**
- `GetProductionQueueQuery` + `GetProductionQueueQueryHandler` (Workshop view)
- `GetProductionJobByIdQuery` + `GetProductionJobByIdQueryHandler` (Detail view)
- `GetProductionOverviewQuery` + `GetProductionOverviewQueryHandler` (Owner/sales view)

**Rationale for CQRS:**
- Write operations (commands) validate FSM rules and raise domain events
- Read operations (queries) optimize for UI performance (pre-calculate isOverdue, currentStepIndex, durationMinutes)

---

## Rationale

### Why Layer 2 (Driver)?

**Alternative Considered:** Implement production tracking directly in Kernel (Layer 1).

**Rejected Because:**
- Kernel is **domain-agnostic** — production workflow is **industry-specific**
- Violates Golden Rule #2 (Modular Monolith) — Kernel should not know about "Szabászat" or "Összeszerelés"
- Hard to remove/replace if production workflow changes

**Chosen Approach:**
- Production module is a **pluggable driver** at Layer 2
- Kernel provides infrastructure (auth, multi-tenancy, event bus, audit)
- Production module can be independently developed, tested, deployed

### Why 6 STAGE (Condensed)?

**Alternative Considered:** 17 micro-phases (full Munkamenet.pdf workflow).

**Rejected Because:**
- Too granular for mobile UI (workshop managers want fast status updates, not detailed Excel tracking)
- Excel Kísérőlevél system already handles 17-phase tracking (don't duplicate)
- Mobile app goal: **complement** Excel, not **replace** it

**Chosen Approach:**
- 6 STAGE = summary view for mobile app + owner/sales dashboard
- Excel Kísérőlevél = detailed tracking (unchanged)
- Best of both worlds: fast status + detailed tracking

### Why 2-Level FSM?

**Alternative Considered:** Single-level FSM (only ProductionJob.Status).

**Rejected Because:**
- Cannot track individual step status (Szabászat Done, Megmunkálás InProgress, etc.)
- Cannot enforce IN-ORDER rule (Step N+1 only after Step N Done)
- Cannot detect which step is blocking progress

**Chosen Approach:**
- **Aggregate-level FSM** (ProductionJob.Status) — high-level job state
- **Entity-level FSM** (WorkflowStep.Status) — granular step tracking
- FSM rules enforced in domain model (StartStep, CompleteStep methods)

### Why OrderId in CuttingCompleted Event?

**Alternative Considered:**
1. **Mapping Table** (CuttingJobId → OrderId lookup)
2. **Correlation ID** (Separate correlation table)
3. **OrderId in Event Payload** (Proposed)

**Option 1 (Mapping Table) — Rejected:**
- Requires synchronous coupling between Cutting and Production modules
- Mapping table must be updated on every OrderConfirmed event
- Race condition: CuttingCompleted before mapping table updated

**Option 2 (Correlation ID) — Rejected:**
- Adds complexity (separate correlation store)
- Overkill for simple 1:1 correlation (CuttingJob belongs to exactly 1 Order)

**Option 3 (OrderId in Event Payload) — CHOSEN:**
- **Simplest solution** — event is self-contained
- No additional storage or lookups needed
- Event handler can directly correlate: `CuttingCompleted.OrderId` → `ProductionJob.OrderId`
- **Trade-off:** Requires Cutting module to know OrderId (acceptable, as CuttingJob is created FROM Order)

**Recommended Action:** Update Cutting module to include OrderId in `CuttingCompleted` event payload (see Finding 3.2).

### Why SSE (Server-Sent Events)?

**Alternative Considered:**
1. **Polling** (Frontend polls `/api/production/jobs` every 5 seconds)
2. **WebSockets** (Bidirectional real-time connection)
3. **SSE** (Server-Sent Events, unidirectional push)

**Option 1 (Polling) — Rejected:**
- High network overhead (unnecessary requests even when no changes)
- Battery drain on mobile devices
- Delayed updates (5-second latency)

**Option 2 (WebSockets) — Rejected:**
- Overkill for one-way updates (Frontend only READS status, does NOT push to Backend)
- More complex infrastructure (WebSocket server, connection management, reconnection logic)
- Browser compatibility issues on older mobile devices

**Option 3 (SSE) — CHOSEN:**
- **Perfect fit:** Unidirectional push (Backend → Frontend)
- **Simple HTTP/2 connection** (built-in browser support, no library needed)
- **Auto-reconnect** (Browser handles connection drops)
- **Low overhead** (only push when events occur)

---

## Consequences

### Positive Consequences

1. **Real-Time Visibility**
   - Owner/sales see live production status (Viber replacement)
   - Workshop managers see all jobs in one mobile-first UI
   - No more paper tracking + photo uploads

2. **Deadline Awareness**
   - `isOverdue` flag highlights late jobs (proactive alerts)
   - Owner can intervene before customer deadline missed

3. **Audit Trail**
   - Every step completion tracked with timestamp + operator name
   - Photo documentation (assembly stage)
   - Full event sourcing for debugging/analytics

4. **Modular Architecture**
   - Production module is **independently testable**
   - Can be removed/replaced without touching Kernel
   - Clear integration contracts (event bus, provider interfaces)

5. **Excel Workflow Preserved**
   - Doorstar's existing 17-phase Kísérőlevél system **unchanged**
   - Mobile app **complements** it (not competes with it)
   - Smooth adoption (workshop managers keep familiar Excel workflow)

6. **Scalability**
   - DDD aggregate pattern ensures consistency boundary
   - CQRS separates write (FSM validation) from read (UI optimization)
   - SSE scales horizontally (stateless HTTP/2 connections)

### Negative Consequences

1. **Event Correlation Complexity**
   - CuttingCompleted → ProductionJob correlation requires OrderId in event
   - **Mitigation:** Cutting module update (low risk, single field addition)

2. **Race Conditions**
   - CuttingCompleted may arrive BEFORE ProductionJob created (if OrderConfirmed slow)
   - **Mitigation:** Handler logs warning and returns (graceful degradation, low probability)

3. **Photo Storage Cost**
   - Assembly photos stored in blob storage (Azure Blob/S3)
   - **Mitigation:** Compress images, set retention policy (e.g., 90 days)

4. **Mobile Network Dependency**
   - SSE requires stable internet connection
   - **Mitigation:** Offline-first caching (React Query, IndexedDB) — future enhancement

5. **Excel Workflow Confusion Risk**
   - Workshop managers might forget to update Excel Kísérőlevél (thinking mobile app is enough)
   - **Mitigation:** Training + clear communication: "Mobile app = summary, Excel = detailed tracking"

---

## Alternatives Considered

### Alternative 1: Unified 17-Phase Tracking (Mobile App Replaces Excel)

**Proposal:** Implement all 17 micro-phases from Munkamenet.pdf in mobile app, deprecate Excel.

**Rejected Because:**
- **Too complex UI** — 17 steps = overwhelming on mobile screen
- **Excel workflow deeply embedded** — Doorstar uses Folyamatok.xlsm for part-specific Kísérőlevél (different workflows per part category)
- **High adoption risk** — workshop managers resist losing Excel (familiar tool)
- **Over-engineering** — mobile app goal is **status visibility**, not detailed tracking

**Lessons Learned:** Don't try to replace working tools. Complement them.

### Alternative 2: Synchronous REST Integration (No Events)

**Proposal:** Production module calls Cutting module REST API to check if cutting done (synchronous lookup).

**Rejected Because:**
- **Tight coupling** — Production module depends on Cutting module availability
- **Circular dependency risk** — Cutting → Production → Cutting (if Cutting also needs Production data)
- **Performance** — Every job status query triggers Cutting API call
- **Scalability** — Synchronous calls block threads, reduce throughput

**Lessons Learned:** Event-driven integration is preferred for cross-module communication (loose coupling, async, scalable).

### Alternative 3: NoSQL (MongoDB) Instead of PostgreSQL

**Proposal:** Store ProductionJob as JSON document in MongoDB (flexible schema).

**Rejected Because:**
- **SpaceOS standard:** PostgreSQL + EF Core (consistency across modules)
- **ACID transactions needed** — FSM state transitions must be atomic
- **RLS (Row Level Security) required** — multi-tenancy isolation (PostgreSQL feature)
- **Aggregate pattern works well with relational model** — ProductionJob + WorkflowSteps = 1-to-many

**Lessons Learned:** Stick to platform standards unless there's a compelling reason to diverge.

---

## Implementation Notes

### Task Breakdown (Backend)

**Domain Layer** (~1 day):
- [x] `ProductionJob` aggregate root
- [x] `WorkflowStep` entity
- [x] Value objects (`ProductionJobId`, `WorkflowStepName`, `ProductionDeadline`, `PhotoUrl`)
- [x] Domain events (`ProductionJobStarted`, `WorkflowStepCompleted`, `ProductionJobShippingReady`)
- [x] FSM validation logic (`StartStep()`, `CompleteStep()` methods)

**Application Layer** (~1 day):
- [ ] Commands + Handlers (`StartProductionJobCommand`, `CompleteWorkflowStepCommand`, etc.)
- [ ] Queries + Handlers (`GetProductionQueueQuery`, `GetProductionOverviewQuery`, etc.)
- [ ] DTOs (corrected versions from Section 1)

**Infrastructure Layer** (~1 day):
- [ ] `ProductionDbContext` (EF Core)
- [ ] `ProductionJobRepository` (aggregate persistence)
- [ ] Event handlers (`OrderConfirmedEventHandler`, `CuttingCompletedEventHandler`, `ProductionJobShippingReadyEventHandler`)
- [ ] SSE publisher (`ProductionSSEPublisher`)

**API Layer** (~0.5 day):
- [ ] `ProductionController` (REST endpoints)
- [ ] OpenAPI spec (corrected version from Section 1)

**Integration Tests** (~1 day):
- [ ] E2E: OrderConfirmed → ProductionJob created
- [ ] E2E: CuttingCompleted → Szabászat auto-complete
- [ ] E2E: 6 STAGE manual completion → ShippingReady event
- [ ] E2E: ShippingReady → Inventory + Notification

**Total Backend Estimate:** ~4.5 days

### Task Breakdown (Frontend)

**Components** (~1.5 days):
- [ ] `ProductionJobCard` (touch-optimized, STAGE progress)
- [ ] `WorkflowStepStepper` (6 STAGE visual stepper)
- [ ] `KioskMobileLayout` (minimal nav, full-screen STAGE view)
- [ ] `ProductionOverviewPage` (owner/sales: all jobs state)

**SSE Integration** (~0.5 day):
- [ ] `useProductionSSE` hook (SSE channel subscription)
- [ ] React Query cache invalidation on SSE events

**Total Frontend Estimate:** ~2 days

**Timeline:** Backend + Frontend can run **in parallel** → Total ~5 days (4.5 days Backend || 2 days Frontend)

---

## Security Considerations

1. **Multi-Tenancy (RLS):**
   - All queries MUST filter by `TenantId` (PostgreSQL RLS enforced)
   - Domain events MUST include `TenantId` (see Finding 2.5)

2. **Authorization (RBAC):**
   - Workshop Manager: Can start/complete steps (role: `production.workshop`)
   - Owner/Sales: Read-only access (role: `production.viewer`)
   - Admin: Full access (role: `production.admin`)

3. **Photo Upload Validation:**
   - File type: JPEG/PNG only (no executables)
   - File size: Max 5MB (prevent blob storage abuse)
   - Virus scan: Optional (cloud storage provider feature)

4. **SSE Authentication:**
   - SSE endpoint requires JWT token (same auth as REST API)
   - SSE channel scoped to TenantId (no cross-tenant leaks)

---

## Testing Strategy

1. **Unit Tests** (Domain Layer):
   - FSM transitions (valid + invalid)
   - Business rules (IN-ORDER, SINGLE ACTIVE, etc.)

2. **Integration Tests** (Application + Infrastructure):
   - Event handlers (OrderConfirmed, CuttingCompleted, ShippingReady)
   - Repository persistence (aggregate + owned entities)

3. **E2E Tests** (API Layer):
   - Full workflow: OrderConfirmed → 6 STAGE completion → ShippingReady → Notification
   - SSE event delivery

4. **Manual Testing** (Doorstar Pilot):
   - Real production jobs (Doorstar workshop)
   - Mobile device testing (Android tablet)
   - Owner/sales dashboard (desktop browser)

---

## Monitoring and Observability

1. **Metrics:**
   - Average job duration (Queued → ShippingReady)
   - Average step duration (per STAGE)
   - Overdue job count (alert threshold: >3 overdue jobs)

2. **Alerts:**
   - CuttingCompleted correlation failure (logs warning, alert if >5/hour)
   - Notification delivery failure (logs error, retry 3× with backoff)
   - SSE connection drops (logs info, monitor reconnection rate)

3. **Audit Log:**
   - All step start/complete actions (userId, timestamp, stepName)
   - Photo uploads (userId, timestamp, photoUrl, fileSize)

---

## References

- **MSG-BACKEND-194:** Doorstar Production Workflow Implementation Plan
- **MSG-BACKEND-196:** Production Module Full DDD Implementation
- **MSG-FRONTEND-107:** Production UI TypeScript Types
- **ADR-038:** Cutting Module Architecture (CuttingJob.CuttingCompleted event)
- **ADR-054:** CRM Domain Model (CQRS + FSM pattern reference)
- **ADR-055:** Kontrolling Domain Model (Calculated aggregates pattern)
- **MSG-ARCHITECT-074:** Architecture Review Findings (Sections 1-4)

---

## Approval

**Status:** DRAFT v1 — Pending Backend/Conductor review

**Next Steps:**
1. Backend Team reviews findings from MSG-ARCHITECT-074 (Sections 1-4)
2. Backend Team addresses HIGH severity findings (DTO mismatches, event correlation, SSE events)
3. Conductor approves ADR-062 → Implementation proceeds

---

**Document Metadata:**
- **Lines:** ~550 lines
- **Estimated Read Time:** 15-20 minutes
- **Last Updated:** 2026-07-10
- **Version:** v1 (DRAFT)
