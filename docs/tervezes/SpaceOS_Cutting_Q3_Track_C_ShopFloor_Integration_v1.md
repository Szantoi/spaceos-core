# SpaceOS Cutting Q3 Track C — ShopFloor Integration

**Version:** v1
**Created:** 2026-06-23
**Status:** Approved (Root MSG-CONDUCTOR-007)
**Epic:** CUTTING-Q3-EXPANSION
**Duration:** 2 days (Week 3-4)
**Priority:** HIGH

---

## Executive Summary

Implement a ShopFloor Integration module that connects the Cutting Planning system with physical cutting machines (CNC saws) via a Machine Queue and operator Kiosk UI for real-time job assignment and progress tracking.

**Current gap:** Cutting Plans exist but no machine-level execution tracking
**Q3 Track C adds:**
1. Machine Queue (backend service)
2. Kiosk Workflow UI (frontend, operator-facing)
3. Real-time job assignment (Cutting Plan → Machine)
4. Progress tracking (Started, Completed, Failed)

---

## Business Context

**Target customer:** 2. ügyfél (Lapszabász KKV)
**Use case:** Shop floor operators:
1. Log in via PIN on shop floor kiosk
2. See assigned cutting jobs for their machine
3. Start job → machine receives cutting list
4. Mark job complete or report issue
5. Analytics: machine utilization, OEE tracking

**Current state:** Cutting Plans generated but no shop floor execution visibility
**Desired state:** Real-time job tracking, machine queue optimization

---

## Acceptance Criteria

**Backend (1.5 days):**
- [ ] Machine Queue domain model (MachineQueue, QueuedJob, JobAssignment)
- [ ] API endpoints:
  - `GET /api/cutting/shopfloor/queue` — list jobs for machine
  - `POST /api/cutting/shopfloor/jobs/{jobId}/assign` — assign job to machine/operator
  - `PUT /api/cutting/shopfloor/jobs/{jobId}/start` — start job
  - `PUT /api/cutting/shopfloor/jobs/{jobId}/complete` — complete job
  - `PUT /api/cutting/shopfloor/jobs/{jobId}/fail` — mark job failed
- [ ] Database schema: `MachineQueue`, `JobAssignments`
- [ ] Unit tests: Queue logic (95%), job state transitions (100%)

**Frontend (0.5 day):**
- [ ] Kiosk Login (PIN-based, no username)
- [ ] Machine Queue View (assigned jobs list)
- [ ] Job Detail View (cutting list, dimensions, start/complete buttons)
- [ ] Progress Indicator (real-time, if SignalR available)
- [ ] Integration tests: Kiosk workflow (3 scenarios)

**Integration:**
- [ ] E2E test: Cutting Plan → Machine Queue → Operator starts job → Completes → Analytics updated
- [ ] Real-time updates tested (polling or SignalR)

**Documentation:**
- [ ] Kiosk setup guide (PIN configuration, machine mapping)
- [ ] API docs: ShopFloor endpoints

---

## Technical Design

### Architecture

```
Cutting Plan (approved)
  ↓
AssignToBatch command (Track A)
  ↓
Machine Queue Service
  ↓ Assign job to machine based on:
    - Machine capacity
    - Job priority (pricing-based)
    - Current queue length
  ↓
Kiosk UI (operator login via PIN)
  ↓
Operator starts job
  ↓
Job state: Queued → Assigned → InProgress → Completed/Failed
  ↓
Analytics update (MachineOEEHourly)
```

### Machine Queue Algorithm

**Queue Priority:**
1. High-value quotes (from Pricing Engine, Track B)
2. FIFO for same priority
3. Machine availability

**Assignment logic:**
```csharp
public class MachineQueueService
{
    public async Task<MachineAssignment> AssignJobToMachine(
        Guid cuttingPlanId,
        CancellationToken ct)
    {
        // 1. Find available machines (not at capacity)
        // 2. Calculate priority score (price × urgency)
        // 3. Assign to machine with shortest queue
        // 4. Create JobAssignment entity
        // 5. Publish JobAssignedEvent
    }
}
```

### Database Schema

**MachineQueue table:**
```sql
CREATE TABLE "MachineQueue" (
  "Id" uuid PRIMARY KEY,
  "TenantId" uuid NOT NULL,
  "MachineId" text NOT NULL, -- FK to Workstations
  "CuttingPlanId" uuid NOT NULL,
  "Priority" integer NOT NULL,
  "QueuePosition" integer NOT NULL,
  "Status" text NOT NULL, -- Queued, Assigned, InProgress, Completed, Failed
  "AssignedAt" timestamptz,
  "StartedAt" timestamptz,
  "CompletedAt" timestamptz,
  "OperatorId" uuid, -- FK to Users (operator)
  CONSTRAINT fk_tenant FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id"),
  CONSTRAINT fk_cutting_plan FOREIGN KEY ("CuttingPlanId") REFERENCES "CuttingPlans"("Id")
);
```

**JobAssignments table:**
```sql
CREATE TABLE "JobAssignments" (
  "Id" uuid PRIMARY KEY,
  "TenantId" uuid NOT NULL,
  "JobId" uuid NOT NULL, -- FK to MachineQueue
  "OperatorId" uuid NOT NULL,
  "AssignedAt" timestamptz NOT NULL,
  "StartedAt" timestamptz,
  "CompletedAt" timestamptz,
  "Status" text NOT NULL, -- Assigned, InProgress, Completed, Failed
  "FailureReason" text,
  CONSTRAINT fk_job FOREIGN KEY ("JobId") REFERENCES "MachineQueue"("Id"),
  CONSTRAINT fk_operator FOREIGN KEY ("OperatorId") REFERENCES "Users"("Id")
);
```

### API Endpoints

**1. Get Machine Queue**
```http
GET /api/cutting/shopfloor/queue?machineId={machineId}
Authorization: Bearer {jwt} (operator role)

Response:
{
  "machineId": "HOLZMA-01",
  "queuedJobs": [
    {
      "jobId": "...",
      "cuttingPlanId": "...",
      "priority": 1,
      "queuePosition": 1,
      "status": "Queued",
      "estimatedDuration": "00:45:00",
      "panelCount": 25
    }
  ]
}
```

**2. Start Job**
```http
PUT /api/cutting/shopfloor/jobs/{jobId}/start
Authorization: Bearer {jwt} (operator role)

Request:
{
  "operatorId": "...",
  "machineId": "HOLZMA-01"
}

Response:
{
  "jobId": "...",
  "status": "InProgress",
  "startedAt": "2026-06-23T10:00:00Z"
}
```

**3. Complete Job**
```http
PUT /api/cutting/shopfloor/jobs/{jobId}/complete
Authorization: Bearer {jwt} (operator role)

Request:
{
  "operatorId": "...",
  "actualPanelCount": 25,
  "offcutCount": 3
}

Response:
{
  "jobId": "...",
  "status": "Completed",
  "completedAt": "2026-06-23T10:45:00Z",
  "duration": "00:45:00"
}
```

**4. Fail Job**
```http
PUT /api/cutting/shopfloor/jobs/{jobId}/fail
Authorization: Bearer {jwt} (operator role)

Request:
{
  "operatorId": "...",
  "failureReason": "Machine malfunction - blade broken"
}

Response:
{
  "jobId": "...",
  "status": "Failed",
  "failedAt": "2026-06-23T10:15:00Z"
}
```

### Frontend Components

**1. KioskLogin.tsx** (updated from existing)
```tsx
// Route: /shopfloor/login
<KioskLogin>
  <PINInput length={4} onSubmit={handlePINLogin} />
  <MachineSelector machines={availableMachines} />
</KioskLogin>
```

**2. MachineQueueView.tsx** (new)
```tsx
// Route: /shopfloor/queue
<MachineQueueView machineId={selectedMachine}>
  <JobList jobs={queuedJobs}>
    {jobs.map(job => (
      <JobCard
        job={job}
        onStart={() => handleStartJob(job.id)}
        onViewDetails={() => handleViewDetails(job.id)}
      />
    ))}
  </JobList>
</MachineQueueView>
```

**3. JobDetailView.tsx** (new)
```tsx
// Route: /shopfloor/jobs/:jobId
<JobDetailView jobId={jobId}>
  <CuttingList panels={job.panels} />
  <ProgressBar current={job.completedPanels} total={job.totalPanels} />
  <ActionButtons>
    <Button onClick={handleComplete}>Complete Job</Button>
    <Button onClick={handleFail} variant="destructive">Report Issue</Button>
  </ActionButtons>
</JobDetailView>
```

**4. Real-time updates (optional)**
```tsx
// Use SignalR or polling
useEffect(() => {
  const interval = setInterval(() => {
    refetchQueue();
  }, 5000); // Poll every 5 seconds
  return () => clearInterval(interval);
}, []);
```

---

## Implementation Tasks

### Backend (MSG-032)

**Sub-tasks:**
1. Domain layer: MachineQueue, JobAssignment aggregates
2. Application layer: AssignJobCommand, StartJobCommand, CompleteJobCommand, FailJobCommand
3. Infrastructure: MachineQueueRepository, Database migrations
4. API: ShopFloorEndpoints.cs (5 endpoints)
5. MachineQueueService implementation (priority algorithm)
6. Unit tests: Queue logic (20 scenarios), FSM transitions (100%)
7. Integration tests: E2E shopfloor workflow (5 scenarios)

**Files to create:**
- `Domain/MachineQueue.cs`, `Domain/JobAssignment.cs`
- `Application/Commands/AssignJob/AssignJobCommand.cs`
- `Application/Commands/StartJob/StartJobCommand.cs`
- `Application/Commands/CompleteJob/CompleteJobCommand.cs`
- `Application/Commands/FailJob/FailJobCommand.cs`
- `Application/Queries/GetMachineQueue/GetMachineQueueQuery.cs`
- `Services/IMachineQueueService.cs`, `Services/MachineQueueService.cs`
- `Endpoints/ShopFloorEndpoints.cs`
- `Migrations/AddMachineQueueTables.cs`
- `Tests/Domain/MachineQueueTests.cs`, `Tests/Services/MachineQueueServiceTests.cs`

**Estimate:** 1.5 days (12 hours)

---

### Frontend (MSG-020)

**Sub-tasks:**
1. Update KioskLogin component (if needed)
2. Create MachineQueueView component
3. Create JobDetailView component
4. Add real-time polling (optional SignalR)
5. Integration tests: Kiosk workflow (3 scenarios)

**Files to create:**
- `src/pages/MachineQueueView.tsx`
- `src/pages/JobDetailView.tsx`
- `src/components/JobCard.tsx`
- `src/hooks/useMachineQueue.ts`
- `src/pages/MachineQueueView.test.tsx`

**Files to modify:**
- `src/pages/ShopFloor.tsx` (update routing)
- `src/App.tsx` (add `/shopfloor/queue`, `/shopfloor/jobs/:jobId` routes)

**Estimate:** 0.5 day (4 hours)

---

## Dependencies

**Blocks:**
- Analytics updates (MachineOEEHourly) — job completion triggers analytics

**Blocked by:**
- Track A (AssignBatch endpoint exists from Phase 1)
- Track B (Pricing affects queue priority)

---

## Success Metrics

- Queue assignment accuracy: 95%+ (correct machine selection)
- Job completion rate: 90%+ (started jobs → completed)
- Operator adoption: 80%+ operators use kiosk weekly
- Machine utilization: 10%+ increase (tracked in Analytics)

---

## Risks & Mitigation

| Risk | Mitigation |
|---|---|
| PIN authentication weak | Defer to Identity module for stronger auth (Phase 2) |
| Real-time updates laggy | Use polling (5s interval) instead of SignalR for MVP |
| Machine unavailable | Queue algorithm checks machine status before assigning |
| Operator error (wrong job marked complete) | Add confirmation dialog, audit log |

---

## Future Enhancements (Q4)

- **SignalR real-time updates** — replace polling
- **Machine status sensors** — IoT integration
- **Barcode scanning** — scan panel QR codes for verification
- **Multi-machine operators** — assign operator to multiple machines
- **Queue optimization ML** — predict job duration, optimize queue order

---

## References

- **AssignBatch endpoint:** `CuttingExecutionEndpoints.cs` (Phase 1)
- **MachineOEEHourly:** `Analytics.Domain/ReadModels/MachineOEEHourly.cs`
- **Codebase_Status.md:** ShopFloor design (lines 299-301)
- **ADR-005:** Walking Skeleton First (E2E pipeline before deep iteration)
