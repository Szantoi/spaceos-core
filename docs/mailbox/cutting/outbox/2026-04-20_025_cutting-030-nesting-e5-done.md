---
id: MSG-CUTTING-030-DONE
from: cutting
to: root
type: done
status: READ
priority: high
date: 2026-04-20
ref: MSG-CUTTING-030
---

# CUTTING-030 DONE — Nesting NuGet E5 integráció

## Összefoglaló

A `GetNestingResultQueryHandler` átírva: `NestingService` helyett `INestingStrategy` (SpaceOS.Nesting.Algorithms NuGet).

## Módosított fájlok

| Fájl | Változás |
|---|---|
| `SpaceOS.Modules.Cutting.Application.csproj` | `ProjectReference` hozzáadva → `SpaceOS.Nesting.Algorithms` |
| `SpaceOS.Modules.Cutting.Infrastructure.csproj` | `ProjectReference` hozzáadva → `SpaceOS.Nesting.Algorithms` |
| `src/.../Extensions/ServiceCollectionExtensions.cs` | `NestingService` singleton eltávolítva; `INestingStrategy, FfdhNestingStrategy` + `NestingStrategyFactory` singleton regisztrálva |
| `src/.../Queries/GetNestingResult/GetNestingResultQueryHandler.cs` | `NestingService` → `INestingStrategy`; mapper: `CuttingLine` → `NestingPart`, `PanelStockDto/OffcutDto` → `AvailablePanel`; async `ComputeAsync` hívás |
| `src/.../Domain/Services/NestingService.cs` | `[Obsolete]` attribútum hozzáadva |
| `tests/.../Domain/NestingServiceTests.cs` | `#pragma warning disable CS0618` — meglévő legacy tesztek megőrizve |

## Tesztek

```
Build: 0 error, 0 warning (TreatWarningsAsErrors=true)
Tests: Passed 194/194
  - SpaceOS.Modules.Cutting.Tests:          184/184
  - SpaceOS.Modules.Cutting.Contracts.Tests: 10/10
```

## Security review

- **Authorization**: API endpointok `[Authorize(Policy = "ManufacturerOnly")]` — nem változott
- **RLS**: tenant-specifikus táblák RLS FORCE — nem változott
- **Immutability**: CuttingSheet immutable — nem változott
- **DI**: `FfdhNestingStrategy` és `NestingStrategyFactory` singleton — stateless, threadsafe
- **OWASP**: nincs input validation változás, nesting input a saját DB-ből és Inventory-ból jön

## Változtatások részletei

**Mapper (CuttingLine → NestingPart):**
```csharp
new NestingPart(PartId: l.Id.ToString(), Name: l.PartName,
    WidthMm: l.WidthMm, HeightMm: l.HeightMm, CanRotate: true, Quantity: l.Quantity)
```

**Mapper (PanelStockDto → AvailablePanel):**
```csharp
new AvailablePanel(PanelId: Guid.NewGuid().ToString(), MaterialCode: materialType,
    WidthMm: stock.WidthMm, HeightMm: stock.HeightMm, IsOffcut: false)
```

**NestingResult → Response:**
```csharp
Guid.TryParse(a.PanelId, out var panelGuid) ? panelGuid : Guid.Empty
```
(Panel ID-k most string-ek a NuGet modellben; Guid-ra parse-olva a response-ban megőrzött visszafelé kompatibilitás miatt)

## Kockázatok / kérdések

- Contracts 1.3.0 NuGet frissítés (1. lépés): A Cutting modul jelenleg nem referálja `SpaceOS.Modules.Contracts`-t (csak `SpaceOS.Modules.Inventory.Contracts` 1.1.0-t). A `SpaceOS.Modules.Contracts` 1.3.0 nupkg artifact még nem létezik a lokális feed-ben — ha a root azt kívánja, hogy a cutting modul is referálja, ezt külön inbox üzenetben kell specifikálni.
- `NestingService` meglévő tesztjei megőrizve (`#pragma warning disable CS0618`) — a `NestingService` osztály törlése majd v1.4.0-ban lehetséges.
