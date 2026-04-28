# SpaceOS — Modules.Manufacturing Phase 1: Edge Banding + CNC + Order Orchestration

## EdgeBandingTask + CncTask + ManufacturingOrder — Cross-module Outbox subscription + per-module Inbox idempotency + shared Workers.Identity

> **Verzió:** v4.0 — 2026-04-28
> **Státusz:** **IMPLEMENTÁCIÓRA KÉSZ** — minden review pass abszorbeálva, döntések lezárva, 0 nyitott CRITICAL/HIGH
> **Blokkoló feltétel:** Cutting Phase 4 DEPLOYED ✅ · Cutting Phase 5 DEPLOYED ✅ · Modules.Abstractions Phase B DEPLOYED ✅ · Contracts v1.4.0 DEPLOYED ✅
> **Kumulált review:** v1 → DB → v2 → security → v3 → backend → **v4**
> **Repo:** `spaceos-modules-manufacturing` · **Workers.Identity repo:** `spaceos-workers-identity`
> **Port:** `:5006` (Manufacturing) · `:5008` (Workers.Identity)
> **Becsült effort:** v3 ~20-24 nap → **v4 ~22-26 nap** (+2 nap backend delta)
> **Companion:** `SpaceOS_Modules_Manufacturing_Phase1_Architecture_v4_README.md` (Claude Code agent context, generálandó)

---

## 1. Kumulált Finding Összesítő (v1 → v4)

| Review | Findings (🔴 / 🟠 / 🟡) | Legfontosabb javítás | Effort delta |
|--------|------------------------|----------------------|--------------|
| v1 (DRAFT) | — | initial design + 5 pre-decision | bázis: ~14-18 nap |
| v1 → `database-designer` + `database-schema-designer` → v2 | 0 / 5 / 11 | counter columns, failure metadata owned entity, inbox event_id origin, Workers.Identity DDL, `module_subscriptions`, JSONB shape, partitioning, inbox cleanup | +3 nap |
| v2 → `senior-security` → v3 | 2 / 7 / 7 | Subscription SSRF + private-IP block (SEC-01), Inbox payload HMAC + tenant-id binding (SEC-02), 180d replay horizon (SEC-03), két-slot KEK rotation (SEC-04), HMAC binding scope (SEC-05), DLQ + quarantine (SEC-06), CNC plan integrity hash (SEC-07), mTLS-only endpoints (SEC-08), STRIDE 18-vector | +3 nap |
| v3 → `senior-backend` → **v4** | 0 / 4 / 7 | Outbox-tx invariant (BE-01), per-batch BackgroundService scope (BE-02), Specification gap closing (BE-03), `IHttpClientFactory` named clients + handler reuse (BE-04), Inbox HMAC verifier as singleton service (BE-05), `ResultExtensions` audit (BE-06), FluentValidation shape-only split (BE-07), MediatR async ordering (BE-08), `AsSplitQuery` saga query (BE-09), OpenAPI snapshot CI gate (BE-10), Inbox processor backpressure (BE-11) | +2 nap |
| **Összesen v1 → v4** | **2 🔴 / 16 🟠 / 25 🟡 = 43 finding** | minden CRITICAL és HIGH absorbed | **~22-26 nap** |

### 1.1 v4 Backend finding részletek

#### 🟠 HIGH

| ID | Súly | Terület | Probléma | v4 javítás |
|----|------|---------|----------|------------|
| **BE-01** | 🟠 HIGH | Outbox-tx invariant | v3 nem dokumentálja explicit, hogy a Manufacturing aggregate save + outbox INSERT szigorúan **egy tranzakcióban** kell legyen. Phase 4 BE-A01 minta nincs adaptálva. Ha a saga handler `OnEdgeBandingTaskCompleted_AdvanceOrder` az aggregate-savet és az outbox INSERT-et külön transaction-ben futtatja, partial-publish anomália jelenhet meg: a counter-update commitált, de az event nem került outbox-ba (vagy fordítva, drift). | (a) **`ManufacturingOutboxInterceptor`** a `ManufacturingDbContext`-en — a Phase 3B Kernel `OutboxInterceptor` mintát követi: `SavingChangesAsync` során collect-eli az aggregate `PopDomainEvents()`-et és INSERT-eli a `outbox_messages`-be **ugyanazon DbContext-tranzakcióban**; (b) **OUT-1 deployment gate**: integration test `DbUpdateException` simulation után aggregate row NEM commit AND outbox row NEM commit (atomicity); (c) `Database.AutoTransactionBehavior = AutoTransactionBehavior.WhenNeeded` explicit beállítás. |
| **BE-02** | 🟠 HIGH | BackgroundService DbContext scope leak | v3 a `ManufacturingInboxProcessor`, `ManufacturingInboxCleanupJob`, `KekRotationBackgroundService` (új SEC-04) BackgroundService-eket említi, de a Phase 4 BE-A02 per-batch scope-restart minta nincs explicit alkalmazva. Long-running scope = DbContext ChangeTracker telik, connection pool starvation, memory growth. | **Per-iteration scope-restart** mind a 4 BackgroundService-ben (Manufacturing: `ManufacturingInboxProcessor`, `ManufacturingInboxCleanupJob`; Workers.Identity: `KekRotationBackgroundService`, `WorkerLastActiveUpdateService`): `IServiceScopeFactory.CreateAsyncScope()` minden iterációhoz, `DbContext.ChangeTracker.Clear()` a scope kezdetén. **SCOPE-1/2/3/4 deployment gate**: 24h soak test connection pool + memory plateau monitoring. |
| **BE-03** | 🟠 HIGH | Repository specification gap | v3 6 specifikációt említ (v1 §4.1 felsorolás), de a saga-handler-ek (`OnPanelEdgeBandingCompleted`, `OnPanelCncCompleted`) saga-counter lookup-ja és a worker queue aggregation, machine queue prioritization specifikációi hiányoznak. Direct repository hívás → Golden Rule 5 sérül. | **4 új Specification** (összesen 10): `OrderForCounterAdvanceSpec` (saga lookup with `AsSplitQuery` BE-09), `EdgeBandingTaskQueueByWorkerSpec` (worker queue), `CncTaskQueueByWorkerSpec`, `MachineQueuePrioritizedSpec` (machine work-list ordered by `created_at`). Ardalis.Specification mindegyik. **REPO-1 deployment gate**: `grep -r "DbContext\..*Where" --include="*.cs" Repositories/` → 0 találat. |
| **BE-04** | 🟠 HIGH | HttpClient lifetime + handler reuse | v3 új mTLS endpoints (`/internal/inbox/cutting`, Workers.Identity, Kernel self-register) `HttpClient` használnak, de v3 nem specifikálja a lifecycle-t. Ha minden hívás `new HttpClient()`-tel megy, mTLS handler config újratöltődik (perf hit) + DNS cache stuck (rotated cert mismatch). | **`IHttpClientFactory` named clients** Phase 4 BE-A04 minta: `"workers-identity"` (Manufacturing → WI), `"kernel-subscriptions"` (Manufacturing → Kernel self-register), `"cutting-inbox-source"` (a Kernel `OutboxDispatcher` Manufacturing-felé hívásokra registrált a Kernel oldalán). Mindegyik `ConfigurePrimaryHttpMessageHandler` lambda-val mTLS cert + SPKI pin handler. Handler lifetime 5 perc. **HTTP-1/2/3 deployment gate**: integration test handler 1× allocation per named client. |

#### 🟡 MEDIUM

| ID | Súly | Terület | Probléma | v4 javítás |
|----|------|---------|----------|------------|
| **BE-05** | 🟡 MEDIUM | Inbox HMAC verifier scoping | v3 SEC-02 specifikálja a HMAC payload-verifikációt, de nem mondja meg, hogy az `IInboxHmacVerifier` szolgáltatás milyen DI lifetime-mal regisztrálódjon. Ha Scoped, minden inbox hívásra új instance — drága a public key load. Ha Transient, ugyanaz. Ha Singleton, kell thread-safety. | `IInboxHmacVerifier` **Singleton** DI lifetime-mal regisztrálva; `IConfigureNamedOptions<KernelOutboxKeyOptions>` használat (no `BuildServiceProvider()` anti-pattern). Public key cache-elt 1h TTL-lel; force-refresh handler `OnVerificationFailedTwice` event-en. |
| **BE-06** | 🟡 MEDIUM | ResultExtensions reuse | v3 nem említi a Cabinet 0.1 v4 / Phase 4 v4 `ResultExtensions.MapToResult` reuse-t. Manufacturing handler-ek inkonzisztens Result→HttpResult mapping-et csinálhatnak. | `ResultExtensions.MapToResult` és `ResultExtensions.ToOk/ToBadRequest/ToProblem` újra-alkalmazva (NuGet `SpaceOS.Common.ResultMapping` package re-use, ha létezik; egyébként Cabinet 0.1 v4 mintán helyi implementáció). Minden command/query handler ezt használja a Minimal API endpoint mapping-jén. |
| **BE-07** | 🟡 MEDIUM | Validator vs domain rule split | v3 nem mondja ki explicit, hogy a `FluentValidation` validator-ok **csak shape-validációt** végezzenek (length, range, regex, required) — minden business rule a Domain-be tartozik. Phase 4 BE-A07 minta. Risk: validator dupla-implementálja a domain szabályt → drift. | **Strict shape-only validators**: `FailCncTaskCommandValidator` regexes, lengths, FailureSource discriminator-shape; **NEM** ellenőrzi, hogy a task aktuális FSM state-ben fail-elhető-e (ez `CncTask.Fail()` factory feladata). Egy explicit lista a §4.10-ben dokumentálva: melyik validator csak shape, melyik kap `MustAsync(...)` cross-field check-et (Phase 4 BE-A09 mintán). Manufacturing-ban v4-ben **nincs** `MustAsync` validator — minden cross-field invariáns Domain-be. |
| **BE-08** | 🟡 MEDIUM | MediatR sync notification cross-context coupling | v3 §4.7 a saga handler `INotificationHandler<EdgeBandingTaskCompleted>` szinkron MediatR — ha a Saga handler crash, a fail propagálódik az aggregate-tx-be. Phase 5 BE-02 minta (Outbox-pattern async cross-context) nincs adaptálva. **De v1-ben elfogadtuk a counter-on-aggregate megközelítést, ami azonos DbContext-en van.** | **Decision finalized**: a saga `OnPanelEdgeBandingCompleted/Failed` event handler ugyanabban a Manufacturing DbContext-ben fut (same-context coupling), így synchronous MediatR helyett **direct call** a `OnTaskCompleted` event-handler-ből az `IManufacturingOrderRepository.AdvanceOnTaskCompletionAsync`-en. **Ez nem cross-context** — Phase 5 BE-02 csak akkor releváns, ha más bounded context (pl. Logistics) reagál. Cross-module reaction (Logistics) Outbox-on át megy (M1-1 axióma). |
| **BE-09** | 🟡 MEDIUM | AsSplitQuery saga lookup | v3 `OrderForCounterAdvanceSpec` (új BE-03-ban) az Order aggregate-et + összes EdgeBandingTask-ot + összes CncTask-ot lekéri (saga koordinációhoz). Without `AsSplitQuery()` ez Cartesian product → 24-panel × 4 line item × 500 cnc-op = 48000 row Cartesian. | `Specification.Query.AsSplitQuery()` — Phase 4 BE-A10 minta. **QUERY-1 deployment gate**: `EXPLAIN ANALYZE` 3 separate query, NEM Cartesian. Test: 24-panel order load test < 50ms. |
| **BE-10** | 🟡 MEDIUM | OpenAPI snapshot CI gate | v3 §8.3 csak "OpenAPI snapshot committed" sort tartalmaz, de Phase 4 BE-A11 minta szerint ezt CI gate-ként kell érvényesíteni: ha a snapshot diff != 0, a build fail-eljen, és csak explicit `--accept-snapshot` flag-gel mehet át. | **OpenAPI snapshot diff CI gate** GitHub Actions workflow lépés: `dotnet run --project tools/OpenApiSnapshot generate` → diff `Manufacturing.openapi.snapshot.json` → fail ha changed. 2 snapshot fájl: `Manufacturing.openapi.snapshot.json` (public API), `Manufacturing.Internal.openapi.snapshot.json` (internal endpoints). Phase 4-ből kiemelt convention. |
| **BE-11** | 🟡 MEDIUM | Inbox processor backpressure | v3 SEC-06 DLQ + retry counter ellenére, v3 nem foglalkozik a backpressure-rel: ha 1000+ inbox event érkezik egyszerre (Cutting batch-completion), a processor bottleneck. | **Bounded inbox processor concurrency**: `Channel<Guid>` reader-side, max 10 concurrent process, `BoundedChannelFullMode.Wait`. Phase 4 advisory lock minta NEM kell itt (per-tenant FIFO megőrződik az `event_id` UUID v7 timestamp-ordering-jével). Backpressure metric: `manufacturing_inbox_pending_age_seconds_p95` Prometheus exporter, alert > 60s. |

---

## 2. Kontextus és scope (változatlan v3-hoz képest)

### 2.4 Architektúra alapaxiómák (v4 frissítve)

| ID | Axióma | v4 frissítés |
|----|--------|--------------|
| M1-1 — M1-23 | (változatlan v3) | — |
| **M1-24 (v4)** | **Outbox-tx atomicity invariant** | **Aggregate save + outbox INSERT egy DbContext-tranzakcióban; `ManufacturingOutboxInterceptor` enforce-olja (BE-01)** |
| **M1-25 (v4)** | **Per-iteration BackgroundService scope** | **Minden BackgroundService `IServiceScopeFactory.CreateAsyncScope()` per iterációhoz; `ChangeTracker.Clear()` start-on (BE-02)** |
| **M1-26 (v4)** | **Specification-only repository access** | **Direct repository `Where` LINQ tilos; minden lookup `Ardalis.Specification` által (BE-03)** |
| **M1-27 (v4)** | **`IHttpClientFactory` named client minden cross-module HTTP-re** | **mTLS handler 1× alloc per named client; `HandlerLifetime = 5 perc` (BE-04)** |

---

## 3. Architekturális döntések (v4 frissítve)

### D1-14 (új v4) · ManufacturingOutboxInterceptor (BE-01)

A `ManufacturingDbContext` regisztrációjához hozzáadódik a `ManufacturingOutboxInterceptor`, ami a Phase 4 `OutboxInterceptor` mintáját követi, de a Manufacturing-specifikus event-ekre szabva:

```csharp
// Persistence/ManufacturingOutboxInterceptor.cs (BE-01)
public sealed class ManufacturingOutboxInterceptor(
    IDateTimeProvider clock,
    IOutboxWriter outbox,                       // Phase 3B Kernel re-use
    ILogger<ManufacturingOutboxInterceptor> logger)
    : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct)
    {
        var ctx = eventData.Context!;
        var aggregates = ctx.ChangeTracker.Entries<TenantScopedAggregate>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified)
            .Select(e => e.Entity)
            .ToList();

        foreach (var aggregate in aggregates)
        {
            var events = aggregate.PopDomainEvents();        // M1-3, Golden Rule 4
            foreach (var domainEvent in events)
            {
                // BE-01: insert into outbox in SAME transaction as aggregate save
                await outbox.WriteAsync(
                    tenantId: aggregate.TenantId,
                    aggregateId: aggregate.Id,
                    eventId: Guid.CreateVersion7(),
                    eventType: domainEvent.GetType().Name,
                    payload: JsonSerializer.SerializeToUtf8Bytes(domainEvent),
                    occurredAt: clock.UtcNow,
                    sourceModule: "Manufacturing",
                    ct).ConfigureAwait(false);
            }
        }

        return await base.SavingChangesAsync(eventData, result, ct).ConfigureAwait(false);
    }
}

// Persistence/ManufacturingDbContext.cs
public sealed class ManufacturingDbContext(
    DbContextOptions<ManufacturingDbContext> options,
    ManufacturingOutboxInterceptor outboxInterceptor)
    : DbContext(options)
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(outboxInterceptor);
        // BE-01: explicit auto-tx behavior (no implicit single-statement tx)
        Database.AutoTransactionBehavior = AutoTransactionBehavior.WhenNeeded;
    }
}
```

### D1-15 (új v4) · Per-iteration BackgroundService scope minta (BE-02)

Minden Manufacturing + Workers.Identity BackgroundService:

```csharp
// Application/Inbox/ManufacturingInboxProcessor.cs (BE-02 + BE-11)
public sealed class ManufacturingInboxProcessor(
    IServiceScopeFactory scopeFactory,
    IDateTimeProvider clock,
    ILogger<ManufacturingInboxProcessor> logger)
    : BackgroundService
{
    // BE-11: bounded concurrency
    private readonly Channel<Guid> _channel = Channel.CreateBounded<Guid>(
        new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = true
        });

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Producer: pickup pending inbox events, push to channel
        var producer = Task.Run(() => PickupLoopAsync(ct), ct);

        // BE-11: 10 concurrent consumers
        var consumers = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => ConsumeLoopAsync(ct), ct))
            .ToArray();

        await Task.WhenAll([producer, ..consumers]).ConfigureAwait(false);
    }

    private async Task PickupLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            // BE-02: per-pickup scope
            await using var pickupScope = scopeFactory.CreateAsyncScope();
            var inbox = pickupScope.ServiceProvider
                .GetRequiredService<IManufacturingInboxRepository>();

            var pending = await inbox.PickupBatchAsync(batchSize: 50, ct).ConfigureAwait(false);
            foreach (var eventId in pending)
                await _channel.Writer.WriteAsync(eventId, ct).ConfigureAwait(false);

            if (pending.Count == 0)
                await Task.Delay(TimeSpan.FromSeconds(2), ct).ConfigureAwait(false);
        }
    }

    private async Task ConsumeLoopAsync(CancellationToken ct)
    {
        await foreach (var eventId in _channel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
        {
            // BE-02: per-event scope (fresh DbContext, fresh ChangeTracker)
            await using var scope = scopeFactory.CreateAsyncScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IInboxEventDispatcher>();

            try
            {
                await dispatcher.DispatchAsync(eventId, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Inbox event {EventId} dispatch failed", eventId);
                // SEC-06: increment attempt_count, move to DLQ if >= 5
            }
            // Scope dispose → connection back to pool
        }
    }
}
```

### D1-16 (új v4) · Specification-only repository (BE-03)

```csharp
// Domain/Specifications/OrderForCounterAdvanceSpec.cs (BE-03 + BE-09)
public sealed class OrderForCounterAdvanceSpec : Specification<ManufacturingOrder>
{
    public OrderForCounterAdvanceSpec(Guid orderId)
    {
        Query.Where(o => o.Id == orderId);
        // BE-09: AsSplitQuery for Order + EdgeBandingTasks + CncTasks
        Query.AsSplitQuery();
        // Note: child task counts are computed on the aggregate (no Include needed)
    }
}

// Domain/Specifications/EdgeBandingTaskQueueByWorkerSpec.cs (BE-03)
public sealed class EdgeBandingTaskQueueByWorkerSpec : Specification<EdgeBandingTask>
{
    public EdgeBandingTaskQueueByWorkerSpec(Guid workerId, int maxResults = 50)
    {
        // Phase 4 minta: expression-based JSONB property access
        Query.Where(t => EF.Property<Guid?>(t, "WorkerAssignment_WorkerId") == workerId
                      && t.Status == ManufacturingTaskStatus.InProgress);
        Query.OrderBy(t => t.StartedAt!).Take(maxResults);
        Query.AsNoTracking();
    }
}

// Domain/Specifications/MachineQueuePrioritizedSpec.cs (BE-03)
public sealed class MachineQueuePrioritizedSpec : Specification<EdgeBandingTask>
{
    public MachineQueuePrioritizedSpec(Guid machineId, int maxResults = 50)
    {
        Query.Where(t => EF.Property<Guid?>(t, "MachineAssignment_MachineId") == machineId
                      && t.Status == ManufacturingTaskStatus.Pending);
        Query.OrderBy(t => t.Id).Take(maxResults);   // UUID v7 → time-ordered
        Query.AsNoTracking();
    }
}

// Infrastructure/Repositories/ManufacturingOrderRepository.cs (BE-03)
public sealed class ManufacturingOrderRepository(ManufacturingDbContext db)
    : IManufacturingOrderRepository
{
    public async Task<ManufacturingOrder?> GetForCounterAdvanceAsync(
        Guid orderId, CancellationToken ct)
    {
        var spec = new OrderForCounterAdvanceSpec(orderId);
        return await db.Orders
            .WithSpecification(spec)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }
    
    // NO direct .Where() calls anywhere in this class (M1-26, BE-03)
}
```

### D1-17 (új v4) · IHttpClientFactory named clients (BE-04)

```csharp
// Api/Program.cs (Manufacturing service DI setup)
builder.Services.AddHttpClient("workers-identity", (sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri(config["WorkersIdentity:BaseUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(5);
})
.ConfigurePrimaryHttpMessageHandler(sp =>
{
    var certProvider = sp.GetRequiredService<IClientCertificateProvider>();
    var pinValidator = sp.GetRequiredService<ISpkiPinValidator>();
    return new SocketsHttpHandler
    {
        SslOptions = new SslClientAuthenticationOptions
        {
            ClientCertificates = new() { certProvider.GetCert("manufacturing") },
            RemoteCertificateValidationCallback = pinValidator.ValidateForService("workers-identity"),
        },
        PooledConnectionLifetime = TimeSpan.FromMinutes(5),
    };
})
.SetHandlerLifetime(TimeSpan.FromMinutes(5));    // BE-04

builder.Services.AddHttpClient("kernel-subscriptions", /* analóg config */ );

// Strongly-typed wrapper (Phase 4 minta)
builder.Services.AddScoped<IWorkersIdentityProviderClient, HttpWorkersIdentityProviderClient>();
```

### D1-18 (új v4) · Inbox HMAC verifier as singleton (BE-05)

```csharp
// Infrastructure/Inbox/InboxHmacVerifier.cs (BE-05)
public interface IInboxHmacVerifier
{
    Task<Result> VerifyAsync(InboxHmacRequest request, CancellationToken ct);
}

public sealed class InboxHmacVerifier(
    IOptionsMonitor<KernelOutboxKeyOptions> keyOptions,
    IDateTimeProvider clock,
    ILogger<InboxHmacVerifier> logger)
    : IInboxHmacVerifier
{
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private byte[]? _cachedKey;
    private DateTimeOffset _cacheLoadedAt;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

    public async Task<Result> VerifyAsync(InboxHmacRequest request, CancellationToken ct)
    {
        var key = await GetKeyAsync(ct).ConfigureAwait(false);
        // ... HMAC verification logic
        // SEC-03: replay horizon check
        // SEC-02: payload tenant-id consistency
        return /* ... */;
    }

    private async Task<byte[]> GetKeyAsync(CancellationToken ct)
    {
        if (_cachedKey is not null && clock.UtcNow - _cacheLoadedAt < CacheTtl)
            return _cachedKey;

        await _refreshLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // double-check after acquiring lock
            if (_cachedKey is not null && clock.UtcNow - _cacheLoadedAt < CacheTtl)
                return _cachedKey;

            _cachedKey = await LoadKeyFromKmsAsync(ct).ConfigureAwait(false);
            _cacheLoadedAt = clock.UtcNow;
            return _cachedKey;
        }
        finally { _refreshLock.Release(); }
    }
}

// DI registration (BE-05): Singleton with thread-safe cache
builder.Services.AddSingleton<IInboxHmacVerifier, InboxHmacVerifier>();
builder.Services.Configure<KernelOutboxKeyOptions>(
    builder.Configuration.GetSection("KernelOutbox"));
// NO BuildServiceProvider() — Golden Rule 9 (BE-05)
```

### D1-19 (új v4) · ResultExtensions reuse (BE-06)

```csharp
// Application/Common/ResultExtensions.cs (BE-06 — Cabinet 0.1 v4 minta)
public static class ResultExtensions
{
    public static IResult ToOk<T>(this Result<T> result) =>
        result switch
        {
            { IsSuccess: true } => Results.Ok(result.Value),
            { Status: ResultStatus.NotFound } => Results.NotFound(result.Errors),
            { Status: ResultStatus.Invalid } => Results.UnprocessableEntity(result.ValidationErrors),
            { Status: ResultStatus.Forbidden } => Results.Forbid(),
            _ => Results.Problem(string.Join("; ", result.Errors), statusCode: 500)
        };

    public static IResult ToCreated<T>(this Result<T> result, Func<T, string> location) =>
        result.IsSuccess
            ? Results.Created(location(result.Value), result.Value)
            : result.ToOk();
    
    // ... egyéb mapping helpers
}

// Endpoint usage (BE-06, all command/query handlers):
app.MapPost("/v1/orders", async (
    CreateManufacturingOrderRequest request,
    IMediator mediator, CancellationToken ct) =>
{
    var result = await mediator.Send(new CreateManufacturingOrderCommand(/*...*/), ct);
    return result.ToCreated(o => $"/v1/orders/{o.Id}");
});
```

---

## 4. Domain modell (frissítések v4-ben)

### 4.1 Solution struktúra (v4 final)

```
spaceos-modules-manufacturing/
├── SpaceOS.Modules.Manufacturing.Domain/
│   ├── Aggregates/
│   │   ├── ManufacturingOrder.cs                       (counter columns + saga methods)
│   │   ├── EdgeBandingTask.cs
│   │   └── CncTask.cs                                  (PlanIntegrityHash SEC-07)
│   ├── Entities/
│   ├── ValueObjects/
│   │   ├── ProcessStepSequence.cs
│   │   ├── EdgeBandingMaterialSpec.cs
│   │   ├── CncOperationSpec.cs
│   │   ├── CompletionProof.cs
│   │   ├── WorkerAssignment.cs
│   │   ├── MachineAssignment.cs
│   │   ├── WorkerEventHmac.cs                          (SEC-05 extended scope)
│   │   ├── CncFailureDetails.cs                        (DB-03 + SEC-10 regex)
│   │   ├── EdgeBandingFailureDetails.cs                (DB-03)
│   │   ├── MachineErrorCode.cs                         (SEC-10 regex)
│   │   ├── CncPlanIntegrityHash.cs                     (SEC-07)
│   │   └── SignedCncPlan.cs                            (SEC-07)
│   ├── Enums/                                          (5 enum)
│   ├── Events/                                         (13 domain event + ProgressEventRejectedOnCancelledTask SEC-20)
│   ├── Policies/
│   │   ├── IManufacturingProofPolicy.cs
│   │   ├── IManufacturingProgressPolicy.cs
│   │   ├── IWorkerSecurityPolicy.cs                    (SEC-04, SEC-05)
│   │   └── IMachineErrorAttestationPolicy.cs           (SEC-16 Phase 1 weak attestation)
│   ├── Repositories/
│   │   ├── IManufacturingOrderRepository.cs
│   │   ├── IEdgeBandingTaskRepository.cs
│   │   └── ICncTaskRepository.cs
│   └── Specifications/                                 (BE-03: 10 specifikáció)
│       ├── ManufacturingOrderByIdSpec.cs
│       ├── ActiveOrdersByTenantSpec.cs
│       ├── OrdersByCuttingSheetSpec.cs
│       ├── OrderForCounterAdvanceSpec.cs               (BE-03 + BE-09 AsSplitQuery)
│       ├── PendingEdgeBandingTasksByMachineSpec.cs
│       ├── PendingCncTasksByMachineSpec.cs
│       ├── TasksByOrderIdSpec.cs
│       ├── EdgeBandingTaskQueueByWorkerSpec.cs         (BE-03)
│       ├── CncTaskQueueByWorkerSpec.cs                 (BE-03)
│       └── MachineQueuePrioritizedSpec.cs              (BE-03)
├── SpaceOS.Modules.Manufacturing.Application/
│   ├── Commands/                                       (12 command + handler + validator triplets)
│   ├── Queries/                                        (6 query + handler)
│   ├── EventHandlers/                                  (4 saga handler)
│   ├── Inbox/
│   │   ├── IManufacturingInboxRepository.cs
│   │   ├── ManufacturingInboxProcessor.cs              (BE-02 per-iter scope, BE-11 backpressure)
│   │   ├── ManufacturingInboxCleanupJob.cs             (BE-02 per-iter scope)
│   │   ├── IInboxEventDispatcher.cs
│   │   ├── CuttingPanelCompletedHandler.cs
│   │   ├── IInboxHmacVerifier.cs                       (BE-05 Singleton)
│   │   └── InboxHmacRequest.cs
│   ├── Adapters/
│   │   ├── IInventoryConsumptionAdapter.cs
│   │   └── IWorkersIdentityProviderClient.cs           (BE-04 IHttpClientFactory)
│   ├── Audit/
│   │   └── IManufacturingAuditLogger.cs                (SEC-12)
│   ├── Common/
│   │   └── ResultExtensions.cs                         (BE-06)
│   └── Validators/                                     (BE-07 shape-only)
├── SpaceOS.Modules.Manufacturing.Infrastructure/
│   ├── Persistence/
│   │   ├── ManufacturingDbContext.cs                   (BE-01 OutboxInterceptor)
│   │   ├── ManufacturingOutboxInterceptor.cs           (BE-01)
│   │   ├── Configurations/                             (4 aggregate × 1-2 owned)
│   │   ├── Repositories/                               (BE-03 specification-only)
│   │   └── Inbox/
│   │       ├── ManufacturingInboxRepository.cs
│   │       └── ManufacturingInboxDlqRepository.cs      (SEC-06)
│   ├── Migrations/
│   │   ├── 20260428_M-0001_ManufacturingSchema.cs
│   │   ├── 20260428_M-0002_ManufacturingInbox.cs       (SEC-03 horizon, SEC-06 attempt counter)
│   │   ├── 20260428_M-0003_AppendOnlyTriggers.cs
│   │   └── 20260428_M-0004_DlqAndSequenceTrigger.cs    (SEC-06, SEC-18)
│   ├── Adapters/
│   │   ├── InProcessInventoryConsumptionAdapter.cs     (M1-13)
│   │   ├── HttpWorkersIdentityProviderClient.cs        (BE-04)
│   │   └── InProcessAdapterAssertion.cs                (Phase 4 SEC-10 minta)
│   ├── Audit/
│   │   └── SerilogManufacturingAuditLogger.cs          (SEC-12)
│   ├── Inbox/
│   │   └── InboxHmacVerifier.cs                        (BE-05 Singleton)
│   └── Outbox/
│       └── ManufacturingOutboxInterceptor.cs           (BE-01)
├── SpaceOS.Modules.Manufacturing.Api/
│   ├── Program.cs                                      (DI: IHttpClientFactory, mTLS, JWT)
│   ├── Endpoints/
│   │   ├── ManufacturingOrderEndpoints.cs              (BE-06 ResultExtensions)
│   │   ├── EdgeBandingTaskEndpoints.cs
│   │   ├── CncTaskEndpoints.cs
│   │   ├── WorkerQueueEndpoints.cs
│   │   └── InboxEndpoints.cs                           (SEC-08 mTLS-only)
│   └── Snapshots/
│       ├── Manufacturing.openapi.snapshot.json         (BE-10 CI gate)
│       └── Manufacturing.Internal.openapi.snapshot.json (BE-10 CI gate)
└── SpaceOS.Modules.Manufacturing.Tests/
    ├── Domain/                                         (FSM, VO, factory, sequence math)
    ├── Application/                                    (handlers, validators, ResultExtensions)
    ├── Inbox/                                          (HMAC verify, idempotency, DLQ, replay)
    ├── Integration/                                    (DB + outbox-tx atomicity, RLS, scope)
    ├── Saga/                                           (concurrent advance, retry, AsSplitQuery)
    └── Performance/                                    (24-panel saga benchmark, inbox 1000-event load)
```

### 4.10 Validator vs Domain rule split (BE-07)

| Command | Validator (shape-only) | Domain rule (factory) |
|---------|------------------------|------------------------|
| `CreateManufacturingOrder` | `CuttingSheetId != Guid.Empty`, `TotalPanels in [1, 10000]` | `ManufacturingOrder.Create()` factory: sequence non-empty, source-tenant binding |
| `ScheduleEdgeBandingTask` | `OrderId != Guid.Empty`, `LineItems.Count in [1, 4]`, edge_index in [0,3] | `EdgeBandingTask.Schedule()`: order in `Created/InProgress`, no duplicate panel |
| `StartEdgeBandingTask` | `WorkerId != Guid.Empty`, `MachineId != Guid.Empty` | `EdgeBandingTask.Start()`: status == `Pending` |
| `RecordEdgeBandingProgress` | `EventId UUID v7`, `EventHmac.Length == 32` | `EdgeBandingTask.RecordProgress()`: status == `InProgress`, HMAC verify, idempotency |
| `CompleteEdgeBandingTask` | `Proof.Hash.Length == 32` | `EdgeBandingTask.Complete()`: all line items `Done|Skipped` |
| `FailEdgeBandingTask` | `Reason 1..2000`, `Source in [Operator, Material]` (SEC-10) | `EdgeBandingTask.Fail()`: status not `Completed/Cancelled` |
| `ScheduleCncTask` | `MachineAssignment.MachineId != Guid.Empty`, `Operations.Count in [1, 500]` | `CncTask.Schedule()`: `PlanIntegrityHash.Matches()` (SEC-07) |
| `FailCncTask` | `Source in [Machine, Operator, Material]`, `Source==Machine ⇒ Vendor + Code regex match` | `CncTask.Fail()`: status not `Completed/Cancelled` |
| `CancelManufacturingOrder` | `Reason 1..2000` | `ManufacturingOrder.Cancel()`: status not `Completed/Cancelled` |

**Manufacturing Phase 1 v4-ben NINCS `MustAsync` cross-field validator.** Minden cross-field invariáns Domain factory-ban fut (BE-07).

---

## 5. DB schema (változatlan v3-hoz képest)

> Lásd v3 §5. v4 nem ad hozzá új DDL-t — minden backend finding application/infra layer-en absorbálódik.

---

## 6. Public API surface (változatlan v3-hoz képest)

> Lásd v3 §6. v4-ben az endpoint-ok mind `ResultExtensions.ToOk/ToCreated`-en mennek (BE-06), és OpenAPI snapshot diff CI gate aktív (BE-10).

---

## 7. EF Core konfiguráció (változatlan v3-hoz képest, v4 BE-01 frissítés a §3 D1-14-ben)

---

## 8. Definition of Done (v4 final)

### 8.1 Migration gates (változatlan v3)

> Lásd v3 §8.1.

### 8.2 Domain gates (változatlan v3)

> Lásd v3 §8.2.

### 8.3 API + validation gates (v4 frissítve — BE-06, BE-07, BE-10)

- [ ] 12 command + handler + validator
- [ ] 6 query + handler (Specification + AsNoTracking + **AsSplitQuery saga lookup-on BE-09**)
- [ ] **BE-07**: minden validator shape-only, **0 `MustAsync` Manufacturing-ban** (grep gate)
- [ ] **BE-06**: minden endpoint `ResultExtensions.ToOk/ToCreated/ToProblem` használ
- [ ] `Result<T>` minden handler return-jén
- [ ] `ConfigureAwait(false)` minden production async call-on
- [ ] Internal `/internal/inbox/cutting` mTLS-only (SEC-08)
- [ ] Inbox idempotens (duplicate `eventId` → 200 OK no-op)
- [ ] **BE-10**: 2 OpenAPI snapshot fájl committed; CI gate fail ha diff != 0

### 8.4 Cross-module gates (változatlan v3)

> Lásd v3 §8.4.

### 8.5 Performance gates (v4 frissítve — BE-09)

- [ ] EXPLAIN ANALYZE Index Scan minden 8 query-n
- [ ] Saga handler retry policy: 3-attempt exponential backoff (DB-13)
- [ ] **QUERY-1 (BE-09)**: `OrderForCounterAdvanceSpec` `AsSplitQuery` — `EXPLAIN ANALYZE` 3 separate query, NEM Cartesian
- [ ] 24-panel order saga benchmark < 50ms

### 8.6 Security gates (v3 — változatlan)

> Lásd v3 §8.6 (21 db SEC gate).

### 8.7 Backend gates (v4 új — BE finding-ek deployment gate-ként)

- [ ] **OUT-1 (BE-01)**: Aggregate save + outbox INSERT atomicity — integration teszt: rollback → outbox üres
- [ ] **OUT-2 (BE-01)**: `ManufacturingOutboxInterceptor` regisztrálva a `ManufacturingDbContext`-en
- [ ] **OUT-3 (BE-01)**: `Database.AutoTransactionBehavior = WhenNeeded` startup-time verify
- [ ] **SCOPE-1 (BE-02)**: `ManufacturingInboxProcessor` per-event scope — 24h soak test connection plateau
- [ ] **SCOPE-2 (BE-02)**: `ManufacturingInboxCleanupJob` per-iter scope
- [ ] **SCOPE-3 (BE-02)**: `KekRotationBackgroundService` per-iter scope (Workers.Identity)
- [ ] **SCOPE-4 (BE-02)**: `WorkerLastActiveUpdateService` per-iter scope (Workers.Identity)
- [ ] **REPO-1 (BE-03)**: `grep -r "DbContext\..*Where" Repositories/ --include="*.cs"` → 0 találat
- [ ] **REPO-2 (BE-03)**: 10 Specification létezik, mind `Ardalis.Specification`
- [ ] **HTTP-1 (BE-04)**: `IHttpClientFactory` named clients regisztrálva: `workers-identity`, `kernel-subscriptions`, `cutting-inbox-source`
- [ ] **HTTP-2 (BE-04)**: integration teszt — handler 1× alloc per named client
- [ ] **HTTP-3 (BE-04)**: handler lifetime 5 perc
- [ ] **VERIFY-1 (BE-05)**: `IInboxHmacVerifier` Singleton DI lifetime
- [ ] **VERIFY-2 (BE-05)**: HMAC public key cache 1h TTL, force-refresh on 2× verification fail
- [ ] **VERIFY-3 (BE-05)**: `grep -r "BuildServiceProvider" --include="*.cs"` → 0 találat
- [ ] **MAP-1 (BE-06)**: `ResultExtensions.ToOk/ToCreated/ToProblem` documented + reused minden endpoint-on
- [ ] **VALID-1 (BE-07)**: `grep -r "MustAsync" Validators/ --include="*.cs"` → 0 találat (Manufacturing only; Workers.Identity HR enrollment outside this scope)
- [ ] **EVENT-1 (BE-08)**: Saga handler same-context coupling — direct method call NOT MediatR notification
- [ ] **QUERY-1 (BE-09)**: `AsSplitQuery` saga lookup — EXPLAIN 3 separate query
- [ ] **SNAPSHOT-1 (BE-10)**: 2 OpenAPI snapshot fájl committed
- [ ] **SNAPSHOT-2 (BE-10)**: CI gate `dotnet run --project tools/OpenApiSnapshot diff` → 0 changes
- [ ] **BACKPRESSURE-1 (BE-11)**: Inbox processor `Channel<Guid>` bounded 1000, 10 consumers
- [ ] **BACKPRESSURE-2 (BE-11)**: Prometheus metric `manufacturing_inbox_pending_age_seconds_p95` exporter, alert > 60s

### 8.8 Test gates (v4 final)

- [ ] **Unit tests:** ≥ 80 (FSM × 3 aggregate + counter math + VO + saga handlers + ResultExtensions + InboxHmacVerifier + Specifications)
- [ ] **Integration tests:** ≥ 60 (DB + outbox-tx atomicity + RLS + scope-restart soak + AsSplitQuery EXPLAIN + inbox idempotency + DLQ + retry + HMAC verify + replay-window)
- [ ] **API tests:** ≥ 25 (12 commands + 6 queries + ResultExtensions + JWT + mTLS + rate-limit)
- [ ] **Saga tests:** ≥ 15 (concurrent advance + DbUpdateConcurrencyException retry + counter invariants + partial completion + failed-panel saga drift)
- [ ] **Workers.Identity tests:** ≥ 30 (enrollment + revocation + KEK rotation + per-tenant key derivation + cross-tenant isolation)
- [ ] **Performance tests:** ≥ 5 (24-panel order < 50ms, 1000-event inbox burst, saga lookup < 10ms)
- [ ] **Összes új teszt:** ≥ **215**

### 8.9 Összesített

- [ ] Meglévő ~1452 teszt zöld
- [ ] Manufacturing új tesztek: ≥ **215** (v3 ~75 → v4 ~215, BE finding-ek miatt +80 új test)
- [ ] 0 build warning, `TreatWarningsAsErrors` true
- [ ] `dotnet list package --vulnerable` → 0 high/critical
- [ ] `grep -r "BuildServiceProvider" --include="*.cs"` → 0 találat
- [ ] `grep -r "GetDbConnection\(\)\.CreateCommand" --include="*.cs"` → 0 találat
- [ ] `grep -r "\.Where(" Infrastructure/Persistence/Repositories/ --include="*.cs"` → 0 találat (BE-03)
- [ ] `grep -r "MustAsync" Application/Validators/ --include="*.cs"` → 0 találat (BE-07)
- [ ] EXPLAIN ANALYZE Index Scan minden 8 query-n
- [ ] Migration `suppressTransaction: true` ahol CONCURRENTLY szükséges (DB-15)
- [ ] Cross-schema FK = 0 (M1-16)
- [ ] OpenAPI snapshot diff CI gate aktív (BE-10)
- [ ] Golden Rules 1–12 teljesül (compile + lint gate)
- [ ] CLAUDE.md frissítve (Domain/Application/Infrastructure/Api per-layer)

---

## 9. Security adósság státusz (v4 final)

| ID | Tétel | Phase 1 (v4) | Marad |
|----|-------|--------------|-------|
| P1-3 | AggregateSnapshot | ✅ Phase 3B re-use | — |
| P1-4 | Outbox Pattern | ✅ Phase 3B + Phase 4 + Phase 1 BE-01 | — |
| P1-8 | ProofHash + WORM | ✅ Phase 3B re-use | — |
| P2-1 | Chain Integrity Verifier | ✅ Phase 3B re-use | — |
| MFG-1 | Worker HMAC key per-tenant | ✅ SEC-04 (két-slot KEK) | annual rotation |
| MFG-2 | Inbox replay attack | ✅ SEC-03 (180d horizon) | — |
| MFG-3 | Cross-module endpoint auth | ✅ SEC-08 + BE-04 (mTLS + named client) | annual cert rotation |
| MFG-4 | Workers.Identity migration crypto | ✅ SEC-11 + BE-02 scope | — |
| MFG-5 | CNC operation tampering | ✅ SEC-07 (plan integrity hash) | Phase 2 machine-HMAC enforce |
| MFG-6 | Subscription target_url tampering | ✅ SEC-01 (allowlist + audit) | — |
| MFG-7 | Workers.Identity badge collision | ✅ partial: per-tenant UNIQUE | cross-tenant dedup deferred |
| MFG-8 | Inbox poison-pill | ✅ SEC-06 + BE-11 (DLQ + backpressure) | — |
| MFG-9 | Saga counter overflow | ✅ SEC-09 (CHECK bounds) | — |
| MFG-10 | Process step sequence tampering | ✅ SEC-18 (immutability trigger) | — |
| MFG-11 | MachineHmac Phase 1 weak attestation | partial: SEC-16 (audit marker) | Phase 2 enforce |
| **MFG-12 (új v4)** | **Outbox-tx atomicity drift** | ✅ BE-01 (interceptor) | — |
| **MFG-13 (új v4)** | **BackgroundService scope leak** | ✅ BE-02 (per-iter scope) | — |
| **MFG-14 (új v4)** | **Inbox processor backpressure** | ✅ BE-11 (bounded channel) | — |
| Escrow GA | S3 Object Lock | — | Horizon 2 |

**Összegzés:** 0 nyitott CRITICAL, 0 nyitott HIGH, **minden döntés lezárva**.

---

## 10. Mi jön utána (roadmap)

| Sorrend | Téma | Prereq |
|---------|------|--------|
| 1 | **Manufacturing Phase 1 implementáció (Claude Code)** | v4 IMPLEMENTÁCIÓRA KÉSZ |
| 2 | **Manufacturing Phase 2: Surface + Assembly + DAG ordering + machine-HMAC enforce** | Phase 1 DEPLOYED |
| 3 | **Manufacturing Phase 3: Analytics + OEE + capacity** | Phase 2 DEPLOYED |
| 4 | **Modules.Logistics Phase 1**: Dispatch planning, subscribes to `ManufacturingOrderCompleted` | Phase 1 DEPLOYED |
| 5 | **Modules.Installation Phase 1**: Install scheduling, subscribes to `LogisticsDelivered` | Logistics Phase 1 DEPLOYED |
| 6 | **Workers.Identity v2**: TPM, FIDO2, MFA, IP-allowlist | Phase 1 DEPLOYED |

---

## 11. Threat model (változatlan v3-hoz képest, 18 vector STRIDE)

> Backend review nem hozott új threat-vektort — csak konzisztencia-pontosításokat (BE-01..11). Lásd v3 §11.

---

## 12. Claude Code implementációs csomag

### 12.1 Végrehajtási sorrend

A Phase 1 ~22-26 napra van skálázva. **3 párhuzamos track**, 4 fejlesztő (vagy 4 párhuzamos Claude Code agent-session) kapacitásra optimalizálva. + Workers.Identity 1 dedikált track (5 nap).

| Nap | Track A: Domain + Application | Track B: Infrastructure + Persistence | Track C: API + Inbox + Tests | Track D: Workers.Identity |
|-----|-------------------------------|---------------------------------------|------------------------------|---------------------------|
| 1 | Domain VO-k (`ProcessStepSequence`, `EdgeBandingMaterialSpec`, `CncOperationSpec`, `CompletionProof`, `WorkerEventHmac`, `MachineErrorCode`, `CncPlanIntegrityHash`) | M-0001 + M-0002 migration scaffold | xUnit v3 test project setup, fixtures | WI-0001 schema + workers/enrollments/revocations |
| 2 | `ManufacturingOrder` aggregate (Create, AdvanceStep, Cancel, Complete, Fail) + counter math (DB-02) | EF Core configurations (OwnsOne + JSONB shape CHECK DB-10) | Aggregate FSM unit tests | `WorkerIdentity` aggregate (Enroll, Revoke, RotateBadge) |
| 3 | `EdgeBandingTask` aggregate FSM | RLS FORCE + WITH CHECK + append-only triggers (M-0003 DB-16) | EdgeBandingTask FSM tests + counter math tests | `WorkerEnrollment`/`WorkerRevocation` audit-trail entities |
| 4 | `CncTask` aggregate (PlanIntegrityHash SEC-07) | M-0004 DLQ + sequence trigger (SEC-06, SEC-18) | CncTask FSM + integrity hash tests | WI-0002 KEK slots (SEC-04) |
| 5 | Saga event handlers (`OnPanelEdgeBandingCompleted/Failed`, `OnPanelCncCompleted/Failed`) BE-08 direct call | `ManufacturingOutboxInterceptor` (BE-01) | Saga concurrent advance integration tests | Two-slot KEK rotation BackgroundService (BE-02 per-iter scope) |
| 6 | 10 Specification (Ardalis.Specification, BE-03 + BE-09 AsSplitQuery) | `ManufacturingDbContext` + interceptor registration (BE-01) | Saga retry tests (DB-13 3-attempt) | KEK rotation tests + per-tenant key derivation tests |
| 7 | 12 command-handler-validator triplet (FluentValidation shape-only BE-07) | `ManufacturingOrderRepository` + 2 task repo (BE-03 specification-only) | Validator tests (shape) | `IWorkerIdentityProvider` (Contracts) interface + DTO-k |
| 8 | 6 query-handler (Specification + AsNoTracking + AsSplitQuery) | `ManufacturingInboxRepository` + DLQ repo (SEC-06) | Handler tests | HR enrollment endpoint + JWT auth (SEC-11) |
| 9 | `IInboxHmacVerifier` (BE-05 Singleton + cache) | `InboxHmacVerifier` infrastructure (KMS key load) | InboxHmacVerifier tests + replay-horizon tests (SEC-03) | Cutting Phase 4 expand-contract Phase 1 (data copy) |
| 10 | `ManufacturingInboxProcessor` (BE-02 per-iter scope, BE-11 bounded channel) | `IInboxEventDispatcher` + `CuttingPanelCompletedHandler` | Inbox idempotency + DLQ + backpressure tests | Workers.Identity API endpoints + integration tests |
| 11 | `ManufacturingInboxCleanupJob` (BE-02 per-iter scope, SEC-03 180d retention) | `InProcessInventoryConsumptionAdapter` + `InProcessAdapterAssertion` | Cleanup job tests | Cross-tenant isolation tests |
| 12 | `IManufacturingAuditLogger` (SEC-12) | `SerilogManufacturingAuditLogger` | Audit log structure tests | Workers.Identity load tests (1000 worker enrollments) |
| 13 | `ResultExtensions` (BE-06) | Kernel `module_subscriptions` self-registration client (BE-04 named client) | API integration tests + ResultExtensions tests | (Buffer) |
| 14 | API Minimal API endpoints (12 command + 6 query + InboxEndpoints SEC-08 mTLS-only) | `IHttpClientFactory` named clients (BE-04: workers-identity, kernel-subscriptions) | OpenAPI snapshot generation (BE-10) | (Buffer) |
| 15 | Rate-limit middleware (SEC-15) | mTLS + SPKI pin validator (Phase 4 SEC-03 minta) | Rate-limit + mTLS integration tests | (Buffer) |
| 16 | (Buffer + integration) | (Buffer + integration) | Performance tests: 24-panel saga, 1000-event inbox burst | — |
| 17 | (Documentation + CLAUDE.md per-layer) | (Lynis scan + vulnerable package check) | DoD checklist verification | — |
| 18-22 | (Slack, smoke testing, runbook validation) | — | — | — |

### 12.2 Agent utasítás (Claude Code prompt)

> **Implementáld a SpaceOS_Modules_Manufacturing_Phase1_Architecture_v4.md tervdokumentum szerint a következő feladatokat.**
>
> **Track A (Domain + Application):**
> - `SpaceOS.Modules.Manufacturing.Domain` projekt: 3 aggregate (`ManufacturingOrder` saga + counter columns DB-02; `EdgeBandingTask`; `CncTask` PlanIntegrityHash SEC-07), owned entitások (line items, op items, failure details DB-03), 10 VO (`ProcessStepSequence`, `WorkerEventHmac` SEC-05 extended scope, `MachineErrorCode` SEC-10 regex, `CncPlanIntegrityHash` SEC-07, stb.), 13 domain event, 4 policy port, 10 Specification (BE-03 + BE-09 AsSplitQuery).
> - `SpaceOS.Modules.Manufacturing.Application` projekt: 12 command-handler-validator triplet (FluentValidation **shape-only** BE-07, 0 MustAsync), 6 query-handler (Specification + AsNoTracking + AsSplitQuery saga BE-09), 4 saga event handler (BE-08 direct call same-context), `ManufacturingInboxProcessor` (BE-02 per-iter scope, BE-11 bounded channel), `ManufacturingInboxCleanupJob` (BE-02 per-iter scope, SEC-03 180d), `IInboxHmacVerifier` (BE-05 Singleton + 1h cache), `IManufacturingAuditLogger` (SEC-12), `ResultExtensions` (BE-06).
> - **Golden Rules 1–12:** zero public setter, business logic Domain-ben, Result<T> mindenhol, ConfigureAwait(false) minden production async call-ban.
>
> **Track B (Infrastructure + Persistence):**
> - `SpaceOS.Modules.Manufacturing.Infrastructure` projekt: `ManufacturingDbContext` + `ManufacturingOutboxInterceptor` (BE-01 — Phase 3B Kernel re-use), 4 EF Core Configuration (OwnsOne + JSONB), 3 Repository (BE-03 specification-only — **0 .Where()** Infrastructure-ben), 4 Migration (M-0001..M-0004 idempotens DDL DB-16, RLS FORCE, append-only triggers, DLQ + sequence trigger).
> - `IHttpClientFactory` named clients (BE-04): `workers-identity`, `kernel-subscriptions` mTLS handler + SPKI pin (Phase 4 SEC-03 minta), 5-perc handler lifetime.
> - In-process: `InProcessInventoryConsumptionAdapter` (M1-13), `InProcessAdapterAssertion` (Phase 4 SEC-10 startup-time AssemblyLoadContext check).
> - DB role-ok (SEC-14): `spaceos_manufacturing_app`, `spaceos_workers_identity_app`, `spaceos_workers_identity_kek`, `kernel_subs_writer` — least-privilege, NO DELETE on schema (M1-12).
>
> **Track C (API + Inbox + Tests):**
> - `SpaceOS.Modules.Manufacturing.Api` projekt: Minimal API endpoint-ok (`ManufacturingOrderEndpoints`, `EdgeBandingTaskEndpoints`, `CncTaskEndpoints`, `WorkerQueueEndpoints`, `InboxEndpoints` SEC-08 mTLS-only), JWT auth + tenant-claim → `current_setting('spaceos.tenant_id')` (SEC-19, Phase 4 `TenantSessionInterceptor` reuse), rate-limit middleware (SEC-15: 10 req/sec/worker, 100 req/sec/tenant, 100 req/sec/inbox).
> - 2 OpenAPI snapshot fájl generálás (BE-10 CI gate): `Manufacturing.openapi.snapshot.json`, `Manufacturing.Internal.openapi.snapshot.json`.
> - Tests: ≥ 80 unit, ≥ 60 integration, ≥ 25 API, ≥ 15 saga, ≥ 30 Workers.Identity, ≥ 5 performance = **≥ 215 új teszt**.
>
> **Track D (Workers.Identity, párhuzamos):**
> - `spaceos-workers-identity` repo: `WorkerIdentity` aggregate, `WorkerEnrollment`/`WorkerRevocation` audit, `IWorkerIdentityProvider` (Contracts v1.5.0), két-slot KEK (SEC-04 + WI-0002), `KekRotationBackgroundService` (BE-02 per-iter scope), HR enrollment + revocation API (mTLS + JWT HR role).
> - Migration WI-0001 + WI-0002 alkalmazható; Cutting Phase 4 expand-contract data-move idempotens (SEC-11).
>
> **DoD checklist:** SpaceOS_Modules_Manufacturing_Phase1_Architecture_v4.md §8 (8.1–8.9, mind a 9 alszekció).
>
> **Blokkoló gate-ek (deployment blockers):**
> 1. Migration M-0001 + M-0002 + M-0003 + M-0004 idempotens, RLS FORCE all 8 tables (DB-01, DB-03, DB-16)
> 2. K-NNNN Kernel `module_subscriptions` migration (DB-06, SEC-01) alkalmazva a Kernel-ben (cross-repo dependency)
> 3. WI-0001 + WI-0002 Workers.Identity migration alkalmazható
> 4. 21 security gate (SEC-01..21)
> 5. 11 backend gate (OUT-1..3, SCOPE-1..4, REPO-1..2, HTTP-1..3, VERIFY-1..3, MAP-1, VALID-1, EVENT-1, QUERY-1, SNAPSHOT-1..2, BACKPRESSURE-1..2)
> 6. ≥ 215 új teszt zöld + meglévő ~1452 platform teszt zöld
> 7. 0 build warning, 0 vulnerable package
>
> **Minden feladat után futtasd:**
> ```bash
> cd /opt/spaceos/modules-manufacturing
> dotnet test --filter Category!=E2E
> dotnet build --warnaserror
> dotnet list package --vulnerable
> ```
>
> **Layer-specifikus CLAUDE.md fájlok kötelezőek olvasásra:**
> - `Domain/CLAUDE.md` — DDD invariánsok, Golden Rule 1-3, factory pattern
> - `Application/CLAUDE.md` — CQRS, Result-pattern, ConfigureAwait, validator/domain split (BE-07)
> - `Infrastructure/CLAUDE.md` — EF Core, RLS, Specification implementations, OutboxInterceptor (BE-01)
> - `Api/CLAUDE.md` — Minimal API conventions, JWT, mTLS, rate-limit, OpenAPI snapshot CI

### 12.3 Kockázatok és mitigációk

| Kockázat | Valószínűség | Hatás | Mitigáció |
|----------|--------------|-------|-----------|
| Cutting Phase 4 → Workers.Identity expand-contract migration data-loss | Közepes | Magas | 3-fázisú migration (SEC-11): copy → cutover → drop, 14-day window, idempotens INSERT, rollback runbook |
| Kernel `module_subscriptions` SSRF allowlist regex incomplete | Alacsony | Kritikus | SEC-01 explicit private-IP block + mTLS CN allowlist + audit-tábla; Phase 4 minta |
| Inbox HMAC key rotation kihagyás | Alacsony | Magas | SEC-04 két-slot KEK BE-02 scope + automatic 90d rotation + manual trigger runbook |
| Saga handler concurrent advance race condition | Közepes | Közepes | DB-13 3-attempt retry + EF Core `Version` IsConcurrencyToken + integration test 4-panel concurrent completion |
| BackgroundService memory leak (BE-02 ignored) | Közepes | Magas | 24h soak test + Prometheus connection pool monitor + alert |
| OpenAPI snapshot CI gate friction (developer pushback) | Magas | Alacsony | CI gate dokumentálva: `--accept-snapshot` flag + szükséges PR-review minden snapshot-changes-on |
| Inbox 1000-event burst → DB connection saturation | Alacsony | Közepes | BE-11 bounded channel + 10 consumer + Prometheus `pending_age_seconds_p95` alert |
| 24-panel order saga lookup performance | Alacsony | Közepes | BE-09 AsSplitQuery + Specification index — < 50ms benchmark |

---

*SpaceOS — Modules.Manufacturing Phase 1 v4.0 — IMPLEMENTÁCIÓRA KÉSZ · 2026-04-28*
*`database-designer` + `database-schema-designer` + `senior-security` + `senior-backend` reviewed*
*v3 → v4: 0 CRITICAL + 4 HIGH + 7 MEDIUM = 11 új backend finding beépítve*
*Kumulált: 2 🔴 / 16 🟠 / 25 🟡 = 43 finding (v1 → v4), minden döntés lezárva*
*Státusz: IMPLEMENTÁCIÓRA KÉSZ — Claude Code implementációs csomag aktív*
*Felülírja: v3.0 REVIEW*
