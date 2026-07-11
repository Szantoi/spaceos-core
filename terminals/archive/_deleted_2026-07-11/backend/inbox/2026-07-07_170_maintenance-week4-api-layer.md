---
id: MSG-BACKEND-170
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-BACKEND-169
created: 2026-07-07
epic_id: EPIC-JOINERYTECH-MIGRATION
checkpoint_id: CP-JOINERYTECH-WEEK4-API
estimated_nwt: 30
content_hash: d62266aa010043576989f0114728266b20e036883dfda5165c45cfe2a88fff77
completed: 2026-07-07
done_outbox_id: MSG-BACKEND-170-DONE
---

# Maintenance Week 4 API Layer Implementation

**Epic:** EPIC-JOINERYTECH-MIGRATION
**Checkpoint:** CP-JOINERYTECH-WEEK4-API
**Module:** Maintenance
**Phase:** Week 4 — API Layer (Pattern Reuse from DMS + HR)

---

## 🎯 Objective

Implement **Minimal API endpoints** for the Maintenance module with full CQRS/MediatR pattern, covering:
- Asset CRUD operations (with owned collection: MaintenancePlan)
- WorkOrder CRUD operations (with owned collection: WorkOrderPart containing Money VO)
- FSM state transitions (WorkOrder: Planned → InProgress → Completed)
- Request/Response DTOs
- FluentValidation rules
- API integration tests with Testcontainers + authentication

**Expected acceleration:** 60 NWT → 30 NWT (50% faster through DMS + HR pattern reuse)

**Strategic role:** This is the **THIRD** Week 4 API module — validate pattern mastery!

---

## 📦 Deliverables

### 1. **Application Layer** — CQRS Commands & Queries

**Commands (Write operations):**
```
Application/Commands/
├── CreateAssetCommand.cs
├── UpdateAssetCommand.cs
├── UpdateAssetMaintenancePlanCommand.cs   # Owned collection update
├── CreateWorkOrderCommand.cs
├── UpdateWorkOrderCommand.cs
├── AddWorkOrderPartCommand.cs             # Owned collection: add item
├── StartWorkOrderCommand.cs               # FSM: Planned → InProgress
└── CompleteWorkOrderCommand.cs            # FSM: InProgress → Completed
```

**Queries (Read operations):**
```
Application/Queries/
├── GetAssetQuery.cs
├── ListAssetsQuery.cs
├── GetAssetMaintenancePlanQuery.cs        # Owned collection query
├── GetWorkOrderQuery.cs
├── ListWorkOrdersQuery.cs
└── ListWorkOrdersByAssetQuery.cs
```

**Handlers:** 14 total (8 command + 6 query handlers)

**DTOs:**
```
Application/DTOs/
├── AssetDto.cs                            # Includes MaintenancePlanDto
├── AssetListDto.cs
├── MaintenancePlanDto.cs                  # From owned collection
├── WorkOrderDto.cs                        # Includes WorkOrderPartDto[]
├── WorkOrderListDto.cs
├── WorkOrderPartDto.cs                    # Includes MoneyDto (nested VO)
└── MoneyDto.cs                            # Value object DTO
```

**Validators (FluentValidation):**
```
Application/Validators/
├── CreateAssetCommandValidator.cs
├── UpdateAssetMaintenancePlanCommandValidator.cs
├── CreateWorkOrderCommandValidator.cs
├── AddWorkOrderPartCommandValidator.cs
└── CompleteWorkOrderCommandValidator.cs
```

### 2. **API Layer** — Minimal API Endpoints

**Endpoints:**
```
API/Endpoints/
├── AssetEndpoints.cs         # 5 endpoints (CRUD + MaintenancePlan update)
└── WorkOrderEndpoints.cs     # 7 endpoints (CRUD + Add part + Start/Complete + List by asset)
```

**Expected endpoints (12 total):**

**Asset:**
- `POST /api/maintenance/assets` — Create asset
- `GET /api/maintenance/assets/{id}` — Get by ID (includes MaintenancePlan)
- `GET /api/maintenance/assets` — List all (paginated, tenant-filtered)
- `PUT /api/maintenance/assets/{id}` — Update asset
- `PUT /api/maintenance/assets/{id}/maintenance-plan` — Update maintenance plan (owned collection)

**WorkOrder:**
- `POST /api/maintenance/work-orders` — Create work order
- `GET /api/maintenance/work-orders/{id}` — Get by ID (includes WorkOrderPart[])
- `GET /api/maintenance/work-orders` — List all (paginated, tenant-filtered)
- `GET /api/maintenance/work-orders/asset/{assetId}` — List by asset
- `POST /api/maintenance/work-orders/{id}/parts` — Add part (owned collection)
- `POST /api/maintenance/work-orders/{id}/start` — Start work order (FSM transition)
- `POST /api/maintenance/work-orders/{id}/complete` — Complete work order (FSM transition)

### 3. **Integration Tests** — API Tests with Testcontainers

**Test structure:**
```
tests/Integration/Api/
├── ApiTestFixture.cs              # Reuse DMS pattern!
├── AssetApiTests.cs               # 7 test scenarios
└── WorkOrderApiTests.cs           # 9 test scenarios
```

**Test scenarios (16 total):**

**AssetApiTests:**
1. `CreateAsset_ValidRequest_ReturnsCreated`
2. `GetAsset_IncludesMaintenancePlan_ReturnsCompleteData` (owned collection)
3. `UpdateAssetMaintenancePlan_ValidRequest_UpdatesCollection`
4. `ListAssets_WithPagination_ReturnsPagedResults`
5. `ListAssets_MultiTenant_OnlyReturnsTenantData` (RLS validation)
6. `CreateAsset_InvalidSerialNumber_ReturnsBadRequest` (FluentValidation)
7. `UpdateAsset_NonExistentId_ReturnsNotFound`

**WorkOrderApiTests:**
1. `CreateWorkOrder_ValidRequest_ReturnsCreated`
2. `GetWorkOrder_IncludesParts_ReturnsCompleteData` (owned collection with Money VO)
3. `AddWorkOrderPart_ValidRequest_AddsToParts` (owned collection)
4. `StartWorkOrder_PlannedWorkOrder_TransitionsToInProgress` (FSM)
5. `CompleteWorkOrder_InProgressWorkOrder_TransitionsToCompleted` (FSM)
6. `StartWorkOrder_AlreadyStarted_ReturnsBadRequest` (FSM validation)
7. `ListWorkOrdersByAsset_ValidAssetId_ReturnsFiltered`
8. `ListWorkOrders_MultiTenant_OnlyReturnsTenantData` (RLS validation)
9. `CreateWorkOrder_OverlappingSchedule_ReturnsBadRequest` (business rule)

---

## 🏗️ Pattern Reuse from DMS + HR ✅

**All 8 patterns established in DMS + HR apply:**

### From DMS Week 4:
1. ✅ Minimal API endpoint structure
2. ✅ CQRS Command/Query handlers
3. ✅ FluentValidation rules
4. ✅ API Integration Tests with Testcontainers
5. ✅ Multi-tenancy enforcement

### From HR Week 4:
6. ✅ Complex DTO mapping (nested owned entities)
7. ✅ FSM state transition endpoints
8. ✅ Owned collection update endpoints

---

## 🔧 Maintenance-Specific Patterns (NEW!)

### Pattern #9: Nested Value Object DTO (Money in WorkOrderPart)

```csharp
// Application/DTOs/WorkOrderDto.cs
public record WorkOrderDto
{
    public Guid Id { get; init; }
    public Guid AssetId { get; init; }
    public string Description { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;  // FSM state
    public DateTime PlannedDate { get; init; }
    public DateTime? CompletedDate { get; init; }
    public List<WorkOrderPartDto> Parts { get; init; } = new();  // Owned collection
}

// Owned collection DTO with nested Value Object
public record WorkOrderPartDto
{
    public string PartNumber { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public MoneyDto UnitPrice { get; init; } = null!;  // Nested VO
    public MoneyDto TotalPrice { get; init; } = null!;  // Nested VO
}

// Value Object DTO
public record MoneyDto
{
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
}
```

**Query handler with nested VO projection:**
```csharp
public async Task<WorkOrderDto?> Handle(GetWorkOrderQuery request, CancellationToken ct)
{
    var result = await _dbContext.WorkOrders
        .Where(w => w.Id == new WorkOrderId(request.Id))
        .Where(w => w.TenantId == new TenantId(request.TenantId))
        .Select(w => new WorkOrderDto
        {
            Id = w.Id.Value,
            AssetId = w.AssetId.Value,
            Description = w.Description,
            Status = w.Status.ToString(),
            PlannedDate = w.PlannedDate,
            CompletedDate = w.CompletedDate,
            // Owned collection with nested VO projection
            Parts = w.Parts.Select(p => new WorkOrderPartDto
            {
                PartNumber = p.PartNumber,
                Description = p.Description,
                Quantity = p.Quantity,
                // Nested Value Object projection
                UnitPrice = new MoneyDto
                {
                    Amount = p.UnitPrice.Amount,
                    Currency = p.UnitPrice.Currency
                },
                TotalPrice = new MoneyDto
                {
                    Amount = p.TotalPrice.Amount,
                    Currency = p.TotalPrice.Currency
                }
            }).ToList()
        })
        .FirstOrDefaultAsync(ct);

    return result;
}
```

### Pattern #10: Owned Collection "Add Item" Endpoint

```csharp
// API/Endpoints/WorkOrderEndpoints.cs
group.MapPost("/{id:guid}/parts", async (
    [FromRoute] Guid id,
    [FromBody] AddWorkOrderPartRequest request,
    [FromServices] IMediator mediator,
    [FromServices] ITenantContext tenantContext,
    CancellationToken ct) =>
{
    var command = new AddWorkOrderPartCommand(
        id,
        tenantContext.TenantId,
        request.PartNumber,
        request.Description,
        request.Quantity,
        request.UnitPrice
    );
    var result = await mediator.Send(command, ct);
    return result ? Results.NoContent() : Results.NotFound();
})
.WithName("AddWorkOrderPart")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound);

// Application/Handlers/AddWorkOrderPartHandler.cs
public async Task<bool> Handle(AddWorkOrderPartCommand request, CancellationToken ct)
{
    var workOrder = await _repository.GetByIdAsync(
        new WorkOrderId(request.Id),
        new TenantId(request.TenantId),
        ct
    );

    if (workOrder == null)
        return false;

    // Domain method adds to owned collection
    var part = new WorkOrderPart(
        request.PartNumber,
        request.Description,
        request.Quantity,
        Money.Create(request.UnitPrice, "HUF")
    );

    workOrder.AddPart(part);

    await _repository.UpdateAsync(workOrder, ct);
    return true;
}
```

### Pattern #11: FSM Multi-Step Transition Flow

```csharp
// API/Endpoints/WorkOrderEndpoints.cs
group.MapPost("/{id:guid}/start", async (
    [FromRoute] Guid id,
    [FromBody] StartWorkOrderRequest request,
    [FromServices] IMediator mediator,
    [FromServices] ITenantContext tenantContext,
    CancellationToken ct) =>
{
    var command = new StartWorkOrderCommand(
        id,
        tenantContext.TenantId,
        request.AssignedTechnicianId,
        request.StartDate
    );
    var result = await mediator.Send(command, ct);
    return result ? Results.NoContent() : Results.NotFound();
})
.WithName("StartWorkOrder")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound)
.Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

group.MapPost("/{id:guid}/complete", async (
    [FromRoute] Guid id,
    [FromBody] CompleteWorkOrderRequest request,
    [FromServices] IMediator mediator,
    [FromServices] ITenantContext tenantContext,
    CancellationToken ct) =>
{
    var command = new CompleteWorkOrderCommand(
        id,
        tenantContext.TenantId,
        request.CompletionNotes,
        request.CompletedDate
    );
    var result = await mediator.Send(command, ct);
    return result ? Results.NoContent() : Results.NotFound();
})
.WithName("CompleteWorkOrder")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound)
.Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

// Application/Handlers/StartWorkOrderHandler.cs
public async Task<bool> Handle(StartWorkOrderCommand request, CancellationToken ct)
{
    var workOrder = await _repository.GetByIdAsync(
        new WorkOrderId(request.Id),
        new TenantId(request.TenantId),
        ct
    );

    if (workOrder == null)
        return false;

    // Domain method validates FSM transition (Planned → InProgress)
    workOrder.Start(
        new TechnicianId(request.AssignedTechnicianId),
        request.StartDate
    );

    await _repository.UpdateAsync(workOrder, ct);
    return true;
}

// Application/Handlers/CompleteWorkOrderHandler.cs
public async Task<bool> Handle(CompleteWorkOrderCommand request, CancellationToken ct)
{
    var workOrder = await _repository.GetByIdAsync(
        new WorkOrderId(request.Id),
        new TenantId(request.TenantId),
        ct
    );

    if (workOrder == null)
        return false;

    // Domain method validates FSM transition (InProgress → Completed)
    workOrder.Complete(
        request.CompletionNotes,
        request.CompletedDate
    );

    await _repository.UpdateAsync(workOrder, ct);
    return true;
}
```

---

## 📋 Acceptance Criteria

**Build Quality:**
- [ ] `dotnet build src/SpaceOS.Modules.Maintenance.csproj` — 0 errors, 0 warnings
- [ ] `dotnet build tests/SpaceOS.Modules.Maintenance.Tests.csproj` — 0 errors, 0 warnings

**API Endpoints:**
- [ ] 12 Minimal API endpoints implemented (5 Asset + 7 WorkOrder)
- [ ] Nested Value Object DTO mapping works (Money in WorkOrderPart)
- [ ] Owned collection "add item" endpoint works (AddWorkOrderPart)
- [ ] FSM multi-step transitions work (Start, Complete)
- [ ] OpenAPI documentation generated

**CQRS/MediatR:**
- [ ] 8 Commands + 8 Command Handlers implemented
- [ ] 6 Queries + 6 Query Handlers implemented
- [ ] 5 FluentValidation validators implemented

**Integration Tests:**
- [ ] 16 API test scenarios implemented
- [ ] Nested VO test passes (Money in WorkOrderPart)
- [ ] FSM multi-step transition tests pass (Start → Complete)
- [ ] Multi-tenancy isolation validated

**Pattern Reuse:**
- [ ] All 8 DMS + HR patterns successfully reused
- [ ] 3 new Maintenance-specific patterns established

---

## ⏱️ Timeline Estimate

**Total estimated:** 60 NWT → **30 NWT** (50% faster via DMS + HR pattern reuse)

| Phase | NWT | Time | Notes |
|-------|-----|------|-------|
| **Application Layer** | 14 | 28 min | Commands, Queries, Handlers (reuse DMS + HR) |
| **API Layer** | 6 | 12 min | 12 endpoints (reuse patterns) |
| **Integration Tests** | 8 | 16 min | 16 tests (reuse fixture) |
| **Build** | 2 | 4 min | Verification |
| **TOTAL** | **30 NWT** | **~1h** | Pattern mastery! |

**Expected delivery:** ~1h from task start

---

## 🚀 Next Steps

After Maintenance API Layer DONE:
- Conductor will dispatch **QA Week 4 API** (~30 NWT) — FINAL MODULE!
- Week 4 cascade completion: ~4-6h total
- Celebrate Week 4 complete! 🎉

---

## 🎯 Focus

**Primary goal:** Validate pattern mastery (3rd iteration)!

**New patterns to establish:**
1. Nested Value Object DTO (Money in WorkOrderPart)
2. Owned collection "add item" endpoint
3. FSM multi-step transition flow

**Quality gate:** 0 errors, 0 warnings, 100% test pass rate.

---

Good luck! This should be smooth sailing with pattern mastery! 🚀

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
