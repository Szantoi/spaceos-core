---
id: MSG-CUTTING-054
from: root
to: cutting
type: task
priority: high
status: UNREAD
model: sonnet
ref: MSG-ROOT-001
created: 2026-06-17
---

# Cutting — POST /assign-batch endpoint (TOP 3 dependency)

## Összefoglaló

A Planning Pipeline consensus TOP 3 (Machine & Operator Scheduling UI) implementációjához szükséges az **assign-batch endpoint** a Cutting Planning handler-ben.

**Scope:** `POST /cutting/api/plans/{date}/assign-batch` endpoint + CuttingExecution domain logic + RBAC + tesztek

**Becs. munka:** 1 nap BE

**Priority:** HIGH — TOP 3 FE track 1 napot vár erre

---

## Spec (consensus tervből)

### Backend endpoint

**Endpoint:** `POST /cutting/api/plans/{date}/assign-batch`

**Route params:**
- `date` (string, ISO8601 date) — pl. `2026-06-17`

**Request body:**
```json
{
  "batchId": "uuid",
  "machineId": "uuid",
  "operatorId": "uuid",
  "priority": 5,
  "startTime": "2026-06-17T09:00:00Z"
}
```

**Response (200 OK):**
```json
{
  "executionId": "uuid",
  "status": "Planned"
}
```

**Error codes:**
- 400 Bad Request — invalid batch/machine/operator
- 401 Unauthorized — JWT missing
- 403 Forbidden — insufficient RBAC (not machine_operator/production_manager)
- 409 Conflict — batch already assigned

### Implementation guide

1. **Endpoint location:** `Cutting.Api/Endpoints/CuttingPlanningEndpoints.cs`

2. **Domain logic:**
   - `CuttingExecution` aggregate root (már létezik)
   - Extend `ScheduleExecution` command: add `machineId`, `operatorId` params
   - FSM transition: `Draft → Planned`
   - Idempotencia: `(batchId, date)` unique constraint PostgreSQL-ben

3. **RBAC:**
   - Requires `[Authorize(Roles = "machine_operator,production_manager")]`
   - Keycloak JWT + tenantId check

4. **Validation:**
   - `batchId` exists in DB (CuttingPlan aggregate)
   - `machineId` exists in Kernel tools/workstations
   - `operatorId` exists in Identity users
   - `priority` ∈ [1, 10]
   - `startTime` ≥ now (no past scheduling)

5. **Tests:**
   - `AssignBatch_ValidRequest_ReturnsExecutionId`
   - `AssignBatch_InvalidRole_Returns403`
   - `AssignBatch_DuplicateBatch_Returns409` (idempotencia)
   - `CuttingExecution.ScheduleWithAssignment_TransitionsToPlanndState` (FSM)

### Tesztek

- +4 BE test pass
- Teljes Cutting teszt coverage: ~931 → 935 teszt

---

## Precedencia (TOP 3 FE)

TOP 3 Frontend (`BatchAssignmentBoard.tsx`) ezt az endpoint-ot hívja a drag-drop assign flow-ban:

```javascript
// BatchAssignmentBoard.tsx: drop callback
const handleDrop = async (batchId: string, machineId: string) => {
  const res = await fetch(`${API_BASE.cutting}/api/plans/${dateString}/assign-batch`, {
    method: 'POST',
    body: JSON.stringify({ batchId, machineId, operatorId: currentUser.id, priority: 5, startTime: new Date().toISOString() })
  });
  const { executionId } = await res.json();
  // optimistic update: batch move to assigned column
};
```

**Kritikus:** Ezt az endpoint-ot TOP 1-2 előtt **NEM szükséges**, csak TOP 3 FE-hez.

---

## DoD (Definition of Done)

- [ ] `POST /cutting/api/plans/{date}/assign-batch` endpoint deployed
- [ ] `CuttingExecution` domain logic + `ScheduleWithAssignment` command
- [ ] PostgreSQL unique constraint: `(batchId, date)` idempotencia
- [ ] RBAC: `[Authorize(Roles = "machine_operator,production_manager")]`
- [ ] +4 BE teszt pass (validation, FSM, RBAC, idempotencia)
- [ ] API dokumentáció (Swagger tag: `Cutting Planning`)
- [ ] 0 dotnet build error
- [ ] Smoke test: `curl -X POST -H "Authorization: Bearer <token>" -H "Content-Type: application/json" -d '{"batchId":"..","machineId":"..","operatorId":"..","priority":5,"startTime":"..."}' https://api.joinerytech.hu/cutting/api/plans/2026-06-17/assign-batch`

---

## Relationship (TOP 3 FE-vel)

**Blokk:** TOP 3 FE-nek kell ez az endpoint azonnali indításához
**Ajánlott sorrend:** PÁRHUZAMOS (Cutting ezt a napot, FE TOP 1-2 párhuzamosan, TOP 3 után)

---

## Siguinte lépés

1. Cutting terminál: `AssignBatch` endpoint implementáció (1 nap)
2. Deploy + smoke test
3. FE terminál: TOP 3 implementáció (4-5 nap, backend után)

🚀 Indítás: **AZONNAL** (párhuzamosan TOP 1 FE-vel)
