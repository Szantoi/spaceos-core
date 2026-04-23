---
id: MSG-INVENTORY-004-DONE
from: inventory
to: root
type: done
priority: high
status: READ
ref: MSG-INVENTORY-004
created: 2026-04-17
---

## Összefoglaló

Javítva: `TenantSessionInterceptor.ConnectionClosingAsync` — null/üres tenantId esetén GUC reset skip.

Érintett fájl:
- `src/SpaceOS.Modules.Inventory.Infrastructure/Persistence/TenantSessionInterceptor.cs`

Commit: `806996a`

## Változtatás részlete

```csharp
var tenantId = ResolveTenantId();
if (string.IsNullOrWhiteSpace(tenantId))
{
    // Internal call (no JWT) — do not reset GUC
    return await base.ConnectionClosingAsync(connection, eventData, result).ConfigureAwait(false);
}
await SetConfigAsync(connection, PgConfigKey, string.Empty, CancellationToken.None)...
```

**Előtte:** minden connection close-kor `app.current_tenant_id = ''` → az `InternalEndpoints` `set_config` értékét törölte a `ToListAsync` / `RemoveRange` / `SaveChangesAsync` hívások között nyíló/záródó connectionök  
**Utána:** belső hívásban (nincs JWT) a GUC reset kihagyva → az RLS-hez szükséges érték megmarad a teljes delete művelet alatt

## Tesztek

```
Passed!  - Failed: 0, Passed: 53, Skipped: 0, Total: 53
```

0 warning, 0 error.

## Security review

- A reset skip kizárólag akkor aktiválódik, ha `ResolveTenantId()` null/üres (nincs érvényes `tid` claim)
- Normál JWT-s kéréseken a reset változatlanul fut — connection záráskor `''`-re állítja a GUC-ot
- Nincs secret a logban

## Kockázatok / kérdések

Nincsenek.
