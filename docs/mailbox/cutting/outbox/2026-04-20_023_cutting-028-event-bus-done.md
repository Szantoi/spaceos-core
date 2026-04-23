---
id: MSG-CUTTING-028-DONE
from: cutting
to: root
type: done
status: READ
priority: high
date: 2026-04-20
ref: MSG-CUTTING-028
---

# CUTTING-028 DONE — Real Integration Event Bus: CuttingJobCompletedEvent

## Összefoglaló

CUTTING-028 teljes scope implementálva: valódi cross-service event publishing
`CuttingJob.Status → "Cut"` átmeneten keresztül, HTTP via CUTTING (:5005) → INVENTORY (:5004).

---

## Módosított / új fájlok

### CUTTING modul

**Domain:**
- `Domain/Aggregates/CuttingJob.cs` — `MarkAsCut()` metódus hozzáadva (Pending/InProgress → Cut)

**Application:**
- `Application/Events/ICuttingEventPublisher.cs` — új interface
- `Application/Commands/CompleteJob/CompleteJobCommand.cs` — új command
- `Application/Commands/CompleteJob/CompleteJobCommandHandler.cs` — Mark + Save + Publish sorrendben

**Infrastructure:**
- `Infrastructure/Events/CuttingEventPublisher.cs` — HttpClient implementáció (POST + X-Internal-Service header)
- `Infrastructure/Extensions/ServiceCollectionExtensions.cs` — ICuttingEventPublisher typed HttpClient DI regisztráció

**Repository:**
- `Domain/Interfaces/ICuttingRepository.cs` — +`GetCuttingJobTrackedAsync(Guid jobId)`
- `Infrastructure/Repositories/CuttingRepository.cs` — implementáció

**API:**
- `Api/Endpoints/CuttingPlanningEndpoints.cs` — `PUT /api/cutting/planning/jobs/{jobId}/complete` endpoint hozzáadva

**Tests (CUTTING):**
- `Domain/CuttingJobTests.cs` — +2 MarkAsCut teszt
- `Application/CompleteJobCommandHandlerTests.cs` — +7 unit teszt (success + not found + already cut)
- `Api/CompleteJobEndpointTests.cs` — +4 API teszt (200 + 404 + 400 + 401)

### INVENTORY modul

**API:**
- `Api/Endpoints/IntegrationEndpoints.cs` — új fájl: `POST /api/inventory/integration/cutting-job-completed`
  - Auth: `X-Internal-Service` header kötelező (403 ha hiányzik)
  - Dispatches `CuttingJobCompletedEvent` via MediatR → meglévő handler fut

**Application:**
- `Application/Events/CuttingJobCompletedEventHandler.cs` — dimension guard hozzáadva:
  `if (WidthMm <= 0 || HeightMm <= 0 || ThicknessMm <= 0) return;`
  (v1 stub: CUTTING még nem küldi a pontos méretet, offcut létrehozás v1.5-re halasztva)

**Program.cs:**
- `MapInventoryIntegrationEndpoints()` regisztrálva

**Tests (INVENTORY):**
- `Api/IntegrationEndpointsTests.cs` — +4 teszt (202 ok + 403 missing header)

---

## Tesztek

| Projekt | Korábbi | Most |
|---|---|---|
| Cutting.Contracts.Tests | 10 | 10 |
| Cutting.Tests | 171 | 184 |
| Inventory.Tests | 150 | 154 |
| **Összesen** | **331** | **348** |

```
Build (CUTTING): 0 error, 0 warning
Build (INVENTORY): 0 error, 0 warning
Tests (CUTTING): Passed 194/194
Tests (INVENTORY): Passed 154/154
```

---

## Commit hashek

- CUTTING: `b0a11ba` (feat: CUTTING-028 — real event bus CuttingJobCompletedEvent via HTTP)
- INVENTORY: `2fe889e` (feat: CUTTING-028 — integration endpoint + handler guard)

---

## Security review

- `POST /api/inventory/integration/cutting-job-completed` → `AllowAnonymous`, de `X-Internal-Service` header kötelező (403 ha hiányzik) — internal service auth pattern
- CUTTING `CuttingEventPublisher` minden requestben küldi az `X-Internal-Service: cutting` headert
- `ICuttingEventPublisher` az Application rétegben definiált, implementáció Infrastructure-ban — layering betartva
- `PUT /api/cutting/planning/jobs/{jobId}/complete` endpoint `RequireAuthorization("ManufacturerOnly")` group alatt él

## Megjegyzések

**v1 stub korlát:** A `CuttingEventPublisher` dispatch-kor `WidthMm=0, HeightMm=0, ThicknessMm=0`
értékeket küld (CUTTING-028 payload nem tartalmaz méreti adatot). Az INVENTORY handler
gracefully kihagyja az offcut létrehozást, ha a méretek nincsenek meg.

**v1.5 upgrade útja:** Ha a CUTTING Jobs aggregátumán anyagadatok (materialCatalogId, widthMm, heightMm)
elérhetők lesznek, az `ICuttingEventPublisher.PublishJobCompletedAsync` interface bővíthető, és a
INVENTORY endpoint handler valódi offcut-ot hozhat létre.

**INVENTORY stub tesztek:** A `CuttingJobCompletedEventHandlerTests.cs` meglévő unit tesztjei
érintetlenül zölden futnak — a dimension guard nem töri el ezeket (valódi méretadatokkal tesztelnek).
Az INVENTORY terminál mock-alapú stub tesztjeit nem kellett módosítani (azok az egységes handler
logikát tesztelik, nem az HTTP pipeline-t).
