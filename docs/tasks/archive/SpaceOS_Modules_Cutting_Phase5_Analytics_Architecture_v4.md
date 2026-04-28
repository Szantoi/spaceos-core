# SpaceOS — Modules.Cutting Phase 5: Analytics Architecture
## DailyMetrics + OEE + WasteReport + OperatorPerformance + Deferred-debt closure (P4-4 + P4-9)
## v4 IMPLEMENTÁCIÓRA KÉSZ — full review pipeline complete

> **Verzió:** v4.0 — 2026-04-28
> **Státusz:** **IMPLEMENTÁCIÓRA KÉSZ** — minden review pass abszorbeálva, döntések lezárva, 0 nyitott CRITICAL/HIGH
> **Blokkoló feltétel:** Cutting Phase 4 DEPLOYED
> **Kumulált review:** v1 → DB → v2 → security → v3 → backend → **v4**
> **Precedens:** `SpaceOS_Modules_Cutting_Phase4_Execution_Architecture_v4.md`
> **Repository:** `spaceos-modules-cutting`
> **Port:** `:5005`
> **Schema:** `cutting_analytics`
> **Companion:** `SpaceOS_Modules_Cutting_Phase5_Analytics_Architecture_v4_README.md` (Claude Code agent context)

---

## 1. Kumulált Finding Összesítő (v1 → v4)

| Review | Findings (🔴 / 🟠 / 🟡 / 🟢) | Legfontosabb javítás | Effort delta |
|--------|------------------------------|----------------------|--------------|
| v1 → `database-designer` + `database-schema-designer` → v2 | 1 / 3 / 7 / 5 | DB-01 RLS GDPR fix · DB-02 dedup ledger · DB-03 Day-0 backfill · DB-05 dedikált schema | +5 nap |
| v2 → `senior-security` → v3 | 1 / 4 / 9 / 3 | SEC-01 quasi-identifier inference defense · SEC-02 TPM fallback DISABLED · SEC-03 dedup retention 90d · SEC-04 BYPASSRLS audit · SEC-05 analytics rate limit | +4.1 nap |
| v3 → `senior-backend` → **v4** | 0 / 4 / 7 / 5 | BE-01 EF Core interceptor compliance · BE-02 Outbox-pattern tenant-onboard · BE-03 l-diversity index + cache · BE-04 Phase 4 backward-compat | +3.5 nap |
| **Összesen v1 → v4** | **2 🔴 / 11 🟠 / 23 🟡 / 13 🟢 = 49 finding** | minden CRITICAL és HIGH absorbed; minden v3 nyitott Q lezárva | **~21.1 nap** |

### v4 Finding-részletes táblázat (backend review)

| ID | Súly | Terület | Probléma | v4 javítás |
|----|------|---------|----------|------------|
| **BE-01** | 🟠 | EF Core interceptor bypass | `ProjectionIdempotencyGate` raw ADO.NET (`GetDbConnection().CreateCommand()`) — kikerüli `TenantSessionInterceptor`-t és `OutboxInterceptor`-t; RLS session var nem áll be → cross-tenant write veszély. | `db.Database.ExecuteSqlInterpolatedAsync` használata interceptor pipeline-on át. |
| **BE-02** | 🟠 | MediatR sync vs async (Q13) | `OnTenantActivatedHandler` szinkron MediatR notification — tenant onboarding TX-éhez kötve; ha analytics DB unreachable → onboarding fail. | Outbox-pattern (Phase 4 BE-A01 reuse): `TenantActivatedEvent` Kernel outboxba → `OutboxAnalyticsSubscriber` aszinkron pickup → `TenantActivatedProjector` rebuild job-ot indít. |
| **BE-03** | 🟠 | l-diversity perf (Q11) | 14 600 row aggregation per query, seq scan ~200ms; 60 req/min/tenant → connection pool exhaustion + DB CPU spike. | (a) Expression index `(tenant_id, metric_date, (panels_completed/5)*5, worker_id) WHERE worker_id IS NOT NULL` — 40× speedup. (b) Combined function `assert_anonymity_constraints` — 2 round-trip → 1. (c) Redis cache 5 perc TTL stable range-en. |
| **BE-04** | 🟠 | Backward-compat (Q12) | `RedisSentinelHandshakeRateLimiter` rename (SEC-05) breaking change — Phase 4 deployed env-ben DI service not found → startup crash. | `IHandshakeRateLimiter` interface megmarad, új `RedisSentinelRateLimiter` implementálja mind `IHandshakeRateLimiter`-t és `IRateLimiter`-t. `[Obsolete]` 1-release deprecation. |
| **BE-05** | 🟡 | Pagination missing | 90 napos OEE × N gép × 24 óra = 2160 sor egy lapon. | `PagedSpec<T>` base + Skip/Take, `PagedResult<T>` DTO; default 100, max 500. |
| **BE-06** | 🟡 | OpenAPI Capability flag versioning | Q7: 3 új flag — additive, de régi kliens unknown-flag deserialization failure. | Contract NuGet `v1.4.0` (minor); `LenientFlagsEnumConverter` tolerálja unknown bit-eket; OpenAPI snapshot CI diff. |
| **BE-07** | 🟡 | Idempotency tx boundary | Gate INSERT + projection UPSERT 2 külön tx → "ghost" dedup entry permanens data loss. | Same-transaction: `BeginTransactionAsync` → gate INSERT → projection UPSERT → `Commit`. |
| **BE-08** | 🟡 | AsSplitQuery analytics specs | Dashboard query 5 read-modelből egyesít — cartesian explosion kockázat. | `Query.AsSplitQuery()` minden multi-Include spec-en (Phase 4 BE-A10 reuse); dashboard 5 külön query + app-szintű compose. |
| **BE-09** | 🟡 | DB function konsolidáció | Külön k-anon + l-diversity = 2 round-trip. | `assert_anonymity_constraints(p_from, p_to, p_k, p_l)` — egyetlen function, fail-fast belül. C-0015/C-0016 → deprecated, drop later release. |
| **BE-10** | 🟡 | Result extensions audit | v3 új minták (Conflict/Forbidden/NotFound) HTTP status mapping konzisztencia. | `ResultExtensions.ToActionResult()` (Phase 4 reuse) audit: minden új variant explicit mapping. |
| **BE-11** | 🟡 | Specification naming | Vegyes: `ActiveRebuildJobSpec` vs `DailyExecutionMetricByDateRangeSpec`. | Egységesen `<Entity><Filter>Spec`. |
| **BE-12** | 🟢 | DI registration explicit | Új típusok scope (singleton/scoped/transient) dokumentációja hiányzott. | Section 13-ban explicit DI tábla. |
| **BE-13** | 🟢 | Capability flag bit allocation | bit 0-13 használt; tartalék 18 flag-re. | OK most; jövőbeli `long` vagy kategória-felbontás revízió. |
| **BE-14** | 🟢 | BackgroundService graceful shutdown | Új BG service-ek drain-window. | Phase 4 pattern reuse: 30 sec drain `StopAsync`-ben. |
| **BE-15** | 🟢 | Idempotency-Key header | POST /v1/analytics/rebuild double-POST UX. | Optional `Idempotency-Key` header in-memory + Redis 5 perc cache. |
| **BE-16** | 🟢 | ChunkProgress event | Rebuild progress nem emit-ál → UI nem tud progress bar-t. | `RebuildChunkProgressedEvent` opcionális emit; SignalR fan-out. |

---

## 2. Kontextus és scope

### 2.1 Mit csinál a Cutting Phase 5 (v4 final)

| Képesség | Mit jelent | Felelős |
|----------|------------|---------|
| Daily execution metrics | Per-(tenant, dátum, gép) | `Cutting.Analytics` projection |
| Daily material usage | Per-(tenant, dátum, anyag) | `Cutting.Analytics` projection |
| OEE breakdown | Hourly, gép-szintű | `Cutting.Analytics` projection |
| Operator performance | Per-worker (consent + l-diversity) **vagy** anonimizált (k≥5 + min 7 nap) | `Cutting.Analytics` + `IAnonymizationService` (cached) |
| Waste/Capacity/OEE/Operator/Dashboard API | 5 query endpoint, paginated | `AnalyticsController` |
| Rebuild API | Admin-triggered + onboarding hook (Outbox) | `AnalyticsController` + `TenantActivatedProjector` |
| Day-0 + new-tenant backfill | DB-03 + SEC-12 + BE-02 | `AnalyticsRebuildService` |
| Analytics rate limit | Per-tenant 60 req/min + 5 concurrent | `RedisSentinelRateLimiter` (BE-04 backward-compat) |
| GDPR Art. 30 audit | Personal view hozzáférés rögzítése | `ICuttingAuditLogger` reuse |
| TPM fallback policy | Per-tenant config, prod default DISABLED | `ITenantSecurityPolicy` |
| BYPASSRLS pre-deploy gate | CI gate `pg_roles` ellenőrzés | `PreDeployValidator` |
| **Cached anonymity constraints** (új BE-03) | k-anon + l-diversity Redis cache 5 perc TTL | `CachedAnonymizationService` |

### 2.2 Mit nem csinál (változatlan)

Doorstar Portal UI · ML predictive · Cross-tenant aggregátum · Manufacturing OEE · Inventory waste analytics · külső BI integráció (csak előkészítve).

### 2.3 Architektúra alapaxiómák (v4 final)

| ID | Axióma | Kötelezettség |
|----|--------|----------------|
| A5-1 .. A5-14 | (változatlan v3) | — |
| **A5-15** (új BE-01) | **Read-model write csak EF Core interceptor pipeline-on át** | Raw ADO.NET tilos; `ExecuteSqlInterpolatedAsync` vagy `SaveChangesAsync` |
| **A5-16** (új BE-02) | **Cross-bounded-context async event-en át** | Sync MediatR notification cross-context tilos; Outbox-pattern kötelező |
| **A5-17** (új BE-04) | **Phase 4 public API backward-compat** | Interface rename helyett adapter; `[Obsolete]` 1-release cycle |

---

## 3. Domain modell — v4 final

### 3.1 Solution struktúra (v4 új/módosított fájlok BE finding-ek miatt)

```
SpaceOS.Modules.Cutting.Analytics/
├── Domain/
│   ├── ReadModels/                                     (változatlan v3-hoz)
│   ├── Aggregates/                                     (változatlan v3-hoz)
│   ├── ValueObjects/                                   (változatlan v3-hoz)
│   ├── Specifications/                                 ⟵ átnevezve BE-11 szerint
│   │   ├── DailyExecutionMetricByDateRangeSpec.cs
│   │   ├── DailyMaterialUsageByDateRangeSpec.cs
│   │   ├── DailyOperatorMetricByDateRangeSpec.cs
│   │   ├── DailyOperatorMetricAnonymizedSpec.cs
│   │   ├── MachineOEEHourlyByDateRangeSpec.cs
│   │   ├── AnalyticsRebuildJobActiveSpec.cs            ⟵ átnevezve (volt: ActiveRebuildJobSpec)
│   │   ├── AnalyticsRebuildJobByStatusSpec.cs          ⟵ átnevezve (volt: PendingRebuildJobsSpec)
│   │   └── ProcessedOutboxEventByEventIdSpec.cs
│   └── Common/
│       ├── PagedSpec.cs                                ⟵ ÚJ (BE-05) base
│       └── PagedResult.cs                              ⟵ ÚJ (BE-05) DTO
├── Application/
│   ├── Projections/
│   │   ├── ProjectionIdempotencyGate.cs                ⟵ BE-01 (ExecuteSqlInterpolated) + BE-07 (same-tx)
│   │   ├── TenantActivatedProjector.cs                 ⟵ BE-02 (Outbox-pattern, replaces v3 OnTenantActivatedHandler)
│   │   └── ... (5 projector változatlan)
│   ├── Services/
│   │   ├── AnonymizationService.cs                     ⟵ BE-03 (combined function)
│   │   └── CachedAnonymizationService.cs               ⟵ ÚJ (BE-03 Redis cache)
│   ├── Common/
│   │   ├── ResultExtensions.cs                         ⟵ BE-10 audit (Phase 4 reuse)
│   │   └── LenientFlagsEnumConverter.cs                ⟵ ÚJ (BE-06)
│   └── ... (változatlan)
├── Infrastructure/
│   ├── Migrations/
│   │   ├── ... (C-0007 .. C-0018 v3-ből)
│   │   └── YYYYMMDD_C-0019_CombinedAnonymityFunction.cs   ⟵ ÚJ (BE-09)
│   └── Tools/
│       └── PreDeployValidator.cs                       (változatlan v3-hoz)
│
└── SpaceOS.Modules.Cutting.Execution.Infrastructure/  (BE-04 backward-compat módosítás)
    └── Redis/
        ├── IRateLimiter.cs                             ⟵ ÚJ general interface
        ├── IHandshakeRateLimiter.cs                    ⟵ MEGTARTVA (Phase 4 backward-compat)
        ├── RedisSentinelRateLimiter.cs                 ⟵ implementálja mind a 2 interface-t
        └── RedisSentinelHandshakeRateLimiter.cs        ⟵ [Obsolete] 1-release deprecation, adapter
```

### 3.2 ProjectionIdempotencyGate — BE-01 + BE-07 frissítés

```csharp
// Application/Projections/ProjectionIdempotencyGate.cs (v4 final)
public sealed class ProjectionIdempotencyGate(
    CuttingAnalyticsDbContext db,
    IDateTimeProvider clock,
    ILogger<ProjectionIdempotencyGate> logger)
{
    /// <summary>
    /// BE-01: EF Core interceptor pipeline-on át megy (TenantSessionInterceptor + audit).
    /// BE-07: caller felel a transaction boundary-ért — gate + projection same-tx.
    /// Returns true if event was already processed by this subscriber.
    /// </summary>
    public async Task<bool> IsAlreadyProcessedAsync(
        Guid eventId, string subscriberName, Guid tenantId, CancellationToken ct)
    {
        var rowsAffected = await db.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO cutting_analytics.processed_outbox_event
                (event_id, subscriber_name, tenant_id, created_at)
            VALUES ({eventId}, {subscriberName}, {tenantId}, {clock.UtcNow})
            ON CONFLICT (event_id, subscriber_name) DO NOTHING
        ", ct).ConfigureAwait(false);

        // BE-01: ExecuteSqlInterpolatedAsync goes through interceptor pipeline
        // BE-07: caller wraps in transaction with projection logic
        var alreadyProcessed = rowsAffected == 0;

        if (alreadyProcessed)
            logger.LogInformation(
                "Skipping duplicate event {EventId} for subscriber {Subscriber}",
                eventId, subscriberName);

        return alreadyProcessed;
    }
}

// Application/Projections/OffcutReportedProjector.cs (v4 — BE-07 same-tx)
public async Task ProjectAsync(OffcutReportedEvent evt, CancellationToken ct)
{
    // BE-07: same-transaction gate + UPSERT
    await using var tx = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
    try
    {
        if (await gate.IsAlreadyProcessedAsync(
                evt.EventId, nameof(OffcutReportedProjector), evt.TenantId, ct)
            .ConfigureAwait(false))
        {
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            return;
        }

        // UPSERT logic (változatlan v3-hoz)
        await db.Database.ExecuteSqlInterpolatedAsync($@" ... ", ct)
            .ConfigureAwait(false);

        await tx.CommitAsync(ct).ConfigureAwait(false);
    }
    catch
    {
        await tx.RollbackAsync(ct).ConfigureAwait(false);
        throw;
    }
}
```

### 3.3 TenantActivatedProjector — BE-02 (Q13 lezárás)

```csharp
// Application/Projections/TenantActivatedProjector.cs (új, BE-02)
// Phase 4 BE-A01 Outbox-pattern reuse:
// Kernel: TenantActivatedEvent → OutboxMessage
// Cutting.Analytics: OutboxAnalyticsSubscriber → TenantActivatedProjector
public sealed class TenantActivatedProjector(
    CuttingAnalyticsDbContext db,
    IAnalyticsRebuildJobRepository repo,
    ProjectionIdempotencyGate gate,
    IDateTimeProvider clock,
    ILogger<TenantActivatedProjector> logger) : IProjector<TenantActivatedEvent>
{
    public async Task ProjectAsync(TenantActivatedEvent evt, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        try
        {
            if (await gate.IsAlreadyProcessedAsync(
                    evt.EventId, nameof(TenantActivatedProjector), evt.TenantId, ct)
                .ConfigureAwait(false))
            {
                await tx.RollbackAsync(ct).ConfigureAwait(false);
                return;
            }

            // Auto-trigger Full rebuild for newly activated tenant
            var job = AnalyticsRebuildJob.CreateFull(evt.TenantId, clock);
            await repo.AddAsync(job, ct).ConfigureAwait(false);

            await tx.CommitAsync(ct).ConfigureAwait(false);

            logger.LogInformation(
                "Auto-triggered Full rebuild for newly activated tenant {TenantId}",
                evt.TenantId);
        }
        catch
        {
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }
    }
}

// OutboxAnalyticsSubscriber subscription update (BE-02)
private static readonly IReadOnlyCollection<string> SubscribedEventTypes =
[
    "ExecutionStarted", "ProgressRecorded", "OffcutReported",
    "ExecutionCompleted", "ExecutionCancelled", "WorkerConsentWithdrawn",
    "TenantActivated"   // ⟵ BE-02 új
];
```

### 3.4 CachedAnonymizationService — BE-03

```csharp
// Application/Services/AnonymizationService.cs (BE-03 + BE-09 update)
public interface IAnonymizationService
{
    Task<AnonymizationDecision> EvaluateAsync(
        Guid tenantId, IReadOnlyCollection<Guid> requestedWorkerIds,
        AnalyticsView requestedView, KAnonymityThreshold threshold,
        CancellationToken ct);

    /// <summary>BE-09: combined k-anon + l-diversity in single round-trip.</summary>
    Task EnforceAnonymityConstraintsAsync(
        Guid tenantId, DateRange range,
        int k = 5, int l = 2, CancellationToken ct = default);
}

public sealed class AnonymizationService(CuttingAnalyticsDbContext db) : IAnonymizationService
{
    public async Task EnforceAnonymityConstraintsAsync(
        Guid tenantId, DateRange range, int k = 5, int l = 2, CancellationToken ct = default)
    {
        // BE-09: single function call (combined check)
        await db.Database.ExecuteSqlInterpolatedAsync($@"
            SELECT cutting_analytics.assert_anonymity_constraints(
                {range.From}, {range.To}, {k}, {l})
        ", ct).ConfigureAwait(false);
        // PostgresException 42501 propagates if constraint violated
    }

    // ... EvaluateAsync (változatlan)
}

// Application/Services/CachedAnonymizationService.cs (új, BE-03)
public sealed class CachedAnonymizationService(
    IAnonymizationService inner,
    IDistributedCache cache,
    ILogger<CachedAnonymizationService> logger) : IAnonymizationService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public Task<AnonymizationDecision> EvaluateAsync(
        Guid tenantId, IReadOnlyCollection<Guid> requestedWorkerIds,
        AnalyticsView requestedView, KAnonymityThreshold threshold,
        CancellationToken ct)
        => inner.EvaluateAsync(tenantId, requestedWorkerIds, requestedView, threshold, ct);

    public async Task EnforceAnonymityConstraintsAsync(
        Guid tenantId, DateRange range, int k = 5, int l = 2, CancellationToken ct = default)
    {
        var cacheKey = $"anon:{tenantId}:{range.From:yyyyMMdd}-{range.To:yyyyMMdd}:k{k}l{l}";

        var cached = await cache.GetStringAsync(cacheKey, ct).ConfigureAwait(false);
        if (cached == "OK")
            return;     // Cache hit — already validated within TTL

        await inner.EnforceAnonymityConstraintsAsync(tenantId, range, k, l, ct)
            .ConfigureAwait(false);

        await cache.SetStringAsync(cacheKey, "OK",
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl }, ct)
            .ConfigureAwait(false);
    }
}
```

### 3.5 RedisSentinelRateLimiter — BE-04 backward-compat

```csharp
// Execution.Infrastructure.Redis/IRateLimiter.cs (új general interface)
public interface IRateLimiter
{
    Task<RateLimitResult> CheckAsync(
        string bucket, Guid tenantId, RateLimitPolicy policy, CancellationToken ct);
}

// Execution.Infrastructure.Redis/IHandshakeRateLimiter.cs (Phase 4 — MEGTARTVA)
public interface IHandshakeRateLimiter
{
    Task<RateLimitResult> CheckAsync(string key, CancellationToken ct);
}

// Execution.Infrastructure.Redis/RedisSentinelRateLimiter.cs (BE-04 — implements both)
public sealed class RedisSentinelRateLimiter(
    IConnectionMultiplexer redis,
    ILogger<RedisSentinelRateLimiter> logger)
    : IRateLimiter, IHandshakeRateLimiter
{
    // General implementation
    public async Task<RateLimitResult> CheckAsync(
        string bucket, Guid tenantId, RateLimitPolicy policy, CancellationToken ct)
    {
        var key = $"rl:{bucket}:{tenantId:N}";
        // ... sliding window in Redis ...
    }

    // Phase 4 IHandshakeRateLimiter — adapter
    public Task<RateLimitResult> CheckAsync(string key, CancellationToken ct)
    {
        // Phase 4 backward-compat: handshake bucket implicit
        return CheckAsync(
            bucket: "handshake",
            tenantId: ExtractTenantFromKey(key),
            policy: HandshakeRateLimits.Default,
            ct: ct);
    }

    private static Guid ExtractTenantFromKey(string key) =>
        Guid.TryParse(key.Split(':').Last(), out var id) ? id : Guid.Empty;
}

// Execution.Infrastructure.Redis/RedisSentinelHandshakeRateLimiter.cs (deprecated wrapper)
[Obsolete("Use RedisSentinelRateLimiter directly. Will be removed in v1.5.0.")]
public sealed class RedisSentinelHandshakeRateLimiter(IRateLimiter inner) : IHandshakeRateLimiter
{
    public Task<RateLimitResult> CheckAsync(string key, CancellationToken ct)
    {
        if (inner is IHandshakeRateLimiter handshake)
            return handshake.CheckAsync(key, ct);
        throw new InvalidOperationException("Inner limiter does not support handshake interface");
    }
}

// DI registration (Program.cs / Startup)
services.AddSingleton<RedisSentinelRateLimiter>();
services.AddSingleton<IRateLimiter>(sp => sp.GetRequiredService<RedisSentinelRateLimiter>());
services.AddSingleton<IHandshakeRateLimiter>(sp => sp.GetRequiredService<RedisSentinelRateLimiter>());
// Phase 4 deployed config: IHandshakeRateLimiter binding továbbra is működik
```

### 3.6 PagedSpec base + PagedResult — BE-05

```csharp
// Domain/Common/PagedSpec.cs (új, BE-05)
public abstract class PagedSpec<T> : Specification<T>
{
    protected PagedSpec(int pageNumber, int pageSize)
    {
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
        if (pageSize < 1 || pageSize > 500)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "1..500");

        Query.Skip((pageNumber - 1) * pageSize).Take(pageSize);
    }
}

// Domain/Common/PagedResult.cs (új, BE-05)
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;
}

// Application/Queries/GetOEEReport/GetOEEReportQueryHandler.cs (példa BE-05)
public async Task<Result<PagedResult<MachineOEEHourlyDto>>> Handle(
    GetOEEReportQuery request, CancellationToken ct)
{
    var spec = new MachineOEEHourlyByDateRangeSpec(
        _tenantContext.TenantId,
        new DateRange(request.From, request.To),
        request.MachineId,
        request.PageNumber,
        request.PageSize);

    var totalCount = await repo.CountAsync(spec, ct).ConfigureAwait(false);
    var items = await repo.ListAsync(spec, ct).ConfigureAwait(false);

    return Result.Success(new PagedResult<MachineOEEHourlyDto>(
        items.Select(MachineOEEHourlyDto.FromReadModel).ToList(),
        request.PageNumber, request.PageSize, totalCount));
}
```

---

## 4. DB schema — v4 új migration

### 4.1 Migration C-0019 — Combined anonymity function (új, BE-09)

```sql
-- BE-09: combined k-anon + l-diversity in single function
CREATE OR REPLACE FUNCTION cutting_analytics.assert_anonymity_constraints(
    p_from DATE,
    p_to DATE,
    p_k INTEGER DEFAULT 5,
    p_l INTEGER DEFAULT 2
)
RETURNS BOOLEAN
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = cutting_analytics, pg_temp
AS $$
DECLARE
    v_tenant_id UUID;
    v_distinct_workers INTEGER;
    v_min_diversity INTEGER;
BEGIN
    v_tenant_id := current_setting('app.tenant_id', false)::uuid;

    -- k-anonymity check (changed from C-0015 to support custom k)
    SELECT COUNT(DISTINCT worker_id) INTO v_distinct_workers
    FROM cutting_analytics.daily_operator_metric
    WHERE tenant_id = v_tenant_id
      AND metric_date BETWEEN p_from AND p_to
      AND worker_id IS NOT NULL;

    IF v_distinct_workers < p_k THEN
        RAISE NOTICE 'k-anon violation: tenant=% distinct=% k=%',
                     v_tenant_id, v_distinct_workers, p_k;
        RAISE EXCEPTION 'anonymity check failed' USING ERRCODE = 'insufficient_privilege';
    END IF;

    -- l-diversity check (covered by expression index below)
    WITH bucketed AS (
        SELECT
            metric_date,
            (panels_completed / 5) * 5 AS panel_bucket,
            COUNT(DISTINCT worker_id) AS distinct_workers_in_cell
        FROM cutting_analytics.daily_operator_metric
        WHERE tenant_id = v_tenant_id
          AND metric_date BETWEEN p_from AND p_to
          AND worker_id IS NOT NULL
        GROUP BY metric_date, (panels_completed / 5) * 5
    )
    SELECT MIN(distinct_workers_in_cell) INTO v_min_diversity FROM bucketed;

    IF v_min_diversity IS NULL OR v_min_diversity < p_l THEN
        RAISE NOTICE 'l-diversity violation: tenant=% min=% l=%',
                     v_tenant_id, v_min_diversity, p_l;
        RAISE EXCEPTION 'anonymity check failed' USING ERRCODE = 'insufficient_privilege';
    END IF;

    RETURN TRUE;
END $$;

REVOKE ALL ON FUNCTION cutting_analytics.assert_anonymity_constraints(DATE, DATE, INTEGER, INTEGER) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION cutting_analytics.assert_anonymity_constraints(DATE, DATE, INTEGER, INTEGER) TO cutting_app;

-- BE-03 (a): expression index covering l-diversity GROUP BY hot path
CREATE INDEX IF NOT EXISTS idx_dom_l_diversity_cover
    ON cutting_analytics.daily_operator_metric (
        tenant_id,
        metric_date,
        ((panels_completed / 5) * 5),
        worker_id
    )
    WHERE worker_id IS NOT NULL;

-- C-0015 (assert_k_anonymity) and C-0016 (assert_l_diversity) marked deprecated
-- DO NOT DROP yet — gradual migration; drop in next phase release
COMMENT ON FUNCTION cutting_analytics.assert_k_anonymity IS
    'DEPRECATED: use assert_anonymity_constraints. Will be dropped in Phase 6.';
COMMENT ON FUNCTION cutting_analytics.assert_l_diversity IS
    'DEPRECATED: use assert_anonymity_constraints. Will be dropped in Phase 6.';
```

---

## 5. API surface — v4 final

### 5.1 ICuttingProvider extension (változatlan v3)

3 új metódus + Capability flag bit 11/12/13. Contracts NuGet **v1.4.0**.

### 5.2 REST endpoints (v4 — pagination BE-05)

| Method | Path | Auth | Capability | Pagination | Returns |
|--------|------|------|------------|------------|---------|
| GET | `/v1/analytics/waste?from&to&materialCode&page&pageSize` | JWT | CuttingWaste | ✅ | `PagedResult<WasteReportRowDto>` |
| GET | `/v1/analytics/capacity?from&to&machineId&page&pageSize` | JWT (Admin) | CuttingCapacity | ✅ | `PagedResult<CapacityRowDto>` |
| GET | `/v1/analytics/oee?from&to&machineId&page&pageSize` | JWT (Admin) | CuttingOEE | ✅ | `PagedResult<OEERowDto>` |
| GET | `/v1/analytics/operators?from&to&view&page&pageSize` | JWT (Admin) | CuttingOperatorMetrics | ✅ | `PagedResult<OperatorMetricRowDto>` |
| GET | `/v1/analytics/dashboard?date` | JWT | composite | n/a (single-day) | `DashboardSnapshotDto` |
| POST | `/v1/analytics/rebuild` | JWT (Admin) + `Idempotency-Key` (BE-15) | — | n/a | `202 Accepted` + jobId |
| GET | `/v1/analytics/rebuild/{id}` | JWT (Admin) | — | n/a | rebuild status + chunk progress |

### 5.3 LenientFlagsEnumConverter — BE-06

```csharp
// Application/Common/LenientFlagsEnumConverter.cs (új, BE-06)
public sealed class LenientFlagsEnumConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert,
                                JsonSerializerOptions options)
    {
        var raw = reader.GetInt64();
        var allValidBits = Enum.GetValues<TEnum>()
            .Cast<long>()
            .Aggregate(0L, (acc, v) => acc | v);

        // Mask out unknown bits — tolerant parse (BE-06)
        var maskedRaw = raw & allValidBits;
        return (TEnum)Enum.ToObject(typeof(TEnum), maskedRaw);
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
        => writer.WriteNumberValue(Convert.ToInt64(value));
}

// Contracts/ProviderCapability.cs (BE-06)
[Flags]
[JsonConverter(typeof(LenientFlagsEnumConverter<ProviderCapability>))]
public enum ProviderCapability
{
    None = 0,
    CuttingSubmit       = 1 << 0,
    // ... existing ...
    CuttingCapacity         = 1 << 11,
    CuttingOEE              = 1 << 12,
    CuttingOperatorMetrics  = 1 << 13,
}
```

---

## 6. EF Core konfiguráció (v4 final — BE-08 AsSplitQuery)

```csharp
// Infrastructure/Persistence/Repositories/DailyOperatorMetricRepository.cs (BE-08)
public sealed class DailyOperatorMetricRepository(CuttingAnalyticsDbContext db)
    : IDailyOperatorMetricRepository
{
    public async Task<IReadOnlyList<DailyOperatorMetric>> ListAsync(
        ISpecification<DailyOperatorMetric> spec, CancellationToken ct)
    {
        // BE-08: AsSplitQuery for multi-include scenarios
        return await SpecificationEvaluator.Default
            .GetQuery(db.DailyOperatorMetrics.AsNoTracking().AsSplitQuery(), spec)
            .ToListAsync(ct).ConfigureAwait(false);
    }
}
```

---

## 7. Projection logika — v4 final (BE-01, BE-02, BE-07 absorbed)

A v3 §7 alapszerkezet érvényes, kiegészítve:
- **BE-01:** `ExecuteSqlInterpolatedAsync` minden raw SQL-ben (interceptor pipeline)
- **BE-07:** same-transaction gate + UPSERT minden projector-ben
- **BE-02:** `TenantActivatedProjector` outbox-pattern-ben (replaces v3 OnTenantActivatedHandler)

---

## 8. Phase 4 deferred debt — v4 final (BE-04 backward-compat)

A v3 §8 érvényes, kiegészítve:
- **BE-04:** `RedisSentinelRateLimiter` implements both `IRateLimiter` és `IHandshakeRateLimiter`
- `RedisSentinelHandshakeRateLimiter` `[Obsolete]` deprecation 1-release cycle
- DI: két binding ugyanarra az instance-ra (Phase 4 deployed config nem törik)

---

## 9. Definition of Done (v4 final)

### Migration gates

- [ ] C-0007 .. C-0019 idempotens (`IF NOT EXISTS` + DO blocks)
- [ ] **BE-09 PROOF**: `assert_anonymity_constraints` deployed; C-0015 + C-0016 `[DEPRECATED]` comment-tel
- [ ] **BE-03 PROOF**: `idx_dom_l_diversity_cover` index létezik; EXPLAIN ANALYZE l-diversity query → Index Scan (nem Seq Scan)
- [ ] DB-01..DB-16 + SEC-04 PRE-DEPLOY GATE proof-ok (v3-ból változatlan)

### Domain gates

- [ ] **BE-11**: minden Specification átnevezve `<Entity><Filter>Spec` konvencióra
- [ ] **BE-05**: `PagedSpec<T>` base + `PagedResult<T>` DTO + minden read-model query Skip/Take-en megy
- [ ] DB-04 + DB-08 + SEC-01 + SEC-14 (v3-ból változatlan)
- [ ] Read-model entity-knek nincs public setter
- [ ] Golden Rules 1–12

### Projection gates

- [ ] **BE-01 PROOF**: `grep -r "GetDbConnection\(\)\.CreateCommand" --include="*.cs"` → 0 találat
- [ ] **BE-01 PROOF**: integration test — `ProjectionIdempotencyGate` írás során `app.tenant_id` PostgreSQL session var korrekten beáll
- [ ] **BE-02 PROOF**: integration test — `TenantActivatedEvent` Kernel outboxba kerül; `TenantActivatedProjector` aszinkron pickup-pal hozza létre a rebuild job-ot; tenant onboarding TX nem várja meg
- [ ] **BE-07 PROOF**: integration test — projection közbeni hiba → dedup ledger sem ír (rollback együtt)
- [ ] DB-02 dedup proof + UPSERT pattern + 7 event type subscription (v3 6 + BE-02 új TenantActivated)

### API + validation gates

- [ ] FluentValidation 5 query + 1 command
- [ ] **BE-05**: minden paginated endpoint `PageNumber`, `PageSize` query paramétert validál (1..500)
- [ ] **BE-06**: `LenientFlagsEnumConverter` Capability flag JSON deserialization-ön
- [ ] **BE-06**: OpenAPI snapshot CI gate — additive változások engedélyezettek, módosítás meglévőn → fail
- [ ] **BE-10**: `ResultExtensions.ToActionResult()` audit — Conflict/Forbidden/NotFound/Invalid HTTP status mapping konzisztens
- [ ] **BE-15**: `Idempotency-Key` header optional support POST /v1/analytics/rebuild-en
- [ ] SEC-01 + SEC-05 + SEC-06 + DB-10 (v3-ból változatlan)

### Performance gates (v4 új)

- [ ] **BE-03 PROOF**: load test — 60 req/min/tenant operator query × 10 tenant → DB CPU < 50%, p99 latency < 200ms
- [ ] **BE-03 PROOF**: cache hit ratio (`CachedAnonymizationService`) > 80% steady-state
- [ ] **BE-08 PROOF**: dashboard query EXPLAIN ANALYZE — 5 separate query, no Cartesian explosion
- [ ] **BE-14**: BackgroundService graceful shutdown — `StopAsync(ct)` 30 sec drain window respected; in-flight projection befejezhető

### Backward-compat gates (v4 új)

- [ ] **BE-04 PROOF**: Phase 4 deployed config (`services.AddSingleton<IHandshakeRateLimiter, RedisSentinelHandshakeRateLimiter>()`) startup-on átmegy; rate-limit funkcionális teszt PASS
- [ ] **BE-04 PROOF**: `[Obsolete]` warning a `RedisSentinelHandshakeRateLimiter`-ben; build warning lista tartalmazza, nem error

### Security gates (v3-ból változatlan)

SEC-01..SEC-17 proof-ok.

### Deployment gates (v3-ból változatlan)

DB-03 Day-0 backfill + new tenant hook (most BE-02 outbox-pattern-ben).

### Schema versioning (v3-ból változatlan)

SEC-15.

### Összesített

- [ ] Meglévő ~1873 teszt zöld
- [ ] Cutting Phase 5 új tesztek: ≥ **115 db** (v3: 97 → v4: +18)
  - 20 domain teszt
  - 15 projector + idempotency
  - 10 query handler
  - 10 API integration
  - 5 anonymization service (k-anon + l-diversity + cache)
  - 5 P4-4/P4-9
  - 5 DB-01 RLS pen-test · 5 DB-10 k-anon · 5 DB-03 backfill
  - 5 SEC-01 inference · 3 SEC-02 TPM · 2 SEC-04 BYPASSRLS · 3 SEC-05 rate-limit · 2 SEC-06 audit · 2 SEC-12 onboarding
  - **+5 BE-01 EF Core interceptor pipeline** (v4 új)
  - **+3 BE-02 Outbox tenant-onboard async** (v4 új)
  - **+3 BE-03 perf load test + cache hit** (v4 új)
  - **+3 BE-04 backward-compat smoke** (v4 új)
  - **+2 BE-05 pagination boundary** (v4 új)
  - **+2 BE-07 same-transaction rollback** (v4 új)
- [ ] 0 build warning (kivéve `BE-04` `[Obsolete]` deprecation warning explicit accepted list-ben)
- [ ] `ConfigureAwait(false)` mindenhol · `AsNoTracking()` mindenhol · `AsSplitQuery()` multi-include spec-eken
- [ ] `dotnet list package --vulnerable` → 0 high/critical
- [ ] `grep -r "BuildServiceProvider" --include="*.cs"` → 0 találat
- [ ] `grep -r "GetDbConnection\(\)\.CreateCommand" --include="*.cs"` → 0 találat (BE-01)
- [ ] EXPLAIN ANALYZE: Index Scan minden új query endpoint-on (BE-03 expression index covering)
- [ ] Migration `suppressTransaction: true` ahol szükséges (jelenleg nincs ilyen)
- [ ] CLAUDE.md frissítve

---

## 10. Security adósság státusz (v4 final)

| ID | Tétel | Phase 5 v4 | Marad |
|----|-------|-----------|-------|
| P4-4 | Redis Sentinel | ✅ + generalizált rate-limiter (SEC-05) + backward-compat (BE-04) | — |
| P4-9 | TPM enrollment | ✅ tpm2-pkcs11 + per-tenant policy (SEC-02) + monitoring (SEC-09) | — |
| P5-A1 | k-anon inference | ✅ DateRange.MinDays + l-diversity (SEC-01) + index + cache (BE-03) | residual LOW dokumentált |
| P5-A2 | Projection idempotency | ✅ DB-02 + same-tx (BE-07) + EF Core interceptor compliance (BE-01) | — |
| P5-A3 | TPM fallback downgrade | ✅ SEC-02 production DISABLED | — |
| P5-A4 | RLS bypass via reader | ✅ DB-01 VIEW-only + SEC-04 BYPASSRLS audit + SEC-10 security_barrier | — |
| P5-A5 | dedup ledger DoS | ✅ SEC-03 90d retention | — |
| P5-A6 | analytics endpoint DoS | ✅ SEC-05 rate-limit + BE-05 pagination | — |
| P5-A7 | GDPR Art. 30 compliance | ✅ SEC-06 audit log | — |
| P5-A8 | covert channel consent toggle | ✅ SEC-11 rate-limit | — |
| **P5-A9** (új v4) | sync MediatR cross-context coupling | ✅ BE-02 Outbox-pattern | — |
| **P5-A10** (új v4) | Phase 4 backward-compat törés | ✅ BE-04 adapter pattern | — |

**Összegzés:** 0 nyitott CRITICAL, 0 nyitott HIGH, **minden döntés lezárva**.

---

## 11. Threat model (v4 final — 14 vektor változatlan, BE-01/BE-07 erősíti vektor #4-et)

A v3 §11 14 vektor érvényes, **#4 vektort** (Projection drift) BE-01 (interceptor compliance) + BE-07 (same-tx) tovább erősíti — most nemcsak dedup védelem van, hanem RLS session var és audit log is konzisztens.

---

## 12. Mi jön utána (roadmap)

| Phase | Tartalom | Prerequisite | Effort |
|-------|----------|--------------|--------|
| **Phase 5 implementáció** | Claude Code agent (lásd Section 13) | DoD complete | ~21.1 nap |
| **Phase 5 DEPLOYED** | Day-0 backfill + 9 endpoint élben + Doorstar admin curl-szintű elérés | Phase 5 production gate | — |
| **Doorstar Portal Analytics Dashboard sprint** | UI sprint a 9 endpoint fölé (külön repo) | Phase 5 DEPLOYED | ~5-7 nap |
| **Cutting Phase 6: Adapters** | OptiCut + külső nesting adapter; C-0015/C-0016 deprecated function drop | Phase 5 DEPLOYED | ~5-8 nap |
| **Modules.AnalyticsML** (későbbi) | Predictive maintenance, OEE-trend forecasting | Phase 5 DEPLOYED + ≥3 hónap historikus adat | ~10-15 nap |

---

## 13. Claude Code implementációs csomag

### 13.1 Végrehajtási sorrend

A Phase 5 ~21.1 napra van skálázva. **3 párhuzamos track**, 4 fejlesztő (vagy 4 párhuzamos Claude Code agent-session) kapacitásra optimalizálva.

| Nap | Track A: Domain + Application | Track B: Infrastructure + Persistence | Track C: API + BackgroundService + Tests |
|-----|-------------------------------|---------------------------------------|------------------------------------------|
| 1 | Domain VO-k (`DateRange`, `KAnonymityThreshold`, `OEEBreakdown`, `WastePercentage`, `CapacityUtilization`, `ChunkProgress`, `PagedSpec`, `PagedResult`) | C-0007 (schema + roles) + C-0008 (DailyExecutionMetric) migration scaffold | xUnit v3 test project setup, fixtures (`CuttingAnalyticsDbContextFixture`) |
| 2 | Read-model entity-k (5 db) + factory-k + Apply* metódusok | C-0009 + C-0010 + C-0011 (DailyMaterialUsage, DailyOperatorMetric, MachineOEEHourly) | Read-model entity unit tests (lifecycle, factory) |
| 3 | `AnalyticsRebuildJob` aggregate + chunk-progress + state machine | C-0012 + C-0013 (rebuild_job + processed_outbox_event) | Aggregate state-machine unit tests |
| 4 | 8 Specification (`Ardalis.Specification`) BE-11 naming + PagedSpec | C-0014 (anonymized VIEW security_barrier) + C-0015/C-0016 (deprecated comment) | Specification unit tests + paging boundary |
| 5 | `IAnonymizationService` + `AnonymizationService` + BE-09 combined function | C-0019 (combined function + expression index BE-03) + C-0017 (dedup retention 90d SEC-03) | Anonymization service unit tests |
| 6 | `CachedAnonymizationService` (BE-03 Redis cache) + `IDistributedCache` integráció | C-0018 (rebuild per-tenant unique SEC-07) + idempotens DDL re-run gate teszt | Cache hit/miss integration test |
| 7 | `ProjectionIdempotencyGate` BE-01 (`ExecuteSqlInterpolatedAsync`) + BE-07 same-tx wrapper | `CuttingAnalyticsDbContext` + `TenantSessionInterceptor` (Phase 4 reuse) | Gate integration test (interceptor pipeline + same-tx rollback) |
| 8 | 6 Projector (`ExecutionStarted`, `ProgressRecorded`, `OffcutReported`, `ExecutionCompleted`, `ExecutionCancelled`, `ConsentWithdrawn`) | EF Core configurations (5 read-model + 2 ledger) | Projector unit + integration tests (UPSERT idempotency) |
| 9 | `TenantActivatedProjector` (BE-02 outbox-pattern) + `OutboxAnalyticsSubscriber` 7 event type | `OutboxInterceptor` regisztráció (Phase 4 BE-A01 reuse) | TenantActivated outbox integration test |
| 10 | 5 Query + handler + validator (Waste/Capacity/OEE/Operator/Dashboard) — pagination BE-05 | Repository implementations + `AsSplitQuery` BE-08 | Query handler tests (pagination + spec) |
| 11 | `TriggerAnalyticsRebuildCommand` + handler + `Idempotency-Key` middleware (BE-15) | `AnalyticsRebuildService` BackgroundService — chunkolás (DB-08) | Rebuild service integration test (chunk progress) |
| 12 | FluentValidation 6 commands/queries (DateRange.MinDays SEC-01) | `IRateLimiter` + `RedisSentinelRateLimiter` (BE-04 backward-compat both interfaces) | Rate-limit load test (SEC-05) |
| 13 | `AnalyticsController` 7 endpoint + Capability flag check + `Result.ToActionResult` (BE-10) | `LenientFlagsEnumConverter` (BE-06) + Contracts NuGet v1.4.0 build | API integration tests (endpoints + rate-limit + capability) |
| 14 | `IAnalyticsAuditLogger` (SEC-06) + `OperatorMetricsAccessed` event | OpenAPI snapshot generálás + diff CI gate (BE-06) | GDPR Art. 30 audit integration test |
| 15 | `ConsentChangeRateLimiter` (SEC-11) + Phase 4 `IWorkerSecurityPolicy` extension | `TpmFallbackPolicy` v3 update (SEC-02 per-tenant) + `TpmAvailabilityMetricsCollector` (SEC-09) | Consent rate-limit + TPM policy tests |
| 16 | `PreDeployValidator` (SEC-04 BYPASSRLS audit) | Phase 4 Redis ACL config update (SEC-05 bucket separation) | Pre-deploy gate integration test |
| 17 | Cross-tenant pen-test fixtures (DB-01 VIEW-only) + RLS test `app_user_test` role | Phase 4 `RedisSentinelHandshakeRateLimiter` `[Obsolete]` wrapper (BE-04) | Pen-test suite (DB-01 + SEC-01 + SEC-04) |
| 18 | `RebuildChunkProgressedEvent` (BE-16) + SignalR fan-out optional | EXPLAIN ANALYZE script — minden query endpoint (DoD gate) | E2E test: deploy → backfill → query → result |
| 19 | Day-0 backfill orchestration script (DB-03) | pg_cron job: dedup retention + jobs cleanup | Backfill smoke test (10 napos historikus adat) |
| 20 | Documentation: schema-version evolution README (SEC-15) + DI registration táblázat (BE-12) | Lynis scan, vulnerable package check, BackgroundService graceful shutdown teszt (BE-14) | DoD checklist verify + final pen-test sweep |
| 21 | Companion README generation (Claude Code agent context) + post-merge cleanup | Production runbook: KEK rotation + TPM enrollment + analytics dashboard tuning | Smoke test full pipeline + handoff |

### 13.2 Agent utasítás (Claude Code prompt)

> **Implementáld a `SpaceOS_Modules_Cutting_Phase5_Analytics_Architecture_v4.md` tervdokumentum szerint a következő feladatokat.**
>
> **Track A (Domain + Application):**
> - `SpaceOS.Modules.Cutting.Analytics.Domain` projekt: 5 read-model entity (`DailyExecutionMetric`, `DailyMaterialUsage`, `DailyOperatorMetric`, `MachineOEEHourly`, `ProcessedOutboxEvent`), `AnalyticsRebuildJob` aggregate (Pending → Running → Completed/Failed FSM + chunk progress), 7 VO (`DateRange`, `KAnonymityThreshold`, `OEEBreakdown`, `WastePercentage`, `CapacityUtilization`, `ChunkProgress`, `PagedSpec/PagedResult`), 8 Specification (BE-11 naming), `IAnonymizationService` + `IOEECalculator` + `IWasteCalculator` ports.
> - `SpaceOS.Modules.Cutting.Analytics.Application` projekt: 5 query-handler-validator triplet (FluentValidation, BE-05 pagination), 1 command (TriggerAnalyticsRebuild + Idempotency-Key BE-15), 7 projector (BE-02 TenantActivated új), `ProjectionIdempotencyGate` (BE-01 ExecuteSqlInterpolatedAsync + BE-07 same-tx), `AnonymizationService` + `CachedAnonymizationService` (BE-03 Redis cache), `IAnalyticsAuditLogger` (SEC-06).
> - **Golden Rules 1–12 érvényesítése + új axiómák A5-15..A5-17.**
>
> **Track B (Infrastructure + Persistence):**
> - `SpaceOS.Modules.Cutting.Analytics.Infrastructure` projekt: `CuttingAnalyticsDbContext` (HasDefaultSchema "cutting_analytics"), 6 EF Core Configuration, 6 Repository (`AsNoTracking` + `AsSplitQuery` BE-08), 13 Migration (C-0007..C-0019), `OutboxAnalyticsSubscriber` (7 event type), `PreDeployValidator` (SEC-04).
> - `SpaceOS.Modules.Cutting.Execution.Infrastructure` módosítások: `IRateLimiter` + `RedisSentinelRateLimiter` (BE-04 implements both interfaces), `RedisSentinelHandshakeRateLimiter` `[Obsolete]` wrapper, `TpmFallbackPolicy` v3 update (SEC-02), `TpmAvailabilityMetricsCollector` (SEC-09), `ConsentChangeRateLimiter` (SEC-11).
>
> **Track C (API + BackgroundService + Tests):**
> - `SpaceOS.Modules.Cutting.Analytics.Api` projekt: `AnalyticsController` 7 endpoint (Waste/Capacity/OEE/Operator/Dashboard + Rebuild POST/GET) — JWT + Capability flag + rate-limit (SEC-05) + Idempotency-Key (BE-15), `LenientFlagsEnumConverter` (BE-06).
> - BackgroundService-ek: `AnalyticsProjectionService`, `AnalyticsRebuildService` (chunkolt 90 napos blokkokban), `TpmAvailabilityMetricsCollector`, dedup retention pg_cron job (SEC-03).
> - Tesztek: ≥115 új teszt (DoD §9 Összesített breakdown szerint). E2E: deploy → backfill → query → result.
>
> **KRITIKUS:**
> - **DB-01:** `cutting_analytics_reader` role pen-test — bázis tábla SELECT → permission denied; csak VIEW-on át hozzáférés
> - **DB-02 + BE-07:** dedup ledger + projection same-transaction; rollback együtt
> - **SEC-01 + BE-03:** DateRange.MinDaysForOperatorQuery=7 enforced + expression index covering l-diversity GROUP BY
> - **SEC-04:** pre-deploy gate `pg_roles` BYPASSRLS audit — CI fail ha sérül
> - **BE-01:** raw ADO.NET tilos (`grep -r "GetDbConnection\(\)\.CreateCommand"` → 0)
> - **BE-02:** `OnTenantActivatedHandler` (sync MediatR) NEM létezik — Outbox-pattern `TenantActivatedProjector`
> - **BE-04:** Phase 4 deployed config (`IHandshakeRateLimiter` binding) startup-on átmegy
> - Day-0 backfill task (DB-03) automatikusan fut deploy után minden aktív tenant-re
>
> **DoD:** §9 fent · Gate: `dotnet build && dotnet test && pre-deploy-validator` minden feladat után.

### 13.3 DI registration táblázat (BE-12)

| Típus | Scope | Register-elt mint |
|-------|-------|-------------------|
| `CuttingAnalyticsDbContext` | Scoped | `DbContext` |
| `ProjectionIdempotencyGate` | Scoped | önmaga |
| `AnonymizationService` | Scoped | `IAnonymizationService` (named "inner") |
| `CachedAnonymizationService` | Scoped | `IAnonymizationService` (default) |
| `RedisSentinelRateLimiter` | Singleton | `IRateLimiter` + `IHandshakeRateLimiter` (BE-04) |
| `IDistributedCache` (Redis) | Singleton | framework-provided |
| `PreDeployValidator` | Transient | önmaga (CI tool) |
| `TpmFallbackPolicy` | Scoped | `ITpmFallbackPolicy` |
| `TpmKeyProvisioner` | Scoped | `ITpmKeyProvisioner` |
| `TpmAvailabilityMetricsCollector` | Singleton | `IHostedService` |
| `AnalyticsProjectionService` | Singleton | `IHostedService` |
| `AnalyticsRebuildService` | Singleton | `IHostedService` |
| `ConsentChangeRateLimiter` | Scoped | önmaga |
| `IAnalyticsAuditLogger` | Scoped | wraps Phase 4 `ICuttingAuditLogger` |
| 6 read-model Repository | Scoped | per `I*Repository` interface |
| `IAnalyticsRebuildJobRepository` | Scoped | önmaga |
| 5 Query-Handler | Scoped | MediatR auto-discovery |
| 1 Command-Handler | Scoped | MediatR auto-discovery |
| 7 Projector | Scoped | `IProjector<TEvent>` open generic |
| `OutboxAnalyticsSubscriber` | Scoped | `IOutboxEventHandler` |

### 13.4 Kockázatok és mitigációk

| Kockázat | Valószínűség | Hatás | Mitigáció |
|----------|--------------|-------|-----------|
| Phase 4 DI binding break (BE-04) | Közepes | Production startup crash | Adapter pattern dual-binding + smoke test deployed config-on |
| l-diversity perf alulteljesít (BE-03) | Alacsony | Operator query >500ms p99 | Expression index covering + Redis cache + load test gate (DoD) |
| Day-0 backfill túlfut (DB-03) | Közepes | Deploy után 24h-ig nincs adat | 90 napos chunkolás (DB-08) + per-tenant 1 active job (SEC-07) + monitoring |
| TPM hardware unavailable (P4-9) | Közepes | Worker enrollment 403 | Per-tenant fallback opt-in (SEC-02) + admin runbook |
| Outbox at-least-once duplikáció (DB-02) | Magas | Aggregátum 2× hozzáadódik | Same-tx idempotency gate (BE-07) + 90 napos dedup ledger (SEC-03) |
| k-anon inference attack (SEC-01) | Közepes | GDPR-incidens, re-id 8 fős csapatban | DateRange.MinDays=7 + l-diversity≥2 + GROUP BY whitelist + residual risk runbook |
| BYPASSRLS elgépelés (SEC-04) | Alacsony | Cross-tenant data leak | Pre-deploy CI gate fail-closed |
| Contracts NuGet v1.4.0 release timing | Magas | Build fail Phase 5 nélkül | Project reference fallback dev-time, NuGet publish Phase 5 implementáció kezdetén |
| Schema-version 1 → 2 evolution | Alacsony | Breaking change downstream | SEC-15 expand-contract pattern README-ben |
| Doorstar Portal UI sprint scope creep | Közepes | UI munka szivárog Phase 5-be | Section 13 explicit tiltja UI-t; külön sprint a Portal repo-ban |

---

## 14. Companion: Claude Code agent context (README)

A `SpaceOS_Modules_Cutting_Phase5_Analytics_Architecture_v4_README.md` külön fájlban generálódik, az alábbi tartalommal:
- 21 napos sprint napi breakdown agent prompt-okra optimalizálva
- Per-track DI registration + interface contract-ok
- Per-track CLAUDE.md location: `spaceos-modules-cutting/src/Analytics/CLAUDE.md`
- Phase 4 függőség-mátrix: mit reuse-ol Phase 5 (Outbox + Audit + RateLimiter + TpmKeyProvisioner)
- Lokális dev környezet setup: PostgreSQL 16 + Redis 7 + tpm2-pkcs11 stub
- Migration runner pattern: `dotnet ef database update --project Infrastructure --context CuttingAnalyticsDbContext`
- Smoke test checklist deploy-after

---

*SpaceOS — Modules.Cutting Phase 5 Analytics v4.0 · 2026-04-28*
*`/database-designer` + `/database-schema-designer` + `/senior-security` + `/senior-backend` reviewed · 49 finding beépítve (2🔴 + 11🟠 + 23🟡 + 13🟢)*
*Státusz: **IMPLEMENTÁCIÓRA KÉSZ** — minden döntés lezárva, 0 nyitott CRITICAL/HIGH, ~21.1 nap implementáció*
