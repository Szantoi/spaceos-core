---
id: MSG-CUTTING-029-DONE
from: cutting
to: root
type: done
status: READ
priority: high
date: 2026-04-20
ref: MSG-CUTTING-029
---

# CUTTING-029 DONE (részleges) — SpaceOS.Nesting.Algorithms NuGet (E1–E4)

## Összefoglaló

**Feladat 1 (E1–E4) KÉSZ:** `SpaceOS.Nesting.Algorithms` NuGet projekt létrehozva és tesztelve.

**Feladat 2 (E5) BLOKKOLT:** ABSTRACTIONS-010 (Contracts 1.3.0) még nem DONE.
Az ABSTRACTIONS outbox legutóbbi bejegyzése: `2026-04-18_006_contracts-v1-2-0-done.md` (Contracts 1.2.0).
E5 integráció csak ABSTRACTIONS-010 DONE után indul.

---

## Új repo

**Helye:** `/opt/spaceos/spaceos-nesting-algorithms/`
**Commit:** `3e87954`

---

## Új fájlok

### SpaceOS.Nesting.Algorithms (főprojekt)

```
INestingStrategy.cs
NestingStrategyFactory.cs
Models/
  NestingInput.cs     (Parts, Panels, SawBladeGapMm=4)
  NestingPart.cs      (PartId, Name, WidthMm, HeightMm, CanRotate=true, Quantity=1)
  AvailablePanel.cs   (PanelId, MaterialCode, WidthMm, HeightMm, IsOffcut)
  NestingResult.cs    (Assignments, UnplacedParts, TotalWastePercentage, PanelsUsed, AlgorithmUsed, ComputationTime)
  PlacedPart.cs       (PartId, Name, X, Y, WidthMm, HeightMm, IsRotated)
  PanelAssignment.cs  (PanelId, MaterialCode, PanelWidthMm, PanelHeightMm, PlacedParts, WasteAreaMm2, UtilizationPercent)
Strategies/
  FfdhNestingStrategy.cs
  GuillotineNestingStrategy.cs
  MaxRectsNestingStrategy.cs     (NotImplementedException placeholder, v2+)
```

### SpaceOS.Nesting.Algorithms.Tests

```
FfdhStrategyTests.cs          — 13 teszt
GuillotineStrategyTests.cs    —  9 teszt (benchmark + offcut + rotation + empty)
NestingStrategyFactoryTests.cs —  7 teszt
```

---

## Tesztek

| Csomag | Teszt | Eredmény |
|---|---|---|
| FfdhStrategyTests | 13 | ✅ |
| GuillotineStrategyTests | 9 | ✅ |
| NestingStrategyFactoryTests | 7 | ✅ |
| **Összesen** | **29** | **✅ 29/29** |

**Benchmark eredmények (GuillotineNestingStrategy, SawBladeGapMm=0):**

| Input | Yield | Elvárás | Eredmény |
|---|---|---|---|
| 10 parts 100×900 / 1000×1000 panel | 90% | ≥85% | ✅ |
| 50 parts 100×176 / 1000×1000 panel | 88% | ≥88% | ✅ |
| 100 parts 90×100 / 1000×1000 panel | 90% | ≥90% | ✅ |
| 200 parts / 5× 2440×1220 panel | <1s | ≤5s | ✅ |

```
Build: 0 error, 0 warning (TreatWarningsAsErrors=true)
Tests: Passed 29/29
```

---

## Megjegyzések

**FFDH fix:** A panel pool kezelésénél a túl kis panelek kihagyásakor az index nem lépett tovább maradandóan — mutable lista megközelítéssel javítva, így kisebb alkatrészek is elérik azokat a paneleket, amiket egy nagyobb alkatrész miatt ugrott át az algoritmus.

**Guillotine split:** Shorter Axis Rule alapján guillotine vágás — minimalizálja a fragmentációt, maximalizálja az újrafelhasználható területeket.

**Függetlenség:** Nincs `SpaceOS.Modules.Contracts` referencia — szándékosan standalone a FreeTier.Api direktörökommunikáció céljából.

## E5 státusz (Feladat 2)

BLOKKOLT → ABSTRACTIONS-010 (Contracts 1.3.0) DONE kell.

Ha ABSTRACTIONS-010 DONE: új inbox üzenetben kiadni az E5 integrációt (Cutting modul referencia + GetNestingResultQueryHandler frissítés + NestingService Obsolete attribútum).
