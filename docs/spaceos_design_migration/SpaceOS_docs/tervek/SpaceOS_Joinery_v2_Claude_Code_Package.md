# SpaceOS — Modules.Joinery v2: Claude Code Implementation Package

> **Dátum:** 2026-04-10
> **Tervdokumentum:** `SpaceOS_Modules_Joinery_v2_Architecture_v4.md`
> **Státusz:** IMPLEMENTÁCIÓ INDÍTVA
> **Effort:** ~16 fejlesztői nap · 6 track · 3 repo
> **Baseline tesztek:** Joinery 109 · Abstractions 61 · Orchestrator 176 · Kernel 927
> **Cél:** ≥40 új Joinery teszt (109 → ≥149)

---

## 0. KONTEXTUS — Ezt olvasd el először

### Mi ez a projekt?

SpaceOS egy multi-tenant SaaS platform asztalosipar számára. A Modules.Joinery v2 a **PDF gyártásilap** funkciót adja hozzá: a felhasználó beküld egy ajtó-rendelést → a Graph Engine kiszámolja a szabászlistát → a rendszer gyártásra kész PDF-et generál.

### Meglévő architektúra

```
Browser → Orchestrator :3000 (Node.js, stateless BFF)
            ├─→ Kernel API :5001 (.NET 8, PostgreSQL — FlowEpic, Tenant, Audit)
            ├─→ Joinery API :5002 (.NET 8, PostgreSQL spaceos_joinery — DoorOrder, DoorItem)
            └─→ Abstractions API :5003 (.NET 8, PostgreSQL spaceos_modules — ProductTemplate, Graph Engine)
```

### Data Sovereignty axióma (NON-NEGOTIABLE)

A PDF-ben érzékeny gyártási adat van (méretek, anyagok, vágáslista). **Minden renderelés a Modules rétegben (L2) történik.** Az Orchestrator csak proxy-zza a kész PDF binárist — nem deserializálja, nem olvassa, nem loggol tartalmat. A frontend soha nem számol méretet.

### Golden Rules (minden fájlban érvényes)

1. No public setters on aggregates
2. All business logic in Domain — never in handlers
3. Every mutation raises a domain event
4. `PopDomainEvents()` + `DispatchAsync()` at the end of every mutating handler
5. Every list query goes through `Ardalis.Specification` — no raw repository calls
6. `Result<T>` return type on every handler
7. `ConfigureAwait(false)` on every production async call
8. `AsNoTracking()` on every read-only repository method
9. No `BuildServiceProvider()` in DI setup
10. No eager-loaded navigation properties on aggregate roots
11. Security gates are deployment blockers — not afterthoughts
12. `EXPLAIN ANALYZE` on every new query endpoint — no Seq Scan

### Approved packages (Joinery repo)

MediatR · FluentValidation · Ardalis.Result · Ardalis.Specification · EF Core 8 · xUnit v3 · Moq · **QuestPDF** (2024.12.*)

---

## 1. MEGLÉVŐ KÓDBÁZIS — Joinery v1

### Solution struktúra

```
spaceos-modules-joinery/
├── SpaceOS.Modules.Joinery.Domain/
│   ├── Aggregates/
│   │   └── DoorOrder.cs              ← MÓDOSÍTANDÓ (v2 FSM bővítés)
│   ├── Entities/
│   │   └── DoorItem.cs
│   ├── Enums/
│   │   └── DoorOrderStatus.cs        ← MÓDOSÍTANDÓ (+3 enum)
│   ├── ValueObjects/
│   │   ├── DoorDimensions.cs
│   │   ├── SurfaceSpec.cs
│   │   ├── HardwareSpec.cs
│   │   └── ...
│   ├── Events/
│   │   └── DoorOrderCreated.cs, DoorItemAdded.cs, ...
│   └── Services/
│       └── IDoorCalculationService.cs  ← DEPRECATED (nem törölni, nem hívni v2-ben)
├── SpaceOS.Modules.Joinery.Application/
│   ├── Commands/ (Create, AddItem, Calculate, Submit)
│   └── Queries/ (GetCuttingList, GetProcessPlan, ...)
├── SpaceOS.Modules.Joinery.Infrastructure/
│   ├── Persistence/
│   │   └── JoineryDbContext.cs         ← MÓDOSÍTANDÓ (új entity-k regisztrálása)
│   ├── Services/
│   │   └── DoorCalculationService.cs   ← NEM HASZNÁLT v2-ben (Abstractions váltja)
│   └── Migrations/
│       ├── 0001_InitialCreate.cs
│       └── 0002_GlobalConstants.cs
├── SpaceOS.Modules.Joinery.Api/
│   └── Program.cs                      ← MÓDOSÍTANDÓ (új endpoint-ok)
└── SpaceOS.Modules.Joinery.Tests/      ← 109 teszt (73 unit + 36 HTTP integrációs)
```

### Meglévő DoorOrder aggregate (v1 — amit bővíteni kell)

```csharp
public sealed class DoorOrder : TenantScopedEntity
{
    private readonly List<DoorItem> _items = new();

    public Guid FlowEpicId { get; private set; }
    public string CustomerName { get; private set; }
    public DoorOrderStatus Status { get; private set; }
    public IReadOnlyList<DoorItem> Items => _items.AsReadOnly();

    private DoorOrder() { }

    public static Result<DoorOrder> Create(Guid tenantId, Guid flowEpicId, string customerName)
    {
        // ... factory
    }

    public Result<DoorItem> AddItem(/* params */)
    {
        if (Status != DoorOrderStatus.Draft)
            return Result.Error("Cannot add items to non-draft order");
        if (_items.Count >= 500) return Result.Error("Max 500 items");
        // ...
    }
}
```

### Meglévő DoorOrderStatus (v1)

```csharp
public enum DoorOrderStatus
{
    Draft,
    Submitted,
    InProduction,
    Completed
}
```

### Meglévő DB séma (spaceos_joinery)

```sql
-- Létező táblák:
-- DoorOrders (Id, TenantId, FlowEpicId, CustomerName, Status, IsArchived, CreatedAt, UpdatedAt)
-- DoorItems (Id, TenantId, DoorOrderId FK, DoorType, Width, Height, ...)
-- DoorTypeRules (15 sor seed)
-- ProcessTaskTemplates (41 sor seed)
-- GlobalConstants (3 sor seed)
-- RLS FORCE mindegyiken
```

---

## 2. MEGLÉVŐ KÓDBÁZIS — Abstractions (Phase A+B DONE)

A Graph Engine már kész — a Joinery v2 **felhasználja**, nem módosítja.

### Elérhető API endpoint-ok (:5003)

| Method | Endpoint | Leírás |
|---|---|---|
| GET | `/api/modules/templates/{id}` | Template részletek |
| POST | `/api/modules/templates/{id}/calculate` | Graph Engine számítás |
| GET | `/api/modules/templates/{id}/cnc-plan` | CNC műveleti terv |
| GET | `/api/modules/templates/{id}/process-plan` | Gyártási folyamat |

### Számítás output — CalculationResult struktúra

```csharp
public sealed record CalculationResult(
    ProductTemplate Template,
    IReadOnlyDictionary<Guid, ResolvedDimensions> Dimensions,
    IReadOnlyList<CuttingListItem> CuttingList,
    IReadOnlyDictionary<string, decimal> Parameters);

public sealed record CuttingListItem(
    Guid SlotId, string ComponentName, string ComponentType,
    decimal Width, decimal Height,
    decimal CuttingWidth, decimal CuttingHeight,
    string? Material, decimal? Thickness,
    int Quantity, int SortOrder);
```

### ManufacturingDerivation output

```csharp
// CNC plan
public sealed record CncOperation(
    string ComponentName, MachiningOperation Operation,
    string? Position, decimal? Diameter,
    decimal? Depth, decimal? Angle, string? Note);

// Process plan
public sealed record ProductionStep(
    ProcessPhase Phase, int StepOrder,
    string Description, int EstimatedSeconds);
```

### ÚJ endpoint szükséges Track C-ben

```
POST /api/templates/{name}/calculate   ← név alapján, nem ID-vel
```

Ez azért kell, mert a Joinery OutboxWorker a **template névvel** referál (pl. "FAF_T"), nem UUID-vel.

---

## 3. ASYNC FLOW — Teljes szekvencia (a v2 lényege)

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

**Fontos:** Az Orchestrator a /internal/ endpoint-okon keresztül kommunikál. Ezek NEM publikusak.

---

## 4. DOMAIN MODELL — v2 bővítések (C# kód)

### 4.1 DoorOrderStatus bővítés

```csharp
// SpaceOS.Modules.Joinery.Domain/Enums/DoorOrderStatus.cs
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
// SpaceOS.Modules.Joinery.Domain/Aggregates/DoorOrder.cs — BŐVÍTÉS
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

### 4.3 CuttingListSnapshot entity (ÚJ)

```csharp
// SpaceOS.Modules.Joinery.Domain/Entities/CuttingListSnapshot.cs
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

### 4.4 Value Objects (ÚJ)

```csharp
// SpaceOS.Modules.Joinery.Domain/ValueObjects/CuttingListLine.cs
public sealed record CuttingListLine(
    string ComponentName,   // max 100 (SEC-08)
    string ComponentType,
    decimal Width, decimal Height,
    decimal CuttingWidth, decimal CuttingHeight,
    string Material, decimal Thickness,
    int Quantity, int SortOrder);

// SpaceOS.Modules.Joinery.Domain/ValueObjects/CncInstruction.cs
public sealed record CncInstruction(
    string ComponentName, string Operation,
    string Position, decimal? Diameter,
    decimal? Depth, decimal? Angle, string? Note);

// SpaceOS.Modules.Joinery.Domain/ValueObjects/ProcessStep.cs
public sealed record ProcessStep(
    string Phase, int StepOrder,
    string Description, int EstimatedSeconds);
```

### 4.5 Domain Events (ÚJ)

```csharp
// SpaceOS.Modules.Joinery.Domain/Events/
public sealed record DoorOrderSubmitted(Guid OrderId, Guid TenantId);
public sealed record DoorOrderCalculated(Guid OrderId, Guid TenantId);
public sealed record DoorOrderCalculationFailed(Guid OrderId, Guid TenantId, string? Reason);
public sealed record DoorOrderReverted(Guid OrderId, Guid TenantId);  // SEC-03 trigger
```

---

## 5. DB SCHEMA — Migration J-0002

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

### 5.2 RLS (KÖTELEZŐ — deployment blocker)

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

## 6. API SURFACE

### 6.1 Joinery API (:5002) — új endpoint-ok

| Method | Endpoint | Leírás | Response |
|---|---|---|---|
| PUT | `/api/orders/{id}/submit` | Submit + Outbox trigger | 202 Accepted |
| GET | `/api/orders/{id}` | Order + status (MÓDOSÍTOTT: bővített response) | 200 + `{status, itemCount, snapshotCount}` |
| GET | `/api/orders/{id}/snapshots` | Összes latest snapshot | 200 + `CuttingListSnapshot[]` |
| GET | `/api/orders/{id}/items/{itemId}/snapshot` | Egyedi item snapshot | 200 + `CuttingListSnapshot` |
| GET | `/api/orders/{id}/sheet` | PDF gyártásilap (lazy cache) | 200 + `application/pdf` (SEC-05 headers) |
| GET | `/api/orders/{id}/items/{itemId}/sheet` | Egyedi item PDF | 200 + `application/pdf` |
| PUT | `/api/orders/{id}/revert` | → Draft (SEC-03: cache invalidáció) | 200 |

### 6.2 Orchestrator BFF — új route-ok

| Method | BFF Endpoint | Proxy target | Hozzáférés |
|---|---|---|---|
| PUT | `/bff/joinery/orders/{id}/submit` | Joinery :5002 | Publikus (authenticated) |
| GET | `/bff/joinery/orders/{id}` | Joinery :5002 | Publikus |
| GET | `/bff/joinery/orders/{id}/snapshots` | Joinery :5002 | Publikus |
| GET | `/bff/joinery/orders/{id}/sheet` | Joinery :5002 | Publikus |
| PUT | `/bff/joinery/orders/{id}/revert` | Joinery :5002 | Publikus |
| POST | `/internal/abstractions/calculate` | Abstractions :5003 | **Belső only** (SEC-01) |
| PUT | `/internal/joinery/results` | Joinery :5002 | **Belső only** (SEC-01) |

### 6.3 SEC-01 — Internal endpoint guard (Orchestrator)

```typescript
// src/middleware/internalGuard.ts
app.use('/internal/*', (req, res, next) => {
  if (req.headers['x-spaceos-internal'] !== process.env.INTERNAL_SECRET) {
    return res.status(403).json({ error: 'Internal endpoint' });
  }
  next();
});
```

A Portal route-jai NEM proxy-zzák az `/internal/` prefix-et. Soha.

### 6.4 Abstractions API (:5003) — ÚJ endpoint

| Method | Endpoint | Leírás |
|---|---|---|
| POST | `/api/templates/{name}/calculate` | Graph Engine calculate **NÉV** alapján (SEC-04: tenant validated) |

---

## 7. INFRASTRUCTURE — Kulcs komponensek (C# kód)

### 7.1 JoineryOutboxWorker

```csharp
// SpaceOS.Modules.Joinery.Infrastructure/Outbox/JoineryOutboxWorker.cs
public sealed class JoineryOutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JoineryOutboxWorker> _logger;
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(5));

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("JoineryOutboxWorker started");
        while (await _timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<JoineryDbContext>();
                var client = scope.ServiceProvider.GetRequiredService<IOrchestratorClient>();

                // SEC-02: FOR UPDATE SKIP LOCKED — nincs duplikált feldolgozás
                var pending = await db.JoineryOutboxEntries
                    .FromSqlRaw(@"
                        SELECT * FROM ""JoineryOutboxEntries""
                        WHERE ""ProcessedAt"" IS NULL
                          AND ""FailedAt"" IS NULL
                          AND ""RetryCount"" < 3
                        ORDER BY ""CreatedAt""
                        LIMIT 10
                        FOR UPDATE SKIP LOCKED")
                    .ToListAsync(ct).ConfigureAwait(false);

                foreach (var entry in pending)
                {
                    try
                    {
                        var result = await client.CalculateAsync(entry, ct).ConfigureAwait(false);
                        if (result.IsSuccess)
                            entry.MarkProcessed();
                        else
                            entry.IncrementRetry(result.Errors.First().ErrorMessage);
                    }
                    catch (Exception ex)
                    {
                        entry.IncrementRetry(ex.Message[..Math.Min(ex.Message.Length, 2000)]);
                    }
                }

                if (pending.Any())
                    await db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "OutboxWorker tick failed");
            }
        }
    }
}
```

### 7.2 IOrchestratorClient (BE-02: retry)

```csharp
// SpaceOS.Modules.Joinery.Infrastructure/Http/OrchestratorClient.cs
public sealed class OrchestratorClient : IOrchestratorClient
{
    private static readonly TimeSpan[] RetryDelays =
        { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5) };
    private readonly HttpClient _httpClient;

    public async Task<Result<CalculationResponse>> CalculateAsync(
        JoineryOutboxEntry entry, CancellationToken ct)
    {
        foreach (var (delay, attempt) in RetryDelays.Select((d, i) => (d, i)))
        {
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, ct).ConfigureAwait(false);

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(10)); // timeout

                var response = await _httpClient.PostAsJsonAsync(
                    "/internal/abstractions/calculate",
                    JsonSerializer.Deserialize<object>(entry.PayloadJson),
                    cts.Token).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                    return Result.Success(
                        await response.Content.ReadFromJsonAsync<CalculationResponse>(ct: ct)
                            .ConfigureAwait(false));

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
// SpaceOS.Modules.Joinery.Application/Commands/SaveCalculationResultCommand.cs
public async Task<Result> Handle(SaveCalculationResultCommand request, CancellationToken ct)
{
    // DB-03: régi snapshot IsLatest=false
    var oldSnapshot = await _db.CuttingListSnapshots
        .FirstOrDefaultAsync(s => s.DoorItemId == request.DoorItemId && s.IsLatest, ct)
        .ConfigureAwait(false);
    oldSnapshot?.MarkNotLatest();

    var snapshot = CuttingListSnapshot.Create(/* ... from request ... */);
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
// SpaceOS.Modules.Joinery.Infrastructure/EventHandlers/DoorOrderRevertedEventHandler.cs
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
// SpaceOS.Modules.Joinery.Infrastructure/Outbox/JoineryOutboxCleanupJob.cs
public sealed class JoineryOutboxCleanupJob : BackgroundService
{
    private readonly PeriodicTimer _timer = new(TimeSpan.FromHours(1));
    private readonly IServiceScopeFactory _scopeFactory;

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

## 8. PDF GYÁRTÁSILAP — QuestPDF

### 8.1 Interface

```csharp
// SpaceOS.Modules.Joinery.Domain/Services/IProductionSheetGenerator.cs
public interface IProductionSheetGenerator
{
    Stream Generate(DoorOrder order, IReadOnlyList<CuttingListSnapshot> snapshots);
}
```

### 8.2 QuestPDF implementáció

A PDF tartalmazzon:
- **Fejléc:** Rendelésszám, ügyfél, dátum, tenant logo placeholder
- **Szabászlista tábla:** Component | Anyag | Szélesség | Magasság | Vágás Sz. | Vágás M. | Vastagság | Db
- **CNC műveletek:** Component | Művelet | Pozíció | Átmérő | Mélység | Szög
- **Gyártási folyamat:** Fázis | Lépés | Leírás | Becsült idő
- **QR kód:** Order ID (opcionális, QuestPDF beépített)
- **Lábléc:** "SpaceOS — Gyártásilap — {dátum}"

### 8.3 Response headers (SEC-05 — deployment blocker)

```csharp
// API endpoint
app.MapGet("/api/orders/{id}/sheet", async (Guid id, IMediator mediator) =>
{
    var result = await mediator.Send(new GetProductionSheetQuery(id));
    if (!result.IsSuccess) return Results.NotFound();

    return Results.File(result.Value, "application/pdf",
        fileDownloadName: $"gyartasilap_{id:N}.pdf",
        enableRangeProcessing: false);
})
.Produces(200, contentType: "application/pdf");
// KÖTELEZŐ response headers (middleware vagy endpoint-ben):
// Content-Disposition: attachment
// X-Content-Type-Options: nosniff
// Cache-Control: private, no-store
```

### 8.4 PDF file cache pattern

```
/opt/spaceos/data/joinery/pdf/{tenantId}/{orderId}_{hash}.pdf
```

- Cache key: `SnapshotId` (1:1 ProductionSheetCache)
- Invalidáció: `DoorOrderReverted` event törli a fájlt + DB rekordot
- Lazy generation: PDF csak akkor renderelődik, ha nincs cache

### 8.5 Approved package

```xml
<PackageReference Include="QuestPDF" Version="2024.12.*" />
```

QuestPDF Community Edition — MIT, kereskedelmi < $1M annual revenue OK.

---

## 9. CORE 3 SEED — Abstractions DB

### 9.1 Template-ek

| Template | Slots | Connections | Parameters | Effort |
|---|---|---|---|---|
| FAF_T (félüveges ajtó, tok nélkül) | ~15 | ~20 | 7 | 2 nap (pattern) |
| FAF_Ü (félüveges ajtó, üveges) | ~18 | ~24 | 9 | 0.5 nap (klón) |
| BFAJ (belső furnér ajtó) | ~12 | ~16 | 7 | 0.5 nap (klón) |

### 9.2 Seed location

Abstractions repo: `Infrastructure/Seeding/DoorstarProductTemplateSeed.cs` — `IDataSeeder` implementáció.
A seed az **Abstractions DB-be** (spaceos_modules) megy, nem a Joinery-be.

A OutboxWorker a template **névvel** hivatkozik (pl. "FAF_T"), az Abstractions API név alapján keres.

---

## 10. FINDING ÖSSZESÍTŐ — 19 beépített finding

| ID | Súly | Terület | Probléma | Javítás |
|----|------|---------|----------|---------|
| DB-01 | 🟠 HIGH | DoorOrders.SnapshotId | FK constraint hiányzott | **SnapshotId eltávolítva** — status flag elegendő |
| DB-02 | 🟠 HIGH | SnapshotId szemantika | 1 Order N item = N snapshot, de 1 SnapshotId | SnapshotId oszlop törölve. Status = count check |
| DB-03 | 🟠 HIGH | Snapshot uniqueness | Recalc = duplikált | `IsLatest` + partial unique index |
| DB-04 | 🟡 MEDIUM | Outbox RetryCount | Nincs limit | CHECK(0..5) |
| DB-05 | 🟡 MEDIUM | Outbox partial index | Nem illeszkedik query-hez | Pending filter |
| DB-06 | 🟡 MEDIUM | FilePath traversal | Nincs DB CHECK | `!~ '^\.\.'` |
| SEC-01 | 🔴 CRITICAL | Internal endpoint | Portal hívhatja | `/internal/` + header guard |
| SEC-02 | 🔴 CRITICAL | OutboxWorker race | Duplikált snapshot | `FOR UPDATE SKIP LOCKED` |
| SEC-03 | 🟠 HIGH | PDF cache stale | Revert után régi PDF | Event handler: cache + file delete |
| SEC-04 | 🟠 HIGH | Cross-tenant template | Más tenant template-je | RLS + app check |
| SEC-05 | 🟠 HIGH | PDF response headers | Inline render + sniff | attachment + nosniff + no-store |
| SEC-06 | 🟠 HIGH | ContentHash | TenantId hiányzik | TenantId első hash elem |
| SEC-07 | 🟡 MEDIUM | Outbox PayloadJson | Üzleti adat | Dokumentálva; RLS véd |
| SEC-08 | 🟡 MEDIUM | QuestPDF input | Long name spoof | Max length + truncate |
| BE-01 | 🟠 HIGH | SaveCalculationResult race | Concurrent MarkCalculated | `[ConcurrencyCheck]` + retry |
| BE-02 | 🟠 HIGH | IOrchestratorClient | Nincs retry | 3× exponential backoff |
| BE-03 | 🟠 HIGH | MarkCalculated param | DB-02 után érvénytelen | Paraméter nélkül |
| BE-04 | 🟡 MEDIUM | Outbox cleanup | Tábla korlátlan | PeriodicTimer 1h, 7d retention |
| BE-05 | 🟡 MEDIUM | PDF MemoryStream | Nagy rendelés = nagy memória | Dokumentálva, 500 item limit |

---

## 11. VÉGREHAJTÁSI SORREND

| Nap | Feladat | Track | Repo | Függőség |
|-----|---------|-------|------|----------|
| 1 | Migration J-0002 DDL + RLS + indexek | A-DB | joinery | — |
| 2 | Domain: DoorOrderStatus + VO-k + events + CuttingListSnapshot | A-Domain | joinery | — |
| 3 | EF Core: JoineryDbContext bővítés + ConcurrencyCheck config | A-Infra | joinery | Nap 1-2 |
| 4 | JoineryOutboxWorker + JoineryOutboxEntry + OutboxCleanupJob | B-Outbox | joinery | Nap 1 |
| 5 | IOrchestratorClient (HTTP + retry) + SubmitDoorOrderCommandHandler (Outbox trigger) | B-Bridge | joinery | Nap 4 |
| 6 | SaveCalculationResultCommand + concurrency handling + IsLatest logic | B-Bridge | joinery | Nap 3-4 |
| 7 | Abstractions: POST /api/templates/{name}/calculate endpoint + tenant check | C-Abs | abstractions | — |
| 8 | Orchestrator: /internal/ prefix + guard + proxy routes | C-Orc | orchestrator | Nap 7 |
| 9 | IProductionSheetGenerator (QuestPDF) + PDF layout | D-PDF | joinery | Nap 3 |
| 10 | GetProductionSheetQuery + lazy cache + response headers | D-PDF | joinery | Nap 9 |
| 11 | DoorOrderRevertedEventHandler (cache invalidáció) + RevertToDraft handler | D-PDF | joinery | Nap 10 |
| 12 | FAF_T seed (Abstractions repo) — pattern kialakítás | E-Seed | abstractions | Nap 7 |
| 13 | FAF_Ü + BFAJ seed (klónok) + ITemplateValidator tesztek | E-Seed | abstractions | Nap 12 |
| 14-16 | Tesztek: ≥40 új (unit + integration), E2E snapshot | F-Test | Nap 1-13 |

---

## 12. DEFINITION OF DONE

### Migration gates

- [ ] Migration J-0002: 7 tábla/ALTER
- [ ] RLS FORCE + tenant_isolation policy 6 új táblán
- [ ] DB-03: `UX_CuttingListSnapshots_DoorItemId_Latest` partial unique index
- [ ] DB-04: OutboxEntries RetryCount CHECK(0..5)
- [ ] DB-05: Outbox partial index
- [ ] DB-06: ProductionSheetCache.FilePath path traversal CHECK
- [ ] DoorOrders: Status CHECK bővítve
- [ ] DoorOrders: Version column (BE-01)

### Domain gates

- [ ] `DoorOrderStatus` +3 enum
- [ ] `DoorOrder`: Submit, MarkCalculating, MarkCalculated, MarkCalculationFailed, RevertToDraft — FSM guards
- [ ] `DoorOrder.Version` `[ConcurrencyCheck]`
- [ ] `CuttingListSnapshot.Create()`: immutable, ContentHash incl. TenantId, max 200 lines, IsLatest
- [ ] `CuttingListLine`, `CncInstruction`, `ProcessStep` record VOs
- [ ] Domain events: Submitted, Calculated, CalculationFailed, Reverted
- [ ] `IDoorCalculationService` DEPRECATED (nem törölve, nem hívva)

### API + validation gates

- [ ] `PUT /api/orders/{id}/submit` → 202 + Outbox INSERT
- [ ] `GET /api/orders/{id}/snapshots` → latest snapshots
- [ ] `GET /api/orders/{id}/sheet` → PDF stream (SEC-05 headers)
- [ ] `PUT /api/orders/{id}/revert` → Draft + cache invalidáció
- [ ] Abstractions: `POST /api/templates/{name}/calculate` (SEC-04: tenant check)
- [ ] FluentValidation: InputWidth/Height > 0, <= 10000; TemplateName nem üres; ComponentName max 100

### Infrastructure gates

- [ ] `JoineryOutboxWorker`: PeriodicTimer 5s, `FOR UPDATE SKIP LOCKED`, graceful shutdown
- [ ] `IOrchestratorClient`: 3× retry exponential backoff, 10s timeout
- [ ] `IProductionSheetGenerator`: QuestPDF implementáció
- [ ] PDF file cache: `{tenantId}/{orderId}_{hash}.pdf` path pattern
- [ ] `DoorOrderRevertedEventHandler`: cache + file cleanup
- [ ] `JoineryOutboxCleanupJob`: 1h tick, 7d retention
- [ ] Orchestrator: `/internal/*` prefix + `X-SpaceOS-Internal` guard

### Seed gates

- [ ] FAF_T ProductTemplate: ~15 slot, ~20 connection, 7 parameter
- [ ] FAF_Ü ProductTemplate: FAF_T klón + üveg slots
- [ ] BFAJ ProductTemplate: egyszerűsített struktúra
- [ ] `ITemplateValidator.Validate()` PASS mindhárom template-re

### Security gates (DEPLOYMENT BLOCKER)

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

## 13. KOCKÁZATOK

| Kockázat | Valószínűség | Hatás | Mitigáció |
|----------|-------------|-------|-----------|
| QuestPDF Community licenc ($1M limit) | Low | Med | Professional $699/yr |
| FAF_T seed hibás offset értékek | Med | High | Doorstar manuális: 3 teszt rendelés |
| OutboxWorker deadlock | Low | Med | SKIP LOCKED + max 10 batch |
| Abstractions API nem elérhető | Low | High | Phase A+B DONE, csak 1 új endpoint |

---

## 14. MI JÖN UTÁNA

| Fázis | Tartalom | Függőség | Effort |
|---|---|---|---|
| Joinery v2.1 | Többi ajtótípus seed (~95%) | v2 DONE | 3 nap |
| KC01-03 | Keycloak IdP production auth | Független | 9 nap |
| Joinery v2.5 | Klasszikus nézet + Excel import | v2 + UX | 8 nap |
| Joinery v3 | CNC G-code export | v2.1 + specs | 10 nap |

---

*SpaceOS — Modules.Joinery v2 Implementation Package · 2026-04-10*
*Forrás: SpaceOS_Modules_Joinery_v2_Architecture_v4.md (19 finding, 4 review pass)*
