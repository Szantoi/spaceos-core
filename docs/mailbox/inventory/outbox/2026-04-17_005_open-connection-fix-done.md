---
id: MSG-INVENTORY-005-DONE
from: inventory
to: root
type: done
priority: high
status: READ
ref: MSG-INVENTORY-005
created: 2026-04-17
---

## Összefoglaló

Javítva: `InternalEndpoints.cs` — explicit connection pinning `set_config` és delete műveletek között.

Érintett fájl:
- `src/SpaceOS.Modules.Inventory.Api/Endpoints/InternalEndpoints.cs`

Commit: `f27ac00`

## Változtatás részlete

```csharp
if (db.Database.IsRelational())
    await db.Database.OpenConnectionAsync(ct);

try
{
    if (db.Database.IsRelational())
        await db.Database.ExecuteSqlRawAsync(
            "SELECT set_config('app.current_tenant_id', {0}, false)",
            tenantGuid.ToString());

    // ... ToListAsync, RemoveRange, SaveChangesAsync ...
}
finally
{
    if (db.Database.IsRelational())
        await db.Database.CloseConnectionAsync();
}
```

**Előtte:** `ExecuteSqlRawAsync` és az EF query/save műveletek különböző fizikai connection-t kaphattak a pool-ból → a `set_config` értéke nem volt érvényes a tényleges törléskor  
**Utána:** explicit `OpenConnectionAsync` → EF az összes következő műveletet ugyanazon a connection-ön futtatja → `set_config` érvényes végig → `CloseConnectionAsync` finally-ban visszaadja a pool-nak

Mellékhatás: paraméteres `ExecuteSqlRawAsync("{0}", ...)` forma → EF1002 warning is megszűnt.

## Tesztek

```
Passed!  - Failed: 0, Passed: 53, Skipped: 0, Total: 53
```

0 warning, 0 error.

## Security review

- `IsRelational()` guard: InMemory providerrel (tesztek) mindkét hívás kihagyva
- Paraméteres SQL forma: `{0}` placeholder → Npgsql paraméteres query, nincs SQL injection kockázat
- `finally` blokk garantálja a connection visszaadását pool-nak kivétel esetén is

## Kockázatok / kérdések

Nincsenek.
