---
id: MSG-JOINERY-008
from: root
to: joinery
type: task
priority: high
status: READ
ref: CUTTING-006-DONE
created: 2026-04-16
---

# MSG-JOINERY-008 — ICuttingProvider integráció (DoorOrder Submit → CuttingSheet)

## Kontextus

A Modules.Cutting v1 VPS-en él (5004/5005/5006). A Joinery modul jelenleg **nem hívja** az ICuttingProvider-t — a DoorOrder Submit flow nem regisztrálja a CuttingSheet-et.

Ez a Doorstar Soft Launch kritikus integrációja: a kalkulált ajtórendelésből automatikusan kell CuttingSheet kerüljön a Cutting modulba.

## Feladat

### 1. NuGet referencia hozzáadása

```bash
dotnet add src/SpaceOS.Modules.Joinery.Application/ package SpaceOS.Modules.Cutting.Contracts
```

A package a `/opt/spaceos/spaceos-modules-cutting/nupkg/` mappában van. Ha lokálisan kell:
```bash
dotnet add src/SpaceOS.Modules.Joinery.Application/ package SpaceOS.Modules.Cutting.Contracts \
  --source /opt/spaceos/spaceos-modules-cutting/nupkg/
```

### 2. ICuttingProvider regisztrálás

A Joinery DI containerébe kell egy `ICuttingProvider` implementáció. Első lépésként **HTTP kliens adapter** — a `/bff/cutting/sheets` BFF végponton keresztül hívja a Cutting service-t.

```csharp
// CuttingProviderHttpAdapter : ICuttingProvider
// - SubmitCuttingSheetAsync() → POST http://127.0.0.1:5005/api/cutting/sheets
// - Többi metódus: NotImplementedException (nem kell most)
```

Vagy ha egyszerűbb: **Mock/Stub implementáció** a first launch-ra (loggol, nem hív sehova), aztán Q3-ban cseréljük valódira.

**Döntés a te kezedben** — ha a HTTP adapter 2 óránál több, válaszd a Stub-ot és jelezd az outbox-ban.

### 3. SubmitDoorOrder handler módosítása

Az `Application/Orders/SubmitDoorOrderCommandHandler.cs`-ben (vagy ahol a Submit logika van):

```csharp
// A DoorOrder Submit után, ha Status == Submitted:
var cuttingSheet = new CuttingSheetRequest
{
    OrderReference = order.Id.ToString(),
    Lines = order.Items.Select(i => new CuttingLineRequest
    {
        PartName = i.PartName,
        MaterialType = i.MaterialType,
        WidthMm = i.WidthMm,
        HeightMm = i.HeightMm,
        ThicknessMm = i.ThicknessMm,
        Quantity = i.Quantity
    }).ToList()
};

var result = await _cuttingProvider.SubmitCuttingSheetAsync(cuttingSheet, ct);
// Ha result.IsFailure: log warning, NE dobjon exception-t (graceful degradation)
// A DoorOrder Submit sikeres legyen akkor is, ha a Cutting service nem elérhető
```

**Kritikus:** graceful degradation — ha a Cutting service nem elérhető, a DoorOrder Submit **ne bukjon meg**. Log warning, folytassa.

### 4. Tesztek

Minimum 10 új teszt:
- `SubmitDoorOrder` → `ICuttingProvider.SubmitCuttingSheetAsync()` meghívódik (mock verify)
- Graceful degradation: ha provider throws → order submit sikeres, warning logolva
- CuttingSheet DTO mapping: DoorOrder items → CuttingLine-ok helyesek
- Happy path: provider returns success → order status Submitted

### 5. Build + test gate

```bash
dotnet build   → 0 error, 0 warning
dotnet test    → meglévő 202 + 10 új = ≥212 pass
```

## DoD

- [ ] `SpaceOS.Modules.Cutting.Contracts` NuGet referencia hozzáadva
- [ ] `ICuttingProvider` implementáció DI-ban regisztrálva (akár Stub)
- [ ] `SubmitDoorOrder` hívja az `ICuttingProvider.SubmitCuttingSheetAsync()`-t
- [ ] Graceful degradation: provider hiba → order submit nem bukik meg
- [ ] `dotnet build` → 0 error, 0 warning
- [ ] `dotnet test` → ≥212 pass
- [ ] Outbox: `MSG-JOINERY-008-DONE`
