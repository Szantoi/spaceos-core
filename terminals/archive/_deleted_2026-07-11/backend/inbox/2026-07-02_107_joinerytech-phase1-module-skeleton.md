---
id: MSG-BACKEND-107
from: conductor
to: backend
type: task
priority: high
status: INJECTED
injected: 2026-07-03
model: haiku
epic_id: EPIC-JT-CRM
ref: MSG-BACKEND-105
created: 2026-07-02
content_hash: 4903582f13038b0ddb6f8bc2facea2dc4ace489f8fd5a158542ef1408a35e8c6
---

# JoineryTech Phase 1 Module Skeleton — Foundation Setup

## Context

Backend Architecture Plan APPROVED (MSG-BACKEND-105-DONE). OpenAPI spec kész és reviewed.

**Most következik:** .NET 8 projekt struktúra + Catalog modul skeleton (első modul).

## Task

Hozd létre a JoineryTech backend **projekt struktúrát** és a **Catalog modul skeleton**-ját.

### 1. Project Structure Setup

```
/opt/spaceos/joinerytech-backend/
├── src/
│   ├── JoineryTech.Kernel/              # Core infrastruktúra
│   │   ├── Domain/                      # Base classes
│   │   │   ├── AggregateRoot.cs
│   │   │   ├── Entity.cs
│   │   │   ├── ValueObject.cs
│   │   │   └── DomainEvent.cs
│   │   ├── Application/
│   │   │   └── IUnitOfWork.cs
│   │   └── JoineryTech.Kernel.csproj
│   │
│   ├── JoineryTech.Catalog/             # Catalog modul (első)
│   │   ├── Domain/
│   │   │   ├── CatalogItem.cs           # Aggregate Root
│   │   │   ├── Category.cs              # Entity
│   │   │   ├── ItemStatus.cs            # Enum
│   │   │   └── Money.cs                 # Value Object
│   │   ├── Application/
│   │   │   ├── Queries/
│   │   │   │   ├── GetCatalogItemsQuery.cs
│   │   │   │   └── GetCatalogItemsHandler.cs
│   │   │   └── ICatalogRepository.cs
│   │   ├── Infrastructure/
│   │   │   ├── CatalogRepository.cs
│   │   │   └── CatalogDbContext.cs
│   │   ├── Api/
│   │   │   └── CatalogController.cs     # REST endpoints
│   │   └── JoineryTech.Catalog.csproj
│   │
│   └── JoineryTech.Api/                 # API Gateway
│       ├── Program.cs
│       ├── appsettings.json
│       └── JoineryTech.Api.csproj
│
├── tests/
│   └── JoineryTech.Catalog.Tests/
│       └── Domain/
│           └── CatalogItemTests.cs
│
├── JoineryTech.sln
└── README.md
```

### 2. Kernel Setup (Base Classes)

**AggregateRoot.cs:**
```csharp
public abstract class AggregateRoot : Entity
{
    private readonly List<DomainEvent> _domainEvents = new();
    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
```

**Entity.cs:**
```csharp
public abstract class Entity
{
    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime? UpdatedAt { get; protected set; }
}
```

### 3. Catalog Module Skeleton

**CatalogItem.cs (Aggregate Root):**
```csharp
public class CatalogItem : AggregateRoot
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public ItemStatus Status { get; private set; }
    public Money Price { get; private set; }
    public Guid? CategoryId { get; private set; }

    public void Activate()
    {
        if (Status != ItemStatus.Draft)
            throw new InvalidOperationException("Only draft items can be activated");

        Status = ItemStatus.Active;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new CatalogItemActivatedEvent(Id));
    }
}
```

**Money.cs (Value Object):**
```csharp
public record Money(decimal Amount, string Currency)
{
    public static Money FromHUF(decimal amount) => new(amount, "HUF");
    public static Money FromEUR(decimal amount) => new(amount, "EUR");
}
```

### 4. Repository Interface

**ICatalogRepository.cs:**
```csharp
public interface ICatalogRepository
{
    Task<CatalogItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<CatalogItem>> GetActiveItemsAsync(
        int page,
        int pageSize,
        CancellationToken ct = default);
    Task AddAsync(CatalogItem item, CancellationToken ct = default);
    Task UpdateAsync(CatalogItem item, CancellationToken ct = default);
}
```

### 5. CQRS Query Handler

**GetCatalogItemsHandler.cs:**
```csharp
public class GetCatalogItemsHandler : IQueryHandler<GetCatalogItemsQuery, CatalogItemListDto>
{
    private readonly ICatalogRepository _repository;

    public async Task<CatalogItemListDto> HandleAsync(
        GetCatalogItemsQuery query,
        CancellationToken ct)
    {
        var items = await _repository.GetActiveItemsAsync(
            query.Page,
            query.PageSize,
            ct);

        return new CatalogItemListDto
        {
            Data = items.Select(MapToDto).ToList(),
            Total = items.Count(),
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}
```

### 6. API Controller Stub

**CatalogController.cs:**
```csharp
[ApiController]
[Route("api/catalog")]
public class CatalogController : ControllerBase
{
    [HttpGet("items")]
    public async Task<ActionResult<CatalogItemListDto>> GetItems(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        // TODO: Wire up handler
        return Ok(new CatalogItemListDto());
    }

    [HttpGet("items/{id}")]
    public async Task<ActionResult<CatalogItemDto>> GetItemById(Guid id)
    {
        // TODO: Wire up handler
        return Ok(new CatalogItemDto());
    }
}
```

## Deliverables

- [ ] .NET 8 solution létrehozva (`JoineryTech.sln`)
- [ ] Kernel projekt (base classes: AggregateRoot, Entity, ValueObject)
- [ ] Catalog modul skeleton (Domain/Application/Infrastructure/Api)
- [ ] 1 Aggregate Root: CatalogItem (FSM: Draft→Active→Archived)
- [ ] 1 Value Object: Money
- [ ] Repository interface
- [ ] 1 CQRS Query handler (GetCatalogItems)
- [ ] REST Controller stub (3 endpoint)
- [ ] Unit test project setup (CatalogItemTests)
- [ ] README.md (project structure dokumentáció)
- [ ] Build sikeres (`dotnet build`)

## Technical Stack

- .NET 8 SDK
- ASP.NET Core Minimal API
- Entity Framework Core 8 (később)
- PostgreSQL (később)
- xUnit (tests)

## Reference

- Backend Plan: `/opt/spaceos/docs/joinerytech/BACKEND_ARCHITECTURE_PLAN.md`
- OpenAPI: `/opt/spaceos/docs/api/joinerytech-phase1-openapi.yaml`
- SpaceOS Kernel: `/opt/spaceos/spaceos-kernel/` (DDD pattern referencia)

## Acceptance Criteria

- [ ] Project struktúra létrehozva és buildelhető
- [ ] Kernel base classes implementálva
- [ ] Catalog modul skeleton kész (4 layer)
- [ ] 1 working endpoint stub (GET /api/catalog/items)
- [ ] Unit test projekt setup
- [ ] Git commit + DONE outbox

## Timeline

**Becsült idő:** 2-3 óra (skeleton setup, no DB yet)

## Notes

Ez a **foundation setup** - még nincs DB connection, csak a struktúra. A DB setup Phase 1-B lesz (következő task).

**Haiku model** - template-based scaffold, straightforward implementation.
