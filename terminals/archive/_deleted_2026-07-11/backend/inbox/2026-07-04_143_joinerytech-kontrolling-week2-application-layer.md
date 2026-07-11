---
id: MSG-BACKEND-143
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
epic_id: EPIC-JT-CTRL
ref: MSG-BACKEND-142-DONE
created: 2026-07-04
estimated_nwt: 180
content_hash: 24fe1490c2e312bc859c5a8943994409054f7584e51000e67a86443ddbc3a31f
---

# JoineryTech Kontrolling — Week 2 Application Layer Implementation

**Epic:** EPIC-JT-CTRL (Kontrolling Modul)
**Priority:** HIGH (Week 1 domain layer DONE, continue momentum)
**Estimated:** 180 NWT (~6 hours)
**Dependency Status:** ✅ UNBLOCKED (Week 1 domain layer complete)

---

## Context

Kontrolling Week 1 Domain Layer **DONE** (MSG-BACKEND-141-DONE):
- ✅ 57 unit tests — Mind zöld ✅
- ✅ Build: 0 warnings, 0 errors
- ✅ Domain entities: Money, CategoryCost, Revenue, Margin, ProjectCostCalculation, CostAdjustment
- ✅ EAC calculation formula: `projected[category] = MAX(planned, actual)`
- ✅ 3 overhead allocation methods

**Week 2 Focus:** Application layer (CQRS handlers, DTOs, validators, calculation engine service)

---

## Task: Week 2 — Application Layer Implementation

### Deliverables

#### 1. CQRS Query Handlers (6 queries)

**Based on OpenAPI spec:** `/opt/spaceos/docs/api/joinerytech-kontrolling-v1.yaml`

```
spaceos-modules-kontrolling/Application/
  Queries/
    GetProjectCostSummary/
      GetProjectCostSummaryQuery.cs
      GetProjectCostSummaryQueryHandler.cs
      CostSummaryDto.cs
    GetEACCalculation/
      GetEACCalculationQuery.cs
      GetEACCalculationQueryHandler.cs
      EACCalculationDto.cs
    GetCostBreakdown/
      GetCostBreakdownQuery.cs
      GetCostBreakdownQueryHandler.cs
      CostBreakdownDto.cs
    GetVarianceAnalysis/
      GetVarianceAnalysisQuery.cs
      GetVarianceAnalysisQueryHandler.cs
      VarianceAnalysisDto.cs
    GetPortfolioSummary/
      GetPortfolioSummaryQuery.cs
      GetPortfolioSummaryQueryHandler.cs
      PortfolioSummaryDto.cs
    GetOverheadConfig/
      GetOverheadConfigQuery.cs
      GetOverheadConfigQueryHandler.cs
      OverheadConfigDto.cs
```

**Query Handler Pattern:**
```csharp
public class GetProjectCostSummaryQueryHandler
    : IRequestHandler<GetProjectCostSummaryQuery, CostSummaryDto>
{
    private readonly IProjectCostCalculationService _calculationService;
    private readonly IMemoryCache _cache;

    public async Task<CostSummaryDto> Handle(
        GetProjectCostSummaryQuery request,
        CancellationToken ct)
    {
        var cacheKey = $"project-cost-{request.ProjectId}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

            var calculation = await _calculationService.CalculateAsync(
                request.ProjectId,
                request.TenantId,
                ct);

            return MapToDto(calculation);
        });
    }
}
```

#### 2. CQRS Command Handlers (4 commands)

```
spaceos-modules-kontrolling/Application/
  Commands/
    SetOverheadConfig/
      SetOverheadConfigCommand.cs
      SetOverheadConfigCommandHandler.cs
      SetOverheadConfigCommandValidator.cs
    UpdateOverheadConfig/
      UpdateOverheadConfigCommand.cs
      UpdateOverheadConfigCommandHandler.cs
    CreateCostAdjustment/
      CreateCostAdjustmentCommand.cs
      CreateCostAdjustmentCommandHandler.cs
      CreateCostAdjustmentCommandValidator.cs
    DeleteCostAdjustment/
      DeleteCostAdjustmentCommand.cs
      DeleteCostAdjustmentCommandHandler.cs
```

**Command Handler Pattern:**
```csharp
public class CreateCostAdjustmentCommandHandler
    : IRequestHandler<CreateCostAdjustmentCommand, Guid>
{
    private readonly ICostAdjustmentRepository _repository;
    private readonly IEventPublisher _eventPublisher;

    public async Task<Guid> Handle(
        CreateCostAdjustmentCommand request,
        CancellationToken ct)
    {
        var adjustment = CostAdjustment.Create(
            request.TenantId,
            request.ProjectId,
            request.Category,
            Money.FromHUF(request.Amount),
            request.Reason,
            request.EffectiveDate,
            request.Scope,
            request.CreatedByUserId);

        await _repository.AddAsync(adjustment, ct);
        await _eventPublisher.PublishAsync(adjustment.DomainEvents, ct);

        return adjustment.Id.Value;
    }
}
```

#### 3. DTOs (Response Models)

```csharp
// CostSummaryDto.cs
public record CostSummaryDto(
    Guid ProjectId,
    MoneyDto Revenue,
    CostsDto Costs,
    MarginsDto Margins,
    DateTime CalculatedAt
);

public record MoneyDto(decimal Amount, string Currency);

public record CostsDto(
    MoneyDto Planned,
    MoneyDto Actual,
    MoneyDto Eac,
    MoneyDto Variance,
    decimal VariancePercentage
);

public record MarginsDto(
    MarginDto PlannedMargin,
    MarginDto ActualMargin,
    MarginDto EacMargin
);

public record MarginDto(MoneyDto Amount, decimal Percentage);

// EACCalculationDto.cs
public record EACCalculationDto(
    Guid ProjectId,
    Dictionary<CostCategory, CategoryCostDto> CostByCategory,
    MoneyDto TotalEac,
    MoneyDto Overhead,
    OverheadAllocationMethod OverheadMethod,
    DateTime CalculatedAt
);

public record CategoryCostDto(
    MoneyDto Planned,
    MoneyDto Actual,
    MoneyDto Projected,
    MoneyDto Variance
);

// VarianceAnalysisDto.cs
public record VarianceAnalysisDto(
    Guid ProjectId,
    Dictionary<CostCategory, VarianceDetailDto> Variances,
    MoneyDto TotalVariance,
    decimal VariancePercentage,
    CostCategory? WorstPerformingCategory,
    DateTime AnalyzedAt
);

public record VarianceDetailDto(
    MoneyDto Planned,
    MoneyDto Actual,
    MoneyDto Variance,
    decimal VariancePercentage
);

// PortfolioSummaryDto.cs
public record PortfolioSummaryDto(
    int ProjectCount,
    MoneyDto TotalRevenue,
    MoneyDto TotalEac,
    MarginDto AggregatedMargin,
    List<ProjectSummaryDto> TopPerformingProjects,
    List<ProjectSummaryDto> WorstPerformingProjects,
    List<VarianceWarningDto> TopVariances,
    DateTime CalculatedAt
);

public record ProjectSummaryDto(
    Guid ProjectId,
    string ProjectName,
    MoneyDto Revenue,
    MoneyDto Eac,
    MarginDto Margin
);

public record VarianceWarningDto(
    Guid ProjectId,
    string ProjectName,
    CostCategory Category,
    MoneyDto Variance,
    decimal VariancePercentage
);
```

#### 4. FluentValidation Validators

```csharp
// SetOverheadConfigCommandValidator.cs
public class SetOverheadConfigCommandValidator
    : AbstractValidator<SetOverheadConfigCommand>
{
    public SetOverheadConfigCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("TenantId is required");

        RuleFor(x => x.OverheadRate)
            .GreaterThan(0)
            .LessThanOrEqualTo(1)
            .WithMessage("Overhead rate must be between 0 and 1 (0-100%)");

        RuleFor(x => x.Method)
            .IsInEnum()
            .WithMessage("Invalid overhead allocation method");
    }
}

// CreateCostAdjustmentCommandValidator.cs
public class CreateCostAdjustmentCommandValidator
    : AbstractValidator<CreateCostAdjustmentCommand>
{
    public CreateCostAdjustmentCommandValidator()
    {
        RuleFor(x => x.Amount)
            .NotEqual(0)
            .WithMessage("Adjustment amount cannot be zero");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MinimumLength(10)
            .WithMessage("Reason must be at least 10 characters");

        RuleFor(x => x.Category)
            .IsInEnum()
            .WithMessage("Invalid cost category");

        When(x => x.Scope == AdjustmentScope.Project, () =>
        {
            RuleFor(x => x.ProjectId)
                .NotNull()
                .WithMessage("ProjectId is required when scope is Project");
        });

        When(x => x.Scope == AdjustmentScope.Portfolio, () =>
        {
            RuleFor(x => x.ProjectId)
                .Null()
                .WithMessage("ProjectId must be null when scope is Portfolio");
        });
    }
}
```

#### 5. Domain Service: IProjectCostCalculationService

```csharp
public interface IProjectCostCalculationService
{
    Task<ProjectCostCalculation> CalculateAsync(
        Guid projectId,
        Guid tenantId,
        CancellationToken ct = default);
}

public class ProjectCostCalculationService : IProjectCostCalculationService
{
    private readonly IIntegrationDataProvider _integrationProvider;
    private readonly IOverheadConfigRepository _overheadConfigRepo;
    private readonly ICostAdjustmentRepository _adjustmentRepo;

    public async Task<ProjectCostCalculation> CalculateAsync(
        Guid projectId,
        Guid tenantId,
        CancellationToken ct = default)
    {
        // 1. Fetch integration data (Production, HR, Finance, Warehouse, Logistics)
        var integrationData = await _integrationProvider.GetProjectDataAsync(
            projectId, tenantId, ct);

        // 2. Fetch overhead config
        var overheadConfig = await _overheadConfigRepo.GetByTenantAsync(
            tenantId, ct);

        // 3. Fetch cost adjustments
        var adjustments = await _adjustmentRepo.GetByProjectAsync(
            projectId, tenantId, ct);

        // 4. Calculate costs using domain model
        var calculation = ProjectCostCalculation.Calculate(
            projectId,
            integrationData.Revenue,
            integrationData.MfgPrepData,
            integrationData.TimeLogs,
            integrationData.WarehouseReceipts,
            integrationData.Shipments,
            integrationData.SupplierInvoices,
            overheadConfig,
            adjustments);

        return calculation;
    }
}
```

#### 6. Integration Data Provider

```csharp
public interface IIntegrationDataProvider
{
    Task<ProjectIntegrationData> GetProjectDataAsync(
        Guid projectId,
        Guid tenantId,
        CancellationToken ct = default);
}

public record ProjectIntegrationData(
    Revenue Revenue,
    MfgPrepCostData MfgPrepData,
    IEnumerable<TimeLogCostData> TimeLogs,
    IEnumerable<WarehouseReceiptData> WarehouseReceipts,
    IEnumerable<ShipmentCostData> Shipments,
    IEnumerable<InboundInvoiceData> SupplierInvoices
);

// Integration DTOs (readonly, from other modules)
public record MfgPrepCostData(
    Guid ProjectId,
    Money MaterialCost,
    Money LaborCost,
    decimal EstimatedLaborHours
);

public record TimeLogCostData(
    Guid ProjectId,
    Guid EmployeeId,
    decimal HoursWorked,
    decimal HourlyRate,
    Money CostTotal
);

public record InboundInvoiceData(
    Guid InvoiceId,
    Guid ProjectId,
    Guid SupplierId,
    Money Amount,
    DateTime InvoiceDate
);

public record WarehouseReceiptData(
    Guid ReceiptId,
    Guid ProjectId,
    Guid MaterialId,
    decimal Quantity,
    Money UnitCost,
    Money TotalCost
);

public record ShipmentCostData(
    Guid ShipmentId,
    Guid ProjectId,
    Money EstimatedCost,
    Money? ActualCost
);
```

---

## Technical Constraints

### 1. ADR-055 Compliance

**Calculation-First Architecture:**
- ✅ `ProjectCostCalculation` is calculated on-demand, NOT stored
- ✅ Integration data is read-only (direct DB queries via EF Core)
- ✅ Only `CostAdjustment` and `OverheadConfig` are stored
- ✅ 5-minute cache for project costs, 10-minute cache for portfolio

### 2. CQRS Pattern (MediatR)

```csharp
// Install packages:
dotnet add package MediatR
dotnet add package MediatR.Extensions.Microsoft.DependencyInjection
dotnet add package FluentValidation
dotnet add package FluentValidation.DependencyInjection
```

**DI Registration:**
```csharp
services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyMarker).Assembly));
services.AddValidatorsFromAssembly(typeof(ApplicationAssemblyMarker).Assembly);
```

### 3. Caching Strategy

**In-Memory Cache:**
```csharp
services.AddMemoryCache();
```

**Cache Keys:**
- Project cost summary: `project-cost-{projectId}` (5 minutes TTL)
- EAC calculation: `project-eac-{projectId}` (5 minutes TTL)
- Portfolio summary: `portfolio-summary-{tenantId}` (10 minutes TTL)
- Overhead config: `overhead-config-{tenantId}` (1 hour TTL)

**Cache Invalidation:**
- Manual adjustment created/deleted → invalidate project cache
- Overhead config updated → invalidate all tenant caches

### 4. Integration Pattern (Direct DB Queries)

**Recommended:** Direct DB queries (RLS-aware, read-only replica)

```csharp
public class IntegrationDataProvider : IIntegrationDataProvider
{
    private readonly SpaceOsDbContext _context;

    public async Task<ProjectIntegrationData> GetProjectDataAsync(
        Guid projectId,
        Guid tenantId,
        CancellationToken ct = default)
    {
        // Query Production module (MfgPrep)
        var mfgPrepData = await _context.Set<MfgPrepCostEstimate>()
            .Where(x => x.ProjectId == projectId && x.TenantId == tenantId)
            .Select(x => new MfgPrepCostData(
                x.ProjectId,
                new Money(x.MaterialCost, "HUF"),
                new Money(x.LaborCost, "HUF"),
                x.EstimatedLaborHours))
            .FirstOrDefaultAsync(ct);

        // Query HR module (TimeLogs)
        var timeLogs = await _context.Set<TimeLog>()
            .Where(x => x.ProjectId == projectId && x.TenantId == tenantId)
            .Select(x => new TimeLogCostData(
                x.ProjectId,
                x.EmployeeId,
                x.Hours,
                x.HourlyRate,
                new Money(x.Hours * x.HourlyRate, "HUF")))
            .ToListAsync(ct);

        // ... similar queries for other modules

        return new ProjectIntegrationData(
            revenue,
            mfgPrepData,
            timeLogs,
            warehouseReceipts,
            shipments,
            supplierInvoices);
    }
}
```

---

## Acceptance Criteria

**DONE when:**
- [ ] 6 query handlers implemented (GetProjectCostSummary, GetEACCalculation, GetCostBreakdown, GetVarianceAnalysis, GetPortfolioSummary, GetOverheadConfig)
- [ ] 4 command handlers implemented (SetOverheadConfig, UpdateOverheadConfig, CreateCostAdjustment, DeleteCostAdjustment)
- [ ] All DTOs defined (CostSummaryDto, EACCalculationDto, VarianceAnalysisDto, PortfolioSummaryDto, etc.)
- [ ] FluentValidation validators implemented (SetOverheadConfigCommandValidator, CreateCostAdjustmentCommandValidator)
- [ ] IProjectCostCalculationService implemented
- [ ] IIntegrationDataProvider implemented (direct DB queries)
- [ ] In-memory cache configured (5 min project, 10 min portfolio)
- [ ] Unit tests: CQRS handlers (15+ test cases)
- [ ] Unit tests: Validators (10+ test cases)
- [ ] Unit tests: Calculation service (8+ test cases)
- [ ] Build succeeds: 0 errors, 0 warnings
- [ ] Code coverage: >80% (application logic)

---

## Testing Requirements

### Unit Test Cases (Application Logic)

**Query Handlers:**
1. GetProjectCostSummary — cache hit/miss scenarios
2. GetEACCalculation — category breakdown accuracy
3. GetVarianceAnalysis — variance percentage calculation
4. GetPortfolioSummary — top/worst performing projects sorting

**Command Handlers:**
1. CreateCostAdjustment — domain event published
2. DeleteCostAdjustment — soft delete implemented
3. SetOverheadConfig — duplicate prevention
4. UpdateOverheadConfig — cache invalidation

**Validators:**
1. Overhead rate boundaries (0-1 range)
2. Adjustment reason length (min 10 chars)
3. Scope validation (Project scope requires ProjectId)
4. Amount validation (non-zero)

**Calculation Service:**
1. Integration data aggregation
2. Cache invalidation on adjustment
3. Overhead config fallback (default 15%)
4. Multi-tenant data isolation (RLS)

---

## Integration Points (Week 3-4)

**NOT in Week 2 scope:**
- EF Core configuration (Week 3)
- PostgreSQL database schema (Week 3)
- REST API controllers (Week 4)
- OpenAPI Swagger documentation (Week 4)

**Week 2 Output:**
- Pure application logic (CQRS handlers, DTOs, validators)
- Testable in isolation (unit tests with mocked repositories)
- Ready for Infrastructure layer integration (Week 3)

---

## Files to Create (Week 2)

```
spaceos-modules-kontrolling/Application/
  Queries/
    GetProjectCostSummary/ (3 files: Query, Handler, Dto)
    GetEACCalculation/ (3 files)
    GetCostBreakdown/ (3 files)
    GetVarianceAnalysis/ (3 files)
    GetPortfolioSummary/ (3 files)
    GetOverheadConfig/ (3 files)
  Commands/
    SetOverheadConfig/ (3 files: Command, Handler, Validator)
    UpdateOverheadConfig/ (2 files: Command, Handler)
    CreateCostAdjustment/ (3 files)
    DeleteCostAdjustment/ (2 files)
  Services/
    ProjectCostCalculationService.cs
    IntegrationDataProvider.cs
  DTOs/
    CostSummaryDto.cs
    EACCalculationDto.cs
    VarianceAnalysisDto.cs
    PortfolioSummaryDto.cs
    OverheadConfigDto.cs
    MoneyDto.cs
  Application.Tests/
    QueryHandlerTests.cs (6 test classes)
    CommandHandlerTests.cs (4 test classes)
    ValidatorTests.cs (2 test classes)
    CalculationServiceTests.cs
```

**Total:** ~1,500-1,800 lines of application code + tests

---

## References

- **OpenAPI Spec:** `/opt/spaceos/docs/api/joinerytech-kontrolling-v1.yaml`
- **Domain Model:** Week 1 domain layer (MSG-BACKEND-141-DONE)
- **Week 1 Tests:** 57 unit tests ✅ (Money, CategoryCost, ProjectCostCalculation, etc.)
- **ADR-055:** JoineryTech Kontrolling Domain Model
- **MediatR CQRS:** https://github.com/jbogard/MediatR
- **FluentValidation:** https://docs.fluentvalidation.net/

---

## Priority Rationale

**Why HIGH priority:**
- ✅ Week 1 domain layer DONE (momentum maintained)
- ✅ OpenAPI spec locked (contract-first workflow)
- ✅ No blockers (domain model complete, tests green)
- ✅ Application layer critical for Week 3 Infrastructure integration
- ✅ CQRS pattern reusable for other JoineryTech modules (HR, Maintenance, QA)

**Timeline:**
- Week 2 (6 hours) → Week 3 Infrastructure (4 hours) → Week 4 API (2 hours) → Week 5 Testing (2 hours)

---

**Next After Completion:**
Week 3 — Infrastructure layer (EF Core configurations, PostgreSQL schema, RLS policies, integration queries)

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
