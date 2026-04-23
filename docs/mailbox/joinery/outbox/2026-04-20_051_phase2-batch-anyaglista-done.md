---
id: MSG-JOINERY-051-DONE
from: joinery
to: root
type: done
priority: high
status: READ
ref: MSG-JOINERY-051
created: 2026-04-20
---

## Összefoglaló

JOINERY-051 — Phase 2 Batch PDF + Anyaglista teljes.

**Commit:** `7025c1f`

**Módosított/létrehozott fájlok:**

**Domain:**
- `Domain/Core/GyartasilapBatch.cs` — aggregate (Pending/Generating/Ready/Failed FSM), backing field fix (IReadOnlyList<Guid> → private List<Guid>)
- `Domain/Core/Anyaglista.cs` — aggregate
- `Domain/Services/IAnyaglistaPdfBuilder.cs` + `AnyaglistaData`, `AnyaglistaRow` record-ok
- `Domain/Services/IGyartasilapStorage.cs` — bővítve: StoreZipAsync, GetPresignedUrlAsync, StoreAnyaglistaPdfAsync

**Application:**
- `Gyartasilap/Commands/GenerateBatch/` — command + handler (System.IO.Compression ZIP)
- `Gyartasilap/Queries/GetBatchStatus/` — query + handler
- `Gyartasilap/Repositories/IGyartasilapBatchRepository.cs`
- `Anyaglista/Commands/GenerateAnyaglista/` — command + handler
- `Anyaglista/Queries/GetAnyaglista/` — query + handler
- `Anyaglista/Repositories/IAnyaglistaRepository.cs`

**Infrastructure:**
- `Infrastructure/Documents/AnyaglistaPdfBuilder.cs` — QuestPDF táblázatos nézet
- `Infrastructure/Storage/GyartasilapMinioStorage.cs` — ZIP + presigned URL + anyaglista tárolás
- `Infrastructure/Storage/NullGyartasilapStorage.cs` — interface teljes implementáció
- `Infrastructure/Persistence/Repositories/GyartasilapBatchRepository.cs`
- `Infrastructure/Persistence/Repositories/AnyaglistaRepository.cs`
- `Infrastructure/Persistence/Configurations/GyartasilapBatchConfiguration.cs`
- `Infrastructure/Persistence/Configurations/AnyaglistaConfiguration.cs`
- `Infrastructure/Persistence/JoineryDbContext.cs` — GyartasilapBatches + Anyaglistak DbSet
- `Infrastructure/Migrations/20260420000001_AddGyartasilapBatch.cs` + Designer
- `Infrastructure/Migrations/20260420000002_AddAnyaglista.cs` + Designer
- `Infrastructure/DependencyInjection.cs` — új DI regisztrációk

**Api:**
- `Api/Endpoints/GyartasilapEndpoints.cs` — +3 batch route (POST /batch, GET /batch/{id}/status, GET /batch/{id}/download)
- `Api/Endpoints/AnyaglistaEndpoints.cs` — új fájl (POST /generate, GET /{orderId})
- `Api/Endpoints/Requests.cs` — GenerateBatchRequest, GenerateAnyaglistaRequest
- `Api/Program.cs` — MapAnyaglistaEndpoints()

## Tesztek

**384/384 pass** (344 korábban → +40 új teszt)

Új tesztfájlok:
- `Tests/Domain/GyartasilapBatchTests.cs` — FSM tesztek (10 teszt)
- `Tests/Domain/AnyaglistaTests.cs` (5 teszt)
- `Tests/Handlers/GenerateBatchCommandHandlerTests.cs` — happy path, partial failure, storage error (8 teszt)
- `Tests/Handlers/GetBatchStatusQueryHandlerTests.cs` — státusz lekérdezés, not found (5 teszt)
- `Tests/Handlers/GenerateAnyaglistaCommandHandlerTests.cs` — happy path, not found, storage error (6 teszt)
- `Tests/Documents/AnyaglistaPdfBuilderTests.cs` — tartalom ellenőrzés (5 teszt)

Utolsó sor: `Passed! - Failed: 0, Passed: 384, Skipped: 0, Total: 384, Duration: 11 s`

## Security review

- ✅ **Authorization:** minden endpoint `RequireAuthorization("ManufacturerOnly")` — group szinten
- ✅ **RLS:** mindkét új tábla (GyartasilapBatches, Anyaglistak) ENABLE + FORCE RLS, `app.tenant_id` GUC
- ✅ **Tenant isolation:** TenantId minden command/query-ben, RLS double-check
- ✅ **Input validation:** üres GyartasilapIds lista → Invalid result (nem panic)
- ✅ **Nincs TODO/FIXME** a kódban
- ✅ **EF Core fix:** IReadOnlyList<Guid> backing field regressziót okozott a meglévő tesztekben — javítva (private List<Guid> _gyartasilapIds, Ignore a public property-re)

## Kockázatok / kérdések

**MinIO presigned URL:** A `GetPresignedUrlAsync` NullStorage implementációja visszaad egy dummy path-t (`null://...`). Éles deployhoz a MinIO konfig és a presigned URL TTL (`Gyartasilap:MinIO:PresignedUrlExpiryMinutes`) beállítandó — INFRA feladat.

**Anyaglista anyagaggregáció:** Jelenleg egyszerűsített: DoorItems-ből MaterialType = DoorTypeCode, Quantity = db. Phase 3-ban finomítható anyagkalkulátorral ha a domain igényli.

Nincsenek blokkoló kockázatok.
