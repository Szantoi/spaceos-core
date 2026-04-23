---
id: MSG-CUTTING-005
from: root
to: cutting
type: task
priority: critical
status: READ
ref: MSG-CUTTING-004-DONE
created: 2026-04-16
---

# MSG-CUTTING-005 — Panel méret fix: PanelStockDto WidthMm + HeightMm

## Probléma

A `NestingService.ComputeNesting()` jelenleg **hardcode 2800×2070mm** értékkel dolgozik a full panel méretekre, mert a `PanelStockDto` nem tartalmaz méretadatot. Ez helytelen — valóságban sok különböző tábla méret létezik (pl. 2800×2070, 2440×1220, 3050×1220, custom méretek stb.).

A panel méretét a `MaterialCatalog` tartalmazza. A `GetStockAsync()` handler ezt már lekérdezi (JOIN-olva), de a `PanelStockDto`-ban nem adja vissza.

## Feladat

### 1. Contracts frissítés — `SpaceOS.Modules.Inventory.Contracts` → **1.1.0**

`PanelStockDto` bővítése:

```csharp
public record PanelStockDto(
    string MaterialType,
    int FullPanelCount,
    int WidthMm,       // ← ÚJ: MaterialCatalog.WidthMm
    int HeightMm,      // ← ÚJ: MaterialCatalog.HeightMm
    IReadOnlyList<OffcutDto> Offcuts
);
```

`OffcutDto` szintén kell méretet tartalmazzon (ha még nem tartalmazza):

```csharp
public record OffcutDto(
    Guid Id,
    string MaterialType,
    int WidthMm,   // ← valós offcut méret (nem catalog)
    int HeightMm,
    Guid OriginCuttingSheetId
);
```

**Contracts verzió bump:** `SpaceOS.Modules.Inventory.Contracts.csproj` → `<Version>1.1.0</Version>`

### 2. Inventory Application — `GetStockQueryHandler` frissítése

A `GetStockAsync()` implementáció JOIN-olja a `MaterialCatalog`-ot és töltse ki a `WidthMm` + `HeightMm` mezőket:

```csharp
// GetStockQueryHandler.cs — a lekérdezésben MaterialCatalog JOIN
var stocks = await _db.PanelStocks
    .AsNoTracking()
    .Include(s => s.Material)   // MaterialCatalog
    .Where(s => s.TenantId == tenantId)
    .ToListAsync(ct);

// DTO mapping:
new PanelStockDto(
    MaterialType: s.Material.MaterialType,
    FullPanelCount: s.FullPanelCount,
    WidthMm: s.Material.WidthMm,       // ← MaterialCatalog-ból
    HeightMm: s.Material.HeightMm,     // ← MaterialCatalog-ból
    Offcuts: offcuts
)
```

### 3. NestingService — hardcode eltávolítása

Az `AvailablePanel` DTO és a `NestingService.ComputeNesting()` már helyesen kapja a méreteket paraméterként — csak a **handler oldali DTO mapping kell javítani**:

`GetNestingResultQueryHandler.cs`:

```csharp
// JELENLEG (HELYTELEN):
var panels = stocks.Select(s => new AvailablePanel(
    PanelStockId: s.Id,
    MaterialType: s.MaterialType,
    WidthMm: 2800,    // ← HARDCODE — TÖRÖLNI
    HeightMm: 2070,   // ← HARDCODE — TÖRÖLNI
    IsOffcut: false,
    AreaMm2: 2800 * 2070
)).ToList();

// HELYES:
var panels = stocks.Select(s => new AvailablePanel(
    PanelStockId: s.Id,
    MaterialType: s.MaterialType,
    WidthMm: s.WidthMm,         // ← PanelStockDto-ból
    HeightMm: s.HeightMm,       // ← PanelStockDto-ból
    IsOffcut: false,
    AreaMm2: s.WidthMm * s.HeightMm
)).ToList();
```

Offcut-ok esetén az `OffcutDto.WidthMm` / `HeightMm` már az offcut tényleges mérete (nem a catalog mérete).

### 4. NuGet csomag újracsomagolás

```bash
dotnet pack src/SpaceOS.Modules.Inventory.Contracts/ -c Release
# → SpaceOS.Modules.Inventory.Contracts.1.1.0.nupkg
# A LocalInventory NuGet forrásba bemásolni (ahogy 1.0.0 is ment)
```

### 5. Tesztek frissítése

- `NestingServiceTests.cs` — a mock `AvailablePanel`-ek kapjanak valós, különböző méreteket (pl. 2800×2070, 2440×1220) — ne legyen implicit egységes méret feltételezés
- `GetNestingResultHandlerTests.cs` — a mock `PanelStockDto`-k tartalmazzanak `WidthMm` + `HeightMm` értéket
- Új teszt: 2 különböző méretű tábla a pool-ban → a nesting a megfelelő méretű táblát választja

### 6. Build + test gate

```bash
dotnet build   → 0 error, 0 warning
dotnet test    → ≥151 pass (meglévők) + legalább 1 új "különböző méretű tábla" teszt
```

## DoD

- [ ] `PanelStockDto` tartalmaz `WidthMm` + `HeightMm`
- [ ] `OffcutDto` tartalmaz `WidthMm` + `HeightMm`
- [ ] `SpaceOS.Modules.Inventory.Contracts` verzió: `1.1.0`
- [ ] `GetStockQueryHandler` a `MaterialCatalog`-ból tölti ki a méreteket
- [ ] `GetNestingResultQueryHandler` — NINCS hardcode 2800/2070
- [ ] NuGet 1.1.0 becsomagolva a LocalInventory forrásba
- [ ] `dotnet build` → 0 error, 0 warning
- [ ] `dotnet test` → ≥152 pass (≥1 új "különböző méretű tábla" teszt)
- [ ] Outbox: `MSG-CUTTING-005-DONE`
