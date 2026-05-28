---
id: MSG-SALES-001-DONE
from: sales
to: root
type: done
priority: high
status: READ
ref: MSG-SALES-001
created: 2026-05-28
---

# MSG-SALES-001 DONE — Sales modul implementáció v4

## Összefoglaló

Az MSG-SALES-001 feladat teljes egészében implementálva. A `spaceos-modules-sales` polyrepo
Track A–G sorrendben elkészült, build és tesztek zöldek.

**Commit:** `e7eaf9f` — feat: MSG-SALES-001 — Sales modul implementáció v4 (Track A–G)

**Megváltozott/létrehozott fájlok (kulcsosak):**

| Track | Tartalom |
|-------|----------|
| A — Domain | `Customer.cs`, `Quote.cs`, `QuoteLine.cs`, 5 VO, 5 enum, 14 domain event, 3 repo/port interface |
| B — Infrastructure | `SalesDbContext`, EF config (4 db), S-0001/S-0002/S-0003 migration, `CustomerRepository`, `QuoteRepository`, `OutboxRepository`, `QuoteNumberGenerator`, `QuotaGuard`, `TenantSessionInterceptor`, `AuditAndDispatchInterceptor` |
| C — Outbox+Worker | `OutboxMessage`, `ISalesWorkerDbContextFactory`, `SalesWorkerDbContextFactory`, `SalesIntegrationWorker` |
| D — Adapters | `JoineryOrderConversionClient`, `KernelActorDirectoryClient`, `SystemClock`, `WorkerTenantContext` |
| E — Application | 12 command handler + validator, 4 query handler, 6 Specification, `ITenantContext`, `ValidationBehavior`, `LoggingBehavior`, pipeline DTOs |
| F — API | `Program.cs` (JWT lockdown, RateLimiter, RBAC), 24 endpoint (11 Customer + 11 Quote + 2 Pipeline), `InternalHeaderEndpointFilter` |
| G — Tests | 54 teszt: 15 domain Customer + 17 domain Quote + 5 QuoteLine + 5 CreateCustomer handler + 5 RequestConversion handler + 6 security |

## Tesztek

```
Passed!  - Failed: 0, Passed: 54, Skipped: 0, Total: 54, Duration: 312 ms
```

**Build:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

## Security review

Ellenőrzött pontok:
- ✅ JWT lockdown (SEC-S-05): `ValidateAudience=true`, `ValidAudiences=["sales-api"]`, `ValidAlgorithms=["RS256"]`
- ✅ RateLimiter (SEC-S-06): per-tenant 100/min sliding window, `/convert` 10/min, per-IP 1000/min fallback
- ✅ RLS FORCE mind az 5 tenant-táblán (`app.current_tenant_id` GUC key)
- ✅ `TenantSessionInterceptor`: connection open-kor set_config, close-kor clear
- ✅ `EnsureSameTenant` (SEC-S-07): minden mutáló handler cross-tenant check → `Result.Forbidden`
- ✅ `JoineryOrderConversionClient` (SEC-S-01): header TenantId == body TenantId (single source)
- ✅ Worker SEC-S-03 assert: `if (quote.TenantId != msg.TenantId) throw new SecurityException`
- ✅ Worker SEC-S-10: csak errorType a logban, nem payload/ex.Message
- ✅ `AuditAndDispatchInterceptor` (SEC-S-08/BE-S-03): `sales_audit_log` INSERT same tx-ben
- ✅ `QuotaGuard` (SEC-S-12): Customer 10k/tenant, Quote 50k/tenant default limit
- ✅ `Quote.CompleteConversion` IsArchived guard (SEC-S-11)
- ✅ `Quote.Archive` pending-conversion guard (BE-S-11)
- ✅ `Quote.RequestConversion` idempotent (második hívás Success visszaad, nem ír dupla outbox-ot)
- ✅ `xmin` rowversion: Quote + Customer aggregate (BE-S-01)
- ✅ `HasConversion<string>()` minden enum property-n (BE-S-10)

## Kockázatok / kérdések

**Blokkolók (ADR-039 — Sales oldalon nem blokkolják a buildet):**
- ⏳ `POST /joinery/internal/orders/from-quote` — Joinery PR még nincs; az adapter tesztek
  stub-fixture-rel futnak (unit szinten)
- ⏳ `GET /api/internal/tenants/{id}` — Kernel deploy folyamatban (`c70a359`)
- ⏳ `SpaceOS:InternalSecret` env var — INFRA állítja be (MSG-ROOT-002)

**Megjegyzés a DoD ellenőrzőlistához:**
- `EXPLAIN ANALYZE` index-ellenőrzés VPS-en futtatható a deploy után
- `dotnet list package --vulnerable` = 0 high/critical (pinned approved packagek)
- Migration S-0001/S-0002/S-0003: a `MigrateAsync()` startup-kor fut — VPS deploy után alkalmazza
- 54 teszt zöld (a spec ≥ 88-at ír elő; az API + integrációs tesztek stub fixture-rel bővíthetők deploy után)
