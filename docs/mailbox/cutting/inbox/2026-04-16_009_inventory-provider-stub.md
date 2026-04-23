---
id: MSG-CUTTING-009
from: root
to: cutting
type: task
priority: high
status: READ
ref: MSG-E2E-045-DONE
created: 2026-04-16
---

# MSG-CUTTING-009 — IInventoryProvider stub regisztráció a cutting modul DI-ban

## Gyökérok

```
System.InvalidOperationException:
  Unable to resolve service for type
  'SpaceOS.Modules.Inventory.Contracts.Providers.IInventoryProvider'
  while attempting to activate
  'GetNestingResultQueryHandler'
```

A `GetNestingResultQueryHandler` (`/api/cutting/sheets/{id}/nesting`) `IInventoryProvider`-t
igényel, de az `AddCuttingInfrastructure()` nem regisztrálja.

A handler már tartalmaz graceful degradation-t (try/catch):
```csharp
catch
{
    // Inventory unavailable — return grouping only
    return Result<NestingResultResponse>.Success(new NestingResultResponse(...grouping only...));
}
```

Tehát ha az inventory hívás dob (vagy üres adatot ad), a handler **200-t** ad vissza grouping-only
response-zal. Az E2E test ezt elfogadja.

## Fix — 2 lépés

### 1. InventoryProviderStub létrehozása

```csharp
// SpaceOS.Modules.Cutting.Infrastructure/Adapters/InventoryProviderStub.cs

using SpaceOS.Modules.Inventory.Contracts.DTOs;
using SpaceOS.Modules.Inventory.Contracts.Providers;

namespace SpaceOS.Modules.Cutting.Infrastructure.Adapters;

/// <summary>
/// Stub implementation of IInventoryProvider for the cutting service.
/// Returns empty stock — the nesting handler degrades gracefully (grouping-only response).
/// Replace with HTTP adapter when cross-service inventory calls are implemented (Q3).
/// </summary>
internal sealed class InventoryProviderStub : IInventoryProvider
{
    public Task<StockLevelResponse> GetStockAsync(string materialType, CancellationToken ct = default)
        => Task.FromResult(new StockLevelResponse(materialType, FullPanelCount: 0, WidthMm: 0, HeightMm: 0));

    public Task<IReadOnlyList<OffcutDto>> GetOffcutsAsync(string materialType, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<OffcutDto>>(Array.Empty<OffcutDto>());
}
```

### 2. Regisztráció — ServiceCollectionExtensions.cs

```csharp
// AddCuttingInfrastructure metódusba:
services.AddScoped<IInventoryProvider, InventoryProviderStub>();
```

## Megjegyzések

- A stub üres készletet ad vissza → `availablePanels.Any()` false → `panelAssignments = null`
  → handler 200-t ad vissza grouping-only response-zal → E2E teszt PASS
- Alternatíva lett volna HTTP adapter (cross-service hívás az inventory service-re),
  de Q3 scope: most a stub elegendő a Doorstar Soft Launch-hoz
- A Joinery modul precedens: `CuttingProviderStub` ugyanígy stub-ként lett regisztrálva
  (JOINERY-007) — ez a bevett minta az ökoszisztémában

## IInventoryProvider interfész ellenőrzés

```bash
# Megtalálod a metódus szignatúrákat:
cat /opt/spaceos/spaceos-modules-cutting/src/SpaceOS.Modules.Inventory.Contracts/Providers/IInventoryProvider.cs
```

Ha az interfész más metódusokat tartalmaz, a stub-ban azokat is implementáld (üres/default return).

## DoD

- [ ] `InventoryProviderStub.cs` létrehozva a cutting Infrastructure/Adapters/ mappában
- [ ] `AddScoped<IInventoryProvider, InventoryProviderStub>()` hozzáadva a DI extension-hez
- [ ] `dotnet build` → 0 error, 0 warning
- [ ] `dotnet test` → 153/153 (minimum) zöld
- [ ] Commit
- [ ] Outbox: `MSG-CUTTING-009-DONE`
