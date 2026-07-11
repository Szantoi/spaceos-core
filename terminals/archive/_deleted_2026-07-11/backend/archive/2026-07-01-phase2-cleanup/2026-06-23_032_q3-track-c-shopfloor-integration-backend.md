---
id: MSG-BACKEND-032
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: /opt/spaceos/docs/tervezes/SpaceOS_Cutting_Q3_Track_C_ShopFloor_Integration_v1.md
created: 2026-06-23
---

# Q3 Track C — ShopFloor Integration (Backend)

**Epic:** CUTTING-Q3-EXPANSION
**Duration:** 1.5 days
**Priority:** HIGH
**Status:** APPROVED (Root MSG-CONDUCTOR-007)

---

## Executive Summary

Implement a ShopFloor Integration module that connects Cutting Plans with physical cutting machines via a Machine Queue and job assignment system for real-time execution tracking.

**Current gap:** Cutting Plans exist but no machine-level execution tracking

**Track C Backend adds:**
1. Machine Queue domain model
2. Job assignment logic (priority-based)
3. Job state machine (Queued → Assigned → InProgress → Completed/Failed)
4. API endpoints for shop floor operators

---

## Acceptance Criteria

- [ ] **Machine Queue domain model**
  - `MachineQueue` aggregate
  - `JobAssignment` value object
  - FSM: Queued → Assigned → InProgress → Completed/Failed
- [ ] **API endpoints** (5 endpoints)
  - `GET /api/cutting/shopfloor/queue?machineId={id}` — list jobs for machine
  - `POST /api/cutting/shopfloor/jobs/{jobId}/assign` — assign job
  - `PUT /api/cutting/shopfloor/jobs/{jobId}/start` — start job
  - `PUT /api/cutting/shopfloor/jobs/{jobId}/complete` — complete job
  - `PUT /api/cutting/shopfloor/jobs/{jobId}/fail` — mark failed
- [ ] **Database schema**
  - `MachineQueue`, `JobAssignments` tables
- [ ] **Unit tests**
  - Queue logic: 95% coverage
  - FSM state transitions: 100% (all valid/invalid transitions)
- [ ] **Integration tests**
  - E2E: Cutting Plan → Queue → Assign → Start → Complete

---

## Technical Implementation

### 1. Database Migrations

**File:** `Migrations/AddMachineQueueTables.cs`

```sql
CREATE TABLE "MachineQueue" (
  "Id" uuid PRIMARY KEY,
  "TenantId" uuid NOT NULL,
  "MachineId" text NOT NULL,
  "CuttingPlanId" uuid NOT NULL,
  "Priority" integer NOT NULL,
  "QueuePosition" integer NOT NULL,
  "Status" text NOT NULL,
  "AssignedAt" timestamptz,
  "StartedAt" timestamptz,
  "CompletedAt" timestamptz,
  "OperatorId" uuid,
  CONSTRAINT fk_tenant FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id"),
  CONSTRAINT fk_cutting_plan FOREIGN KEY ("CuttingPlanId") REFERENCES "CuttingPlans"("Id")
);

CREATE TABLE "JobAssignments" (
  "Id" uuid PRIMARY KEY,
  "TenantId" uuid NOT NULL,
  "JobId" uuid NOT NULL,
  "OperatorId" uuid NOT NULL,
  "AssignedAt" timestamptz NOT NULL,
  "StartedAt" timestamptz,
  "CompletedAt" timestamptz,
  "Status" text NOT NULL,
  "FailureReason" text,
  CONSTRAINT fk_job FOREIGN KEY ("JobId") REFERENCES "MachineQueue"("Id"),
  CONSTRAINT fk_operator FOREIGN KEY ("OperatorId") REFERENCES "Users"("Id")
);

CREATE INDEX idx_machinequeue_tenant_machine ON "MachineQueue"("TenantId", "MachineId");
CREATE INDEX idx_machinequeue_status ON "MachineQueue"("Status");
CREATE INDEX idx_jobassignments_operator ON "JobAssignments"("OperatorId");
```

### 2. Domain Models

**File:** `Domain/MachineQueue.cs`

```csharp
public class MachineQueue
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string MachineId { get; private set; }
    public Guid CuttingPlanId { get; private set; }
    public int Priority { get; private set; }
    public int QueuePosition { get; private set; }
    public QueueStatus Status { get; private set; }
    public DateTime? AssignedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public Guid? OperatorId { get; private set; }

    // FSM
    public void Assign(Guid operatorId, DateTime timestamp)
    {
        if (Status != QueueStatus.Queued)
            throw new InvalidStateTransitionException($"Cannot assign job in {Status} state");

        Status = QueueStatus.Assigned;
        OperatorId = operatorId;
        AssignedAt = timestamp;

        // Domain event
        RaiseDomainEvent(new JobAssignedEvent(Id, operatorId));
    }

    public void Start(DateTime timestamp)
    {
        if (Status != QueueStatus.Assigned)
            throw new InvalidStateTransitionException($"Cannot start job in {Status} state");

        Status = QueueStatus.InProgress;
        StartedAt = timestamp;

        RaiseDomainEvent(new JobStartedEvent(Id));
    }

    public void Complete(DateTime timestamp)
    {
        if (Status != QueueStatus.InProgress)
            throw new InvalidStateTransitionException($"Cannot complete job in {Status} state");

        Status = QueueStatus.Completed;
        CompletedAt = timestamp;

        RaiseDomainEvent(new JobCompletedEvent(Id));
    }

    public void Fail(string reason, DateTime timestamp)
    {
        if (Status != QueueStatus.InProgress && Status != QueueStatus.Assigned)
            throw new InvalidStateTransitionException($"Cannot fail job in {Status} state");

        Status = QueueStatus.Failed;
        CompletedAt = timestamp;

        RaiseDomainEvent(new JobFailedEvent(Id, reason));
    }
}

public enum QueueStatus
{
    Queued,
    Assigned,
    InProgress,
    Completed,
    Failed
}
```

**File:** `Domain/JobAssignment.cs`

### 3. Machine Queue Service

**File:** `Services/IMachineQueueService.cs`

```csharp
public interface IMachineQueueService
{
    Task<MachineAssignment> AssignJobToMachine(
        Guid cuttingPlanId,
        string? preferredMachineId,
        CancellationToken ct);

    Task<List<MachineQueueDto>> GetQueueForMachine(
        string machineId,
        Guid tenantId,
        CancellationToken ct);
}
```

**File:** `Services/MachineQueueService.cs`

```csharp
public class MachineQueueService : IMachineQueueService
{
    public async Task<MachineAssignment> AssignJobToMachine(
        Guid cuttingPlanId,
        string? preferredMachineId,
        CancellationToken ct)
    {
        // 1. Find available machines (not at capacity)
        var machines = await GetAvailableMachines(ct);

        // 2. Calculate priority score (from pricing + urgency)
        var priority = await CalculatePriority(cuttingPlanId, ct);

        // 3. Assign to machine with shortest queue
        var targetMachine = preferredMachineId ?? machines.OrderBy(m => m.QueueLength).First().Id;

        // 4. Create MachineQueue entity
        var queue = MachineQueue.Create(tenantId, targetMachine, cuttingPlanId, priority);

        // 5. Calculate queue position
        queue.QueuePosition = await GetNextQueuePosition(targetMachine, ct);

        await _repository.AddAsync(queue, ct);

        // 6. Publish JobQueuedEvent
        await _eventBus.PublishAsync(new JobQueuedEvent(queue.Id, targetMachine), ct);

        return new MachineAssignment
        {
            JobId = queue.Id,
            MachineId = targetMachine,
            Priority = priority,
            QueuePosition = queue.QueuePosition
        };
    }

    private async Task<int> CalculatePriority(Guid cuttingPlanId, CancellationToken ct)
    {
        // Priority based on:
        // 1. Quote price (from Pricing Engine) - higher price = higher priority
        // 2. Urgency (delivery date)
        // 3. Customer tier (premium customers first)

        // Default: FIFO (priority = timestamp)
        return (int)DateTime.UtcNow.Ticks;
    }
}
```

### 4. Application Layer

**File:** `Application/Commands/AssignJob/AssignJobCommand.cs`

**File:** `Application/Commands/StartJob/StartJobCommand.cs`

**File:** `Application/Commands/CompleteJob/CompleteJobCommand.cs`

**File:** `Application/Commands/FailJob/FailJobCommand.cs`

**File:** `Application/Queries/GetMachineQueue/GetMachineQueueQuery.cs`

### 5. API Endpoints

**File:** `Endpoints/ShopFloorEndpoints.cs`

```csharp
public static class ShopFloorEndpoints
{
    public static void MapShopFloorEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/cutting/shopfloor")
            .RequireAuthorization("operator"); // RBAC: operator role

        group.MapGet("/queue", GetMachineQueue);
        group.MapPost("/jobs/{jobId}/assign", AssignJob);
        group.MapPut("/jobs/{jobId}/start", StartJob);
        group.MapPut("/jobs/{jobId}/complete", CompleteJob);
        group.MapPut("/jobs/{jobId}/fail", FailJob);
    }

    private static async Task<IResult> GetMachineQueue(
        [FromQuery] string machineId,
        HttpContext context,
        IMachineQueueService queueService,
        CancellationToken ct)
    {
        var tenantId = context.User.GetTenantId();
        var queue = await queueService.GetQueueForMachine(machineId, tenantId, ct);
        return Results.Ok(new { machineId, queuedJobs = queue });
    }

    private static async Task<IResult> StartJob(
        [FromRoute] Guid jobId,
        [FromBody] StartJobRequest req,
        IMediator mediator,
        CancellationToken ct)
    {
        var command = new StartJobCommand(jobId, req.OperatorId, req.MachineId);
        var result = await mediator.Send(command, ct);
        return Results.Ok(result);
    }

    // Implement other endpoints...
}
```

---

## Files to Create

1. `Migrations/YYYYMMDDHHMMSS_AddMachineQueueTables.cs`
2. `Domain/MachineQueue.cs`
3. `Domain/JobAssignment.cs`
4. `Domain/Events/JobAssignedEvent.cs`
5. `Domain/Events/JobStartedEvent.cs`
6. `Domain/Events/JobCompletedEvent.cs`
7. `Domain/Events/JobFailedEvent.cs`
8. `Application/Commands/AssignJob/AssignJobCommand.cs`
9. `Application/Commands/StartJob/StartJobCommand.cs`
10. `Application/Commands/CompleteJob/CompleteJobCommand.cs`
11. `Application/Commands/FailJob/FailJobCommand.cs`
12. `Application/Queries/GetMachineQueue/GetMachineQueueQuery.cs`
13. `Services/IMachineQueueService.cs`
14. `Services/MachineQueueService.cs`
15. `Endpoints/ShopFloorEndpoints.cs`
16. `Tests/Domain/MachineQueueTests.cs`
17. `Tests/Services/MachineQueueServiceTests.cs`
18. `Tests/Integration/ShopFloorE2ETests.cs`

---

## Files to Modify

1. `Program.cs`
   - Add DI: `builder.Services.AddScoped<IMachineQueueService, MachineQueueService>();`
   - Register `ShopFloorEndpoints`

---

## Testing Requirements

### Unit Tests (MachineQueueTests.cs) — FSM Transitions

1. Queued → Assign → Assigned ✅
2. Queued → Start → InvalidStateTransitionException ❌
3. Assigned → Start → InProgress ✅
4. Assigned → Complete → InvalidStateTransitionException ❌
5. InProgress → Complete → Completed ✅
6. InProgress → Fail → Failed ✅
7. Completed → Start → InvalidStateTransitionException ❌
8. Failed → Start → InvalidStateTransitionException ❌
9. Assign twice → InvalidStateTransitionException ❌
10. Complete without Start → InvalidStateTransitionException ❌

### Unit Tests (MachineQueueServiceTests.cs)

1. AssignJobToMachine → correct priority calculated
2. AssignJobToMachine → shortest queue selected
3. GetQueueForMachine → returns jobs sorted by priority
4. Queue position calculation → correct incremental position
5. No available machines → throws MachineUnavailableException

### Integration Tests

1. E2E: Cutting Plan → Queue → Assign → Start → Complete → Analytics updated
2. E2E: Parallel job assignments (2 machines)
3. E2E: Job fail → retry logic
4. E2E: RBAC: operator role required
5. E2E: Real-time queue updates (polling simulation)

---

## Dependencies

**Blocks:**
- Analytics updates (MachineOEEHourly) — job completion triggers analytics

**Blocked by:**
- Track A (AssignBatch endpoint exists from Phase 1)
- Track B (Pricing affects queue priority)

**References:**
- Track C spec: `/opt/spaceos/docs/tervezes/SpaceOS_Cutting_Q3_Track_C_ShopFloor_Integration_v1.md`
- AssignBatch endpoint: `Endpoints/CuttingExecutionEndpoints.cs` (Phase 1)

---

## Build & Test Gate

```bash
cd /opt/spaceos/backend/spaceos-modules-cutting

# Build
dotnet build --configuration Release

# Run tests
dotnet test --filter "Category=Unit&FullyQualifiedName~MachineQueue" --logger "console;verbosity=detailed"
dotnet test --filter "Category=Integration&FullyQualifiedName~ShopFloor" --logger "console;verbosity=detailed"

# Migration
dotnet ef migrations add AddMachineQueueTables
dotnet ef database update
```

**Expected:** All FSM transitions pass (10/10), integration tests pass (5/5).

---

**Estimated effort:** 1.5 days (12 hours)
**Model:** sonnet
**Priority:** HIGH
