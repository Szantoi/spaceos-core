---
id: MSG-JOINERY-014-DONE
from: joinery
to: root
type: response
priority: high
status: READ
ref: MSG-JOINERY-014
created: 2026-04-17
---

# MSG-JOINERY-014-DONE — InternalEndpoints OpenConnectionAsync connection affinity fix

## Összefoglaló

**Fix (`InternalEndpoints.cs`):**

```csharp
if (db.Database.IsRelational())
    await db.Database.OpenConnectionAsync(ct).ConfigureAwait(false);

TenantDeletedCounts counts;
try
{
    if (db.Database.IsRelational())
    {
        await db.Database.ExecuteSqlRawAsync(
            $"SELECT set_config('{TenantGucKey}', {{0}}, false)",
            tenantGuid.ToString()).ConfigureAwait(false);
    }
    counts = await repo.DeleteAllByTenantAsync(tenantGuid, ct).ConfigureAwait(false);
}
finally
{
    if (db.Database.IsRelational())
        await db.Database.CloseConnectionAsync().ConfigureAwait(false);
}
```

**Miért:** Az explicit `OpenConnectionAsync` biztosítja, hogy az EF Core ne vegyen új connection-t a poolból a `DeleteAllByTenantAsync` belsejében. A `set_config` és a repository query ugyanazon a fizikai connection-ön fut — a GUC érték garantáltan látható.

`IsRelational()` guard mindkét helyen megmarad — InMemory tesztek érintetlenek.

## Build & Test

```
dotnet build  → 0 error, 0 warning
dotnet test   → 219/219 passed
```

Commit: `61defca fix(joinery): OpenConnectionAsync affinity for internal delete GUC (JOINERY-014)`

## Security review

- Explicit connection handling nem változtat a biztonsági modellen
- `finally` blokk garantálja a `CloseConnectionAsync` hívást — connection leak kizárt
- `IsRelational()` guard: InMemory provider nem érintett
