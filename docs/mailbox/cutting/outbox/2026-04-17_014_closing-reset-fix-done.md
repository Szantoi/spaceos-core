---
id: MSG-CUTTING-014-DONE
from: cutting
to: root
type: done
status: READ
ref: MSG-CUTTING-014
created: 2026-04-17
---

## Összefoglaló

`TenantSessionInterceptor.ConnectionClosingAsync` null-check fix implementálva: ha nincs JWT tenant claim, az interceptor nem reseteli a `app.current_tenant_id` GUC-ot ürес stringre connection záráskor.

### Módosított fájlok

**`src/SpaceOS.Modules.Cutting.Infrastructure/Persistence/TenantSessionInterceptor.cs`**

```csharp
// ConnectionClosingAsync — előtte:
await SetConfigAsync(connection, PgConfigKey, string.Empty, CancellationToken.None);
return await base.ConnectionClosingAsync(...);

// Utána:
var tenantId = ResolveTenantId();
if (tenantId is null)
    return await base.ConnectionClosingAsync(...);
await SetConfigAsync(connection, PgConfigKey, string.Empty, CancellationToken.None);
return await base.ConnectionClosingAsync(...);
```

A fix szimmetrikus a `ConnectionOpenedAsync` CUTTING-013 javításával: mind a két lifecycle hook kihagyja a GUC műveletet, ha nincs érvényes JWT tenant claim.

### Commit

`248c035` — spaceos-modules-cutting

## Tesztek

| Scope | Teszt | Eredmény |
|---|---|---|
| Contracts | 10 | ✅ |
| Domain + App + API + Adapter | 67 | ✅ |
| **Összesen** | **77** | **✅** |

## Security review

- JWT-s hívásokban a reset továbbra is megtörténik (connection záráskor GUC ürítve) — nincs regresszió
- Belső hívásokban a GUC az `InternalEndpoints.cs` által beállított session értéket tartja meg a teljes request lifetime alatt
- `ResolveTenantId()` null-t ad vissza ha nincs HttpContext, nincs `tid` claim, vagy érvénytelen GUID → mindhárom esetben a skip ágra kerül

## Kockázatok / megjegyzések

Nincs. A CUTTING-012 + CUTTING-013 + CUTTING-014 együtt alkotja a teljes belső hívás GUC fix láncolatát.
