---
id: MSG-K026
from: architect
to: kernel
type: task
status: UNREAD
priority: P0
sprint: "Sprint D · Phase 3B"
ref: MSG-K024
---

# Phase 3B — Escrow GA Foundation

**Tervdokumentum:** `docs/SpaceOS_Phase3B_Architecture_v4.md`
**README:** `docs/PHASE_3B_README.md`
**Blokkoló feltétel:** Sprint D Phase 2 DoD ✅ TELJESÍTVE
**Migration sorszám:** 0020–0023
**Test baseline:** 814 pass / 0 fail (Phase 3A után)
**Cél:** ≥ 45 új teszt · 0 build warning · 0 CVE

---

## Miért most?

A Doorstar Escrow feature flag production-ban **nem kapcsolható be** amíg:
1. nincs snapshot store (mi volt az állapot a kifizetés pillanatában?)
2. a ProofUrl bárki által törölhető (hash nélkül nincs bizonyíték)
3. a genesis hash konstans van a codebase-ben (nem Key Vault)

Phase 3B ezt a 3 hiányt zárja le.

---

## Implementálandó 7 task (14 nap, Track A→D)

```
Track A (sequential, Escrow blocker):  T-01 → T-02 → T-03 → T-04
Track B (parallel A nap 7-től):        T-05 → T-06
Track C (parallel A nap 9-től):        T-07
Track D: Tesztek + DoD (nap 11-14)
```

---

## T-01 — AggregateSnapshot + ISnapshotable (Nap 1–2)

### Domain

**Új fájlok:**

`SpaceOS.Kernel.Domain/Entities/AggregateSnapshot.cs`
```csharp
public sealed class AggregateSnapshot : TenantScopedEntity
{
    public Guid           AggregateId    { get; private set; }
    public AggregateType  AggregateType  { get; private set; }
    public int            Version        { get; private set; }
    public DateTimeOffset SnapshotAt     { get; private set; }
    public Guid?          TriggerEventId { get; private set; }
    public string         StateJson      { get; private set; }
    public string         SnapshotHash   { get; private set; }

    public static AggregateSnapshot Create(
        Guid tenantId, Guid aggregateId, AggregateType aggregateType,
        int version, string stateJson, Guid? triggerEventId)
    {
        if (Encoding.UTF8.GetByteCount(stateJson) > 524_288)
            throw new DomainException(
                $"StateJson exceeds 512 KB for {aggregateType} {aggregateId}");

        var snap = new AggregateSnapshot
        {
            TenantId       = tenantId,
            AggregateId    = aggregateId,
            AggregateType  = aggregateType,
            Version        = version,
            SnapshotAt     = DateTimeOffset.UtcNow,
            TriggerEventId = triggerEventId,
            StateJson      = stateJson,
            SnapshotHash   = ComputeHash(stateJson)
        };
        snap.AddDomainEvent(new AggregateSnapshotCreatedEvent(
            snap.Id, tenantId, aggregateId, aggregateType.ToString(),
            version, snap.SnapshotHash));
        return snap;
    }

    private static string ComputeHash(string json)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)))
               .ToLowerInvariant();
}
```

`SpaceOS.Kernel.Domain/Enums/AggregateType.cs`
```csharp
public enum AggregateType { FlowEpic, FlowMilestone, B2BHandshake, SpaceLayer }
// 'Other' NEM szerepelhet — exhaustive enum gate
```

`SpaceOS.Kernel.Domain/Enums/OutboxStatus.cs`
```csharp
public enum OutboxStatus { Pending, Processing, Processed, Dead }
```

`SpaceOS.Kernel.Domain/Common/ISnapshotable.cs`
```csharp
public interface ISnapshotable
{
    string ToSnapshotJson();
}
```

`SpaceOS.Kernel.Domain/Events/AggregateSnapshotCreatedEvent.cs`
```csharp
// readonly record struct, IDomainEvent
// Payload: SnapshotId, TenantId, AggregateId, AggregateType, Version, SnapshotHash
// SEC-P3B-04: SnapshotHash az event payload-ban → bekerül az AuditEvent chain-be
```

`SpaceOS.Kernel.Domain/Repositories/IAggregateSnapshotRepository.cs`
```csharp
Task<int> GetNextVersionAsync(Guid aggregateId, Guid tenantId, CancellationToken ct);
Task AddAsync(AggregateSnapshot snapshot, CancellationToken ct);
// Ardalis.Specification: FirstOrDefaultAsync(ISpecification<AggregateSnapshot> spec, ...)
// ListAsync(ISpecification<AggregateSnapshot> spec, ...)
```

### Snapshot DTO-k (BE-P3B-01 fix)

**KRITIKUS:** `JsonSerializer.Serialize(aggregate)` TILOS — DDD private setter-ek → üres JSON.
Helyette explicit DTO az aggregate `ToSnapshotDto()` metódusán át.

`SpaceOS.Kernel.Application/Snapshots/Dtos/FlowEpicSnapshotDto.cs`
```csharp
public sealed record FlowEpicSnapshotDto(
    Guid   EpicId,
    Guid   TenantId,
    string Title,
    string FsmState,
    string WorkflowPhase,
    int    FsmRetryCount,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<FlowTaskSnapshotDto> Tasks);

public sealed record FlowTaskSnapshotDto(
    Guid   TaskId,
    string Title,
    string FsmState,
    string? ProofHash,
    bool   IsAcceptedByArchitect);
```

`FlowEpic` aggregate bővítése (`ISnapshotable` implementáció):
```csharp
public FlowEpicSnapshotDto ToSnapshotDto() => new(
    Id.Value, TenantId, Title, State.ToString(),
    WorkflowPhase.ToString(), FsmRetryCount, CompletedAt,
    Tasks.Select(t => t.ToSnapshotDto()).ToList().AsReadOnly());

string ISnapshotable.ToSnapshotJson()
    => JsonSerializer.Serialize(ToSnapshotDto());
```

### Migration 0020

```sql
CREATE TABLE "AggregateSnapshots" (
    "Id"             uuid         NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"       uuid         NOT NULL,
    "AggregateId"    uuid         NOT NULL,
    "AggregateType"  varchar(50)  NOT NULL,
    "Version"        integer      NOT NULL,
    "SnapshotAt"     timestamptz  NOT NULL DEFAULT now(),
    "TriggerEventId" uuid         NULL,
    "StateJson"      jsonb        NOT NULL,
    "SnapshotHash"   varchar(64)  NOT NULL,
    CONSTRAINT "PK_AggregateSnapshots"          PRIMARY KEY ("Id"),
    CONSTRAINT "CK_AggregateSnapshots_Type"     CHECK ("AggregateType" IN ('FlowEpic','FlowMilestone','B2BHandshake','SpaceLayer')),
    CONSTRAINT "CK_AggregateSnapshots_Version"  CHECK ("Version" > 0),
    CONSTRAINT "CK_AggregateSnapshots_JsonSize" CHECK (pg_column_size("StateJson") < 524288),
    CONSTRAINT "UQ_AggregateSnapshots_Version"  UNIQUE ("AggregateId", "Version")
);
ALTER TABLE "AggregateSnapshots" OWNER TO spaceos_schema_owner;
ALTER TABLE "AggregateSnapshots" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "AggregateSnapshots" FORCE ROW LEVEL SECURITY;
CREATE POLICY "tenant_isolation" ON "AggregateSnapshots"
    USING ("TenantId" = current_setting('app.current_tenant_id')::uuid
           OR current_setting('app.current_tenant_id')::uuid = '00000000-0000-0000-0000-000000000001');
CREATE INDEX "IX_AggregateSnapshots_TenantId"
    ON "AggregateSnapshots" ("TenantId");
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_AggregateSnapshots_AggregateId_SnapshotAt"
    ON "AggregateSnapshots" ("AggregateId", "SnapshotAt" DESC);
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_AggregateSnapshots_TriggerEventId"
    ON "AggregateSnapshots" ("TriggerEventId")
    WHERE "TriggerEventId" IS NOT NULL;
ALTER TABLE "AggregateSnapshots"
    ADD CONSTRAINT "FK_AggregateSnapshots_AuditEvents"
    FOREIGN KEY ("TriggerEventId") REFERENCES "AuditEvents"("Id") ON DELETE SET NULL;
-- CK_Tenants_NoSystemId hozzáadása a Tenants táblához (SEC-P3B-02):
ALTER TABLE "Tenants"
    ADD CONSTRAINT "CK_Tenants_NoSystemId"
    CHECK ("Id" <> '00000000-0000-0000-0000-000000000001');
```

Migration fájlnév: `20260407150000_Migration_0020_AggregateSnapshots.cs`
- CONCURRENTLY indexekre: `suppressTransaction: true`

---

## T-02 — OutboxEntry + OutboxWorker (Nap 3–4)

### Domain

`SpaceOS.Kernel.Domain/Entities/OutboxEntry.cs`
```csharp
public sealed class OutboxEntry : TenantScopedEntity
{
    private const int MaxRetries = 5;

    public string         EventType   { get; private set; }
    public string         Payload     { get; private set; }
    public DateTimeOffset CreatedAt   { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public int            RetryCount  { get; private set; }
    public string?        LastError   { get; private set; }
    public OutboxStatus   Status      { get; private set; }

    public static OutboxEntry Create(Guid tenantId, string eventType, string payload) => ...;

    public void MarkProcessed() { Status = OutboxStatus.Processed; ProcessedAt = DateTimeOffset.UtcNow; }

    public void MarkFailed(string error)
    {
        RetryCount++;
        // SEC-P3B-07: max 2000 char, stack trace NEM kerülhet bele
        LastError = error.Length > 2000 ? error[..2000] : error;
        Status = RetryCount >= MaxRetries ? OutboxStatus.Dead : OutboxStatus.Pending;
        if (Status == OutboxStatus.Dead)
            AddDomainEvent(new OutboxEntryDeadEvent(Id, TenantId, EventType, RetryCount));
    }
}
```

`SpaceOS.Kernel.Domain/Events/OutboxEntryDeadEvent.cs` — `readonly record struct`, `IDomainEvent`

`SpaceOS.Kernel.Domain/Repositories/IOutboxRepository.cs`
```csharp
Task<IReadOnlyList<OutboxEntry>> ClaimPendingAsync(int batchSize, CancellationToken ct);
Task AddAsync(OutboxEntry entry, CancellationToken ct);
Task SaveChangesAsync(CancellationToken ct);
```

### Application

`SpaceOS.Kernel.Application/Outbox/IOutboxEventHandler.cs`
```csharp
public interface IOutboxEventHandler
{
    string EventType { get; }
    Task HandleAsync(string payload, Guid tenantId, CancellationToken ct);
}
```

### Infrastructure

`OutboxRepository.cs`
- `ClaimPendingAsync`: `FOR UPDATE SKIP LOCKED` + system UUID RLS bypass:
```sql
SET LOCAL app.current_tenant_id = '00000000-0000-0000-0000-000000000001';
SELECT ... FROM "OutboxEntries"
WHERE "Status" IN ('Pending','Processing') ORDER BY "CreatedAt" ASC
LIMIT @batchSize FOR UPDATE SKIP LOCKED;
UPDATE "OutboxEntries" SET "Status" = 'Processing' WHERE "Id" = ANY(@ids);
```

`OutboxWorker.cs` (BackgroundService)
```csharp
// BE-P3B-02: IServiceScopeFactory, NEM direkt IOutboxRepository inject (Captive Dependency!)
private readonly IServiceScopeFactory _scopeFactory;
// SEC-P3B-02: UUID konstans (nem string)
private static readonly Guid SystemWorkerId = new("00000000-0000-0000-0000-000000000001");

protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5)); // NEM Task.Delay
    while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var repo     = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var handlers = scope.ServiceProvider
            .GetServices<IOutboxEventHandler>()
            .ToDictionary(h => h.EventType);
        await ProcessBatchAsync(repo, handlers, stoppingToken).ConfigureAwait(false);
    }
}

// Catch: OperationCanceledException → throw (graceful shutdown!)
// entry.MarkFailed(ex.Message) — NEM ex.ToString() (stack trace kizárva)
// ismeretlen EventType → Log.Warning, nem exception
```

### Migration 0021

```sql
CREATE TABLE "OutboxEntries" (
    "Id"          uuid          NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"    uuid          NOT NULL,
    "EventType"   varchar(100)  NOT NULL,
    "Payload"     jsonb         NOT NULL,
    "CreatedAt"   timestamptz   NOT NULL DEFAULT now(),
    "ProcessedAt" timestamptz   NULL,
    "RetryCount"  integer       NOT NULL DEFAULT 0,
    "LastError"   varchar(2000) NULL,
    "Status"      varchar(20)   NOT NULL DEFAULT 'Pending',
    CONSTRAINT "PK_OutboxEntries"          PRIMARY KEY ("Id"),
    CONSTRAINT "CK_OutboxEntries_Status"   CHECK ("Status" IN ('Pending','Processing','Processed','Dead')),
    CONSTRAINT "CK_OutboxEntries_Retry"    CHECK ("RetryCount" >= 0 AND "RetryCount" <= 10)
);
ALTER TABLE "OutboxEntries" OWNER TO spaceos_schema_owner;
ALTER TABLE "OutboxEntries" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "OutboxEntries" FORCE ROW LEVEL SECURITY;
CREATE POLICY "tenant_isolation" ON "OutboxEntries"
    USING ("TenantId" = current_setting('app.current_tenant_id')::uuid
           OR current_setting('app.current_tenant_id')::uuid = '00000000-0000-0000-0000-000000000001');
-- DB-P3B-05: stale Processing sorok is benne vannak (Pending + Processing)
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_OutboxEntries_Polling"
    ON "OutboxEntries" ("CreatedAt" ASC)
    WHERE "Status" IN ('Pending', 'Processing');
CREATE INDEX "IX_OutboxEntries_TenantId"
    ON "OutboxEntries" ("TenantId");
```

Migration fájlnév: `20260407160000_Migration_0021_OutboxEntries.cs`

---

## T-03 — SnapshotService + FlowEpicClosedDoneHandler (Nap 5)

`SpaceOS.Kernel.Application/Snapshots/ISnapshotService.cs`
```csharp
// internal — nem publikus, Api réteg nem láthatja
internal interface ISnapshotService
{
    Task TakeSnapshotAsync<T>(T aggregate, AggregateType type,
        Guid? triggerEventId, CancellationToken ct)
        where T : AggregateRoot, ISnapshotable;
}
```

`SnapshotService.cs` (internal sealed class):
```csharp
var stateJson = aggregate.ToSnapshotJson();  // BE-P3B-01: DTO-n át
var version   = await _snapshotRepo
    .GetNextVersionAsync(aggregate.Id.Value, aggregate.TenantId, ct)
    .ConfigureAwait(false);  // BE-P3B-06: ConfigureAwait mindenütt
var snapshot  = AggregateSnapshot.Create(...);
await _snapshotRepo.AddAsync(snapshot, ct).ConfigureAwait(false);
```

`SpaceOS.Kernel.Infrastructure/Outbox/Handlers/FlowEpicClosedDoneHandler.cs`
```csharp
// IOutboxEventHandler, EventType = "FlowEpicClosedDone"
// Payload: { EpicId, TenantId, TriggerEventId }
// → FlowEpic betöltése → ISnapshotService.TakeSnapshotAsync()
```

`SpaceOS.Kernel.Application/Snapshots/Handlers/FlowEpicClosedDoneOutboxHandler.cs`
```csharp
// MediatR notification handler a FSM CLOSED_DONE state-váltásra
// → OutboxEntry.Create("FlowEpicClosedDone", payload) INSERT
// Ugyanabban a UoW tranzakcióban mint a FSM state change
```

---

## T-04 — Snapshot Queries (Nap 6)

`GetSnapshotAtQuery.cs` + Handler:
- FluentValidation: `At <= DateTimeOffset.UtcNow.AddSeconds(5)` (SEC-P3B-09 — jövőbeli dátum)
- Handler: `SnapshotAtSpecification(aggregateId, at, tenantId)` Ardalis.Spec-en (BE-P3B-03)

`GetSnapshotVersionsQuery.cs` + Handler:
- `SnapshotVersionsSpecification(aggregateId, tenantId)` — `OrderByDescending(s => s.Version)`
- `Result<PagedList<SnapshotVersionDto>>`

`SpaceOS.Kernel.Application/Snapshots/Specs/SnapshotAtSpecification.cs`
```csharp
public sealed class SnapshotAtSpecification : Specification<AggregateSnapshot>
{
    public SnapshotAtSpecification(Guid aggregateId, DateTimeOffset at, Guid tenantId)
    {
        Query
            .Where(s => s.AggregateId == aggregateId
                     && s.TenantId == tenantId
                     && s.SnapshotAt <= at)
            .OrderByDescending(s => s.SnapshotAt)
            .AsNoTracking();
    }
}
```

`SnapshotEndpoints.cs`:
```
GET /api/snapshots/{aggregateId}?at=          → GetSnapshotAtQueryHandler
GET /api/snapshots/{aggregateId}/versions?page=&pageSize=  → GetSnapshotVersionsQueryHandler
```

---

## T-05 — ProofHash + WORM Storage (Nap 7–8)

### Migration 0022

```sql
ALTER TABLE "ImplementationSummaries" ALTER COLUMN "ProofUrl" DROP NOT NULL;
ALTER TABLE "ImplementationSummaries"
    ADD COLUMN "ProofHash"            varchar(64)   NULL,
    ADD COLUMN "ProofStorageKey"      varchar(1024) NULL,
    ADD COLUMN "ProofStorageProvider" varchar(20)   NULL;
ALTER TABLE "ImplementationSummaries"
    ADD CONSTRAINT "CK_ImplSummaries_Provider"
        CHECK ("ProofStorageProvider" IN ('local','s3','azure') OR "ProofStorageProvider" IS NULL);
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_ImplementationSummaries_ProofHash"
    ON "ImplementationSummaries" ("ProofHash")
    WHERE "ProofHash" IS NOT NULL;
```

Migration fájlnév: `20260407170000_Migration_0022_ImplSummaryProofHash.cs`

### IProofStorageService (Domain)

Elfogadott MIME típusok:
```csharp
private static readonly HashSet<string> AllowedMimeTypes =
[
    "image/jpeg", "image/png", "image/webp", "image/gif",
    "application/pdf", "video/mp4", "video/webm"
];
```

`StorageKey` formátum: `{tenantId}/{yyyy/MM/dd}/{guid}_{sanitizedFileName}` (SEC-P3B-01)

### LocalProofStorageService (dev)

- MIME validáció az `UploadAsync`-ban
- `VerifyHashAsync`: lokális fájl SHA-256 újraszámítása
- `IsAvailableAsync`: `Task.FromResult(true)`

### S3WormProofStorageService (prod stub)

- S3 Object Lock GOVERNANCE mode
- `IsAvailableAsync`: connection check, nem exception

### Proof upload endpoint (BE-P3B-04)

```csharp
// NEM IFormFile, NEM Command-ban stream — Request.Body közvetlen streamelés
app.MapPost("/api/tasks/{taskId}/proof", async (
    Guid taskId, HttpRequest request, IProofStorageService storage,
    ISender mediator, ClaimsPrincipal user, CancellationToken ct) =>
{
    var contentType = request.ContentType ?? "";
    // MIME validáció a storage service-ben — 415 UnsupportedMediaType ha nem engedélyezett
    var (hash, key) = await storage
        .UploadAsync(request.Body, "proof", contentType, tenantId, ct)
        .ConfigureAwait(false);
    var cmd = new AttachProofCommand(taskId, tenantId, hash, key, storage.ProviderName);
    var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
    return result.IsSuccess ? Results.Ok(result.Value) : result.ToApiResult();
});
```

`AttachProofCommand` + Handler:
- Cross-tenant guard: Task.TenantId != JWT tenantId → `Result.Forbidden`
- ImplementationSummary.ProofHash + ProofStorageKey + ProofStorageProvider beállítása
- `ProofAttachedEvent` domain event

---

## T-06 — VerifyChain Endpoint (Nap 9)

`VerifyChainQuery.cs` + Handler:
```csharp
public sealed record ChainVerificationDto(
    bool IsValid,
    DateTimeOffset? FirstBrokenAt,
    int TotalRecordsChecked,
    bool WormStorageAvailable,   // SEC-P3B-05: nem 500
    string? DiagnosticMessage);
```

`VerifyChainEndpoint.cs`:
```
GET /api/audit-events/verify-chain?tenantId=&from=&to=  → AdminOnly
```

- Ha WORM storage unavailable: `WormStorageAvailable = false`, HTTP 200 (nem 500)
- `IProofStorageService.IsAvailableAsync()` try-catch → flag

---

## T-07 — Genesis Hash KV + HashAlgorithm (Nap 10)

### GenesisHash konstans törlés (SEC-P3B-06)

```bash
grep -rn "000000000000000000000000000000000000000000000000000000000000000" --include="*.cs"
```
→ Ez a konstans törlendő. Csak `IGenesisHashProvider` implementáción át érhető el.

`KeyVaultGenesisHashProvider.cs` (Infrastructure/Security):
- Prod: Azure Key Vault / environment variable
- `ValidateOnStart()` → startup fail ha unavailable

`ConstantGenesisHashProvider.cs`:
- **CSAK** `IsDevelopment() == true` esetén regisztrálható
- Ha prod-on megpróbálják: `InvalidOperationException` a DI-ban

`Program.cs` DI:
```csharp
if (app.Environment.IsDevelopment())
    services.AddSingleton<IGenesisHashProvider, ConstantGenesisHashProvider>();
else
    services.AddSingleton<IGenesisHashProvider, KeyVaultGenesisHashProvider>()
            .AddOptions<KeyVaultOptions>().ValidateOnStart();
```

### Migration 0023

```sql
-- PostgreSQL virtual DEFAULT — nincs table rewrite, ACCESS EXCLUSIVE ms-os
ALTER TABLE "AuditEvents"
    ADD COLUMN "HashAlgorithm" varchar(20) NOT NULL DEFAULT 'SHA256';
ALTER TABLE "AuditEvents"
    ADD CONSTRAINT "CK_AuditEvents_HashAlgorithm"
        CHECK ("HashAlgorithm" IN ('SHA256','SHA3_256'));
```

Migration fájlnév: `20260407180000_Migration_0023_AuditEventsHashAlgorithm.cs`

---

## Tesztek (Nap 11–12, ≥ 45 új)

### Unit tesztek (≥ 25)

- `AggregateSnapshotTests.cs`:
  - `Create()` determinisztikus hash
  - StateJson > 512KB → `DomainException`
  - `AggregateSnapshotCreatedEvent` payload tartalmazza SnapshotHash-t

- `OutboxEntryTests.cs`:
  - `MarkFailed()` 5x → Status = Dead + `OutboxEntryDeadEvent`
  - `MarkFailed()` error > 2000 char → truncated
  - `MarkProcessed()` → Status = Processed + ProcessedAt != null

- `FlowEpicSnapshotDtoTests.cs`:
  - `ToSnapshotDto()` — private setter mezők megjelennek (Tasks, FsmState)
  - `ToSnapshotJson()` nem üres JSON (`{}`)

- `SnapshotServiceTests.cs` (mock repóval):
  - Version increment helyes
  - `ISnapshotable.ToSnapshotJson()` hívva, nem `JsonSerializer.Serialize(aggregate)`

- `ProofStorageKeyTests.cs`:
  - StorageKey TenantId-vel kezdődik
  - Sanitized fileName: path traversal karakter (`../`) kizárva

### Integration tesztek (≥ 15)

- `OutboxWorkerTests.cs`:
  - PeriodicTimer → batch feldolgozás
  - Ismeretlen EventType → Log.Warning, nem exception
  - OperationCanceledException → rethrow (graceful shutdown)
  - `IServiceScopeFactory` scope per batch (nem leaking)

- `SnapshotEndpointTests.cs`:
  - `GET /api/snapshots/{id}?at=` hamis TenantId → 0 sor
  - `at=` jövőbeli timestamp → `FluentValidation` 422
  - `at=` múltbeli → legközelebbi snapshot visszaadva

- `ProofUploadTests.cs`:
  - Nem engedélyezett MIME → 415
  - Cross-tenant TaskId → 403
  - Sikeres upload: ProofHash !== ProofUrl

### Security tesztek (≥ 5)

- `ConstantGenesisHashProvider` prod DI-ban regisztrálva → `InvalidOperationException`
- `grep -r "BuildServiceProvider"` → 0 (CI gate)
- GenesisHash konstans grep → 0
- `VerifyChain` non-admin → 403
- `VerifyChain` WORM unavailable → 200 + `WormStorageAvailable: false`

---

## DoD Checklist — kritikus gate-ek

| # | Ellenőrzés |
|---|-----------|
| ✅ | `AggregateSnapshot.Create()` hash determinisztikus; > 512KB → DomainException |
| ✅ | `ToSnapshotJson()` — nem üres JSON; private setter mezők benne vannak |
| ✅ | `OutboxEntry.MarkFailed()` — max 2000 char; stack trace kizárva |
| ✅ | `OutboxWorker` — IServiceScopeFactory, PeriodicTimer, graceful shutdown |
| ✅ | `ISnapshotService` — internal, Api réteg nem látja |
| ✅ | Proof upload — MIME whitelist; hash szerveren számított; Request.Body stream (nem buffer) |
| ✅ | `ProofStorageKey` — TenantId az első path component |
| ✅ | `ConstantGenesisHashProvider` — csak `IsDevelopment()` esetén |
| ✅ | GenesisHash konstans grep → 0 találat |
| ✅ | `VerifyChain` WORM unavailable → 200 + flag (nem 500) |
| ✅ | Migration 0020–0023 fut; FORCE RLS + spaceos_schema_owner |
| ✅ | `CK_Tenants_NoSystemId` — system UUID nem regisztrálható tenant-ként |
| ✅ | 814 meglévő teszt zöld + ≥ 45 új |
| ✅ | 0 build warning · 0 CVE |
| ⏳ | EXPLAIN ANALYZE minden új endpointon (PostgreSQL-en) |
| ⏳ | OutboxEntries polling: Index Scan on `IX_OutboxEntries_Polling` |

---

## Elvárt outbox üzenet

`type: response`, `ref: MSG-K026`:
- T-01..T-07 minden task: ✅ DONE vagy ⚠️ eltérés
- Build + test eredmény (pass/fail + új tesztek száma)
- `dotnet list package --vulnerable` kimenet
- Bármilyen blocker (különösen: `FlowEpic.Tasks` collection hozzáférhető-e a snapshot DTO-hoz?)
