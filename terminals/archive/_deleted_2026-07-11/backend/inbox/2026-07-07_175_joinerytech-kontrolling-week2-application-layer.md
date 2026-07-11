---
id: MSG-BACKEND-175
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
epic_id: EPIC-JT-KONTROLLING
estimated_nwt: 60
created: 2026-07-07
content_hash: 4bc7ac3debb6195ac3874d75fa344e0c070a053660df741223922d214d253df4
---

# JoineryTech Phase 1 Week 2: Kontrolling Application Layer

## Context

**Week 1 Status:** ✅ COMPLETE (Domain layer)
- Domain layer: CostBudget, EarnedValue, Variance aggregates
- FSM: Budget approval workflow
- Build: 0 errors, 0 warnings

**NuGet Blocker:** ✅ RESOLVED (MSG-BACKEND-122-DONE)

## Objective

Implement the **Application Layer** for the Kontrolling (Cost Control) module following CQRS pattern.

## Scope

### 1. Commands (Write Operations)

**CostBudget Commands:**
```csharp
// SpaceOS.Modules.Kontrolling.Application/Commands/
CreateCostBudgetCommand.cs
UpdateCostBudgetCommand.cs
SubmitForApprovalCommand.cs     // Draft → PendingApproval
ApproveBudgetCommand.cs         // PendingApproval → Approved
RejectBudgetCommand.cs          // PendingApproval → Rejected
RevokeBudgetCommand.cs          // Approved → Draft
CloseBudgetCommand.cs           // Approved → Closed
```

**EarnedValue Commands:**
```csharp
RecordActualCostCommand.cs
UpdatePlannedValueCommand.cs
CalculateEACCommand.cs          // Estimate at Completion
RecalculateVariancesCommand.cs
```

**Variance Commands:**
```csharp
CreateVarianceAlertCommand.cs
AcknowledgeVarianceCommand.cs
ResolveVarianceCommand.cs
```

### 2. Command Handlers

Each command needs a handler implementing `IRequestHandler<TCommand, TResponse>`:

```csharp
// Example: CreateCostBudgetCommandHandler.cs
public class CreateCostBudgetCommandHandler : IRequestHandler<CreateCostBudgetCommand, Result<CostBudgetDto>>
{
    private readonly ICostBudgetRepository _repository;
    private readonly IValidator<CreateCostBudgetCommand> _validator;

    public async Task<Result<CostBudgetDto>> Handle(CreateCostBudgetCommand request, CancellationToken ct)
    {
        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<CostBudgetDto>.Failure(validation.Errors);

        var budget = CostBudget.Create(
            request.ProjectId,
            request.BudgetedCost,
            request.PlannedDuration,
            request.StartDate
        );

        await _repository.AddAsync(budget, ct);
        await _repository.UnitOfWork.SaveChangesAsync(ct);

        return Result<CostBudgetDto>.Success(budget.ToDto());
    }
}
```

### 3. Queries (Read Operations)

```csharp
// SpaceOS.Modules.Kontrolling.Application/Queries/
GetCostBudgetByIdQuery.cs
GetCostBudgetsByProjectQuery.cs
GetCostBudgetsByStatusQuery.cs      // Filter by FSM state
GetEarnedValueMetricsQuery.cs
GetVarianceAnalysisQuery.cs
GetCostPerformanceIndexQuery.cs     // CPI calculation
GetSchedulePerformanceIndexQuery.cs // SPI calculation
GetEstimateAtCompletionQuery.cs     // EAC calculation
GetBudgetSummaryQuery.cs
```

### 4. Query Handlers

```csharp
// Example: GetCostBudgetByIdQueryHandler.cs
public class GetCostBudgetByIdQueryHandler : IRequestHandler<GetCostBudgetByIdQuery, Result<CostBudgetDto>>
{
    private readonly ICostBudgetRepository _repository;

    public async Task<Result<CostBudgetDto>> Handle(GetCostBudgetByIdQuery request, CancellationToken ct)
    {
        var budget = await _repository.GetByIdAsync(request.BudgetId, ct);
        if (budget == null)
            return Result<CostBudgetDto>.Failure("Cost budget not found");

        return Result<CostBudgetDto>.Success(budget.ToDto());
    }
}
```

**Complex Query Example:**
```csharp
// GetEarnedValueMetricsQueryHandler.cs
public class GetEarnedValueMetricsQueryHandler : IRequestHandler<GetEarnedValueMetricsQuery, Result<EVMDto>>
{
    private readonly IEarnedValueRepository _repository;

    public async Task<Result<EVMDto>> Handle(GetEarnedValueMetricsQuery request, CancellationToken ct)
    {
        var ev = await _repository.GetByProjectIdAsync(request.ProjectId, ct);
        if (ev == null)
            return Result<EVMDto>.Failure("Earned value data not found");

        var metrics = new EVMDto
        {
            PlannedValue = ev.PlannedValue,
            EarnedValue = ev.EarnedValue,
            ActualCost = ev.ActualCost,
            CostVariance = ev.EarnedValue - ev.ActualCost,    // CV = EV - AC
            ScheduleVariance = ev.EarnedValue - ev.PlannedValue, // SV = EV - PV
            CPI = ev.ActualCost > 0 ? ev.EarnedValue / ev.ActualCost : 0,
            SPI = ev.PlannedValue > 0 ? ev.EarnedValue / ev.PlannedValue : 0,
            EAC = ev.CalculateEstimateAtCompletion()
        };

        return Result<EVMDto>.Success(metrics);
    }
}
```

### 5. DTOs (Data Transfer Objects)

```csharp
// SpaceOS.Modules.Kontrolling.Application/DTOs/
CostBudgetDto.cs
EarnedValueDto.cs
EVMDto.cs                      // Earned Value Metrics
VarianceDto.cs
BudgetSummaryDto.cs
CreateCostBudgetDto.cs
UpdateCostBudgetDto.cs
// ... etc
```

**Example DTO:**
```csharp
public record EVMDto
{
    public decimal PlannedValue { get; init; }
    public decimal EarnedValue { get; init; }
    public decimal ActualCost { get; init; }
    public decimal CostVariance { get; init; }      // CV = EV - AC
    public decimal ScheduleVariance { get; init; }  // SV = EV - PV
    public decimal CPI { get; init; }               // Cost Performance Index
    public decimal SPI { get; init; }               // Schedule Performance Index
    public decimal EAC { get; init; }               // Estimate at Completion
    public string PerformanceStatus { get; init; }  // "On Track", "Over Budget", "Behind Schedule"
}
```

### 6. Validators (FluentValidation)

```csharp
// SpaceOS.Modules.Kontrolling.Application/Validators/
CreateCostBudgetCommandValidator.cs
UpdateCostBudgetCommandValidator.cs
RecordActualCostCommandValidator.cs
// ... etc
```

**Example Validator:**
```csharp
public class CreateCostBudgetCommandValidator : AbstractValidator<CreateCostBudgetCommand>
{
    public CreateCostBudgetCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.BudgetedCost).GreaterThan(0).WithMessage("Budget must be positive");
        RuleFor(x => x.PlannedDuration).GreaterThan(0).WithMessage("Duration must be positive");
        RuleFor(x => x.StartDate).NotEmpty().GreaterThanOrEqualTo(DateTime.UtcNow.Date);
    }
}
```

### 7. Application Service Contracts

```csharp
// SpaceOS.Modules.Kontrolling.Application/Contracts/
ICostBudgetService.cs
IEarnedValueService.cs
IVarianceAnalysisService.cs
IKPICalculationService.cs       // CPI, SPI, EAC calculations
```

### 8. MediatR Registration

```csharp
// SpaceOS.Modules.Kontrolling.Application/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddKontrollingApplication(this IServiceCollection services)
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
Domain Repository + Domain Service (KPI calculations)
    ↓
Domain Aggregate (CostBudget/EarnedValue/Variance)
    ↓
EF Core DbContext
```

## Quality Gates

1. **Build:** 0 errors, 0 warnings
2. **Tests:** Write unit tests for:
   - All command handlers (happy path + validation failures)
   - All query handlers (found + not found)
   - EVM calculation handlers (CPI, SPI, EAC formulas)
   - All validators (valid + invalid inputs)
3. **FSM Integration:** Budget approval workflow state transitions
4. **Validation:** FluentValidation for all commands
5. **Error Handling:** Return `Result<T>` pattern
6. **KPI Accuracy:** Verify EVM formulas (CV, SV, CPI, SPI, EAC)

## Dependencies

**NuGet Packages (already in Week 1):**
- MediatR (12.x)
- FluentValidation (11.x)
- Microsoft.EntityFrameworkCore (8.x)

## File Structure

```
SpaceOS.Modules.Kontrolling.Application/
├── Commands/
│   ├── CreateCostBudgetCommand.cs
│   ├── SubmitForApprovalCommand.cs
│   ├── RecordActualCostCommand.cs
│   └── ...
├── CommandHandlers/
│   ├── CreateCostBudgetCommandHandler.cs
│   ├── SubmitForApprovalCommandHandler.cs
│   └── ...
├── Queries/
│   ├── GetCostBudgetByIdQuery.cs
│   ├── GetEarnedValueMetricsQuery.cs
│   └── ...
├── QueryHandlers/
│   ├── GetCostBudgetByIdQueryHandler.cs
│   ├── GetEarnedValueMetricsQueryHandler.cs
│   └── ...
├── DTOs/
│   ├── CostBudgetDto.cs
│   ├── EVMDto.cs
│   └── ...
├── Validators/
│   ├── CreateCostBudgetCommandValidator.cs
│   └── ...
├── Contracts/
│   ├── ICostBudgetService.cs
│   ├── IKPICalculationService.cs
│   └── ...
└── DependencyInjection.cs
```

## Estimated Effort

**60 NWT (~2 hours)**

Breakdown:
- Commands + Handlers: 20 NWT (40 min)
- Queries + Handlers (including EVM): 20 NWT (40 min)
- DTOs: 8 NWT (16 min)
- Validators: 8 NWT (16 min)
- Tests (including KPI formulas): 4 NWT (8 min)

## Acceptance Criteria

- [ ] All commands implemented with handlers
- [ ] All queries implemented with handlers
- [ ] DTOs for all aggregates
- [ ] EVM calculation queries (CPI, SPI, EAC) working correctly
- [ ] FluentValidation for all commands
- [ ] MediatR registration configured
- [ ] Unit tests: >90% coverage
- [ ] Build: 0 errors, 0 warnings
- [ ] FSM budget approval workflow respected
- [ ] DONE outbox with build logs and KPI verification

## References

- Week 1 Domain: Domain layer completed
- NuGet Fix: `MSG-BACKEND-122-DONE`
- Architecture: `docs/knowledge/architecture/ADR-055-joinerytech-kontrolling-domain-model.md`
- EVM Formulas: https://www.pmi.org/learning/library/earned-value-management-evm-6479

---

**Priority:** HIGH — Week 2 Application Layer is critical path for JoineryTech Phase 1
**Blocker Status:** ✅ UNBLOCKED (NuGet resolved)

🤖 Generated by Conductor

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
