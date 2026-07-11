---
id: MSG-BACKEND-178
from: conductor
to: backend
type: task
priority: medium
status: READ
model: sonnet
epic_id: EPIC-JT-QA
estimated_nwt: 60
created: 2026-07-07
completed: 2026-07-07
content_hash: f62d7254f28c90fec17103a9185428accb8e5157e7d551abd3d9022dcb5c4637
---

# JoineryTech Phase 1 Week 2: QA Application Layer

## Context

**Week 1 Status:** ✅ COMPLETE (Domain layer)
- Domain layer: Inspection, Ticket, Checkpoint aggregates
- FSM: Ticket lifecycle workflow
- Build: 0 errors, 0 warnings

**Week 4 Status:** ✅ COMPLETE (API layer)
- MSG-BACKEND-171-DONE: API layer complete

**NuGet Blocker:** ✅ RESOLVED (MSG-BACKEND-122-DONE)

## Objective

Implement the **Application Layer** for the QA (Quality Assurance) module following CQRS pattern.

## Scope

### 1. Commands (Write Operations)

**Inspection Commands:**
```csharp
// SpaceOS.Modules.QA.Application/Commands/
CreateInspectionCommand.cs
StartInspectionCommand.cs
RecordFindingCommand.cs
CompleteInspectionCommand.cs
ReopenInspectionCommand.cs
```

**Ticket Commands:**
```csharp
CreateTicketCommand.cs
AssignTicketCommand.cs
ResolveTicketCommand.cs
CloseTicketCommand.cs
ReopenTicketCommand.cs
EscalateTicketCommand.cs
```

**Checkpoint Commands:**
```csharp
CreateCheckpointCommand.cs
MarkCheckpointPassCommand.cs
MarkCheckpointFailCommand.cs
AddCheckpointNoteCommand.cs
```

### 2. Command Handlers

```csharp
// Example: CreateTicketCommandHandler.cs
public class CreateTicketCommandHandler : IRequestHandler<CreateTicketCommand, Result<TicketDto>>
{
    private readonly ITicketRepository _repository;
    private readonly IValidator<CreateTicketCommand> _validator;

    public async Task<Result<TicketDto>> Handle(CreateTicketCommand request, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<TicketDto>.Failure(validation.Errors);

        var ticket = Ticket.Create(
            request.Title,
            request.Description,
            request.Severity,
            request.ReportedBy
        );

        await _repository.AddAsync(ticket, ct);
        await _repository.UnitOfWork.SaveChangesAsync(ct);

        return Result<TicketDto>.Success(ticket.ToDto());
    }
}
```

### 3. Queries (Read Operations)

```csharp
// SpaceOS.Modules.QA.Application/Queries/
GetInspectionByIdQuery.cs
GetInspectionsByProductQuery.cs
GetFailedInspectionsQuery.cs
GetTicketByIdQuery.cs
GetOpenTicketsQuery.cs
GetTicketsByStatusQuery.cs
GetTicketsBySeverityQuery.cs
GetCheckpointsByInspectionQuery.cs
GetFailedCheckpointsQuery.cs
GetQAMetricsQuery.cs                // Pass/fail rates, avg resolution time
```

### 4. Query Handlers

```csharp
// Example: GetQAMetricsQueryHandler.cs
public class GetQAMetricsQueryHandler : IRequestHandler<GetQAMetricsQuery, Result<QAMetricsDto>>
{
    private readonly IInspectionRepository _inspectionRepo;
    private readonly ITicketRepository _ticketRepo;

    public async Task<Result<QAMetricsDto>> Handle(GetQAMetricsQuery request, CancellationToken ct)
    {
        var inspections = await _inspectionRepo.GetByDateRangeAsync(request.StartDate, request.EndDate, ct);
        var tickets = await _ticketRepo.GetByDateRangeAsync(request.StartDate, request.EndDate, ct);

        var metrics = new QAMetricsDto
        {
            TotalInspections = inspections.Count,
            PassedInspections = inspections.Count(i => i.Status == InspectionStatus.Passed),
            FailedInspections = inspections.Count(i => i.Status == InspectionStatus.Failed),
            PassRate = inspections.Count > 0 ? (decimal)inspections.Count(i => i.Status == InspectionStatus.Passed) / inspections.Count : 0,
            TotalTickets = tickets.Count,
            OpenTickets = tickets.Count(t => t.Status != TicketStatus.Closed),
            AverageResolutionTime = tickets.Where(t => t.ResolvedAt.HasValue).Average(t => (t.ResolvedAt.Value - t.CreatedAt).TotalHours)
        };

        return Result<QAMetricsDto>.Success(metrics);
    }
}
```

### 5. DTOs (Data Transfer Objects)

```csharp
// SpaceOS.Modules.QA.Application/DTOs/
InspectionDto.cs
TicketDto.cs
CheckpointDto.cs
QAMetricsDto.cs
CreateInspectionDto.cs
CreateTicketDto.cs
// ... etc
```

**Example DTO:**
```csharp
public record QAMetricsDto
{
    public int TotalInspections { get; init; }
    public int PassedInspections { get; init; }
    public int FailedInspections { get; init; }
    public decimal PassRate { get; init; }
    public int TotalTickets { get; init; }
    public int OpenTickets { get; init; }
    public double AverageResolutionTime { get; init; }  // Hours
}
```

### 6. Validators (FluentValidation)

```csharp
// SpaceOS.Modules.QA.Application/Validators/
CreateInspectionCommandValidator.cs
CreateTicketCommandValidator.cs
RecordFindingCommandValidator.cs
// ... etc
```

**Example Validator:**
```csharp
public class CreateTicketCommandValidator : AbstractValidator<CreateTicketCommand>
{
    public CreateTicketCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Severity).IsInEnum();
        RuleFor(x => x.ReportedBy).NotEmpty();
    }
}
```

### 7. Application Service Contracts

```csharp
// SpaceOS.Modules.QA.Application/Contracts/
IInspectionService.cs
ITicketService.cs
ICheckpointService.cs
IQAMetricsService.cs
```

### 8. MediatR Registration

```csharp
// SpaceOS.Modules.QA.Application/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddQAApplication(this IServiceCollection services)
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
   - QA metrics calculations
   - All validators (valid + invalid inputs)
3. **FSM Integration:** Ticket lifecycle state transitions
4. **Validation:** FluentValidation for all commands
5. **Error Handling:** Return `Result<T>` pattern
6. **Metrics Accuracy:** Verify pass rate and resolution time calculations

## Dependencies

**NuGet Packages (already in Week 1):**
- MediatR (12.x)
- FluentValidation (11.x)
- Microsoft.EntityFrameworkCore (8.x)

## File Structure

```
SpaceOS.Modules.QA.Application/
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
- Queries + Handlers (including metrics): 18 NWT (36 min)
- DTOs: 8 NWT (16 min)
- Validators: 8 NWT (16 min)
- Tests (including metrics): 6 NWT (12 min)

## Acceptance Criteria

- [ ] All commands implemented with handlers
- [ ] All queries implemented with handlers
- [ ] DTOs for all aggregates
- [ ] QA metrics query working correctly (pass rate, resolution time)
- [ ] FluentValidation for all commands
- [ ] MediatR registration configured
- [ ] Unit tests: >90% coverage
- [ ] Build: 0 errors, 0 warnings
- [ ] FSM ticket workflow respected
- [ ] DONE outbox with build logs

## References

- Week 1 Domain: Domain layer completed
- Week 4 API: `MSG-BACKEND-171-DONE`
- NuGet Fix: `MSG-BACKEND-122-DONE`
- Architecture: JoineryTech QA domain model

---

**Priority:** MEDIUM — Week 2 completes the stack
**Blocker Status:** ✅ UNBLOCKED (NuGet resolved)

🤖 Generated by Conductor

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
