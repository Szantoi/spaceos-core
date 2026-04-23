---
id: MSG-JOINERY-006
from: root
to: joinery
type: task
priority: high
status: READ
created: 2026-04-15
---

# MSG-JOINERY-006 — Modules.Cutting Sprint 6: Contracts NuGet csomagok

## Kontextus

Sprint 6 első fázisa: a három új modul (Cutting, Inventory, Procurement) **Contract** rétege.
A contracts az implementáció előfeltétele — ezek a NuGet package-ek definiálják az interface-eket.

**Mintát követ:** `SpaceOS.Modules.Contracts` 1.0.0 (amit te csináltál korábban)
**Helyszín:** `/opt/spaceos/spaceos-modules-cutting/` (INFRA-095 már előkészítette)

## Dependency sorrend (fontos!)

```
IInventoryProvider (önálló — senkitől nem függ)
    ↓
ICuttingProvider (függ: IInventoryProvider — készlet lekérdezéshez)
    ↓
IProcurementProvider (függ: IInventoryProvider — reorder alerthez)
```

## Feladat: 3 NuGet Contracts package létrehozása

### Solution struktúra

```
spaceos-modules-cutting/
├── SpaceOS.Modules.Cutting.sln
├── src/
│   ├── SpaceOS.Modules.Inventory.Contracts/
│   │   ├── SpaceOS.Modules.Inventory.Contracts.csproj
│   │   ├── Providers/
│   │   │   └── IInventoryProvider.cs
│   │   ├── Dtos/
│   │   │   ├── MaterialCatalogDto.cs
│   │   │   ├── PanelStockDto.cs
│   │   │   ├── OffcutDto.cs
│   │   │   └── StockMovementDto.cs
│   │   └── Events/
│   │       ├── LowStockEvent.cs
│   │       └── StockUpdatedEvent.cs
│   │
│   ├── SpaceOS.Modules.Cutting.Contracts/
│   │   ├── SpaceOS.Modules.Cutting.Contracts.csproj
│   │   ├── Providers/
│   │   │   └── ICuttingProvider.cs
│   │   ├── Dtos/
│   │   │   ├── CuttingSheetDto.cs
│   │   │   ├── CuttingLineDto.cs
│   │   │   ├── PanelAssignmentDto.cs
│   │   │   ├── CuttingExecutionDto.cs
│   │   │   └── WasteReportDto.cs
│   │   └── Events/
│   │       ├── CuttingSheetSubmittedEvent.cs
│   │       └── CuttingExecutionCompletedEvent.cs
│   │
│   └── SpaceOS.Modules.Procurement.Contracts/
│       ├── SpaceOS.Modules.Procurement.Contracts.csproj
│       ├── Providers/
│       │   └── IProcurementProvider.cs
│       ├── Dtos/
│       │   ├── SupplierDto.cs
│       │   ├── PurchaseOrderDto.cs
│       │   ├── DeliveryDto.cs
│       │   └── ReorderAlertDto.cs
│       └── Events/
│           ├── PurchaseOrderCreatedEvent.cs
│           └── DeliveryReceivedEvent.cs
│
└── tests/
    └── SpaceOS.Modules.Cutting.Contracts.Tests/
        └── ContractSmokeTests.cs
```

### Interface specifikációk

#### IInventoryProvider

```csharp
public interface IInventoryProvider
{
    Task<PanelStockDto> GetStockAsync(string materialType, CancellationToken ct = default);
    Task<IReadOnlyList<OffcutDto>> GetOffcutsAsync(string materialType, CancellationToken ct = default);
    Task RecordConsumptionAsync(IReadOnlyList<StockMovementDto> items, CancellationToken ct = default);
    Task RecordInboundAsync(StockMovementDto delivery, CancellationToken ct = default);
    Task RecordOffcutAsync(OffcutDto offcut, CancellationToken ct = default);
    Task<ConsumptionTrendDto> GetConsumptionTrendAsync(DateRange range, CancellationToken ct = default);
}
```

#### ICuttingProvider

```csharp
public interface ICuttingProvider
{
    Task<Guid> SubmitCuttingSheetAsync(CuttingSheetDto sheet, CancellationToken ct = default);
    Task<PanelAssignmentDto> GetNestingResultAsync(Guid sheetId, CancellationToken ct = default);
    Task<CuttingExecutionDto> GetExecutionStatusAsync(Guid sheetId, CancellationToken ct = default);
    Task<WasteReportDto> GetWasteReportAsync(DateRange range, CancellationToken ct = default);
}
```

#### IProcurementProvider

```csharp
public interface IProcurementProvider
{
    Task<Guid> CreatePurchaseOrderAsync(PurchaseOrderDto order, CancellationToken ct = default);
    Task<PurchaseOrderDto> GetOrderStatusAsync(Guid orderId, CancellationToken ct = default);
    Task<IReadOnlyList<SupplierPriceDto>> GetSupplierPricesAsync(string materialType, CancellationToken ct = default);
    Task RecordDeliveryAsync(DeliveryDto delivery, CancellationToken ct = default);
}
```

### DTO-k (minimális, bővíthető)

**CuttingSheetDto:** `Guid Id`, `Guid TenantId`, `Guid SourceOrderId`, `IReadOnlyList<CuttingLineDto> Lines`, `string MaterialType`, `DateTime CreatedAt`

**CuttingLineDto:** `string Name`, `string PartType`, `decimal RawWidth`, `decimal RawHeight`, `decimal Thickness`, `int Quantity`, `bool CanRotate`, `string? EdgeBanding`

**PanelStockDto:** `string MaterialType`, `decimal Thickness`, `int FullPanelCount`, `IReadOnlyList<OffcutDto> Offcuts`

**OffcutDto:** `Guid Id`, `decimal Width`, `decimal Height`, `decimal Thickness`, `string MaterialType`, `Guid OriginSheetId`

**PurchaseOrderDto:** `Guid Id`, `Guid TenantId`, `Guid SupplierId`, `string MaterialType`, `decimal Quantity`, `string Status`, `DateTime? ExpectedDelivery`

### .csproj (mindhárom)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <IsPackable>true</IsPackable>
    <PackageId>SpaceOS.Modules.[X].Contracts</PackageId>
    <Version>1.0.0</Version>
    <Authors>SpaceOS</Authors>
  </PropertyGroup>
</Project>
```

Inventory.Contracts függőségek: nincs külső. Cutting.Contracts: `<ProjectReference>` Inventory.Contracts-ra. Procurement.Contracts: `<ProjectReference>` Inventory.Contracts-ra.

### Smoke tesztek

```csharp
// Csak azt ellenőrzi, hogy az interface-ek lefordulnak és a DTOs-ok helyes struktúrájúak
[Fact] void IInventoryProvider_HasGetStock() { ... }
[Fact] void ICuttingProvider_HasSubmitCuttingSheet() { ... }
[Fact] void IProcurementProvider_HasCreatePurchaseOrder() { ... }
```

### Pack

```bash
dotnet build --configuration Release
dotnet pack src/SpaceOS.Modules.Inventory.Contracts -c Release -o ./nupkg/
dotnet pack src/SpaceOS.Modules.Cutting.Contracts -c Release -o ./nupkg/
dotnet pack src/SpaceOS.Modules.Procurement.Contracts -c Release -o ./nupkg/
```

## DoD

- [ ] `dotnet build` → 0 error, 0 warning
- [ ] `dotnet test` → minden smoke teszt zöld
- [ ] 3 db `.nupkg` fájl létrejön a `./nupkg/` mappában
- [ ] Interface-ek pontosan megvalósítják a fenti specifikációt
- [ ] `git add -A && git commit` a `main` branch-en

## Kockázatok

- Ha az `/opt/spaceos/spaceos-modules-cutting/` könyvtár nem létezik → jelezd BLOCKED-dal
- DTO-khoz hozzáadhatsz mezőket ha logikailag szükséges, de ne vegyél el a fentiekből
- Ne hozz létre implementációs kódot — csak contracts és DTOs
