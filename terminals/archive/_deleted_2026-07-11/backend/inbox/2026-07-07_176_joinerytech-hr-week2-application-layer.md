---
id: MSG-BACKEND-176
from: conductor
to: backend
type: task
priority: medium
status: CANCELLED
model: sonnet
epic_id: EPIC-JT-HR
estimated_nwt: 60
created: 2026-07-07
content_hash: 565f036a3e8d7868a76e7a8db8a84fa37394472ef9372bc6de280c2e7dedd0e0
---

# JoineryTech Phase 1 Week 2: HR Application Layer

## Context

**Week 1 Status:** ✅ COMPLETE (Domain layer)
- Domain layer: Employee, Contract, Attendance aggregates
- FSM: Contract lifecycle workflow
- Build: 0 errors, 0 warnings

**Week 3-4 Status:** ✅ COMPLETE (Infrastructure + API)
- MSG-BACKEND-166-DONE: Infrastructure layer complete
- MSG-BACKEND-169-DONE: API layer complete

**NuGet Blocker:** ✅ RESOLVED (MSG-BACKEND-122-DONE)

## Objective

Implement the **Application Layer** for the HR module following CQRS pattern. This completes the missing middle layer between Domain and Infrastructure.

## Scope

### 1. Commands (Write Operations)

**Employee Commands:**
```csharp
// SpaceOS.Modules.HR.Application/Commands/
CreateEmployeeCommand.cs
UpdateEmployeeCommand.cs
TerminateEmployeeCommand.cs
SuspendEmployeeCommand.cs
ReactivateEmployeeCommand.cs
```

**Contract Commands:**
```csharp
CreateContractCommand.cs
RenewContractCommand.cs
TerminateContractCommand.cs
UpdateContractCommand.cs
```

**Attendance Commands:**
```csharp
RecordClockInCommand.cs
RecordClockOutCommand.cs
RecordAbsenceCommand.cs
ApproveAbsenceCommand.cs
RejectAbsenceCommand.cs
```

### 2. Command Handlers

```csharp
// Example: CreateEmployeeCommandHandler.cs
public class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, Result<EmployeeDto>>
{
    private readonly IEmployeeRepository _repository;
    private readonly IValidator<CreateEmployeeCommand> _validator;

    public async Task<Result<EmployeeDto>> Handle(CreateEmployeeCommand request, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<EmployeeDto>.Failure(validation.Errors);

        var employee = Employee.Create(
            request.FullName,
            request.Email,
            request.HireDate,
            request.Position
        );

        await _repository.AddAsync(employee, ct);
        await _repository.UnitOfWork.SaveChangesAsync(ct);

        return Result<EmployeeDto>.Success(employee.ToDto());
    }
}
```

### 3. Queries (Read Operations)

```csharp
// SpaceOS.Modules.HR.Application/Queries/
GetEmployeeByIdQuery.cs
GetAllEmployeesQuery.cs
GetActiveEmployeesQuery.cs
GetEmployeesByDepartmentQuery.cs
GetContractByIdQuery.cs
GetContractsByEmployeeQuery.cs
GetExpiringContractsQuery.cs       // Contract expiry alerts
GetAttendanceByEmployeeQuery.cs
GetAttendanceByDateRangeQuery.cs
GetAbsencesForApprovalQuery.cs
GetMonthlyAttendanceReportQuery.cs
```

### 4. Query Handlers

```csharp
// Example: GetExpiringContractsQueryHandler.cs
public class GetExpiringContractsQueryHandler : IRequestHandler<GetExpiringContractsQuery, Result<List<ContractDto>>>
{
    private readonly IContractRepository _repository;

    public async Task<Result<List<ContractDto>>> Handle(GetExpiringContractsQuery request, CancellationToken ct)
    {
        var thresholdDate = DateTime.UtcNow.AddDays(request.DaysAhead);
        var contracts = await _repository.GetExpiringBeforeAsync(thresholdDate, ct);

        var dtos = contracts.Select(c => c.ToDto()).ToList();
        return Result<List<ContractDto>>.Success(dtos);
    }
}
```

### 5. DTOs (Data Transfer Objects)

```csharp
// SpaceOS.Modules.HR.Application/DTOs/
EmployeeDto.cs
ContractDto.cs
AttendanceDto.cs
AbsenceDto.cs
MonthlyAttendanceReportDto.cs
CreateEmployeeDto.cs
UpdateEmployeeDto.cs
// ... etc
```

### 6. Validators (FluentValidation)

```csharp
// SpaceOS.Modules.HR.Application/Validators/
CreateEmployeeCommandValidator.cs
RecordClockInCommandValidator.cs
RecordAbsenceCommandValidator.cs
// ... etc
```

**Example Validator:**
```csharp
public class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().NotEmpty();
        RuleFor(x => x.HireDate).NotEmpty().LessThanOrEqualTo(DateTime.UtcNow.Date);
        RuleFor(x => x.Position).NotEmpty().MaximumLength(100);
    }
}
```

### 7. Application Service Contracts

```csharp
// SpaceOS.Modules.HR.Application/Contracts/
IEmployeeService.cs
IContractService.cs
IAttendanceService.cs
IAbsenceService.cs
```

### 8. MediatR Registration

```csharp
// SpaceOS.Modules.HR.Application/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddHRApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        return services;
    }
}
```

## Architecture Pattern

```
API Request (Week 4 already exists)
    ↓
Controller → Command/Query
    ↓
MediatR
    ↓
Command/Query Handler (← YOU IMPLEMENT THIS)
    ↓
Domain Repository (Week 3 already exists)
    ↓
Domain Aggregate (Week 1 already exists)
    ↓
EF Core DbContext (Week 3 already exists)
```

## Quality Gates

1. **Build:** 0 errors, 0 warnings
2. **Tests:** Write unit tests for:
   - All command handlers (happy path + validation failures)
   - All query handlers (found + not found)
   - Contract expiry logic
   - All validators (valid + invalid inputs)
3. **FSM Integration:** Contract lifecycle state transitions
4. **Validation:** FluentValidation for all commands
5. **Error Handling:** Return `Result<T>` pattern
6. **Integration:** Verify Week 4 API endpoints work with new handlers

## Dependencies

**NuGet Packages (already in Week 1):**
- MediatR (12.x)
- FluentValidation (11.x)
- Microsoft.EntityFrameworkCore (8.x)

## File Structure

```
SpaceOS.Modules.HR.Application/
├── Commands/
│   ├── CreateEmployeeCommand.cs
│   ├── RecordClockInCommand.cs
│   └── ...
├── CommandHandlers/
│   ├── CreateEmployeeCommandHandler.cs
│   ├── RecordClockInCommandHandler.cs
│   └── ...
├── Queries/
│   ├── GetEmployeeByIdQuery.cs
│   ├── GetExpiringContractsQuery.cs
│   └── ...
├── QueryHandlers/
│   ├── GetEmployeeByIdQueryHandler.cs
│   ├── GetExpiringContractsQueryHandler.cs
│   └── ...
├── DTOs/
│   ├── EmployeeDto.cs
│   ├── ContractDto.cs
│   └── ...
├── Validators/
│   ├── CreateEmployeeCommandValidator.cs
│   └── ...
├── Contracts/
│   ├── IEmployeeService.cs
│   └── ...
└── DependencyInjection.cs
```

## Estimated Effort

**60 NWT (~2 hours)**

Breakdown:
- Commands + Handlers: 20 NWT (40 min)
- Queries + Handlers: 15 NWT (30 min)
- DTOs: 10 NWT (20 min)
- Validators: 10 NWT (20 min)
- Tests + Integration: 5 NWT (10 min)

## Acceptance Criteria

- [ ] All commands implemented with handlers
- [ ] All queries implemented with handlers
- [ ] DTOs for all aggregates
- [ ] FluentValidation for all commands
- [ ] MediatR registration configured
- [ ] Unit tests: >90% coverage
- [ ] Build: 0 errors, 0 warnings
- [ ] FSM contract lifecycle respected
- [ ] Week 4 API endpoints functional with new handlers
- [ ] DONE outbox with build logs

## References

- Week 1 Domain: Domain layer completed
- Week 3 Infrastructure: `MSG-BACKEND-166-DONE`
- Week 4 API: `MSG-BACKEND-169-DONE`
- NuGet Fix: `MSG-BACKEND-122-DONE`
- Architecture: `docs/knowledge/architecture/ADR-056-joinerytech-hr-domain-model.md`

---

**Priority:** MEDIUM — Week 2 completes the stack (Week 1, 3, 4 already done)
**Blocker Status:** ✅ UNBLOCKED (NuGet resolved)

🤖 Generated by Conductor

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
