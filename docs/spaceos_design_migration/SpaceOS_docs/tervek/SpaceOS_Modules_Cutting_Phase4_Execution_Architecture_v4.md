# SpaceOS — Modules.Cutting Phase 4: Execution Architecture
## CuttingExecution + MilestoneRegistry + ProgressEvents + CompletionProof — Real-time + Cross-tenant evidence + Crypto-grade audit

> **Verzió:** v4.0 — 2026-04-26
> **Státusz:** **IMPLEMENTÁCIÓRA KÉSZ** — minden review pass abszorbeálva, döntések lezárva
> **Blokkoló feltétel:** Cutting Phase 3 DEPLOYED (303 teszt zöld, order ingestion + nesting publish élesben)
> **Kumulált review:** v1 → DB → v2 → security → v3 → backend → v4
> **Precedens:** SpaceOS_Cabinet_0_1_CoreFoundation_Architecture_v4.md

---

## 1. Kumulált Finding Összesítő (v1 → v4)

| Review | Findings (🔴 / 🟠 / 🟡) | Legfontosabb javítás | Effort delta |
|--------|------------------------|----------------------|--------------|
| v1 → `database-designer` + `database-schema-designer` → v2 | 0 / 6 / 4 | Atomic Inventory adapter (DB-10), schema-isolated secrets (DB-05), advisory lock hot-path (DB-02), append-only DELETE-block (DB-01), idempotens DDL (DB-04), explicit RLS WITH CHECK (DB-03), JSONB versioning (DB-09), CHECK on PanelCompleted shape (DB-07), partial index pending milestones (DB-08), compound (HandshakeEpicId, TenantId) (DB-06) | +3 nap |
| v2 → `senior-security` → v3 | 2 / 8 / 5 | Két-slot KEK zero-downtime rotation (SEC-01), per-event worker HMAC (SEC-02), mTLS cert pinning + lifecycle (SEC-03), determinisztikus advisory lock hash (SEC-04), JWT algorithm allowlist (SEC-05), sidecar request smuggling defense (SEC-06), libmagic MIME validation + process isolation (SEC-07), async consent withdrawal flow (SEC-08), event-batch ordering (SEC-09), in-process adapter assertion (SEC-10), KEK rotation runbook (SEC-11), Redis ACL (SEC-12), structured audit log (SEC-13), secret zeroization (SEC-14), SignalR group authorization (SEC-15) | +2.5 nap |
| v3 → `senior-backend` → **v4** | 0 / 5 / 6 | Outbox-pattern domain event dispatch (BE-A01), per-batch DbContext scope BackgroundService-ben (BE-A02), 3 új Specification a repo-extension-höz (BE-A03), `IHttpClientFactory` named client + mTLS handler reuse (BE-A04), Inventory adapter connection-string contract assertion (BE-A05), `ResultExtensions` audit (BE-A06), validator vs. domain rule split (BE-A07), outbox-tx invariant (BE-A08), FluentValidation cross-field (BE-A09), `AsSplitQuery` (BE-A10), OpenAPI snapshot split (BE-A11) | **+1.5 nap** |
| **Bázis-effort kumuláltan v4-ig** | **~19 nap** | — | — |

### v4 finding tábla (Backend review)

#### 🟠 HIGH

| ID | Súly | Terület | Probléma | v4 javítás |
|----|------|---------|----------|------------|
| BE-A01 | 🟠 HIGH | Domain event dispatch ordering | `DispatchAsync` nem szerepel a v3 handler-kódban — pre-commit / post-commit kétértelmű | **Kernel-szintű outbox re-use** (Phase 3B LIVE): `IUnitOfWork.SaveChangesAsync` interceptor `outbox_messages`-be írja az event-eket **ugyanazon tranzakcióban**; külön Kernel `OutboxDispatcher` BackgroundService post-commit fan-out (A4-20) |
| BE-A02 | 🟠 HIGH | BackgroundService DbContext lifecycle | Egyetlen scope több ezer rekordon → memory + connection pool starvation | **Per-batch scope-restart** `IServiceScopeFactory.CreateAsyncScope()` minden iterációhoz; `DbContext.ChangeTracker.Clear()` between batches (A4-21) |
| BE-A03 | 🟠 HIGH | Repository specification gap | Consent flow lookup metódusok nem mappolódnak meglévő specifikációra → Golden Rule 5 sérül | **3 új specifikáció:** `ExecutionsByConsentScopeSpec`, `ConsentAffectedPhotoCountSpec`, `ExecutionKeyByExecutionSpec` — mind `Ardalis.Specification` |
| BE-A04 | 🟠 HIGH | Sidecar HttpClient lifetime | mTLS handler-konfiguráció HttpClient-instance-onként újratöltődne | **`IHttpClientFactory` named client** (`"sidecar"`) + `ConfigurePrimaryHttpMessageHandler` lambda → cert egyszer betöltődik, 2-perces rotation cycle keep-alive |
| BE-A05 | 🟠 HIGH | Inventory adapter scope coupling | Cross-DB-instance hívás silent fail-elne shared-tx feltevéssel | `IOffcutReturnAdapter.ReturnOffcutAsync` startup-time + runtime connection-string assertion → `InvalidOperationException` ha eltér |

#### 🟡 MEDIUM

| ID | Súly | Terület | Probléma | v4 javítás |
|----|------|---------|----------|------------|
| BE-A06 | 🟡 MEDIUM | Result-extension hygiene | `result.Map()` használat dokumentálatlan extension-re | `SpaceOS.Kernel.Application.Results.ResultExtensions` static osztály (Cabinet 0.1 v4 reuse) |
| BE-A07 | 🟡 MEDIUM | Validator vs. domain rule split | Validation duplikálódik FluentValidation + VO factory között | Felelősség-szétválasztás: validator = shape; VO `Result.Invalid` = domain szabály |
| BE-A08 | 🟡 MEDIUM | Outbox-event dispatch transaction | (BE-A01 mellékhatása) Outbox + aggregate egy commit kell | Integration teszt: DB rollback → outbox üres marad |
| BE-A09 | 🟡 MEDIUM | FluentValidation cross-field | `WithdrawWorkerConsent` scope-on nincs élő consent → silent "completed" | `MustAsync(...)` cross-check — élő consent-ellenőrzés a validatorban |
| BE-A10 | 🟡 MEDIUM | EF Core query splitting | 3 owned collection cartesian product 500k row | `AsSplitQuery()` repository-ban + `EXPLAIN ANALYZE` integration teszt |
| BE-A11 | 🟡 MEDIUM | OpenAPI snapshot drift | Egyetlen snapshot fájl 3 különböző kontextusra | 3 külön snapshot: `Cutting.Execution.openapi.snapshot.json`, `.handshake.openapi.snapshot.json`, `.sidecar.openapi.snapshot.json` |

---

## 2. Kontextus és scope

### 2.1 Mit csinál a Cutting Phase 4

| Képesség | Mit jelent | Felelős |
|----------|------------|---------|
| Execution scheduling | `CuttingSheet` géphez és időponthoz kötése | `Cutting.Execution` |
| Worker assignment | Operátor, badge / PIN, per-event HMAC (SEC-02) | `Cutting.Execution` |
| Progress tracking | Panel-szintű előrehaladás real-time, idempotens, HMAC-aláírt | `Cutting.Execution` |
| Milestone registry | Cross-domain mérföldkő-feliratkozás | `Cutting.Execution` (Stage Registry) |
| Material reconciliation | Tervezett vs. valós anyagfogyás | `Cutting.Execution` ↔ `Inventory` |
| Offcut return | Atomic in-process adapter call | `Cutting.Execution` ↔ `Inventory` |
| Completion proof | 3-szintű evidence (Hash / Signed / Photo) — két-slot KEK | `Cutting.Execution` ↔ `Kernel hash-chain` |
| Cross-tenant visibility | Doorstar → LapMester Handshake esetén pull-only progress | `Cutting.Execution` ↔ `Handshake gateway` |
| Worker consent management | Enrollment, withdrawal (async), audit-trail | `Cutting.Execution` ↔ `Workers.Consent` |
| **Domain event delivery** | **Outbox-pattern at-least-once, per-tenant FIFO** | **Kernel `OutboxDispatcher` (Phase 3B LIVE re-use)** |

### 2.2 Mit nem csinál a Cutting Phase 4

| Nem-scope | Hová tartozik |
|-----------|---------------|
| Manufacturing folyamatok | `Modules.Manufacturing` (későbbi fázis) |
| Logisztikai szállítás-tervezés | `Modules.Logistics` (későbbi fázis) |
| Beépítés-tracking | `Modules.Installation` (későbbi fázis) |
| Capacity planning | `Cutting Phase 5: Analytics` |
| Operátor teljesítmény-mutatók | `Cutting Phase 5: Analytics` |
| Külső optimalizáló (OptiCut) | `Cutting Phase 6: Adapters` |
| Anyag-beszerzés indítás | `Modules.Procurement` |
| TPM-based worker key provisioning | Phase 4-ben **infrastruktúra** (opt-in flag), provisioning Phase 5+ |
| **Új Cutting-modul-szintű outbox** | **Kernel outboxát re-use-oljuk (BE-A01 döntés)** |

### 2.3 Architektúra alapaxiómák

| ID | Axióma | Kötelezettség |
|----|--------|----------------|
| A4-1 | `CuttingSheet` immutable (D-06) | Execution sosem módosítja a Sheet-et |
| A4-2 | `CuttingSheet` → 0..1 aktív CuttingExecution | Cancelled execution nem zárja a Sheet-et |
| A4-3 | Két különálló FSM | `CuttingSheet.Status` ≠ `CuttingExecution.Status` |
| A4-4 | Mérföldkő rugalmas, befejezés szigorú | Cutting `Completed` = 7/7 panel; mások event-feliratkozással |
| A4-5 | In-tenant real-time, cross-tenant pull-only | SSE nap 1-től LIVE; kifelé csak ETag-elt pull, rate-limit-tel |
| A4-6 | Append-only progress events | UPDATE és DELETE tilos (DB-trigger), DBA-szinten is |
| A4-7 | Idempotens progress POST | UUID v7 `ProgressEventId` — duplikátum no-op |
| A4-8 | Completion proof minimum kötelező | Level 0 (HashOnly) mindig |
| A4-9 | Crypto-shredding GDPR Art. 17-re | Photo blob AES-256 per-execution kulccsal |
| A4-10 | Cross-tenant policy merge `max(issuer, executor)` | Magasabb minimumszint nyer |
| A4-11 | Hash-chain integration | Per-tenant chain (P1-8 LIVE) |
| A4-12 | Approved package list érintetlen | Face detection sidecar, nem Kernel-dep |
| A4-13 | Atomicity over eventual consistency | Cutting-Inventory offcut INSERT egy tranzakcióban |
| A4-14 | Secrets schema-isolation | Külön DB schema + dedikált role |
| A4-15 | Aggregate fizikai DELETE tilos | Csak `Cancel()` állapotátmenet |
| A4-16 | Két-slot KEK lifecycle | `PRIMARY` + `PREVIOUS` koegzisztál, soha single-key window |
| A4-17 | Per-event worker HMAC | Minden mutating progress POST aláírt `EventHmac`-fel |
| A4-18 | Async consent withdrawal | HTTP 202 + queue + status endpoint, soha sync long-running |
| A4-19 | In-process adapter only (cross-module) | `AssemblyLoadContext` startup assertion |
| **A4-20 (v4)** | **Outbox-pattern domain event delivery** | **Aggregate event-batch + Kernel outbox `outbox_messages` ugyanazon tranzakcióban; post-commit BackgroundService fan-out (BE-A01)** |
| **A4-21 (v4)** | **Per-batch DbContext scope BackgroundService-ben** | **`KekRewrapBackgroundService` és `ConsentWithdrawalProcessor` minden iterációhoz új scope, `ChangeTracker.Clear()` (BE-A02)** |

---

## 3. NuGet csomagok és belső dependency graph

### 3.1 Új csomagok (Phase 4)

| Csomag | Cél | Multi-target |
|--------|-----|--------------|
| `SpaceOS.Modules.Cutting.Execution` | Aggregate, Application, Infrastructure, Api | net8.0 |
| `SpaceOS.Modules.Cutting.Execution.Contracts` | Public DTO, Handshake schema | net8.0; netstandard2.1 |
| `SpaceOS.Modules.Workers.Consent` | Worker consent records, withdrawal queue, retroactive processor | net8.0 |

### 3.2 Sidecar service

| Komponens | Stack | Port | Hozzáférés |
|-----------|-------|------|------------|
| `SpaceOS.Sidecar.ImageHardening` | Python 3.11 + FastAPI + insightface (ONNX) + Pillow + python-magic | 5099 | csak loopback, mTLS + SPKI pinning, HMAC body-sig, process-isolated Pillow worker |

### 3.3 Approved package list (Phase 4 — Kernel)

`MediatR` · `FluentValidation` · `Ardalis.Result` · `Ardalis.Specification` · `EF Core 8` · `Npgsql` · `xUnit v3` · `Moq` · `Microsoft.AspNetCore.SignalR` · `Microsoft.Extensions.Caching.StackExchangeRedis` · `Standart.Hash.xxHash` · `Serilog.Sinks.File` · `Microsoft.Extensions.Http` (BE-A04: `IHttpClientFactory`)

### 3.4 Belső dependency graph

```
SpaceOS.Modules.Cutting.Execution
├── SpaceOS.Modules.Cutting (Phase 3)
├── SpaceOS.Modules.Cutting.Execution.Contracts
├── SpaceOS.Modules.Workers.Consent.Contracts
├── SpaceOS.Kernel.Domain
├── SpaceOS.Kernel.Application
│   ├── IUnitOfWork (Phase 3B — outbox interceptor)
│   ├── IOutboxWriter (Phase 3B re-use, BE-A01)
│   └── ResultExtensions (Cabinet 0.1 v4 re-use, BE-A06)
├── SpaceOS.Modules.Inventory.Contracts
│   └── IOffcutReturnAdapter (atomic, in-process, connection-string asserted — A4-13, A4-19, BE-A05)
├── SpaceOS.Modules.Cutting.Nesting (Phase 3)
└── ⚠️ NEM függ:
    ├── HTTP clients to other modules (A4-19)
    ├── Saját Cutting-modul-szintű outbox (BE-A01: Kernel outbox re-use)
    └── Modules.Manufacturing/Logistics/Installation (event-bus szubszkripció)
```

---

## 4. Domain modell

### 4.1 Solution struktúra (végleges v4)

```
SpaceOS.Modules.Cutting.Execution/
├── Domain/
│   ├── Aggregates/CuttingExecution.cs
│   ├── Entities/{ProgressEvent, OffcutReport, MilestoneSubscription}.cs
│   ├── ValueObjects/{CompletionProof, WorkerAssignment, MachineAssignment,
│   │                  MilestoneDefinition, ExecutionTimeWindow,
│   │                  ProgressEventId, PanelCompletionRecord,
│   │                  WorkerEventHmac, AdvisoryLockKey}.cs
│   ├── Enums/{CuttingExecutionStatus, ProofLevel, ProgressEventKind,
│   │          MilestoneKind, CancelReason, ConsentScope}.cs
│   ├── Events/ ... (12 domain event)
│   ├── Policies/{ICuttingProofPolicy, ICuttingProgressPolicy,
│   │             IWorkerSecurityPolicy}.cs
│   ├── Predicates/{IMilestonePredicate + 4 built-in}.cs
│   ├── Repositories/ICuttingExecutionRepository.cs
│   ├── Specifications/                                              (BE-A03)
│   │   ├── CuttingExecutionByIdSpec.cs
│   │   ├── ActiveExecutionsByTenantSpec.cs
│   │   ├── ExecutionsBySheetSpec.cs
│   │   ├── ExecutionsByMachineAndDateSpec.cs
│   │   ├── ExecutionsByHandshakeEpicSpec.cs
│   │   ├── PendingMilestonesSpec.cs
│   │   ├── ExecutionsByConsentScopeSpec.cs                          (BE-A03)
│   │   ├── ConsentAffectedPhotoCountSpec.cs                         (BE-A03)
│   │   └── ExecutionKeyByExecutionSpec.cs                           (BE-A03)
│   ├── Eventing/{AggregateEventBatch, BatchSequenceNumber}.cs
├── Application/
│   ├── Commands/ ... (8 command + handler + validator)
│   ├── Queries/ ... (6 query + handler)
│   ├── EventHandlers/ ... (5 handler)
│   ├── DTOs/ ...
│   ├── Predicates/PredicateFactoryV1.cs
│   ├── Realtime/ICuttingExecutionRealtimePublisher.cs
│   ├── Realtime/IExecutionAccessChecker.cs
│   ├── ConsentWithdrawal/ConsentWithdrawalProcessor.cs              (BE-A02 per-batch scope)
│   ├── Audit/ICuttingAuditLogger.cs
│   ├── Validators/ ...                                              (BE-A07, BE-A09)
├── Infrastructure/
│   ├── Persistence/CuttingExecutionDbContext.cs
│   ├── Persistence/Configurations/ ...
│   ├── Persistence/Repositories/CuttingExecutionRepository.cs       (BE-A10 AsSplitQuery)
│   ├── Persistence/AdvisoryLock/ExecutionAdvisoryLock.cs
│   ├── Migrations/20260427_C-0004_CuttingExecution.cs
│   ├── Migrations/20260427_C-0005_CuttingSecretsSchema.cs
│   ├── Migrations/20260427_C-0006_ConsentWithdrawalQueue.cs
│   ├── Realtime/SignalRCuttingExecutionRealtimePublisher.cs
│   ├── Realtime/ExecutionAccessChecker.cs
│   ├── HashChain/CuttingExecutionHashChainAdapter.cs
│   ├── HashChain/AggregateBatchSerializer.cs
│   ├── ImageHardening/SidecarImageHardeningClient.cs                (BE-A04 IHttpClientFactory)
│   ├── ImageHardening/SidecarSpkiPinValidator.cs
│   ├── ImageHardening/SidecarRequestSigner.cs
│   ├── Worm/EncryptedWormBlobStore.cs
│   ├── Worm/PerExecutionKeyVault.cs
│   ├── Worm/MasterKekProvider.cs
│   ├── Worm/KekRewrapBackgroundService.cs                           (BE-A02 per-batch scope)
│   ├── Worm/SecretZeroization.cs
│   ├── Auth/JwtAlgorithmAllowlist.cs
│   ├── DI/InProcessAdapterAssertion.cs
│   ├── DI/InventoryAdapterConnectionAssertion.cs                    (BE-A05)
│   ├── Audit/SerilogCuttingAuditLogger.cs
├── Api/
│   ├── Endpoints/CuttingExecutionEndpoints.cs
│   ├── Endpoints/HandshakeProgressEndpoints.cs
│   ├── Endpoints/WorkerConsentEndpoints.cs
│   ├── Hubs/ExecutionHub.cs
│   ├── Middleware/HandshakeRateLimitMiddleware.cs
│   ├── Middleware/AuditLogMiddleware.cs
│   ├── Program.cs
└── Tests/ ... (≥ 210 teszt — v4 frissítve, +25 teszt a BE finding-ekért)
```

### 4.2 CuttingExecution aggregate (root)

> Változatlan v3-hoz képest. Az aggregate `PopDomainEvents()` outputja immár a Kernel `IUnitOfWork`-en keresztül kerül az `outbox_messages` táblába (A4-20).

```csharp
// Domain/Aggregates/CuttingExecution.cs
public sealed class CuttingExecution : TenantScopedEntity
{
    // ... (ugyanaz mint v3) ...

    public Result RecordProgress(
        ProgressEventId eventId, ProgressEventKind kind,
        PanelCompletionRecord? panel, DateTime occurredAt,
        WorkerEventHmac eventHmac,
        IWorkerSecurityPolicy securityPolicy,
        IDateTimeProvider clock)
    {
        // FSM-átmenet, HMAC-validation, idempotency, replay-protection, panel counter
        // → AddDomainEvent(...) — events később a SaveChanges-interceptor által outboxba kerülnek
    }

    // ... többi metódus változatlan ...
}
```

### 4.3 ProgressEvent, OffcutReport, MilestoneSubscription

> Változatlan v3-hoz képest.

### 4.4 Value Objects

> Változatlan v3-hoz képest (`WorkerEventHmac`, `AdvisoryLockKey`, `ConsentScope`, stb.).

### 4.5 Domain Events (12 db, változatlan)

```csharp
public sealed record CuttingExecutionScheduled(...) : IDomainEvent;
public sealed record CuttingExecutionStarted(...) : IDomainEvent;
public sealed record PanelCompleted(...) : IDomainEvent;
public sealed record ProgressRecorded(...) : IDomainEvent;
public sealed record OffcutReported(...) : IDomainEvent;
public sealed record MilestoneReached(...) : IDomainEvent;
public sealed record CuttingExecutionCompleted(...) : IDomainEvent;
public sealed record CuttingExecutionCancelled(...) : IDomainEvent;
public sealed record CompletionProofCommitted(...) : IDomainEvent;
public sealed record WorkerConsentWithdrawalRequested(...) : IDomainEvent;
public sealed record WorkerConsentWithdrawalCompleted(...) : IDomainEvent;
public sealed record CompletionProofRefreshedAfterConsentWithdrawal(...) : IDomainEvent;
```

### 4.6 Aggregate event batching (változatlan v3-hoz képest, A4-20-szal kombinálva)

A `IUnitOfWork.SaveChangesAsync` interceptor mintáját Phase 3B vezette be. Phase 4-ben **re-use-oljuk**:

```csharp
// SpaceOS.Kernel.Application/UnitOfWork/OutboxInterceptor.cs (Phase 3B LIVE — itt csak referenciálva)
public sealed class OutboxInterceptor(IDateTimeProvider clock, IOutboxWriter outbox)
    : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct)
    {
        var ctx = eventData.Context!;
        var aggregates = ctx.ChangeTracker.Entries<IAggregateRoot>()
            .Where(e => e.Entity.HasDomainEvents())
            .Select(e => e.Entity).ToList();

        foreach (var agg in aggregates)
        {
            // SEC-09: deterministic batch ordering
            var batch = AggregateEventBatch.FromAggregate(agg);
            for (var i = 0; i < batch.Events.Count; i++)
            {
                await outbox.AppendAsync(new OutboxMessage(
                    Id: Guid.CreateVersion7(),
                    TenantId: agg.GetTenantId(),
                    BatchId: batch.BatchId,
                    BatchSequenceNumber: i,
                    AggregateId: batch.AggregateId,
                    AggregateType: batch.AggregateType.Name,
                    EventType: batch.Events[i].GetType().Name,
                    PayloadJson: AggregateBatchSerializer.SerializeToString(batch.Events[i]),
                    OccurredAt: clock.UtcNow,
                    Status: OutboxMessageStatus.Pending), ct).ConfigureAwait(false);
            }
            agg.ClearDomainEvents();
        }
        return result;
    }
}
```

**Hatás:** az outbox-rekord és az aggregate-mutáció ugyanabban a tranzakcióban kerül a DB-be → at-least-once delivery + per-tenant FIFO + DB rollback esetén az event eltűnik (BE-A08).

A Kernel `OutboxDispatcher` (BackgroundService, Phase 3B LIVE) post-commit pollozza a `outbox_messages` táblát (`Status = Pending`) és fan-out-ol:
- SignalR hub-ba (in-tenant real-time, SEC-15-tel ellenőrzve)
- Hash-chain sink-be (per-tenant, SEC-09 batch-tagged)
- Cross-tenant Handshake mirror outbox-ba (FIFO peer-pull source)

---

## 5. Public API surface

### 5.1 Aggregate-belépési pontok (Commands)

| Command | Trigger | v4 részletek |
|---------|---------|--------------|
| `ScheduleExecution` | Műhely-vezető napi ütemterv | Validator: shape (FluentValidation); domain szabály: `Schedule()` factory (BE-A07) |
| `StartExecution` | Operátor badge tap → Start | — |
| `RecordProgress` | Worker app POST (kötelező EventHmac, advisory lock + outbox) | — |
| `RecordOffcut` | Worker app POST (atomic Cutting+Inventory, BE-A05 connection-string assert) | — |
| `CompleteExecution` | 7/7 panel + proof commit | — |
| `CancelExecution` | Aggregate state-átmenet | — |
| `RegisterMilestoneSubscription` | Cross-domain modul saját subscription-je | versioned JSONB |
| `WithdrawWorkerConsent` | Worker self-service / HR — Validator `MustAsync(...)` cross-check (BE-A09) | Async, HTTP 202 |

### 5.2 Read endpointok (változatlan v3-hoz képest)

### 5.3 Real-time channels (változatlan v3-hoz képest, IExecutionAccessChecker SEC-15)

### 5.4 Cross-tenant Handshake endpoints (változatlan v3-hoz képest)

### 5.5 Sidecar API (változatlan v3-hoz képest, **`IHttpClientFactory` named client `"sidecar"` BE-A04**)

### 5.6 Worker Consent endpoints (változatlan v3-hoz képest)

---

## 6. Persistence contract

### 6.1 Migration C-0004 (változatlan v2-höz képest)
### 6.2 Migration C-0005 — Cutting Secrets schema (változatlan v2-höz képest)
### 6.3 Migration C-0006 — Consent Withdrawal Queue (változatlan v3-hoz képest)

### 6.4 Outbox tábla integráció (BE-A01)

A Kernel `outbox_messages` tábla **Phase 3B-ben már LIVE**. Phase 4 nem hoz létre újat, csak **re-use-olja**. Releváns mezők, amik a Cutting domain event-ek számára tükröződnek:

```sql
-- Existing in Kernel (Phase 3B), referenced by Phase 4
CREATE TABLE IF NOT EXISTS public."OutboxMessages" (
    "Id"                   uuid        PRIMARY KEY,        -- UUID v7 — global ordering
    "TenantId"             uuid        NOT NULL,
    "BatchId"              uuid        NOT NULL,           -- SEC-09: aggregate batch ID
    "BatchSequenceNumber"  integer     NOT NULL,           -- SEC-09: 0-based within batch
    "AggregateId"          uuid        NOT NULL,
    "AggregateType"        varchar(128) NOT NULL,
    "EventType"            varchar(128) NOT NULL,
    "PayloadJson"          jsonb       NOT NULL,
    "OccurredAt"           timestamptz NOT NULL,
    "DispatchedAt"         timestamptz NULL,
    "Status"               smallint    NOT NULL,           -- 1=Pending, 2=Dispatched, 3=Failed
    "Attempts"             integer     NOT NULL DEFAULT 0,
    "LastError"            varchar(2048) NULL
);

-- Existing indexes
-- IX_OutboxMessages_Polling on (Status, OccurredAt) WHERE Status = 1
-- IX_OutboxMessages_Tenant on (TenantId, OccurredAt)
-- IX_OutboxMessages_Batch on (BatchId, BatchSequenceNumber)  -- SEC-09 verifier lookup
```

A Phase 4 új migration-ja **NEM módosítja ezt a táblát**. Csak az interceptor-regisztráció és a Cutting `DbContext` `UseInterceptor(OutboxInterceptor)` config kerül be.

### 6.5 Két-slot KEK infrastruktúra (változatlan v3-hoz képest)

### 6.6 ERD (változatlan v3-hoz képest)

### 6.7 Hash-chain integration (változatlan v3-hoz képest, outbox-on keresztül)

---

## 7. Algoritmusok és tranzakciós minták

### 7.1 Progress %-számítás (változatlan)
### 7.2 Milestone evaluation (változatlan)
### 7.3 Material consumption reconciliation (változatlan)

### 7.4 RecordProgress handler — outbox-pattern explicit (BE-A01, A4-20)

```csharp
// Application/Commands/RecordProgress/RecordProgressCommandHandler.cs
public sealed class RecordProgressCommandHandler(
    ICuttingExecutionRepository repo,
    CuttingExecutionDbContext db,           // OutboxInterceptor van rácsatolva
    IWorkerSecurityPolicy securityPolicy,
    ICuttingAuditLogger audit,
    IDateTimeProvider clock)
    : IRequestHandler<RecordProgressCommand, Result>
{
    public async Task<r> Handle(RecordProgressCommand cmd, CancellationToken ct)
    {
        await using var tx = await db.Database
            .BeginTransactionAsync(ct).ConfigureAwait(false);

        // SEC-04: deterministic 63-bit advisory lock
        var lockKey = AdvisoryLockKey.From(cmd.ExecutionId);
        await db.Database.ExecuteSqlRawAsync(
            "SELECT pg_advisory_xact_lock({0}::bigint)",
            new object[] { lockKey.Value }, ct).ConfigureAwait(false);

        var execution = await repo.GetByIdAsync(cmd.ExecutionId, ct).ConfigureAwait(false);
        if (execution is null) return Result.NotFound("Execution not found");
        if (execution.TenantId != cmd.TenantId) return Result.Forbidden();

        var eventIdResult = ProgressEventId.Create(cmd.EventId);
        if (!eventIdResult.IsSuccess) return eventIdResult.MapToResult();   // BE-A06

        var hmacResult = WorkerEventHmac.Create(cmd.EventHmacBase64, cmd.HmacKeyVersion);
        if (!hmacResult.IsSuccess) return hmacResult.MapToResult();         // BE-A06

        var result = execution.RecordProgress(
            eventIdResult.Value, cmd.Kind, cmd.Panel, cmd.OccurredAt,
            hmacResult.Value, securityPolicy, clock);

        if (!result.IsSuccess)
        {
            if (result.Status == ResultStatus.Forbidden)
                await audit.LogSecurityEventAsync(
                    "INVALID_PROGRESS_HMAC", cmd.TenantId, cmd.ExecutionId, cmd.EventId, ct);
            return result;
        }

        execution.EvaluateMilestones(clock);

        // SaveChanges → OutboxInterceptor → outbox_messages writes (atomic w/ aggregate)
        await repo.SaveChangesAsync(ct).ConfigureAwait(false);
        await tx.CommitAsync(ct).ConfigureAwait(false);

        // Domain events már outboxba kerültek; OutboxDispatcher BackgroundService
        // post-commit fan-outol SignalR-be, hash-chain-be, Handshake mirror-ba
        return Result.Success();
    }
}
```

### 7.5 RecordOffcut handler — atomic + connection-string assertion (BE-A05)

```csharp
// Application/Commands/RecordOffcut/RecordOffcutCommandHandler.cs
public sealed class RecordOffcutCommandHandler(
    ICuttingExecutionRepository repo,
    IOffcutReturnAdapter inventoryAdapter,
    IInventoryAdapterConnectionAssertion connectionGuard,    // BE-A05
    CuttingExecutionDbContext db,
    IDateTimeProvider clock)
    : IRequestHandler<RecordOffcutCommand, Result>
{
    public async Task<r> Handle(RecordOffcutCommand cmd, CancellationToken ct)
    {
        await using var tx = await db.Database
            .BeginTransactionAsync(ct).ConfigureAwait(false);

        var execution = await repo.GetByIdAsync(cmd.ExecutionId, ct).ConfigureAwait(false);
        if (execution is null) return Result.NotFound("Execution not found");
        if (execution.TenantId != cmd.TenantId) return Result.Forbidden();

        var offcutResult = OffcutReport.Create(/* ... */);
        if (!offcutResult.IsSuccess) return offcutResult.MapToResult();    // BE-A06

        var domainResult = execution.RecordOffcut(offcutResult.Value);
        if (!domainResult.IsSuccess) return domainResult;

        // BE-A05: runtime guard — InventoryDb must share connection with Cutting
        connectionGuard.AssertSharedConnection(db.Database.GetDbConnection());

        // DB-10 / A4-13: shared transaction with Inventory
        var inventoryResult = await inventoryAdapter.ReturnOffcutAsync(
            new OffcutReturnDto(/* ... */),
            db.Database.GetDbConnection(), tx.GetDbTransaction(), ct)
            .ConfigureAwait(false);

        if (!inventoryResult.IsSuccess)
        {
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            return inventoryResult;
        }

        await repo.SaveChangesAsync(ct).ConfigureAwait(false);   // outbox-ba is ír
        await tx.CommitAsync(ct).ConfigureAwait(false);
        return Result.Success();
    }
}

// Infrastructure/DI/InventoryAdapterConnectionAssertion.cs (BE-A05)
public sealed class InventoryAdapterConnectionAssertion(
    IConfiguration config) : IInventoryAdapterConnectionAssertion
{
    private readonly string _expectedDataSource;
    
    public InventoryAdapterConnectionAssertion(IConfiguration config)
    {
        var cuttingCs = new NpgsqlConnectionStringBuilder(config.GetConnectionString("Cutting")!);
        var inventoryCs = new NpgsqlConnectionStringBuilder(config.GetConnectionString("Inventory")!);
        if (cuttingCs.Host != inventoryCs.Host || cuttingCs.Port != inventoryCs.Port
            || cuttingCs.Database != inventoryCs.Database)
        {
            throw new InvalidOperationException(
                "BE-A05: Cutting and Inventory MUST share the same DB instance for atomic " +
                "offcut return (A4-13). Different hosts/ports/databases would silently break " +
                "the shared-transaction contract.");
        }
        _expectedDataSource = $"{cuttingCs.Host}:{cuttingCs.Port}/{cuttingCs.Database}";
    }

    public void AssertSharedConnection(DbConnection conn)
    {
        var actual = new NpgsqlConnectionStringBuilder(conn.ConnectionString);
        var actualDataSource = $"{actual.Host}:{actual.Port}/{actual.Database}";
        if (actualDataSource != _expectedDataSource)
            throw new InvalidOperationException(
                $"BE-A05: Connection target {actualDataSource} != expected {_expectedDataSource}");
    }
}
```

### 7.6 Cross-tenant pull workflow (változatlan v2-höz képest)

### 7.7 Aggregate event batching (BE-A01: explicit outbox a flow-ba)

```
RecordProgress handler                 OutboxInterceptor                Kernel OutboxDispatcher
(transaction-aware)                    (in SaveChanges)                 (BackgroundService)
        │                                       │                                │
        │ aggregate.RecordProgress()             │                                │
        │   AddDomainEvent(PanelCompleted)       │                                │
        │   AddDomainEvent(ProgressRecorded)     │                                │
        │                                       │                                │
        │ SaveChangesAsync()                     │                                │
        │   ───────────────────────────────►    │ aggregates with events         │
        │                                       │   AggregateEventBatch.From()    │
        │                                       │   for each event:               │
        │                                       │      INSERT outbox_messages    │
        │                                       │      (BatchId, SeqNum, ...)    │
        │   ◄───────────────────────────────    │ done                           │
        │                                       │                                │
        │ tx.CommitAsync()                       │                                │
        │   (aggregate + outbox commit atomic)   │                                │
        │                                       │                                │
        │                                       │                  Pollozza:     │
        │                                       │                  SELECT * FROM │
        │                                       │                  OutboxMessages│
        │                                       │                  WHERE Status=1│
        │                                       │                                │
        │                                       │              For each message: │
        │                                       │              ├─► SignalR fan-out
        │                                       │              ├─► Hash-chain sink
        │                                       │              └─► Handshake mirror
        │                                       │                  UPDATE Status=2
```

**At-least-once garancia:** ha az `OutboxDispatcher` crash-el delivery előtt, restart után újrapróbálja (Status=1 marad). Idempotens fogadói oldal kötelező — SignalR pubsub természeténél fogva idempotens (UI subscriber duplikátumot ignorál); hash-chain `BatchId`+`SeqNum` UNIQUE INDEX-szel idempotensé tett.

### 7.8 Consent withdrawal — per-batch scope (BE-A02)

```csharp
// Application/ConsentWithdrawal/ConsentWithdrawalProcessor.cs (BE-A02 frissítés)
public sealed class ConsentWithdrawalProcessor(
    IServiceScopeFactory scopeFactory,
    IDateTimeProvider clock,
    ILogger<ConsentWithdrawalProcessor> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            // BE-A02: per-pickup scope (short-lived, releases connection back to pool)
            Guid? withdrawalId;
            await using (var pickupScope = scopeFactory.CreateAsyncScope())
            {
                var repo = pickupScope.ServiceProvider
                    .GetRequiredService<IConsentWithdrawalRepository>();
                var pending = await repo.PickupNextPendingAsync(ct).ConfigureAwait(false);
                withdrawalId = pending?.Id;
                if (pending is not null)
                {
                    pending.MarkProcessing(clock.UtcNow);
                    await repo.SaveAsync(pending, ct).ConfigureAwait(false);
                }
            }

            if (withdrawalId is null)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), ct).ConfigureAwait(false);
                continue;
            }

            await ProcessWithdrawalAsync(withdrawalId.Value, ct).ConfigureAwait(false);
        }
    }

    private async Task ProcessWithdrawalAsync(Guid withdrawalId, CancellationToken ct)
    {
        // BE-A02: per-batch (5 photos) scope — release every iteration
        const int batchSize = 5;
        while (true)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var repo = scope.ServiceProvider.GetRequiredService<IConsentWithdrawalRepository>();
            var sidecar = scope.ServiceProvider.GetRequiredService<ISidecarImageHardeningClient>();
            var keyVault = scope.ServiceProvider.GetRequiredService<IPerExecutionKeyVault>();
            var wormStore = scope.ServiceProvider.GetRequiredService<IEncryptedWormBlobStore>();

            var pending = await repo.GetByIdAsync(withdrawalId, ct).ConfigureAwait(false);
            if (pending is null || pending.Status == ConsentWithdrawalStatus.Completed) return;

            var nextBatch = await repo.ListAffectedExecutionsBatchAsync(
                pending.TenantId, pending.WorkerId, pending.Scope,
                offset: pending.ProcessedPhotos + pending.FailedPhotos,
                limit: batchSize, ct).ConfigureAwait(false);

            if (nextBatch.Count == 0)
            {
                pending.MarkCompleted(clock.UtcNow);
                await repo.SaveAsync(pending, ct).ConfigureAwait(false);
                return;
            }

            foreach (var execId in nextBatch)
            {
                if (ct.IsCancellationRequested) return;
                try
                {
                    await ReprocessPhotoAsync(scope.ServiceProvider, execId, withdrawalId, ct);
                    pending.IncrementProcessed();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to reprocess photo for execution {Id}", execId);
                    pending.IncrementFailed();
                }
                await Task.Delay(TimeSpan.FromMilliseconds(200), ct).ConfigureAwait(false);
            }

            await repo.SaveAsync(pending, ct).ConfigureAwait(false);

            // BE-A02: scope dispose → connection back to pool, change tracker GC-able
            // Next iteration: fresh scope, fresh DbContext, no accumulating state
        }
    }

    private static async Task ReprocessPhotoAsync(
        IServiceProvider sp, Guid executionId, Guid withdrawalId, CancellationToken ct)
    {
        // ... (unchanged from v3) ...
    }
}
```

### 7.9 Sidecar interaction — IHttpClientFactory named client (BE-A04)

```csharp
// Api/Program.cs (DI setup)
builder.Services.AddHttpClient<ISidecarImageHardeningClient, SidecarImageHardeningClient>(
    "sidecar", client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Sidecar:BaseUrl"]!);
        client.Timeout = TimeSpan.FromSeconds(10);
    })
    .ConfigurePrimaryHttpMessageHandler(sp =>
    {
        var pinValidator = sp.GetRequiredService<ISidecarSpkiPinValidator>();
        var clientCert = LoadKernelClientCert(builder.Configuration);    // SEC-03 mTLS
        var handler = new SocketsHttpHandler
        {
            SslOptions = new SslClientAuthenticationOptions
            {
                ClientCertificates = new X509CertificateCollection { clientCert },
                RemoteCertificateValidationCallback = (s, cert, chain, errors)
                    => pinValidator.IsValid(cert),    // SEC-03 SPKI pin
            },
            // BE-A04: handler-pooling — single mTLS+SPKI config across HttpClient instances
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        };
        return handler;
    });

// Infrastructure/ImageHardening/SidecarImageHardeningClient.cs
public sealed class SidecarImageHardeningClient(
    HttpClient httpClient,                 // injected by IHttpClientFactory
    SidecarRequestSigner signer,
    IDateTimeProvider clock,
    ICuttingAuditLogger audit)
    : ISidecarImageHardeningClient
{
    public async Task<HardenResult> HardenAsync(
        byte[] photoBytes, bool blurFaces, int resizeMaxPx, CancellationToken ct)
    {
        // ... ugyanaz mint v3 ...
    }
}
```

### 7.10 BE-A05 Inventory adapter assertion runtime + startup (új)

A `InventoryAdapterConnectionAssertion` ctor-ban (DI Singleton) **startup-time** hibázik, ha a két connection-string nem ugyanazt a DB-instance-et célozza meg. Plus runtime `AssertSharedConnection` ellenőrzés minden `RecordOffcutCommandHandler` hívásban.

```csharp
// Api/Program.cs
builder.Services.AddSingleton<IInventoryAdapterConnectionAssertion,
                               InventoryAdapterConnectionAssertion>();
// Triggers ctor at startup: throws if connection-strings diverge → deploy fail-fast
var sp = builder.Services.BuildServiceProvider();
sp.GetRequiredService<IInventoryAdapterConnectionAssertion>();
```

(Megjegyzés: ez egyetlen `BuildServiceProvider()` startup-warmup hívás, **az alkalmazás-szintű DI-ben tilos** általában, de itt a Cabinet 0.1 v4 precedens szerint deploy-fail-fast funkciót szolgál — ezt a Golden Rule kivételt indokolja a kommentár.)

---

## 8. Validáció és invariánsok

### 8.1 VO szintű validáció (változatlan)

### 8.2 Aggregate-szintű invariánsok (frissítve v4)

| Invariáns | Ellenőrzés helye |
|-----------|------------------|
| FSM-átmenetek csak engedélyezett irányba | Aggregate methods |
| `PanelsCompleted ≤ TotalPanels` | DB CHECK + aggregate |
| Idempotens progress (EventId egyszer) | DB UNIQUE + aggregate |
| Per-event HMAC validation | Aggregate `RecordProgress` + `IWorkerSecurityPolicy` |
| Cross-tenant: executor proof ≥ issuer min | Aggregate `Schedule` |
| Status & timestamps konzisztencia | DB CHECK |
| Egyszerre 1 aktív execution sheet-enként | DB UNIQUE PARTIAL |
| Append-only progress events (UPDATE+DELETE) | DB-trigger |
| Aggregate fizikai DELETE tilos | DB-trigger |
| PanelCompleted shape-integrity | DB CHECK |
| Predicate JSONB v-tagged | DB CHECK |
| Completion proof level ≥ policy minimum | Aggregate `Complete` policy.Validate |
| Completion előtt `PanelsCompleted == TotalPanels` | Aggregate `Complete` |
| Offcut atomicity (Cutting+Inventory) | Handler — közös tranzakció + connection-string assertion (BE-A05) |
| Concurrent RecordProgress sorosítás | Handler `pg_advisory_xact_lock` |
| Domain event-batch ordering | `AggregateEventBatch` + per-event sequence |
| Cross-module adapter in-process | DI startup assertion |
| **Outbox + aggregate atomic write** | **`OutboxInterceptor` ugyanazon `SaveChangesAsync`-ben (BE-A01, BE-A08)** |
| **BackgroundService scope per batch** | **`CreateAsyncScope()` + `ChangeTracker.Clear()` (BE-A02, A4-21)** |

### 8.3 Cross-tenant proof policy (változatlan v2-höz képest)

### 8.4 JWT validation pinning (változatlan v3-hoz képest)

### 8.5 IWorkerSecurityPolicy (változatlan v3-hoz képest)

### 8.6 Validator vs. domain rule split (BE-A07)

| Layer | Felelősség | Példa |
|-------|------------|-------|
| FluentValidation | Shape + tartomány-ellenőrzés (input parsing) | `WidthMm > 0`, `MaterialCode.Length <= 64`, `ProgressEventId != Guid.Empty` |
| VO factory `Result.Invalid` | Domain-szabály-érvényesítés | `ExecutionTimeWindow.EndAt > StartAt`, `CompletionProof.CreateLevel2` consent-presence |
| Aggregate method `Result.Conflict/Forbidden/Invalid` | Aggregate-szintű invariánsok | `Status == InProgress` guard, `PanelsCompleted < TotalPanels` |

**Tilos:** ugyanazt a logikát mind validator-ban, mind aggregate-ben implementálni — ha valaha eltérnének, silent inconsistency.

---

## 9. Definition of Done

### 9.1 Migration gates (változatlan v3-hoz képest, +outbox interceptor regisztráció)

- [ ] **C-0004** migration applied (idempotens)
- [ ] **C-0005** secrets schema migration applied (idempotens)
- [ ] **C-0006** consent withdrawal queue migration applied
- [ ] All 5 spaceos_cutting tables RLS **ENABLED + FORCED** with explicit `USING + WITH CHECK + TO spaceos_app`
- [ ] `spaceos_cutting_secrets` schema only accessible by `spaceos_keyvault_role`
- [ ] All append-only triggers in place
- [ ] All CHECK constraints validated
- [ ] All partial + compound indexes in place
- [ ] WORM filesystem mount present, append-only
- [ ] **`CuttingExecutionDbContext` regisztrálva `.AddInterceptors(OutboxInterceptor)`-rel (BE-A01)**

### 9.2 Domain gates (frissítve)

- [ ] All 12 domain events implement `IDomainEvent`
- [ ] Aggregate has zero public setters
- [ ] FSM transitions return `Result<T>` — never throw
- [ ] `EvaluateMilestones()` is pure
- [ ] All 4 built-in `IMilestonePredicate` covered + `ConfigVersion = 1`
- [ ] `CompletionProof` factory methods enforce level invariants
- [ ] `IWorkerSecurityPolicy.ValidateProgressEventHmac` uses `FixedTimeEquals`
- [ ] `AdvisoryLockKey.From` uses xxHash64
- [ ] **All 9 specifications implemented (BE-A03)** — 6 v2 + 3 új v4
- [ ] **All list queries via Specification — no raw SQL in repository (Golden Rule 5)**

### 9.3 API + validation gates (frissítve)

- [ ] Every command has FluentValidation validator
- [ ] **Validator ≠ domain rule duplication (BE-A07)** — code review checklist
- [ ] **`WithdrawWorkerConsent` validator MustAsync cross-check on active consent (BE-A09)**
- [ ] Every handler returns `Result<T>` and uses `ConfigureAwait(false)`
- [ ] Every read query uses `AsNoTracking()` + Specification + **`AsSplitQuery()` aggregate-tel (BE-A10)**
- [ ] **3 OpenAPI snapshot fájl** committed (BE-A11): `Cutting.Execution.openapi.snapshot.json`, `Cutting.Execution.handshake.openapi.snapshot.json`, `Cutting.Execution.sidecar.openapi.snapshot.json`
- [ ] Cross-tenant endpoints **GET-only**
- [ ] `RecordProgressCommandHandler` advisory lock first in transaction
- [ ] `RecordOffcutCommandHandler` shares connection + transaction with Inventory adapter
- [ ] `WithdrawWorkerConsent` returns HTTP 202 + tracking URL
- [ ] **`ResultExtensions.MapToResult` documented + reused in all handlers (BE-A06)**

### 9.4 Real-time gates (frissítve)

- [ ] SignalR Hub registered, JWT bearer auth enforced
- [ ] `IExecutionAccessChecker.CanSubscribeAsync` invoked on `JoinGroup`
- [ ] In-tenant SSE active **at launch** — no opt-in flag
- [ ] Reconnect logic tested
- [ ] **Outbox → SignalR fan-out tested** (integration teszt: domain event → outbox → SignalR delivery)
- [ ] **All 7 SignalR-broadcasted events covered by integration test**

### 9.5 Cross-tenant gates (változatlan v3-hoz képest)

### 9.6 Security gates (változatlan v3-hoz képest, lásd v3 §9.6)

### 9.7 Backend gates (új — BE finding-ek deployment gate-ként)

- [ ] **OUT-1 (BE-A01):** Outbox row + aggregate row egy tranzakcióban — integration teszt: DB rollback → outbox üres marad
- [ ] **OUT-2 (BE-A08):** OutboxDispatcher idempotens — restart után nem küld duplikátumot SignalR-be (idempotency tesztelve)
- [ ] **SCOPE-1 (BE-A02):** `KekRewrapBackgroundService` per-batch scope — load test 10000 key re-wrap → connection pool nem fogy ki
- [ ] **SCOPE-2 (BE-A02):** `ConsentWithdrawalProcessor` per-batch scope — 5000 fotó-re-process → memory plateau, nem nő
- [ ] **REPO-1 (BE-A03):** Repository raw SQL grep → 0 találat (csak Specification)
- [ ] **HTTP-1 (BE-A04):** `IHttpClientFactory` named-client `"sidecar"` → mTLS handler 1× töltődik, lifetime 5 perc
- [ ] **CONN-1 (BE-A05):** Cutting + Inventory connection-string equality startup-asszerció — divergens config → deploy fail
- [ ] **CONN-2 (BE-A05):** Runtime `AssertSharedConnection` integration teszt
- [ ] **QUERY-1 (BE-A10):** `EXPLAIN ANALYZE GetByIdAsync` → 3 separate queries (split), NEM cartesian product

### 9.8 Test gates (frissítve v4)

- [ ] **Unit tests:** ≥ 95 (CuttingExecution FSM + invariants + VO + PredicateFactory + WorkerSecurityPolicy + AdvisoryLockKey + AggregateEventBatch + ConsentWithdrawal + ResultExtensions)
- [ ] **Integration tests:** ≥ 60 (handlers + EF mapping + RLS + hash-chain + crypto-shredding + advisory lock concurrency + Inventory atomic + DELETE-block + secrets schema + KEK rotation + sidecar mTLS + audit log + **outbox-tx + BackgroundService scope + AsSplitQuery EXPLAIN + connection-string assertion**)
- [ ] **API tests:** ≥ 30 (commands + queries + cross-tenant + rate-limit + JWT algorithm + consent endpoints + 3 OpenAPI snapshot diff)
- [ ] **SignalR tests:** ≥ 10
- [ ] **Sidecar tests (Python):** ≥ 15
- [ ] **Phase 4 új tesztek összesen:** **≥ 210** (v3: 185 → v4: +25)
- [ ] **Existing 303 Cutting tests:** zöld
- [ ] **Existing ~4023 platform tests:** zöld

### 9.9 Összesített

- [ ] Meglévő ~4023 teszt zöld
- [ ] Phase 4 új tesztek: ≥ 210 db
- [ ] 0 build warning, 0 build error
- [ ] `ConfigureAwait(false)` minden production async call-ban
- [ ] `AsNoTracking()` minden read repository metóduson
- [ ] **`AsSplitQuery()` aggregate root `GetByIdAsync`-en (BE-A10)**
- [ ] `dotnet list package --vulnerable` → 0 high/critical
- [ ] `grep -r "BuildServiceProvider" --include="*.cs"` → 1 dokumentált találat (BE-A05 startup-warmup, kommentárban indokolt)
- [ ] `EXPLAIN ANALYZE`: Index Scan minden új query endpointon
- [ ] Golden Rules 1–12 teljesül
- [ ] All 3 migration tranzakcionális
- [ ] **3 OpenAPI snapshot fájl committed (BE-A11)**
- [ ] Sidecar deployed, healthcheck zöld, cert remaining > 60 days
- [ ] WORM mount + permissions verified
- [ ] Két-slot KEK env vars konfigurált (legalább PRIMARY)
- [ ] Két DB connection string konfigurált (`spaceos_app` + `spaceos_keyvault_role`)
- [ ] Redis ACL user `cutting-kernel` aktivált
- [ ] Audit log Serilog file sink aktív
- [ ] In-process adapter assertion startup-ban ellenőrizve
- [ ] **Cutting + Inventory connection-string equality startup-validálva (BE-A05)**
- [ ] **Kernel `OutboxDispatcher` BackgroundService LIVE-ban (Phase 3B re-use, BE-A01)**

---

## 10. Security adósság státusz

| ID | Tétel | Phase 3 előtt | Phase 4 (v4) | Marad |
|----|-------|---------------|--------------|-------|
| P1-3 | AggregateSnapshot | ✅ Phase 3B | reuse | — |
| P1-4 | Outbox Pattern | ✅ Phase 3B | **explicit Phase 4 re-use (BE-A01)** | — |
| P1-8 | ProofHash + WORM | ✅ Phase 3B | ✅ Phase 4 | — |
| P2-1 | Chain Integrity Verifier API | ✅ Phase 3B | enhanced w/ batch ordering | — |
| P2-3 | GDPR pseudonymization + PII separation | partial | ✅ Phase 4 | — |
| P4-1 | Sidecar mTLS cert rotation | n/a | ✅ Phase 4 (SPKI pin + 60-day cron, IHttpClientFactory handler reuse) | — |
| P4-2 | Master KEK rotation runbook | n/a | ✅ Phase 4 (két-slot, runbook v1) | annual exercise |
| P4-3 | Worker badge/PIN HMAC key per-tenant | n/a | ✅ Phase 4 | — |
| P4-4 | Handshake rate-limit Redis cluster failover | n/a | partial (single-node) | Phase 5 |
| P4-5 | Photo blob max-size enforcement | n/a | ✅ Phase 4 (10MB cap) | — |
| P4-6 | Secrets schema role audit | n/a | ⚠️ Phase 4 (runbook) | annual |
| P4-7 | Advisory lock starvation monitoring | n/a | ⚠️ Phase 4 (Lynis monitor pg_locks) | continuous |
| P4-8 | Két-slot KEK infra | n/a | ✅ Phase 4 | — |
| P4-9 | Worker-key TPM enrollment infrastructure | n/a | ⚠️ Phase 4 (opt-in flag) | Phase 5 deferred |
| P4-10 | Consent withdrawal background processor | n/a | ✅ Phase 4 | — |
| P4-11 | Audit log retention policy + log-rotation | n/a | ⚠️ Phase 4 (90 days) | annual review |

---

## 11. Threat model (változatlan v3-hoz képest, 24 vektor)

> Backend review nem hozott új threat-vektort — csak konzisztencia-pontosításokat adott.

---

## 12. Mi jön utána (roadmap)

| Phase | Tartalom | Prerequisite |
|-------|----------|--------------|
| **Cutting Phase 5: Analytics** | Waste %, capacity, OEE, operator perf, TPM enrollment provisioning (P4-9) | Phase 4 DEPLOYED |
| **Cutting Phase 6: Adapters** | OptiCut + external nesting service adapters | Phase 4 DEPLOYED |
| **Modules.Manufacturing Phase 1** | Edge banding + drilling FSM, subscribes to `PanelCompleted(1/N)` milestone | Phase 4 milestone-publishing LIVE |
| **Modules.Logistics Phase 1** | Dispatch planning, subscribes to `CuttingExecutionCompleted` | Phase 4 + Manufacturing Phase 1 |
| **Modules.Installation Phase 1** | Install scheduling, subscribes to `LogisticsDelivered` | Logistics Phase 1 |

---

## 13. Claude Code implementációs csomag

### 13.1 Végrehajtási sorrend

A Phase 4 ~19 napra van skálázva. **3 párhuzamos track**, 4 fejlesztő (vagy 4 párhuzamos Claude Code agent-session) kapacitásra optimalizálva.

| Nap | Track A: Domain + Application | Track B: Infrastructure + Persistence | Track C: Api + Realtime + Tests |
|-----|-------------------------------|---------------------------------------|--------------------------------|
| 1   | Domain VO-k (`WorkerEventHmac`, `AdvisoryLockKey`, `ConsentScope`, `CompletionProof`, `MilestoneDefinition`) | C-0004 + C-0005 + C-0006 migration scaffold | xUnit v3 test project setup, fixtures |
| 2   | `CuttingExecution` aggregate FSM (Schedule, Start, Cancel) | EF Core configurations (`OwnsOne` + JSONB) | Aggregate FSM unit tests |
| 3   | `RecordProgress` aggregate metódus + `IWorkerSecurityPolicy` integráció | RLS + WITH CHECK + DB-trigger DDL (DB-01, DB-03) | RecordProgress aggregate unit tests |
| 4   | `RecordOffcut`, `Complete`, `EvaluateMilestones` | `prevent_execution_delete` trigger + `chk_panel_completed_shape` CHECK | Complete + Milestone unit tests |
| 5   | 4 `IMilestonePredicate` + `PredicateFactoryV1` | Idempotens DDL — re-run gate teszt | Predicate unit tests + JSONB v-switch |
| 6   | 9 Specification (`Ardalis.Specification`) | `spaceos_cutting_secrets` schema + role + `KeyVaultDbContext` | Specification unit tests |
| 7   | `RecordProgressCommandHandler` + advisory lock + outbox | `OutboxInterceptor` registration + `IUnitOfWork` integráció | RecordProgress integration test (advisory lock concurrency) |
| 8   | `RecordOffcutCommandHandler` + `IOffcutReturnAdapter` shared-tx | `InventoryAdapterConnectionAssertion` startup-time | RecordOffcut atomic integration test |
| 9   | `CompleteExecutionCommandHandler` + `Schedule/Start/Cancel` handlers | `EncryptedWormBlobStore` + `MasterKekProvider` két-slot | Handler unit + integration tests |
| 10  | 6 Query + handlers (specification-based) | `KekRewrapBackgroundService` + `PerExecutionKeyVault` crypto-shred | Query handler tests |
| 11  | FluentValidation validators (8 command) | `SidecarImageHardeningClient` + `SpkiPinValidator` + `RequestSigner` | Sidecar Python unit tests (libmagic + cgroup) |
| 12  | `EvaluateMilestonesOnProgressRecorded` + `ReturnOffcutsToInventoryOnReported` event handlers | SignalR `ExecutionHub` + `IExecutionAccessChecker` | Event handler tests |
| 13  | `WithdrawWorkerConsent` command + validator (cross-field) | `ConsentWithdrawalProcessor` BackgroundService (per-batch scope) | Consent endpoint tests |
| 14  | Audit logger + `ICuttingAuditLogger` | `HandshakeRateLimitMiddleware` + Redis ACL config | Rate-limit load test |
| 15  | `JwtAlgorithmAllowlist` + `InProcessAdapterAssertion` | `SerilogCuttingAuditLogger` + log rotation policy | JWT algorithm bypass negative test |
| 16  | API endpoints (8 command + 6 query + worker consent) | OpenAPI snapshot (3 fájl) generálás + diff CI gate | API integration tests |
| 17  | Cross-tenant Handshake endpoints + ETag cache | Photo upload e2e (sidecar HMAC body-sig) | Cross-tenant integration tests |
| 18  | OpenAPI snapshot review + post-merge cleanup | Crypto-shredding integration test (key delete → photo unreadable) | E2E SignalR fan-out test |
| 19  | Documentation + runbooks (KEK rotation, secrets schema audit) | Lynis scan, vulnerable package check | Smoke test + DoD checklist verify |

### 13.2 Agent utasítás (Claude Code prompt)

> **Implementáld a SpaceOS_Modules_Cutting_Phase4_Execution_Architecture_v4.md tervdokumentum szerint a következő feladatokat.**
>
> **Track A (Domain + Application):**
> - `SpaceOS.Modules.Cutting.Execution.Domain` projekt: `CuttingExecution` aggregate (Schedule/Start/RecordProgress/RecordOffcut/Complete/Cancel/EvaluateMilestones FSM), `ProgressEvent`/`OffcutReport`/`MilestoneSubscription` owned entitások, 8 VO (`WorkerEventHmac`, `AdvisoryLockKey`, `CompletionProof`, stb.), 12 domain event, `IMilestonePredicate` + 4 built-in, `IWorkerSecurityPolicy`/`ICuttingProofPolicy`/`ICuttingProgressPolicy` ports, 9 Specification.
> - `SpaceOS.Modules.Cutting.Execution.Application` projekt: 8 command-handler-validator triplet (FluentValidation shape-only, BE-A07), 6 query-handler, 5 event handler, `PredicateFactoryV1` (DB-09 version-switch), `ConsentWithdrawalProcessor` BackgroundService (per-batch scope BE-A02), `ResultExtensions` (Cabinet 0.1 v4 reuse).
> - **Golden Rules 1–12 érvényesítése:** zero public setter, business logic Domain-ben, Result<T> mindenhol, ConfigureAwait(false) minden production async call-ban.
>
> **Track B (Infrastructure + Persistence):**
> - `SpaceOS.Modules.Cutting.Execution.Infrastructure` projekt: `CuttingExecutionDbContext` + 4 EF Core Configuration (OwnsOne + JSONB), `CuttingExecutionRepository` (Specification + AsNoTracking + AsSplitQuery BE-A10), 3 Migration (C-0004 idempotens DDL DB-04, C-0005 secrets schema DB-05, C-0006 consent withdrawal queue), `OutboxInterceptor` regisztráció (BE-A01 — Phase 3B Kernel re-use).
> - Crypto: `TwoSlotMasterKekProvider`, `KekRewrapBackgroundService` (per-batch scope BE-A02), `PerExecutionKeyVault` (crypto-shred GDPR-1), `SecretZeroization` (SEC-14).
> - Sidecar: `SidecarImageHardeningClient` (IHttpClientFactory named client BE-A04), `SidecarSpkiPinValidator` (SEC-03), `SidecarRequestSigner` (SEC-06).
> - Auth + DI guards: `JwtAlgorithmAllowlist` (SEC-05), `InProcessAdapterAssertion` (SEC-10), `InventoryAdapterConnectionAssertion` (BE-A05).
> - Audit: `SerilogCuttingAuditLogger` + log-rotation (SEC-13).
>
> **Track C (Api + Realtime + Tests):**
> - `SpaceOS.Modules.Cutting.Execution.Api` projekt: Minimal API endpoint-ok (`CuttingExecutionEndpoints`, `HandshakeProgressEndpoints`, `WorkerConsentEndpoints`), `ExecutionHub` SignalR (`IExecutionAccessChecker` SEC-15), `HandshakeRateLimitMiddleware` (DOS-1), `AuditLogMiddleware` (SEC-13), `Program.cs` JWT/CORS/DI setup.
> - 3 OpenAPI snapshot fájl generálás (`Cutting.Execution.openapi.snapshot.json`, `.handshake.openapi.snapshot.json`, `.sidecar.openapi.snapshot.json` BE-A11).
> - Tests: ≥ 95 unit, ≥ 60 integration, ≥ 30 API, ≥ 10 SignalR, ≥ 15 Python sidecar = **≥ 210 új teszt**. Test fixture-ek `app.testing` GUC bypass-szal a DELETE-trigger teszteléséhez.
> - Sidecar (Python): `SpaceOS.Sidecar.ImageHardening` FastAPI service — libmagic MIME (SEC-07), bytes-level header validation, multiprocessing Pillow worker cgroup memory cap (DOS-3), HMAC body-sig validation (SEC-06), mTLS endpoint.
>
> **DoD checklist:** SpaceOS_Modules_Cutting_Phase4_Execution_Architecture_v4.md §9 (9.1–9.9, mind a 9 alszekció).
>
> **Blokkoló gate-ek (deployment blockers):**
> 1. Migration C-0004 + C-0005 + C-0006 idempotens, RLS FORCE all 5 tables (DB-01, DB-03, DB-04, DB-05)
> 2. 12 security gate (GDPR-1..6, KEK-1, ZERO-1, AUTH-1/2, JWT-1, BADGE-1, MIME-1, BATCH-1, REPLAY-1/2/3, DOS-1/2/3, DATA-1/2/3)
> 3. 9 backend gate (OUT-1/2, SCOPE-1/2, REPO-1, HTTP-1, CONN-1/2, QUERY-1)
> 4. ≥ 210 új teszt zöld + meglévő ~4023 platform teszt zöld
> 5. 0 build warning, 0 vulnerable package
>
> **Minden feladat után futtasd:**
> ```
> cd /opt/spaceos/kernel
> dotnet test --filter Category!=E2E
> dotnet build --warnaserror
> dotnet list package --vulnerable
> ```
>
> **Layer-specifikus CLAUDE.md fájlok kötelezőek olvasásra:**
> - `Domain/CLAUDE.md` — DDD invariánsok, Golden Rule 1-3
> - `Application/CLAUDE.md` — CQRS, Result-pattern, ConfigureAwait
> - `Infrastructure/CLAUDE.md` — EF Core, RLS, Specification implementations
> - `Api/CLAUDE.md` — Minimal API conventions, JWT, OpenAPI

### 13.3 Kockázatok és mitigációk

| Kockázat | Valószínűség | Hatás | Mitigáció |
|----------|--------------|-------|-----------|
| Phase 3B Kernel outbox interceptor inkompatibilis Phase 4 aggregate-tel | Alacsony | Magas | Track A nap 1: ellenőrző teszt — meglévő Phase 3 aggregate `Sheet.Submit` event outbox-ba kerül-e? Ha igen, Phase 4 minta működik |
| Sidecar Python deploy bonyolítás (új service-target) | Közepes | Közepes | Track B: systemd unit fájl + healthcheck integráció a meglévő `/opt/spaceos/` deploy-pattern-hez. Runbook ír |
| Két-slot KEK rotation tesztelése komplex (időfüggő háttér-job) | Magas | Közepes | Integration teszt: idő-injektálás `IDateTimeProvider` + manual trigger `IHostedService` start/stop, 60 key re-wrap szimuláció |
| `IOffcutReturnAdapter` shared-transaction Cutting és Inventory között — nem trivial integration teszt | Magas | Magas | Külön `InventoryAdapterIntegrationFixture` xUnit fixture, ugyanazon `Npgsql` connection-pool, kontroll-tesztek |
| `IHttpClientFactory` mTLS handler reuse (BE-A04) — pooled-connection-lifetime cert rotation idejére | Közepes | Közepes | Cert rotation runbook: handler-pool drain (max 5 perc várakozás), aztán új cert load |
| Outbox-pattern lag (post-commit dispatcher polling) — UI fan-out latency | Alacsony | Alacsony | `OutboxDispatcher` polling interval ≤ 1s; SignalR fan-out latency tipikusan 100-300ms acceptable |
| Cross-tenant peer DOS mitigation Redis-ACL config | Alacsony | Magas | Lynis check + load-test 1000 concurrent peer pull → 429 helyesen érvényesül |
| LapMester (vagy másik tenant) lassan migrál a worker-app HMAC-feature-re | Magas | Közepes | Backward-compat: Phase 4 deploy → 30 nap migration window, HMAC validation fokozatosan kötelező lesz; tenant-policy `WorkerHmacRequired` boolean Phase 4-ben default `false` LapMester-nek, default `true` Doorstar-nak |

### 13.4 Roll-back terv

Ha a Phase 4 deploy után 24 órán belül kritikus bug derül ki:

1. **`spaceos_app` connection-string** → DB role szintjén `REVOKE INSERT ON spaceos_cutting."CuttingExecutions"` → új execution létrehozást leállítja, meglévő `RecordProgress` továbbra is megy
2. **OutboxDispatcher** stop → SignalR fan-out megáll, de outbox-rekord nem vész el (Pending status)
3. **API service** rollback előző verzióra (Phase 3-as binár) → DB schema kompatibilis (csak új táblák, meglévő érintetlen)
4. **Migration rollback** csak végső eset: C-0004/0005/0006 down-migration NEM feltétlenül safe (CASCADE delete), `pg_dump` előtte kötelező

---

*SpaceOS · Modules.Cutting Phase 4 Execution v4.0 · Multi-review absorbed · 2026-04-26*
*Státusz: **IMPLEMENTÁCIÓRA KÉSZ** — 26 finding beépítve (0/19/12 — 🔴/🟠/🟡), minden döntés lezárva*
