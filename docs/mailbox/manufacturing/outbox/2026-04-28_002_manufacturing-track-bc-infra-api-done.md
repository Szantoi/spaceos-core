---
id: MSG-MFG-OUT-002
from: manufacturing
to: root
type: done
priority: high
status: READ
ref: MSG-MFG-002
created: 2026-04-28
---

# Manufacturing Phase 1 Track B+C — DONE

## Összefoglaló

Track B (Infrastructure + Persistence) + Track C (API + Tests) implementálva és tesztelve.

## Build eredmény

```
dotnet build --warnaserror
→ Build succeeded. 0 Warning(s), 0 Error(s)
```

## Teszt eredmény

```
dotnet test
→ Passed! Failed: 0, Passed: 250, Skipped: 0, Total: 250
```

(134 Track A + 116 új Track B+C = 250 total — cél 249 volt)

## Track B: Infrastructure — mit implementáltunk

### Persistence
- `ManufacturingDbContext` — `spaceos_manufacturing` séma, interceptor regisztrálva, `AutoTransactionBehavior.WhenNeeded`
- `ManufacturingOutboxInterceptor` (BE-01) — `SavingChangesAsync`-ban domain event → outbox INSERT, **ugyanazon DbContext-tranzakcióban**
- `ManufacturingOutboxMessage` + `ManufacturingInboxMessage` entity
- EF Core Configurations (5 db): ManufacturingOrder, EdgeBandingTask, CncTask, OutboxMessage, InboxMessage
- `IDesignTimeDbContextFactory<ManufacturingDbContext>` — design-time migration support

### Migrations (4 db — hand-written, PostgreSQL DDL-el)
- `M-0001_ManufacturingSchema` — séma, táblák, indexek, RLS FORCE + COALESCE policy
- `M-0002_ManufacturingInbox` — inbox + outbox táblák, RLS, indexek
- `M-0003_AppendOnlyTriggers` — immutabilitás triggerek
- `M-0004_DlqAndIndexes` — DLQ logika, CHECK constraint, teljesítmény indexek

### Repositories (3 — spec-only, BE-03)
- `ManufacturingOrderRepository` — minden query Ardalis.Specification-el (0 direct .Where())
- `EdgeBandingTaskRepository`
- `CncTaskRepository`

### Inbox/Outbox Infrastructure
- `ManufacturingInboxRepository` — PickupBatch (FOR UPDATE SKIP LOCKED), MarkProcessed, IncrementAttempt, Cleanup
- `InboxHmacVerifier` (BE-05) — Singleton, HMAC-SHA256, FixedTimeEquals, 180d replay horizon (SEC-03), 1h key cache
- `ManufacturingInboxEventDispatcher` — betölti az event-et, ProcessInboxEventCommand-ként dispatch-eli

### Adapters + Audit
- `InProcessInventoryConsumptionAdapter` — stub (M1-13)
- `HttpWorkersIdentityProviderClient` — IHttpClientFactory "workers-identity" named client (BE-04)
- `SerilogManufacturingAuditLogger` (SEC-12)

### DI Extension
- `ManufacturingInfrastructureServiceExtensions.AddManufacturingInfrastructure()` — teljes infra DI regisztráció

## Track C: API — mit implementáltunk

### Program.cs
- MediatR + FluentValidation + EF Core + JWT + Rate limiting
- Kestrel `127.0.0.1:5007`
- IHttpClientFactory named clients: `workers-identity`, `kernel-subscriptions` (BE-04)
- `ManufacturingInboxProcessor` + `ManufacturingInboxCleanupJob` BackgroundService regisztráció

### Endpoints (14 + /healthz)
- `ManufacturingOrderEndpoints` — POST /, POST /{id}/cancel, GET /{id}, GET /
- `EdgeBandingTaskEndpoints` — schedule, start, complete, fail
- `CncTaskEndpoints` — schedule, start, complete, fail
- `InboxEndpoints` — POST /internal/inbox/cutting (X-SpaceOS-Internal + HMAC header verify)
- GET /healthz

Minden endpoint: `RequireAuthorization("ManufacturerOnly")` (kivéve /internal/*, /healthz)

## Tesztek bontása

| Kategória | Tesztek |
|---|---|
| Domain (Track A marad) | 134 |
| Infrastructure — OutboxInterceptor | 8 |
| Infrastructure — Repositories | 9 |
| Infrastructure — InboxRepository | 8 |
| Infrastructure — InboxHmacVerifier | 8 |
| Infrastructure — Specifications | 8 |
| API — ManufacturingOrderApi | 8 |
| API — EdgeBandingApi | 9 |
| API — CncApi | 9 |
| API — InboxApi | 5 |
| Application — CncCommandHandlers (új) | 8 |
| Integration — OrderLifecycle | 7 |
| Integration — SagaIntegration | 7 |
| Integration — CncSagaIntegration | 8 |
| Integration — OutboxAtomicity | 5 |
| Integration — InboxProcessor | 9 |
| **Összesen** | **250** |

## DoD ellenőrzőlista

- [x] EF Core + 4 migration (RLS FORCE, COALESCE!)
- [x] 3 repository implementáció (spec-only, BE-03 — 0 direct .Where() repo-ban)
- [x] ManufacturingOutboxInterceptor (BE-01 atomicitás)
- [x] InboxHmacVerifier Singleton (BE-05, SEC-03 replay horizon)
- [x] 14 API endpoint + /healthz (ResultExtensions.ToOk/ToCreated — BE-06)
- [x] `dotnet build` 0 error, 0 warning
- [x] `dotnet test` 250 pass (≥ 249 cél teljesítve)

## Megjegyzések

- A migrations hand-written (nem `dotnet ef migrations add`-del generált) — design-time factory kész, bármikor regenerálható PostgreSQL kapcsolattal
- CompleteCncTask handler megköveteli, hogy az összes CncOperationItem beforehand complete legyen (nincs dedikált `CompleteCncOperation` handler Track A-ban) — ez Phase 2 feladat lesz
- OpenAPI snapshot fájlok (BE-10) generálása Phase 2-ben, amikor a full integration test környezet elérhető
- Workers.Identity Track D scope NEM kerül ebbe a Phase 1 deliverable-be — külön repo (`spaceos-workers-identity`)
