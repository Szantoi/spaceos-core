---
id: MSG-A045
from: architect
to: abstractions
type: task
priority: P0
date: 2026-04-09
sprint: "Modules.Abstractions v1 — Phase B-Manufacturing"
effort: "~8 nap"
---

# Modules.Abstractions v1 — Phase B-Manufacturing: CNC + Process Plan + FAF_T Seed

## Kontextus

Phase A-Core DONE: 46/46 teszt, Migration 0001 alkalmazva, spaceos_modules schema él.

Repo: `/opt/spaceos/spaceos-modules-abstractions/`
Ref: `/opt/spaceos/docs/SpaceOS_Modules_Abstractions_Architecture_v4.md`

**Cél:** Gyártási nézetek (CNC + Process Plan) deriválása a ProductTemplate gráfból, Doorstar FAF_T ProductTemplate seed.

---

## T1 — ManufacturingDerivationService (Nap 16-17)

### Fájl: `Infrastructure/Services/ManufacturingDerivationService.cs`

Implementálja: `IManufacturingDerivation` (Domain/Services/IManufacturingDerivation.cs)

```csharp
public sealed class ManufacturingDerivationService : IManufacturingDerivation
{
    // IReadOnlyList<CncOperation> DeriveCncPlan(CalculationResult result)
    // IReadOnlyList<ProductionStep> DeriveProcessPlan(CalculationResult result)
}
```

### DeriveCncPlan(result) — Nézet 2: CNC műveleti terv

Logika:
- Csak a nem-virtuális slot-ok kerülnek a listába (`!slot.IsVirtual`)
- Minden slot bemenő connection-jeit vizsgálja (`template.Connections.Where(c => c.ChildSlotId == slot.Id)`)
- `MachiningOperation.None` → kihagyja (nem kerül CncOperation-be)
- Minden más `MachiningOp` → `CncOperation` rekord:
  ```csharp
  new CncOperation(
      SlotId: slot.Id,
      SlotName: SanitizeSlotName(slot.Name),  // SEC-07
      Operation: conn.MachiningOp,
      GrooveDepth: conn.GrooveDepth,
      GrooveWidth: conn.GrooveWidth,
      DrillDiameter: conn.DrillDiameter,
      DrillDepth: conn.DrillDepth,
      Angle: conn.Angle,
      Radius: conn.Radius,
      Note: conn.JointNote
  )
  ```
- Sorrend: topológiai (a CalculationResult Dimensions kulcs-sorrendjéből, ami már sorted)

**SEC-07 — ComponentName sanitize:**
```csharp
private static readonly Regex SlotNameSanitizer =
    new(@"[^a-zA-ZáéíóöőúüűÁÉÍÓÖŐÚÜŰ0-9 _\-]", RegexOptions.Compiled);

private static string SanitizeSlotName(string name) =>
    SlotNameSanitizer.Replace(name.Length > 100 ? name[..100] : name, string.Empty);
```

### DeriveProcessPlan(result) — Nézet 3: Gyártási folyamat

Logika:
- Minden slot (virtuális is!) → `ProductionStep`
- Sorrend: topológiai sorrend a `CalculationResult.Dimensions` kulcsaiból
  - A Kahn's sort már megadja a sorrendet: `result.Dimensions.Keys` sorrendje = topológiai sorrend
- `ProcessPhase` forrása: a slot bemenő connection-jének `ProcessPhase`
  - Ha nincs bemenő connection (root slot) → `ProcessPhase.Design`
- `Order` = 0-tól szekvenciálisan növekvő
- `JointType` forrása: a bemenő connection `JointType` (root esetén `JointType.Offset`)

```csharp
new ProductionStep(
    SlotId: slot.Id,
    SlotName: SanitizeSlotName(slot.Name),
    Phase: incomingConn?.ProcessPhase ?? ProcessPhase.Design,
    Order: order++,
    JointType: incomingConn?.JointType ?? JointType.Offset,
    Note: incomingConn?.JointNote
)
```

**Fontos:** Egy slotnak lehet több bemenő connection (különböző tengelyek). Ilyenkor az első nem-Design ProcessPhase-t vedd, vagy ha mind Design → `ProcessPhase.Design`.

### DI regisztráció: `Infrastructure/InfrastructureServiceExtensions.cs`

```csharp
services.AddScoped<IManufacturingDerivation, ManufacturingDerivationService>();
```

---

## T2 — Application CQRS: GetCncPlanQuery + GetProcessPlanQuery (Nap 18)

### Fájl: `Application/Calculation/Queries/GetCncPlanQuery.cs`

```csharp
public sealed record GetCncPlanQuery(
    Guid TemplateId, Guid TenantId,
    decimal Width, decimal Height, decimal Depth) : IRequest<Result<IReadOnlyList<CncOperation>>>;

// Handler: betölti template-et → Calculate() → DeriveCncPlan()
```

### Fájl: `Application/Calculation/Queries/GetProcessPlanQuery.cs`

```csharp
public sealed record GetProcessPlanQuery(
    Guid TemplateId, Guid TenantId,
    decimal Width, decimal Height, decimal Depth) : IRequest<Result<IReadOnlyList<ProductionStep>>>;

// Handler: betölti template-et → Calculate() → DeriveProcessPlan()
```

### Handler logika (mindkettőnél):

```csharp
var template = await _repository.GetTemplateWithAllAsync(request.TemplateId, ct);
if (template == null) return Result<...>.NotFound(...);
if (template.TenantId != request.TenantId) return Result<...>.Forbidden();

var root = new DimensionInput(request.Width, request.Height, request.Depth);
var calcResult = _engine.Calculate(template, root);
var plan = _derivation.DeriveCncPlan(calcResult);  // vagy DeriveProcessPlan
return Result<...>.Success(plan);
```

---

## T3 — API endpoints (Nap 18)

### Fájl: `Api/Endpoints/ProductTemplateEndpoints.cs` — két új endpoint

```csharp
// GET /api/modules/templates/{id}/cnc-plan?w=900&h=2100&d=40
group.MapGet("{id:guid}/cnc-plan", async (
    Guid id, IMediator mediator, HttpContext ctx, HttpResponse response,
    [FromQuery] decimal w, [FromQuery] decimal h, [FromQuery] decimal d,
    CancellationToken ct) =>
{
    response.Headers["Cache-Control"] = "no-store";
    var tenantId = GetTenantId(ctx);
    if (tenantId == null) return Results.Forbid();
    var result = await mediator.Send(new GetCncPlanQuery(id, tenantId.Value, w, h, d), ct);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors);
});

// GET /api/modules/templates/{id}/process-plan?w=900&h=2100&d=40
group.MapGet("{id:guid}/process-plan", async (
    Guid id, IMediator mediator, HttpContext ctx, HttpResponse response,
    [FromQuery] decimal w, [FromQuery] decimal h, [FromQuery] decimal d,
    CancellationToken ct) =>
{
    response.Headers["Cache-Control"] = "no-store";
    var tenantId = GetTenantId(ctx);
    if (tenantId == null) return Results.Forbid();
    var result = await mediator.Send(new GetProcessPlanQuery(id, tenantId.Value, w, h, d), ct);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors);
});
```

---

## T4 — Doorstar FAF_T ProductTemplate Seed (Nap 19-20)

### Fájl: `Application/Seeding/ITemplateSeeder.cs`

```csharp
public interface ITemplateSeeder
{
    Task SeedAsync(CancellationToken ct = default);
}
```

### Fájl: `Infrastructure/Seeding/FafTTemplateSeeder.cs`

Implementálja `ITemplateSeeder`.

**Doorstar tenant UUID:** `a1b2c3d4-e5f6-7890-abcd-ef1234567890`

**FAF_T ProductTemplate struktúra** (egyszer, idempotens — ha már létezik, skip):

```
Neve: "FAF_T"
TradeType: "door"
TenantId: a1b2c3d4-e5f6-7890-abcd-ef1234567890

TemplateParameter:
  - CuttingOversize = 1.0

Slot-ok (sorrendben):
  1. "Root"         ComponentType=Root,      IsVirtual=true,  Qty=1, SortOrder=0
  2. "BKM-panel"    ComponentType=Panel,     IsVirtual=false, Qty=1, DefaultMaterial="MDF", DefaultThickness=18, SortOrder=1
  3. "Ajtólap"      ComponentType=Door,      IsVirtual=false, Qty=1, DefaultMaterial="MDF", DefaultThickness=18, SortOrder=2
  4. "FrameCore-Alap" ComponentType=FrameCore, IsVirtual=false, Qty=2, DefaultMaterial="Solid", DefaultThickness=40, SortOrder=3
  5. "Ajtó-Él-V"    ComponentType=Edge,      IsVirtual=false, Qty=2, DefaultMaterial="ABS", DefaultThickness=2, SortOrder=4
  6. "Ajtó-Él-F"    ComponentType=Edge,      IsVirtual=false, Qty=2, DefaultMaterial="ABS", DefaultThickness=2, SortOrder=5

Connection-ök (Operator, Operand, Axis, JointType, MachiningOp, ProcessPhase):
  Root → BKM-panel:
    - Width:  Identity 0, Offset, None, Design
    - Height: Identity 0, Offset, None, Design

  BKM-panel → Ajtólap:
    - Width:  Identity 0,   Butt,  Cut,    Cutting
    - Height: Subtract 6,   Butt,  Cut,    Cutting   (felső+alsó keret = 3+3mm)

  Ajtólap → FrameCore-Alap:
    - Width:  Subtract 8,  Dado,  Groove, CNC     (2×4mm oldallap)
    - Height: Subtract 4,  Dado,  Groove, CNC     (2×2mm)

  Ajtólap → Ajtó-Él-V:
    - Height: Identity 0,  EdgeBand, EdgeBand, EdgeBanding
    - Depth:  Constant 2,  EdgeBand, EdgeBand, EdgeBanding

  Ajtólap → Ajtó-Él-F:
    - Width:  Identity 0,  EdgeBand, EdgeBand, EdgeBanding
    - Depth:  Constant 2,  EdgeBand, EdgeBand, EdgeBanding
```

**Idempotencia:**
```csharp
// Ellenőrzés: EXISTS ProductTemplates WHERE TenantId = doorstarTenant AND Name = "FAF_T"
// Ha igen → skip
var exists = await _db.ProductTemplates
    .AnyAsync(t => t.TenantId == DoorstarTenantId && t.Name == "FAF_T", ct);
if (exists) return;
```

**Fontos:** A seed közvetlen DB-be ír (bypass RLS), ezért `SET app.tenant_id` szükséges a connection-re írás előtt:
```csharp
await _db.Database.ExecuteSqlRawAsync(
    $"SET app.tenant_id = '{DoorstarTenantId}'", ct);
```

### Regisztráció: `InfrastructureServiceExtensions.cs`

```csharp
services.AddScoped<ITemplateSeeder, FafTTemplateSeeder>();
```

### Program.cs startup hook

Másold a Joinery mintájára — `ApplicationStarted` eseményre:

```csharp
app.Lifetime.ApplicationStarted.Register(async () =>
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AbstractionsDbContext>();
    if (!db.Database.IsRelational()) return;

    var seeder = scope.ServiceProvider.GetRequiredService<ITemplateSeeder>();
    await seeder.SeedAsync(CancellationToken.None).ConfigureAwait(false);
});
```

---

## T5 — Tesztek (Nap 19-20)

Placeholder fájlok már megvannak. Töltsd fel őket!

Célszám: **≥15 új teszt** (Phase B összesen, a 46 mellé)

### `Tests/Manufacturing/CncDerivationTests.cs` (≥8 teszt)

```
- SingleGrooveConnection_ProducesCncOperation
- NoneOperation_SkippedInCncPlan
- MultipleConnectionsPerSlot_AllOperationsDerived
- VirtualSlot_ExcludedFromCncPlan
- SlotNameSanitized_NoSpecialChars (SEC-07)
- GrooveParameters_CorrectlyPropagated (GrooveDepth, GrooveWidth)
- DrillParameters_CorrectlyPropagated (DrillDiameter, DrillDepth)
- FafTTemplate_CncPlan_ContainsGrooveOperations
```

### `Tests/Manufacturing/ProcessPlanTests.cs` (≥7 teszt)

```
- RootSlot_HasDesignPhase_Order0
- LinearChain_OrderedTopologically
- ProcessPhase_Cutting_BeforeEdgeBanding (Cutting=1 < EdgeBanding=5 sorrendben)
- AllSlots_IncludingVirtual_InProcessPlan
- PhaseOrder_Reflects_TopologicalSort
- FafTTemplate_ProcessPlan_HasCuttingAndCnc
- MultiplePhases_SortedByTopologicalOrder
```

**Teszt segéd — FafT template builder:**
```csharp
// Hozz létre egy BuildFafTTemplate() helper metódust (ugyanaz mint a DoorFafTFullPathwayTests-ben)
// vagy tedd ki egy közös TestHelpers/TemplateBuilder.cs-be
```

---

## DoD gates (Phase B)

```bash
cd /opt/spaceos/spaceos-modules-abstractions
dotnet build 2>&1 | tail -3      # 0 error, 0 warning
dotnet test --no-build 2>&1 | tail -5  # ≥61 pass (46 + ≥15), 0 fail
```

**Funkcionális ellenőrzés:**
```bash
# Service indítás (csak ellenőrzés, nem kell live-on futtatni):
# Program.cs startup hook lefut → FafTTemplateSeeder futtat
# Ellenőrzés:
PGPASSWORD=spaceos_db_pass psql -U spaceos -h 127.0.0.1 -p 5433 -d spaceos \
  -c "SET app.tenant_id = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890'; SELECT name, tradetype, version FROM spaceos_modules.\"ProductTemplates\";"
# → FAF_T, door, 1
```

---

## Output

Ha kész: `mailbox/outbox/2026-04-09_045_abstractions-phase-b-manufacturing-done.md`

Tartalom:
- Teszt összesítő (Passed/Failed — min 61)
- FAF_T seed DB ellenőrzés kimenete
- ManufacturingDerivationService implementált metódusok listája
- Esetleges eltérések az architektúra doc-tól
