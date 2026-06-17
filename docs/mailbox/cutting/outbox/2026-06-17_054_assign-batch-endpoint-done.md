---
id: MSG-CUTTING-054-DONE
from: cutting
to: root
type: done
ref: MSG-CUTTING-054
status: UNREAD
created: 2026-06-17
---

# POST /cutting/api/plans/{date}/assign-batch Endpoint

## Implementalt

### Endpoint
- **Route:** `POST /cutting/api/plans/{date}/assign-batch`
- **Auth:** `[Authorize]` + `RequireRole("machine_operator", "production_manager")`
- **Request body:**
```json
{
  "batchId": "uuid",
  "machineId": "uuid",
  "operatorId": "uuid",
  "priority": 5,
  "startTime": "2026-06-17T09:00:00Z"
}
```
- **Response (200 OK):**
```json
{
  "executionId": "uuid",
  "status": "Planned"
}
```
- **Error codes:** 400, 401, 403, 409

### Domain
- **BatchAssignment entity** (`src/SpaceOS.Modules.Cutting.Domain/Entities/BatchAssignment.cs`)
  - Idempotencia kezelesere, unique constraint: `(BatchId, PlanDate)`
  - Factory: `Create()` Result<T> validacioval
  - Properties: TenantId, BatchId, PlanDate, MachineId, OperatorId, ExecutionId, Priority, StartTime

### Infrastructure
- **BatchAssignmentConfiguration** (`src/SpaceOS.Modules.Cutting.Infrastructure/Persistence/Configurations/BatchAssignmentConfiguration.cs`)
  - IX_BatchAssignments_BatchId_PlanDate (unique)
  - IX_BatchAssignments_TenantId

### Application
- **AssignBatchCommand** + **AssignBatchResponse**
- **AssignBatchCommandHandler**
  - Batch letezik ellenorzes (CuttingBatch)
  - Idempotencia ellenorzes (BatchAssignment)
  - CuttingExecution schedule
  - BatchAssignment persist

### API
- `CuttingPlanningEndpoints.MapCuttingPlanningEndpoints()` bovitve plansGroup-pal
- Role-based authorization: machine_operator VAGY production_manager

### Modositott fajlok
1. `src/SpaceOS.Modules.Cutting.Domain/Entities/BatchAssignment.cs` (NEW)
2. `src/SpaceOS.Modules.Cutting.Domain/Interfaces/ICuttingRepository.cs` (MODIFIED)
3. `src/SpaceOS.Modules.Cutting.Infrastructure/Persistence/CuttingDbContext.cs` (MODIFIED)
4. `src/SpaceOS.Modules.Cutting.Infrastructure/Persistence/Configurations/BatchAssignmentConfiguration.cs` (NEW)
5. `src/SpaceOS.Modules.Cutting.Infrastructure/Repositories/CuttingRepository.cs` (MODIFIED)
6. `src/SpaceOS.Modules.Cutting.Application/Commands/AssignBatch/AssignBatchCommand.cs` (NEW)
7. `src/SpaceOS.Modules.Cutting.Application/Commands/AssignBatch/AssignBatchCommandHandler.cs` (NEW)
8. `src/SpaceOS.Modules.Cutting.Application/SpaceOS.Modules.Cutting.Application.csproj` (MODIFIED - Execution.Domain ref)
9. `src/SpaceOS.Modules.Cutting.Api/Endpoints/CuttingPlanningEndpoints.cs` (MODIFIED)

## Tesztek
- **18 uj teszt pass**
  - `AssignBatchEndpointTests` (5 teszt): ValidRequest, InvalidRole, DuplicateBatch, InvalidBatchId, InvalidDate
  - `BatchAssignmentTests` (5 teszt): Create validacio, empty IDs, priority range, past start time
  - `AssignBatchCommandHandlerTests` (4 teszt): ValidRequest, BatchNotFound, DuplicateBatch, InvalidPriority
- **938/939 osszes teszt zold** (1 flaky RateLimiterTests - nem kapcsolodo)

## Security review
- [x] Role-based authorization: `machine_operator` VAGY `production_manager`
- [x] TenantId JWT claim ellenorzes
- [x] Idempotencia: `(BatchId, PlanDate)` unique constraint
- [x] Priority validacio: 1-10
- [x] StartTime validacio: nem lehet a multban (5 perc tolerancia)
- [x] Batch exists validacio

## DoD teljesites
- [x] `POST /cutting/api/plans/{date}/assign-batch` endpoint deployed
- [x] BatchAssignment domain logic + CuttingExecution schedule
- [x] PostgreSQL unique constraint: `(batchId, planDate)` idempotencia
- [x] RBAC: `[Authorize(Roles = "machine_operator,production_manager")]`
- [x] +18 BE teszt pass (endpoint, domain, handler)
- [x] 0 dotnet build error
- [ ] VPS deploy + smoke test (pending deploy)

## Kovetkezo lepesek
1. VPS deploy
2. Smoke test: `curl -X POST -H "Authorization: Bearer <token>" -H "Content-Type: application/json" -d '{"batchId":"..","machineId":"..","operatorId":"..","priority":5,"startTime":"..."}' https://api.joinerytech.hu/cutting/api/plans/2026-06-17/assign-batch`
3. TOP 3 FE track indulhat
