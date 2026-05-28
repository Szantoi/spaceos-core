---
id: MSG-JOINERY-055-DONE
from: joinery
to: root
type: done
priority: high
status: READ
ref: MSG-JOINERY-055
created: 2026-05-28
---

## Összefoglaló

ADR-039 Quote→Order Conversion Receiver implementálva. Commit: `da7199f`

**Érintett fájlok (24 db, 1490 sor):**

**Domain (Track A)**
- `DoorOrderStatus.cs` — `ConfirmedFromSales` enum érték hozzáadva
- `DoorOrderConvertedLine.cs` — új owned entity, `internal static Create()` factory
- `DoorOrderCreatedFromConversion.cs` — új domain event
- `DoorOrder.cs` — 9 új conversion field (private set), `_convertedLines` backing list, `CreateFromConversion()` factory
- `ConversionLineData.cs` — bridge record Domain→Application boundary

**Application (Track B)**
- `CreateOrderFromConversionCommand.cs` + `CreateOrderFromConversionResult`
- `CreateOrderFromConversionCommandHandler.cs` — idempotency-first (lookup → conflict check → create → 23505 catch + fallback lookup)
- `CreateOrderFromConversionValidator.cs` — FluentValidation, minden mező validálva
- `IDoorOrderRepository.cs` — `FindBySourceQuoteIdAsync` hozzáadva

**Infrastructure (Track C)**
- `DoorOrderRepository.cs` — `FindBySourceQuoteIdAsync` impl (`AsNoTracking()`)
- `DoorOrderConfiguration.cs` — conversion columns, partial unique index, `OwnsMany(ConvertedLines)`
- `JoineryDbContext.cs` — `DoorOrderConvertedLines` DbSet
- `InternalOrderConversionMiddleware.cs` — `CryptographicOperations.FixedTimeEquals` + `IPAddress.IsLoopback`
- `20260528000001_J003_SalesIntegrationReceiver.cs` — migration (ALTER DoorOrders + CREATE DoorOrderConvertedLines + RLS + FORCE RLS + policy + GRANT + CHECK constraint)

**API (Track D)**
- `OrderConversionRequestDto.cs` — 1:1 Sales IOrderConversionPort map
- `InternalOrdersEndpoints.cs` — `POST /joinery/internal/orders/from-quote`, `ExcludeFromDescription()`
- `Program.cs` — `UseWhen("/joinery/internal/...")` + `MapInternalOrdersEndpoints()`

**Tests (Track E)**
- `DoorOrderConversionFactoryTests.cs` — 8 teszt
- `DoorOrderConvertedLineTests.cs` — 6 teszt
- `CreateOrderFromConversionHandlerTests.cs` — 11 teszt (+ concurrent collision)
- `InternalOrderConversionApiTests.cs` — 6 teszt
- `JoineryWebFactory.cs` — `SpaceOS:InternalSecret` default config hozzáadva

## Tesztek

**389 meglévő → 420 total (31 új teszt)**
```
Passed!  - Failed: 0, Passed: 420, Skipped: 0, Total: 420, Duration: 15s
```
Build: 0 error, 0 warning.

## Security review

| Finding | Állapot |
|---------|---------|
| SEC-01 (timing attack) | ✅ `CryptographicOperations.FixedTimeEquals` byte-szintű összehasonlítás |
| SEC-02 (RLS ConvertedLines) | ✅ Migration J-003: `ENABLE ROW LEVEL SECURITY` + `FORCE ROW LEVEL SECURITY` + `tenant_isolation_converted_lines` policy |
| SEC-03 (concurrent collision) | ✅ `DbUpdateException → PostgresException SqlState "23505"` catch + fallback lookup |
| SEC-04 (ContentHash DB limit) | ✅ `varchar(256)` + EF `HasMaxLength(256)` |
| SEC-05 (response PII) | ✅ 409 body csak `"ContentHash mismatch — idempotency conflict."` — hash értékek csak a logban |
| SEC-07 (loopback assert) | ✅ `IPAddress.IsLoopback(remoteIp)` — middleware első kapuként |
| SEC-08 (Status CHECK) | ✅ Migration: `CK_DoorOrders_Status` CHECK constraint minden valid státusszal |
| SEC-S-01 (header=body TenantId) | ✅ Strict-equal assert az endpointban, 400 ha eltér |
| Input validation | ✅ FluentValidation + domain factory dupla validáció |
| Authorization | ✅ `/joinery/internal/*` route: JWT auth NEM szükséges (loopback + shared secret védi) |
| OpenAPI | ✅ `ExcludeFromDescription()` — nem jelenik meg public Swagger-ben |

## Migration VPS-en alkalmazandó

A Migration J-003 `dotnet ef database update` via `app.Database.MigrateAsync()` automatikusan fut startup-kor. VPS-en a következő deploy után fut le.

**Migration tartalom:**
- `ALTER TABLE "DoorOrders"`: SourceQuoteId, SourceContentHash, ConfirmedFromSalesAt + pénzügyi mezők
- `CREATE UNIQUE INDEX "UX_DoorOrders_TenantId_SourceQuoteId"` (partial WHERE IS NOT NULL)
- `ALTER TABLE "DoorOrders" ADD CONSTRAINT "CK_DoorOrders_Status"` — ConfirmedFromSales benne van
- `CREATE TABLE "DoorOrderConvertedLines"` — minden CHECK constraint
- `ENABLE ROW LEVEL SECURITY` + `FORCE ROW LEVEL SECURITY`
- `tenant_isolation_converted_lines` policy
- `GRANT SELECT, INSERT, UPDATE, DELETE ON DoorOrderConvertedLines TO spaceos_joinery_app`

## Kockázatok / kérdések

**`SpaceOS:InternalSecret` konfiguráció szükséges VPS-en.**
A middleware startup-kor dobja az `InvalidOperationException`-t ha nincs konfigurálva. A VPS-en be kell állítani az environment variable vagy appsettings-en keresztül, a Sales modul által ismert értékkel azonosan.

Egyéb kockázat nincs.
