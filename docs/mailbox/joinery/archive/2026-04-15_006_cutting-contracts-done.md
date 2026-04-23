---
id: MSG-JOINERY-006-DONE
from: joinery
to: root
type: response
priority: high
status: READ
ref: MSG-JOINERY-006
created: 2026-04-15
---

# MSG-JOINERY-006-DONE — Cutting Contracts NuGet packages v1.0.0

## Összefoglaló

3 Contracts NuGet package létrehozva, build zöld, 9 smoke teszt zöld, 3 `.nupkg` kész.

Helyszín: `/opt/spaceos/spaceos-modules-cutting/`
Commit: `84bb708 feat: Cutting/Inventory/Procurement Contracts NuGet packages v1.0.0`

---

## Létrehozott package-ek

| Package | Fájl |
|---|---|
| SpaceOS.Modules.Inventory.Contracts 1.0.0 | `nupkg/SpaceOS.Modules.Inventory.Contracts.1.0.0.nupkg` |
| SpaceOS.Modules.Cutting.Contracts 1.0.0 | `nupkg/SpaceOS.Modules.Cutting.Contracts.1.0.0.nupkg` |
| SpaceOS.Modules.Procurement.Contracts 1.0.0 | `nupkg/SpaceOS.Modules.Procurement.Contracts.1.0.0.nupkg` |

## Interface-ek

**IInventoryProvider** (6 metódus): GetStockAsync, GetOffcutsAsync, RecordConsumptionAsync, RecordInboundAsync, RecordOffcutAsync, GetConsumptionTrendAsync

**ICuttingProvider** (4 metódus): SubmitCuttingSheetAsync, GetNestingResultAsync, GetExecutionStatusAsync, GetWasteReportAsync

**IProcurementProvider** (4 metódus): CreatePurchaseOrderAsync, GetOrderStatusAsync, GetSupplierPricesAsync, RecordDeliveryAsync

## Dependency sorrend betartva

```
Inventory.Contracts (önálló)
    ↓
Cutting.Contracts    → ProjectReference: Inventory.Contracts
Procurement.Contracts → ProjectReference: Inventory.Contracts
```

## Build & Test

```
dotnet build  → 0 error, 0 warning
dotnet test   → 9/9 passed
dotnet pack   → 3 × .nupkg létrehozva
```

## Megjegyzés

`.gitignore` hozzáadva (`bin/`, `obj/`, `nupkg/` kizárva). Az xunit 2.5.3 nem inject-el implicit global usings-t, ezért `using Xunit;` explicit a tesztfájlban.
