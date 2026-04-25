---
id: MSG-CUTTING-043-DONE
from: cutting
to: root
type: done
priority: high
status: READ
ref: MSG-CUTTING-043
created: 2026-04-25
---

## Összefoglaló

CUTTING-043: Order Ingestion endpoint + CuttingJob enrichment implementálva.

**Új Domain fájlok:**
- `Domain/Enums/GrainDirection.cs` — `None`, `Vertical`, `Horizontal`

**Módosított Domain fájlok:**
- `Domain/Aggregates/CuttingJob.cs` — `Material` (string) + `GrainDirection` (enum) property + `Create()` bővítve
- `Domain/Interfaces/ICuttingRepository.cs` — `GetOpenSlotsOrderedByDateAsync()` + `HasJobsForOrderAsync()` hozzáadva

**Új Application fájlok:**
- `Commands/IngestOrder/IngestOrderCommand.cs` — command record + `IngestOrderItem` DTO
- `Commands/IngestOrder/IngestOrderCommandHandler.cs` — CuttingJob létrehozás, legkorábbi Open DaySlot keresés kapacitásmodellel, OrderId idempotencia

**Módosított Infrastructure fájlok:**
- `Configurations/CuttingJobConfiguration.cs` — `Material` (maxLength 100) + `GrainDirection` (default None) oszlopok
- `Repositories/CuttingRepository.cs` — `GetOpenSlotsOrderedByDateAsync()` + `HasJobsForOrderAsync()` implementáció

**Módosított API fájlok:**
- `Endpoints/InternalEndpoints.cs` — `POST /internal/ingest-order` endpoint, `X-SpaceOS-Internal: true` header ellenőrzés

**Migration:** `AddCuttingJobMaterialGrainDirection` — `Material` + `GrainDirection` oszlopok a `CuttingJobs` táblán

## Tesztek

**293/293 pass** (284 → +9 új):

IngestOrderCommandHandlerTests.cs (8 teszt):
1. `Handle_HappyPath_3Items_Creates3Jobs` ✅
2. `Handle_JobDimensions_AreFromItems` ✅
3. `Handle_MaterialAndGrainDirection_Persisted` ✅
4. `Handle_AssignsToEarliestOpenSlot` ✅
5. `Handle_DuplicateOrderId_ReturnsZero` ✅ (idempotens)
6. `Handle_EmptyItems_ReturnsInvalid` ✅
7. `Handle_NoOpenSlots_ReturnsError` ✅
8. `Handle_TenantIsolation_JobsCreatedWithCorrectOrderId` ✅

InternalEndpointsTests.cs (+1 teszt):
- `IngestOrder_MissingInternalHeader_Returns403` ✅

Contracts: 10/10 pass.

## Security review

- `POST /internal/ingest-order` endpoint `X-SpaceOS-Internal: true` header szükséges → 403 nélküle ✅
- Endpoint `AllowAnonymous()` de internal header gate-tel — Orchestrator hívja, nem publikus ✅
- OrderId idempotencia: duplikált hívás nem hoz létre duplikált job-okat ✅
- GrainDirection enum parsing: ismeretlen érték → `None` (nem dob hibát) ✅
- Material string sanitization: nincs HTML/JS tartalomfeldolgozás ✅

## Kockázatok / kérdések

Nincs.
