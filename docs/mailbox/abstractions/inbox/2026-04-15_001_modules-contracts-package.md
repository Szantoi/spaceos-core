---
id: MSG-ABSTRACTIONS-001
from: root
to: abstractions
type: task
priority: high
status: READ
ref: SpaceOS_Modules_Contracts_Architecture_v4
created: 2026-04-15
---

# MSG-ABSTRACTIONS-001 — SpaceOS.Modules.Contracts NuGet package

## Összefoglaló

**Sprint 4 párhuzamos feladata.** Teljes specifikáció: `docs/tasks/new/SpaceOS_Modules_Contracts_Architecture_v4.md`

Új polyrepo: `spaceos-modules-contracts` — ICuttingProvider + IInventoryProvider + IProcurementProvider shared interface package.

**Becsült effort:** ~3 fejlesztői nap  
**Blokkoló:** Modules.Abstractions Phase B DONE ✅ · ADR-019 ✅

---

## Repo és projekt struktúra

```
/opt/spaceos/spaceos-modules-contracts/
  SpaceOS.Modules.Contracts/
    SpaceOS.Modules.Contracts.csproj    (Class Library, net8.0, csak Ardalis.Result dependency)
    Cutting/
      ICuttingProvider.cs
      Dtos/
        SubmitCuttingSheetRequest.cs    (input DTO — nincs TenantId, SEC-01)
        CuttingSheetDto.cs              (output DTO — van TenantId read-only)
        CuttingOffcutResultDto.cs
    Inventory/
      IInventoryProvider.cs
      Dtos/
        StockItemDto.cs
        StockMovementDto.cs
        InboundReceiptDto.cs
    Procurement/
      IProcurementProvider.cs
      Dtos/
        SubmitPurchaseOrderRequest.cs
        PurchaseOrderDto.cs
    Shared/
      ProviderCapability.cs             ([Flags] enum)
      ProviderHealthStatus.cs
      ModuleEvent.cs                    (base record, auto EventId)
  SpaceOS.Modules.Contracts.Tests/
    ...
  README.md
```

---

## Kulcsdöntések (v4 findings alapján)

### SEC-01: TenantId NINCS a request DTO-kban

```csharp
// HELYES (v4):
public record SubmitCuttingSheetRequest(
    Guid SourceEntityId,
    string ComponentName,       // [MaxLength(100)] — SEC-04
    // TenantId NINCS — JWT-ből veszi az implementáció
    IReadOnlyList<CuttingLineDto> Lines   // max 200 — SEC-02
);

// HELYTELEN lenne:
// public record SubmitCuttingSheetRequest(Guid TenantId, ...); // SPOOFABLE!
```

### CD-01: Input/Output DTO szétválasztás

```csharp
// Input (nincs Id):
public record SubmitCuttingSheetRequest(...);

// Output (van Id, TenantId read-only):
public record CuttingSheetDto(Guid Id, Guid TenantId, ...);
```

### CD-02: ProviderCapability — [Flags] enum

```csharp
[Flags]
public enum ProviderCapability
{
    None        = 0,
    Nesting     = 1 << 0,   // Automatic nesting algorithm
    CncExport   = 1 << 1,   // CNC G-code / DXF export
    Tracking    = 1 << 2,   // Real-time cut tracking
    Optimization = 1 << 3,  // Material yield optimization
}
```

### BE-01: GetCuttingSheetsBySourceAsync (v4 new method)

```csharp
public interface ICuttingProvider
{
    Task<Result<CuttingSheetDto>> SubmitCuttingSheetAsync(SubmitCuttingSheetRequest request, CancellationToken ct);
    Task<Result<CuttingSheetDto>> GetCuttingSheetAsync(Guid id, CancellationToken ct);
    Task<Result<IReadOnlyList<CuttingSheetDto>>> GetCuttingSheetsBySourceAsync(Guid sourceEntityId, CancellationToken ct); // BE-01
    Task<Result<bool>> HealthCheckAsync(CancellationToken ct);   // max 5s — BE-02
    ProviderCapability Capabilities { get; }
}
```

### Event pattern (CD-04 törölve — domain events maradnak)

```csharp
public abstract record ModuleEvent
{
    public Guid EventId { get; } = Guid.NewGuid();  // auto-generated — CD-03
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
    // NINCS: OnEvent(callback) — CD-04 törölve
}
```

---

## NuGet csomag beállítás

```xml
<PropertyGroup>
  <PackageId>SpaceOS.Modules.Contracts</PackageId>
  <Version>1.0.0</Version>
  <Authors>SpaceOS</Authors>
  <Description>Shared module provider contracts: ICuttingProvider, IInventoryProvider, IProcurementProvider</Description>
  <!-- MAJOR: breaking interface change, MINOR: új metódus, PATCH: DTO + doc — BE-03 -->
</PropertyGroup>
<ItemGroup>
  <PackageReference Include="Ardalis.Result" Version="8.*" />
</ItemGroup>
```

---

## DoD

- [ ] `spaceos-modules-contracts` repo + projekt struktúra
- [ ] `ICuttingProvider` + összes DTO (SEC-01 compliant, max-length XML docs)
- [ ] `IInventoryProvider` + összes DTO
- [ ] `IProcurementProvider` + összes DTO
- [ ] `ProviderCapability [Flags] enum`
- [ ] `ModuleEvent` base record (auto EventId)
- [ ] Unit tesztek (interface mock implementations, DTO validation edge cases)
- [ ] `dotnet pack` sikeres
- [ ] README: SemVer stratégia + fogyasztói útmutató
- [ ] Commit + push origin
- [ ] DONE outbox: projekt struktúra + teszt count + pack output
