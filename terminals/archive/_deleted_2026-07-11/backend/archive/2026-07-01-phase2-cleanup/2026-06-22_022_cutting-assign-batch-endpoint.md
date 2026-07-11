---
id: MSG-BACKEND-022
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: CONSENSUS_TOP1-3_Design-Cutting-Nesting-Scheduling.md
created: 2026-06-22
content_hash: bbb3fc729cf0d9b92cb3cf5f28bf94bbeabf79150245f401783b56b7d51a142c
---

# Cutting TOP 3: POST /assign-batch Endpoint Implementation

## Kontextus

A **TOP 3 Machine & Operator Scheduling UI** frontend implementációjához szükséges 1 új backend endpoint a Cutting modulban.

**Doorstar use-case:** A művezetők reggel drag-and-drop interfésszel rendelik a vágási terveket a gépekhez és operátorokhoz.

## Feladat

Implementáld a `POST /cutting/api/plans/{date}/assign-batch` endpointot.

### Endpoint specifikáció

**Request:**
```
POST /cutting/api/plans/{date}/assign-batch
Content-Type: application/json

{
  "batchId": "uuid",        // CuttingPlan ID
  "machineId": "uuid",      // Workstation UUID (Kernel tools)
  "operatorId": "uuid",     // User UUID (Keycloak tenantId attr)
  "priority": 5,            // 1-10 scale
  "startTime": "2026-06-22T08:00:00Z"  // ISO8601
}
```

**Response:**
```json
{
  "executionId": "uuid",    // CuttingExecution aggregátum UUID
  "status": "Planned"       // FSM státusz
}
```

### Implementációs scope

1. **Domain:** `CuttingExecution` aggregátum kiterjesztése
   - Új command vagy meglévő `ScheduleExecution` command kiterjesztése `machineId` + `operatorId` paraméterekkel
   - FSM transition: `Draft → Planned` (már létezik)

2. **Application:** Command handler
   - Validáció: `batchId`, `machineId`, `operatorId` léteznek
   - Priority range: 1-10
   - Idempotencia: `batchId` + `date` unique constraint

3. **API:** Presentation layer
   - Új endpoint: `src/SpaceOS.Modules.Cutting.Api/Endpoints/CuttingPlanningEndpoints.cs`
   - RBAC: `machine_operator` vagy `production_manager` role check
   - HTTP 201 Created response

4. **Tesztek (minimum 3):**
   - Integration test: `AssignBatch_ValidRequest_ReturnsExecutionId`
   - FSM test: `CuttingExecution.ScheduleWithAssignment_TransitionsToPlannedState`
   - RBAC test: `AssignBatch_NonOperatorRole_Returns403`

### DoD

- [ ] Endpoint működik: `POST /cutting/api/plans/{date}/assign-batch`
- [ ] +3 teszt pass (integration, FSM, RBAC)
- [ ] `dotnet test` pass (931+ tests)
- [ ] `dotnet publish` sikeres
- [ ] RBAC: csak `machine_operator` vagy `production_manager` hívhatja

### Implementációs irányelvek

**Követendő minták:**
- Cutting modul már használ FSM-et és RBAC-t
- Kerül CuttingExecution endpoints: `src/.../Endpoints/CuttingExecutionEndpoints.cs`
- Provider pattern: Kernel Tools API már elérhető (workstation validáláshoz)

**Kockázatok:**
- Idempotencia: `batchId` + `date` PostgreSQL unique constraint
- RBAC: JWT claim-ek ellenőrzése endpoint szinten

### Kapcsolódó fájlok

- `backend/spaceos-modules-cutting/src/SpaceOS.Modules.Cutting.Domain/`
- `backend/spaceos-modules-cutting/src/SpaceOS.Modules.Cutting.Application/Commands/`
- `backend/spaceos-modules-cutting/src/SpaceOS.Modules.Cutting.Api/Endpoints/`
- `backend/spaceos-modules-cutting/tests/`

### Becsült idő

**1 nap** (handler + domain logic + 3 teszt)

---

**Priority:** HIGH — Frontend TOP 3 blokkolt ezen endpoint nélkül
**Ref:** `/opt/spaceos/docs/tasks/archive/CONSENSUS_TOP1-3_Design-Cutting-Nesting-Scheduling.md`
