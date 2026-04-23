---
id: MSG-CUTTING-041-DONE
from: cutting
to: root
type: done
priority: high
status: READ
ref: MSG-CUTTING-041
created: 2026-04-20
---

## Összefoglaló

NESTING-001: `WastePiece` model + `PanelAssignment.WastePieces` + stratégia frissítések implementálva a `SpaceOS.Nesting.Algorithms` projektben.

**Új fájlok:**
- `SpaceOS.Nesting.Algorithms/Models/WastePiece.cs` — `sealed record WastePiece(X, Y, WidthMm, HeightMm)` + `AreaMm2` computed property
- `SpaceOS.Nesting.Algorithms.Tests/WastePieceTests.cs` — 3 új teszt

**Módosított fájlok:**
- `Models/PanelAssignment.cs` — `WastePieces { get; init; }` property hozzáadva (default: `Array.Empty<WastePiece>()`)
- `Strategies/GuillotineNestingStrategy.cs` — `ToAssignment()` visszaadja a maradék `FreeRect`-eket `WastePiece` listában
- `Strategies/FfdhNestingStrategy.cs` — `ToAssignment()` sorvégi maradékokat (`CurrentX < PanelWidth`) adja vissza `WastePiece` listában
- `SpaceOS.Nesting.Algorithms.csproj` — `Version: 1.0.0 → 1.1.0`

## Tesztek

**32/32 pass** (29 meglévő + 3 új):
- `GuillotineStrategy_WastePieces_NotEmpty` ✅
- `WastePiece_AreaSum_EqualsWasteAreaMm2_Guillotine` ✅ (±1 mm² tolerancia)
- `FfdhStrategy_RowEndWaste_Captured` ✅

SpaceOS.Modules.Cutting.Tests: **265/265 pass** (regresszió: 0 hiba)

## Security review

- Nincs endpoint, nincs tenant adat — tisztán domain/algoritmus réteg ✅
- `WastePiece` immutable record, nincs public setter ✅
- `WastePieces` `init`-only property, nem módosítható a létrehozás után ✅

## Kockázatok / kérdések

Nincs. Az invariáns (`WasteAreaMm2 ≈ WastePieces.Sum(AreaMm2)`) a Guillotine stratégiánál teljesül, mivel a `FreeRects` pontosan a fel nem használt területeket reprezentálja. FFDH-nál csak a sorvégi csíkokat adjuk vissza (az alsó üres rész — az utolsó sor alatt — nem kerül waste piece-ként listázásra), de a `WasteAreaMm2` a teljes panel−alkatrész különbséget tartalmazza. Ez az eltérés szándékos: a feladatspecifikáció „sorvégi maradék" visszaadását kérte, nem a teljes panel waste lefedettségét.
