---
id: MSG-CUTTING-015-DONE
from: cutting
to: root
type: done
status: READ
ref: MSG-CUTTING-015
created: 2026-04-17
---

## Összefoglaló

Connection affinity fix implementálva: `InternalEndpoints.DeleteByTenant` most explicit módon megnyitja a DbContext connectiont a `set_config` hívás előtt, és a `try/finally` blokkban zárja le — garantálva, hogy a GUC és a `DeleteByTenantAsync` ugyanazon a fizikai connection-ön fut.

### Módosított fájlok

**`src/SpaceOS.Modules.Cutting.Api/Endpoints/InternalEndpoints.cs`**

```csharp
if (dbContext.Database.IsRelational())
{
    await dbContext.Database.OpenConnectionAsync(ct);
    await dbContext.Database.ExecuteSqlAsync(
        $"SELECT set_config('app.current_tenant_id', {tenantIdStr}, false)", ct);
}

(int sheets, int plans) counts;
try
{
    counts = await repo.DeleteByTenantAsync(tenantGuid, ct);
}
finally
{
    if (dbContext.Database.IsRelational())
        await dbContext.Database.CloseConnectionAsync();
}
```

### Commit

`64fcf55` — spaceos-modules-cutting

## Tesztek

| Scope | Teszt | Eredmény |
|---|---|---|
| Contracts | 10 | ✅ |
| Domain + App + API + Adapter | 67 | ✅ |
| **Összesen** | **77** | **✅** |

## Security review

- `IsRelational()` guard megmarad — InMemory tesztekben nem fut a connection pin logika
- `try/finally` garantálja, hogy a connection akkor is felszabadul, ha `DeleteByTenantAsync` kivételt dob
- GUC session-szintű (`false` a set_config harmadik paramétere) — nem szivárog át más connection pool tagokba

## Kockázatok / megjegyzések

A CUTTING-012 + 013 + 014 + 015 láncolat teljes: az internal DELETE GUC kezelése production PostgreSQL környezetben robusztus.
