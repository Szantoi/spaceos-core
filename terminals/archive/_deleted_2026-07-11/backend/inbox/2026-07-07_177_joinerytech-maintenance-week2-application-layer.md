---
id: MSG-BACKEND-177
from: conductor
to: backend
type: task
priority: medium
status: CANCELLED
model: sonnet
epic_id: EPIC-JT-MAINTENANCE
estimated_nwt: 60
created: 2026-07-07
content_hash: 8fa2f65998c306426bbd0cf2ed3c072a069e2ac307cb4b1ec67fc22c7da5c49e
---

# JoineryTech Phase 1 Week 2: Maintenance Application Layer

## Context

**Week 1 Status:** ✅ COMPLETE (Domain layer)
- Domain layer: WorkOrder, MaintenanceSchedule, Inspection aggregates
- FSM: Work order lifecycle workflow
- Build: 0 errors, 0 warnings

**Week 4 Status:** ✅ COMPLETE (API layer)
- MSG-BACKEND-170-DONE: API layer complete

**NuGet Blocker:** ✅ RESOLVED (MSG-BACKEND-122-DONE)

## Objective

Implement the **Application Layer** for the Maintenance module following CQRS pattern.

## Scope

### 1. Commands (Write Operations)

**WorkOrder Commands:**
```csharp
// SpaceOS.Modules.Maintenance.Application/Commands/
CreateWorkOrderCommand.cs
AssignTechnicianCommand.cs
StartWorkCommand.cs
CompleteWorkCommand.cs
CancelWorkOrderCommand.cs
ReassignWorkOrderCommand.cs
```

**MaintenanceSchedule Commands:**
```csharp
CreateMaintenanceScheduleCommand.cs
UpdateScheduleCommand.cs
SkipScheduledMaintenanceCommand.cs
GenerateWorkOrdersFromScheduleCommand.cs  // Batch generation
```

**Inspection Commands:**
```csharp
CreateInspectionCommand.cs
RecordInspectionFindingCommand.cs
CompleteInspectionCommand.cs
FailInspectionCommand.cs
```

### 2. Command Handlers

```csharp
// Example: CreateWorkOrderCommandHandler.cs
public class CreateWorkOrderCommandHandler : IRequestHandler<CreateWorkOrderCommand, Result<WorkOrderDto>>
{
    private readonly IWorkOrderRepository _repository;
    private readonly IValidator<CreateWorkOrderCommand> _validator;

    public async Task<Result<WorkOrderDto>> Handle(CreateWorkOrderCommand request, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<WorkOrderDto>.Failure(validation.Errors);

        var workOrder = WorkOrder.Create(
            request.AssetId,
            request.Description,
            request.Priority,
            request.ScheduledDate
        );

        await _repository.AddAsync(workOrder, ct);
        await _repository.UnitOfWork.SaveChangesAsync(ct);

        return Result<WorkOrderDto>.Success(workOrder.ToDto());
    }
}
```

### 3. Queries (Read Operations)

```csharp
// SpaceOS.Modules.Maintenance.Application/Queries/
GetWorkOrderByIdQuery.cs
GetWorkOrdersByStatusQuery.cs
GetWorkOrdersByTechnicianQuery.cs
GetOverdueWorkOrdersQuery.cs
GetMaintenanceScheduleByIdQuery.cs
GetUpcomingMaintenanceQuery.cs          // Next 30 days
GetInspectionByIdQuery.cs
GetInspectionsByAssetQuery.cs
GetFailedInspectionsQuery.cs
GetMaintenanceHistoryQuery.cs           // Audit trail
```

### 4. Query Handlers

```csharp
// Example: GetOverdueWorkOrdersQueryHandler.cs
public class GetOverdueWorkOrdersQueryHandler : IRequestHandler<GetOverdueWorkOrdersQuery, Result<List<WorkOrderDto>>>
{
    private readonly IWorkOrderRepository _repository;

    public async Task<Result<List<WorkOrderDto>>> Handle(GetOverdueWorkOrdersQuery request, CancellationToken ct)
    {
        var overdueOrders = await _repository.GetOverdueAsync(ct);
        var dtos = overdueOrders.Select(wo => wo.ToDto()).ToList();
        return Result<List<WorkOrderDto>>.Success(dtos);
    }
}
```

### 5. DTOs (Data Transfer Objects)

```csharp
// SpaceOS.Modules.Maintenance.Application/DTOs/
WorkOrderDto.cs
MaintenanceScheduleDto.cs
InspectionDto.cs
InspectionFindingDto.cs
MaintenanceHistoryDto.cs
CreateWorkOrderDto.cs
UpdateScheduleDto.cs
// ... etc
```

### 6. Validators (FluentValidation)

```csharp
// SpaceOS.Modules.Maintenance.Application/Validators/
CreateWorkOrderCommandValidator.cs
AssignTechnicianCommandValidator.cs
CreateMaintenanceScheduleCommandValidator.cs
// ... etc
```

**Example Validator:**
```csharp
public class CreateWorkOrderCommandValidator : AbstractValidator<CreateWorkOrderCommand>
{
    public CreateWorkOrderCommandValidator()
    {
        RuleFor(x => x.AssetId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Priority).IsInEnum();
        RuleFor(x => x.ScheduledDate).GreaterThanOrEqualTo(DateTime.UtcNow.Date);
    }
}
```

### 7. Application Service Contracts

```csharp
// SpaceOS.Modules.Maintenance.Application/Contracts/
IWorkOrderService.cs
IMaintenanceScheduleService.cs
IInspectionService.cs
```

### 8. MediatR Registration

```csharp
// SpaceOS.Modules.Maintenance.Application/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddMaintenanceApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        return services;
    }
}
```

## Architecture Pattern

```
API Request (Week 4)
    ↓
Controller → Command/Query
    ↓
MediatR
    ↓
Command/Query Handler (← YOU IMPLEMENT THIS)
    ↓
Domain Repository
    ↓
Domain Aggregate (Week 1)
    ↓
EF Core DbContext
```

## Quality Gates

1. **Build:** 0 errors, 0 warnings
2. **Tests:** Write unit tests for:
   - All command handlers (happy path + validation failures)
   - All query handlers (found + not found)
   - Overdue detection logic
   - All validators (valid + invalid inputs)
3. **FSM Integration:** Work order lifecycle state transitions
4. **Validation:** FluentValidation for all commands
5. **Error Handling:** Return `Result<T>` pattern
6. **Batch Operations:** GenerateWorkOrdersFromScheduleCommand tested

## Dependencies

**NuGet Packages (already in Week 1):**
- MediatR (12.x)
- FluentValidation (11.x)
- Microsoft.EntityFrameworkCore (8.x)

## File Structure

```
SpaceOS.Modules.Maintenance.Application/
├── Commands/
├── CommandHandlers/
├── Queries/
├── QueryHandlers/
├── DTOs/
├── Validators/
├── Contracts/
└── DependencyInjection.cs
```

## Estimated Effort

**60 NWT (~2 hours)**

Breakdown:
- Commands + Handlers: 20 NWT (40 min)
- Queries + Handlers: 15 NWT (30 min)
- DTOs: 10 NWT (20 min)
- Validators: 10 NWT (20 min)
- Tests: 5 NWT (10 min)

## Acceptance Criteria

- [ ] All commands implemented with handlers
- [ ] All queries implemented with handlers
- [ ] DTOs for all aggregates
- [ ] FluentValidation for all commands
- [ ] MediatR registration configured
- [ ] Unit tests: >90% coverage
- [ ] Build: 0 errors, 0 warnings
- [ ] FSM work order workflow respected
- [ ] DONE outbox with build logs

## References

- Week 1 Domain: Domain layer completed
- Week 4 API: `MSG-BACKEND-170-DONE`
- NuGet Fix: `MSG-BACKEND-122-DONE`
- Architecture: `docs/knowledge/architecture/ADR-057-joinerytech-maintenance-domain-model.md`

---

**Priority:** MEDIUM — Week 2 completes the stack
**Blocker Status:** ✅ UNBLOCKED (NuGet resolved)

🤖 Generated by Conductor

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
