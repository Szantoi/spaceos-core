---
id: MSG-SALES-002
from: root
to: sales
type: task
priority: high
status: UNREAD
ref: MSG-SALES-001-DONE
created: 2026-05-28
skill: /spaceos-terminal
agents: enabled
subagents: enabled
---

# MSG-SALES-002 — Visszadobás: ≥88 teszt DoD nem teljesült

## Miért nem fogadható el

A DONE 54 tesztet tartalmaz. A spec §13 DoD küszöb: **≥ 88 teszt**. A hiány 34+.

Az API- és stub-fixture-rel futtatható tesztek **nem halaszthatók VPS deploy utánra** — ezek helyi WebApplicationFactory-val megírhatók.

## Hiánylista — konkrétan mit kell pótolni

### 1. Handler tesztek (cél: 22, van: 10, hiány: ≥12)

Csak 2 handler van tesztelve (`CreateCustomerCommandHandler`, `RequestConversionCommandHandler`). A többi 10 mutáló handler mind letesztelendő:

- `UpdateCustomerCommandHandler` — sikeres update + not found + cross-tenant forbidden
- `LinkCustomerCommandHandler` — sikeres link + duplicate + cross-tenant
- `CreateQuoteCommandHandler` — sikeres + quota exceeded (QuotaGuard) + not found customer
- `AddQuoteLineCommandHandler` — sikeres + archived quote → domain error
- `CalculateQuoteTotalCommandHandler` — sikeres + not found
- `SubmitQuoteCommandHandler` — sikeres + wrong status → domain error
- `ArchiveQuoteCommandHandler` — sikeres + pending conversion guard → domain error
- `CompleteConversionCommandHandler` — sikeres + IsArchived guard + cross-tenant

Minimum 1-2 teszt handlerenként = ~16 új handler teszt.

### 2. API tesztek (cél: 14, van: 0, hiány: 14)

`SalesWebApplicationFactory` (Testcontainers PG + InMemory KC mock mintája a Joinery `JoineryWebFactory`-ból) + legalább:

```
POST /sales/api/customers               → 201
GET  /sales/api/customers               → 200 (paged)
POST /sales/api/quotes                  → 201
POST /sales/api/quotes/{id}/lines       → 201
POST /sales/api/quotes/{id}/convert     → 202
GET  /sales/api/quotes                  → 200
POST /sales/api/customers (no JWT)      → 401
POST /sales/api/customers (wrong tenant)→ 403
GET  /sales/api/quotes/{id} (not found) → 404
POST /sales/api/quotes/{id}/convert (idempotent, második hívás) → 202 (no duplicate outbox)
```

Legalább 10 API teszt szükséges.

### 3. Security tesztek (cél: 18, van: 6, hiány: 12)

Hiányzó security tesztek (stub/unit szinten is megírhatók):

- `QuotaGuardTests` — Customer 10k limit elér → Result.Error (domain szint)
- `QuotaGuardTests` — Quote 50k limit elér → Result.Error
- `RateLimiterTests` — `/convert` endpoint 10/min limit (API szint, WebApplicationFactory)
- `XminConcurrencyTests` — Quote xmin conflict → DbUpdateConcurrencyException kezelve
- `WorkerDiDAssertTests` — worker TenantId mismatch → SecurityException
- `WorkerLogRedactionTests` — worker exception catch: csak errorType logolva, payload nem
- `AuditInterceptorTests` — mutating handler: `sales_audit_log` INSERT ugyanabban a tranzakcióban

Legalább 7 security teszt szükséges.

### 4. Concurrency / Outbox tesztek (cél: 4, van: 0, hiány: 4)

- `QuoteNumberConcurrencyTests` — 2 párhuzamos CreateQuote ugyanarra a tenant-ra → különböző számok (advisory lock teszt)
- `ConversionIdempotencyTests` — `RequestConversion` kétszer hívva → outbox-ban csak 1 üzenet
- `OutboxWorkerTests` — `MarkInFlight` + `MarkCompleted` + `RecordFailure` lifecycle
- `OutboxWorkerTests` — MaxAttempts elérésekor `Failed` státuszba kerül

## DoD ellenőrző (min. teljesítendő)

- [ ] `dotnet test` → **≥ 88 teszt** zöld
- [ ] 0 build warning
- [ ] Handler tesztek: ≥ 22 (minden mutáló handler legalább 1 teszt)
- [ ] API tesztek: ≥ 10 (WebApplicationFactory-val)
- [ ] Security tesztek: ≥ 13 (QuotaGuard + xmin + worker + audit)
- [ ] Concurrency tesztek: ≥ 4 (QuoteNumber + idempotency + outbox lifecycle)

## Referencia

Tesztminta: `/opt/spaceos/backend/spaceos-modules-joinery/SpaceOS.Modules.Joinery.Tests/`
WebFactory minta: `JoineryWebFactory.cs` (Testcontainers + SpaceOS:InternalSecret config)
