---
id: MSG-A044
from: architect
to: abstractions
type: task
priority: P0
date: 2026-04-09
sprint: "Modules.Abstractions v1 — Phase A-Core"
effort: "~15 nap"
---

# Modules.Abstractions v1 — Phase A-Core: Template + Graph Engine

## Kontextus

Teljesen új polyrepo. Ref: `/opt/spaceos/docs/SpaceOS_Modules_Abstractions_Architecture_v4.md`

**Cél:** Parametrikus termékgráf motor — ProductTemplate aggregate + ComponentSlot + SlotConnection + GraphCalculationEngine (Kahn's BFS) + Migration 0001 (DDL + RLS + triggers).

**Blokkoló:** Phase 3C+ DoD ✅ (Kernel migrations 0025-0026 megvannak)

---

## T1 — Repo scaffold (Nap 1)

### Solution létrehozás

```bash
cd /opt/spaceos
dotnet new sln -n SpaceOS.Modules.Abstractions -o spaceos-modules-abstractions
cd spaceos-modules-abstractions

dotnet new classlib -n SpaceOS.Modules.Abstractions.Domain         -f net8.0
dotnet new classlib -n SpaceOS.Modules.Abstractions.Application    -f net8.0
dotnet new classlib -n SpaceOS.Modules.Abstractions.Infrastructure -f net8.0
dotnet new webapi   -n SpaceOS.Modules.Abstractions.Api            -f net8.0
dotnet new xunit    -n SpaceOS.Modules.Abstractions.Tests          -f net8.0

dotnet sln add SpaceOS.Modules.Abstractions.Domain/SpaceOS.Modules.Abstractions.Domain.csproj
dotnet sln add SpaceOS.Modules.Abstractions.Application/SpaceOS.Modules.Abstractions.Application.csproj
dotnet sln add SpaceOS.Modules.Abstractions.Infrastructure/SpaceOS.Modules.Abstractions.Infrastructure.csproj
dotnet sln add SpaceOS.Modules.Abstractions.Api/SpaceOS.Modules.Abstractions.Api.csproj
dotnet sln add SpaceOS.Modules.Abstractions.Tests/SpaceOS.Modules.Abstractions.Tests.csproj
```

### Projekt referenciák

```bash
# Application → Domain
dotnet add SpaceOS.Modules.Abstractions.Application/SpaceOS.Modules.Abstractions.Application.csproj \
  reference SpaceOS.Modules.Abstractions.Domain/SpaceOS.Modules.Abstractions.Domain.csproj

# Infrastructure → Application + Domain
dotnet add SpaceOS.Modules.Abstractions.Infrastructure/SpaceOS.Modules.Abstractions.Infrastructure.csproj \
  reference SpaceOS.Modules.Abstractions.Application/SpaceOS.Modules.Abstractions.Application.csproj
dotnet add SpaceOS.Modules.Abstractions.Infrastructure/SpaceOS.Modules.Abstractions.Infrastructure.csproj \
  reference SpaceOS.Modules.Abstractions.Domain/SpaceOS.Modules.Abstractions.Domain.csproj

# Api → Infrastructure
dotnet add SpaceOS.Modules.Abstractions.Api/SpaceOS.Modules.Abstractions.Api.csproj \
  reference SpaceOS.Modules.Abstractions.Infrastructure/SpaceOS.Modules.Abstractions.Infrastructure.csproj

# Tests → Domain + Application + Infrastructure
dotnet add SpaceOS.Modules.Abstractions.Tests/SpaceOS.Modules.Abstractions.Tests.csproj \
  reference SpaceOS.Modules.Abstractions.Domain/SpaceOS.Modules.Abstractions.Domain.csproj
dotnet add SpaceOS.Modules.Abstractions.Tests/SpaceOS.Modules.Abstractions.Tests.csproj \
  reference SpaceOS.Modules.Abstractions.Application/SpaceOS.Modules.Abstractions.Application.csproj
dotnet add SpaceOS.Modules.Abstractions.Tests/SpaceOS.Modules.Abstractions.Tests.csproj \
  reference SpaceOS.Modules.Abstractions.Infrastructure/SpaceOS.Modules.Abstractions.Infrastructure.csproj
```

### NuGet csomagok

```bash
# Domain
dotnet add SpaceOS.Modules.Abstractions.Domain package Ardalis.Result --version 10.1.0

# Application
dotnet add SpaceOS.Modules.Abstractions.Application package MediatR --version 12.4.1
dotnet add SpaceOS.Modules.Abstractions.Application package FluentValidation --version 12.1.1
dotnet add SpaceOS.Modules.Abstractions.Application package Ardalis.Result --version 10.1.0
dotnet add SpaceOS.Modules.Abstractions.Application package Ardalis.Specification.EntityFrameworkCore --version 8.0.0

# Infrastructure
dotnet add SpaceOS.Modules.Abstractions.Infrastructure package Microsoft.EntityFrameworkCore --version 8.0.11
dotnet add SpaceOS.Modules.Abstractions.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.11
dotnet add SpaceOS.Modules.Abstractions.Infrastructure package MediatR --version 12.4.1

# Api
dotnet add SpaceOS.Modules.Abstractions.Api package FluentValidation.AspNetCore --version 11.3.0
dotnet add SpaceOS.Modules.Abstractions.Api package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.11
dotnet add SpaceOS.Modules.Abstractions.Api package Swashbuckle.AspNetCore --version 6.9.0

# Tests
dotnet add SpaceOS.Modules.Abstractions.Tests package xunit --version 2.9.3
dotnet add SpaceOS.Modules.Abstractions.Tests package xunit.runner.visualstudio --version 3.0.2
dotnet add SpaceOS.Modules.Abstractions.Tests package Microsoft.NET.Test.Sdk --version 17.12.0
dotnet add SpaceOS.Modules.Abstractions.Tests package Moq --version 4.20.72
dotnet add SpaceOS.Modules.Abstractions.Tests package FluentAssertions --version 6.12.2
dotnet add SpaceOS.Modules.Abstractions.Tests package Microsoft.EntityFrameworkCore.InMemory --version 8.0.11
dotnet add SpaceOS.Modules.Abstractions.Tests package Microsoft.AspNetCore.Mvc.Testing --version 8.0.11
```

### CLAUDE.md

Hozd létre: `spaceos-modules-abstractions/CLAUDE.md`

```markdown
# SpaceOS.Modules.Abstractions — CLAUDE.md

## Stack
- .NET 8, Clean Architecture + DDD + CQRS
- PostgreSQL 16 schema: spaceos_modules
- EF Core 8 + Npgsql 8.0.11

## Approved packages
MediatR 12.4.1 · FluentValidation 12.1.1 · Ardalis.Result 10.1.0 · Ardalis.Specification 8.0.0
EF Core 8.0.11 · Npgsql 8.0.11 · xUnit 2.9 · Moq 4.20.72 · FluentAssertions 6.12.2

## Layer dependency rule (hard constraint)
Domain ← Application ← Infrastructure ← Api
                                        ← Tests

## Golden Rules
1. Nincs public setter az aggregate-eken
2. Business logic csak Domain-ben
3. Minden mutáció domain eventet ráz
4. PopDomainEvents() + dispatch minden mutating handler végén
5. Result<T> minden handler return type-ja
6. ConfigureAwait(false) minden production async call-ban
7. AsNoTracking() minden read-only lekérdezésben
8. Math.Round(_, 1, MidpointRounding.AwayFromZero) minden kalkulációban (BE-01)
9. RuleOperator unknown → DomainException (SEC-03)
10. FileReference: regex validáció + nem kezdődhet / vagy ..-tal (SEC-02)
11. Kahn's iteratív BFS — NINCS rekurzív graph traversal (BE-02)
12. ITemplateValidator.Validate() minden template mentés előtt (BE-03)

## Mailbox
- Inbox: ./mailbox/inbox/
- Outbox: ./mailbox/outbox/
```

---

## T2 — Domain layer (Nap 1-4)

Ref: `/opt/spaceos/docs/SpaceOS_Modules_Abstractions_Architecture_v4.md` §3

### Enums (7 db)

`Domain/Enums/RuleOperator.cs`, `DimensionAxis.cs`, `JointType.cs`, `MachiningOperation.cs`, `ProcessPhase.cs`, `GeometryLevel.cs`, `SemanticRole.cs`

Pontos értékek: az architektúra doc §3 Enums szekciójából. Ezek CHECK constraint-ben is szerepelnek — kötelező egyezés.

### Domain base

```csharp
// Domain/Common/TenantScopedEntity.cs
public abstract class TenantScopedEntity
{
    public Guid Id { get; protected set; }
    public Guid TenantId { get; protected set; }

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void AddDomainEvent(IDomainEvent e) => _domainEvents.Add(e);
    public IReadOnlyList<IDomainEvent> PopDomainEvents()
    {
        var events = _domainEvents.ToList();
        _domainEvents.Clear();
        return events;
    }
}

public interface IDomainEvent { }
public class DomainException : Exception
{
    public DomainException(string msg) : base(msg) { }
}
```

### Value Objects

`Domain/ValueObjects/DimensionInput.cs`:
```csharp
public sealed record DimensionInput(decimal Width, decimal Height, decimal Depth);
```

`Domain/ValueObjects/ResolvedDimensions.cs`:
```csharp
public sealed record ResolvedDimensions(decimal Width, decimal Height, decimal Depth)
{
    public static ResolvedDimensions FromInput(DimensionInput i) => new(i.Width, i.Height, i.Depth);
}
```

### Entities (ComponentSlot, SlotConnection, TemplateParameter, GeometryAttachment)

Pontos implementáció: architektúra doc §3 alapján.

**ComponentSlot:**
- `static Create(...)` factory — validálja ComponentType (CHECK lista), Quantity 1-100
- Nincs public setter

**SlotConnection:**
- `static Create(...)` factory — validálja ParentSlotId ≠ ChildSlotId (app-szintű DB-02 tükrözése)
- JointType, MachiningOp, ProcessPhase: enum→string parse strict (SEC-03 mintájára)

**TemplateParameter:**
- `Key` max 50 char, `Value` decimal
- `UpdateValue(decimal)` — az egyetlen mutáló metódus

**GeometryAttachment:**
- `FileReference` validáció a Create factory-ban:
  ```csharp
  private static readonly Regex FileRefRegex =
      new(@"^[a-zA-Z0-9_\-/]+\.[a-z]{2,5}$", RegexOptions.Compiled);

  if (fileRef != null && (!FileRefRegex.IsMatch(fileRef) || fileRef.StartsWith('/') || fileRef.Contains("..")))
      return Result.Invalid("Invalid FileReference path"); // SEC-02
  ```

### ProductTemplate aggregate

Pontos implementáció: architektúra doc §3 `ProductTemplate` szekció.

Fontos guard-ok:
- `AddSlot()`: max 200 slot, validálja ComponentType
- `AddConnection()`: max 500, self-loop check, slot-ok template-ben legyenek
- `SetParameter()`: max 100, `UpdateValue()` ha már létezik

### Domain Service interfaces

```csharp
// Domain/Services/IProductCalculationEngine.cs
public interface IProductCalculationEngine
{
    CalculationResult Calculate(
        ProductTemplate template,
        DimensionInput root,
        IReadOnlyDictionary<string, decimal>? parameterOverrides = null);
}

// Domain/Services/IManufacturingDerivation.cs
public interface IManufacturingDerivation
{
    IReadOnlyList<CncOperation> DeriveCncPlan(CalculationResult result);
    IReadOnlyList<ProductionStep> DeriveProcessPlan(CalculationResult result);
}

// Domain/Services/ITemplateValidator.cs
public interface ITemplateValidator
{
    Result Validate(ProductTemplate template);
}
```

### Result records

```csharp
// Domain/Results/CuttingListItem.cs
public sealed record CuttingListItem(
    Guid SlotId, string SlotName, string ComponentType,
    decimal Width, decimal Height, decimal Depth,
    int Quantity, string? Material);

// Domain/Results/CncOperation.cs
public sealed record CncOperation(
    Guid SlotId, string SlotName,
    MachiningOperation Operation,
    decimal? GrooveDepth, decimal? GrooveWidth,
    decimal? DrillDiameter, decimal? DrillDepth,
    decimal? Angle, decimal? Radius,
    string? Note);

// Domain/Results/ProductionStep.cs
public sealed record ProductionStep(
    Guid SlotId, string SlotName,
    ProcessPhase Phase, int Order,
    JointType JointType, string? Note);

// Domain/Results/CalculationResult.cs
public sealed class CalculationResult
{
    public ProductTemplate Template { get; }
    public IReadOnlyDictionary<Guid, ResolvedDimensions> Dimensions { get; }
    public IReadOnlyList<CuttingListItem> CuttingList { get; }
    public IReadOnlyDictionary<string, decimal> Parameters { get; }

    public CalculationResult(
        ProductTemplate template,
        IReadOnlyDictionary<Guid, ResolvedDimensions> dims,
        IReadOnlyList<CuttingListItem> cuttingList,
        IReadOnlyDictionary<string, decimal> parameters)
    { Template = template; Dimensions = dims; CuttingList = cuttingList; Parameters = parameters; }
}
```

### Domain events (4 db)

```csharp
public record ProductTemplateCreated(Guid TemplateId, Guid TenantId, string TradeType, string Name) : IDomainEvent;
public record ProductTemplateVersioned(Guid NewTemplateId, Guid SourceTemplateId, int NewVersion) : IDomainEvent;
public record CalculationCompleted(Guid TemplateId, Guid TenantId, int CuttingListCount) : IDomainEvent;
public record GeometryAttached(Guid SlotId, Guid AttachmentId, GeometryLevel Level) : IDomainEvent;
```

---

## T3 — Infrastructure: DbContext + EF Configs + Migration 0001 (Nap 5-6)

### AbstractionsDbContext

```csharp
// Infrastructure/Persistence/AbstractionsDbContext.cs
public sealed class AbstractionsDbContext : DbContext
{
    public DbSet<ProductTemplate>    ProductTemplates    { get; set; } = null!;
    public DbSet<ComponentSlot>      ComponentSlots      { get; set; } = null!;
    public DbSet<SlotConnection>     SlotConnections     { get; set; } = null!;
    public DbSet<TemplateParameter>  TemplateParameters  { get; set; } = null!;
    public DbSet<GeometryAttachment> GeometryAttachments { get; set; } = null!;

    public AbstractionsDbContext(DbContextOptions<AbstractionsDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("spaceos_modules");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AbstractionsDbContext).Assembly);
    }
}
```

### EF Core konfigurációk

`Infrastructure/Persistence/Configurations/ProductTemplateConfiguration.cs` stb.

Az EF konfigurációk tükrözzék a DDL-t:
- `HasColumnType("varchar(30)")` stb.
- `ProductTemplate.Slots/_connections/_parameters` → `HasMany<>().WithOne()` owned collection (nem `OwnsMany` — ezek saját DbSet-en vannak)
- Minden string property esetén `HasMaxLength`

### TenantSessionInterceptor

Másold át a Modules.Joinery `TenantSessionInterceptor.cs`-ből (vagy hozd létre azonos logikával):
- `SavingChangesAsync`: `SET app.tenant_id = '{tenantId}'` a kapcsolaton

### Migration 0001

```bash
dotnet ef migrations add Migration_0001_ProductConfigurationEngine \
  --project SpaceOS.Modules.Abstractions.Infrastructure \
  --startup-project SpaceOS.Modules.Abstractions.Api \
  -- --environment Development
```

A migration fájlban **kötelező** `suppressTransaction: true` és az összes SQL a §4 DB Schema szekciójából:
- 5 tábla (DDL)
- Indexek
- RLS ENABLE + FORCE + POLICY mind az 5 táblán (SEC-01)
- `check_connection_dag()` trigger (DB-01)
- `prevent_version_change()` trigger (DB-03)

---

## T4 — GraphCalculationEngine (Nap 7-8)

Pontos implementáció: architektúra doc §3 `GraphCalculationEngine` szekciójából.

Fájl: `Infrastructure/Services/GraphCalculationEngine.cs`

**Kötelező:**
- Kahn's iteratív BFS — NINCS rekurzív traversal (BE-02)
- `Math.Round(_, 1, MidpointRounding.AwayFromZero)` (BE-01)
- Kör detektálás: ha `result.Count != slots.Count` → `DomainException` (DB-01)
- Unknown `RuleOperator` → `DomainException` (SEC-03)
- `CuttingOversize` TemplateParameter-ből feloldva, nem hardcoded (BE-04)
- Csak a nem-virtuális slotok kerülnek a CuttingList-be

---

## T5 — TemplateValidatorService (Nap 9)

Fájl: `Infrastructure/Services/TemplateValidatorService.cs`

```csharp
// Implementálja ITemplateValidator
// BE-03: három check:
// 1. Connected graph: minden slot elérhető a root-ból BFS-sel
// 2. Exactly 1 root: pontosan 1 slot amelynek nincs bejövő éle
// 3. No orphan slots: nincs slot amelynek sem ki-, sem bemenő éle nincs
//    (kivéve a root-ot — annak csak kimenő éle van)
```

---

## T6 — Application CQRS (Nap 10-11)

### Commands + Handlers

| Command | Handler feladata |
|---|---|
| `CreateProductTemplateCommand` | `ProductTemplate.Create()` → persist → dispatch events |
| `AddComponentSlotCommand` | Betölti template-et → `AddSlot()` → persist |
| `AddSlotConnectionCommand` | Betölti template-et → `AddConnection()` → `ITemplateValidator.Validate()` → persist |
| `SetTemplateParameterCommand` | Betölti template-et → `SetParameter()` → persist |
| `CloneProductTemplateCommand` | Másolja template-et, new Version = max+1, **target TenantId = JWT TenantId** (SEC-05) |
| `CalculateProductCommand` | Betölti template-et → `IProductCalculationEngine.Calculate()` → persist result → event |

### Queries + Handlers

| Query | Handler |
|---|---|
| `GetProductTemplateQuery` | Ardalis.Specification, tenant filter |
| `ListProductTemplatesQuery` | Paged, `IsActive=true`, `IsArchived=false` |
| `GetTemplateGraphQuery` | Template + összes Slot + Connection betöltve |
| `GetCuttingListQuery` | Utolsó kalkuláció CuttingList-je |

### FluentValidation validátorok

Minden command-hoz validator:
- `CreateProductTemplateCommand`: TradeType `IN ('door','cabinet','window','generic')`, Name nem üres
- `AddComponentSlotCommand`: ComponentType CHECK lista, Quantity 1-100
- `AddSlotConnectionCommand`: Axis enum, Operator enum, ParentSlotId ≠ ChildSlotId
- `CalculateProductCommand`: Width/Height/Depth > 0

---

## T7 — API layer (Nap 12)

### Program.cs

Mintája: a Modules.Joinery `Program.cs` — azonos struktúra:
- `AddApplication()` + `AddInfrastructure(connectionString)`
- JWT Bearer: `JWT_AUTHORITY` + `JWT_AUDIENCE` env var-ból
- `ManufacturerOnly` policy: `tenant_type = "Manufacturer"` claim
- Auto-migrate startup: `db.Database.MigrateAsync()`
- Health endpoint: `GET /health`
- Port: `http://127.0.0.1:5003` (a Joinery már foglalja az 5002-t)

### Endpoints

Fájl: `Api/Endpoints/ProductTemplateEndpoints.cs`

Az összes endpoint az §5 API Surface szekciójából.

Fontos:
- `GET /api/modules/templates/{id}/cutting-list` → `Cache-Control: no-store` (SEC-05 Joinery mintájára)
- Minden endpoint: `[Authorize(Policy = "ManufacturerOnly")]`

---

## T8 — Tesztek (Nap 13-15)

Célszám: **≥60 teszt**

### Graph tesztek (`Tests/Graph/`)

**TopologicalSortTests.cs** (≥8 teszt):
```
- LinearChain_SortedCorrectly (A→B→C → [A,B,C])
- TwoChildrenFromRoot (root→B, root→C → root first)
- Diamond_SortedCorrectly (root→B→leaf, root→C→leaf)
- EmptyTemplate_ReturnsEmpty
- SingleSlot_ReturnsSingleItem
- SortIsDeteministic_SameInputSameOutput
- ThreeLevel_DeepChain_NoStackOverflow (50 slot)
- MaxDepth_200Slots_CompletesUnder100ms (perf guard)
```

**CycleDetectionTests.cs** (≥5 teszt):
```
- DirectCycle_A→B→A_ThrowsDomainException
- IndirectCycle_A→B→C→A_ThrowsDomainException
- NoCycle_ValidDAG_DoesNotThrow
- SelfLoop_AddConnection_ReturnsForbidden (domain layer)
- DagWithDiamond_NoCycle_DoesNotThrow
```

**DimensionPropagationTests.cs** (≥8 teszt):
```
- Identity_ChildEqualsParent
- Subtract_ChildIsParentMinusOperand
- Add_ChildIsParentPlusOperand
- SubtractN_ChildIsParentMinusNTimesOperand
- Max_ChildIsMaxOfTwoParentsMinusOperand
- Min_ChildIsMinOfTwoParentsMinusOperand
- Constant_ChildIsOperand_RegardlessOfParent
- UnknownOperator_ThrowsDomainException (SEC-03)
```

**DoorFafTFullPathwayTests.cs** (≥5 teszt):
```csharp
// FAF_T ajtó teljes gráf:
// Root(900×2100) → BKM-panel(Identity W, Identity H) → Ajtólap(Identity W, Subtract 6 H)
// → FrameCore-Alap(Subtract 8 W, Subtract 4 H)
// Ellenőrzés: CuttingList tartalmaz ≥3 elemet, BKM-panel.Width = 900
// Doorstar FAF_T: CuttingOversize = 1mm, minden méret 1 tizedessel
```

### Manufacturing tesztek (`Tests/Manufacturing/`) — előkészítés B fázisra

**Placeholder fájlok** (üresek, majd B fázisban töltjük):
- `CncDerivationTests.cs`
- `ProcessPlanTests.cs`

### Validation tesztek (`Tests/Validation/`)

**TemplateValidatorTests.cs** (≥5 teszt):
```
- ValidTemplate_ConnectedDAG_ReturnsSuccess
- NoRootSlot_ReturnsInvalid (minden slotnak van szülője)
- MultipleRoots_ReturnsInvalid
- OrphanSlot_ReturnsInvalid
- DisconnectedGraph_ReturnsInvalid
```

**ConnectionRuleTests.cs** (≥5 teszt):
```
- SelfLoop_AddConnection_ReturnsError
- MaxConnections_501st_ReturnsError
- MaxSlots_201st_AddSlot_ReturnsError
- InvalidComponentType_Create_ReturnsInvalid
- InvalidOperand_Negative_IsAllowed (Constant-nál OK)
```

### Security tesztek (`Tests/Security/`)

**CrossTenantTests.cs** (≥4 teszt):
```
- CloneTemplate_TargetTenantIdAlwaysJwtTenantId (SEC-05)
- CreateTemplate_TenantIdFromJwt (nem kérésből)
- AddSlot_WrongTenant_Forbidden
- Calculate_WrongTenant_Forbidden
```

**FilePathTraversalTests.cs** (≥5 teszt):
```
- ValidPath_geo/door_frame.step_ReturnsSuccess
- AbsolutePath_StartsWithSlash_ReturnsInvalid (SEC-02)
- ParentTraversal_DotDot_ReturnsInvalid (SEC-02)
- NullPath_AllowedForOptionalField (SEC-02: nullable)
- InvalidExtension_exe_ReturnsInvalid (SEC-02)
```

---

## DoD gates (Phase A-Core)

```bash
cd /opt/spaceos/spaceos-modules-abstractions
dotnet build 2>&1 | tail -3      # 0 error, 0 warning
dotnet test --no-build 2>&1 | tail -5  # ≥60 pass, 0 fail
```

DB ellenőrzés (migration után):
```bash
PGPASSWORD=spaceos_db_pass psql -U spaceos -h 127.0.0.1 -p 5433 \
  -c "SELECT table_name FROM information_schema.tables WHERE table_schema = 'spaceos_modules';"
# → 5 tábla: ProductTemplates, ComponentSlots, SlotConnections, TemplateParameters, GeometryAttachments

PGPASSWORD=spaceos_db_pass psql -U spaceos -h 127.0.0.1 -p 5433 \
  -c "SELECT tablename, rowsecurity, forcerowsecurity FROM pg_tables WHERE schemaname = 'spaceos_modules';"
# → rowsecurity=t, forcerowsecurity=t mind az 5 táblán
```

---

## Output

Ha kész: `docs/mailbox/abstractions/outbox/2026-04-09_044_abstractions-phase-a-core-done.md`

Visszajelzés tartalma:
- Teszt összesítő (Passed/Failed)
- DB migration státusz (5 tábla, RLS, triggers)
- DoD gate-ek checklistje
- Esetleges eltérések az architektúra doc-tól
