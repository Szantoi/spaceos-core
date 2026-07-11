---
id: MSG-BACKEND-174
from: conductor
to: backend
type: task
priority: high
status: SKIPPED
model: sonnet
ref: MSG-BACKEND-155-DONE
epic_id: EPIC-JT-CRM
estimated_nwt: 60
created: 2026-07-07
content_hash: dbed5cc2aeae516764a8bd4d9367d67868ff3464aa7b57c11bd9b2b1a8a26196
---

# JoineryTech Phase 1 Week 2: CRM Application Layer

## Context

**Week 1 Status:** ✅ COMPLETE (MSG-BACKEND-155-DONE)
- Domain layer: Lead, Opportunity, Customer aggregates
- FSM: 6/6 tests passing
- Build: 0 errors, 0 warnings

**NuGet Blocker:** ✅ RESOLVED (MSG-BACKEND-122-DONE)
- dotnet restore: 4.68s (was timing out at 100s)
- All .NET development unblocked

## Objective

Implement the **Application Layer** for the CRM module following CQRS pattern.

## Scope

### 1. Commands (Write Operations)

**Lead Commands:**
```csharp
// SpaceOS.Modules.CRM.Application/Commands/
CreateLeadCommand.cs
UpdateLeadCommand.cs
QualifyLeadCommand.cs        // Lead → Opportunity transition
DisqualifyLeadCommand.cs     // Mark as lost
```

**Opportunity Commands:**
```csharp
ConvertToCustomerCommand.cs  // Opportunity → Customer
MarkAsWonCommand.cs
MarkAsLostCommand.cs
UpdateOpportunityCommand.cs
```

**Customer Commands:**
```csharp
CreateCustomerCommand.cs
UpdateCustomerCommand.cs
ArchiveCustomerCommand.cs
```

### 2. Command Handlers

Each command needs a handler implementing `IRequestHandler<TCommand, TResponse>`:

```csharp
// Example: CreateLeadCommandHandler.cs
public class CreateLeadCommandHandler : IRequestHandler<CreateLeadCommand, Result<LeadDto>>
{
    private readonly ILeadRepository _repository;
    private readonly IValidator<CreateLeadCommand> _validator;

    public async Task<Result<LeadDto>> Handle(CreateLeadCommand request, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<LeadDto>.Failure(validation.Errors);

        var lead = Lead.Create(
            request.Name,
            request.Email,
            request.Phone,
            request.Source
        );

        await _repository.AddAsync(lead, ct);
        await _repository.UnitOfWork.SaveChangesAsync(ct);

        return Result<LeadDto>.Success(lead.ToDto());
    }
}
```

### 3. Queries (Read Operations)

```csharp
// SpaceOS.Modules.CRM.Application/Queries/
GetLeadByIdQuery.cs
GetAllLeadsQuery.cs
GetOpportunityByIdQuery.cs
GetAllOpportunitiesQuery.cs
GetCustomerByIdQuery.cs
GetAllCustomersQuery.cs
GetLeadsByStatusQuery.cs       // Filter by FSM state
GetOpportunitiesByStageQuery.cs
```

### 4. Query Handlers

```csharp
// Example: GetLeadByIdQueryHandler.cs
public class GetLeadByIdQueryHandler : IRequestHandler<GetLeadByIdQuery, Result<LeadDto>>
{
    private readonly ILeadRepository _repository;

    public async Task<Result<LeadDto>> Handle(GetLeadByIdQuery request, CancellationToken ct)
    {
        var lead = await _repository.GetByIdAsync(request.LeadId, ct);
        if (lead == null)
            return Result<LeadDto>.Failure("Lead not found");

        return Result<LeadDto>.Success(lead.ToDto());
    }
}
```

### 5. DTOs (Data Transfer Objects)

```csharp
// SpaceOS.Modules.CRM.Application/DTOs/
LeadDto.cs
OpportunityDto.cs
CustomerDto.cs
CreateLeadDto.cs
UpdateLeadDto.cs
// ... etc
```

### 6. Validators (FluentValidation)

```csharp
// SpaceOS.Modules.CRM.Application/Validators/
CreateLeadCommandValidator.cs
UpdateLeadCommandValidator.cs
QualifyLeadCommandValidator.cs
// ... etc
```

**Example Validator:**
```csharp
public class CreateLeadCommandValidator : AbstractValidator<CreateLeadCommand>
{
    public CreateLeadCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Phone).Matches(@"^\+?[0-9\s\-()]+$").When(x => !string.IsNullOrEmpty(x.Phone));
        RuleFor(x => x.Source).IsInEnum();
    }
}
```

### 7. Application Service Contracts

```csharp
// SpaceOS.Modules.CRM.Application/Contracts/
ILeadService.cs
IOpportunityService.cs
ICustomerService.cs
```

### 8. MediatR Registration

```csharp
// SpaceOS.Modules.CRM.Application/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddCRMApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        return services;
    }
}
```

## Architecture Pattern

```
API Request
    ↓
Controller → Command/Query
    ↓
MediatR
    ↓
Command/Query Handler
    ↓
Domain Repository
    ↓
Domain Aggregate (Lead/Opportunity/Customer)
    ↓
EF Core DbContext
```

## Quality Gates

1. **Build:** 0 errors, 0 warnings
2. **Tests:** Write unit tests for:
   - All command handlers (happy path + validation failures)
   - All query handlers (found + not found)
   - All validators (valid + invalid inputs)
3. **FSM Integration:** Command handlers must respect FSM state transitions
4. **Validation:** FluentValidation for all commands
5. **Error Handling:** Return `Result<T>` pattern, not exceptions

## Dependencies

**NuGet Packages (already in Week 1):**
- MediatR (12.x)
- FluentValidation (11.x)
- Microsoft.EntityFrameworkCore (8.x)

## File Structure

```
SpaceOS.Modules.CRM.Application/
├── Commands/
│   ├── CreateLeadCommand.cs
│   ├── UpdateLeadCommand.cs
│   ├── QualifyLeadCommand.cs
│   └── ...
├── CommandHandlers/
│   ├── CreateLeadCommandHandler.cs
│   ├── UpdateLeadCommandHandler.cs
│   └── ...
├── Queries/
│   ├── GetLeadByIdQuery.cs
│   ├── GetAllLeadsQuery.cs
│   └── ...
├── QueryHandlers/
│   ├── GetLeadByIdQueryHandler.cs
│   ├── GetAllLeadsQueryHandler.cs
│   └── ...
├── DTOs/
│   ├── LeadDto.cs
│   ├── OpportunityDto.cs
│   └── ...
├── Validators/
│   ├── CreateLeadCommandValidator.cs
│   ├── UpdateLeadCommandValidator.cs
│   └── ...
├── Contracts/
│   ├── ILeadService.cs
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
- Tests: 5 NWT (10 min)

## Acceptance Criteria

- [ ] All commands implemented with handlers
- [ ] All queries implemented with handlers
- [ ] DTOs for all aggregates
- [ ] FluentValidation for all commands
- [ ] MediatR registration configured
- [ ] Unit tests: >90% coverage
- [ ] Build: 0 errors, 0 warnings
- [ ] FSM state transitions respected in handlers
- [ ] DONE outbox with build logs

## References

- Week 1 Domain: `MSG-BACKEND-155-DONE`
- NuGet Fix: `MSG-BACKEND-122-DONE`
- Architecture: `docs/knowledge/architecture/ADR-054-joinerytech-crm-domain-model.md`

---

**Priority:** HIGH — Week 2 Application Layer is critical path for JoineryTech Phase 1
**Blocker Status:** ✅ UNBLOCKED (NuGet resolved)

🤖 Generated by Conductor

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
