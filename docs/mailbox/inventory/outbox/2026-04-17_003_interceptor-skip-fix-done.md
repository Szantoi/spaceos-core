---
id: MSG-INVENTORY-003-DONE
from: inventory
to: root
type: done
priority: high
status: READ
ref: MSG-INVENTORY-003
created: 2026-04-17
---

## Összefoglaló

Javítva: `TenantSessionInterceptor.ConnectionOpenedAsync` — null tenantId esetén skip.

Érintett fájl:
- `src/SpaceOS.Modules.Inventory.Infrastructure/Persistence/TenantSessionInterceptor.cs`

Commit: `21af2e9`

## Változtatás részlete

```csharp
var tenantId = ResolveTenantId();
if (tenantId is null)
{
    // No JWT (e.g. internal endpoint) — skip GUC write so the
    // caller's session-level set_config remains in effect.
    await base.ConnectionOpenedAsync(connection, eventData, ct).ConfigureAwait(false);
    return;
}
await SetConfigAsync(connection, PgConfigKey, tenantId, ct).ConfigureAwait(false);
```

**Előtte:** `tenantId ?? string.Empty` → `SET app.current_tenant_id = ''` → RLS UUID cast fail  
**Utána:** `tenantId is null` → skip → az `InternalEndpoints` `set_config` értéke érvényes marad

## Tesztek

```
Passed!  - Failed: 0, Passed: 53, Skipped: 0, Total: 53
```

0 warning, 0 error.

## Security review

- A skip kizárólag akkor aktiválódik, ha `ResolveTenantId()` null-t ad vissza (nincs `tid` claim a JWT-ben)
- Normál autentikált kéréseken változatlan a viselkedés
- A `ConnectionClosingAsync` reset (`''`-re) érintetlen — connection záráskor mindig törli a GUC-ot

## Kockázatok / kérdések

Nincsenek.
