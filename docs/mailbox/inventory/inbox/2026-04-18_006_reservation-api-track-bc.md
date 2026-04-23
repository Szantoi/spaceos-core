---
id: MSG-INVENTORY-006
from: root
to: inventory
type: task
priority: high
status: READ
created: 2026-04-18
docs:
  - docs/tasks/new/SpaceOS_Modules_Contracts_Architecture_v4_1_Amendment.md
---

# Reservation API — Track B + Track C (Inventory domain + infrastructure)

## Tervdok

`docs/tasks/new/SpaceOS_Modules_Contracts_Architecture_v4_1_Amendment.md` — Section 3–10, Section 11 (DoD)

## Párhuzamos indítás

**Track A (Contracts NuGet) az ABSTRACTIONS terminálnál fut párhuzamosan.**

- **Day 1–2 domain munka (Track B):** NuGet még nem kell — indítható azonnal
- **Day 2 handlerek (Track B):** `IInventoryProvider` + DTOs kelleni fog a Contracts 1.2.0-ból → várj az ABSTRACTIONS DONE outboxára, majd frissítsd a package referenciát
- **Day 1 migration (Track C):** azonnal indítható (nincs NuGet függőség)

---

## Track B — Domain + Application

### Day 1 — Domain (NuGet nélkül indítható)

**`SpaceOS.Modules.Inventory.Domain/Aggregates/Reservation.cs`** — tervdok Section 4
- 12 invariáns (I-01..I-12)
- `Reserve()`, `Release()`, `MarkExpired()`, `MarkConsumed()`, `PopDomainEvents()`
- `CreatedByUserId` optional (SEC-10)

**`SpaceOS.Modules.Inventory.Domain/Aggregates/ReservationItem.cs`**
- `TenantId` denormalized (DB-02)

**`SpaceOS.Modules.Inventory.Domain/Services/IModuleRegistry.cs`** (SEC-13)
- Hardcoded allowlist v1: `Cutting`, `Joinery`, `Cabinet`, `FreeTier`

**`SpaceOS.Modules.Inventory.Domain/Services/ConsumerContextValidator.cs`** (SEC-07, SEC-09)
- Schema validation + XSS regex + PII regex (email, bearer token pattern)

**Specifications:**
- `ReservationByCorrelationActiveSpec` — **BE-06: CSAK Active state-re** (status = 0)
- `ReservationWithItemsSpec`
- `ExpiredActiveReservationsSpec`

### Day 2 — Application handlers (Contracts 1.2.0 UTÁN)

**`ReserveStockHandler`** — tervdok Section 9
- SEC-13 allowlist check
- SEC-07/09 context validation
- **BE-06:** idempotency csak Active-ra (`ReservationByCorrelationActiveSpec`)
- Stock availability check
- `DbUpdateException 23505` explicit catch + re-fetch (SEC-12)

**`ReleaseReservationHandler`** — no-op terminal state-en

**`GetReservationsHandler`**
- Min 1 filter kötelező (DoS guard)
- Max Take: 500

---

## Track C — Infrastructure + Operations

### Day 1 — Migration 0030 (azonnal indítható)

`SpaceOS.Modules.Inventory.Infrastructure/Migrations/0030_AddReservations.cs`

A teljes DDL a tervdok Section 6.1-ben van, 1:1 implementáld:
- `reservations` tábla + constraints (XSS check, TTL range, status range)
- **BE-06:** `ux_reservations_tenant_correlation_active` partial unique index (`WHERE status = 0`)
- `reservation_items` tábla + trigger (`fn_validate_reservation_item_tenant`)
- RLS policies mindkét táblán
- `v_stock_availability` view **`security_invoker = true`** (DB-01)

### Day 2 — Migration 0031 + Worker

`0031_CreateInventoryWorkerRole.cs` — tervdok Section 6.2 (ADR-024)
- `spaceos_inventory_worker` role: `BYPASSRLS NOCREATEDB NOCREATEROLE NOINHERIT`
- Narrow grants: `SELECT, UPDATE` on reservations · `SELECT` on reservation_items
- `REVOKE ALL` on panel_stocks, material_catalog, stock_movements

**`ReservationCleanupWorker.cs`** — tervdok Section 8
- SKIP LOCKED + batch 100 default (BE-05, max 500)
- `IWorkerHeartbeatStore` heartbeat (BE-09)
- Audit logging per tenant
- `ReservationMetrics.CleanupIterationMs` (BE-08)

### Day 3 — Rate limit + Resilience + Observability

**`InventoryRateLimitConfig.cs`** (SEC-08)
- `ReservePolicy`: 100/min/tenant
- `ReleasePolicy`: 100/min/tenant
- `GetPolicy`: 60/min/tenant

**`PollyHttpPolicies.cs`** (BE-07)
- `HttpClientFactory + AddPolicyHandler()`
- 3 retry exp backoff + circuit breaker (5 failure / 30s open)
- **Polly NuGet package hozzáadása** — jóváhagyott (tervdok Section 16 sign-off)

**`ReservationMetrics.cs`** (BE-08) — tervdok Section 7.1
- OpenTelemetry: created/released/expired/consumed counters + duration histogram + idempotency hit counter

**`WorkerHeartbeatStore.cs`** + **`InventoryHealthCheck.cs` update** (BE-09)
- tervdok Section 10 szerint

**`grafana/inventory-reservations.json`** — tervdok Section 7.2 dashboard spec

---

## EF Core konfiguráció

**`ReservationConfiguration.cs`**
- `xmin` → `IsRowVersion()` (DB-03, Npgsql specifikus)
- `ConsumerContextJson` → JSONB mapping

**`ReservationItemConfiguration.cs`**
- `TenantId` NOT NULL

---

## DoD gate-ek (Section 11 — Inventory)

- [ ] Migration 0030 applied: DDL + RLS + trigger + view + XSS constraint
- [ ] Migration 0031: worker role + narrow grants
- [ ] **BE-06:** partial unique index `ux_reservations_tenant_correlation_active` confirmed
- [ ] View `security_invoker = true` confirmed
- [ ] 12 invariáns test green (I-01..I-12)
- [ ] **BE-06:** terminal state idempotency test (Expired → new Reserve → új ReservationId)
- [ ] `ReserveAsync` 10-way concurrent race-free test
- [ ] `DbUpdateException 23505` explicit catch + re-fetch test
- [ ] Rate limit 429 + Retry-After response
- [ ] Worker heartbeat frissül minden iterációban
- [ ] Meglévő **53 Inventory teszt** zöld + **≥24 új teszt** (total: 77)
- [ ] 0 build warning
- [ ] `ConfigureAwait(false)` minden production async call-ban

## Build + test gate

```bash
dotnet build   → 0 error, 0 warning
dotnet test    → 53 meglévő + ≥24 új = ≥77 zöld
```

---

## INFRA deploy

Deploy-t NE kezdeményezz — ROOT adja ki INFRA-nak a DONE után.

Deploy sorrend (tervdok Section 17.4):
1. NuGet 1.2.0 publish
2. Migration 0030 staging → verify
3. Migration 0031 staging → worker role + grants
4. Worker connection string env var (`INVENTORY_WORKER_PASSWORD`)
5. API deploy staging → E2E smoke
6. Production

---

*Skill: `/spaceos-terminal`*
