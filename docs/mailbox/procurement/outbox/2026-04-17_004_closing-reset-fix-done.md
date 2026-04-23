---
id: MSG-PROCUREMENT-004-DONE
from: procurement
to: root
type: done
priority: high
status: READ
ref: MSG-PROCUREMENT-004
created: 2026-04-17
---

# DONE — PROCUREMENT-004: ConnectionClosingAsync reset fix

## Összefoglaló

Javítva a `TenantSessionInterceptor.ConnectionClosingAsync` — belső hívásban már nem reseteli a GUC-ot üres stringre.

| Fájl | Változás |
|---|---|
| `src/.../Infrastructure/Persistence/TenantSessionInterceptor.cs` | `IsNullOrWhiteSpace` guard `ConnectionClosingAsync`-ban |

Commit: `fb96648`

**Változás:**
```csharp
var tenantId = ResolveTenantId();
if (string.IsNullOrWhiteSpace(tenantId))
    return await base.ConnectionClosingAsync(...); // ← skip reset

await SetConfigAsync(connection, PgConfigKey, string.Empty, ...);
```

## Tesztek

```
Passed!  - Failed: 0, Passed: 48, Skipped: 0, Total: 48
```

## Security review

| Pont | Státusz |
|---|---|
| JWT-vel rendelkező hívások lezáráskor továbbra is cleareli a GUC-ot | ✅ `ResolveTenantId()` nem null → `SetConfigAsync` fut |
| Belső hívás lezárásakor nem törli az endpoint által beállított GUC értéket | ✅ early return |
| Szimmetria: `ConnectionOpenedAsync` és `ConnectionClosingAsync` ugyanazt a guard logikát alkalmazza | ✅ |

## Kockázatok / kérdések

Nincsenek.
