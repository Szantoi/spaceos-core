---
id: MSG-PROCUREMENT-003-DONE
from: procurement
to: root
type: done
priority: high
status: READ
ref: MSG-PROCUREMENT-003
created: 2026-04-17
---

# DONE — PROCUREMENT-003: TenantSessionInterceptor skip fix

## Összefoglaló

Javítva a `TenantSessionInterceptor.cs` — belső hívásban JWT nélkül már nem írja felül a manuálisan beállított GUC értéket.

| Fájl | Változás |
|---|---|
| `src/.../Infrastructure/Persistence/TenantSessionInterceptor.cs` | `IsNullOrWhiteSpace` early return `ConnectionOpenedAsync`-ban |

Commit: `cbd900b`

**Változás:**
```csharp
var tenantId = ResolveTenantId();
if (string.IsNullOrWhiteSpace(tenantId))
{
    await base.ConnectionOpenedAsync(connection, eventData, ct).ConfigureAwait(false);
    return;  // ← belső hívás, nincs JWT → ne állítsd be a GUC-ot
}
await SetConfigAsync(connection, PgConfigKey, tenantId, ct).ConfigureAwait(false);
```

## Tesztek

```
Passed!  - Failed: 0, Passed: 48, Skipped: 0, Total: 48
```

## Security review

| Pont | Státusz |
|---|---|
| JWT-vel rendelkező hívások változatlanul működnek | ✅ `ResolveTenantId()` nem null → `SetConfigAsync` fut |
| Belső hívások nem írják felül az endpoint `set_config` értékét | ✅ early return |
| `ConnectionClosingAsync` (GUC clear) érintetlen marad | ✅ nem módosítva |

## Kockázatok / kérdések

Nincsenek.
