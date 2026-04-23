---
id: MSG-CUTTING-008
from: root
to: cutting
type: task
priority: high
status: READ
ref: MSG-E2E-044-DONE
created: 2026-04-16
---

# MSG-CUTTING-008 — TenantSessionInterceptor implementáció (inventory + cutting + procurement)

## Gyökérok

```
SqlState: 22P02
MessageText: invalid input syntax for type uuid: ""
```

A cutting/inventory/procurement modulokban hiányzik a `TenantSessionInterceptor`. A
PostgreSQL RLS policy `current_setting('app.current_tenant_id')::uuid`-t hív, de az értéke
mindig üres string (az INFRA-113 `ALTER DATABASE ... SET "app.current_tenant_id" = ''`
csak registrálta, nem töltötte fel értékkel).

## Referencia — Kernel implementáció

A Kernel `TenantSessionInterceptor` (`DbConnectionInterceptor`) a megoldás mintája:
- Fájl: `/opt/spaceos/spaceos-kernel/SpaceOS.Infrastructure/Persistence/TenantSessionInterceptor.cs`
- `ConnectionOpenedAsync`: `tid` claim → `set_config('app.current_tenant_id', tenantId, false)`
- `ConnectionClosingAsync`: reset üres string-re (connection pool cleanup)
- Regisztráció: `Singleton`, csak PostgreSQL esetén

## Feladat — 3 modul (inventory + cutting + procurement)

### 1. TenantSessionInterceptor létrehozása (mindhárom modulban azonos minta)

```csharp
// SpaceOS.Modules.Cutting.Infrastructure/Persistence/TenantSessionInterceptor.cs
// (és analóg: Inventory.Infrastructure, Procurement.Infrastructure)

using System.Data.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;

internal sealed class TenantSessionInterceptor : DbConnectionInterceptor
{
    private const string PgConfigKey = "app.current_tenant_id";
    private readonly IHttpContextAccessor _http;

    public TenantSessionInterceptor(IHttpContextAccessor http)
        => _http = http;

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken ct)
    {
        var tenantId = ResolveTenantId();
        await SetConfigAsync(connection, PgConfigKey, tenantId ?? string.Empty, ct)
            .ConfigureAwait(false);
        await base.ConnectionOpenedAsync(connection, eventData, ct).ConfigureAwait(false);
    }

    public override async ValueTask<InterceptionResult> ConnectionClosingAsync(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result)
    {
        await SetConfigAsync(connection, PgConfigKey, string.Empty, CancellationToken.None)
            .ConfigureAwait(false);
        return await base.ConnectionClosingAsync(connection, eventData, result).ConfigureAwait(false);
    }

    private string? ResolveTenantId()
    {
        var ctx = _http.HttpContext;
        if (ctx is null) return null;
        var claim = ctx.User.FindFirst("tid")?.Value;
        return Guid.TryParse(claim, out var g) && g != Guid.Empty ? g.ToString() : null;
    }

    private static async Task SetConfigAsync(DbConnection conn, string key, string value, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT set_config(@key, @value, false)";
        var pk = cmd.CreateParameter(); pk.ParameterName = "@key";   pk.Value = key;   cmd.Parameters.Add(pk);
        var pv = cmd.CreateParameter(); pv.ParameterName = "@value"; pv.Value = value; cmd.Parameters.Add(pv);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }
}
```

### 2. DI regisztráció — Program.cs (mindhárom service-ben)

```csharp
// A meglévő AddDbContext hívás ELŐTT:
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<TenantSessionInterceptor>();

// AddDbContext bővítése az interceptorral:
builder.Services.AddDbContext<CuttingDbContext>((sp, options) =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Cutting"));
    options.AddInterceptors(sp.GetRequiredService<TenantSessionInterceptor>());
});
```

Analóg: `InventoryDbContext` → `ConnectionStrings:Inventory`, `ProcurementDbContext` → `ConnectionStrings:Procurement`.

### 3. Tesztek

Az egységtesztekben (`TestAuthHandler.cs`) az interceptor nem fut (SQLite / in-memory),
ezért **nem szükséges** a teszteket módosítani. Az integráció verifikáció az E2E suite-on történik.

## DoD

- [ ] `TenantSessionInterceptor` létrehozva mindhárom Infrastructure projektben
- [ ] DI: `AddHttpContextAccessor` + interceptor regisztrálva mindhárom Program.cs-ben
- [ ] `dotnet build` → 0 error, 0 warning
- [ ] `dotnet test` → 153/153 (vagy több) zöld
- [ ] Security review: paraméteres `set_config` (SQL injection-safe), reset connection close-kor
- [ ] Commit mindhárom repóban
- [ ] Outbox: `MSG-CUTTING-008-DONE`
