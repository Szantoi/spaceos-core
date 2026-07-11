---
id: MSG-BACKEND-032
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: Q3-CUTTING-EXPANSION
created: 2026-06-22
content_hash: 6ad78b3d9260b8d8103ccb9b469204565909eb40bbabcf5d725c4f200f6317eb
---

# Q3 Track C: ShopFloor Integration - Machine Queue + Kiosk Workflow

## Összefoglaló

Implementáld a **ShopFloor Integration** backend API-ját, amely lehetővé teszi a szabászgépek munkavárólista kezelését és a kiosk workflow támogatását (operátori bejelentkezés, feladat átvétel, státusz frissítés).

## Scope

**Modul:** `spaceos-modules-cutting` (port 5005)
**Időkeret:** 2 nap (Track C)
**Prioritás:** HIGH — Doorstar production workflow része

## Implementációs lépések

### 1. Domain Layer Bővítés (0.5 nap)

**Új aggregate:** `MachineQueue`

```csharp
public class MachineQueue : AggregateRoot<Guid>
{
    public Guid TenantId { get; private set; }
    public Guid WorkstationId { get; private set; } // Kernel tools
    public List<QueuedBatch> Batches { get; private set; }
    public int Priority { get; private set; } // 1-10
    public MachineQueueStatus Status { get; private set; }

    // Factory
    public static MachineQueue Create(Guid tenantId, Guid workstationId);

    // Methods
    public void AddBatch(Guid batchId, int priority);
    public void RemoveBatch(Guid batchId);
    public void ReorderBatches(List<Guid> orderedBatchIds);
    public void AssignOperator(Guid operatorUserId);
    public void StartBatch(Guid batchId);
    public void CompleteBatch(Guid batchId);
}

public class QueuedBatch : Entity<Guid>
{
    public Guid BatchId { get; private set; }
    public int QueuePosition { get; private set; }
    public QueuedBatchStatus Status { get; private set; } // Queued, InProgress, Completed
    public Guid? AssignedOperatorId { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
}

public enum MachineQueueStatus
{
    Idle = 0,
    Active = 1,
    Maintenance = 2
}

public enum QueuedBatchStatus
{
    Queued = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}
```

**Bővített `CuttingPlanBatch` aggregate:**

```csharp
public class CuttingPlanBatch
{
    // Existing fields...
    public Guid? AssignedMachineId { get; private set; }
    public Guid? AssignedOperatorId { get; private set; }
    public DateTime? ProductionStartedAt { get; private set; }
    public DateTime? ProductionCompletedAt { get; private set; }

    // New methods
    public void AssignToMachine(Guid workstationId, Guid operatorId);
    public void StartProduction();
    public void CompleteProduction();
}
```

**Domain Events:**
- `BatchAssignedToMachineEvent`
- `BatchProductionStartedEvent`
- `BatchProductionCompletedEvent`
- `MachineQueueReorderedEvent`

### 2. Application Layer (0.5 nap)

**Commands:**
```csharp
public record AssignBatchToMachineCommand(
    Guid BatchId,
    Guid WorkstationId,
    Guid OperatorId
) : IRequest;

public record StartBatchProductionCommand(Guid BatchId) : IRequest;

public record CompleteBatchProductionCommand(
    Guid BatchId,
    int ProducedPieces,
    int WastePieces
) : IRequest;

public record ReorderMachineQueueCommand(
    Guid WorkstationId,
    List<Guid> OrderedBatchIds
) : IRequest;

public record OperatorLoginCommand(
    string OperatorPin,
    Guid WorkstationId
) : IRequest<OperatorSessionDto>;
```

**Queries:**
```csharp
public record GetMachineQueueQuery(Guid WorkstationId) : IRequest<MachineQueueDto>;

public record GetActiveBatchesQuery(Guid? WorkstationId) : IRequest<List<BatchDto>>;

public record GetOperatorSessionQuery(Guid SessionId) : IRequest<OperatorSessionDto>;
```

### 3. Infrastructure Layer (0.5 nap)

**Database:**
```sql
-- spaceos_cutting schema (már létezik)

CREATE TABLE spaceos_cutting.machine_queues (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL,
    workstation_id UUID NOT NULL,
    priority INT NOT NULL DEFAULT 5,
    status INT NOT NULL,
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP NOT NULL
);

CREATE TABLE spaceos_cutting.queued_batches (
    id UUID PRIMARY KEY,
    machine_queue_id UUID NOT NULL REFERENCES spaceos_cutting.machine_queues(id),
    batch_id UUID NOT NULL REFERENCES spaceos_cutting.cutting_plan_batches(id),
    queue_position INT NOT NULL,
    status INT NOT NULL,
    assigned_operator_id UUID,
    started_at TIMESTAMP,
    completed_at TIMESTAMP
);

-- Extend cutting_plan_batches
ALTER TABLE spaceos_cutting.cutting_plan_batches
    ADD COLUMN assigned_machine_id UUID,
    ADD COLUMN assigned_operator_id UUID,
    ADD COLUMN production_started_at TIMESTAMP,
    ADD COLUMN production_completed_at TIMESTAMP;

CREATE TABLE spaceos_cutting.operator_sessions (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL,
    operator_user_id UUID NOT NULL,
    workstation_id UUID NOT NULL,
    login_at TIMESTAMP NOT NULL,
    logout_at TIMESTAMP,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);
```

**RLS:**
```sql
ALTER TABLE spaceos_cutting.machine_queues ENABLE ROW LEVEL SECURITY;
CREATE POLICY machine_queues_tenant_isolation ON spaceos_cutting.machine_queues
    USING (tenant_id::text = current_setting('app.current_tenant_id', TRUE));

ALTER TABLE spaceos_cutting.operator_sessions ENABLE ROW LEVEL SECURITY;
CREATE POLICY operator_sessions_tenant_isolation ON spaceos_cutting.operator_sessions
    USING (tenant_id::text = current_setting('app.current_tenant_id', TRUE));
```

### 4. API Layer (0.5 nap)

**ShopFloor Endpoints:**

```http
# Kiosk operátor login (PIN-based)
POST /cutting/api/shopfloor/login
Content-Type: application/json
{
  "operatorPin": "1234",
  "workstationId": "guid"
}

Response: 200 OK
{
  "sessionId": "guid",
  "operatorId": "guid",
  "operatorName": "Kovács János",
  "workstationId": "guid",
  "workstationName": "Holzma HPP-380",
  "loginAt": "2026-06-22T10:00:00Z"
}

# Machine queue lekérdezés
GET /cutting/api/shopfloor/machines/{workstationId}/queue

Response: 200 OK
{
  "workstationId": "guid",
  "workstationName": "Holzma HPP-380",
  "status": "Active",
  "batches": [
    {
      "batchId": "guid",
      "queuePosition": 1,
      "status": "InProgress",
      "assignedOperator": "Kovács János",
      "estimatedDuration": "45 minutes",
      "pieceCount": 120
    },
    {
      "batchId": "guid",
      "queuePosition": 2,
      "status": "Queued",
      "estimatedDuration": "30 minutes",
      "pieceCount": 80
    }
  ]
}

# Batch assign géphez (Manager/Admin role)
POST /cutting/api/shopfloor/batches/{batchId}/assign
Content-Type: application/json
Authorization: Bearer {token}
{
  "workstationId": "guid",
  "operatorId": "guid"
}

# Batch gyártás indítás (Kiosk)
POST /cutting/api/shopfloor/batches/{batchId}/start
Content-Type: application/json
{
  "sessionId": "guid"
}

# Batch gyártás befejezés (Kiosk)
POST /cutting/api/shopfloor/batches/{batchId}/complete
Content-Type: application/json
{
  "sessionId": "guid",
  "producedPieces": 120,
  "wastePieces": 5
}

# Queue átrendezés (Manager/Admin)
POST /cutting/api/shopfloor/machines/{workstationId}/queue/reorder
Content-Type: application/json
Authorization: Bearer {token}
{
  "orderedBatchIds": ["guid1", "guid2", "guid3"]
}

# Operátor logout
POST /cutting/api/shopfloor/logout
Content-Type: application/json
{
  "sessionId": "guid"
}
```

### 5. Security & RBAC

**Roles:**
- `shopfloor_operator` — kiosk login, start/complete batch
- `shopfloor_supervisor` — queue reorder, batch assign
- `cutting_admin` — minden művelet

**PIN-based Authentication:**
- Operátorhoz tartozik egy 4-jegyű PIN (`User.OperatorPin` — Kernel user attribútum)
- PIN validáció: `IOperatorAuthService`
- Session timeout: 8 óra (műszak vége)

### 6. Tesztek

**Minimum:**
- 10 unit tests (MachineQueue aggregate FSM)
- 8 integration tests (queue operations + RLS)
- 8 API tests (login, assign, start, complete, reorder)

**Összesen:** 26+ teszt

## Definition of Done

✅ MachineQueue aggregate + QueuedBatch implementálva
✅ CuttingPlanBatch bővítve (production tracking)
✅ OperatorSession handling (PIN login)
✅ Database migrations + RLS
✅ ShopFloor API endpoints (`/cutting/api/shopfloor/*`)
✅ RBAC (shopfloor_operator, shopfloor_supervisor)
✅ 26+ teszt pass
✅ OpenAPI docs frissítve

## Blokkolók

**NONE** — Track A/B párhuzamosan futhat.

## Kapcsolódó feladatok

- **Frontend:** MSG-FRONTEND-020 (ShopFloor Kiosk UI)
- **Identity:** Operator PIN management (later, manual workaround most)

## Referenciák

- Kernel tools/workstations: `GET /api/tools/workstations`
- Cutting batches: `spaceos_cutting.cutting_plan_batches`

---

**Határidő:** 2026-06-28 (Track C, 2 nap)
**Assigned to:** Backend terminal
**Model:** sonnet
