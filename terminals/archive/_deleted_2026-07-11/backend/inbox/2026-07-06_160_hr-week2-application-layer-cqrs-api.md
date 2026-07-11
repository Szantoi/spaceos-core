---
id: MSG-BACKEND-160
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
epic_id: EPIC-JT-HR
checkpoint_id: CP-HR-APPLICATION
estimated_nwt: 150
ref: MSG-CONDUCTOR-092
created: 2026-07-06
content_hash: bf5539fc8fdd1cb143e50be81a728d9d351238417265b1d37d9b34208e7dfe7d
---

# HR Week 2 — Application Layer Implementation (CQRS + API)

## Context

HR Week 1 Domain Layer complete (80 tests, CP-HR-BACKEND ✅).

**Week 2 Goal:** Implement Application Layer following CQRS pattern established in DMS/Kontrolling modules.

**Current State:**
- ✅ Domain Layer: Employee, Absence, Capacity aggregates with FSM
- ✅ 80 unit tests PASS (100% coverage)
- ✅ Repository contracts: IEmployeeRepository, IAbsenceRepository, ICapacityRepository

---

## Task

Implement Application Layer with CQRS, FluentValidation, DTOs, and API endpoints:

### 1. CQRS Commands/Queries (MediatR)

**Commands (Write Operations):**
```csharp
// Employee Commands
CreateEmployeeCommand
  - TenantId, FirstName, LastName, Email, HireDate, JobTitle, DepartmentId, Skills[]
  - Handler: CreateEmployeeCommandHandler
  - Validation: FluentValidation rules

UpdateEmployeeCommand
  - EmployeeId, JobTitle, DepartmentId, Skills[]
  - Handler: UpdateEmployeeCommandHandler

TerminateEmployeeCommand
  - EmployeeId, TerminationDate, Reason
  - Handler: TerminateEmployeeCommandHandler
  - Business rule: Cannot terminate twice

// Absence Commands
RequestAbsenceCommand
  - EmployeeId, StartDate, EndDate, AbsenceType, Reason
  - Handler: RequestAbsenceCommandHandler
  - FSM: Requested state
  - Validation: DateRange not in past, max 30 days

ApproveAbsenceCommand
  - AbsenceId, ApproverId
  - Handler: ApproveAbsenceCommandHandler
  - FSM: Requested → Approved

RejectAbsenceCommand
  - AbsenceId, RejecterId, RejectionReason
  - Handler: RejectAbsenceCommandHandler
  - FSM: Requested → Rejected

CancelAbsenceCommand
  - AbsenceId
  - Handler: CancelAbsenceCommandHandler
  - FSM: Approved → Cancelled (only if StartDate not yet reached)
```

**Queries (Read Operations):**
```csharp
// Employee Queries
GetEmployeeQuery
  - EmployeeId
  - Returns: EmployeeDto (with current capacity, active absences)

GetEmployeesQuery
  - TenantId, DepartmentId?, IsActive?, PageNumber, PageSize
  - Returns: PagedResult<EmployeeListDto>

GetEmployeesBySkillQuery
  - TenantId, Skill, PageNumber, PageSize
  - Returns: PagedResult<EmployeeListDto>

// Absence Queries
GetAbsenceQuery
  - AbsenceId
  - Returns: AbsenceDto

GetEmployeeAbsencesQuery
  - EmployeeId, Status?, PageNumber, PageSize
  - Returns: PagedResult<AbsenceListDto>

GetPendingAbsencesQuery
  - TenantId, PageNumber, PageSize
  - Returns: PagedResult<AbsenceListDto> (for approvers)

// Capacity Queries
GetEmployeeCapacityQuery
  - EmployeeId, StartDate, EndDate
  - Returns: EmployeeCapacityDto (total hours, available hours, absences)

GetDepartmentCapacityQuery
  - DepartmentId, StartDate, EndDate
  - Returns: DepartmentCapacityDto (aggregated capacity by employee)
```

**Total:** 6 commands + 8 queries = 14 handlers

---

### 2. FluentValidation Rules

**CreateEmployeeValidator:**
```csharp
public class CreateEmployeeValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .Length(2, 100)
            .WithMessage("First name must be between 2 and 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .Length(2, 100)
            .WithMessage("Last name must be between 2 and 100 characters");

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .WithMessage("Valid email address required");

        RuleFor(x => x.HireDate)
            .LessThanOrEqualTo(DateTime.Today)
            .WithMessage("Hire date cannot be in the future");

        RuleFor(x => x.Skills)
            .Must(s => s == null || s.Length <= 20)
            .WithMessage("Maximum 20 skills allowed");
    }
}
```

**RequestAbsenceValidator:**
```csharp
RuleFor(x => x.StartDate)
    .GreaterThanOrEqualTo(DateTime.Today)
    .WithMessage("Absence cannot start in the past");

RuleFor(x => x.EndDate)
    .GreaterThanOrEqualTo(x => x.StartDate)
    .WithMessage("End date must be after start date");

RuleFor(x => x)
    .Must(x => (x.EndDate - x.StartDate).Days <= 30)
    .WithMessage("Absence cannot exceed 30 days");

RuleFor(x => x.Reason)
    .MaximumLength(500)
    .WithMessage("Reason cannot exceed 500 characters");
```

**Total:** 6 validators (CreateEmployee, UpdateEmployee, TerminateEmployee, RequestAbsence, ApproveAbsence, RejectAbsence)

---

### 3. DTOs (Data Transfer Objects)

**Request DTOs:**
```csharp
public record CreateEmployeeDto(
    string FirstName,
    string LastName,
    string Email,
    DateTime HireDate,
    string JobTitle,
    Guid DepartmentId,
    string[] Skills
);

public record RequestAbsenceDto(
    Guid EmployeeId,
    DateTime StartDate,
    DateTime EndDate,
    AbsenceType Type,
    string Reason
);

public record ApproveAbsenceDto(Guid ApproverId);
public record RejectAbsenceDto(Guid RejecterId, string RejectionReason);
```

**Response DTOs:**
```csharp
public record EmployeeDto(
    Guid Id,
    Guid TenantId,
    string FirstName,
    string LastName,
    string Email,
    DateTime HireDate,
    DateTime? TerminationDate,
    string JobTitle,
    Guid DepartmentId,
    string[] Skills,
    decimal TotalCapacityHours,
    int ActiveAbsences,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record EmployeeListDto(
    Guid Id,
    string FullName,
    string Email,
    string JobTitle,
    Guid DepartmentId,
    bool IsActive,
    DateTime HireDate
);

public record AbsenceDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    DateTime StartDate,
    DateTime EndDate,
    AbsenceType Type,
    AbsenceStatus Status,
    string Reason,
    Guid? ApproverId,
    DateTime? ApprovedAt,
    string? RejectionReason,
    DateTime CreatedAt
);

public record AbsenceListDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    DateTime StartDate,
    DateTime EndDate,
    AbsenceType Type,
    AbsenceStatus Status,
    DateTime CreatedAt
);

public record EmployeeCapacityDto(
    Guid EmployeeId,
    string EmployeeName,
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalCapacityHours,
    decimal AvailableHours,
    List<AbsenceListDto> Absences
);

public record DepartmentCapacityDto(
    Guid DepartmentId,
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalCapacityHours,
    decimal AvailableHours,
    List<EmployeeCapacityDto> Employees
);
```

**Total:** 10 DTOs (4 request + 6 response)

---

### 4. API Endpoints (Minimal API)

**Employee Endpoints:**
```csharp
// POST /api/hr/employees
app.MapPost("/api/hr/employees", async (
    CreateEmployeeDto request,
    IMediator mediator,
    CancellationToken ct) =>
{
    var command = new CreateEmployeeCommand(
        request.FirstName,
        request.LastName,
        request.Email,
        request.HireDate,
        request.JobTitle,
        request.DepartmentId,
        request.Skills
    );

    var employeeId = await mediator.Send(command, ct);

    return Results.Created($"/api/hr/employees/{employeeId}", new { employeeId });
})
.RequireAuthorization("hr.create");

// GET /api/hr/employees/{id}
app.MapGet("/api/hr/employees/{id:guid}", async (
    Guid id,
    IMediator mediator,
    CancellationToken ct) =>
{
    var query = new GetEmployeeQuery(id);
    var employee = await mediator.Send(query, ct);

    return employee is not null
        ? Results.Ok(employee)
        : Results.NotFound();
})
.RequireAuthorization("hr.read");

// GET /api/hr/employees
app.MapGet("/api/hr/employees", async (
    [FromQuery] Guid? departmentId,
    [FromQuery] bool? isActive,
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 20,
    IMediator mediator,
    CancellationToken ct) =>
{
    var query = new GetEmployeesQuery(departmentId, isActive, pageNumber, pageSize);
    var employees = await mediator.Send(query, ct);

    return Results.Ok(employees);
})
.RequireAuthorization("hr.read");

// PUT /api/hr/employees/{id}
app.MapPut("/api/hr/employees/{id:guid}", async (
    Guid id,
    UpdateEmployeeDto request,
    IMediator mediator,
    CancellationToken ct) =>
{
    var command = new UpdateEmployeeCommand(id, request.JobTitle, request.DepartmentId, request.Skills);

    await mediator.Send(command, ct);

    return Results.NoContent();
})
.RequireAuthorization("hr.update");

// POST /api/hr/employees/{id}/terminate
app.MapPost("/api/hr/employees/{id:guid}/terminate", async (
    Guid id,
    TerminateEmployeeDto request,
    IMediator mediator,
    CancellationToken ct) =>
{
    var command = new TerminateEmployeeCommand(id, request.TerminationDate, request.Reason);

    await mediator.Send(command, ct);

    return Results.NoContent();
})
.RequireAuthorization("hr.terminate");
```

**Absence Endpoints:**
```csharp
// POST /api/hr/absences
app.MapPost("/api/hr/absences", async (
    RequestAbsenceDto request,
    IMediator mediator,
    CancellationToken ct) =>
{
    var command = new RequestAbsenceCommand(
        request.EmployeeId,
        request.StartDate,
        request.EndDate,
        request.Type,
        request.Reason
    );

    var absenceId = await mediator.Send(command, ct);

    return Results.Created($"/api/hr/absences/{absenceId}", new { absenceId });
})
.RequireAuthorization("hr.absence.request");

// GET /api/hr/absences/{id}
app.MapGet("/api/hr/absences/{id:guid}", async (
    Guid id,
    IMediator mediator,
    CancellationToken ct) =>
{
    var query = new GetAbsenceQuery(id);
    var absence = await mediator.Send(query, ct);

    return absence is not null
        ? Results.Ok(absence)
        : Results.NotFound();
})
.RequireAuthorization("hr.read");

// POST /api/hr/absences/{id}/approve
app.MapPost("/api/hr/absences/{id:guid}/approve", async (
    Guid id,
    ApproveAbsenceDto request,
    IMediator mediator,
    CancellationToken ct) =>
{
    var command = new ApproveAbsenceCommand(id, request.ApproverId);

    await mediator.Send(command, ct);

    return Results.NoContent();
})
.RequireAuthorization("hr.absence.approve");

// POST /api/hr/absences/{id}/reject
app.MapPost("/api/hr/absences/{id:guid}/reject", async (
    Guid id,
    RejectAbsenceDto request,
    IMediator mediator,
    CancellationToken ct) =>
{
    var command = new RejectAbsenceCommand(id, request.RejecterId, request.RejectionReason);

    await mediator.Send(command, ct);

    return Results.NoContent();
})
.RequireAuthorization("hr.absence.approve");

// DELETE /api/hr/absences/{id}
app.MapDelete("/api/hr/absences/{id:guid}", async (
    Guid id,
    IMediator mediator,
    CancellationToken ct) =>
{
    var command = new CancelAbsenceCommand(id);

    await mediator.Send(command, ct);

    return Results.NoContent();
})
.RequireAuthorization("hr.absence.cancel");

// GET /api/hr/absences/pending
app.MapGet("/api/hr/absences/pending", async (
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 20,
    IMediator mediator,
    CancellationToken ct) =>
{
    var query = new GetPendingAbsencesQuery(pageNumber, pageSize);
    var absences = await mediator.Send(query, ct);

    return Results.Ok(absences);
})
.RequireAuthorization("hr.absence.approve");
```

**Capacity Endpoints:**
```csharp
// GET /api/hr/employees/{id}/capacity
app.MapGet("/api/hr/employees/{id:guid}/capacity", async (
    Guid id,
    [FromQuery] DateTime startDate,
    [FromQuery] DateTime endDate,
    IMediator mediator,
    CancellationToken ct) =>
{
    var query = new GetEmployeeCapacityQuery(id, startDate, endDate);
    var capacity = await mediator.Send(query, ct);

    return Results.Ok(capacity);
})
.RequireAuthorization("hr.read");

// GET /api/hr/departments/{id}/capacity
app.MapGet("/api/hr/departments/{id:guid}/capacity", async (
    Guid id,
    [FromQuery] DateTime startDate,
    [FromQuery] DateTime endDate,
    IMediator mediator,
    CancellationToken ct) =>
{
    var query = new GetDepartmentCapacityQuery(id, startDate, endDate);
    var capacity = await mediator.Send(query, ct);

    return Results.Ok(capacity);
})
.RequireAuthorization("hr.read");
```

**Total:** 14 endpoints

---

### 5. Integration Tests

**Test Scope:**
```csharp
// Command Handler Tests (20+ tests)
CreateEmployeeCommandHandlerTests
  - Valid employee creation
  - Invalid email format
  - Hire date in future
  - Max skills exceeded (>20)

RequestAbsenceCommandHandlerTests
  - Valid absence request
  - Start date in past
  - End date before start date
  - Duration exceeds 30 days

ApproveAbsenceCommandHandlerTests
  - Valid approval (FSM: Requested → Approved)
  - Cannot approve non-Requested absence

// Query Handler Tests
GetEmployeeQueryHandlerTests
  - Employee exists → returns DTO with capacity + absences
  - Employee not found → returns null
  - RLS enforcement (tenant isolation)

GetEmployeeCapacityQueryHandlerTests
  - Capacity calculation with absences
  - Date range filtering

// API Endpoint Tests (E2E with Testcontainers)
HrApiTests
  - POST /api/hr/employees → 201 Created
  - GET /api/hr/employees/{id} → 200 OK
  - POST /api/hr/absences → 201 Created
  - POST /api/hr/absences/{id}/approve → 204 No Content
  - POST /api/hr/absences/{id}/reject → 204 No Content
  - DELETE /api/hr/absences/{id} → 204 No Content (if not started)
  - GET /api/hr/employees/{id}/capacity → 200 OK
```

**Expected Test Count:** 35+ integration tests

---

## Acceptance Criteria

- ✅ **14 CQRS handlers** (6 commands + 8 queries) all implemented
- ✅ **6 FluentValidation validators** with comprehensive rules
- ✅ **10 DTOs** (request + response) properly mapped
- ✅ **14 API endpoints** with correct HTTP verbs and status codes
- ✅ **35+ integration tests** PASS (command handlers + query handlers + E2E API)
- ✅ **Build: 0 errors, 0 warnings**
- ✅ **OpenAPI spec generated** (Swagger UI documentation)
- ✅ **RLS policy enforced** (tenant isolation validated in tests)

---

## Files to Create/Modify

**Application Layer (new directory):**
```
spaceos-modules-hr/src/Application/
├── Commands/
│   ├── CreateEmployeeCommand.cs + Handler
│   ├── UpdateEmployeeCommand.cs + Handler
│   ├── TerminateEmployeeCommand.cs + Handler
│   ├── RequestAbsenceCommand.cs + Handler
│   ├── ApproveAbsenceCommand.cs + Handler
│   └── RejectAbsenceCommand.cs + Handler
│   └── CancelAbsenceCommand.cs + Handler
├── Queries/
│   ├── GetEmployeeQuery.cs + Handler
│   ├── GetEmployeesQuery.cs + Handler
│   ├── GetEmployeesBySkillQuery.cs + Handler
│   ├── GetAbsenceQuery.cs + Handler
│   ├── GetEmployeeAbsencesQuery.cs + Handler
│   ├── GetPendingAbsencesQuery.cs + Handler
│   ├── GetEmployeeCapacityQuery.cs + Handler
│   └── GetDepartmentCapacityQuery.cs + Handler
├── Validators/
│   ├── CreateEmployeeValidator.cs
│   ├── UpdateEmployeeValidator.cs
│   ├── TerminateEmployeeValidator.cs
│   ├── RequestAbsenceValidator.cs
│   ├── ApproveAbsenceValidator.cs
│   └── RejectAbsenceValidator.cs
└── DTOs/
    ├── CreateEmployeeDto.cs
    ├── UpdateEmployeeDto.cs
    ├── TerminateEmployeeDto.cs
    ├── RequestAbsenceDto.cs
    ├── ApproveAbsenceDto.cs
    ├── RejectAbsenceDto.cs
    ├── EmployeeDto.cs
    ├── EmployeeListDto.cs
    ├── AbsenceDto.cs
    └── AbsenceListDto.cs
    └── EmployeeCapacityDto.cs
    └── DepartmentCapacityDto.cs
```

**API Layer (Minimal API):**
```
spaceos-modules-hr/src/Api/
└── Endpoints/
    └── HrEndpoints.cs
```

**Integration Tests:**
```
spaceos-modules-hr/tests/Application/
├── Commands/
│   ├── CreateEmployeeCommandHandlerTests.cs
│   ├── RequestAbsenceCommandHandlerTests.cs
│   └── ApproveAbsenceCommandHandlerTests.cs
├── Queries/
│   ├── GetEmployeeQueryHandlerTests.cs
│   └── GetEmployeeCapacityQueryHandlerTests.cs
└── API/
    └── HrApiTests.cs
```

**Total:** ~50 new files (~4,000 LOC)

---

## References

**DMS Application Layer (Pattern Reference):**
- `/opt/spaceos/backend/spaceos-modules/spaceos-modules-dms/src/Application/`
- CQRS pattern with MediatR (proven Week 2 DMS implementation)
- FluentValidation integration
- DTO mapping

**HR Domain Layer (Week 1):**
- Employee, Absence, Capacity aggregates
- 80 unit tests PASS
- Repository contracts

**Kontrolling Application Layer (Original Reference):**
- CQRS pattern with MediatR
- FluentValidation integration

---

## Estimated Effort

**Total: 150 NWT (~5 hours)**

| Phase | Task | NWT | Time |
|-------|------|-----|------|
| **Phase 1** | CQRS Commands (6 commands + handlers) | 40 | ~1.3 hours |
| **Phase 2** | CQRS Queries (8 queries + handlers) | 35 | ~1.2 hours |
| **Phase 3** | FluentValidation (6 validators) | 20 | ~40 min |
| **Phase 4** | DTOs (10 DTOs) | 15 | ~30 min |
| **Phase 5** | API Endpoints (14 endpoints) | 25 | ~50 min |
| **Phase 6** | Integration Tests (35+ tests) | 10 | ~20 min |
| **Phase 7** | Build + OpenAPI verification | 5 | ~10 min |

**ETA Completion:** ~5 hours from dispatch

---

## Next Steps (After DONE)

**Conductor will dispatch:**
1. Maintenance Week 2 Application Layer → Backend (MSG-BACKEND-161, 150 NWT ~5 hours)
2. QA Week 2 Application Layer → Backend (MSG-BACKEND-162, 150 NWT ~5 hours)

**Frontend readiness:**
- After HR Week 2 DONE → Frontend can start HR UI implementation
- Orval codegen + TanStack Query hooks + React components

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
