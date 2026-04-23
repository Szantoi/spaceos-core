---
id: MSG-CUTTING-041
from: root
to: cutting
type: task
priority: high
status: READ
ref: MSG-ARCH-004-RESPONSE
created: 2026-04-20
---

# NESTING-001 — SpaceOS.Nesting.Algorithms 1.1.0 (WastePiece model)

> **Skill:** `/spaceos-terminal` szerint dolgozz — inbox → build → test → outbox DONE
> **Repo:** `SpaceOS.Nesting.Algorithms` (standalone NuGet)
> **Spec:** `docs/architecture/ADR-038-offcut-creation-at-plan-freeze.md`
> **Timeline:** ~0.5 nap
> **Blokkoló:** — (párhuzamosan futhat INVENTORY-015-tel)

---

## Kontextus

Az offcut loop lezárásához a nesting stratégiák kimenetébe dimenzionált waste téglalapokat kell visszaadni. Jelenleg a `PanelAssignment` csak a `WasteAreaMm2` összesített értéket tartalmazza — az egyedi waste darabok koordinátái és méretei hiányoznak, ezeket kell hozzáadni.

---

## Feladatok

### 1. WastePiece model (ÚJ)

**Fájl:** `Models/WastePiece.cs`

```csharp
public sealed record WastePiece(
    decimal X,
    decimal Y,
    decimal WidthMm,
    decimal HeightMm
)
{
    public decimal AreaMm2 => WidthMm * HeightMm;
}
```

### 2. PanelAssignment módosítása

**Fájl:** `Models/PanelAssignment.cs`

Adj hozzá `WastePieces` property-t:

```csharp
public IReadOnlyList<WastePiece> WastePieces { get; init; } = Array.Empty<WastePiece>();
```

Invariáns ellenőrzés (tesztelendő):
```
WasteAreaMm2 == WastePieces.Sum(w => w.WidthMm * w.HeightMm)
```
(Lebegőpontos eltérés miatt ±1 mm² tolerancia elfogadható.)

### 3. GuillotineNestingStrategy módosítása

**Fájl:** `Strategies/GuillotineNestingStrategy.cs`

A guillotine algoritmus természetes vágás-maradékokat produkál — ezeket adj vissza `WastePiece` rekordokként a `PanelAssignment`-ben.

### 4. FfdhNestingStrategy módosítása

**Fájl:** `Strategies/FfdhNestingStrategy.cs`

FFDH sorvégi maradékok: minden sor végén a kitöltetlen vízszintes csík egy `WastePiece`.

### 5. Version bump

**Fájl:** `SpaceOS.Nesting.Algorithms.csproj`

```xml
<Version>1.1.0</Version>
```

---

## Tesztek

- Meglévő 29 teszt mind zölden
- Legalább 3 új teszt:
  - `GuillotineStrategy_WastePieces_NotEmpty` — Guillotine kimenetben legalább 1 waste darab
  - `WastePiece_AreaSum_EqualsWasteAreaMm2` — invariáns teljesül (±1 mm² tolerancia)
  - `FfdhStrategy_RowEndWaste_Captured` — FFDH sorvégi maradék visszaadva

---

## Definition of Done

- [ ] `WastePiece` model létrehozva
- [ ] `PanelAssignment.WastePieces` property hozzáadva
- [ ] `GuillotineNestingStrategy` waste téglalapokat ad vissza
- [ ] `FfdhNestingStrategy` sor végén maradék visszaadva
- [ ] `WasteAreaMm2 == WastePieces.Sum(...)` invariáns teljesül (±1 mm²)
- [ ] Version: `1.1.0`
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` mind zöld (29+3)
- [ ] Outbox DONE üzenet küldve
