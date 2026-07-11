---
id: MSG-BACKEND-171
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-BACKEND-170
created: 2026-07-07
epic_id: EPIC-JOINERYTECH-MIGRATION
checkpoint_id: CP-JOINERYTECH-WEEK4-API
estimated_nwt: 30
content_hash: 7d69f5eeb8703140d00229a1ad3419ff119ded4e1b40985ccee3cbd245df1e4b
started: 2026-07-07
---

# QA Week 4 API Layer Implementation

**Epic:** EPIC-JOINERYTECH-MIGRATION
**Checkpoint:** CP-JOINERYTECH-WEEK4-API
**Module:** QA (Quality Assurance)
**Phase:** Week 4 — API Layer (FINAL MODULE — Pattern Validation)

---

## 🎯 Objective

Implement **Minimal API endpoints** for the QA module with full CQRS/MediatR pattern, covering:
- QACheckpoint CRUD operations (with owned collection: InspectionCriteria)
- Inspection CRUD operations (with owned collection: FailureNote)
- Production integration endpoints (`HasBlockingInspectionsAsync`, `GetBlockingInspectionsAsync`)
- FSM state transitions (Inspection: Planned → InProgress → Completed)
- Request/Response DTOs
- FluentValidation rules
- API integration tests with Testcontainers + authentication

**Expected acceleration:** 60 NWT → 30 NWT (50% faster through DMS + HR + Maintenance pattern reuse)

**Strategic role:** This is the **FINAL** Week 4 API module — celebrate pattern validation! 🎉

---

## 📦 Deliverables

### 1. **Application Layer** — CQRS Commands & Queries

**Commands (Write operations):**
```
Application/Commands/
├── CreateQACheckpointCommand.cs
├── UpdateQACheckpointCommand.cs
├── UpdateQACheckpointCriteriaCommand.cs  # Owned collection update
├── CreateInspectionCommand.cs
├── UpdateInspectionCommand.cs
├── AddInspectionDefectCommand.cs         # Owned collection: add item
├── StartInspectionCommand.cs             # FSM: Planned → InProgress
└── CompleteInspectionCommand.cs          # FSM: InProgress → Completed (Pass/Fail)
```

**Queries (Read operations):**
```
Application/Queries/
├── GetQACheckpointQuery.cs
├── ListQACheckpointsQuery.cs
├── GetQACheckpointCriteriaQuery.cs       # Owned collection query
├── GetInspectionQuery.cs
├── ListInspectionsQuery.cs
├── ListInspectionsByOrderQuery.cs        # Production integration
└── HasBlockingInspectionsQuery.cs        # Production integration (boolean)
```

**Handlers:** 15 total (8 command + 7 query handlers)

**DTOs:**
```
Application/DTOs/
├── QACheckpointDto.cs                    # Includes InspectionCriteriaDto[]
├── QACheckpointListDto.cs
├── InspectionCriteriaDto.cs              # From owned collection
├── InspectionDto.cs                      # Includes FailureNoteDto[]
├── InspectionListDto.cs
├── FailureNoteDto.cs                     # From owned collection
└── BlockingInspectionDto.cs              # Production integration DTO
```

**Validators (FluentValidation):**
```
Application/Validators/
├── CreateQACheckpointCommandValidator.cs
├── UpdateQACheckpointCriteriaCommandValidator.cs
├── CreateInspectionCommandValidator.cs
├── AddInspectionDefectCommandValidator.cs
└── CompleteInspectionCommandValidator.cs
```

### 2. **API Layer** — Minimal API Endpoints

**Endpoints:**
```
API/Endpoints/
├── QACheckpointEndpoints.cs   # 5 endpoints (CRUD + Criteria update)
└── InspectionEndpoints.cs     # 9 endpoints (CRUD + Defect add + Start/Complete + Production integration)
```

**Expected endpoints (14 total):**

**QACheckpoint:**
- `POST /api/qa/checkpoints` — Create checkpoint
- `GET /api/qa/checkpoints/{id}` — Get by ID (includes InspectionCriteria)
- `GET /api/qa/checkpoints` — List all (paginated, tenant-filtered)
- `PUT /api/qa/checkpoints/{id}` — Update checkpoint
- `PUT /api/qa/checkpoints/{id}/criteria` — Update criteria (owned collection)

**Inspection:**
- `POST /api/qa/inspections` — Create inspection
- `GET /api/qa/inspections/{id}` — Get by ID (includes FailureNote[])
- `GET /api/qa/inspections` — List all (paginated, tenant-filtered)
- `GET /api/qa/inspections/order/{orderId}` — List by order (production integration)
- `POST /api/qa/inspections/{id}/defects` — Add defect (owned collection)
- `POST /api/qa/inspections/{id}/start` — Start inspection (FSM transition)
- `POST /api/qa/inspections/{id}/complete` — Complete inspection (FSM transition)
- `GET /api/qa/inspections/order/{orderId}/has-blocking` — **Production integration (boolean)**
- `GET /api/qa/inspections/order/{orderId}/blocking` — **Production integration (list)**

### 3. **Integration Tests** — API Tests with Testcontainers

**Test structure:**
```
tests/Integration/Api/
├── ApiTestFixture.cs              # Reuse DMS pattern!
├── QACheckpointApiTests.cs        # 7 test scenarios
└── InspectionApiTests.cs          # 11 test scenarios (includes production integration)
```

**Test scenarios (18 total):**

**QACheckpointApiTests:**
1. `CreateQACheckpoint_ValidRequest_ReturnsCreated`
2. `GetQACheckpoint_IncludesCriteria_ReturnsCompleteData` (owned collection)
3. `UpdateQACheckpointCriteria_ValidRequest_UpdatesCollection`
4. `ListQACheckpoints_WithPagination_ReturnsPagedResults`
5. `ListQACheckpoints_MultiTenant_OnlyReturnsTenantData` (RLS validation)
6. `CreateQACheckpoint_InvalidCriticalLevel_ReturnsBadRequest` (FluentValidation)
7. `UpdateQACheckpoint_NonExistentId_ReturnsNotFound`

**InspectionApiTests:**
1. `CreateInspection_ValidRequest_ReturnsCreated`
2. `GetInspection_IncludesDefects_ReturnsCompleteData` (owned collection)
3. `AddInspectionDefect_ValidRequest_AddsToDefects` (owned collection)
4. `StartInspection_PlannedInspection_TransitionsToInProgress` (FSM)
5. `CompleteInspection_InProgressInspection_TransitionsToCompleted` (FSM)
6. `CompleteInspection_WithFailResult_SetsResultToFail` (FSM + result)
7. `StartInspection_AlreadyStarted_ReturnsBadRequest` (FSM validation)
8. `ListInspectionsByOrder_ValidOrderId_ReturnsFiltered` (production integration)
9. `HasBlockingInspections_FailedInspection_ReturnsTrue` (production integration)
10. `HasBlockingInspections_PassedInspection_ReturnsFalse` (production integration)
11. `ListInspections_MultiTenant_OnlyReturnsTenantData` (RLS validation)

---

## 🏗️ Pattern Reuse from DMS + HR + Maintenance ✅

**All 11 patterns established in previous modules apply:**

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

### From Maintenance Week 4:
9. ✅ Nested Value Object DTO (if needed)
10. ✅ Owned collection "add item" endpoint
11. ✅ FSM multi-step transition flow

---

## 🔧 QA-Specific Patterns (NEW!)

### Pattern #12: Production Integration Endpoints (Boolean Check + List)

```csharp
// API/Endpoints/InspectionEndpoints.cs

// Boolean check endpoint
group.MapGet("/order/{orderId:guid}/has-blocking", async (
    [FromRoute] Guid orderId,
    [FromServices] IMediator mediator,
    [FromServices] ITenantContext tenantContext,
    CancellationToken ct) =>
{
    var query = new HasBlockingInspectionsQuery(
        orderId,
        tenantContext.TenantId
    );
    var hasBlocking = await mediator.Send(query, ct);
    return Results.Ok(new { hasBlockingInspections = hasBlocking });
})
.WithName("HasBlockingInspections")
.Produces<bool>(StatusCodes.Status200OK);

// List blocking inspections endpoint
group.MapGet("/order/{orderId:guid}/blocking", async (
    [FromRoute] Guid orderId,
    [FromServices] IMediator mediator,
    [FromServices] ITenantContext tenantContext,
    CancellationToken ct) =>
{
    var query = new GetBlockingInspectionsQuery(
        orderId,
        tenantContext.TenantId
    );
    var blockingInspections = await mediator.Send(query, ct);
    return Results.Ok(blockingInspections);
})
.WithName("GetBlockingInspections")
.Produces<List<BlockingInspectionDto>>(StatusCodes.Status200OK);
```

**Query handlers with business logic:**
```csharp
// Application/Queries/HasBlockingInspectionsQuery.cs
public record HasBlockingInspectionsQuery(
    Guid OrderId,
    Guid TenantId
) : IRequest<bool>;

// Application/Handlers/HasBlockingInspectionsHandler.cs
public class HasBlockingInspectionsHandler
    : IRequestHandler<HasBlockingInspectionsQuery, bool>
{
    private readonly QADbContext _dbContext;

    public async Task<bool> Handle(
        HasBlockingInspectionsQuery request,
        CancellationToken ct)
    {
        // Business rule: Failed inspection blocks production
        var hasBlocking = await _dbContext.Inspections
            .Where(i => i.OrderId == new OrderId(request.OrderId))
            .Where(i => i.TenantId == new TenantId(request.TenantId))
            .Where(i => i.Result == InspectionResult.Fail)
            .AnyAsync(ct);

        return hasBlocking;
    }
}

// Application/Queries/GetBlockingInspectionsQuery.cs
public record GetBlockingInspectionsQuery(
    Guid OrderId,
    Guid TenantId
) : IRequest<List<BlockingInspectionDto>>;

// Application/Handlers/GetBlockingInspectionsHandler.cs
public class GetBlockingInspectionsHandler
    : IRequestHandler<GetBlockingInspectionsQuery, List<BlockingInspectionDto>>
{
    private readonly QADbContext _dbContext;

    public async Task<List<BlockingInspectionDto>> Handle(
        GetBlockingInspectionsQuery request,
        CancellationToken ct)
    {
        var blockingInspections = await _dbContext.Inspections
            .Where(i => i.OrderId == new OrderId(request.OrderId))
            .Where(i => i.TenantId == new TenantId(request.TenantId))
            .Where(i => i.Result == InspectionResult.Fail)
            .Select(i => new BlockingInspectionDto
            {
                InspectionId = i.Id.Value,
                CheckpointId = i.CheckpointId.Value,
                Result = i.Result.ToString(),
                InspectorNotes = i.Notes,
                CompletedAt = i.CompletedAt
            })
            .ToListAsync(ct);

        return blockingInspections;
    }
}
```

**Production Module usage:**
```csharp
// Production Module can call QA API:
var response = await _httpClient.GetAsync(
    $"/api/qa/inspections/order/{orderId}/has-blocking"
);
var result = await response.Content.ReadFromJsonAsync<bool>();

if (result)
{
    throw new BusinessRuleViolationException(
        "Cannot execute production order: failed QA inspection exists"
    );
}
```

### Pattern #13: FSM Result Enum Handling (Pass/Fail)

```csharp
// API/Endpoints/InspectionEndpoints.cs
group.MapPost("/{id:guid}/complete", async (
    [FromRoute] Guid id,
    [FromBody] CompleteInspectionRequest request,  // Includes Result: Pass/Fail
    [FromServices] IMediator mediator,
    [FromServices] ITenantContext tenantContext,
    CancellationToken ct) =>
{
    var command = new CompleteInspectionCommand(
        id,
        tenantContext.TenantId,
        request.Result,  // "Pass" or "Fail"
        request.InspectorNotes,
        request.CompletedDate
    );
    var result = await mediator.Send(command, ct);
    return result ? Results.NoContent() : Results.NotFound();
})
.WithName("CompleteInspection")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound)
.Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

// Application/Handlers/CompleteInspectionHandler.cs
public async Task<bool> Handle(CompleteInspectionCommand request, CancellationToken ct)
{
    var inspection = await _repository.GetByIdAsync(
        new InspectionId(request.Id),
        new TenantId(request.TenantId),
        ct
    );

    if (inspection == null)
        return false;

    // Domain method validates FSM transition + sets result
    var result = InspectionResult.Parse(request.Result);  // Parse "Pass"/"Fail"
    inspection.Complete(
        result,
        request.InspectorNotes,
        request.CompletedDate
    );

    await _repository.UpdateAsync(inspection, ct);
    return true;
}
```

**FluentValidation for Result enum:**
```csharp
// Application/Validators/CompleteInspectionCommandValidator.cs
public class CompleteInspectionCommandValidator
    : AbstractValidator<CompleteInspectionCommand>
{
    public CompleteInspectionCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Inspection ID is required");

        RuleFor(x => x.Result)
            .NotEmpty()
            .WithMessage("Inspection result is required")
            .Must(r => r == "Pass" || r == "Fail")
            .WithMessage("Result must be either 'Pass' or 'Fail'");

        RuleFor(x => x.InspectorNotes)
            .MaximumLength(1000)
            .WithMessage("Inspector notes must not exceed 1000 characters");

        RuleFor(x => x.CompletedDate)
            .NotEmpty()
            .WithMessage("Completion date is required")
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Completion date cannot be in the future");
    }
}
```

---

## 📋 Acceptance Criteria

**Build Quality:**
- [ ] `dotnet build src/SpaceOS.Modules.QA.csproj` — 0 errors, 0 warnings
- [ ] `dotnet build tests/SpaceOS.Modules.QA.Tests.csproj` — 0 errors, 0 warnings

**API Endpoints:**
- [ ] 14 Minimal API endpoints implemented (5 QACheckpoint + 9 Inspection)
- [ ] Production integration endpoints work (HasBlockingInspections, GetBlockingInspections)
- [ ] FSM result enum handling works (Pass/Fail)
- [ ] Owned collection "add item" endpoint works (AddInspectionDefect)
- [ ] OpenAPI documentation generated

**CQRS/MediatR:**
- [ ] 8 Commands + 8 Command Handlers implemented
- [ ] 7 Queries + 7 Query Handlers implemented (including production integration)
- [ ] 5 FluentValidation validators implemented

**Integration Tests:**
- [ ] 18 API test scenarios implemented
- [ ] Production integration tests pass (HasBlockingInspections boolean + list)
- [ ] FSM result enum tests pass (Complete with Pass/Fail)
- [ ] Multi-tenancy isolation validated

**Pattern Reuse:**
- [ ] All 11 DMS + HR + Maintenance patterns successfully reused
- [ ] 2 new QA-specific patterns established (production integration, FSM result enum)

---

## ⏱️ Timeline Estimate

**Total estimated:** 60 NWT → **30 NWT** (50% faster via pattern reuse)

| Phase | NWT | Time | Notes |
|-------|-----|------|-------|
| **Application Layer** | 15 | 30 min | Commands, Queries, Handlers (reuse all patterns) |
| **API Layer** | 7 | 14 min | 14 endpoints (production integration) |
| **Integration Tests** | 6 | 12 min | 18 tests (production integration) |
| **Build** | 2 | 4 min | Verification |
| **TOTAL** | **30 NWT** | **~1h** | FINAL MODULE! 🎉 |

**Expected delivery:** ~1h from task start

---

## 🎉 Week 4 API Layer COMPLETE!

After QA API Layer DONE:
- **Week 4 cascade COMPLETE!** 🎉🎉🎉
- All 4 modules (DMS, HR, Maintenance, QA) API Layer implemented
- Total Week 4 time: ~4-6h (vs ~12-16h without pattern reuse)
- Acceleration: 50-62% faster!

---

## 🚀 Pattern Library Summary (Final Count)

**Week 4 API Layer Patterns Established:**

1. ✅ Minimal API endpoint structure (DMS)
2. ✅ CQRS Command/Query handlers (DMS)
3. ✅ FluentValidation rules (DMS)
4. ✅ API Integration Tests with Testcontainers (DMS)
5. ✅ Multi-tenancy enforcement (DMS)
6. ✅ Complex DTO mapping (HR)
7. ✅ FSM state transition endpoints (HR)
8. ✅ Owned collection update endpoints (HR)
9. ✅ Nested Value Object DTO (Maintenance)
10. ✅ Owned collection "add item" endpoint (Maintenance)
11. ✅ FSM multi-step transition flow (Maintenance)
12. ✅ Production integration endpoints (QA)
13. ✅ FSM result enum handling (QA)

**Ready for future module API implementations!**

---

## 🎯 Focus

**Primary goal:** Complete the FINAL Week 4 API module with full pattern validation!

**New patterns to establish:**
1. Production integration endpoints (boolean check + list)
2. FSM result enum handling (Pass/Fail)

**Quality gate:** 0 errors, 0 warnings, 100% test pass rate.

**Celebrate:** Week 4 cascade complete after this! 🎉

---

Good luck with the FINAL module! You've got this! 🚀

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
