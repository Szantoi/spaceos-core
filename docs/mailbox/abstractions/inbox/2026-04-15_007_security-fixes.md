---
id: MSG-ABSTRACTIONS-007
from: root
to: abstractions
type: task
priority: high
status: READ
ref: MSG-ABSTRACTIONS-006-DONE
created: 2026-04-15
---

# MSG-ABSTRACTIONS-007 — Security fixes: M01 + M02 + M03

## Feladat

A security review 3 közepes találatot azonosított. Javítsd mind a hármat.

---

### M01 — `ValidateAudience = true`

```csharp
// Program.cs — JWT konfig
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateAudience = true,
    ValidAudience = builder.Configuration["Jwt:Audience"]
        ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE")
        ?? "kernel-api",
    // ... többi meglévő param
};
```

`appsettings.json`-ba (ha még nincs): `"Jwt": { "Audience": "kernel-api" }`

---

### M02 — TenantSessionInterceptor read-path kiterjesztése

A `TenantSessionInterceptor` jelenleg csak `SaveChangesInterceptor` — read query-k
előtt nem fut a `SET app.tenant_id`. Javítás: `DbCommandInterceptor` hozzáadása.

```csharp
// Infrastructure/Data/TenantCommandInterceptor.cs (új fájl)
public class TenantCommandInterceptor : DbCommandInterceptor
{
    private readonly ICurrentTenantProvider _tenantProvider;
    public TenantCommandInterceptor(ICurrentTenantProvider tenantProvider)
        => _tenantProvider = tenantProvider;

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        SetTenantSession(command);
        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken ct = default)
    {
        SetTenantSession(command);
        return ValueTask.FromResult(result);
    }

    private void SetTenantSession(DbCommand command)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        if (tenantId != Guid.Empty)
        {
            using var setPrepend = command.Connection!.CreateCommand();
            setPrepend.CommandText =
                $"SET LOCAL app.tenant_id = '{tenantId:D}'";
            setPrepend.ExecuteNonQuery();
        }
    }
}
```

Regisztráció a `DbContext`-ben: `optionsBuilder.AddInterceptors(new TenantCommandInterceptor(...))`.

**⚠️ Ha az implementáció bonyolult lenne** (circular dependency, ICurrentTenantProvider nem injektálható itt), elfogadható alternatíva: M03 fix (TenantId query szűrő) mint elsődleges védelmi réteg + M02 Q3-ra halasztva. Jelezd BLOCKED-ban ha ez a helyzet.

---

### M03 — Repository TenantId szűrő olvasáskor

```csharp
// AbstractionsRepository.cs
public async Task<ProductTemplate?> GetTemplateAsync(
    Guid id, Guid tenantId, CancellationToken ct = default) =>
    await _db.ProductTemplates.AsNoTracking()
             .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId, ct)
             .ConfigureAwait(false);

public async Task<ProductTemplate?> GetTemplateWithAllAsync(
    Guid id, Guid tenantId, CancellationToken ct = default) =>
    await _db.ProductTemplates.AsNoTracking()
             .Include(t => t.Slots)
             .Include(t => t.Connections)
             .Include(t => t.Parameters)
             .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId, ct)
             .ConfigureAwait(false);
```

Frissítsd a hívó handler-eket is (add át a `tenantId` paramétert).

---

## DoD

- [ ] M01: `ValidateAudience = true` — csak `kernel-api` audience elfogadott
- [ ] M02: read-path tenant session (DbCommandInterceptor VAGY BLOCKED ha nem megvalósítható)
- [ ] M03: `GetTemplateAsync` / `GetTemplateWithAllAsync` TenantId szűrővel
- [ ] `dotnet build` → 0 error, 0 warning
- [ ] `dotnet test` → 81+ teszt zöld
- [ ] Commit + push develop
