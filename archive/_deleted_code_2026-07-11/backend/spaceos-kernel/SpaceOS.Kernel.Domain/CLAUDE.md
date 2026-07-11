# CLAUDE.md — SpaceOS.Kernel.Domain

> Domain layer rules. Read root CLAUDE.md first for global rules.
> This layer has ZERO external NuGet dependencies — enforce strictly.

---

## PURPOSE

The Domain is the single source of truth for business logic.
It knows nothing about EF Core, MediatR, HTTP, or any infrastructure concern.

---

## AGGREGATE RULES

```csharp
// SpaceOS.Kernel.Domain/Tenants/Tenant.cs

// ✅ No public setters — mutation through explicit methods only
public TenantName Name { get; private set; }

// ✅ Static factory — never expose a public constructor
public static Tenant Create(TenantName name)
{
    var tenant = new Tenant(TenantId.New(), name);
    tenant.AddDomainEvent(new TenantCreatedEvent(tenant.Id));
    return tenant;
}

// ✅ Every mutation raises a domain event
public void Rename(TenantName newName)
{
    Name = newName;
    AddDomainEvent(new TenantRenamedEvent(Id, newName));
}

// ❌ Never — business logic belongs here, not in the handler
public string Name { get; set; }
```

**Factory naming:** `Create()` for new entities · `From()` for reconstruction · `New()` for IDs

---

## VALUE OBJECT RULES

```csharp
// SpaceOS.Kernel.Domain/Tenants/TenantName.cs

// ✅ readonly record struct — immutable by design
public readonly record struct TenantName
{
    public string Value { get; }

    // ✅ Invariant guard in constructor — throw DomainException, never return null
    private TenantName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Tenant name cannot be empty.");
        if (value.Length > 100)
            throw new DomainException("Tenant name cannot exceed 100 characters.");
        Value = value;
    }

    public static TenantName From(string value) => new(value);
}

// ❌ Never bypass invariants with `with` expression
var invalid = validName with { Value = "" };
```

**ID Value Objects:**
```csharp
public readonly record struct TenantId
{
    public Guid Value { get; }
    private TenantId(Guid value) => Value = value;
    public static TenantId New() => new(Guid.NewGuid());
    public static TenantId From(Guid value) => new(value);
}
```

---

## DOMAIN EVENT RULES

```csharp
// SpaceOS.Kernel.Domain/Tenants/Events/TenantCreatedEvent.cs

// ✅ readonly record struct + IDomainEvent marker interface
public readonly record struct TenantCreatedEvent(TenantId TenantId) : IDomainEvent;
```

- Every aggregate mutation → one domain event minimum
- Events are raised inside the aggregate, never from outside
- Events carry only the data needed — no full aggregate reference

---

## REPOSITORY INTERFACE RULES

```csharp
// SpaceOS.Kernel.Domain/Tenants/ITenantRepository.cs

// ✅ Interface lives in Domain — implementation lives in Infrastructure
public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(TenantId id, CancellationToken ct);
    Task AddAsync(Tenant tenant, CancellationToken ct);
    Task UpdateAsync(Tenant tenant, CancellationToken ct);
    Task<IReadOnlyList<Tenant>> ListAsync(ISpecification<Tenant> spec, CancellationToken ct);
    Task<bool> ExistsByNameAsync(TenantName name, CancellationToken ct); // where relevant
}

// ❌ Never — no ListAllAsync without specification
Task<IReadOnlyList<Tenant>> ListAllAsync(CancellationToken ct);
```

---

## SPECIFICATION RULES

```csharp
// SpaceOS.Kernel.Domain/Tenants/Specs/AllTenantsSpec.cs

// ✅ Ardalis.Specification — every list query goes through a spec
public sealed class AllTenantsSpec : Specification<Tenant> { }

public sealed class FacilitiesByTenantIdSpec : Specification<Facility>
{
    public FacilitiesByTenantIdSpec(TenantId tenantId) =>
        Query.Where(f => f.TenantId == tenantId);
}
```

---

## EXISTING AGGREGATES

### Core Aggregates

| Aggregate | Key Owned Types |
|---|---|
| `Tenant` | `TenantId`, `TenantName` |
| `Facility` | `FacilityId`, `FacilityName`, `TenantId` |
| `WorkStation` | `WorkStationId`, `WorkStationName`, `WorkStationType`, status FSM |
| `SpaceLayer` | `SpaceLayerId`, `TradeType`, JSON intent, federation fields |
| `FlowEpic` | `FlowEpicId`, `FlowEpicTitle`, `B2BHandshake` VO, FSM state |

**`B2BHandshake` VO:** `GuestTenantId` + delegation timestamp — used on `FlowEpic` for cross-tenant delegation.

### Sprint C Supporting Aggregates

| Aggregate | Location | Purpose |
|---|---|---|
| `AuditEvent` | `AuditLog/AuditEvent.cs` | SHA-256 hash chain, compliance trail, SourceBrand tracking |
| `NodeManifest` | `Federation/NodeManifest.cs` | Federation node registration and metadata |
| `SyncSignal` | `Sync/SyncSignal.cs` | Offline state synchronization (eventual consistency) |
| `AggregateSnapshot` | `Snapshots/AggregateSnapshot.cs` | Point-in-time aggregate state snapshots |
| `OutboxMessage` | `Outbox/OutboxMessage.cs` | Transactional outbox for reliable event delivery |
| `UserProfile` | `Users/UserProfile.cs` | GDPR pseudonymization support |

---

## ANTI-PATTERNS

```csharp
// ❌ Public setter
public string Name { get; set; }

// ❌ Business rule in handler instead of aggregate
if (command.Name.Length > 100) return Result.Error(...); // belongs in TenantName VO

// ❌ Domain referencing infrastructure
using Microsoft.EntityFrameworkCore; // never in Domain

// ❌ Mutable domain event
public class TenantCreatedEvent : IDomainEvent { public Guid Id { get; set; } }
```

---

## BACKEND IMPLEMENTÁCIÓS CHECKLIST

Minden feature/bugfix végén, DONE outbox előtt:

- [ ] Entity creation factory method-dal (nem publikus constructor)
- [ ] Setter-ek private-ok
- [ ] Domain validation implementálva (nem controller-ben)
- [ ] Controller/endpoint csak DTO-t ad vissza (entity soha)
- [ ] Unit test üzleti logikára
- [ ] `dotnet build` 0 error
- [ ] `dotnet test` minden zöld

### QA Handoff kritérium

A TESTER terminált ROOT hívja be ha a feladat:
- Üzleti validációs logikát tartalmaz (pl. rendelés állapotgép, ár kalkuláció)
- Pénzügyi számítást végez
- Workflow / FSM state machine-t érint
- A task explicit jelzi: "QA needed: Yes"

Egyszerű CRUD endpoint-ok NEM igényelnek QA-t, kivéve ha explicit kérve van.
