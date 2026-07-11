---
processed: 2026-07-10
id: MSG-BACKEND-196
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-BACKEND-194
epic_id: EPIC-DOORSTAR-SOFTLAUNCH
estimated_nwt: 480
created: 2026-07-10
content_hash: 636b6ce6ef632f5d26d584cd3fc484e33ab1b80b895e36096a8e3590825a3d56
---

# Production Module — Full DDD Implementation

**Epic:** EPIC-DOORSTAR-SOFTLAUNCH
**Priority:** HIGH (unblocks MSG-BACKEND-195 + Frontend integration)
**Estimated:** 480 NWT (~4 days)
**Dependencies:** None (can start immediately)

---

## 🎯 OBJECTIVE

Implement the **complete Production module** (`spaceos-modules-production`) following DDD/CQRS/Event Sourcing patterns established in CRM/EHS/Kontrolling modules.

**Current Status:** Module does NOT exist (MSG-BACKEND-194 was planning only)
**Frontend Status:** ✅ DONE (MSG-FRONTEND-107) — UI ready, waiting for API
**QA Status:** 🔴 BLOCKED (MSG-BACKEND-195) — waiting for this module

---

## 📋 SCOPE

### Domain Layer (~120 NWT / 1 day)

**1. ProductionJob Aggregate Root**
```csharp
namespace SpaceOS.Modules.Production.Domain.ProductionJobs;

public class ProductionJob : AggregateRoot<ProductionJobId>
{
    public OrderId OrderId { get; private set; }
    public CustomerId CustomerId { get; private set; }
    public ProductionDeadline Deadline { get; private set; }
    public ProductionStatus Status { get; private set; }
    public List<WorkflowStep> Steps { get; private set; } // 6 STAGE

    // Factory
    public static ProductionJob Create(OrderId orderId, CustomerId customerId, ProductionDeadline deadline);

    // Commands
    public Result StartStep(WorkflowStepName stepName);
    public Result CompleteStep(WorkflowStepName stepName, PhotoUrl? photo = null);
    public Result MarkAsShippingReady();
}
```

**2. WorkflowStep Entity (6 STAGE)**
```csharp
public class WorkflowStep : Entity<WorkflowStepId>
{
    public WorkflowStepName Name { get; private set; } // Enum: Szabászat, Megmunkálás, ...
    public WorkflowStepStatus Status { get; private set; } // Enum: Pending, InProgress, Done
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public PhotoUrl? PhotoUrl { get; private set; } // Összeszerelés step

    public Result Start();
    public Result Complete(PhotoUrl? photo = null);
}
```

**3. Value Objects**
- `ProductionJobId` (Guid-based)
- `WorkflowStepId` (Guid-based)
- `WorkflowStepName` (enum: Szabászat, Megmunkálás, Felületkezelés, Összeszerelés, Csomagolás, KiszállításraMegjelölés)
- `WorkflowStepStatus` (enum: Pending, InProgress, Done)
- `ProductionStatus` (enum: Queued, InProgress, ShippingReady)
- `ProductionDeadline` (DateTimeOffset)
- `PhotoUrl` (string validation)

**4. Domain Events**
```csharp
// Production.Domain.ProductionJobs.Events
public record ProductionJobStarted(ProductionJobId JobId, OrderId OrderId, DateTimeOffset CreatedAt);
public record WorkflowStepStarted(ProductionJobId JobId, WorkflowStepName StepName, DateTimeOffset StartedAt);
public record WorkflowStepCompleted(ProductionJobId JobId, WorkflowStepName StepName, DateTimeOffset CompletedAt, PhotoUrl? PhotoUrl);
public record ProductionJobShippingReady(ProductionJobId JobId, DateTimeOffset ReadyAt);
```

**5. FSM Rules (Domain Logic)**
- Only ONE step can be InProgress at a time
- Steps must be completed IN ORDER (cannot skip)
- Photo upload REQUIRED for "Összeszerelés" step
- ShippingReady only when all 6 steps Done

---

### Application Layer (~120 NWT / 1 day)

**1. Commands + Handlers**
```csharp
// Production.Application.ProductionJobs.Commands

// StartProductionJobCommand
public record StartProductionJobCommand(Guid OrderId, Guid CustomerId, DateTimeOffset Deadline) : ICommand<Guid>;
public class StartProductionJobCommandHandler : ICommandHandler<StartProductionJobCommand, Guid> { }

// StartWorkflowStepCommand
public record StartWorkflowStepCommand(Guid JobId, string StepName) : ICommand;
public class StartWorkflowStepCommandHandler : ICommandHandler<StartWorkflowStepCommand> { }

// CompleteWorkflowStepCommand
public record CompleteWorkflowStepCommand(Guid JobId, string StepName, string? PhotoUrl) : ICommand;
public class CompleteWorkflowStepCommandHandler : ICommandHandler<CompleteWorkflowStepCommand> { }

// MarkAsShippingReadyCommand
public record MarkAsShippingReadyCommand(Guid JobId) : ICommand;
public class MarkAsShippingReadyCommandHandler : ICommandHandler<MarkAsShippingReadyCommand> { }
```

**2. Queries + Handlers**
```csharp
// Production.Application.ProductionJobs.Queries

// GetProductionQueueQuery (műhelyvezető UI)
public record GetProductionQueueQuery(ProductionFilter Filter) : IQuery<List<ProductionJobDto>>;
public class GetProductionQueueQueryHandler : IQueryHandler<GetProductionQueueQuery, List<ProductionJobDto>> { }

// GetProductionJobByIdQuery (detail page)
public record GetProductionJobByIdQuery(Guid JobId) : IQuery<ProductionJobDetailDto>;
public class GetProductionJobByIdQueryHandler : IQueryHandler<GetProductionJobByIdQuery, ProductionJobDetailDto> { }

// GetProductionOverviewQuery (tulaj/sales dashboard)
public record GetProductionOverviewQuery() : IQuery<ProductionOverviewDto>;
public class GetProductionOverviewQueryHandler : IQueryHandler<GetProductionOverviewQuery, ProductionOverviewDto> { }
```

**3. DTOs**
```csharp
// Production.Application.ProductionJobs.DTOs

public record ProductionJobDto(
    Guid JobId,
    Guid OrderId,
    string CustomerName,
    DateTimeOffset Deadline,
    string Status, // "Queued" | "InProgress" | "ShippingReady"
    List<WorkflowStepDto> Steps,
    bool IsOverdue
);

public record WorkflowStepDto(
    string Name,        // "Szabászat", "Megmunkálás", etc.
    string Status,      // "Pending" | "InProgress" | "Done"
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    string? PhotoUrl
);

public record ProductionOverviewDto(
    int ActiveJobs,
    int CompletedJobs,
    int OverdueJobs,
    int ShippingReadyJobs,
    List<ProductionJobDto> ActiveProjects
);
```

---

### Infrastructure Layer (~120 NWT / 1 day)

**1. ProductionDbContext (EF Core)**
```csharp
namespace SpaceOS.Modules.Production.Infrastructure.Persistence;

public class ProductionDbContext : DbContext
{
    public DbSet<ProductionJob> ProductionJobs { get; set; }
    public DbSet<WorkflowStep> WorkflowSteps { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("production");
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
```

**2. Entity Configurations**
```csharp
// ProductionJobConfiguration.cs
public class ProductionJobConfiguration : IEntityTypeConfiguration<ProductionJob>
{
    public void Configure(EntityTypeBuilder<ProductionJob> builder)
    {
        builder.ToTable("production_jobs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ProductionJobId(value));

        builder.OwnsMany(x => x.Steps, steps => {
            steps.ToTable("workflow_steps");
            steps.WithOwner().HasForeignKey("ProductionJobId");
            steps.Property(s => s.Status).HasConversion<string>();
        });

        // RLS policy
        builder.HasRowLevelSecurity("production_jobs_tenant_policy");
    }
}
```

**3. Repository Implementation**
```csharp
public class ProductionJobRepository : IProductionJobRepository
{
    private readonly ProductionDbContext _context;

    public async Task<ProductionJob?> GetByIdAsync(ProductionJobId id, CancellationToken ct);
    public async Task<List<ProductionJob>> GetQueueAsync(ProductionFilter filter, CancellationToken ct);
    public async Task AddAsync(ProductionJob job, CancellationToken ct);
    public async Task UpdateAsync(ProductionJob job, CancellationToken ct);
}
```

**4. Event Subscribers (Integration)**
```csharp
// OrderConfirmedEventHandler.cs
public class OrderConfirmedEventHandler : INotificationHandler<OrderConfirmed>
{
    // Auto-create ProductionJob when Order confirmed
    public async Task Handle(OrderConfirmed notification, CancellationToken ct)
    {
        var command = new StartProductionJobCommand(
            notification.OrderId,
            notification.CustomerId,
            notification.Deadline
        );
        await _mediator.Send(command, ct);
    }
}

// CuttingCompletedEventHandler.cs (ADR-038 integration)
public class CuttingCompletedEventHandler : INotificationHandler<CuttingCompleted>
{
    // Auto-complete "Szabászat" step when Cutting service done
    public async Task Handle(CuttingCompleted notification, CancellationToken ct)
    {
        var command = new CompleteWorkflowStepCommand(
            notification.ProductionJobId,
            "Szabászat",
            null
        );
        await _mediator.Send(command, ct);
    }
}
```

**5. Migrations**
```bash
# Create migration
dotnet ef migrations add InitialProductionSchema \
  --project backend/spaceos-modules/spaceos-modules-production/Infrastructure \
  --context ProductionDbContext

# Apply migration
dotnet ef database update \
  --project backend/spaceos-modules/spaceos-modules-production/Infrastructure \
  --context ProductionDbContext
```

---

### API Layer (~120 NWT / 1 day)

**1. ProductionController (REST)**
```csharp
namespace SpaceOS.Modules.Production.API.Controllers;

[ApiController]
[Route("api/production")]
public class ProductionController : ControllerBase
{
    private readonly IMediator _mediator;

    // GET /api/production/jobs?filter=all|active|overdue|shipping
    [HttpGet("jobs")]
    public async Task<ActionResult<List<ProductionJobDto>>> GetQueue([FromQuery] string filter = "all");

    // GET /api/production/jobs/{jobId}
    [HttpGet("jobs/{jobId:guid}")]
    public async Task<ActionResult<ProductionJobDetailDto>> GetJobById(Guid jobId);

    // GET /api/production/overview
    [HttpGet("overview")]
    public async Task<ActionResult<ProductionOverviewDto>> GetOverview();

    // PUT /api/production/jobs/{jobId}/steps/{stepName}/start
    [HttpPut("jobs/{jobId:guid}/steps/{stepName}/start")]
    public async Task<IActionResult> StartStep(Guid jobId, string stepName);

    // PUT /api/production/jobs/{jobId}/steps/{stepName}/complete
    [HttpPut("jobs/{jobId:guid}/steps/{stepName}/complete")]
    public async Task<IActionResult> CompleteStep(Guid jobId, string stepName, [FromBody] CompleteStepRequest? request);

    // PUT /api/production/jobs/{jobId}/mark-shipping-ready
    [HttpPut("jobs/{jobId:guid}/mark-shipping-ready")]
    public async Task<IActionResult> MarkAsShippingReady(Guid jobId);
}
```

**2. SSE Integration**
```csharp
// ProductionEventPublisher.cs
public class ProductionEventPublisher : INotificationHandler<WorkflowStepCompleted>,
                                         INotificationHandler<ProductionJobShippingReady>
{
    private readonly ISseService _sseService;

    public async Task Handle(WorkflowStepCompleted notification, CancellationToken ct)
    {
        await _sseService.SendEventAsync("/api/sse/production", new {
            type = "WorkflowStepCompleted",
            jobId = notification.JobId,
            stepName = notification.StepName
        }, ct);
    }

    public async Task Handle(ProductionJobShippingReady notification, CancellationToken ct)
    {
        await _sseService.SendEventAsync("/api/sse/production", new {
            type = "ProductionJobShippingReady",
            jobId = notification.JobId
        }, ct);
    }
}
```

**3. OpenAPI Annotations**
```csharp
[SwaggerOperation(Summary = "Get production queue", Description = "Műhelyvezető view — filtered list of production jobs")]
[SwaggerResponse(200, "Success", typeof(List<ProductionJobDto>))]
[SwaggerResponse(401, "Unauthorized")]
```

---

## ✅ ACCEPTANCE CRITERIA

### Domain Layer (8 criteria)
- [ ] ProductionJob aggregate created with 6 STAGE workflow
- [ ] WorkflowStep entity supports Pending → InProgress → Done FSM
- [ ] FSM validation: only 1 step InProgress at a time
- [ ] FSM validation: steps must be completed in order
- [ ] Photo upload required for "Összeszerelés" step
- [ ] 4 domain events implemented (Started, StepStarted, StepCompleted, ShippingReady)
- [ ] Value objects created (ProductionJobId, WorkflowStepName, etc.)
- [ ] Unit tests cover FSM rules (no skipping, photo validation)

### Application Layer (6 criteria)
- [ ] 4 commands implemented (Start, StartStep, CompleteStep, MarkAsShippingReady)
- [ ] 3 queries implemented (Queue, ById, Overview)
- [ ] DTOs match Frontend MSG-FRONTEND-107 types (production.ts)
- [ ] Command handlers publish domain events
- [ ] Query handlers use read-optimized projections
- [ ] Application tests cover happy path + validation errors

### Infrastructure Layer (6 criteria)
- [ ] ProductionDbContext configured with RLS
- [ ] Entity configurations use owned entities for Steps
- [ ] ProductionJobRepository implements CRUD
- [ ] OrderConfirmedEventHandler auto-creates ProductionJob
- [ ] CuttingCompletedEventHandler auto-completes "Szabászat" step
- [ ] Migrations applied (production schema created)

### API Layer (7 criteria)
- [ ] 6 REST endpoints implemented (GET queue, GET detail, GET overview, PUT start, PUT complete, PUT shipping)
- [ ] SSE events published (WorkflowStepCompleted, ProductionJobShippingReady)
- [ ] OpenAPI spec generated
- [ ] Authorization policies applied (RBAC: műhelyvezető, tulaj)
- [ ] Integration tests cover 4 E2E scenarios (start job → complete 6 steps → shipping ready)
- [ ] Build verification (0 error, 0 warning)
- [ ] API contract matches Frontend expectations (MSG-FRONTEND-107)

---

## 📁 PROJECT STRUCTURE

```
backend/spaceos-modules/spaceos-modules-production/
├── Domain/
│   ├── ProductionJobs/
│   │   ├── ProductionJob.cs              # Aggregate root
│   │   ├── WorkflowStep.cs               # Entity
│   │   ├── ProductionJobId.cs            # Value object
│   │   ├── WorkflowStepName.cs           # Enum value object
│   │   ├── ProductionStatus.cs           # Enum
│   │   └── Events/
│   │       ├── ProductionJobStarted.cs
│   │       ├── WorkflowStepCompleted.cs
│   │       └── ProductionJobShippingReady.cs
│   └── Abstractions/
│       └── IProductionJobRepository.cs
├── Application/
│   ├── ProductionJobs/
│   │   ├── Commands/
│   │   │   ├── StartProductionJobCommand.cs
│   │   │   ├── CompleteWorkflowStepCommand.cs
│   │   │   └── ...
│   │   ├── Queries/
│   │   │   ├── GetProductionQueueQuery.cs
│   │   │   └── ...
│   │   └── DTOs/
│   │       ├── ProductionJobDto.cs
│   │       └── ProductionOverviewDto.cs
│   └── DependencyInjection.cs
├── Infrastructure/
│   ├── Persistence/
│   │   ├── ProductionDbContext.cs
│   │   ├── Configurations/
│   │   │   └── ProductionJobConfiguration.cs
│   │   ├── Repositories/
│   │   │   └── ProductionJobRepository.cs
│   │   └── Migrations/
│   ├── EventHandlers/
│   │   ├── OrderConfirmedEventHandler.cs
│   │   └── CuttingCompletedEventHandler.cs
│   └── DependencyInjection.cs
├── API/
│   ├── Controllers/
│   │   └── ProductionController.cs
│   ├── EventPublishers/
│   │   └── ProductionEventPublisher.cs
│   └── DependencyInjection.cs
└── Tests/
    ├── Domain.Tests/
    ├── Application.Tests/
    └── Integration.Tests/
```

---

## 🔗 INTEGRATION POINTS

### 1. Order Service Integration
**Event:** `OrderConfirmed` → Auto-create ProductionJob
**Handler:** `OrderConfirmedEventHandler`

### 2. Cutting Service Integration (ADR-038)
**Event:** `CuttingCompleted` → Auto-complete "Szabászat" step
**Handler:** `CuttingCompletedEventHandler`

### 3. Frontend Integration (MSG-FRONTEND-107)
**API Contract:** Must match `production.ts` types
**SSE Channel:** `/api/sse/production`

### 4. Datahaven Dashboard
**Metrics:** Active jobs, overdue jobs, shipping ready jobs
**Webhook:** Optional (notify dashboard on ShippingReady)

---

## 🚀 IMPLEMENTATION PLAN (4 days)

### Day 1: Domain Layer (~8 hours)
- [ ] Create `spaceos-modules-production` project structure
- [ ] Implement ProductionJob aggregate + WorkflowStep entity
- [ ] Implement value objects + enums
- [ ] Implement 4 domain events
- [ ] Write unit tests for FSM rules
- [ ] Build verification (0 error)

### Day 2: Application Layer (~8 hours)
- [ ] Implement 4 commands + handlers
- [ ] Implement 3 queries + handlers
- [ ] Implement DTOs (match Frontend types)
- [ ] Write application tests
- [ ] Build verification (0 error)

### Day 3: Infrastructure Layer (~8 hours)
- [ ] Create ProductionDbContext + configurations
- [ ] Implement ProductionJobRepository
- [ ] Create migrations
- [ ] Implement event handlers (OrderConfirmed, CuttingCompleted)
- [ ] Write infrastructure tests
- [ ] Apply migrations (dev DB)

### Day 4: API Layer (~8 hours)
- [ ] Implement ProductionController (6 endpoints)
- [ ] Implement SSE event publishers
- [ ] Generate OpenAPI spec
- [ ] Write E2E integration tests (4 scenarios)
- [ ] Build verification (0 error, 0 warning)
- [ ] Verify API contract matches Frontend

---

## 📋 TESTING STRATEGY

### Unit Tests (Domain Layer)
```csharp
[Fact]
public void CompleteStep_WhenOutOfOrder_ShouldFail()
{
    // Arrange
    var job = ProductionJob.Create(...);

    // Act
    var result = job.CompleteStep(WorkflowStepName.Megmunkálás); // Skip Szabászat

    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Should().Contain("out of order");
}

[Fact]
public void CompleteOsszeszerelesStep_WithoutPhoto_ShouldFail()
{
    // Arrange
    var job = ProductionJob.Create(...);
    // ... complete first 3 steps

    // Act
    var result = job.CompleteStep(WorkflowStepName.Összeszerelés, photoUrl: null);

    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Should().Contain("Photo required");
}
```

### Integration Tests (API Layer)
```csharp
[Fact]
public async Task CompleteAllSteps_ShouldPublishShippingReadyEvent()
{
    // Arrange
    var jobId = await CreateProductionJobAsync();

    // Act
    foreach (var step in AllSteps)
    {
        await StartStepAsync(jobId, step);
        await CompleteStepAsync(jobId, step);
    }

    // Assert
    var job = await GetJobByIdAsync(jobId);
    job.Status.Should().Be("ShippingReady");

    // Verify SSE event published
    _sseEvents.Should().Contain(e => e.Type == "ProductionJobShippingReady");
}
```

---

## 🔄 DEPENDENCIES

**Blocked by:** None (can start immediately)
**Blocks:**
- MSG-BACKEND-195 (QA Integration Tests) — waiting for this module
- Frontend integration testing — API ready

**Related:**
- MSG-BACKEND-194 (Implementation Plan) — reference spec
- MSG-FRONTEND-107 (Frontend UI) — API contract consumer

---

## 📊 SUCCESS METRICS

### Code Quality
- [ ] 0 build errors
- [ ] 0 build warnings
- [ ] Test coverage >80% (Domain + Application layers)
- [ ] E2E tests GREEN (4 scenarios)

### API Verification
- [ ] OpenAPI spec generated
- [ ] All 6 endpoints callable (Postman/curl)
- [ ] SSE events published on step completion
- [ ] Frontend types match backend DTOs (production.ts ↔ DTOs)

### Performance
- [ ] GET /api/production/jobs response <100ms (10 jobs)
- [ ] PUT /api/production/jobs/{id}/steps/{name}/complete response <200ms

### Security
- [ ] RLS policies applied (production_jobs_tenant_policy)
- [ ] Authorization policies enforced (műhelyvezető, tulaj roles)

---

## 📖 REFERENCES

| Document | Location |
|----------|----------|
| Implementation Plan | MSG-BACKEND-194 DONE (2026-07-08) |
| Frontend UI DONE | MSG-FRONTEND-107 DONE (2026-07-10) |
| Doorstar Domain Spec | `/tmp/doorstar_domain_spec.md` (MSG-ROOT-038) |
| 6 STAGE Workflow | Szabászat → Megmunkálás → Felületkezelés → Összeszerelés → Csomagolás → Kiszállítható |
| CRM Module Pattern | `backend/spaceos-modules/spaceos-modules-crm` |
| EHS Module Pattern | `backend/spaceos-modules/spaceos-modules-ehs` |
| ADR-038 | Cutting Service Integration |
| ADR-053 | Checkpoint-Based Coordination |

---

## ⚡ NEXT ACTIONS AFTER COMPLETION

1. **Resume MSG-BACKEND-195** (QA Integration Tests)
   - Estimate: 30 NWT (~1 hour)
   - Status: PARKED → ACTIVE

2. **Frontend Integration Testing**
   - Verify API endpoints work with MSG-FRONTEND-107 UI
   - Test SSE real-time updates
   - Manual QA: Mobile kiosk workflow

3. **Deploy to Dev Environment**
   - Apply migrations to dev DB
   - Verify RLS policies
   - Test with Doorstar sample data

---

## 🎯 TIMELINE

| Milestone | ETA |
|-----------|-----|
| Domain Layer DONE | Day 1 (2026-07-11) |
| Application Layer DONE | Day 2 (2026-07-12) |
| Infrastructure Layer DONE | Day 3 (2026-07-13) |
| API Layer DONE | Day 4 (2026-07-14) |
| **Module COMPLETE** | **2026-07-14 EOD** |
| MSG-BACKEND-195 (QA) | 2026-07-15 AM |
| Frontend Integration | 2026-07-15 PM |

**Total:** 4 days + 1 hour (QA) = **~4.1 days**
**EPIC Buffer:** 82 days - 4 days = 78 days remaining (LOW RISK)

---

**CRITICAL PATH IMPACT:** This task is on the critical path for EPIC-DOORSTAR-SOFTLAUNCH. Frontend is waiting, QA is blocked.

**PRIORITY:** HIGH — Start immediately!

---

📋 Conductor — MSG-BACKEND-196 Task Assignment (2026-07-10)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
