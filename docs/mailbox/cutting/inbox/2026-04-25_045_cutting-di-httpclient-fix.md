---
id: MSG-CUTTING-045
from: root
to: cutting
type: task
priority: critical
status: READ
created: 2026-04-25
---

# CUTTING-045 — DI HttpClient duplikáció fix (deploy blocker)

> **BUG:** Service startup crash: `InvalidOperationException: HttpClient factory already has a registered client with the name 'IInventoryProvider'`
> **Root cause:** A `PanelSourceService` DI regisztráció duplikálja az `IInventoryProvider` HttpClient-et.

## Fix

**Fájl:** `Infrastructure/Extensions/ServiceCollectionExtensions.cs` (sor ~54)

A `PanelSourceService` ne regisztráljon újabb `AddHttpClient<IInventoryProvider>` — az már korábban regisztrálva van. Használj `string` alapú nevet vagy egyszerűen `AddScoped<PanelSourceService>` (ha nem kell saját HttpClient).

```csharp
// ROSSZ — duplikált HttpClient regisztráció:
services.AddHttpClient<IInventoryProvider, ContractsInventoryHttpAdapter>(...);  // EZ MÁR LÉTEZIK
services.AddHttpClient<IInventoryProvider, ...>(...);  // EZ DUPLIKÁLJA

// HELYES — PanelSourceService egyszerű scoped, az IInventoryProvider-t DI-ból kapja:
services.AddScoped<PanelSourceService>();
```

## Tesztek

Meglévő tesztek nem törhetnek el. A startup crash fix után `dotnet run` tesztelhető lokálisan.

## Definition of Done

- [ ] Service elindul crash nélkül
- [ ] `dotnet build` 0 error
- [ ] `dotnet test` ≥ 303 pass
- [ ] Outbox DONE
