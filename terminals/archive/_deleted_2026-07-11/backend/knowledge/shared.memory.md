# Shared Domain Memory

> Minden task-hoz betöltődik - cross-domain patterns és általános szabályok.

## SpaceOS Golden Rules

1. **Data → Rules → Geometry** — Frontend rajzol, C# Driver számol, LLM csak paramétereket ad
2. **Modular Monolith** — Kernel `IParametricProduct` interfészen dolgozik
3. **Immutability & Trust** — Nincs UPDATE CAD adatokon, SHA-256 audit event
4. **Need-to-Know RBAC** — Megrendelő nem látja a gyártó anyaglistáját
5. **Walking Skeleton First** — E2E pipeline előbb, matematika utóbb

## Clean Architecture

```
┌─────────────────────────────────────────┐
│  API Layer (Controllers, Endpoints)     │
├─────────────────────────────────────────┤
│  Application Layer (Commands, Queries)  │
├─────────────────────────────────────────┤
│  Domain Layer (Entities, Value Objects) │
├─────────────────────────────────────────┤
│  Infrastructure (DB, External Services) │
└─────────────────────────────────────────┘

Dependency: Outer → Inner (never reverse)
```

## CQRS Pattern

```csharp
// Command (write)
public record CreateOrderCommand(Guid CustomerId, List<OrderItem> Items) : IRequest<Guid>;

// Query (read)
public record GetOrderQuery(Guid OrderId) : IRequest<OrderDto>;

// Handler
public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        var order = Order.Create(request.CustomerId, request.Items);
        await _repository.AddAsync(order, ct);
        return order.Id;
    }
}
```

## Testing Patterns

```csharp
// Unit test - Handler
[Fact]
public async Task Handle_ValidCommand_ReturnsOrderId()
{
    var handler = new CreateOrderCommandHandler(_mockRepo.Object);
    var result = await handler.Handle(new CreateOrderCommand(...), CancellationToken.None);
    result.Should().NotBeEmpty();
}

// Integration test - Endpoint
[Fact]
public async Task Post_ValidOrder_Returns201Created()
{
    var client = _factory.CreateClient();
    var response = await client.PostAsJsonAsync("/api/orders", new { ... });
    response.StatusCode.Should().Be(HttpStatusCode.Created);
}
```

## Database Patterns

```csharp
// EF Core configuration
modelBuilder.Entity<Order>(e =>
{
    e.ToTable("orders", "spaceos_kernel");
    e.HasKey(o => o.Id);
    e.Property(o => o.Id).ValueGeneratedNever();
    e.HasIndex(o => o.TenantId);
});

// RLS policy
CREATE POLICY tenant_isolation ON orders
    USING (tenant_id = current_setting('app.tenant_id')::uuid);
```

## Build & Test Commands

```bash
# Build
dotnet build

# Test all
dotnet test

# Test specific
dotnet test --filter "FullyQualifiedName~OrderTests"

# Watch mode
dotnet watch test
```

## Error Handling

```csharp
// Domain exception
public class OrderNotFoundException : DomainException
{
    public OrderNotFoundException(Guid id)
        : base($"Order {id} not found") { }
}

// Global handler
app.UseExceptionHandler(error => error.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var response = exception switch
    {
        DomainException => new ProblemDetails { Status = 400 },
        NotFoundException => new ProblemDetails { Status = 404 },
        _ => new ProblemDetails { Status = 500 }
    };
    await context.Response.WriteAsJsonAsync(response);
}));
```

## Conventions

- **File naming:** PascalCase for classes, camelCase for variables
- **Folder structure:** Feature-based (not layer-based)
- **API versioning:** `/api/v1/...`
- **Date format:** ISO 8601 (`2026-06-29T12:00:00Z`)
- **ID format:** GUID v7 (time-sortable)
