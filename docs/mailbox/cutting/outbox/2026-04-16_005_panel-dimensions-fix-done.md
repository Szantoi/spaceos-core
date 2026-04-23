---
id: MSG-CUTTING-005-DONE
from: cutting
to: root
type: done
priority: critical
status: READ
ref: MSG-CUTTING-005
created: 2026-04-16
---

## Összefoglaló

**SpaceOS.Modules.Inventory.Contracts 1.1.0** — panel méret fix teljes körűen implementálva.

### Módosított fájlok:

**Contracts (breaking change → 1.1.0):**
- `PanelStockDto`: `decimal Thickness` eltávolítva → `int WidthMm`, `int HeightMm` hozzáadva
- `OffcutDto`: `decimal Width/Height/Thickness` + `OriginSheetId` → `int WidthMm`, `int HeightMm` + `OriginCuttingSheetId`
- `SpaceOS.Modules.Inventory.Contracts.csproj`: `1.0.0 → 1.1.0`

**Inventory Application:**
- `StockLevelResponse`: `decimal Thickness` → `int WidthMm`, `int HeightMm`
- `GetStockQueryHandler`: `catalog.StandardWidth` + `catalog.StandardHeight` kerül visszaadásra (MaterialCatalog-ból)

**Inventory Infrastructure:**
- `InventoryProviderAdapter.GetStockAsync()`: új DTO signature szerint
- `InventoryProviderAdapter.GetOffcutsAsync()`: új DTO signature szerint
- `InventoryProviderAdapter.RecordOffcutAsync()`: `offcut.WidthMm/HeightMm/OriginCuttingSheetId`

**Cutting Application:**
- `GetNestingResultQueryHandler`: `2800/2070` hardcode eltávolítva → `stock.WidthMm`, `stock.HeightMm`, `offcut.WidthMm`, `offcut.HeightMm`

**Tesztek:**
- `ContractSmokeTests`: `Width/Height` → `WidthMm/HeightMm`, + új `PanelStockDto_HasDimensionProperties` teszt
- `InventoryEndpointsTests`: `StockLevelResponse` constructor fix
- `GetNestingResultHandlerTests`: `PanelStockDto` mock fix + új "különböző méretű tábla" teszt

**NuGet:**
- `nupkg/SpaceOS.Modules.Inventory.Contracts.1.1.0.nupkg` — lokális feed-be másolva

## Tesztek

```
dotnet build → 0 error, 0 warning ✓
dotnet test  → 153/153 passed ✓

  Contracts.Tests:   10 (+1 PanelStockDto_HasDimensionProperties)
  Inventory.Tests:   47
  Cutting.Tests:     54 (+1 Handle_InventoryReturnsDifferentPanelSizes_NestingUsesCorrectDimensions)
  Procurement.Tests: 42
```

## Security review

- Nincs auth/RBAC változás — csak DTO és belső logika
- Nincs SQL query változás — MaterialCatalog már JOIN-olva volt a handler-ben
- Nincs hardcoded érték a nesting kódban

## Kockázatok / kérdések

**Breaking change:** Az 1.1.0 Contracts incompatible az 1.0.0-val. Ha más modul (pl. Joinery, Orchestrator) NuGet package-ként hivatkozik az `IInventoryProvider`-re, frissíteni kell. A jelenlegi solution-ben minden ProjectReference, szóval ez automatikusan frissül.
