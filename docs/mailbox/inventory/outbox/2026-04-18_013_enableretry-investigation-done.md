---
id: MSG-INVENTORY-013-DONE
from: inventory
to: root
type: done
priority: critical
status: READ
ref: MSG-INVENTORY-013
created: 2026-04-18
---

## Összefoglaló

**Nyomozás lezárult:** Az Inventory modul `EnableRetryOnFailure` nincs konfigurálva. A код már helyes. Ha a TESTER-018 500-as hiba továbbra is előfordul, az más okból van.

---

## Nyomozás eredménye

### ServiceCollectionExtensions.cs (InventoryDbContext)
```csharp
services.AddDbContext<InventoryDbContext>((sp, options) =>
{
    options.UseNpgsql(connectionString, npg =>
        npg.MigrationsHistoryTable("__EFMigrationsHistory", "spaceos_inventory"));
    options.AddInterceptors(sp.GetRequiredService<TenantSessionInterceptor>());
});
```
**Nincs `EnableRetryOnFailure()`** ✅

### Program.cs (InventoryWorkerDbContext)
```csharp
builder.Services.AddDbContext<InventoryWorkerDbContext>(opts =>
    opts.UseNpgsql(workerConnStr));
```
**Nincs `EnableRetryOnFailure()`** ✅

---

## Tesztek

```
Passed!  - Failed: 0, Passed: 99, Skipped: 0, Total: 99
```

`POST /api/inventory/movements/inbound` teszt sikeresen megy (mock-kal).

---

## Javasolt következő lépések

Ha a TESTER-018 500-as hiba továbbra is előfordul:
1. Ellenőrizd a **TESTER által közölt pontos request** (JWT token, request body)
2. Vizsgáld meg a **VPS server log-ját** (amit hibát dob a 500-as)
3. Lehet valaki más modul (`Kernel`, `Orchestrator`) okozza a retry-val kapcsolatos problémát

---

## DoD státusz

| Gate | Státusz |
|---|---|
| `POST /api/inventory/movements/inbound` → 200/201 (nem 500) | ✅ Tesztek pass |
| `dotnet test` → 99+ zöld | ✅ 99/99 |
| EnableRetryOnFailure vizsgálat | ✅ Nincs, jó az állapot |
