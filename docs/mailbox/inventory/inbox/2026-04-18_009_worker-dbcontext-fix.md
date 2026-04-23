---
id: MSG-INVENTORY-009
from: root
to: inventory
type: task
priority: high
status: READ
ref: MSG-INFRA-001-BLOCKED
created: 2026-04-18
---

# Worker DbContext fix — ADR-024 BYPASSRLS kapcsolat

## Root cause

`ReservationCleanupWorker` a reguláris `InventoryDbContext`-et használja.
A `TenantSessionInterceptor` üres string-et tölt `app.current_tenant_id`-be (nincs HttpContext a workerben) → az RLS `::uuid` cast `22P02` hibával száll el.

```
SqlState: 22P02 — invalid input syntax for type uuid: ""
Location: ReservationCleanupWorker.cs:89
```

## Fix

### 1. `InventoryWorkerDbContext` — dedikált worker context

Hozz létre egy `InventoryWorkerDbContext`-et ami:
- Az `INVENTORY_WORKER_CONNECTION_STRING` env var-ból veszi a connection string-et
- **NEM** regisztrál `TenantSessionInterceptor`-t
- Csak a worker által szükséges entitásokat tartalmazza: `Reservations`, `ReservationItems`

```csharp
// Infrastructure/Persistence/InventoryWorkerDbContext.cs
public sealed class InventoryWorkerDbContext : DbContext
{
    public InventoryWorkerDbContext(DbContextOptions<InventoryWorkerDbContext> options)
        : base(options) { }

    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<ReservationItem> ReservationItems => Set<ReservationItem>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.ApplyConfiguration(new ReservationConfiguration());
        mb.ApplyConfiguration(new ReservationItemConfiguration());
    }
}
```

### 2. DI regisztráció — `Program.cs`

```csharp
var workerConnStr = builder.Configuration["INVENTORY_WORKER_CONNECTION_STRING"]
    ?? throw new InvalidOperationException("INVENTORY_WORKER_CONNECTION_STRING not set");

builder.Services.AddDbContext<InventoryWorkerDbContext>(opts =>
    opts.UseNpgsql(workerConnStr));
    // ← Nincs AddInterceptors(tenantInterceptor) !
```

### 3. `ReservationCleanupWorker` — cseréld a context típusát

```csharp
// Régi:
var ctx = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
// Új:
var ctx = scope.ServiceProvider.GetRequiredService<InventoryWorkerDbContext>();
```

### 4. Health endpoint 404

Az INFRA `curl https://joinerytech.hu/bff/inventory/health` 404-et kapott.

Ellenőrizd `Program.cs`-ben hogy `app.MapHealthChecks("/health")` szerepel-e.
Ha igen, a path az nginx/BFF-en keresztül `/bff/inventory/health` → ellenőrizd az Orchestrator proxy config-ot is. Ha a proxy nem routeol `/health`-re, add hozzá a BFF-hez (vagy fogadd el hogy csak loopback-on érhető el: `curl http://localhost:5004/health`).

## DoD

- [ ] `InventoryWorkerDbContext` létrehozva, `TenantSessionInterceptor` nélkül
- [ ] Worker connection string DI-ba kötve
- [ ] `ReservationCleanupWorker` az `InventoryWorkerDbContext`-et használja
- [ ] Worker log: nincs `22P02` hiba 15 perc elteltével
- [ ] `dotnet build` → 0 error · `dotnet test` → ≥96 zöld
- [ ] Health endpoint: dokumentáld melyik path-on érhető el (loopback / BFF-en át)

---

*Skill: `/spaceos-terminal`*
