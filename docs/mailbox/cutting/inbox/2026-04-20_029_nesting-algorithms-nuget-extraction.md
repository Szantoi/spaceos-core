---
id: MSG-CUTTING-029
from: root
to: cutting
type: task
priority: high
status: READ
ref: SpaceOS_Modules_Contracts_Architecture_v4_2.md
created: 2026-04-20
---

# CUTTING-029 — SpaceOS.Nesting.Algorithms NuGet + Cutting modul integráció

> **Skill:** `/spaceos-terminal` szerint dolgozz — inbox → build → test → outbox DONE
> **Arch spec:** `docs/architecture/SpaceOS_Modules_Contracts_Architecture_v4_2.md` (szekció 3/E — APPROVED)
> **Timeline:** ~1.25 nap
> **Blokkoló:** ABSTRACTIONS-010 (Contracts 1.3.0) DONE kell mielőtt az integráció elkezdődhet

---

## Kontextus

A FreeTier.Api (v1.5, Q3 2026) közvetlenül fogyasztja a nesting algoritmust — a teljes Cutting modul importálása nélkül. Ezért az algoritmusokat ki kell szervezni egy önálló NuGet csomagba.

**Feltétel:** Contracts 1.3.0 (ABSTRACTIONS-010) DONE után kezd el az E5 integrációt.

---

## Feladat 1: Új repo + `SpaceOS.Nesting.Algorithms` NuGet (E1–E4)

### Projekt létrehozás

```bash
mkdir spaceos-nesting-algorithms
cd spaceos-nesting-algorithms
dotnet new sln -n SpaceOS.Nesting.Algorithms
dotnet new classlib -n SpaceOS.Nesting.Algorithms -f net8.0
dotnet new xunit -n SpaceOS.Nesting.Algorithms.Tests -f net8.0
dotnet sln add SpaceOS.Nesting.Algorithms/SpaceOS.Nesting.Algorithms.csproj
dotnet sln add SpaceOS.Nesting.Algorithms.Tests/SpaceOS.Nesting.Algorithms.Tests.csproj
```

**Helye:** `/opt/spaceos/spaceos-nesting-algorithms/` (új repo — a Cutting repo-tól FÜGGETLEN)

### csproj konfiguráció

```xml
<PackageId>SpaceOS.Nesting.Algorithms</PackageId>
<Version>1.0.0</Version>
<!-- NINCS SpaceOS.Modules.Contracts függőség — szándékosan független -->
```

### Struktúra

```
SpaceOS.Nesting.Algorithms/
  INestingStrategy.cs
  NestingStrategyFactory.cs
  Models/
    NestingInput.cs        (IReadOnlyList<NestingPart> Parts, IReadOnlyList<AvailablePanel> Panels, int SawBladeGapMm = 4)
    NestingPart.cs         (string PartId, string Name, decimal WidthMm, decimal HeightMm, bool CanRotate = true, int Quantity = 1)
    AvailablePanel.cs      (string PanelId, string MaterialCode, decimal WidthMm, decimal HeightMm, bool IsOffcut)
    NestingResult.cs       (IReadOnlyList<PanelAssignment> Assignments, IReadOnlyList<NestingPart> UnplacedParts, decimal TotalWastePercentage, int PanelsUsed, string AlgorithmUsed, TimeSpan ComputationTime)
    PlacedPart.cs          (string PartId, string Name, decimal X, decimal Y, decimal WidthMm, decimal HeightMm, bool IsRotated)
    PanelAssignment.cs     (string PanelId, string MaterialCode, decimal PanelWidthMm, decimal PanelHeightMm, IReadOnlyList<PlacedPart> PlacedParts, decimal WasteAreaMm2, decimal UtilizationPercent)
  Strategies/
    FfdhNestingStrategy.cs
    GuillotineNestingStrategy.cs
    MaxRectsNestingStrategy.cs
```

---

### `INestingStrategy` interfész

```csharp
namespace SpaceOS.Nesting.Algorithms;

public interface INestingStrategy
{
    string AlgorithmName { get; }
    Task<NestingResult> ComputeAsync(NestingInput input, CancellationToken ct = default);
}
```

---

### `FfdhNestingStrategy` (L1 — migráció)

A jelenlegi `SpaceOS.Modules.Cutting.Domain.Services.NestingService.ComputeNesting()` logikát portold át, az alábbi kiegészítésekkel:
- `NestingPart.Quantity > 1` támogatás (többszöri elhelyezés)
- `UnplacedParts` lista (nem fért el parts)
- `ComputationTime` mérés (Stopwatch)
- `Task.FromResult()` wrapper (szinkron logika)

---

### `GuillotineNestingStrategy` (L2 — ÚJ)

Guillotine cut algoritmus — minden vágás edge-to-edge.

**Kötelező benchmark (Definition of Done része):**

| Input | Elvárás |
|---|---|
| 10 alkatrész | Yield ≥ 85% |
| 50 alkatrész | Yield ≥ 88% |
| 100 alkatrész | Yield ≥ 90% |
| Compute time ≤ 5s | 200 alkatrészig |

**Algoritmus vezérelvek:**
- Recursive partitioning: minden vágás két szabad területre osztja a panelt
- Best Area Fit (BAF) heurisztika a part-panel illesztéshez
- Offcut-ok elsőbbséget élveznek (panel pool rendezés)
- Rotáció (CanRotate=true esetén 90°-os forgatás próba)

---

### `MaxRectsNestingStrategy` (L3 — placeholder)

```csharp
public sealed class MaxRectsNestingStrategy : INestingStrategy
{
    public string AlgorithmName => "MaxRects";
    public Task<NestingResult> ComputeAsync(NestingInput input, CancellationToken ct = default)
        => throw new NotImplementedException("MaxRects is a v2+ feature.");
}
```

---

### `NestingStrategyFactory`

```csharp
public sealed class NestingStrategyFactory
{
    private readonly IReadOnlyDictionary<string, INestingStrategy> _strategies;

    public NestingStrategyFactory(IEnumerable<INestingStrategy> strategies)
        => _strategies = strategies.ToDictionary(s => s.AlgorithmName, StringComparer.OrdinalIgnoreCase);

    public INestingStrategy GetStrategy(string algorithmName)
        => _strategies.TryGetValue(algorithmName, out var s)
            ? s
            : throw new KeyNotFoundException($"No nesting strategy registered for '{algorithmName}'.");

    public IEnumerable<string> AvailableStrategies => _strategies.Keys;
}
```

---

### Tesztek (NuGet)

| Fájl | Tesztek |
|---|---|
| `FfdhStrategyTests.cs` | Jelenlegi NestingServiceTests portolása + Quantity=2 test + UnplacedParts test |
| `GuillotineStrategyTests.cs` | Benchmark yield tesztek (10/50/100 alkatrész), offcut prioritás, rotáció |
| `NestingStrategyFactoryTests.cs` | Name lookup (case-insensitive), unknown name → KeyNotFoundException |

**Elvárt:** min. 20 teszt, mind zöld.

---

## Feladat 2: Cutting modul integráció (E5)

**Feltétel:** Contracts 1.3.0 (ABSTRACTIONS-010) DONE után!

### Lépések

1. **Contracts 1.3.0 NuGet frissítés** a Cutting modul csproj-jaiban
2. **Nesting NuGet referencia** hozzáadása (helyi path referencia amíg nincs publikus feed):
   ```xml
   <ProjectReference Include="../../spaceos-nesting-algorithms/SpaceOS.Nesting.Algorithms/SpaceOS.Nesting.Algorithms.csproj" />
   ```
3. **DI regisztráció** a Cutting API rétegben:
   ```csharp
   services.AddSingleton<INestingStrategy, FfdhNestingStrategy>();
   services.AddSingleton<NestingStrategyFactory>();
   ```
4. **GetNestingResultQueryHandler** frissítése:
   - Régi: `new NestingService().ComputeNesting(...)`
   - Új: `_nestingStrategy.ComputeAsync(MapToNestingInput(...), ct)`
   - ~20 soros mapper: `CuttingLineRequest` → `NestingPart`, `AvailablePanel` → `AvailablePanel`
5. **`NestingService`** class: `[Obsolete("Use INestingStrategy via SpaceOS.Nesting.Algorithms NuGet. Will be removed in v1.4.0.")]` attribútum
6. **Tesztek:** meglévő nesting tesztek mind zölden kell maradjanak

---

## Definition of Done

**NuGet (Feladat 1):**
- [ ] `spaceos-nesting-algorithms` repo létrehozva `/opt/spaceos/spaceos-nesting-algorithms/`
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `FfdhNestingStrategy` tesztek: min. 10 teszt zöld
- [ ] `GuillotineNestingStrategy` tesztek: benchmark yield feltételek teljesítve
- [ ] `NestingStrategyFactory` tesztek: 4+ teszt zöld
- [ ] Összesen: min. 20 teszt

**Integráció (Feladat 2 — Contracts 1.3.0 DONE után):**
- [ ] Cutting modul buildelhetők Contracts 1.3.0-val
- [ ] `GetNestingResultQueryHandler` `INestingStrategy`-t használ
- [ ] `NestingService` `[Obsolete]` attribútumot kapott
- [ ] Meglévő Cutting tesztek (184+) mind zölden futnak
- [ ] Outbox DONE üzenet küldve (mindkét feladat elvégzése után)

---

## Megjegyzés

A teljes spec részletei (DTO-k, biztonsági megjegyzések): `docs/architecture/SpaceOS_Modules_Contracts_Architecture_v4_2.md` szekció 3/E.

Ha Guillotine benchmark nem éri el a yield célokat: az algoritmus finomítása előtt jelezd BLOCKED outboxban — az Architect segít a heurisztika hangolásában.
