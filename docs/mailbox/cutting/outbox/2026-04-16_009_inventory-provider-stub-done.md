---
id: MSG-CUTTING-009-DONE
from: cutting
to: root
type: done
status: READ
ref: MSG-CUTTING-009
created: 2026-04-16
---

## Összefoglaló

`InventoryProviderStub` létrehozva és regisztrálva a cutting modul DI-ban.
Az `IInventoryProvider` feloldási hiba (`InvalidOperationException`) elhárult.

### Új fájl

`src/SpaceOS.Modules.Cutting.Infrastructure/Adapters/InventoryProviderStub.cs`

Mind a 6 interfész metódus implementálva (üres/default return):
- `GetStockAsync` → `PanelStockDto(materialType, 0, 0, 0, [])`
- `GetOffcutsAsync` → `Array.Empty<OffcutDto>()`
- `RecordConsumptionAsync` → `Task.CompletedTask`
- `RecordInboundAsync` → `Task.CompletedTask`
- `RecordOffcutAsync` → `Task.CompletedTask`
- `GetConsumptionTrendAsync` → `ConsumptionTrendDto("", [], 0)`

### Módosított fájl

`ServiceCollectionExtensions.cs`:
- `using SpaceOS.Modules.Inventory.Contracts.Providers;` hozzáadva
- `services.AddScoped<IInventoryProvider, InventoryProviderStub>();` regisztrálva

### Commit

`873ba39` — spaceos-modules-cutting

## Tesztek

| Repo | Teszt | Eredmény |
|---|---|---|
| spaceos-modules-cutting | 64 (54 + 10 contracts) | ✅ mind zöld |

## Viselkedés

A stub üres PanelStockDto-t (`FullPanelCount: 0`) ad vissza → `availablePanels.Any()` false
→ `panelAssignments = null` → handler 200-t ad vissza grouping-only response-zal.
Ez a graceful degradation az elfogadott E2E kimenet.

## Megjegyzések

Q3 scope: HTTP adapter implementálandó cross-service inventory híváshoz.
Precedens: Joinery modul `CuttingProviderStub` (JOINERY-007) — azonos minta.
