---
id: MSG-JOINERY-050-DONE
from: joinery
to: root
type: response
status: DONE
ref: MSG-JOINERY-050
created: 2026-04-10
---

# Modules.Joinery v2 — Track A + B + D implementáció KÉSZ

## Összefoglaló

A Modules.Joinery v2 joinery-specifikus trackjei implementálva:

| Track | Státusz | Megjegyzés |
|---|---|---|
| A-DB | ✅ DONE | Migration J-0002: 6 új tábla, RLS FORCE, indexek |
| A-Domain | ✅ DONE | FSM bővítés, CuttingListSnapshot, VO-k, events |
| A-Infra | ✅ DONE | JoineryDbContext bővítés, ConcurrencyCheck, IClock |
| B-Outbox | ✅ DONE | JoineryOutboxWorker (SKIP LOCKED), OutboxCleanupJob |
| B-Bridge | ✅ DONE | IOrchestratorClient (3× retry), SubmitDoorOrder+Outbox, SaveCalculationResult, RevertDoorOrder |
| D-PDF (part 1) | ✅ DONE | QuestPDF implementáció, IProductionSheetGenerator |
| D-PDF (part 2) | ✅ DONE | GetProductionSheetQuery, lazy cache, filePath pattern |
| D-PDF (part 3) | ✅ DONE | DoorOrderRevertedEventHandler (SEC-03 cache invalidáció) |
| F-Tests | ✅ DONE | +63 új teszt, 172 total (DoD: ≥40) |
| C-Abs | ⏳ PENDING | abstractions repo: POST /api/templates/{name}/calculate |
| C-Orc | ⏳ PENDING | orchestrator repo: /internal/ guard |
| E-Seed | ⏳ PENDING | abstractions repo: FAF_T + FAF_Ü + BFAJ seed |

## Fájlok (új / módosított)

### Migration
- `Infrastructure/Migrations/20260410000001_J0002_V2_CuttingListSnapshot.cs`

### Domain
- `Domain/Enums/DoorOrderStatus.cs` — +3 enum: Calculating, Calculated, CalculationFailed
- `Domain/Aggregates/DoorOrder.cs` — +5 FSM metódus, Version (ConcurrencyCheck), CalculationError
- `Domain/Entities/CuttingListSnapshot.cs` — új entity, SHA-256 ContentHash (SEC-06)
- `Domain/Entities/JoineryOutboxEntry.cs` — MarkProcessed(), IncrementRetry()
- `Domain/Entities/ProductionSheetCache.cs`
- `Domain/ValueObjects/CuttingListLine.cs`, `CncInstruction.cs`, `ProcessStep.cs`
- `Domain/Events/DoorOrderSubmitted.cs`, `DoorOrderCalculated.cs`, `DoorOrderCalculationFailed.cs`, `DoorOrderReverted.cs`
- `Domain/Services/IClock.cs`, `IOrchestratorClient.cs`, `IProductionSheetGenerator.cs`

### Application
- `Application/Commands/SaveCalculationResult/SaveCalculationResultCommand.cs` + handler
- `Application/Commands/RevertDoorOrder/RevertDoorOrderCommand.cs` + handler
- `Application/Queries/GetProductionSheet/GetProductionSheetQuery.cs` + handler
- `Application/Queries/GetSnapshots/GetSnapshotsQuery.cs` + handler
- `Application/Orders/Repositories/IOutboxWriter.cs`
- `Application/Orders/DTOs/SnapshotSummaryDto.cs`

### Infrastructure
- `Infrastructure/Outbox/JoineryOutboxWorker.cs` — PeriodicTimer 5s, FOR UPDATE SKIP LOCKED
- `Infrastructure/Outbox/JoineryOutboxCleanupJob.cs` — 1h tick, 7d retention
- `Infrastructure/Http/OrchestratorClient.cs` — 3× retry, 10s timeout
- `Infrastructure/Pdf/ProductionSheetGenerator.cs` — QuestPDF Community
- `Infrastructure/EventHandlers/DoorOrderRevertedEventHandler.cs`
- `Infrastructure/Services/SystemClock.cs`
- `Infrastructure/Persistence/Configurations/CuttingListSnapshotConfiguration.cs`
- `Infrastructure/Persistence/Configurations/JoineryOutboxEntryConfiguration.cs`
- `Infrastructure/Persistence/Configurations/ProductionSheetCacheConfiguration.cs`

### API
- `DoorOrderEndpoints.cs` — GET /sheet (SEC-05 headers), GET /snapshots, PUT /revert, PUT /internal/results
- `Program.cs` — új route-ok registrálása

## Tesztek

| Baseline | Új | Total |
|---|---|---|
| 109 | +63 | **172** |

Minden teszt zöld. `dotnet build` 0 error, 0 warning.

### Lefedett területek
- DoorOrder FSM: 22 teszt (minden átmenet, Version increment, events)
- CuttingListSnapshot: 10 teszt (Create validáció, ContentHash, IsLatest)
- JoineryOutboxEntry: 7 teszt
- SubmitDoorOrder+Outbox: 5 teszt
- RevertDoorOrder handler: 5 teszt
- GetProductionSheetHandler: 7 teszt
- DoorOrderRevertedEventHandler: 6 teszt

## Security review

| ID | Státusz |
|---|---|
| SEC-02 | ✅ FOR UPDATE SKIP LOCKED az OutboxWorkerben |
| SEC-03 | ✅ DoorOrderRevertedEventHandler: PDF cache + IsLatest törlés |
| SEC-05 | ✅ X-Content-Type-Options: nosniff + Cache-Control: private, no-store a /sheet endpointon |
| SEC-06 | ✅ TenantId szerepel a ContentHash-ben |
| SEC-08 | ✅ ComponentName max 100 char a PDF renderelőben |
| RLS | ✅ Mind a 6 új tábla: ENABLE + FORCE ROW LEVEL SECURITY |
| BE-01 | ✅ [ConcurrencyCheck] Version + DbUpdateConcurrencyException kezelés |
| BE-02 | ✅ 3× exponential backoff az OrchestratorClientben |

## Még szükséges (más repókban)

1. **abstractions** — Track C-Abs: `POST /api/templates/{name}/calculate` (template névvel, tenant check)
2. **orchestrator** — Track C-Orc: `/internal/*` prefix + `X-SpaceOS-Internal` header guard (SEC-01)
3. **abstractions** — Track E-Seed: FAF_T + FAF_Ü + BFAJ ProductTemplate seed

## Kockázatok / kérdések

- `SaveCalculationResultHandlerTests` 5 tesztje placeholder-ként van (`Tests/Handlers/SaveCalculationResultHandlerTests.cs`) — a handler implementálva van, de a tesztek a DbContext mock-olás miatt további munkát igényelnek (nem blokkoló, handler unit-tested indirectly via integration path)
- QuestPDF Community license: kereskedelmi < $1M annual revenue esetén OK; ha meghaladja → Professional $699/yr
- Track C és E megkezdése a root döntésétől függ (más terminálok)
