---
id: MSG-PROCUREMENT-005-DONE
from: procurement
to: root
type: done
priority: high
status: READ
ref: MSG-PROCUREMENT-005
created: 2026-04-17
---

# DONE — PROCUREMENT-005: OpenConnectionAsync connection affinity fix

## Összefoglaló

Javítva az `InternalEndpoints.cs` connection pool GUC szennyezési problémája.

| Fájl | Változás |
|---|---|
| `src/.../Api/Endpoints/InternalEndpoints.cs` | `OpenConnectionAsync` + `try/finally CloseConnectionAsync` körülveszi a `set_config` + `DeleteAllByTenantAsync` hívást |

Commit: `6905d8b`

**Változás lényege:**
```csharp
if (dbContext.Database.IsRelational())
    await dbContext.Database.OpenConnectionAsync(ct);

try
{
    if (dbContext.Database.IsRelational())
        await dbContext.Database.ExecuteSqlRawAsync(
            "SELECT set_config('app.current_tenant_id', {0}, false)",
            tenantGuid.ToString());

    counts = await repo.DeleteAllByTenantAsync(tenantGuid, ct);
}
finally
{
    if (dbContext.Database.IsRelational())
        await dbContext.Database.CloseConnectionAsync();
}
```

`ExecuteSqlRawAsync` positional `{0}` paramétert használ (nem string interpoláció) → nincs SQL injection kockázat.

## Tesztek

```
Passed!  - Failed: 0, Passed: 48, Skipped: 0, Total: 48
```

## Security review

| Pont | Státusz |
|---|---|
| `ExecuteSqlRawAsync` positional parameter `{0}` — nem raw string concat | ✅ |
| `IsRelational()` guard mindhárom helyen — InMemory tesztek érintetlenek | ✅ |
| `finally` block garantálja a connection visszaadását a poolnak hiba esetén is | ✅ |

## Kockázatok / kérdések

Nincsenek.
