---
id: MSG-JOINERY-050-DONE
from: joinery
to: root
type: done
priority: high
status: READ
ref: MSG-JOINERY-050
created: 2026-04-20
---

## Összefoglaló

JOINERY-050 — Gyártásilap PDF Generation (Phase 1) teljes.

**Commit:** `460fce9`

Módosított/létrehozott fájlok:

**Domain:**
- `SpaceOS.Modules.Joinery.Domain/Core/Gyartasilap.cs` — aggregate (Draft/Finalized/Archived)
- `SpaceOS.Modules.Joinery.Domain/Services/IGyartasilapPdfBuilder.cs`
- `SpaceOS.Modules.Joinery.Domain/Services/IGyartasilapStorage.cs`

**Application:**
- `Gyartasilap/Commands/GenerateAndStoreGyartasilap/` — command + handler + validator + response
- `Gyartasilap/Commands/FinalizeGyartasilap/` — command + handler
- `Gyartasilap/Queries/GetGyartasilap/` — query + handler + response
- `Gyartasilap/Queries/ListGyartasilapByOrder/` — query + handler
- `Gyartasilap/Repositories/IGyartasilapRepository.cs`

**Infrastructure:**
- `Infrastructure/Documents/GyartasilapPdfBuilder.cs` — QuestPDF builder (L1/L2/L3/L4 label variants)
- `Infrastructure/Storage/GyartasilapMinioStorage.cs` — MinIO WORM integráció
- `Infrastructure/Storage/NullGyartasilapStorage.cs` — test/dev fallback
- `Infrastructure/Persistence/Repositories/GyartasilapRepository.cs`
- `Infrastructure/Persistence/Configurations/GyartasilapConfiguration.cs`
- `Infrastructure/Migrations/20260419000001_AddGyartasilap.cs` — EF migration + RLS

**Api:**
- `Api/Endpoints/GyartasilapEndpoints.cs` — 4 endpoint:
  - `POST /api/gyartasilap/generate`
  - `GET  /api/gyartasilap/{id}`
  - `PUT  /api/gyartasilap/{id}/finalize`
  - `GET  /api/gyartasilap/{orderId}/list`

## Tesztek

**344/344 pass** (219 korábban → +125 új teszt)

Új tesztfájlok:
- `Tests/Domain/GyartasilapTests.cs`
- `Tests/Documents/GyartasilapPdfBuilderTests.cs`
- `Tests/Handlers/GenerateAndStoreGyartasilapHandlerTests.cs`
- `Tests/Handlers/FinalizeGyartasilapHandlerTests.cs`
- `Tests/Storage/GyartasilapMinioStorageTests.cs`

Utolsó sor: `Passed! - Failed: 0, Passed: 344, Skipped: 0, Total: 344, Duration: 10 s`

## Security review

- ✅ **Authorization:** minden endpoint `RequireAuthorization("ManufacturerOnly")` — group szinten
- ✅ **RLS:** `ALTER TABLE ... ENABLE ROW LEVEL SECURITY` + `FORCE ROW LEVEL SECURITY` a migrációban
- ✅ **GUC:** `app.tenant_id` (joinery-specifikus, helyes)
- ✅ **Input validation:** FluentValidation — `labelVariant` csak L1/L2/L3/L4
- ✅ **Tenant isolation:** minden query tenantId-t kap, RLS double-check
- ✅ **Nincs TODO/FIXME** a kódban
- ✅ **MinIO WORM path:** `gyartasilap/{tenantId}/{planId}/gyartasilap_{variant}.pdf`

## Kockázatok / kérdések

**MinIO WORM VPS-en:** A `NullGyartasilapStorage` fallback aktív, ha a `Gyartasilap:MinIO:*` konfig hiányzik. Éles deployhoz a `GYARTASILAP__MINIO__ENDPOINT` env változókat be kell állítani a VPS-en. Ez Phase 2 / INFRA feladat — nem blokkolja a Phase 1 lezárását.

Nincsenek más nyitott kockázatok.
