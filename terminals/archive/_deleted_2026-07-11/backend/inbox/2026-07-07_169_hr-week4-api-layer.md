---
id: MSG-BACKEND-169
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-BACKEND-168
created: 2026-07-07
epic_id: EPIC-JOINERYTECH-MIGRATION
checkpoint_id: CP-JOINERYTECH-WEEK4-API
estimated_nwt: 30
content_hash: 9ad690898e19f1deb2f61d78afd69adebba06d7a347fea977d63addb3bd78da7
completed: 2026-07-07
done_outbox_id: MSG-BACKEND-169-DONE
---

# HR Week 4 API Layer Implementation

**Epic:** EPIC-JOINERYTECH-MIGRATION
**Checkpoint:** CP-JOINERYTECH-WEEK4-API
**Module:** HR (Human Resources)
**Phase:** Week 4 — API Layer (Pattern Reuse from DMS)

---

## 🎯 Objective

Implement **Minimal API endpoints** for the HR module with full CQRS/MediatR pattern, covering:
- Employee CRUD operations (with complex owned entities: PersonalData, Address, Skills)
- Absence CRUD operations
- Request/Response DTOs
- FluentValidation rules
- API integration tests with Testcontainers + authentication

**Expected acceleration:** 60 NWT → 30 NWT (50% faster through DMS pattern reuse)

**Strategic role:** This is the **SECOND** Week 4 API module — validate pattern reuse works!

---

## 📦 Deliverables

### 1. **Application Layer** — CQRS Commands & Queries

**Commands (Write operations):**
```
Application/Commands/
├── CreateEmployeeCommand.cs         # Complex: includes PersonalData, Address
├── UpdateEmployeeCommand.cs
├── UpdateEmployeeSkillsCommand.cs   # Owned collection update
├── TerminateEmployeeCommand.cs      # FSM: Active → Terminated
├── CreateAbsenceCommand.cs
├── UpdateAbsenceCommand.cs
├── ApproveAbsenceCommand.cs         # FSM: Pending → Approved
└── RejectAbsenceCommand.cs          # FSM: Pending → Rejected
```

**Queries (Read operations):**
```
Application/Queries/
├── GetEmployeeQuery.cs
├── ListEmployeesQuery.cs
├── GetEmployeeSkillsQuery.cs        # Owned collection query
├── GetAbsenceQuery.cs
├── ListAbsencesQuery.cs
└── ListAbsencesByEmployeeQuery.cs
```

**Handlers:** 14 total (8 command + 6 query handlers)

**DTOs:**
```
Application/DTOs/
├── EmployeeDto.cs                   # Includes PersonalDataDto, AddressDto
├── EmployeeListDto.cs
├── PersonalDataDto.cs               # Nested: DateOfBirth, MaritalStatus, etc.
├── AddressDto.cs                    # Nested in PersonalDataDto
├── SkillDto.cs                      # From owned collection
├── AbsenceDto.cs
└── AbsenceListDto.cs
```

**Validators (FluentValidation):**
```
Application/Validators/
├── CreateEmployeeCommandValidator.cs       # Complex: nested validation
├── UpdateEmployeeCommandValidator.cs
├── UpdateEmployeeSkillsCommandValidator.cs
├── CreateAbsenceCommandValidator.cs
└── ApproveAbsenceCommandValidator.cs
```

### 2. **API Layer** — Minimal API Endpoints

**Endpoints:**
```
API/Endpoints/
├── EmployeeEndpoints.cs    # 6 endpoints (CRUD + Skills + Terminate)
└── AbsenceEndpoints.cs     # 6 endpoints (CRUD + Approve/Reject + List by employee)
```

**Expected endpoints (12 total):**

**Employee:**
- `POST /api/hr/employees` — Create employee (complex DTO: PersonalData + Address)
- `GET /api/hr/employees/{id}` — Get by ID (includes PersonalData, Address, Skills)
- `GET /api/hr/employees` — List all (paginated, tenant-filtered)
- `PUT /api/hr/employees/{id}` — Update employee
- `PUT /api/hr/employees/{id}/skills` — Update skills (owned collection)
- `POST /api/hr/employees/{id}/terminate` — Terminate (FSM transition)

**Absence:**
- `POST /api/hr/absences` — Create absence
- `GET /api/hr/absences/{id}` — Get by ID
- `GET /api/hr/absences` — List all (paginated, tenant-filtered)
- `GET /api/hr/absences/employee/{employeeId}` — List by employee
- `POST /api/hr/absences/{id}/approve` — Approve absence (FSM transition)
- `POST /api/hr/absences/{id}/reject` — Reject absence (FSM transition)

### 3. **Integration Tests** — API Tests with Testcontainers

**Test structure:**
```
tests/Integration/Api/
├── ApiTestFixture.cs              # Reuse DMS pattern!
├── EmployeeApiTests.cs            # 8 test scenarios
└── AbsenceApiTests.cs             # 8 test scenarios
```

**Test scenarios (16 total):**

**EmployeeApiTests:**
1. `CreateEmployee_ValidRequest_ReturnsCreated`
2. `CreateEmployee_ComplexPersonalData_SavesNested` (PersonalData + Address)
3. `GetEmployee_IncludesSkills_ReturnsCompleteData` (owned collection)
4. `UpdateEmployeeSkills_ValidRequest_UpdatesCollection`
5. `TerminateEmployee_ActiveEmployee_TransitionsToTerminated` (FSM)
6. `ListEmployees_WithPagination_ReturnsPagedResults`
7. `ListEmployees_MultiTenant_OnlyReturnsTenantData` (RLS validation)
8. `CreateEmployee_InvalidEmail_ReturnsBadRequest` (FluentValidation)

**AbsenceApiTests:**
1. `CreateAbsence_ValidRequest_ReturnsCreated`
2. `ApproveAbsence_PendingAbsence_TransitionsToApproved` (FSM)
3. `RejectAbsence_PendingAbsence_TransitionsToRejected` (FSM)
4. `ApproveAbsence_AlreadyApproved_ReturnsBadRequest` (FSM validation)
5. `ListAbsencesByEmployee_ValidEmployeeId_ReturnsFiltered`
6. `ListAbsences_WithPagination_ReturnsPagedResults`
7. `ListAbsences_MultiTenant_OnlyReturnsTenantData` (RLS validation)
8. `CreateAbsence_OverlappingDates_ReturnsBadRequest` (business rule)

---

## 🏗️ Pattern Reuse from DMS Week 4 ✅

**All 5 patterns established in DMS apply directly:**

### Pattern #1: Minimal API Endpoint Structure ✅
- Reuse `MapGroup("/api/hr/...")` pattern
- Reuse `WithTags()`, `RequireAuthorization()`, `Produces<>()`
- Reuse `[FromServices] IMediator` + `ITenantContext` injection

### Pattern #2: CQRS Command Structure ✅
- Reuse `record CreateXxxCommand(...) : IRequest<Guid>`
- Reuse `IRequestHandler<TCommand, TResult>` pattern
- Reuse Aggregate factory pattern (from Week 3)

### Pattern #3: CQRS Query Structure ✅
- Reuse EF Core projection pattern (no repository for reads)
- Reuse `.Select(x => new XxxDto { ... })` pattern

### Pattern #4: FluentValidation Rules ✅
- Reuse `AbstractValidator<TCommand>` pattern
- Reuse `ValidationBehavior` pipeline (already registered in DMS)

### Pattern #5: API Integration Test Structure ✅
- Reuse `ApiTestFixture` with WebApplicationFactory + Testcontainers
- Reuse JWT authentication mock
- Reuse multi-tenant isolation test pattern

---

## 🔧 HR-Specific Patterns (NEW!)

### Pattern #6: Complex DTO Mapping (Nested Owned Entities)

```csharp
// Application/DTOs/EmployeeDto.cs
public record EmployeeDto
{
    public Guid Id { get; init; }
    public string EmployeeNumber { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public PersonalDataDto PersonalData { get; init; } = null!;  // Nested owned entity
    public List<SkillDto> Skills { get; init; } = new();          // Owned collection
    public DateTime HireDate { get; init; }
}

// Nested owned entity DTO
public record PersonalDataDto
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public DateTime DateOfBirth { get; init; }
    public string MaritalStatus { get; init; } = string.Empty;
    public AddressDto Address { get; init; } = null!;  // Nested in nested
}

// Nested in nested DTO
public record AddressDto
{
    public string Street { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
}

// Owned collection DTO
public record SkillDto
{
    public string Name { get; init; } = string.Empty;
    public string Level { get; init; } = string.Empty;
    public int YearsOfExperience { get; init; }
}
```

**Query handler with complex projection:**
```csharp
public async Task<EmployeeDto?> Handle(GetEmployeeQuery request, CancellationToken ct)
{
    var result = await _dbContext.Employees
        .Where(e => e.Id == new EmployeeId(request.Id))
        .Where(e => e.TenantId == new TenantId(request.TenantId))
        .Select(e => new EmployeeDto
        {
            Id = e.Id.Value,
            EmployeeNumber = e.EmployeeNumber,
            Status = e.Status.ToString(),
            HireDate = e.HireDate,
            // Nested owned entity projection
            PersonalData = new PersonalDataDto
            {
                FirstName = e.PersonalData.FirstName,
                LastName = e.PersonalData.LastName,
                Email = e.PersonalData.Email,
                PhoneNumber = e.PersonalData.PhoneNumber,
                DateOfBirth = e.PersonalData.DateOfBirth,
                MaritalStatus = e.PersonalData.MaritalStatus.ToString(),
                // Nested in nested projection
                Address = new AddressDto
                {
                    Street = e.PersonalData.Address.Street,
                    City = e.PersonalData.Address.City,
                    PostalCode = e.PersonalData.Address.PostalCode,
                    Country = e.PersonalData.Address.Country
                }
            },
            // Owned collection projection
            Skills = e.Skills.Select(s => new SkillDto
            {
                Name = s.Name,
                Level = s.Level.ToString(),
                YearsOfExperience = s.YearsOfExperience
            }).ToList()
        })
        .FirstOrDefaultAsync(ct);

    return result;
}
```

### Pattern #7: FSM State Transition Endpoints

```csharp
// API/Endpoints/EmployeeEndpoints.cs
group.MapPost("/{id:guid}/terminate", async (
    [FromRoute] Guid id,
    [FromBody] TerminateEmployeeRequest request,
    [FromServices] IMediator mediator,
    [FromServices] ITenantContext tenantContext,
    CancellationToken ct) =>
{
    var command = new TerminateEmployeeCommand(
        id,
        tenantContext.TenantId,
        request.TerminationDate,
        request.Reason
    );
    var result = await mediator.Send(command, ct);
    return result ? Results.NoContent() : Results.NotFound();
})
.WithName("TerminateEmployee")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound)
.Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

// Application/Handlers/TerminateEmployeeHandler.cs
public async Task<bool> Handle(TerminateEmployeeCommand request, CancellationToken ct)
{
    var employee = await _repository.GetByIdAsync(
        new EmployeeId(request.Id),
        new TenantId(request.TenantId),
        ct
    );

    if (employee == null)
        return false;

    // Domain method handles FSM validation
    employee.Terminate(request.TerminationDate, request.Reason);

    await _repository.UpdateAsync(employee, ct);
    return true;
}
```

### Pattern #8: Owned Collection Update Endpoint

```csharp
// API/Endpoints/EmployeeEndpoints.cs
group.MapPut("/{id:guid}/skills", async (
    [FromRoute] Guid id,
    [FromBody] UpdateEmployeeSkillsRequest request,
    [FromServices] IMediator mediator,
    [FromServices] ITenantContext tenantContext,
    CancellationToken ct) =>
{
    var command = new UpdateEmployeeSkillsCommand(
        id,
        tenantContext.TenantId,
        request.Skills
    );
    var result = await mediator.Send(command, ct);
    return result ? Results.NoContent() : Results.NotFound();
})
.WithName("UpdateEmployeeSkills")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound);

// Application/Handlers/UpdateEmployeeSkillsHandler.cs
public async Task<bool> Handle(UpdateEmployeeSkillsCommand request, CancellationToken ct)
{
    var employee = await _repository.GetByIdAsync(
        new EmployeeId(request.Id),
        new TenantId(request.TenantId),
        ct
    );

    if (employee == null)
        return false;

    // Domain method updates owned collection
    employee.UpdateSkills(request.Skills.Select(s =>
        new Skill(s.Name, SkillLevel.Parse(s.Level), s.YearsOfExperience)
    ).ToList());

    await _repository.UpdateAsync(employee, ct);
    return true;
}
```

---

## 📋 Acceptance Criteria

**Build Quality:**
- [ ] `dotnet build src/SpaceOS.Modules.HR.csproj` — 0 errors, 0 warnings
- [ ] `dotnet build tests/SpaceOS.Modules.HR.Tests.csproj` — 0 errors, 0 warnings

**API Endpoints:**
- [ ] 12 Minimal API endpoints implemented (6 Employee + 6 Absence)
- [ ] Complex DTO mapping works (PersonalData + Address + Skills)
- [ ] FSM transition endpoints work (Terminate, Approve, Reject)
- [ ] OpenAPI documentation generated (Swagger UI accessible)

**CQRS/MediatR:**
- [ ] 8 Commands + 8 Command Handlers implemented
- [ ] 6 Queries + 6 Query Handlers implemented
- [ ] 5 FluentValidation validators implemented

**Integration Tests:**
- [ ] 16 API test scenarios implemented
- [ ] Complex nested DTO test passes
- [ ] FSM transition tests pass (Terminate, Approve, Reject)
- [ ] Multi-tenancy isolation validated

**Pattern Reuse:**
- [ ] All 5 DMS patterns successfully reused
- [ ] 3 new HR-specific patterns established (complex DTO, FSM, owned collection update)

---

## ⏱️ Timeline Estimate

**Total estimated:** 60 NWT → **30 NWT** (50% faster via DMS pattern reuse)

| Phase | NWT | Time | Notes |
|-------|-----|------|-------|
| **Application Layer** | 14 | 28 min | Commands, Queries, Handlers (reuse DMS pattern) |
| **API Layer** | 6 | 12 min | 12 endpoints (reuse DMS pattern) |
| **Integration Tests** | 8 | 16 min | 16 tests (reuse DMS fixture) |
| **Build** | 2 | 4 min | Verification |
| **TOTAL** | **30 NWT** | **~1h** | 50% faster than baseline! |

**Expected delivery:** ~1h from task start

---

## 🚀 Next Steps

After HR API Layer DONE:
- Conductor will dispatch **Maintenance Week 4 API** (~30 NWT)
- Then **QA Week 4 API** (~30 NWT)
- Week 4 total: ~4-6h (DMS + HR + Maintenance + QA)

---

## 🎯 Focus

**Primary goal:** Validate DMS pattern reuse works smoothly!

**New patterns to establish:**
1. Complex DTO mapping (nested owned entities)
2. FSM state transition endpoints
3. Owned collection update endpoints

**Quality gate:** 0 errors, 0 warnings, 100% test pass rate.

---

Good luck! Pattern reuse should make this fast and smooth! 🚀

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
