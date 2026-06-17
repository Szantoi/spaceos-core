# SpaceOS — Shared Module Contracts Architecture
## ICuttingProvider + IInventoryProvider + IProcurementProvider — Contract Package

> **Verzió:** v4.0 — 2026-04-12
> **Státusz:** IMPLEMENTÁCIÓRA KÉSZ
> **Blokkoló feltétel:** Modules.Abstractions Phase A DONE ✅ · ADR-019 ✅ · Cutting Vision v1.2 ✅
> **Kumulált review:** Interface Design Review → v2 · `/senior-security` → v3 · `/senior-backend` → v4
> **Repo:** `spaceos-modules-contracts` (új polyrepo)
> **Becsült effort:** ~3 fejlesztői nap
> **Fogyasztók:** Cutting Core · Inventory Core · Procurement Core · Orchestrator (adapter routing) · Trade modulok (Joinery, Cabinet)

---

## 1. Kumulált Finding Összesítő (v1 → v4)

| Review | Finding-ek | Legfontosabb javítás | Effort delta |
|--------|-----------|----------------------|--------------|
| v1 → Interface Design Review → v2 | 1 CRITICAL · 4 HIGH · 5 MEDIUM | Input/Output DTO szétválasztás · OnEvent callback törlés · OffcutDto dual ownership feloldás | +0.5 nap |
| v2 → `/senior-security` → v3 | 1 CRITICAL · 2 HIGH · 2 MEDIUM | TenantId trust boundary · DTO size/length guard · Event TenantId validáció | +0.5 nap |
| v3 → `/senior-backend` → v4 | 0 CRITICAL · 1 HIGH · 2 MEDIUM | Missing query-by-source · HealthCheck timeout convention · SemVer strategy | +0 nap |
| **Összesen** | **2 CRITICAL · 7 HIGH · 9 MEDIUM** | | **~3 fejlesztői nap** |

### Finding részletek

| ID | Súly | Terület | Probléma | vN javítás |
|----|------|---------|----------|------------|
| CD-01 | 🔴 CRITICAL | DTO lifecycle | `CuttingSheetDto.Id` nullable — input/output nem szétválasztott | v2: `SubmitCuttingSheetRequest` (nincs Id) és `CuttingSheetDto` (required Id). Ugyanez PurchaseOrder-re. |
| SEC-01 | 🔴 CRITICAL | Trust boundary | `SubmitCuttingSheetRequest.TenantId` a hívó adja meg — spoofable. TenantId a JWT-ből kell jöjjön, nem a DTO-ból. | v3: **TenantId TÖRÖLVE a request DTO-kból.** Az implementáció a JWT claim-ből veszi. A response DTO-kban marad (read-only). |
| CD-02 | 🟠 HIGH | Capability | `ProviderCapability` string constant → typo-prone | v2: `[Flags] enum ProviderCapability` — compile-time check |
| CD-03 | 🟠 HIGH | Event identity | EventId-t a fogyasztó generálja → duplikáció risk | v2: Auto-generated `EventId = Guid.NewGuid()` a base record-ban |
| CD-04 | 🟠 HIGH | Event pattern | `OnEvent(callback)` — nincs unsubscribe, nem Kernel-konform | v2: **OnEvent törölve.** Domain event dispatch pattern marad. |
| CD-05 | 🟠 HIGH | Dual ownership | `OffcutDto` Cutting-ben és Inventory-ben — más kontextus | v2: `CuttingOffcutResultDto` (Cutting) és `StockItemDto` (Inventory) szétválasztva |
| SEC-02 | 🟠 HIGH | DoS | Nincs collection size limit a DTO-kban — 10.000 CuttingLine-t is beküldhet | v3: XML doc comment + `[MaxItems]` attribute: Lines max 200, CncInstructions max 500, ProcessSteps max 50 |
| SEC-03 | 🟠 HIGH | Event spoof | Event TenantId-t a provider tölti — a fogyasztónak validálnia kell | v3: XML doc comment: "A fogyasztó KÖTELES ellenőrizni hogy Event.TenantId == JWT TenantId" |
| BE-01 | 🟠 HIGH | Missing query | Nincs `GetCuttingSheetsBySourceAsync` — a Joinery nem tud source alapján keresni | v4: Új metódus: `GetCuttingSheetsBySourceAsync(sourceEntityId, ct)` |
| CD-06 | 🟡 MEDIUM | Naming | `StockMovementDto.Reference` string — magic value | v2: `StockReferenceType` enum + `ReferenceId` Guid? |
| CD-07 | 🟡 MEDIUM | Dependency | `RecordInboundAsync` Guid deliveryId — Inventory nem referálhatja Procurement DTO-t | v2: `InboundReceiptDto` az Inventory contract-ban |
| CD-08 | 🟡 MEDIUM | Validation | Currency string — nincs ISO 4217 constraint | v2: XML doc `[MaxLength(3)]` + ISO 4217 megjegyzés |
| CD-09 | 🟡 MEDIUM | Cross-ref | `PlacedPieceDto` nincs kötve `CuttingLineDto`-hoz | v2: `CuttingLineIndex` int mező hozzáadva |
| CD-10 | 🟡 MEDIUM | Naming | `DaysUntilStockout` — számított érték, lehet null | v2: `EstimatedDaysUntilStockout` nullable int |
| SEC-04 | 🟡 MEDIUM | String length | ComponentName, MaterialCode, Notes nincs max length | v3: XML doc max length-ek: ComponentName 100, MaterialCode 20, Notes 2000 |
| SEC-05 | 🟡 MEDIUM | Capability | Malicious provider bármilyen capability-t claimezhet | v3: XML doc: "A fogyasztó KÖTELES capability-t ellenőrizni hívás előtt; NotSupportedException ha hazudik" |
| BE-02 | 🟡 MEDIUM | Convention | HealthCheckAsync nincs timeout konvenció | v4: XML doc: "Max 5s válaszidő. Ha timeout → false." |
| BE-03 | 🟡 MEDIUM | Versioning | NuGet SemVer stratégia nincs dokumentálva | v4: README-ben: MAJOR = breaking interface change, MINOR = új metódus, PATCH = DTO mező + doc |

---

## 2. Architekturális döntések

| # | Döntés | Választás | Indoklás |
|---|--------|-----------|----------|
| D-01 | Egy csomag vs három | **Egy: `SpaceOS.Modules.Contracts`** | A három contract összefügg (cross-module events); egy verzió, nincs cross-package dependency |
| D-02 | Capability modell | **`[Flags] enum`** | Compile-time safety; HasFlag check; nem typo-prone |
| D-03 | Event pattern | **Domain event dispatch** (nem callback) | Kernel-konform; PopDomainEvents() minta |
| D-04 | TenantId a request-ekben | **NINCS — JWT-ből** (SEC-01) | Trust boundary: a hívó nem határozhatja meg a tenant kontextust |
| D-05 | DTO validation | **XML doc constraint-ek a Contract-ban, FluentValidation az implementációban** | A Contract definiálja a szabályt, az implementáció kényszeríti |
| D-06 | External dependency | **Csak `Ardalis.Result`** | Approved package; minimál dependency |

---

## 3. Solution struktúra

```
spaceos-modules-contracts/
├── SpaceOS.Modules.Contracts/
│   ├── Shared/
│   │   ├── IModuleProvider.cs
│   │   ├── ModuleEvent.cs
│   │   └── ProviderCapability.cs
│   ├── Cutting/
│   │   ├── ICuttingProvider.cs
│   │   ├── Requests/
│   │   │   └── SubmitCuttingSheetRequest.cs
│   │   ├── DTOs/
│   │   │   ├── CuttingSheetDto.cs
│   │   │   ├── CuttingLineDto.cs
│   │   │   ├── CncInstructionDto.cs
│   │   │   ├── ProcessStepDto.cs
│   │   │   ├── NestingResultDto.cs
│   │   │   ├── PanelAssignmentDto.cs
│   │   │   ├── PlacedPieceDto.cs
│   │   │   ├── CuttingOffcutResultDto.cs
│   │   │   ├── ExecutionStatusDto.cs
│   │   │   └── WasteReportDto.cs
│   │   ├── Events/
│   │   │   ├── CuttingSheetReceived.cs
│   │   │   ├── NestingCompleted.cs
│   │   │   ├── CuttingCompleted.cs
│   │   │   └── CuttingFailed.cs
│   │   └── Enums/
│   │       ├── CuttingExecutionStatus.cs
│   │       └── CuttingSheetStatus.cs
│   ├── Inventory/
│   │   ├── IInventoryProvider.cs
│   │   ├── Requests/
│   │   │   └── InboundReceiptDto.cs
│   │   ├── DTOs/
│   │   │   ├── StockItemDto.cs
│   │   │   ├── StockMovementDto.cs
│   │   │   ├── MaterialDto.cs
│   │   │   ├── StockLocationDto.cs
│   │   │   └── ConsumptionTrendDto.cs
│   │   ├── Events/
│   │   │   ├── StockConsumed.cs
│   │   │   ├── StockReceived.cs
│   │   │   ├── OffcutRegistered.cs
│   │   │   └── LowStockAlert.cs
│   │   └── Enums/
│   │       ├── StockMovementType.cs
│   │       ├── StockItemType.cs
│   │       ├── StockReferenceType.cs
│   │       └── MaterialCategory.cs
│   └── Procurement/
│       ├── IProcurementProvider.cs
│       ├── Requests/
│       │   └── CreatePurchaseOrderRequest.cs
│       ├── DTOs/
│       │   ├── PurchaseOrderDto.cs
│       │   ├── PurchaseOrderLineDto.cs
│       │   ├── SupplierDto.cs
│       │   ├── PriceListEntryDto.cs
│       │   ├── DeliveryDto.cs
│       │   ├── DeliveryLineDto.cs
│       │   └── SupplierRatingDto.cs
│       ├── Events/
│       │   ├── PurchaseOrderCreated.cs
│       │   ├── DeliveryReceived.cs
│       │   └── ReorderAlertReceived.cs
│       └── Enums/
│           └── PurchaseOrderStatus.cs
├── SpaceOS.Modules.Contracts.Tests/
│   ├── Cutting/
│   │   └── CuttingContractTests.cs
│   ├── Inventory/
│   │   └── InventoryContractTests.cs
│   ├── Procurement/
│   │   └── ProcurementContractTests.cs
│   └── Shared/
│       ├── ModuleEventTests.cs
│       └── ProviderCapabilityTests.cs
├── SpaceOS.Modules.Contracts.csproj
├── CLAUDE.md
└── README.md
```

---

## 4. Domain modell

### 4.1 Shared base

```csharp
// Shared/IModuleProvider.cs
/// <summary>
/// Base interface for all module providers (cutting, inventory, procurement).
/// Every implementation MUST return within 5s on HealthCheckAsync — timeout = false.
/// Consumer MUST verify Capabilities before calling optional methods.
/// </summary>
public interface IModuleProvider
{
    string ProviderName { get; }
    ProviderCapability Capabilities { get; }
    Task<bool> HealthCheckAsync(CancellationToken ct);
}

// Shared/ProviderCapability.cs
[Flags]
public enum ProviderCapability
{
    None = 0,
    // Cutting
    CuttingSubmit       = 1 << 0,
    CuttingNesting      = 1 << 1,
    CuttingExecution    = 1 << 2,
    CuttingWaste        = 1 << 3,
    // Inventory
    InventoryStock      = 1 << 4,
    InventoryOffcut     = 1 << 5,
    InventoryTrend      = 1 << 6,
    InventoryLocation   = 1 << 7,
    // Procurement
    ProcurementOrder    = 1 << 8,
    ProcurementPricing  = 1 << 9,
    ProcurementRating   = 1 << 10,
}

// Shared/ModuleEvent.cs
/// <summary>
/// Base record for all module events. EventId is auto-generated.
/// CONSUMER MUST verify that TenantId matches the current JWT TenantId claim.
/// </summary>
public abstract record ModuleEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public required Guid TenantId { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
}
```

### 4.2 ICuttingProvider

```csharp
/// <summary>
/// Contract for cutting services. Implementations: SpaceOS built-in, OptiCut, CutRite, manual.
/// TenantId is NOT in request DTOs — the implementation resolves it from JWT.
/// </summary>
public interface ICuttingProvider : IModuleProvider
{
    // Sheet management — requires CuttingSubmit capability
    Task<Result<Guid>> SubmitCuttingSheetAsync(
        SubmitCuttingSheetRequest request, CancellationToken ct);

    Task<Result<CuttingSheetDto>> GetCuttingSheetAsync(
        Guid sheetId, CancellationToken ct);

    // BE-01: query by source entity (Joinery/Cabinet needs this)
    Task<Result<IReadOnlyList<CuttingSheetDto>>> GetCuttingSheetsBySourceAsync(
        Guid sourceEntityId, CancellationToken ct);

    // Nesting — requires CuttingNesting capability
    Task<Result<NestingResultDto>> GetNestingResultAsync(
        Guid sheetId, CancellationToken ct);

    // Execution — requires CuttingExecution capability
    Task<Result<ExecutionStatusDto>> GetExecutionStatusAsync(
        Guid sheetId, CancellationToken ct);

    // Reporting — requires CuttingWaste capability
    Task<Result<WasteReportDto>> GetWasteReportAsync(
        DateTimeOffset from, DateTimeOffset to,
        CancellationToken ct);
}
```

### 4.3 IInventoryProvider

```csharp
/// <summary>
/// Contract for inventory/stock management.
/// TenantId resolved from JWT — not in request parameters.
/// </summary>
public interface IInventoryProvider : IModuleProvider
{
    // Stock queries — requires InventoryStock capability
    Task<Result<IReadOnlyList<StockItemDto>>> GetStockAsync(
        string materialCode, CancellationToken ct);

    // Offcut queries — requires InventoryOffcut capability
    Task<Result<IReadOnlyList<StockItemDto>>> GetUsableOffcutsAsync(
        string materialCode, decimal minWidth, decimal minHeight,
        CancellationToken ct);

    // Stock mutations
    Task<Result> RecordConsumptionAsync(
        IReadOnlyList<StockMovementDto> movements, CancellationToken ct);

    Task<Result<Guid>> RecordOffcutAsync(
        CuttingOffcutResultDto offcut, CancellationToken ct);

    Task<Result> RecordInboundAsync(
        IReadOnlyList<InboundReceiptDto> items, CancellationToken ct);

    // Trends — requires InventoryTrend capability
    Task<Result<ConsumptionTrendDto>> GetConsumptionTrendAsync(
        string materialCode, DateTimeOffset from, DateTimeOffset to,
        CancellationToken ct);
}
```

### 4.4 IProcurementProvider

```csharp
/// <summary>
/// Contract for procurement/purchasing.
/// TenantId resolved from JWT — not in request parameters.
/// </summary>
public interface IProcurementProvider : IModuleProvider
{
    // Purchase orders — requires ProcurementOrder capability
    Task<Result<Guid>> CreatePurchaseOrderAsync(
        CreatePurchaseOrderRequest request, CancellationToken ct);

    Task<Result<PurchaseOrderDto>> GetPurchaseOrderAsync(
        Guid orderId, CancellationToken ct);

    // Pricing — requires ProcurementPricing capability
    Task<Result<IReadOnlyList<PriceListEntryDto>>> GetPricesAsync(
        string materialCode, CancellationToken ct);

    // Delivery
    Task<Result> RecordDeliveryAsync(
        DeliveryDto delivery, CancellationToken ct);

    // Supplier — requires ProcurementRating capability
    Task<Result<SupplierRatingDto>> GetSupplierRatingAsync(
        Guid supplierId, CancellationToken ct);
}
```

### 4.5 Request DTOs (SEC-01: nincs TenantId)

```csharp
// Cutting request — TenantId a JWT-ből jön
/// <summary>
/// Max constraints: Lines ≤ 200, CncInstructions ≤ 500, ProcessSteps ≤ 50.
/// ComponentName ≤ 100 chars, MaterialCode ≤ 20 chars.
/// </summary>
public sealed record SubmitCuttingSheetRequest(
    Guid SourceEntityId,
    string SourceModuleType,            // "door" / "cabinet" / "window"
    Guid? SourceItemId,
    string TemplateName,                // max 100
    int TemplateVersion,
    decimal InputWidth,
    decimal InputHeight,
    string? ParameterOverridesJson,     // max 10KB
    IReadOnlyList<CuttingLineDto> Lines,
    IReadOnlyList<CncInstructionDto> CncInstructions,
    IReadOnlyList<ProcessStepDto> ProcessSteps);

// Procurement request — TenantId a JWT-ből jön
public sealed record CreatePurchaseOrderRequest(
    Guid SupplierId,
    IReadOnlyList<PurchaseOrderLineDto> Lines,
    DateTimeOffset? ExpectedDelivery,
    string? Notes);                     // max 2000

// Inventory inbound — TenantId a JWT-ből jön
/// <summary>
/// MaterialCode: max 20 chars, ISO format recommended.
/// QualityNote: max 2000 chars.
/// </summary>
public sealed record InboundReceiptDto(
    string MaterialCode,
    int Quantity,
    decimal Width, decimal Height, decimal Thickness,
    string? DeliveryReference,
    string? QualityNote);
```

### 4.6 Response DTOs

```csharp
// --- Cutting ---
public sealed record CuttingSheetDto(
    Guid Id,
    Guid TenantId,
    Guid SourceEntityId,
    string SourceModuleType,
    Guid? SourceItemId,
    string TemplateName,
    int TemplateVersion,
    decimal InputWidth,
    decimal InputHeight,
    string? ParameterOverridesJson,
    string ContentHash,
    DateTimeOffset CalculatedAt,
    CuttingSheetStatus Status,
    IReadOnlyList<CuttingLineDto> Lines,
    IReadOnlyList<CncInstructionDto> CncInstructions,
    IReadOnlyList<ProcessStepDto> ProcessSteps);

/// <summary>CanRotate: dekorminta engedi-e a 90° forgatást (nesting-hez)</summary>
public sealed record CuttingLineDto(
    string ComponentName, string ComponentType,
    decimal Width, decimal Height,
    decimal CuttingWidth, decimal CuttingHeight,
    string Material, decimal Thickness,
    int Quantity, int SortOrder,
    bool CanRotate);

public sealed record CncInstructionDto(
    string ComponentName, string Operation,
    string? Position, decimal? Diameter,
    decimal? Depth, decimal? Angle, string? Note);

public sealed record ProcessStepDto(
    string Phase, int StepOrder,
    string Description, int EstimatedSeconds);

public sealed record NestingResultDto(
    Guid SheetId,
    IReadOnlyList<PanelAssignmentDto> Assignments,
    decimal TotalWastePercentage,
    int PanelsUsed);

public sealed record PanelAssignmentDto(
    Guid PanelStockId,
    string MaterialCode,
    decimal PanelWidth, decimal PanelHeight,
    IReadOnlyList<PlacedPieceDto> Pieces,
    IReadOnlyList<CuttingOffcutResultDto> ResultingOffcuts,
    decimal WasteArea);

public sealed record PlacedPieceDto(
    int CuttingLineIndex,
    string ComponentName,
    decimal X, decimal Y,
    decimal Width, decimal Height,
    bool IsRotated);

public sealed record CuttingOffcutResultDto(
    string MaterialCode,
    decimal Width, decimal Height, decimal Thickness,
    Guid OriginSheetId);

public sealed record ExecutionStatusDto(
    Guid SheetId,
    CuttingExecutionStatus Status,
    Guid? OperatorId,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt);

public sealed record WasteReportDto(
    DateTimeOffset From, DateTimeOffset To,
    decimal TotalMaterialUsedM2,
    decimal TotalWasteM2,
    decimal WastePercentage,
    IReadOnlyList<WasteByMaterialDto> ByMaterial);

public sealed record WasteByMaterialDto(
    string MaterialCode, decimal UsedM2, decimal WasteM2);

// --- Inventory ---
public sealed record StockItemDto(
    Guid Id,
    string MaterialCode,
    decimal Width, decimal Height, decimal Thickness,
    StockItemType ItemType,
    int Quantity,
    string? LocationCode);

public sealed record StockMovementDto(
    Guid StockItemId,
    StockMovementType MovementType,
    int Quantity,
    StockReferenceType ReferenceType,
    Guid? ReferenceId);

public sealed record MaterialDto(
    string Code,
    string Name,
    MaterialCategory Category,
    decimal StandardWidth,
    decimal StandardHeight,
    decimal Thickness,
    string? Unit);

public sealed record StockLocationDto(
    Guid Id,
    string Code,
    string? Description);

public sealed record ConsumptionTrendDto(
    string MaterialCode,
    DateTimeOffset From, DateTimeOffset To,
    decimal TotalConsumed,
    decimal AverageDaily,
    int? EstimatedDaysUntilStockout);

// --- Procurement ---
public sealed record PurchaseOrderDto(
    Guid Id,
    Guid TenantId,
    Guid SupplierId,
    PurchaseOrderStatus Status,
    IReadOnlyList<PurchaseOrderLineDto> Lines,
    DateTimeOffset? ExpectedDelivery,
    DateTimeOffset CreatedAt,
    string? Notes);

public sealed record PurchaseOrderLineDto(
    string MaterialCode,
    int Quantity,
    decimal? UnitPrice,
    string? Currency);                  // ISO 4217, max 3 chars

public sealed record SupplierDto(
    Guid Id,
    string Name,
    string? ContactEmail,
    int LeadTimeDays,
    string? Notes);

public sealed record PriceListEntryDto(
    Guid SupplierId,
    string SupplierName,
    string MaterialCode,
    decimal UnitPrice,
    string Currency,
    int? MinimumQuantity,
    DateTimeOffset? ValidUntil);

public sealed record DeliveryDto(
    Guid PurchaseOrderId,
    DateTimeOffset DeliveredAt,
    IReadOnlyList<DeliveryLineDto> Lines);

public sealed record DeliveryLineDto(
    string MaterialCode,
    int QuantityOrdered,
    int QuantityReceived,
    string? QualityNote);

public sealed record SupplierRatingDto(
    Guid SupplierId,
    decimal DeliveryAccuracy,
    decimal QualityScore,
    decimal PriceCompetitiveness,
    int TotalOrders,
    int LateDeliveries);
```

### 4.7 Enums

```csharp
// Cutting
public enum CuttingExecutionStatus
{ Planned, InProgress, Completed, Failed }

public enum CuttingSheetStatus
{ Received, Queued, InNesting, Nested, InExecution, Completed }

// Inventory
public enum StockMovementType
{ Inbound, Consumed, OffcutCreated, Scrapped, Adjusted, Returned }

public enum StockItemType
{ FullPanel, Offcut }

public enum StockReferenceType
{ CuttingSheet, PurchaseOrder, Manual, StockCount, Return }

public enum MaterialCategory
{ Board, Edge, Veneer, Hardware, Adhesive, Other }

// Procurement
public enum PurchaseOrderStatus
{ Draft, Submitted, Confirmed, Shipped, Delivered, Cancelled }
```

### 4.8 Events

```csharp
// --- Cutting Events ---
public sealed record CuttingSheetReceived : ModuleEvent
{
    public required Guid SheetId { get; init; }
    public required Guid SourceEntityId { get; init; }
    public required string SourceModuleType { get; init; }
}

public sealed record NestingCompleted : ModuleEvent
{
    public required Guid SheetId { get; init; }
    public required int PanelsUsed { get; init; }
    public required decimal WastePercentage { get; init; }
}

public sealed record CuttingCompleted : ModuleEvent
{
    public required Guid SheetId { get; init; }
    public required Guid OperatorId { get; init; }
    public required IReadOnlyList<Guid> OffcutIds { get; init; }
}

public sealed record CuttingFailed : ModuleEvent
{
    public required Guid SheetId { get; init; }
    public required string Reason { get; init; }    // max 2000
}

// --- Inventory Events ---
public sealed record StockConsumed : ModuleEvent
{
    public required string MaterialCode { get; init; }
    public required int Quantity { get; init; }
    public required StockReferenceType ReferenceType { get; init; }
    public required Guid? ReferenceId { get; init; }
}

public sealed record StockReceived : ModuleEvent
{
    public required string MaterialCode { get; init; }
    public required int Quantity { get; init; }
    public required string? DeliveryReference { get; init; }
}

public sealed record OffcutRegistered : ModuleEvent
{
    public required Guid OffcutId { get; init; }
    public required string MaterialCode { get; init; }
    public required decimal Width { get; init; }
    public required decimal Height { get; init; }
}

public sealed record LowStockAlert : ModuleEvent
{
    public required string MaterialCode { get; init; }
    public required int CurrentQuantity { get; init; }
    public required int ThresholdQuantity { get; init; }
}

// --- Procurement Events ---
public sealed record PurchaseOrderCreated : ModuleEvent
{
    public required Guid OrderId { get; init; }
    public required Guid SupplierId { get; init; }
}

public sealed record DeliveryReceived : ModuleEvent
{
    public required Guid OrderId { get; init; }
    public required Guid DeliveryId { get; init; }
}

public sealed record ReorderAlertReceived : ModuleEvent
{
    public required string MaterialCode { get; init; }
    public required int CurrentQuantity { get; init; }
    public required int SuggestedQuantity { get; init; }
}
```

---

## 5. Project file

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <PackageId>SpaceOS.Modules.Contracts</PackageId>
    <Version>1.0.0</Version>
    <Authors>SpaceOS</Authors>
    <Description>Shared module contracts for SpaceOS ecosystem: Cutting, Inventory, Procurement</Description>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Ardalis.Result" Version="9.*" />
  </ItemGroup>
</Project>
```

---

## 6. Definition of Done

### Package gates
- [ ] `dotnet build` → 0 error, 0 warning
- [ ] `GenerateDocumentationFile` → XML doc minden public type-on
- [ ] Ardalis.Result az egyetlen PackageReference
- [ ] `net8.0` TargetFramework
- [ ] `TreatWarningsAsErrors` true

### Interface gates
- [ ] `IModuleProvider` base: ProviderName + Capabilities (Flags enum) + HealthCheckAsync
- [ ] `ICuttingProvider` : 6 metódus (Submit, Get, GetBySource, Nesting, Execution, Waste)
- [ ] `IInventoryProvider` : 7 metódus (GetStock, GetOffcuts, RecordConsumption, RecordOffcut, RecordInbound, GetTrend)
- [ ] `IProcurementProvider` : 5 metódus (CreatePO, GetPO, GetPrices, RecordDelivery, GetRating)
- [ ] Minden metódus: `Result<T>` return + `CancellationToken ct` paraméter
- [ ] Nincs `TenantId` egyetlen request DTO-ban sem (SEC-01)

### DTO gates
- [ ] Input DTO-k: `SubmitCuttingSheetRequest`, `CreatePurchaseOrderRequest`, `InboundReceiptDto` — nincs Id, nincs TenantId
- [ ] Output DTO-k: `CuttingSheetDto`, `PurchaseOrderDto` — required Id, required TenantId
- [ ] `CuttingLineDto.CanRotate` mező (nesting support)
- [ ] `PlacedPieceDto.CuttingLineIndex` mező (CD-09)
- [ ] `StockMovementDto`: `StockReferenceType` enum + `ReferenceId` (CD-06)
- [ ] `ConsumptionTrendDto.EstimatedDaysUntilStockout` nullable int (CD-10)
- [ ] XML doc max length-ek: ComponentName 100, MaterialCode 20, Notes 2000, Currency 3

### Event gates
- [ ] `ModuleEvent` base: auto-generated EventId, required TenantId, required OccurredAt
- [ ] Cutting: 4 event (SheetReceived, NestingCompleted, CuttingCompleted, CuttingFailed)
- [ ] Inventory: 4 event (StockConsumed, StockReceived, OffcutRegistered, LowStockAlert)
- [ ] Procurement: 3 event (PurchaseOrderCreated, DeliveryReceived, ReorderAlertReceived)
- [ ] Nincs OnEvent callback (CD-04)

### Enum gates
- [ ] `ProviderCapability` [Flags] enum — 11 értékkel (CD-02)
- [ ] `CuttingExecutionStatus`: Planned, InProgress, Completed, Failed
- [ ] `CuttingSheetStatus`: Received, Queued, InNesting, Nested, InExecution, Completed
- [ ] `StockMovementType`: Inbound, Consumed, OffcutCreated, Scrapped, Adjusted, Returned
- [ ] `StockReferenceType`: CuttingSheet, PurchaseOrder, Manual, StockCount, Return
- [ ] `PurchaseOrderStatus`: Draft, Submitted, Confirmed, Shipped, Delivered, Cancelled

### Test gates
- [ ] Minden enum értéke string-re serializálható és vissza (JsonStringEnumConverter kompatibilitás)
- [ ] `ModuleEvent.EventId` auto-generated és unique (2 instance != same Id)
- [ ] `ProviderCapability` Flags: HasFlag compose/decompose helyes
- [ ] DTO record equality: azonos mezőkkel azonos hash
- [ ] ≥ 15 teszt összesen

### Összesített
- [ ] 0 build warning
- [ ] `dotnet list package --vulnerable` → 0 high/critical
- [ ] README.md: SemVer stratégia dokumentálva (BE-03)
- [ ] CLAUDE.md: implementációs útmutató Claude Code-nak

---

## 7. Security adósság

| ID | Tétel | Ez a fázis | Marad |
|----|-------|------------|-------|
| SEC-01 | TenantId trust boundary | ✅ request DTO-kból törölve | — |
| SEC-02 | DTO size limits | ✅ XML doc constraint-ek | Implementáció kényszeríti |
| SEC-03 | Event TenantId validáció | ✅ XML doc figyelmeztetés | Implementáció kényszeríti |
| SEC-05 | Capability spoof | ✅ XML doc + NotSupportedException | Implementáció kényszeríti |
| Escrow GA | S3 Object Lock | — | Future |
| P2-3 | GDPR pseudo | — | Future |

---

## 8. Mi jön utána

| Sorrend | Téma | Prereq |
|---------|------|--------|
| 1 | **Inventory Core** implementáció | Contracts DONE |
| 2 | **Cutting Core** implementáció | Contracts + Inventory Core DONE |
| 3 | **Procurement Core** implementáció | Contracts DONE |
| 4 | Joinery v2 (Cutting integrálás) | Cutting Core DEPLOYED |
| 5 | Adapters (OptiCut, WMS, ERP) | Contracts DONE |

---

## 9. Claude Code implementációs csomag

### Végrehajtási sorrend

| Nap | Feladat | Track | Függőség |
|-----|---------|-------|----------|
| 1 | Repo scaffold: solution + csproj + CLAUDE.md + README.md | Setup | — |
| 1 | Shared: IModuleProvider + ProviderCapability + ModuleEvent | A-Shared | — |
| 2 | Cutting: ICuttingProvider + Request + 10 DTO + 4 Event + 2 Enum | B-Cutting | Nap 1 |
| 2 | Inventory: IInventoryProvider + InboundReceiptDto + 5 DTO + 4 Event + 4 Enum | C-Inventory | Nap 1 |
| 3 | Procurement: IProcurementProvider + Request + 7 DTO + 3 Event + 1 Enum | D-Procurement | Nap 1 |
| 3 | Tesztek: ≥ 15 (enum serialize, event identity, capability flags, DTO equality) | E-Test | Nap 1-3 |

### Agent utasítás

> "Implementáld a `SpaceOS_Modules_Contracts_Architecture_v4.md` alapján:
>
> Track A: Shared — IModuleProvider, ProviderCapability [Flags], ModuleEvent (auto EventId)
> Track B: Cutting — ICuttingProvider (6 metódus), SubmitCuttingSheetRequest, 10 DTO, 4 Event, 2 Enum
> Track C: Inventory — IInventoryProvider (7 metódus), InboundReceiptDto, 5 DTO, 4 Event, 4 Enum
> Track D: Procurement — IProcurementProvider (5 metódus), CreatePurchaseOrderRequest, 7 DTO, 3 Event, 1 Enum
> Track E: ≥ 15 teszt
>
> KRITIKUS: TenantId NINCS request DTO-kban (SEC-01). XML doc minden public type-on.
> DoD: #6 section · Gate: `dotnet build && dotnet test`"

### Kockázatok

| Kockázat | P | Hatás | Mitigáció |
|----------|---|-------|-----------|
| DTO breaking change a Core implementáció közben | Közepes | NuGet verzió bump, downstream rebuild | SemVer: MINOR amíg nincs production consumer |
| Ardalis.Result verzió inkompatibilitás a Kernel 9.x-szel | Alacsony | Build fail | Explicit version pin a csproj-ban |
| Túl sok DTO — nehéz átlátni | Alacsony | Maintenance | Folder-per-module struktúra, XML doc |

---

*SpaceOS — Shared Module Contracts Architecture v4.0*
*Interface Design + `/senior-security` + `/senior-backend` reviewed · 2026-04-12*
*Státusz: IMPLEMENTÁCIÓRA KÉSZ — 18 finding beépítve, minden döntés lezárva*
