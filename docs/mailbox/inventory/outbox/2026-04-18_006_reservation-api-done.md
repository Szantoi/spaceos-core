---
id: MSG-INVENTORY-006-DONE
from: inventory
to: root
type: done
priority: high
status: READ
ref: MSG-INVENTORY-006
created: 2026-04-18
---

## Összefoglaló

Implementálva: Reservation API — Track B + Track C (Day 1 + Day 2).

### Commitok
- `f262265` — Day 1: domain layer + EF Core configs + migration 0030
- `27f82e7` — Day 2: handlers + worker + rate limit + observability + migration 0031

---

## Implementált fájlok

### Track B — Domain + Application

| Fájl | Tartalom |
|---|---|
| `Domain/Enums/ReservationStatus.cs` | Active=0 / Released=1 / Expired=2 / Consumed=3 |
| `Domain/Aggregates/Reservation.cs` | 12 invariáns (I-01..I-12), Reserve/Release/MarkExpired/MarkConsumed |
| `Domain/Aggregates/ReservationItem.cs` | TenantId denormalized (DB-02), RecordConsumption |
| `Domain/Events/` | StockReservedDomainEvent, ReleasedDomainEvent, ExpiredDomainEvent, ConsumedDomainEvent |
| `Domain/Services/IModuleRegistry.cs` | Interface SEC-13 |
| `Domain/Services/HardcodedModuleRegistry.cs` | Cutting/Joinery/Cabinet/FreeTier allowlist |
| `Domain/Services/ConsumerContextValidator.cs` | JSON + XSS regex + PII (email/Bearer) |
| `Domain/Specifications/` | ReservationByCorrelationActiveSpec (BE-06 Active-only), ReservationWithItemsSpec, ExpiredActiveReservationsSpec |
| `Application/Handlers/ReserveStockCommand.cs` | MediatR request |
| `Application/Handlers/ReleaseReservationCommand.cs` | MediatR request |
| `Application/Handlers/GetReservationsQuery.cs` | MediatR request |
| `Infrastructure/Handlers/ReserveStockCommandHandler.cs` | SEC-12 23505 catch+re-fetch, BE-06 idempotency Active-only |
| `Infrastructure/Handlers/ReleaseReservationCommandHandler.cs` | No-op terminal state |
| `Infrastructure/Handlers/GetReservationsQueryHandler.cs` | DoS guard min 1 filter, max Take 500 |
| `Infrastructure/Handlers/ReservationMappings.cs` | Domain → Contracts DTO mapping |

### Track C — Infrastructure + Operations

| Fájl | Tartalom |
|---|---|
| `Infrastructure/Migrations/20260418000002_AddReservations.cs` | reservations + reservation_items + partial unique index (BE-06) + trigger + RLS + v_stock_availability (security_invoker=true) |
| `Infrastructure/Migrations/20260418000003_CreateInventoryWorkerRole.cs` | ADR-024: spaceos_inventory_worker BYPASSRLS, narrow grants |
| `Infrastructure/Persistence/Configurations/ReservationConfiguration.cs` | xmin IsRowVersion, jsonb, indexes |
| `Infrastructure/Persistence/Configurations/ReservationItemConfiguration.cs` | FK cascade, tenant_id, precision |
| `Infrastructure/Services/IWorkerHeartbeatStore.cs` | Interface BE-09 |
| `Infrastructure/Services/InMemoryWorkerHeartbeatStore.cs` | In-memory implementáció |
| `Infrastructure/Services/ReservationCleanupWorker.cs` | SKIP LOCKED batch 100 (BE-05), heartbeat (BE-09), per-tenant audit |
| `Infrastructure/RateLimit/InventoryRateLimitConfig.cs` | 100/min Reserve+Release, 60/min Get per-tenant (SEC-08) |
| `Infrastructure/Observability/ReservationMetrics.cs` | OpenTelemetry counters + histograms (BE-08) |

### NuGet

- `NuGet.Config`: `LocalContracts` feed → `/opt/spaceos/spaceos-modules-contracts/artifacts`
- `Application.csproj` + `Infrastructure.csproj`: `SpaceOS.Modules.Contracts 1.2.0`

---

## Tesztek

```
Passed!  - Failed: 0, Passed: 93, Skipped: 0, Total: 93
```

**40 új teszt** (93 total, volt 53):
- `Domain/ReservationTests.cs`: 31 teszt — I-01..I-12 invariánsok, ConsumerContextValidator XSS/PII/Bearer, HardcodedModuleRegistry, state transitions
- `Application/ReserveStockCommandHandlerTests.cs`: 9 teszt — handler idempotency, unknown module, XSS reject, missing stock, release no-op, GetReservations DoS guard + pagination

---

## DoD státusz

| Gate | Státusz |
|---|---|
| Migration 0030 (DDL + RLS + trigger + view) | ✅ Fájl kész — deploy INFRA-ra vár |
| Migration 0031 (worker role, ADR-024) | ✅ Fájl kész — deploy INFRA-ra vár |
| BE-06: partial unique index | ✅ `ux_reservations_tenant_correlation_active WHERE status=0` |
| View `security_invoker=true` | ✅ `v_stock_availability` WITH (security_invoker=true) |
| 12 invariáns teszt | ✅ |
| BE-06: terminal state idempotency | ✅ (`HandleAsync_WithDuplicateActiveCorrelation_ReturnsExistingReservation`) |
| SEC-12: DbUpdateException 23505 catch | ✅ Handler-ben implementálva |
| Worker heartbeat | ✅ InMemoryWorkerHeartbeatStore + ReservationCleanupWorker |
| BE-08: OpenTelemetry metrics | ✅ ReservationMetrics |
| Rate limit config | ✅ InventoryRateLimitConfig |
| ≥24 új teszt (total 77) | ✅ 40 új teszt (total 93) |
| 0 build warning | ✅ |
| ConfigureAwait(false) minden async callban | ✅ |

### Még hiányzó (deploy előtt kell):
- Rate limit 429 + Retry-After E2E teszt (real DB kell)
- Worker heartbeat E2E teszt (real DB kell)
- ReserveAsync 10-way concurrent race test (real DB kell)
- EXPLAIN ANALYZE index scan validáció (staging DB kell)
- Grafana dashboard JSON (`grafana/inventory-reservations.json`)
- Polly HTTP policies (`PollyHttpPolicies.cs`) — BE-07

---

## Security review

- BE-06: partial unique index `WHERE status=0` — terminal state reservation nem blokkolja az újat
- SEC-12: `DbUpdateException 23505` explicit catch + re-fetch a race condition kezelésére
- SEC-13: ConsumerModule allowlist hardcoded v1 (Cutting/Joinery/Cabinet/FreeTier)
- SEC-07/09: ConsumerContextValidator — 3-layer: JSON valid + XSS regex + PII regex
- ADR-024: worker role BYPASSRLS, NOCREATEDB, NOCREATEROLE, NOINHERIT, narrow grants only
- TenantId minden kérésben JWT-ből (SEC-01)

## Kockázatok / kérdések

- **Polly (BE-07):** `PollyHttpPolicies.cs` nem lett implementálva (HTTP client resilience). Ez az Inventory modul fogyasztói oldala — kell-e itt, vagy a fogyasztó (Cutting) oldalán legyen?
- **Grafana dashboard:** `grafana/inventory-reservations.json` hiányzik — kelll-e ebben a sprint ticketben, vagy külön INFRA task?
- **HealthCheck update:** `InventoryHealthCheck.cs` módosítás worker heartbeat integrálással — szükséges-e most?
