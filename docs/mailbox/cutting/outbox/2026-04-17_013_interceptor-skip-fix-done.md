---
id: MSG-CUTTING-013-DONE
from: cutting
to: root
type: done
status: READ
ref: MSG-CUTTING-013
created: 2026-04-17
---

## Összefoglaló

`TenantSessionInterceptor` skip fix implementálva: ha a JWT `tid` claim hiányzik (belső hívás, nincs Bearer token), az interceptor nem hívja meg a `SET app.current_tenant_id` GUC-ot, így a `CUTTING-012` által kézzel beállított session-szintű érték érvényes marad.

### Módosított fájlok

**`src/SpaceOS.Modules.Cutting.Infrastructure/Persistence/TenantSessionInterceptor.cs`**

```csharp
// ConnectionOpenedAsync — előtte:
var tenantId = ResolveTenantId();
await SetConfigAsync(connection, PgConfigKey, tenantId ?? string.Empty, ct);

// Utána:
var tenantId = ResolveTenantId();
if (tenantId is null)
{
    await base.ConnectionOpenedAsync(connection, eventData, ct).ConfigureAwait(false);
    return;
}
await SetConfigAsync(connection, PgConfigKey, tenantId, ct).ConfigureAwait(false);
```

`ResolveTenantId()` már korábban is `null`-t adott vissza érvénytelen/hiányzó claim esetén — a fix csak azt a pontot zárja le, ahol ez `string.Empty`-re esett volna vissza és felülírta volna a GUC-ot.

### Commit

`c3323ed` — spaceos-modules-cutting

## Tesztek

| Scope | Teszt | Eredmény |
|---|---|---|
| Contracts | 10 | ✅ |
| Domain + App + API + Adapter | 67 | ✅ |
| **Összesen** | **77** | **✅** |

## Security review

- GUC-t csak akkor állítja be az interceptor, ha érvényes JWT tenant claim létezik
- Belső hívások esetén a GUC az `InternalEndpoints.cs` által URL-ből beállított értéket tartja meg
- Fail-closed: ha nincs JWT és nincs manuális set_config, az RLS üres stringet lát → PostgreSQL visszautasítja (nem silent bypass)

## Kockázatok / megjegyzések

Nincs. A fix minimális és célzott.
