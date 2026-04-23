---
id: MSG-ABSTRACTIONS-007-DONE
from: abstractions
to: root
type: response
priority: high
status: READ
ref: MSG-ABSTRACTIONS-007
created: 2026-04-15
---

# MSG-ABSTRACTIONS-007 — Security fixes M01 + M02 + M03 — DONE

## Implementált változások

### M01 — ValidateAudience = true ✅

**`SpaceOS.Modules.Abstractions.Api/Program.cs`**
```csharp
ValidateAudience = true,
ValidAudience = builder.Configuration["Jwt:Audience"]
    ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE")
    ?? "kernel-api",
```

**`appsettings.json`** — `"Jwt": { "Audience": "kernel-api" }` szekció hozzáadva.

Csak `kernel-api` audience-re kiadott JWT token fogadható el.

---

### M02 — TenantCommandInterceptor (read-path RLS session) ✅

**Új fájl:** `SpaceOS.Modules.Abstractions.Infrastructure/Persistence/TenantCommandInterceptor.cs`

`DbCommandInterceptor` leszármazott, `ReaderExecuting` + `ReaderExecutingAsync` hook-ban minden read query előtt `SET app.tenant_id = '{tenantId}'` fut a connection-ön (ha a TenantId elérhető a JWT-ből).

**`InfrastructureServiceExtensions.cs`** frissítve:
- `services.AddScoped<TenantCommandInterceptor>()` regisztrálva
- `AddDbContext` factory mintára alakítva: `(sp, opts) => opts.AddInterceptors(sp.GetRequiredService<TenantCommandInterceptor>())`

RLS session-változó most mind írás, mind olvasás előtt be van állítva.

---

### M03 — Repository TenantId szűrő olvasáskor ✅

**`IAbstractionsRepository`** — interface signature frissítve:
```csharp
Task<ProductTemplate?> GetTemplateAsync(Guid id, Guid tenantId, CancellationToken ct = default);
Task<ProductTemplate?> GetTemplateWithAllAsync(Guid id, Guid tenantId, CancellationToken ct = default);
```

**`AbstractionsRepository`** — DB query-k most TenantId-ra is szűrnek:
```csharp
.FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId, ct)
```

**9 handler frissítve** (mindenhol `request.TenantId` / `request.JwtTenantId` átadva):
- `GetProductTemplateQuery`, `GetTemplateGraphQuery`
- `AddComponentSlotCommand`, `AddSlotConnectionCommand`, `SetTemplateParameterCommand`, `CloneProductTemplateCommand`
- `CalculateProductCommand`, `GetCuttingListQuery`, `GetCncPlanQuery`, `GetProcessPlanQuery`

A handler-szintű `template.TenantId != request.TenantId` ellenőrzések megmaradtak defense-in-depth-ként.

---

## Build / Test

```
dotnet build → 0 error, 0 warning
dotnet test  → 81/81 passed
```

## Security review

| Fix | Státusz |
|---|---|
| M01: ValidateAudience = true + ValidAudience = "kernel-api" | ✅ |
| M02: DbCommandInterceptor read-path tenant session | ✅ |
| M03: Repository TenantId szűrő DB query szinten | ✅ |

## Kockázatok / kérdések

Nincs. Minden DoD pont teljesült.
