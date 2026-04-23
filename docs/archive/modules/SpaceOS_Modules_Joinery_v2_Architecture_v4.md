# SpaceOS — Modules.Joinery v2
## PDF Gyártásilap + Graph Engine Async Bridge + Core 3 Seed

> **Verzió:** v4.0 — 2026-04-09
> **Státusz:** IMPLEMENTÁCIÓRA KÉSZ
> **Blokkoló feltétel:** Modules.Joinery v1 DoD + Modules.Abstractions v1 Phase A+B DoD
> **Kumulált review:** `/database-designer` + `/database-schema-designer` → v2 · `/senior-security` → v3 · `/senior-backend` → v4
> **Repo:** `spaceos-modules-joinery` (meglévő polyrepo)
> **DB schema:** `spaceos_joinery` (meglévő PostgreSQL 16)
> **Kernel kapcsolat:** `FlowEpicId` FK — Orchestrator-mediated (ADR-010)
> **Abstractions kapcsolat:** Orchestrator → Abstractions API (async Outbox)
> **Becsült effort:** ~16 fejlesztői nap (3 feature block + review delta)

---

## 1. Kumulált Finding Összesítő (v1 → v4)

| Review | Finding-ek | Legfontosabb javítás | Effort delta |
|--------|-----------|----------------------|--------------|
| v1 → `/database-designer` + `/database-schema-designer` → v2 | 0 CRITICAL · 3 HIGH · 3 MEDIUM | SnapshotId eltávolítás DoorOrders-ről · IsLatest partial unique index · Outbox partial index javítás | +1 nap |
| v2 → `/senior-security` → v3 | 2 CRITICAL · 4 HIGH · 2 MEDIUM | Internal endpoint guard · OutboxWorker SKIP LOCKED · PDF cache invalidáció · ContentHash tenant scoping | +1 nap |
| v3 → `/senior-backend` → v4 | 0 CRITICAL · 3 HIGH · 2 MEDIUM | SaveCalculationResult race (optimistic concurrency) · IOrchestratorClient retry · MarkCalculated() paraméter javítás | +0.5 nap |
| **Összesen** | **2 CRITICAL · 10 HIGH · 7 MEDIUM** | | **~16 fejlesztői nap** |

### Finding részletek

| ID | Súly | Terület | Probléma | v_ javítás |
|----|------|---------|----------|------------|
| DB-01 | 🟠 HIGH | DoorOrders.SnapshotId | FK constraint hiányzott | v2: **SnapshotId eltávolítva** (DB-02 javítás) — status flag elegendő |
| DB-02 | 🟠 HIGH | SnapshotId szemantika | Egy Order N item-je = N snapshot, de 1 SnapshotId | v2: SnapshotId oszlop törölve DoorOrders-ről. `Calculated` status = `COUNT(snapshots) = COUNT(items)` |
| DB-03 | 🟠 HIGH | Snapshot uniqueness | RevertToDraft + recalc = duplikált snapshot | v2: `IsLatest boolean DEFAULT true` + `UNIQUE (DoorItemId) WHERE IsLatest = true` partial index. Régi snapshot: `IsLatest = false` |
| DB-04 | 🟡 MEDIUM | Outbox RetryCount | Nincs DB CHECK | v2: `CHECK ("RetryCount" >= 0 AND "RetryCount" <= 5)` |
| DB-05 | 🟡 MEDIUM | Outbox partial index | Nem illeszkedik query pattern-hez | v2: `WHERE "ProcessedAt" IS NULL AND "FailedAt" IS NULL AND "RetryCount" < 3` |
| DB-06 | 🟡 MEDIUM | PDF cache FilePath | Path traversal nincs DB-szinten validálva | v2: `CHECK ("FilePath" !~ '^\.\.' AND "FilePath" !~ '/\.\.')` |
| SEC-01 | 🔴 CRITICAL | Abstractions calculate endpoint | Portal-ból is hívható belső endpoint | v3: Orchestrator guard: dedikált `/internal/` prefix + `X-SpaceOS-Internal` header check. Portal route-ok nem proxy-zzák az `/internal/` path-t |
| SEC-02 | 🔴 CRITICAL | OutboxWorker race condition | Párhuzamos tick duplikált snapshot-ot hoz létre | v3: `SELECT ... FOR UPDATE SKIP LOCKED` a pending outbox query-ben |
| SEC-03 | 🟠 HIGH | PDF cache invalidation | RevertToDraft után stale PDF | v3: `RevertToDraft()` → delete `ProductionSheetCache` rekord + file unlink. Cache key `ContentHash`-t tartalmaz |
| SEC-04 | 🟠 HIGH | Abstractions template tenant | Cross-tenant template calculate lehetséges | v3: Explicit `WHERE TenantId = @tenantId` az Abstractions endpoint-ban (RLS + app check) |
| SEC-05 | 🟠 HIGH | PDF response headers | Inline render + sniff lehetséges | v3: `Content-Disposition: attachment`, `X-Content-Type-Options: nosniff`, `Cache-Control: private, no-store` |
| SEC-06 | 🟠 HIGH | ContentHash tenant scoping | TenantId hiányzik hash inputból | v3: `sb.Append(TenantId)` első elemként a hash-ben |
| SEC-07 | 🟡 MEDIUM | Outbox PayloadJson | Üzleti adat (nem PII) — elfogadott kockázat | v3: Dokumentálva; RLS véd |
| SEC-08 | 🟡 MEDIUM | QuestPDF input sanitize | Visuális spoofing extrém hosszú névvel | v3: Max length validáció + truncate PDF rendererben |
| BE-01 | 🟠 HIGH | SaveCalculationResult race | Concurrent callback: 2× MarkCalculated() | v4: `[ConcurrencyCheck]` a DoorOrder.Version-ön + `DbUpdateConcurrencyException` retry |
| BE-02 | 🟠 HIGH | IOrchestratorClient retry | Nincs HTTP-szintű retry | v4: 3× retry exponential backoff (0s, 2s, 5s) az HTTP client-ben |
| BE-03 | 🟠 HIGH | MarkCalculated(snapshotId) | DB-02 után nincs SnapshotId paraméter | v4: `MarkCalculated()` paraméter nélkül |
| BE-04 | 🟡 MEDIUM | Outbox cleanup | Tábla korlátlanul nő | v4: PeriodicTimer 1h, 7 napos retention |
| BE-05 | 🟡 MEDIUM | PDF MemoryStream | 500 item = ~15MB csúcs | v4: Dokumentálva, elfogadott (500 limit) |

---

## 2. Architekturális döntések

| # | Döntés | Választás | Indoklás |
|---|---|---|---|
| D-01 | Graph Engine hívás | **Aszinkron** (Outbox + DomainEvent) | Decoupled; nem blokkol API-t; retry built-in |
| D-02 | CuttingList tárolás | **Denormalizált snapshot** (Joinery DB) | Audit trail; historikus gyártásilap; template változás nem hat vissza |
| D-03 | PDF library | **QuestPDF** (MIT, C# natív) | Nincs external binary; QR beépített |
| D-04 | PDF generálás | **On-demand lazy cache** (API-ban szinkron) | CuttingList DONE event után; PDF csak ha kérik |
| D-05 | Ajtótípusok | **Core 3** (FAF_T + FAF_Ü + BFAJ — ~70%) | Soft launch hitelesség |
| DB-02 | SnapshotId | **Eltávolítva** DoorOrders-ről | Status flag + query pattern elegendő |
| DB-03 | Snapshot history | **IsLatest** flag + partial unique index | Régi snapshot nem törlődik, de nincs duplikáció |
| SEC-01 | Internal endpoint | `/internal/` prefix + header guard | Portal soha nem éri el |
| BE-01 | Concurrency | EF Core `[ConcurrencyCheck]` + retry | Standard optimistic pattern |

---

## 3. Async Flow — teljes szekvencia

```
Portal                  Orchestrator :3000       Joinery :5002              Abstractions :5003
  │                          │                       │                          │
  ├─PUT /bff/joinery/        │                       │                          │
  │  orders/{id}/submit ────→│                       │                          │
  │                          ├─PUT /api/orders/      │                          │
  │                          │   {id}/submit ───────→│                          │
  │                          │                       │  DoorOrder.Submit()      │
  │                          │                       │  → Status = Submitted    │
  │                          │                       │  → OutboxEntry × N items │
  │                          │←─── 202 Accepted ─────┤                          │
  │←──── 202 Accepted ───────┤                       │                          │
  │                          │                       │                          │
  │   [async — JoineryOutboxWorker, 5s tick]          │                          │
  │                          │                       │                          │
  │                          │                       ├─ SELECT ... FOR UPDATE   │
  │                          │                       │   SKIP LOCKED (SEC-02)   │
  │                          │                       │                          │
  │                          │←── POST /internal/    │                          │
  │                          │    abstractions/      │                          │
  │                          │    calculate ─────────┤                          │
  │                          │    (X-SpaceOS-Internal)│  (SEC-01)               │
  │                          │                       │                          │
  │                          ├── POST /api/templates/│                          │
  │                          │   {name}/calculate ──────────────────────────────→│
  │                          │   (tenantId in body)  │                          │
  │                          │                       │    WHERE TenantId=X      │
  │                          │                       │    (SEC-04 + RLS)        │
  │                          │←── CalculationResult ─│←─────────────────────────┤
  │                          │                       │                          │
  │                          ├── PUT /internal/      │                          │
  │                          │   joinery/results ───→│                          │
  │                          │                       │  CuttingListSnapshot     │
  │                          │                       │  .Create(result)         │
  │                          │                       │  ContentHash incl.       │
  │                          │                       │  TenantId (SEC-06)       │
  │                          │                       │                          │
  │                          │                       │  if all items done:      │
  │                          │                       │    MarkCalculated()      │
  │                          │                       │    (ConcurrencyCheck     │
  │                          │                       │     BE-01)               │
  │                          │                       │                          │
  │  [Portal polls status]   │                       │                          │
  │  GET .../orders/{id} ───→├──→ GET ──────────────→│                          │
  │←── {status:"Calculated"} │                       │                          │
  │                          │                       │                          │
  │  [User clicks PDF]       │                       │                          │
  │  GET .../sheet ─────────→├──→ GET /api/orders/   │                          │
  │                          │    {id}/sheet ────────→│                          │
  │                          │                       │  Lazy: QuestPDF render   │
  │                          │                       │  + file cache            │
  │←── PDF (attachment,      │←── stream ────────────┤                          │
  │    nosniff) (SEC-05)     │                       │                          │
```

---

## 4. Domain modell — v2 bővítések

### 4.1 DoorOrderStatus bővítés

```csharp
public enum DoorOrderStatus
{
    Draft,              // Szerkeszthető
    Submitted,          // Outbox-ra írva, kalkuláció indítva
    Calculating,        // Graph Engine dolgozik (OutboxWorker felvette)
    Calculated,         // Minden item snapshot kész, PDF generálható
    CalculationFailed,  // Graph Engine hiba
    InProduction,       // Gyártásban
    Completed           // Lezárva
}
```

### 4.2 DoorOrder aggregate bővítés

```csharp
public sealed class DoorOrder : TenantScopedEntity
{
    // ... v1 fields ...
    public string? CalculationError { get; private set; }

    // BE-01: optimistic concurrency
    [ConcurrencyCheck]
    public int Version { get; private set; }

    public Result Submit()
    {
        if (Status != DoorOrderStatus.Draft)
            return Result.Error($"Cannot submit order in {Status} status");
        if (!_items.Any())
            return Result.Error("Order must have at least one item");

        Status = DoorOrderStatus.Submitted;
        CalculationError = null;
        Version++;
        AddDomainEvent(new DoorOrderSubmitted(Id, TenantId));
        return Result.Success();
    }

    public Result MarkCalculating()
    {
        if (Status != DoorOrderStatus.Submitted)
            return Result.Error($"Cannot start calculation in {Status} status");
        Status = DoorOrderStatus.Calculating;
        Version++;
        return Result.Success();
    }

    // BE-03: no snapshotId parameter
    public Result MarkCalculated()
    {
        if (Status != DoorOrderStatus.Calculating)
            return Result.Error($"Cannot mark calculated in {Status} status");
        Status = DoorOrderStatus.Calculated;
        Version++;
        AddDomainEvent(new DoorOrderCalculated(Id, TenantId));
        return Result.Success();
    }

    public Result MarkCalculationFailed(string reason)
    {
        if (Status != DoorOrderStatus.Calculating)
            return Result.Error($"Cannot mark failed in {Status} status");
        Status = DoorOrderStatus.CalculationFailed;
        CalculationError = reason?.Length > 2000 ? reason[..2000] : reason;
        Version++;
        AddDomainEvent(new DoorOrderCalculationFailed(Id, TenantId, reason));
        return Result.Success();
    }

    public Result RevertToDraft()
    {
        if (Status is not (DoorOrderStatus.CalculationFailed or DoorOrderStatus.Calculated))
            return Result.Error($"Cannot revert from {Status}");
        Status = DoorOrderStatus.Draft;
        CalculationError = null;
        Version++;
        // SEC-03: PDF cache invalidáció a handler-ben (nem a domain-ben — infra concern)
        AddDomainEvent(new DoorOrderReverted(Id, TenantId));
        return Result.Success();
    }
}
```

### 4.3 CuttingListSnapshot entity

```csharp
public sealed class CuttingListSnapshot : TenantScopedEntity
{
    private readonly List<CuttingListLine> _lines = new();
    private readonly List<CncInstruction> _cncInstructions = new();
    private readonly List<ProcessStep> _processSteps = new();

    public Guid DoorOrderId { get; private set; }
    public Guid DoorItemId { get; private set; }
    public string TemplateName { get; private set; }
    public int TemplateVersion { get; private set; }
    public decimal InputWidth { get; private set; }
    public decimal InputHeight { get; private set; }
    public string ParameterOverridesJson { get; private set; }
    public string ContentHash { get; private set; }
    public DateTimeOffset CalculatedAt { get; private set; }
    public bool IsLatest { get; private set; }  // DB-03

    public IReadOnlyList<CuttingListLine> Lines => _lines.AsReadOnly();
    public IReadOnlyList<CncInstruction> CncInstructions => _cncInstructions.AsReadOnly();
    public IReadOnlyList<ProcessStep> ProcessSteps => _processSteps.AsReadOnly();

    private CuttingListSnapshot() { }

    public static Result<CuttingListSnapshot> Create(
        Guid tenantId, Guid doorOrderId, Guid doorItemId,
        string templateName, int templateVersion,
        decimal inputWidth, decimal inputHeight,
        string? parameterOverridesJson,
        IReadOnlyList<CuttingListLine> lines,
        IReadOnlyList<CncInstruction> cncInstructions,
        IReadOnlyList<ProcessStep> processSteps,
        IClock clock)
    {
        if (!lines.Any()) return Result.Error("CuttingList cannot be empty");
        if (lines.Count > 200) return Result.Error("Maximum 200 lines per snapshot");

        var snapshot = new CuttingListSnapshot
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DoorOrderId = doorOrderId,
            DoorItemId = doorItemId,
            TemplateName = templateName,
            TemplateVersion = templateVersion,
            InputWidth = inputWidth,
            InputHeight = inputHeight,
            ParameterOverridesJson = parameterOverridesJson ?? "{}",
            CalculatedAt = clock.UtcNow,
            IsLatest = true
        };
        snapshot._lines.AddRange(lines);
        snapshot._cncInstructions.AddRange(cncInstructions);
        snapshot._processSteps.AddRange(processSteps);
        // SEC-06: TenantId benne a hash-ben
        snapshot.ContentHash = snapshot.ComputeHash();
        return Result.Success(snapshot);
    }

    private string ComputeHash()
    {
        var sb = new StringBuilder();
        sb.Append(TenantId);  // SEC-06
        sb.Append(TemplateName).Append(TemplateVersion)
          .Append(InputWidth).Append(InputHeight)
          .Append(ParameterOverridesJson);
        foreach (var line in _lines.OrderBy(l => l.SortOrder))
            sb.Append(line.ComponentName).Append(line.Width).Append(line.Height)
              .Append(line.Material).Append(line.Thickness).Append(line.Quantity);
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString())));
    }

    internal void MarkNotLatest() => IsLatest = false;  // DB-03: régi snapshot archív
}
```

### 4.4 Value Objects

```csharp
public sealed record CuttingListLine(
    string ComponentName,   // max 100 (SEC-08)
    string ComponentType,
    decimal Width, decimal Height,
    decimal CuttingWidth, decimal CuttingHeight,
    string Material, decimal Thickness,
    int Quantity, int SortOrder);

public sealed record CncInstruction(
    string ComponentName, string Operation,
    string Position, decimal? Diameter,
    decimal? Depth, decimal? Angle, string? Note);

public sealed record ProcessStep(
    string Phase, int StepOrder,
    string Description, int EstimatedSeconds);
```

### 4.5 Domain Events

```csharp
public sealed record DoorOrderSubmitted(Guid OrderId, Guid TenantId);
public sealed record DoorOrderCalculated(Guid OrderId, Guid TenantId);
public sealed record DoorOrderCalculationFailed(Guid OrderId, Guid TenantId, string? Reason);
public sealed record DoorOrderReverted(Guid OrderId, Guid TenantId);  // SEC-03 trigger
```

---

## 5. DB Schema — Migration J-0002

### 5.1 DDL

```sql
-- Migration J-0002 — CuttingList Snapshot + Async Outbox + PDF Cache

-- 1. DoorOrders bővítés (DB-02: nincs SnapshotId)
ALTER TABLE "DoorOrders"
    ADD COLUMN "CalculationError" varchar(2000) DEFAULT NULL,
    ADD COLUMN "Version" int NOT NULL DEFAULT 1;  -- BE-01: optimistic concurrency

ALTER TABLE "DoorOrders" DROP CONSTRAINT IF EXISTS "CK_DoorOrders_Status";
ALTER TABLE "DoorOrders" ADD CONSTRAINT "CK_DoorOrders_Status"
    CHECK ("Status" IN ('Draft','Submitted','Calculating','Calculated',
                        'CalculationFailed','InProduction','Completed'));

-- 2. CuttingListSnapshots
CREATE TABLE "CuttingListSnapshots" (
    "Id"                        uuid          NOT NULL PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId"                  uuid          NOT NULL,
    "DoorOrderId"               uuid          NOT NULL REFERENCES "DoorOrders"("Id") ON DELETE CASCADE,
    "DoorItemId"                uuid          NOT NULL REFERENCES "DoorItems"("Id") ON DELETE CASCADE,
    "TemplateName"              varchar(200)  NOT NULL,
    "TemplateVersion"           int           NOT NULL,
    "InputWidth"                decimal(8,2)  NOT NULL CHECK ("InputWidth" > 0 AND "InputWidth" <= 10000),
    "InputHeight"               decimal(8,2)  NOT NULL CHECK ("InputHeight" > 0 AND "InputHeight" <= 10000),
    "ParameterOverridesJson"    jsonb         NOT NULL DEFAULT '{}',
    "ContentHash"               varchar(64)   NOT NULL,
    "CalculatedAt"              timestamptz   NOT NULL DEFAULT NOW(),
    "IsLatest"                  boolean       NOT NULL DEFAULT true  -- DB-03
);

-- DB-03: partial unique — egy DoorItem-hez max 1 IsLatest=true
CREATE UNIQUE INDEX "UX_CuttingListSnapshots_DoorItemId_Latest"
    ON "CuttingListSnapshots" ("DoorItemId") WHERE "IsLatest" = true;

-- 3. CuttingListLines
CREATE TABLE "CuttingListLines" (
    "Id"              uuid          NOT NULL PRIMARY KEY DEFAULT gen_random_uuid(),
    "SnapshotId"      uuid          NOT NULL REFERENCES "CuttingListSnapshots"("Id") ON DELETE CASCADE,
    "TenantId"        uuid          NOT NULL,
    "ComponentName"   varchar(100)  NOT NULL,
    "ComponentType"   varchar(50)   NOT NULL,
    "Width"           decimal(8,2)  NOT NULL CHECK ("Width" > 0),
    "Height"          decimal(8,2)  NOT NULL CHECK ("Height" > 0),
    "CuttingWidth"    decimal(8,2)  NOT NULL CHECK ("CuttingWidth" > 0),
    "CuttingHeight"   decimal(8,2)  NOT NULL CHECK ("CuttingHeight" > 0),
    "Material"        varchar(100)  NOT NULL,
    "Thickness"       decimal(6,2)  NOT NULL CHECK ("Thickness" > 0),
    "Quantity"        int           NOT NULL CHECK ("Quantity" > 0 AND "Quantity" <= 100),
    "SortOrder"       int           NOT NULL DEFAULT 0
);

-- 4. CncInstructions
CREATE TABLE "CncInstructions" (
    "Id"              uuid          NOT NULL PRIMARY KEY DEFAULT gen_random_uuid(),
    "SnapshotId"      uuid          NOT NULL REFERENCES "CuttingListSnapshots"("Id") ON DELETE CASCADE,
    "TenantId"        uuid          NOT NULL,
    "ComponentName"   varchar(100)  NOT NULL,
    "Operation"       varchar(30)   NOT NULL CHECK ("Operation" IN (
        'None','Cut','AngledCut','Groove','Drill','EdgeBand','Chamfer','Round','Pocket','Profile'
    )),
    "Position"        varchar(200),
    "Diameter"        decimal(6,2),
    "Depth"           decimal(6,2),
    "Angle"           decimal(6,2),
    "Note"            varchar(500)
);

-- 5. ProcessSteps
CREATE TABLE "ProcessSteps" (
    "Id"                uuid          NOT NULL PRIMARY KEY DEFAULT gen_random_uuid(),
    "SnapshotId"        uuid          NOT NULL REFERENCES "CuttingListSnapshots"("Id") ON DELETE CASCADE,
    "TenantId"          uuid          NOT NULL,
    "Phase"             varchar(30)   NOT NULL CHECK ("Phase" IN (
        'Design','Cutting','CNC','EdgeBanding','Surface','Assembly','QualityControl','Packaging'
    )),
    "StepOrder"         int           NOT NULL,
    "Description"       varchar(500)  NOT NULL,
    "EstimatedSeconds"  int           NOT NULL CHECK ("EstimatedSeconds" >= 0)
);

-- 6. ProductionSheetCache
CREATE TABLE "ProductionSheetCache" (
    "Id"              uuid          NOT NULL PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId"        uuid          NOT NULL,
    "SnapshotId"      uuid          NOT NULL REFERENCES "CuttingListSnapshots"("Id") ON DELETE CASCADE,
    "FilePath"         varchar(500)  NOT NULL
        CHECK ("FilePath" !~ '^\.\.' AND "FilePath" !~ '/\.\.'),  -- DB-06
    "FileHash"         varchar(64)   NOT NULL,
    "GeneratedAt"      timestamptz   NOT NULL DEFAULT NOW(),
    UNIQUE ("SnapshotId")
);

-- 7. JoineryOutboxEntries
CREATE TABLE "JoineryOutboxEntries" (
    "Id"              uuid          NOT NULL PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId"        uuid          NOT NULL,
    "EventType"       varchar(200)  NOT NULL,
    "PayloadJson"     jsonb         NOT NULL,
    "CreatedAt"       timestamptz   NOT NULL DEFAULT NOW(),
    "ProcessedAt"     timestamptz   DEFAULT NULL,
    "FailedAt"        timestamptz   DEFAULT NULL,
    "Error"           varchar(2000) DEFAULT NULL,
    "RetryCount"      int           NOT NULL DEFAULT 0
        CHECK ("RetryCount" >= 0 AND "RetryCount" <= 5)  -- DB-04
);
```

### 5.2 RLS

```sql
-- RLS FORCE minden új táblán (6 db)
DO $$
DECLARE t text;
BEGIN
  FOREACH t IN ARRAY ARRAY[
    'CuttingListSnapshots','CuttingListLines','CncInstructions',
    'ProcessSteps','ProductionSheetCache','JoineryOutboxEntries'
  ] LOOP
    EXECUTE format('ALTER TABLE %I ENABLE ROW LEVEL SECURITY', t);
    EXECUTE format('ALTER TABLE %I FORCE ROW LEVEL SECURITY', t);
    EXECUTE format(
      'CREATE POLICY tenant_isolation ON %I USING ("TenantId" = current_setting(''app.tenant_id'')::uuid)', t);
  END LOOP;
END $$;
```

### 5.3 Indexek

```sql
CREATE INDEX "IX_CuttingListSnapshots_DoorOrderId" ON "CuttingListSnapshots" ("DoorOrderId");
CREATE INDEX "IX_CuttingListSnapshots_DoorItemId" ON "CuttingListSnapshots" ("DoorItemId");
CREATE INDEX "IX_CuttingListSnapshots_TenantId" ON "CuttingListSnapshots" ("TenantId");
CREATE INDEX "IX_CuttingListLines_SnapshotId" ON "CuttingListLines" ("SnapshotId");
CREATE INDEX "IX_CncInstructions_SnapshotId" ON "CncInstructions" ("SnapshotId");
CREATE INDEX "IX_ProcessSteps_SnapshotId" ON "ProcessSteps" ("SnapshotId");

-- DB-05: outbox partial index illeszkedik a query pattern-hez
CREATE INDEX "IX_JoineryOutboxEntries_Pending" ON "JoineryOutboxEntries" ("CreatedAt")
    WHERE "ProcessedAt" IS NULL AND "FailedAt" IS NULL AND "RetryCount" < 3;
```

---

## 6. API surface

### 6.1 Joinery API (:5002)

| Method | Endpoint | Leírás | Response |
|---|---|---|---|
| PUT | `/api/orders/{id}/submit` | Submit + Outbox trigger | 202 Accepted |
| GET | `/api/orders/{id}` | Order + status | 200 + `{status, itemCount, snapshotCount}` |
| GET | `/api/orders/{id}/snapshots` | Összes latest snapshot | 200 + `CuttingListSnapshot[]` |
| GET | `/api/orders/{id}/items/{itemId}/snapshot` | Egyedi item snapshot | 200 + `CuttingListSnapshot` |
| GET | `/api/orders/{id}/sheet` | PDF gyártásilap (lazy cache) | 200 + `application/pdf` (SEC-05 headers) |
| GET | `/api/orders/{id}/items/{itemId}/sheet` | Egyedi item PDF | 200 + `application/pdf` |
| PUT | `/api/orders/{id}/revert` | → Draft (SEC-03: cache invalidáció) | 200 |

### 6.2 Orchestrator BFF

| Method | BFF Endpoint | Proxy target | Hozzáférés |
|---|---|---|---|
| PUT | `/bff/joinery/orders/{id}/submit` | Joinery :5002 | Publikus (authenticated) |
| GET | `/bff/joinery/orders/{id}` | Joinery :5002 | Publikus |
| GET | `/bff/joinery/orders/{id}/snapshots` | Joinery :5002 | Publikus |
| GET | `/bff/joinery/orders/{id}/sheet` | Joinery :5002 | Publikus |
| PUT | `/bff/joinery/orders/{id}/revert` | Joinery :5002 | Publikus |
| POST | `/internal/abstractions/calculate` | Abstractions :5003 | **Belső only** (SEC-01) |
| PUT | `/internal/joinery/results` | Joinery :5002 | **Belső only** (SEC-01) |

**SEC-01 implementáció (Orchestrator):**
```typescript
// Middleware: /internal/* prefix
app.use('/internal/*', (req, res, next) => {
  if (req.headers['x-spaceos-internal'] !== process.env.INTERNAL_SECRET) {
    return res.status(403).json({ error: 'Internal endpoint' });
  }
  next();
});
```

### 6.3 Abstractions API (:5003)

| Method | Endpoint | Leírás |
|---|---|---|
| POST | `/api/templates/{name}/calculate` | Graph Engine calculate (SEC-04: tenant validated) |

---

## 7. Infrastructure — key components

### 7.1 JoineryOutboxWorker

```csharp
// SEC-02: FOR UPDATE SKIP LOCKED
var pending = await db.JoineryOutboxEntries
    .FromSqlRaw(@"
        SELECT * FROM ""JoineryOutboxEntries""
        WHERE ""ProcessedAt"" IS NULL AND ""FailedAt"" IS NULL AND ""RetryCount"" < 3
        ORDER BY ""CreatedAt""
        LIMIT 10
        FOR UPDATE SKIP LOCKED")
    .ToListAsync(ct).ConfigureAwait(false);
```

### 7.2 IOrchestratorClient (BE-02: retry)

```csharp
public sealed class OrchestratorClient : IOrchestratorClient
{
    private static readonly TimeSpan[] RetryDelays = { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5) };

    public async Task<Result<CalculationResponse>> CalculateAsync(...)
    {
        foreach (var (delay, attempt) in RetryDelays.Select((d, i) => (d, i)))
        {
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, ct).ConfigureAwait(false);

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(10)); // SEC: timeout

                var response = await _httpClient.PostAsJsonAsync(
                    "/internal/abstractions/calculate", request, cts.Token).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                    return Result.Success(await response.Content.ReadFromJsonAsync<CalculationResponse>(ct: ct).ConfigureAwait(false));

                if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound)
                    return Result.Error($"Calculation failed: {response.StatusCode}");
                // 5xx → retry
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested) { /* timeout → retry */ }
            catch (HttpRequestException) { /* transient → retry */ }
        }
        return Result.Error("Orchestrator unreachable after 3 attempts");
    }
}
```

### 7.3 SaveCalculationResultCommand (BE-01: concurrency)

```csharp
public async Task<Result> Handle(SaveCalculationResultCommand request, CancellationToken ct)
{
    // DB-03: régi snapshot IsLatest=false
    var oldSnapshot = await _db.CuttingListSnapshots
        .FirstOrDefaultAsync(s => s.DoorItemId == request.DoorItemId && s.IsLatest, ct)
        .ConfigureAwait(false);
    oldSnapshot?.MarkNotLatest();

    var snapshot = CuttingListSnapshot.Create(/* ... */);
    if (!snapshot.IsSuccess) return Result.Error(snapshot.Errors.First().ErrorMessage);
    _db.CuttingListSnapshots.Add(snapshot.Value);

    // Check if all items done
    var order = await _repo.GetByIdAsync(request.DoorOrderId, ct).ConfigureAwait(false);
    var allItemIds = order!.Items.Select(i => i.Id).ToHashSet();
    var doneItemIds = await _db.CuttingListSnapshots
        .Where(s => s.DoorOrderId == request.DoorOrderId && s.IsLatest)
        .Select(s => s.DoorItemId)
        .ToListAsync(ct).ConfigureAwait(false);
    doneItemIds.Add(request.DoorItemId);

    if (allItemIds.SetEquals(doneItemIds))
        order.MarkCalculated();  // BE-03: no parameter

    try
    {
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
    catch (DbUpdateConcurrencyException)
    {
        // BE-01: másik outbox consumer már MarkCalculated()-et hívott — reload + retry
        return Result.Error("Concurrency conflict — will be retried");
    }

    await _eventDispatcher.DispatchAsync(order.PopDomainEvents(), ct).ConfigureAwait(false);
    return Result.Success();
}
```

### 7.4 DoorOrderRevertedEventHandler (SEC-03: cache invalidáció)

```csharp
public sealed class DoorOrderRevertedEventHandler : INotificationHandler<DoorOrderReverted>
{
    public async Task Handle(DoorOrderReverted notification, CancellationToken ct)
    {
        // Töröld a PDF cache rekordokat + fájlokat
        var cacheEntries = await _db.ProductionSheetCache
            .Join(_db.CuttingListSnapshots,
                c => c.SnapshotId, s => s.Id,
                (c, s) => new { Cache = c, Snapshot = s })
            .Where(x => x.Snapshot.DoorOrderId == notification.OrderId)
            .Select(x => x.Cache)
            .ToListAsync(ct).ConfigureAwait(false);

        foreach (var entry in cacheEntries)
        {
            if (File.Exists(entry.FilePath))
                File.Delete(entry.FilePath);
            _db.ProductionSheetCache.Remove(entry);
        }

        // IsLatest=false minden snapshot-on (DB-03)
        var snapshots = await _db.CuttingListSnapshots
            .Where(s => s.DoorOrderId == notification.OrderId && s.IsLatest)
            .ToListAsync(ct).ConfigureAwait(false);
        foreach (var s in snapshots) s.MarkNotLatest();

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
```

### 7.5 JoineryOutboxCleanupJob (BE-04)

```csharp
public sealed class JoineryOutboxCleanupJob : BackgroundService
{
    private readonly PeriodicTimer _timer = new(TimeSpan.FromHours(1));
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (await _timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<JoineryDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                @"DELETE FROM ""JoineryOutboxEntries"" WHERE ""ProcessedAt"" < NOW() - INTERVAL '7 days'",
                ct).ConfigureAwait(false);
        }
    }
}
```

---

## 8. PDF Gyártásilap — QuestPDF

### 8.1 IProductionSheetGenerator

```csharp
public interface IProductionSheetGenerator
{
    Stream Generate(DoorOrder order, IReadOnlyList<CuttingListSnapshot> snapshots);
}
```

### 8.2 Response headers (SEC-05)

```csharp
// Joinery API endpoint
app.MapGet("/api/orders/{id}/sheet", async (Guid id, IMediator mediator) =>
{
    var result = await mediator.Send(new GetProductionSheetQuery(id));
    if (!result.IsSuccess) return Results.NotFound();

    return Results.File(result.Value, "application/pdf",
        fileDownloadName: $"gyartasilap_{id:N}.pdf",
        enableRangeProcessing: false);
})
.Produces(200, contentType: "application/pdf")
.WithMetadata(new EndpointMetadataCollection(
    new HeaderAttribute("X-Content-Type-Options", "nosniff"),
    new HeaderAttribute("Cache-Control", "private, no-store")));
```

### 8.3 Approved package

```xml
<PackageReference Include="QuestPDF" Version="2024.12.*" />
```

**QuestPDF Community Edition** — MIT licence, kereskedelmi használat megengedett < $1M annual revenue alatt. Felette: Professional ($699/yr). Doorstar soft launch: Community OK.

---

## 9. Core 3 Seed — Abstractions DB

### 9.1 Template effort

| Template | Slots | Connections | Parameters | Effort |
|---|---|---|---|---|
| FAF_T | ~15 | ~20 | 7 | 2 nap (pattern) |
| FAF_Ü | ~18 | ~24 | 9 | 0.5 nap (klón) |
| BFAJ | ~12 | ~16 | 7 | 0.5 nap (klón) |

### 9.2 Seed location

Abstractions repo: `DoorstarProductTemplateSeed.cs` — `IDataSeeder` implementáció.

---

## 10. Definition of Done

### Migration gates

- [ ] Migration J-0002: 7 tábla/ALTER (`DoorOrders` ALTER + `CuttingListSnapshots` + `CuttingListLines` + `CncInstructions` + `ProcessSteps` + `ProductionSheetCache` + `JoineryOutboxEntries`)
- [ ] RLS FORCE + tenant_isolation policy 6 új táblán
- [ ] DB-03: `UX_CuttingListSnapshots_DoorItemId_Latest` partial unique index
- [ ] DB-04: OutboxEntries RetryCount CHECK(0..5)
- [ ] DB-05: Outbox partial index illeszkedik query pattern-hez
- [ ] DB-06: ProductionSheetCache.FilePath path traversal CHECK
- [ ] DoorOrders: Status CHECK bővítve (Calculating, Calculated, CalculationFailed)
- [ ] DoorOrders: Version column (BE-01 optimistic concurrency)

### Domain gates

- [ ] `DoorOrderStatus` bővítés: +3 enum érték
- [ ] `DoorOrder`: Submit, MarkCalculating, MarkCalculated (no param — BE-03), MarkCalculationFailed, RevertToDraft — FSM guards
- [ ] `DoorOrder.Version` `[ConcurrencyCheck]` (BE-01)
- [ ] `CuttingListSnapshot.Create()`: immutable, ContentHash incl. TenantId (SEC-06), max 200 lines, IsLatest (DB-03)
- [ ] `CuttingListLine`, `CncInstruction`, `ProcessStep` record VOs
- [ ] Domain events: Submitted, Calculated, CalculationFailed, Reverted
- [ ] `IDoorCalculationService` DEPRECATED (nem törölve, nem hívva)

### API + validation gates

- [ ] `PUT /api/orders/{id}/submit` → 202 + Outbox INSERT
- [ ] `GET /api/orders/{id}/snapshots` → latest snapshots
- [ ] `GET /api/orders/{id}/sheet` → PDF stream (SEC-05 headers)
- [ ] `PUT /api/orders/{id}/revert` → Draft + cache invalidáció (SEC-03)
- [ ] Abstractions: `POST /api/templates/{name}/calculate` (SEC-04: tenant check)
- [ ] FluentValidation: InputWidth/Height > 0, <= 10000; TemplateName nem üres; ComponentName max 100 (SEC-08)

### Infrastructure gates

- [ ] `JoineryOutboxWorker`: PeriodicTimer 5s, `FOR UPDATE SKIP LOCKED` (SEC-02), graceful shutdown
- [ ] `IOrchestratorClient`: 3× retry exponential backoff (BE-02), 10s timeout
- [ ] `IProductionSheetGenerator`: QuestPDF implementáció
- [ ] PDF file cache: `{tenantId}/{orderId}_{hash}.pdf` path pattern
- [ ] `DoorOrderRevertedEventHandler`: cache + file cleanup (SEC-03)
- [ ] `JoineryOutboxCleanupJob`: 1h tick, 7d retention (BE-04)
- [ ] Orchestrator: `/internal/*` prefix + `X-SpaceOS-Internal` guard (SEC-01)

### Seed gates

- [ ] FAF_T ProductTemplate: ~15 slot, ~20 connection, 7 parameter
- [ ] FAF_Ü ProductTemplate: FAF_T klón + üveg slots
- [ ] BFAJ ProductTemplate: egyszerűsített struktúra
- [ ] `ITemplateValidator.Validate()` PASS mindhárom template-re

### Security gates (deployment blocker)

- [ ] SEC-01: `/internal/*` endpoint guard — Portal nem éri el
- [ ] SEC-02: `FOR UPDATE SKIP LOCKED` — nincs duplikált snapshot
- [ ] SEC-03: RevertToDraft → PDF cache invalidáció
- [ ] SEC-04: Abstractions calculate — tenant ownership validáció
- [ ] SEC-05: PDF response: attachment + nosniff + no-store
- [ ] SEC-06: ContentHash tartalmazza TenantId-t
- [ ] RLS FORCE minden Joinery táblán — cross-tenant teszt

### Összesített

- [ ] Meglévő 109 Joinery teszt zöld
- [ ] v2 új tesztek: ≥ 40 db
- [ ] 0 build warning
- [ ] `ConfigureAwait(false)` minden production async call-ban
- [ ] `dotnet list package --vulnerable` → 0 high/critical
- [ ] `EXPLAIN ANALYZE`: CuttingListSnapshots Index Scan (DoorOrderId), Outbox partial index
- [ ] Golden Rules 1–12 teljesül

---

## 11. Claude Code implementációs csomag

### Végrehajtási sorrend

| Nap | Feladat | Track | Függőség |
|-----|---------|-------|----------|
| 1 | Migration J-0002 DDL + RLS + indexek | A-DB | — |
| 2 | Domain: DoorOrderStatus bővítés + VO-k + events + CuttingListSnapshot entity | A-Domain | — |
| 3 | EF Core: JoineryDbContext bővítés + ConcurrencyCheck config | A-Infra | Nap 1-2 |
| 4 | JoineryOutboxWorker + JoineryOutboxEntry + OutboxCleanupJob | B-Outbox | Nap 1 |
| 5 | IOrchestratorClient (HTTP + retry) + SubmitDoorOrderCommandHandler (Outbox trigger) | B-Bridge | Nap 4 |
| 6 | SaveCalculationResultCommand + concurrency handling + IsLatest logic | B-Bridge | Nap 3-4 |
| 7 | Abstractions: POST /api/templates/{name}/calculate endpoint + tenant check | C-Abs | — |
| 8 | Orchestrator: /internal/ prefix + guard + proxy routes | C-Orc | Nap 7 |
| 9 | IProductionSheetGenerator (QuestPDF) + PDF layout | D-PDF | Nap 3 |
| 10 | GetProductionSheetQuery + lazy cache + response headers | D-PDF | Nap 9 |
| 11 | DoorOrderRevertedEventHandler (cache invalidáció) + RevertToDraft handler | D-PDF | Nap 10 |
| 12 | FAF_T seed (Abstractions repo) — pattern kialakítás | E-Seed | Nap 7 |
| 13 | FAF_Ü + BFAJ seed (klónok) + ITemplateValidator tesztek | E-Seed | Nap 12 |
| 14-16 | Tesztek: ≥40 új (unit + integration), E2E snapshot | F-Test | Nap 1-13 |

### Agent utasítás

> "Implementáld a Modules.Joinery v2 tervdokumentum szerint a következő feladatokat:
> Track A: Migration J-0002, Domain bővítés, EF Core config
> Track B: JoineryOutboxWorker, IOrchestratorClient, SaveCalculationResult
> Track C: Abstractions calculate endpoint, Orchestrator /internal/ guard
> Track D: QuestPDF PDF generator, lazy cache, response headers, cache invalidáció
> Track E: FAF_T, FAF_Ü, BFAJ seed
> Track F: ≥40 teszt
> DoD checklist: SpaceOS_Modules_Joinery_v2_Architecture_v4.md#10
> Blokkoló gate-ek: Migration J-0002, SEC-01, SEC-02
> Minden feladat után futtasd: dotnet test && dotnet build"

### Kockázatok és mitigációk

| Kockázat | Valószínűség | Hatás | Mitigáció |
|----------|-------------|-------|-----------|
| QuestPDF Community licenc revenue limit ($1M) | Alacsony (soft launch) | Közép — licencváltás szükséges | Monitoring; Professional license budget ($699/yr) |
| Abstractions API nem kész időben | Közép | Magas — teljes pipeline blokkolt | Phase A+B DONE ✅, csak 1 új endpoint kell |
| OutboxWorker deadlock sok item-nél | Alacsony | Közép — kalkuláció megáll | SKIP LOCKED + max 10 batch + monitoring |
| FAF_T seed hibás offset értékek | Közép | Magas — hibás gyártásilap | Doorstar validáció: manuális méretellenőrzés 3 teszt rendelésen |

---

## 12. Mi jön utána

| Fázis | Tartalom | Függőség | Effort |
|---|---|---|---|
| Joinery v2.1 | Többi ajtótípus seed (~95%) | v2 DONE | 3 nap |
| Joinery v2.5 | Klasszikus nézet + Excel import | v2 DONE + UX design | 8 nap |
| Joinery v3 | CNC G-code export | v2.1 + Doorstar CNC specs | 10 nap |
| Cabinet v1 | Szekrénygyártás domain | Abstractions v1 DONE | 14 nap |

---

*SpaceOS — Modules.Joinery v2 · v4.0 · `/database-designer` + `/database-schema-designer` + `/senior-security` + `/senior-backend` reviewed · 2026-04-09*
*Státusz: IMPLEMENTÁCIÓRA KÉSZ — 19 finding beépítve (2 CRITICAL + 10 HIGH + 7 MEDIUM), minden döntés lezárva*
