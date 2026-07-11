# Kernel Domain Memory

> Automatikusan betöltődik ha a feladat Kernel-hez kapcsolódik.

## Domain Scope

- **Modul:** `spaceos-kernel`
- **Felelősség:** Auth, RBAC, Audit, FSM, Multi-tenancy, Escrow
- **Tech stack:** .NET 8, PostgreSQL, Keycloak

## Aktív Patterns

### 1. Multi-Tenant RLS
```csharp
// Minden query tenant-aware
services.AddDbContext<KernelDbContext>(options =>
    options.UseNpgsql(connectionString)
           .AddInterceptors(new TenantInterceptor()));
```

### 2. FSM State Machine
```csharp
public interface IStateMachine<TState, TEvent>
{
    TState CurrentState { get; }
    Task<bool> TryTransitionAsync(TEvent @event);
}
```

### 3. Audit Event Sourcing
```csharp
// Minden változás immutable audit event
await _auditService.LogAsync(new AuditEvent
{
    EntityType = "Order",
    EntityId = orderId,
    Action = "StatusChanged",
    OldValue = oldStatus,
    NewValue = newStatus,
    UserId = currentUser.Id,
    TenantId = tenant.Id,
    Timestamp = DateTime.UtcNow,
    Hash = ComputeSha256(...)
});
```

## API Endpoints

| Endpoint | Method | Auth |
|----------|--------|------|
| `/api/auth/login` | POST | Anonymous |
| `/api/auth/refresh` | POST | JWT |
| `/api/users` | GET/POST | Admin |
| `/api/tenants` | GET/POST | SuperAdmin |
| `/api/audit/events` | GET | Admin |

## Legutóbbi Tanulságok

- **MapInboundClaims = false** kell Keycloak integrációhoz
- **RLS policy-k** minden táblán kötelezők
- **Escrow** használata fizetéseknél (nem direct payment)

## Kapcsolódó Fájlok

- `src/SpaceOS.Kernel/` - Core domain
- `src/SpaceOS.Kernel.Infrastructure/` - Persistence
- `src/SpaceOS.Kernel.Api/` - REST endpoints
- `tests/SpaceOS.Kernel.Tests/` - Unit + Integration tests
