# SpaceOS — Modules.Manufacturing Phase 1 v4 — Claude Code Agent Context

> **Companion to:** `SpaceOS_Modules_Manufacturing_Phase1_Architecture_v4.md`
> **Verzió:** v4.0 README — 2026-04-28
> **Cél:** Claude Code agent-context — Implementation runbook, per-track DI registration, layer-CLAUDE.md vázak, prereq mátrix
> **Becsült effort:** 22-26 nap (3 párhuzamos track + Workers.Identity dedikált track)
> **Repos érintve:** `spaceos-modules-manufacturing` (új) · `spaceos-workers-identity` (új) · `spaceos-kernel` (K-NNNN migration cross-repo prereq) · `spaceos-modules-contracts` (v1.5.0 bump) · `spaceos-modules-cutting` (Phase 4 expand-contract)

---

## 1. Quick orientation

**Mit építünk:** A Doorstar production chain post-cutting feldolgozási láncszemét — `EdgeBandingTask` + `CncTask` + `ManufacturingOrder` saga koordinátor + cross-module Inbox a Cutting `PanelCompleted` event-re.

**Architektúra-mantra (M1-1 → M1-27 axiómák):** `Outbox + Inbox + Idempotency + Specification + Result<T> + Domain-rules + ConfigureAwait`.

**Kritikus mantrák minden agent-session elején:**
1. **Olvasd el a layer-specifikus CLAUDE.md-t** (Domain/Application/Infrastructure/Api).
2. **Specification-only repository** — `grep -r ".Where(" Repositories/ --include="*.cs"` → 0 találat.
3. **Per-iteration BackgroundService scope** — soha nem long-living scope, mindig `IServiceScopeFactory.CreateAsyncScope()`.
4. **Outbox-tx atomicity** — aggregate save + outbox INSERT egy DbContext-tranzakcióban (`ManufacturingOutboxInterceptor`).
5. **Validator csak shape** — minden cross-field invariáns Domain factory-ban.
6. **`ConfigureAwait(false)` minden production async call-on.**

---

## 2. Cross-repo prereq mátrix

A Manufacturing Phase 1 implementáció **nem indulhat** a következő prereq-ek nélkül:

| Prereq | Repo | Migration / Commit | Felelős | Becsült |
|--------|------|--------------------|---------|---------|
| **Kernel `module_subscriptions` tábla** | `spaceos-kernel` | K-NNNN (új) | Kernel maintainer | 1.5 nap |
| **Kernel `OutboxDispatcher` cross-module fan-out HMAC + mTLS** | `spaceos-kernel` | K-NNNN+1 (új) | Kernel maintainer | 1 nap |
| **Contracts v1.5.0 — `IManufacturingProvider` + `IWorkerIdentityProvider`** | `spaceos-modules-contracts` | NuGet bump | shared | 0.5 nap |
| **Cutting Phase 4 expand-contract Phase 1 (data copy)** | `spaceos-modules-cutting` | C-0007 (új) | Cutting maintainer | 1 nap |
| **Modules.Abstractions `DeriveCncPlan()` returns `SignedCncPlan`** | `spaceos-modules-abstractions` | code-only patch | Abstractions maintainer | 0.5 nap |

**Total prereq: ~4.5 nap** — **párhuzamosan futtatható** a Manufacturing Track A/B/C scaffolddal, de Day 5-ig lock-elni kell (a saga handler integration test ezektől függ).

---

## 3. Daily sprint breakdown (22-day plan, 4 párhuzamos track)

A Track-ek eloszlása (Phase 4/5 minta):

- **Track A — Domain + Application** (1 agent-session, dedikált)
- **Track B — Infrastructure + Persistence** (1 agent-session, dedikált)
- **Track C — API + Inbox + Tests** (1 agent-session, dedikált)
- **Track D — Workers.Identity** (1 agent-session, párhuzamos, 8 nap fókuszált munka)

| Nap | Track A: Domain + Application | Track B: Infrastructure + Persistence | Track C: API + Inbox + Tests | Track D: Workers.Identity |
|-----|-------------------------------|---------------------------------------|------------------------------|---------------------------|
| 1 | Repo scaffold, 5 csproj, CLAUDE.md per layer + Domain VO-k (10 db) | M-0001 + M-0002 migration scaffold (RLS, idempotens DDL) | xUnit v3 test project setup, `ManufacturingDbContextFixture`, `WorkersIdentityClientFake` | `spaceos-workers-identity` repo scaffold + WI-0001 migration |
| 2 | `ManufacturingOrder` aggregate (Create, Cancel, Complete, Fail) + counter math (DB-02) | EF Core configurations: 4 aggregate + JSONB shape CHECK (DB-10) + OwnsOne | Aggregate FSM unit tests (Order Create + Cancel + Complete) | `WorkerIdentity` aggregate (Enroll, Suspend, Revoke) + audit-trail entitások |
| 3 | `EdgeBandingTask` aggregate (Schedule, Start, RecordProgress, Complete, Fail) | RLS FORCE + WITH CHECK + append-only triggers (M-0003) | EdgeBandingTask FSM tests + counter math invariants | `WorkerEnrollment`/`WorkerRevocation` audit-trail + per-tenant UNIQUE badge_id |
| 4 | `CncTask` aggregate (Schedule + `PlanIntegrityHash` SEC-07) + `CncFailureDetails` VO discriminator | M-0004 DLQ + sequence trigger (SEC-06, SEC-18) + secrets schema role | CncTask FSM + integrity hash matching tests | WI-0002 KEK slots schema (SEC-04) + role separation `*_app` vs `*_kek` |
| 5 | Saga event handlers (`OnPanelEdgeBandingCompleted/Failed` — BE-08 same-context direct call) | `ManufacturingOutboxInterceptor` (BE-01) + `ManufacturingDbContext` interceptor registration | Saga concurrent advance integration tests (4-panel, 24-panel) | `KekRotationBackgroundService` (BE-02 per-iter scope) + 90d rotation logic |
| 6 | 10 Specification (`Ardalis.Specification`, BE-03 + BE-09 `AsSplitQuery`) | `ManufacturingOrderRepository` + 2 task repo (BE-03 specification-only — **zero** `.Where()`) | Saga retry tests (DB-13 3-attempt exponential backoff) | KEK rotation tests + per-tenant key derivation (HKDF-SHA256) tests |
| 7 | 12 command-handler-validator triplet (FluentValidation **shape-only** BE-07) | `ManufacturingInboxRepository` + DLQ repo (SEC-06) | 12 command validator unit tests (csak shape) | `IWorkerIdentityProvider` interface + DTO-k (Contracts v1.5.0) |
| 8 | 6 query-handler (Specification + AsNoTracking + AsSplitQuery saga lookup) | `ManufacturingInboxDlqRepository` + `manufacturing_inbox_dlq` mapping | 6 query handler unit tests + AsSplitQuery EXPLAIN ANALYZE | HR enrollment endpoint + JWT HR role auth (SEC-11) |
| 9 | `IInboxHmacVerifier` (BE-05 Singleton + 1h cache + force-refresh) | `InboxHmacVerifier` infrastructure (KMS key load + lazy cache) | InboxHmacVerifier tests + replay-horizon tests (SEC-03 180d ± 5min skew) | Cutting Phase 4 expand-contract Phase 1 (idempotens data copy WI schema-ba) |
| 10 | `ManufacturingInboxProcessor` (BE-02 per-iter scope, BE-11 bounded channel 1000 + 10 consumer) | `IInboxEventDispatcher` + `CuttingPanelCompletedHandler` | Inbox idempotency + DLQ + backpressure tests (1000-event burst) | Workers.Identity API endpoints + integration tests |
| 11 | `ManufacturingInboxCleanupJob` (BE-02 per-iter scope, SEC-03 180d retention, 30d Processed retention) | `InProcessInventoryConsumptionAdapter` (M1-13) + `InProcessAdapterAssertion` startup-time | Cleanup job tests + retention-window edge cases | Cross-tenant isolation tests (RLS pen-test 5+ scenario) |
| 12 | `IManufacturingAuditLogger` (SEC-12) + 8 audit event mapping | `SerilogManufacturingAuditLogger` + 180d log retention sink | Audit log structure tests + format compliance tests | Workers.Identity load tests (1000 worker enrollment + revoke + KEK rotate) |
| 13 | `ResultExtensions` (BE-06: `ToOk/ToCreated/ToProblem`) + endpoint mapping pattern | Kernel `module_subscriptions` self-registration client (BE-04 named client `kernel-subscriptions`) | API integration tests + ResultExtensions mapping tests | (Buffer + Workers.Identity smoke tests) |
| 14 | API Minimal API endpoints (12 command + 6 query + InboxEndpoints SEC-08 mTLS-only) | `IHttpClientFactory` named clients (BE-04: `workers-identity` + `kernel-subscriptions`) + mTLS handler + SPKI pin | OpenAPI snapshot generation (BE-10) — 2 fájl (public + internal) | (Buffer + WI documentation) |
| 15 | Rate-limit middleware (SEC-15: 10 req/sec/worker, 100 req/sec/tenant, 100 req/sec/inbox) | mTLS + SPKI pin validator (Phase 4 SEC-03 minta) | Rate-limit + mTLS integration tests | (Buffer + per-track CLAUDE.md polishing) |
| 16 | (Buffer + integration polish) | (Buffer + integration polish) | Performance tests: 24-panel saga < 50ms, 1000-event inbox burst < 5s | — |
| 17 | Cutting Phase 4 expand-contract Phase 2 — cutover (`IWorkerIdentityProvider` használat Cutting-ben) | (Buffer) | Cutover smoke tests (Cutting Phase 4 + Manufacturing E2E) | — |
| 18 | Documentation: per-layer CLAUDE.md frissítés + ADR | Lynis scan + vulnerable package check + BackgroundService graceful shutdown teszt (Phase 5 BE-14 minta) | DoD checklist verifikáció (8.1-8.9) | — |
| 19 | (Documentation: ADR-MFG-01 sequence ordered list, ADR-MFG-02 inbox-pattern, ADR-MFG-03 Workers.Identity bounded context) | (Phase 5 minta runbook: KEK rotation + DLQ replay) | Pre-deploy gate verification + final pen-test sweep | — |
| 20 | Companion README post-merge cleanup | Production runbook: inbox poison-pill alert response + KEK rotation manual trigger | Smoke test full pipeline (Cutting → Manufacturing E2E) | — |
| 21 | (Slack — pufferes nap) | (Slack — pufferes nap) | DoD final verify + handoff | — |
| 22 | (Phase 1 v2 design kickoff: Surface + Assembly + DAG + machine-HMAC enforce) | — | — | — |

---

## 4. Per-track CLAUDE.md vázak

Minden Manufacturing layer kap egy CLAUDE.md fájlt a layer gyökerében. Ezek a Claude Code agent contextusban beolvasódnak minden session-elején. A vázak alább.

### 4.1 `SpaceOS.Modules.Manufacturing.Domain/CLAUDE.md`

```markdown
# Manufacturing Domain Layer — CLAUDE.md

## Layer rules (DDD invariants — Golden Rule 1-3)

- **Zero external NuGet dependencies.** Csak `Ardalis.Result`, `Ardalis.Specification` (interface only).
- **No public setters** aggregate-eken. Mutation csak explicit metódus-on át.
- **Static `Create()` / `Schedule()` / `From()` factory** minden aggregate-en — invariáns ellenőrzés itt fut.
- **`AddDomainEvent()`** minden mutation végén. `PopDomainEvents()` az interceptor (Infrastructure) felelőssége.
- **`Result<T>` return** minden domain-method-en. Soha nincs throw business rule sértés esetén.
- **Value Objects** `sealed record` + `static Create()` factory + max-length / range / regex CHECK.
- **`ConfigureAwait(false)` NEM kötelező** Domain-ben (de Application + Infra-ban igen).

## Aggregate-katalógus

| Aggregate | Felelősség | FSM |
|-----------|------------|-----|
| `ManufacturingOrder` | Saga koordinátor, counter columns, sequence advance | Created → InProgress → Completed \| Cancelled \| Failed |
| `EdgeBandingTask` | Per-panel élzárás, line items per edge | Pending → InProgress → Completed \| Failed \| Cancelled |
| `CncTask` | Per-panel CNC operations + `PlanIntegrityHash` (SEC-07) | Pending → InProgress → Completed \| Failed \| Cancelled |

## Specification katalógus (Ardalis.Specification, BE-03)

10 db a `Domain/Specifications/` mappában. **Repository sosem hív direct LINQ `.Where()`-t** (M1-26).

## Tilos

- `DateTime.Now` / `DateTimeOffset.Now` — mindig `IDateTimeProvider`-en át.
- `throw new BusinessException` — `Result.Error(...)` helyett.
- `IRepository.LoadAll()` style — kötelező Specification.
- `Aggregate.Cancel()` után további mutation engedélyezés — M1-12.
- Ha találsz `_navigation` lazy-load property-t, rögtön szólj — anti-pattern.

## Tesztelési minimum (Track A unit tests)

- FSM happy path × 3 aggregate (≥ 30 teszt)
- FSM negative tests (invalid state transition) × 3 aggregate (≥ 20 teszt)
- VO factory tests (Create + Reject invalid input) × 10 VO (≥ 30 teszt)
```

### 4.2 `SpaceOS.Modules.Manufacturing.Application/CLAUDE.md`

```markdown
# Manufacturing Application Layer — CLAUDE.md

## Layer rules

- **CQRS:** `IRequest<Result<T>>` MediatR command/query, dedikált handler.
- **Validator vs Domain split (BE-07):** FluentValidation **csak shape** (length, range, regex). **0 `MustAsync` Manufacturing-ban.** Cross-field invariáns Domain factory-ban.
- **`Result<T>` return** minden handler-en (Golden Rule 6).
- **`ConfigureAwait(false)`** minden production async call-on (Golden Rule 7).
- **`AsNoTracking()`** minden read-only query-n (Golden Rule 8).
- **Saga handler same-context (BE-08)**: `OnPanelEdgeBandingCompleted/Failed` direct method call az event-handler-ből, NEM külön MediatR notification.

## Inbox flow (cross-module)

```
Kernel OutboxDispatcher → mTLS + HMAC → /internal/inbox/cutting
  ↓
InboxEndpoint:
  1. mTLS CN check (SEC-08)
  2. IInboxHmacVerifier.VerifyAsync (SEC-02)
  3. JWT/mTLS source-tenant ↔ payload tenantId egyezés
  4. replay_horizon_at < occurredAt < now() + 5min skew (SEC-03)
  5. INSERT INTO manufacturing_inbox ON CONFLICT (event_id) DO NOTHING
  ↓
ManufacturingInboxProcessor (BackgroundService):
  - bounded Channel<Guid> 1000 (BE-11)
  - 10 concurrent consumers
  - per-event scope (BE-02)
  - attempt_count >= 5 → DLQ (SEC-06)
  ↓
IInboxEventDispatcher:
  - eventType match → CuttingPanelCompletedHandler
  - hydrate ManufacturingOrder via tenant config
  - SaveAsync → ManufacturingOutboxInterceptor publish
```

## Saga flow (within-module)

```
EdgeBandingTaskCompleted (domain event) → outbox INSERT (BE-01 same tx)
  ↓
OnPanelEdgeBandingCompleted_AdvanceOrder (event handler)
  → ManufacturingOrder.OnPanelEdgeBandingCompleted (counter ++)
  → if all done → AdvanceStep → CompletedSteps++
  → if last step → Complete → ManufacturingOrderCompleted event
  → outbox publish → Logistics (later)
```

## Tilos

- `MediatR.Send()` cross-context (handler hívja másik bounded context handler-jét) — outbox + inbox helyett.
- Validator-ban `MustAsync` cross-field check.
- Direct `await Task` pattern `ConfigureAwait(false)` nélkül.
- `IRepository.GetAll()` style Specification nélkül.

## Tesztelési minimum (Track C)

- 12 command validator tests (csak shape) — ≥ 25 teszt
- 6 query handler tests — ≥ 15 teszt
- 4 saga event handler tests (concurrent advance + retry) — ≥ 10 teszt
- Inbox idempotency + replay-horizon + DLQ — ≥ 15 teszt
```

### 4.3 `SpaceOS.Modules.Manufacturing.Infrastructure/CLAUDE.md`

```markdown
# Manufacturing Infrastructure Layer — CLAUDE.md

## Layer rules

- **EF Core 8** + PostgreSQL 16. SQLite csak unit tests-ben (in-memory).
- **`ManufacturingOutboxInterceptor` (BE-01)** kötelezően regisztrálva a `ManufacturingDbContext`-en.
- **`Database.AutoTransactionBehavior = WhenNeeded`** explicit.
- **Repository Specification-only (BE-03, M1-26):** `grep -r ".Where(" Repositories/` → 0 találat.
- **`AsSplitQuery()` saga lookup-on (BE-09)** — soha nem Cartesian product.
- **`IHttpClientFactory` named clients (BE-04):** `workers-identity` + `kernel-subscriptions` mTLS handler + SPKI pin + 5-perc lifetime.
- **`InProcessAdapterAssertion`** startup-time `AssemblyLoadContext` check (M1-13, Phase 4 SEC-10 minta) — `IInventoryConsumptionAdapter` ugyanabban a process-ben fut.

## DB role separation (SEC-14)

| Role | Schema | Permissions | Felhasználó |
|------|--------|-------------|-------------|
| `spaceos_manufacturing_app` | `spaceos_manufacturing` | SELECT, INSERT, UPDATE (NO DELETE — M1-12) | Manufacturing service connection-string |
| `spaceos_workers_identity_app` | `spaceos_workers_identity` (workers, enrollments, revocations) | SELECT, INSERT, UPDATE | Workers.Identity service connection-string |
| `spaceos_workers_identity_kek` | `spaceos_workers_identity` (kek_slots only) | SELECT, INSERT, UPDATE | Workers.Identity KEK rotation BackgroundService impersonation |
| `kernel_subs_writer` | `kernel.module_subscriptions` | EXECUTE upsert procedure only | Manufacturing service self-registration handshake |

## Migration files

- `M-0001_ManufacturingSchema.cs` — 4 aggregate tábla + RLS + JSONB shape CHECK + DB-08 indexes
- `M-0002_ManufacturingInbox.cs` — inbox tábla + DLQ + replay_horizon + attempt_counter (CONCURRENTLY indexes → `suppressTransaction: true`)
- `M-0003_AppendOnlyTriggers.cs` — progress_events append-only + sequence immutability post-Create (SEC-18)
- `M-0004_DlqAndRoles.cs` — DLQ + 4 DB role + privilege grants

## Outbox interceptor pattern (BE-01)

```csharp
// SavingChangesAsync hook:
foreach (aggregate IN ChangeTracker.Entries<TenantScopedAggregate>())
{
    foreach (event IN aggregate.PopDomainEvents())
    {
        // SAME transaction as aggregate save:
        await outbox.WriteAsync(tenantId, aggregateId, eventId, eventType, payload, occurredAt, "Manufacturing", ct);
    }
}
```

## Tilos

- Raw `DbConnection.CreateCommand()` — kikerüli az interceptor pipeline-t (Phase 5 BE-01 mintán tilos).
- `BuildServiceProvider()` DI setup-ban — Golden Rule 9 + lint gate.
- Connection string hard-code — `appsettings.json` + `IOptions<>` pattern.
- `lazy-load` navigation property aggregate-en.

## Tesztelési minimum (Track B integration tests)

- Migration apply + re-apply idempotens (DB-16) — ≥ 6 teszt
- RLS FORCE cross-tenant pen-test — ≥ 8 teszt (4 tábla × 2 scenario)
- Outbox-tx atomicity (BE-01 OUT-1 gate) — ≥ 5 teszt
- AsSplitQuery saga EXPLAIN ANALYZE — ≥ 3 teszt
- Specification spec match (10 spec × 1 happy + 1 negative) — ≥ 20 teszt
- BackgroundService scope soak (24h, BE-02) — 1 teszt (long-running)
```

### 4.4 `SpaceOS.Modules.Manufacturing.Api/CLAUDE.md`

```markdown
# Manufacturing API Layer — CLAUDE.md

## Layer rules

- **Minimal API** (no MVC controllers).
- **JWT bearer auth** + `tenant_id` claim → `current_setting('spaceos.tenant_id', true)` (SEC-19, Phase 4 `TenantSessionInterceptor` reuse).
- **`/internal/*` endpoints mTLS-only** (SEC-08) + SPKI pin validation.
- **`ResultExtensions.ToOk/ToCreated/ToProblem` (BE-06)** minden endpoint mapping-en.
- **OpenAPI snapshot CI gate (BE-10):** `Manufacturing.openapi.snapshot.json` + `Manufacturing.Internal.openapi.snapshot.json` committed; diff-fail = build-fail.
- **Rate-limit middleware (SEC-15):** Redis sliding-window 10 req/sec/worker progress, 100 req/sec/tenant, 100 req/sec/inbox.

## Endpoint kategóriák

| Kategória | Endpointok | Auth |
|-----------|------------|------|
| Public commands | 12 db (Create*Order, Schedule*Task, Start*Task, Record*Progress, Complete*Task, Fail*Task, Cancel*Order) | JWT |
| Public queries | 6 db (`Get*ByOrder`, `ListActiveOrders`, `GetPendingTasksByMachine`, `GetWorkerTaskQueue`) | JWT |
| Internal inbox | `POST /internal/inbox/cutting` | mTLS-only + SPKI pin + HMAC |
| Internal subscription | `POST /internal/kernel/subscribe` | mTLS-only |

## DI registration (SOLID, Phase 5 BE-12 minta)

```csharp
// Application layer
services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(CreateManufacturingOrderCommand).Assembly));
services.AddValidatorsFromAssembly(typeof(CreateManufacturingOrderCommandValidator).Assembly);

// Inbox + Saga
services.AddSingleton<IInboxHmacVerifier, InboxHmacVerifier>();           // BE-05
services.AddHostedService<ManufacturingInboxProcessor>();                  // BE-02
services.AddHostedService<ManufacturingInboxCleanupJob>();                 // BE-02

// Outbox + DbContext
services.AddSingleton<ManufacturingOutboxInterceptor>();                   // BE-01
services.AddDbContext<ManufacturingDbContext>((sp, opt) => {
    opt.UseNpgsql(connStr);
    opt.AddInterceptors(sp.GetRequiredService<ManufacturingOutboxInterceptor>());
});

// IHttpClientFactory named clients (BE-04)
services.AddHttpClient("workers-identity", ...).ConfigurePrimaryHttpMessageHandler(...).SetHandlerLifetime(TimeSpan.FromMinutes(5));
services.AddHttpClient("kernel-subscriptions", ...);

// Adapters
services.AddScoped<IInventoryConsumptionAdapter, InProcessInventoryConsumptionAdapter>();   // M1-13
services.AddScoped<IWorkersIdentityProviderClient, HttpWorkersIdentityProviderClient>();    // BE-04

// Audit
services.AddScoped<IManufacturingAuditLogger, SerilogManufacturingAuditLogger>();           // SEC-12

// Startup-time assertions
services.AddSingleton<InProcessAdapterAssertion>();
services.AddHostedService<StartupAssertionRunner>();    // M1-13 + DB role check + outbox interceptor presence check
```

## Tilos

- Token-only inbox endpoint (csak mTLS — SEC-08).
- Endpoint-en belül DbContext direct usage — handler-be delegálás kötelező.
- `IResult` mapping ad-hoc — kötelezően `ResultExtensions`-on át.
- OpenAPI snapshot ignore (CI gate-et bypass-olni nem szabad).

## Tesztelési minimum (Track C)

- Endpoint integration tests (12 command + 6 query) — ≥ 25 teszt
- mTLS + SPKI pin verify tests — ≥ 5 teszt
- Rate-limit middleware tests — ≥ 5 teszt
- OpenAPI snapshot stability — ≥ 2 teszt
```

### 4.5 `spaceos-workers-identity/CLAUDE.md`

```markdown
# Workers.Identity Service — CLAUDE.md

## Felelősség

Worker identity bounded context — Manufacturing + Cutting **közös** használat. Phase 4-ből kiemelt struktúrák (Phase 4 worker consent + photo crypto-shred apparatus a Cutting-ben marad — M1-7).

## Aggregate

- `WorkerIdentity`: BadgeId, PinHash (argon2id), HmacKeyRef (KMS), Status (Active|Suspended|Revoked)
- `WorkerEnrollment` (audit-trail, append-only)
- `WorkerRevocation` (audit-trail, append-only trigger)

## KEK rotation (két-slot, SEC-04)

```
PRIMARY ─────► used for new HMAC sign + verify
PREVIOUS ────► used for verify only (90d window)

Rotation 90d cron:
  PRIMARY → PREVIOUS
  new PRIMARY generated (KMS)
  audit log: WorkerKekRotated
```

## API endpoints

- `POST /internal/workers/enroll` (mTLS + JWT HR role)
- `POST /internal/workers/{id}/revoke` (mTLS + JWT HR role)
- `GET /internal/workers/verify?badgeId&pin` (mTLS, called by Manufacturing/Cutting service)
- `POST /internal/workers/{id}/sign-event` (mTLS, called by Manufacturing/Cutting for HMAC sign)

## DB schema isolation

`spaceos_workers_identity` schema, RLS FORCE, 2 role: `*_app` (workers + enrollments + revocations CRUD) és `*_kek` (kek_slots CRUD only).

## Cutting Phase 4 expand-contract migráció (SEC-11)

3-fázisú, 14-day window:
1. Day 9 — data copy (idempotens INSERT ON CONFLICT) Cutting → Workers.Identity
2. Day 17 — Cutting cutover: `IWorkerIdentityProvider`-en keresztül olvas
3. Day 21+ — Cutting `C-0007` migration: source rows DROP COLUMN

## Tesztelési minimum

- WorkerIdentity FSM tests — ≥ 8 teszt
- KEK rotation tests (90d cycle, manual trigger, audit log) — ≥ 8 teszt
- Per-tenant key derivation (HKDF-SHA256) tests — ≥ 5 teszt
- HR enrollment + revocation API tests — ≥ 6 teszt
- Cross-tenant isolation pen-tests — ≥ 5 teszt
```

---

## 5. DI registration táblázat (BE-05, BE-12 minta)

A komponensek DI lifetime-jukkel együtt — minden agent ezt használja regisztráláskor. Anti-pattern check a DoD-ban.

| Komponens | Lifetime | Indok |
|-----------|----------|-------|
| `ManufacturingOutboxInterceptor` | Singleton | Stateless, thread-safe, registration once |
| `IInboxHmacVerifier` | **Singleton** | Cache + force-refresh handler thread-safe (BE-05) |
| `ManufacturingDbContext` | Scoped | EF Core default |
| `IManufacturingOrderRepository` | Scoped | DbContext dependency |
| `IEdgeBandingTaskRepository` | Scoped | — |
| `ICncTaskRepository` | Scoped | — |
| `IManufacturingInboxRepository` | Scoped | — |
| `IManufacturingInboxDlqRepository` | Scoped | — |
| `IInventoryConsumptionAdapter` | Scoped | In-process adapter, M1-13 |
| `IWorkersIdentityProviderClient` | Scoped | HttpClient via `IHttpClientFactory` |
| `IManufacturingAuditLogger` | Scoped | Serilog enrich + tenant context |
| `ManufacturingInboxProcessor` | Singleton (BackgroundService) | `IServiceScopeFactory.CreateAsyncScope()` per iteration (BE-02) |
| `ManufacturingInboxCleanupJob` | Singleton (BackgroundService) | Same pattern (BE-02) |
| `KekRotationBackgroundService` (Workers.Identity) | Singleton (BackgroundService) | Same pattern (BE-02) |
| `WorkerLastActiveUpdateService` (Workers.Identity) | Singleton (BackgroundService) | Same pattern (BE-02) |
| `IDateTimeProvider` | Singleton | Stateless |
| `IClientCertificateProvider` | Singleton | Cert load once + 60-day rotation |
| `ISpkiPinValidator` | Singleton | Pin db load once |
| `InProcessAdapterAssertion` | Singleton | Startup-time assertion |
| `IConfigureNamedOptions<KernelOutboxKeyOptions>` | Singleton | Options pattern (no `BuildServiceProvider()`) |

---

## 6. Phase 4 függőség-mátrix (mit reuse-ol Manufacturing Phase 1)

| Phase 4 / Phase 5 komponens | Manufacturing Phase 1 reuse | Hogyan |
|------------------------------|------------------------------|--------|
| Kernel `outbox_messages` tábla | ✅ teljes reuse | `ManufacturingOutboxInterceptor` ugyanazt írja (BE-01) |
| Kernel `OutboxDispatcher` BackgroundService | ✅ teljes reuse | Cross-module fan-out + HMAC + mTLS |
| Phase 4 `TenantSessionInterceptor` | ✅ teljes reuse | DbContext-be plug-in (SEC-19) |
| Phase 4 `IWorkerSecurityPolicy` interface | ✅ kiterjesztve | SEC-05 extended HMAC binding scope (`taskId + taskKind`) |
| Phase 4 `WorkerEventHmac` VO | ✅ Workers.Identity-ba kiemelve | SEC-04 + SEC-05 |
| Phase 4 `CompletionProof` VO | ✅ teljes reuse | M1-11 Level 0 mandatory |
| Phase 4 `IHttpClientFactory` minta + SPKI pin | ✅ teljes reuse | BE-04 named clients |
| Phase 4 `OutboxInterceptor` minta | ✅ Manufacturing-specifikus újraimplementálás | BE-01 |
| Phase 4 `ConsentWithdrawalProcessor` per-batch scope minta | ✅ minta reuse | BE-02 minden BackgroundService |
| Phase 4 `HandshakeRateLimitMiddleware` | ✅ teljes reuse | SEC-15 |
| Phase 4 `SerilogCuttingAuditLogger` minta | ✅ Manufacturing-specifikus újraimplementálás | SEC-12 (`SerilogManufacturingAuditLogger`) |
| Phase 4 `InProcessAdapterAssertion` | ✅ teljes reuse | M1-13 startup-time check |
| Phase 4 két-slot KEK + KekRewrap minta | ✅ Workers.Identity-ban újra-alkalmazva | SEC-04 (de **HMAC** kulcsra, nem encryption) |
| Phase 5 `ResultExtensions` | ✅ teljes reuse | BE-06 |
| Phase 5 `OpenAPI snapshot CI gate` | ✅ teljes reuse | BE-10 |
| Phase 5 `IConfigureNamedOptions` minta (no `BuildServiceProvider()`) | ✅ teljes reuse | BE-05 |
| Phase 5 BackgroundService graceful shutdown | ✅ teljes reuse | DoD §8.7 SCOPE-* |

---

## 7. Lokális dev környezet setup

### 7.1 Prereq

| Tool | Verzió | Telepítés |
|------|--------|-----------|
| .NET SDK | 8.0.x | `apt install dotnet-sdk-8.0` |
| PostgreSQL | 16+ | `apt install postgresql-16` |
| Redis | 7+ | `apt install redis` |
| `dotnet-ef` | 8.0.x | `dotnet tool install -g dotnet-ef --version 8.0.*` |

### 7.2 Local DB setup

```bash
# Manufacturing schema + role
sudo -u postgres psql <<EOF
CREATE DATABASE spaceos_dev;
\c spaceos_dev
CREATE SCHEMA spaceos_manufacturing;
CREATE ROLE spaceos_manufacturing_app NOLOGIN;
GRANT USAGE ON SCHEMA spaceos_manufacturing TO spaceos_manufacturing_app;
EOF

# Workers.Identity schema + role
sudo -u postgres psql -d spaceos_dev <<EOF
CREATE SCHEMA spaceos_workers_identity;
CREATE ROLE spaceos_workers_identity_app NOLOGIN;
CREATE ROLE spaceos_workers_identity_kek NOLOGIN;
GRANT USAGE ON SCHEMA spaceos_workers_identity TO spaceos_workers_identity_app, spaceos_workers_identity_kek;
EOF
```

### 7.3 Migration runner pattern

```bash
# Manufacturing
cd ~/spaceos-modules-manufacturing
dotnet ef database update \
  --project src/SpaceOS.Modules.Manufacturing.Infrastructure \
  --startup-project src/SpaceOS.Modules.Manufacturing.Api \
  --context ManufacturingDbContext

# Workers.Identity
cd ~/spaceos-workers-identity
dotnet ef database update \
  --project src/SpaceOS.WorkersIdentity.Infrastructure \
  --startup-project src/SpaceOS.WorkersIdentity.Api \
  --context WorkersIdentityDbContext
```

### 7.4 Local mTLS fake

A lokális dev-en a Kernel `OutboxDispatcher` mTLS hívásait egy `FakeKernelOutboxClient` szimulálja (test fixture). Production-on a real mTLS handler aktív.

### 7.5 Smoke test E2E

```bash
# 1. Cutting service boot
cd ~/spaceos-modules-cutting && dotnet run --project src/SpaceOS.Modules.Cutting.Execution.Api &

# 2. Workers.Identity boot
cd ~/spaceos-workers-identity && dotnet run --project src/SpaceOS.WorkersIdentity.Api &

# 3. Manufacturing boot
cd ~/spaceos-modules-manufacturing && dotnet run --project src/SpaceOS.Modules.Manufacturing.Api &

# 4. E2E test
cd ~/spaceos-e2e && dotnet test --filter "Category=ManufacturingPhase1"
```

---

## 8. Pre-deploy gate verification

A deploy előtti utolsó gate-script (Phase 5 SEC-04 `PreDeployValidator` minta):

```bash
#!/bin/bash
# pre-deploy-manufacturing.sh

set -e

echo "=== Pre-deploy gate verification ==="

# Build + test
dotnet build --warnaserror
dotnet test --filter "Category!=E2E"

# Vulnerable packages
VULN=$(dotnet list package --vulnerable 2>&1 | grep -E "(High|Critical)" || true)
[ -z "$VULN" ] || { echo "FAIL: vulnerable packages found"; exit 1; }

# BuildServiceProvider grep
BSP=$(grep -r "BuildServiceProvider" --include="*.cs" src/ || true)
[ -z "$BSP" ] || { echo "FAIL: BuildServiceProvider() found"; exit 1; }

# .Where() in repositories grep
WHERE=$(grep -r ".Where(" --include="*.cs" src/SpaceOS.Modules.Manufacturing.Infrastructure/Persistence/Repositories/ || true)
[ -z "$WHERE" ] || { echo "FAIL: direct .Where() in repository"; exit 1; }

# MustAsync in validators grep
MUSTASYNC=$(grep -r "MustAsync" --include="*.cs" src/SpaceOS.Modules.Manufacturing.Application/Validators/ || true)
[ -z "$MUSTASYNC" ] || { echo "FAIL: MustAsync validator (BE-07)"; exit 1; }

# Raw DbConnection grep
RAW=$(grep -r "GetDbConnection().CreateCommand" --include="*.cs" src/ || true)
[ -z "$RAW" ] || { echo "FAIL: raw DbConnection found (Phase 5 BE-01)"; exit 1; }

# OpenAPI snapshot drift
dotnet run --project tools/OpenApiSnapshot diff
[ $? -eq 0 ] || { echo "FAIL: OpenAPI snapshot drift"; exit 1; }

# Migration apply (idempotens, dry-run)
dotnet ef migrations script --context ManufacturingDbContext > /tmp/m.sql
psql -d spaceos_staging -f /tmp/m.sql --dry-run

# RLS pen-test
dotnet test --filter "Category=RlsPenTest"

# Cross-schema FK = 0
FK_COUNT=$(psql -d spaceos_staging -tA -c "
  SELECT COUNT(*) FROM information_schema.referential_constraints
  WHERE constraint_schema <> 'spaceos_manufacturing' AND constraint_schema <> 'public'")
[ "$FK_COUNT" = "0" ] || { echo "FAIL: cross-schema FK detected (M1-16)"; exit 1; }

echo "=== ALL GATES PASS ==="
```

---

## 9. Production runbooks (Phase 5 minta)

### 9.1 KEK rotation manual trigger

```bash
# When: HR incident / suspected key compromise
# Frequency: 90d auto, manual on-demand

# 1. Trigger via admin endpoint (mTLS + admin JWT)
curl --cert admin.pem --key admin-key.pem \
     -X POST https://workers-identity:5008/admin/kek/rotate \
     -H "Authorization: Bearer $ADMIN_JWT" \
     -d '{"tenantId": "...", "reason": "HR incident"}'

# 2. Verify audit log
journalctl -u workers-identity | grep WorkerKekRotated

# 3. Check kek_slots
psql -d spaceos_prod -c "SELECT tenant_id, slot, rotated_at FROM spaceos_workers_identity.kek_slots WHERE tenant_id = '...'"

# Expected: PRIMARY < 1 perc rotated_at, PREVIOUS = old PRIMARY
```

### 9.2 Inbox poison-pill alert response

```bash
# Trigger: Prometheus alert "manufacturing_inbox_failed_count > 0 for 1h"

# 1. Inspect DLQ
psql -d spaceos_prod -c "
SELECT event_id, source_module, event_type, failure_reason, attempt_count
FROM spaceos_manufacturing.manufacturing_inbox_dlq
WHERE moved_at > now() - interval '1h'"

# 2. Inspect a single failure detail
psql -d spaceos_prod -c "SELECT payload FROM spaceos_manufacturing.manufacturing_inbox_dlq WHERE event_id = '...'"

# 3. Replay (admin only, after fix)
curl --cert admin.pem --key admin-key.pem \
     -X POST https://manufacturing:5006/admin/inbox/dlq/replay \
     -H "Authorization: Bearer $ADMIN_JWT" \
     -d '{"eventId": "..."}'

# 4. If poison-pill due to upstream bug, quarantine tenant
curl ... -X POST /admin/tenants/.../disable-manufacturing
```

### 9.3 Saga drift detection

```bash
# Trigger: weekly cron — check for orders stuck > 7d

psql -d spaceos_prod -c "
SELECT id, tenant_id, status, created_at,
       edge_banding_completed_panels, edge_banding_failed_panels,
       cnc_completed_panels, cnc_failed_panels, total_panels
FROM spaceos_manufacturing.manufacturing_orders
WHERE status = 'InProgress'
  AND created_at < now() - interval '7 days'"

# Investigate per order: missing event? failed task? worker not assigned?
```

### 9.4 Cutting Phase 4 expand-contract cutover (1×, Day 17)

```bash
# Pre-cutover verification (Day 16):
# 1. Workers.Identity has all rows from Cutting Phase 4
psql -c "SELECT COUNT(*) FROM spaceos_cutting.workers" # should match
psql -c "SELECT COUNT(*) FROM spaceos_workers_identity.workers" # should match

# 2. Backfill verification — random row equality check
diff <(psql -c "...cutting...") <(psql -c "...workers_identity...")

# Cutover (Day 17, off-hours):
# 1. Stop Cutting service
systemctl stop spaceos-cutting

# 2. Deploy Cutting Phase 4 patch (uses IWorkerIdentityProvider)
cd ~/spaceos-modules-cutting && git pull origin worker-identity-cutover
dotnet build && systemctl start spaceos-cutting

# 3. Smoke test (10 min observability)
journalctl -u spaceos-cutting -f | grep "WorkerVerify"

# Day 21+: drop source columns
psql -d spaceos_prod -c "ALTER TABLE spaceos_cutting.workers DROP COLUMN pin_hash, hmac_key_ref"
```

---

## 10. ADR-Manufacturing-01 (companion ADR-vázlatok)

**ADR-MFG-01: Process step sequence as ordered list (not DAG)**
- Context: Phase 1 — process orchestration típus döntés
- Decision: ordered `IReadOnlyList<ProcessStepKind>` tenant config-ban; nem DAG
- Consequences: simpler FSM, Phase 2 vezeti be a DAG-ot
- Status: Accepted (v1 pre-decision #1)

**ADR-MFG-02: Inbox-pattern + Kernel Outbox cross-module trigger**
- Context: Cutting → Manufacturing event delivery
- Decision: Kernel `outbox_messages` reuse + per-module `manufacturing_inbox` table; idempotency via `event_id` PK; HMAC + mTLS
- Consequences: at-least-once + dedup; loose coupling; partial-publish anomaly elkerülve outbox-tx-vel
- Status: Accepted (v1 pre-decision #3, BE-01 enforce)

**ADR-MFG-03: Workers.Identity bounded context kiemelés Cutting Phase 4-ből**
- Context: Worker identity duplikáció Cutting + Manufacturing között
- Decision: shared `Workers.Identity` bounded context (BadgeId + HmacKey + enrollment) + scope-specific consent (Cutting photo crypto-shred) marad helyben (M1-7)
- Consequences: Cutting Phase 4 expand-contract migráció (3-fázisú, 14d window); minimum-scope identity, extensibility hooks (TPM/FIDO2 Phase 5+)
- Status: Accepted (v1 pre-decision #2)

**ADR-MFG-04: Counter-based saga consistency (not projection / not advisory lock)**
- Context: 4-24-1000 panel order saga koordináció
- Decision: counter columns on Order aggregate + EF Core `Version` IsConcurrencyToken + 3-attempt retry
- Consequences: O(1) read, race-safe, DB CHECK constraint cross-column invariáns; 1000+ panel skálázódás Phase 2-re halasztva
- Status: Accepted (v2 DB-02)

**ADR-MFG-05: CNC plan integrity hash + in-process Modules.Abstractions delivery**
- Context: CNC tampering kockázat (X/Y/diameter MITM módosítás)
- Decision: `CncPlanIntegrityHash` SHA-256 VO + `SignedCncPlan` Modules.Abstractions oldalán aláírva + in-process call (no network)
- Consequences: tampering detect at scheduling time; FixedTimeEquals constant-time compare
- Status: Accepted (v3 SEC-07)

**ADR-MFG-06: mTLS-only cross-module endpoints + SPKI pin**
- Context: Token-only auth bypass kockázat
- Decision: `/internal/inbox/cutting` + Workers.Identity API + Kernel self-register csak mTLS + SPKI pin; token-only path eltávolítva
- Consequences: cert rotation runbook (60d, Phase 4 minta); SPKI pin cache + force-refresh
- Status: Accepted (v3 SEC-08)

---

## 11. Definition of Ready (Claude Code agent kickoff)

A Manufacturing Phase 1 implementáció **csak akkor indítható**, ha minden alábbi PASS:

- [ ] `SpaceOS_Modules_Manufacturing_Phase1_Architecture_v4.md` review-on átment, project knowledge-be feltöltve
- [ ] `SpaceOS_Modules_Manufacturing_Phase1_Architecture_v4_README.md` (ez a dokumentum) review-on átment
- [ ] **Cross-repo prereq mátrix (§2) committed**: K-NNNN, K-NNNN+1, Contracts v1.5.0, C-0007 prep, Modules.Abstractions patch
- [ ] Codebase Status `Codebase_Status_YYYYMMDD.md` frissítve a tervezett Manufacturing Phase 1 sprint kezdettel
- [ ] 4 párhuzamos Claude Code session-track allokálva (vagy 1 sequential session 22-day plan-nal)
- [ ] VPS `/opt/spaceos/modules-manufacturing/` + `/opt/spaceos/workers-identity/` directory létrehozva
- [ ] Tmux dispatcher session `spaceos-manufacturing` és `spaceos-workers-identity` hozzáadva (Phase 4.1 Amendment minta)
- [ ] PostgreSQL 16 dev + staging DB-n schema + role létrehozva (§7.2)
- [ ] CI workflow `.github/workflows/manufacturing-ci.yml` setup-elve (build + test + OpenAPI snapshot diff)

---

## 12. Definition of Done (sprint exit criteria)

A Manufacturing Phase 1 sprint **akkor zárul**, ha minden alábbi PASS:

- [ ] **8.1-8.9 DoD checklist** (architecture v4 doc) minden gate ✅
- [ ] **Pre-deploy gate script (§8) PASS**
- [ ] **Smoke test E2E (§7.5) PASS**
- [ ] **Cutting Phase 4 expand-contract Phase 1 + Phase 2 DEPLOYED**, Phase 3 (column drop) ütemezve Day 21-re
- [ ] **Doorstar staging tenant** Manufacturing capability flag enabled, 1× E2E happy-path order PASS (Cutting → Manufacturing → ManufacturingOrderCompleted event)
- [ ] **Codebase Status `Codebase_Status_YYYYMMDD.md` frissítve**: új teszt count, új migration count, új security gate count
- [ ] **Production runbooks (§9) verified** (KEK rotation, DLQ replay, saga drift detection, cutover)
- [ ] **Companion handoff document** generálva: Manufacturing Phase 2 kickoff prereq (Surface + Assembly + DAG + machine-HMAC enforce)

---

## 13. Risk register (Claude Code session execution)

| Risk | Impact | Mitigation |
|------|--------|------------|
| 4 párhuzamos session race condition (Track A → B file lock conflict) | Közepes | Tmux dispatcher mailbox-protokoll FE precedens (Phase 4.1 Amendment); commit-szintű szinkronizáció git branch-eken |
| Cutting Phase 4 expand-contract data-loss | Magas | Idempotens INSERT ON CONFLICT + 14d window + rollback runbook (SEC-11) |
| Kernel `module_subscriptions` CI drift (K-NNNN nincs Manufacturing impl előtt) | Magas | Cross-repo prereq lock (§2); Manufacturing Day 1 verify Kernel migration applied |
| Workers.Identity KEK rotation race a Cutting Phase 4 worker progress event-tel | Közepes | Két-slot KEK + PREVIOUS slot 90d window (SEC-04); HMAC verify try-PRIMARY-then-PREVIOUS pattern |
| Inbox poison-pill production incident before alerting set up | Magas | Day 12 — `IManufacturingAuditLogger` + Prometheus exporter aktív; manual DLQ check runbook (§9.2) |
| OpenAPI snapshot CI gate developer pushback (frequent diff failures) | Alacsony | Snapshot drift accept procedure dokumentálva (Phase 5 BE-A11 minta); PR template `--accept-snapshot` checkbox |
| 24-panel saga performance below 50ms target | Alacsony | BE-09 AsSplitQuery + Specification index; performance test Day 16; ha fail → SkipQuery → multi-query fallback |

---

*SpaceOS — Modules.Manufacturing Phase 1 v4.0 README · 2026-04-28*
*Companion to: `SpaceOS_Modules_Manufacturing_Phase1_Architecture_v4.md`*
*Claude Code agent context — Implementation runbook + per-track CLAUDE.md vázak*
*Státusz: READY FOR CLAUDE CODE — minden prereq, runbook, és per-layer instrukció dokumentálva*
