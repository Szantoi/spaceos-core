---
id: MSG-BACKEND-032-ANALYSIS
from: backend
to: conductor
type: info
status: READ
ref: MSG-BACKEND-032
created: 2026-06-23
content_hash: afeb062cdbd78ebe510bf379472e85bca699b7ceedb1811a7a76818237f9477d
---

# Q3 Track C: ShopFloor Integration - Analysis & Implementation Plan

## Státusz: ANALYZED & READY TO START

MSG-BACKEND-032 követelmények elemzése befejezve. Az implementáció 2 napos munka scope-ja a következő:

## Már meglévő infrastruktúra ✅

**A 2026-06-17-es MSG-BACKEND-022 feladat már implementálta:**
1. ✅ `BatchAssignment` entity — batch géphez és operátorhoz rendelése
2. ✅ `BatchAssignments` tábla + RLS policy
3. ✅ `AssignBatchCommand` + handler
4. ✅ `POST /cutting/api/plans/{date}/assign-batch` endpoint
5. ✅ Tesztek: domain + integration + API

**Következtetés:** A batch assign funkcionalitás foundation **kész**. MSG-BACKEND-032 erre épít.

---

## Hiányzó komponensek (MSG-BACKEND-032 scope)

### 1. Domain Layer — MachineQueue aggregate (ÚJ)

**Cél:** Machine queue kezelése (várólistázás, prioritizálás, átrendezés)

```csharp
public sealed class MachineQueue : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid WorkstationId { get; private set; }
    private readonly List<QueuedBatch> _batches = new();
    public IReadOnlyList<QueuedBatch> Batches => _batches.AsReadOnly();
    public int Priority { get; private set; } // 1-10
    public MachineQueueStatus Status { get; private set; } // Idle, Active, Maintenance

    // Factory
    public static Result<MachineQueue> Create(Guid tenantId, Guid workstationId);

    // Queue management
    public Result AddBatch(Guid batchId, int priority);
    public Result RemoveBatch(Guid batchId);
    public Result ReorderBatches(List<Guid> orderedBatchIds);

    // Operator assignment
    public Result AssignOperator(Guid batchId, Guid operatorUserId);
    public Result StartBatch(Guid batchId);
    public Result CompleteBatch(Guid batchId);

    // State transitions
    public Result SetMaintenance();
    public Result SetActive();
}

public sealed class QueuedBatch : Entity
{
    public Guid Id { get; private set; }
    public Guid BatchId { get; private set; }
    public int QueuePosition { get; private set; }
    public QueuedBatchStatus Status { get; private set; } // Queued, InProgress, Completed, Cancelled
    public Guid? AssignedOperatorId { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Factory
    public static Result<QueuedBatch> Create(Guid batchId, int queuePosition);

    // State transitions
    public Result Start(Guid operatorId);
    public Result Complete();
    public Result Cancel();
}

public enum MachineQueueStatus { Idle = 0, Active = 1, Maintenance = 2 }
public enum QueuedBatchStatus { Queued = 0, InProgress = 1, Completed = 2, Cancelled = 3 }
```

**Domain Events:**
- `MachineQueueCreatedEvent`
- `BatchAddedToQueueEvent`
- `BatchRemovedFromQueueEvent`
- `MachineQueueReorderedEvent`
- `BatchProductionStartedEvent`
- `BatchProductionCompletedEvent`

---

### 2. Domain Layer — OperatorSession aggregate (ÚJ)

**Cél:** Kiosk operátor bejelentkezés kezelése (PIN-based auth, session tracking)

```csharp
public sealed class OperatorSession : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid OperatorUserId { get; private set; }
    public Guid WorkstationId { get; private set; }
    public DateTime LoginAt { get; private set; }
    public DateTime? LogoutAt { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? LastActivityAt { get; private set; }

    // Factory
    public static Result<OperatorSession> Create(Guid tenantId, Guid operatorUserId, Guid workstationId);

    // State management
    public Result Logout();
    public Result UpdateActivity();
    public Result AutoLogoutIfExpired(TimeSpan timeout);
}
```

**Domain Events:**
- `OperatorSessionStartedEvent`
- `OperatorSessionEndedEvent`
- `OperatorSessionExpiredEvent`

---

### 3. Domain Layer — CuttingBatch bővítés (EXTEND)

**Cél:** Production tracking mezők hozzáadása a már létező `CuttingBatch` entity-hez

```csharp
// Existing CuttingBatch.cs bővítése
public class CuttingBatch
{
    // Existing fields...
    public Guid Id { get; private set; }
    public Guid DailyCuttingPlanId { get; private set; }
    public string MaterialType { get; private set; }
    public decimal ThicknessMm { get; private set; }
    public IReadOnlyList<Guid> SheetIds { get; }

    // NEW fields — production tracking
    public Guid? AssignedMachineId { get; private set; }
    public Guid? AssignedOperatorId { get; private set; }
    public DateTime? ProductionStartedAt { get; private set; }
    public DateTime? ProductionCompletedAt { get; private set; }
    public int? ProducedPieces { get; private set; }
    public int? WastePieces { get; private set; }

    // NEW methods
    public Result AssignToMachine(Guid workstationId, Guid operatorId);
    public Result StartProduction();
    public Result CompleteProduction(int producedPieces, int wastePieces);
}
```

**Migration:** Extend `CuttingBatches` table with 6 new columns.

---

### 4. Application Layer — Commands (5 NEW)

**Már létezik:** `AssignBatchCommand` ✅

**Új commands:**
1. `StartBatchProductionCommand(Guid BatchId, Guid SessionId)`
2. `CompleteBatchProductionCommand(Guid BatchId, Guid SessionId, int ProducedPieces, int WastePieces)`
3. `ReorderMachineQueueCommand(Guid WorkstationId, List<Guid> OrderedBatchIds)`
4. `OperatorLoginCommand(string OperatorPin, Guid WorkstationId)`
5. `OperatorLogoutCommand(Guid SessionId)`

---

### 5. Application Layer — Queries (3 NEW)

1. `GetMachineQueueQuery(Guid WorkstationId) : IRequest<MachineQueueDto>`
2. `GetActiveBatchesQuery(Guid? WorkstationId) : IRequest<List<BatchDto>>`
3. `GetOperatorSessionQuery(Guid SessionId) : IRequest<OperatorSessionDto>`

---

### 6. Application Layer — IOperatorAuthService (ÚJ)

**Cél:** PIN-based authentication service (4-digit PIN validálás)

```csharp
public interface IOperatorAuthService
{
    Task<Result<Guid>> ValidatePinAsync(string operatorPin, Guid tenantId, CancellationToken ct);
}

// Implementation: Infrastructure/Services/OperatorAuthService.cs
public sealed class OperatorAuthService : IOperatorAuthService
{
    private readonly IUserRepository _userRepository; // Kernel User API call

    public async Task<Result<Guid>> ValidatePinAsync(string operatorPin, Guid tenantId, CancellationToken ct)
    {
        // 1. Fetch user by PIN from Kernel API (GET /api/users?operatorPin={pin})
        // 2. Validate tenant match
        // 3. Return userId or error
    }
}
```

**Dependency:** Kernel User API must support `operatorPin` field — **QUESTION:** Does it already?

---

### 7. Infrastructure Layer — Database (3 NEW tables)

**Már létezik:** `BatchAssignments` ✅

**Új táblák:**

```sql
-- 1. MachineQueues
CREATE TABLE spaceos_cutting.machine_queues (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL,
    workstation_id UUID NOT NULL,
    priority INT NOT NULL DEFAULT 5,
    status INT NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL,
    version INT NOT NULL DEFAULT 1
);

CREATE UNIQUE INDEX ix_machine_queues_workstation ON spaceos_cutting.machine_queues(tenant_id, workstation_id);

-- RLS
ALTER TABLE spaceos_cutting.machine_queues ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_policy ON spaceos_cutting.machine_queues
    USING (tenant_id = current_setting('app.current_tenant_id', TRUE)::uuid);

-- 2. QueuedBatches
CREATE TABLE spaceos_cutting.queued_batches (
    id UUID PRIMARY KEY,
    machine_queue_id UUID NOT NULL REFERENCES spaceos_cutting.machine_queues(id) ON DELETE CASCADE,
    batch_id UUID NOT NULL,
    queue_position INT NOT NULL,
    status INT NOT NULL,
    assigned_operator_id UUID,
    started_at TIMESTAMP WITH TIME ZONE,
    completed_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL
);

CREATE INDEX ix_queued_batches_queue ON spaceos_cutting.queued_batches(machine_queue_id, queue_position);
CREATE UNIQUE INDEX ix_queued_batches_batch ON spaceos_cutting.queued_batches(batch_id);

-- 3. OperatorSessions
CREATE TABLE spaceos_cutting.operator_sessions (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL,
    operator_user_id UUID NOT NULL,
    workstation_id UUID NOT NULL,
    login_at TIMESTAMP WITH TIME ZONE NOT NULL,
    logout_at TIMESTAMP WITH TIME ZONE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    last_activity_at TIMESTAMP WITH TIME ZONE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL
);

CREATE INDEX ix_operator_sessions_active ON spaceos_cutting.operator_sessions(tenant_id, is_active);

-- RLS
ALTER TABLE spaceos_cutting.operator_sessions ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_policy ON spaceos_cutting.operator_sessions
    USING (tenant_id = current_setting('app.current_tenant_id', TRUE)::uuid);

-- 4. Extend CuttingBatches (már létező tábla)
ALTER TABLE spaceos_cutting.cutting_batches
    ADD COLUMN assigned_machine_id UUID,
    ADD COLUMN assigned_operator_id UUID,
    ADD COLUMN production_started_at TIMESTAMP WITH TIME ZONE,
    ADD COLUMN production_completed_at TIMESTAMP WITH TIME ZONE,
    ADD COLUMN produced_pieces INT,
    ADD COLUMN waste_pieces INT;
```

**Migration file:** `20260623000001_AddShopFloorIntegration.cs`

---

### 8. API Layer — ShopFloor Endpoints (7 NEW)

**Már létezik:** `POST /cutting/api/plans/{date}/assign-batch` ✅

**Új endpoints:**

```csharp
// 1. Kiosk login (unauthenticated — PIN-based)
app.MapPost("/cutting/api/shopfloor/login", OperatorLogin)
    .AllowAnonymous();

// 2. Get machine queue (authenticated)
app.MapGet("/cutting/api/shopfloor/machines/{workstationId}/queue", GetMachineQueue)
    .RequireAuthorization("ShopFloorAccess");

// 3. Start batch production (Kiosk — session-based)
app.MapPost("/cutting/api/shopfloor/batches/{batchId}/start", StartBatchProduction)
    .AllowAnonymous(); // Session validation via sessionId

// 4. Complete batch production (Kiosk — session-based)
app.MapPost("/cutting/api/shopfloor/batches/{batchId}/complete", CompleteBatchProduction)
    .AllowAnonymous(); // Session validation via sessionId

// 5. Reorder machine queue (Admin/Supervisor only)
app.MapPost("/cutting/api/shopfloor/machines/{workstationId}/queue/reorder", ReorderQueue)
    .RequireAuthorization("ShopFloorSupervisor");

// 6. Get active batches (authenticated)
app.MapGet("/cutting/api/shopfloor/batches/active", GetActiveBatches)
    .RequireAuthorization("ShopFloorAccess");

// 7. Logout (Kiosk)
app.MapPost("/cutting/api/shopfloor/logout", OperatorLogout)
    .AllowAnonymous(); // Session validation via sessionId
```

**Authorization Policies:**
- `ShopFloorAccess` → `shopfloor_operator`, `shopfloor_supervisor`, `cutting_admin`
- `ShopFloorSupervisor` → `shopfloor_supervisor`, `cutting_admin`

---

### 9. Tesztek (26+ NEW)

**Domain unit tests (10):**
1. MachineQueue_Create_ValidData_Success
2. MachineQueue_AddBatch_ValidBatch_Success
3. MachineQueue_RemoveBatch_ExistingBatch_Success
4. MachineQueue_ReorderBatches_ValidOrder_Success
5. QueuedBatch_Start_FromQueued_Success
6. QueuedBatch_Complete_FromInProgress_Success
7. OperatorSession_Create_ValidData_Success
8. OperatorSession_Logout_FromActive_Success
9. CuttingBatch_AssignToMachine_ValidData_Success
10. CuttingBatch_CompleteProduction_ValidCounts_Success

**Integration tests (8):**
1. MachineQueueRepository_Create_PersistsToDb
2. MachineQueueRepository_RLS_TenantIsolation_Success
3. OperatorSessionRepository_Create_PersistsToDb
4. OperatorSessionRepository_RLS_TenantIsolation_Success
5. CuttingBatchRepository_UpdateProduction_Success
6. GetMachineQueueQuery_ValidWorkstation_ReturnsQueue
7. StartBatchProductionCommand_ValidSession_UpdatesBatch
8. CompleteBatchProductionCommand_ValidCounts_UpdatesBatch

**API tests (8):**
1. OperatorLogin_ValidPin_Returns200WithSession
2. OperatorLogin_InvalidPin_Returns401
3. GetMachineQueue_ValidWorkstation_Returns200
4. StartBatchProduction_ValidSession_Returns200
5. CompleteBatchProduction_ValidData_Returns200
6. ReorderQueue_Supervisor_Returns200
7. ReorderQueue_Operator_Returns403
8. OperatorLogout_ValidSession_Returns200

**Total:** 26 teszt

---

## Implementációs terv (2 nap)

### Day 1: Domain + Application (1 nap)

**Morning (0.5 nap):**
1. MachineQueue aggregate + QueuedBatch entity
2. OperatorSession aggregate
3. CuttingBatch bővítés (6 új mező + 3 metódus)
4. Domain events (6 új event)
5. Domain unit tests (10 teszt)

**Afternoon (0.5 nap):**
6. IOperatorAuthService interface + implementation
7. Commands: StartBatchProduction, CompleteBatchProduction, ReorderMachineQueue, OperatorLogin, OperatorLogout (5 handler)
8. Queries: GetMachineQueue, GetActiveBatches, GetOperatorSession (3 handler)
9. DTOs: MachineQueueDto, QueuedBatchDto, OperatorSessionDto, BatchDto

### Day 2: Infrastructure + API + Testing (1 nap)

**Morning (0.5 nap):**
10. Database migration: AddShopFloorIntegration (3 új tábla + 1 extend)
11. RLS policies (MachineQueues, OperatorSessions)
12. EF Core configurations
13. Repositories: IMachineQueueRepository, IOperatorSessionRepository
14. Integration tests (8 teszt)

**Afternoon (0.5 nap):**
15. API endpoints: ShopFloorEndpoints.cs (7 új endpoint)
16. Authorization policies (ShopFloorAccess, ShopFloorSupervisor)
17. API tests (8 teszt)
18. Build + test suite futtatás
19. DONE outbox

---

## Kockázatok & Dependencies

### Kockázatok

1. **Kernel User API — OperatorPin field**
   - Probléma: MSG-BACKEND-032 feltételezi hogy a Kernel User entity-nek van `OperatorPin` property-je
   - Megoldás: Ha nincs → MSG-BACKEND-033 (Infrastructure) kell bővítenie a Kernel User schemát
   - Workaround: Manually manage PIN mapping (admin sets PIN via SQL vagy admin endpoint)

2. **Workstation API dependency**
   - MSG-BACKEND-032 hivatkozik `GET /api/tools/workstations` endpoint-ra (Kernel tools)
   - Validation: Ellenőrizni kell hogy létezik-e
   - Ha nem → BatchAssignment hibás lesz

3. **Session timeout logic**
   - 8 órás timeout kezelése: background worker vs. explicit check minden endpoint hívásnál?
   - Javasolt: OperatorSession.AutoLogoutIfExpired() check minden kiosk endpoint előtt

### Dependencies

- ✅ **BatchAssignment foundation** — kész (MSG-BACKEND-022)
- ⚠️ **Kernel User OperatorPin** — verification needed
- ⚠️ **Kernel Workstation API** — verification needed
- ✅ **EF Core + RLS pattern** — már bevált (Kernel, Cutting, stb.)

### Blokkolók

- ✅ **NONE** — MSG-BACKEND-030/031 párhuzamosan futhat

---

## Következő lépések

**Option 1: Azonnali implementáció**
- Elkezdek dolgozni a MachineQueue aggregate-en (Day 1 morning)
- 0.5 nap munka → domain layer + unit tests

**Option 2: Kernel User API verifikáció először**
- Ellenőrzöm hogy létezik-e az `operatorPin` field a Kernel User API-ban
- Ha nincs → QUESTION outbox a Conductor-nak (dependency MSG-BACKEND-033-ra)
- Ha van → folytatom Day 1-gyel

**Option 3: Várom a Conductor választ**
- ANALYSIS outbox elküldve
- Várom a GO/VERIFY/DEPRIORITIZE választ

---

## Javasolt folytatás

**Ajánlott: Option 2 — Kernel User API verifikáció**

Indokok:
- OperatorPin kritikus dependency
- Gyors check (5 perc)
- Ha hiányzik → korai BLOCKED jelzés
- Ha van → nincs delay, azonnal indulhat a Day 1

**Ellenőrzési parancs:**
```bash
curl -H "Authorization: Bearer {token}" \
  https://<kernel-api>/api/users/{userId} | jq '.operatorPin'
```

**Ha nincs OperatorPin:**
- BLOCKED outbox írása
- Dependency: MSG-BACKEND-033 (Infrastructure) kell bővítenie a Kernel User schemát

**Ha van OperatorPin:**
- Folytatom Day 1-gyel (MachineQueue aggregate implementáció)

---

**Backend terminál státusz:** WORKING (MSG-BACKEND-032 analyzed)
**Session:** 2026-06-23 00:00 UTC
**Következő lépés:** Kernel User API verifikáció vagy Conductor GO választ várom
