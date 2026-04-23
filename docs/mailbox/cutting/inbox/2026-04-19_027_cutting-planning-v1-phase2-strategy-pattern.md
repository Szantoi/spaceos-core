---
id: MSG-CUTTING-027
from: root
to: cutting
type: task
priority: high
status: READ
ref: MSG-CUTTING-026-DONE
created: 2026-04-19
---

# CUTTING-027 — CUTTING Planning v1 Phase 2: Strategy Pattern + Yield Optimization

## Context

**Phase 1 DONE:** CuttingPlan data model + 5 HTTP endpoints ✅ (136 tests, RLS live)

**Phase 2 Scope:** 4 planning strategies + yield calculation + strategy selection in API

**Timeline:** 3 days, ~18 developer-hours  
**Doorstar context:** MaxCut competitive feature — yield optimization (91%+ benchmark)

---

## Architecture: Strategy Pattern

**Interface:** `IPlanningStrategy` (new file)

```csharp
namespace SpaceOS.Modules.Cutting.Application.Strategies;

public interface IPlanningStrategy
{
    /// <summary>
    /// Schedules jobs into daily plan slots based on strategy rules.
    /// Returns: list of jobs scheduled with allocatedCapacity updated
    /// </summary>
    Task<IEnumerable<CuttingJob>> ScheduleJobsAsync(
        IEnumerable<CuttingJob> unscheduledJobs,
        IEnumerable<DailyPlan> dailyPlans,
        CancellationToken ct);

    /// <summary>
    /// Calculates material yield % for a complete plan
    /// (scheduled jobs / available material)
    /// </summary>
    decimal CalculateYield(CuttingPlan plan, IEnumerable<DailyPlan> dailyPlans);

    /// <summary>
    /// Returns strategy display name (for UI dropdown)
    /// </summary>
    string GetLabel();

    /// <summary>
    /// Validates plan state before execution
    /// Throws ValidationException if invalid
    /// </summary>
    Task<ValidationResult> ValidateAsync(CuttingPlan plan, CancellationToken ct);
}
```

---

## Strategy Implementations (4 variants)

### 1. **MaxCutStrategy** (default)

- **Logic:** Guillotine packing optimization (maximize material usage)
- **Order:** By material width desc → height desc → priority
- **Yield target:** 91%+
- **Use case:** Doorstar standard (minimizes waste)
- **File:** `Application/Strategies/MaxCutStrategy.cs`

```csharp
public class MaxCutStrategy : IPlanningStrategy
{
    public async Task<IEnumerable<CuttingJob>> ScheduleJobsAsync(...)
    {
        // 1. Sort jobs by width desc, height desc, priority
        // 2. For each job, find first DailyPlan slot with capacity >= estimatedTimeHours
        // 3. Allocate; update utilizationPercent
        // 4. Return scheduled list
    }

    public decimal CalculateYield(...)
    {
        // Yield = (sum of material used) / (total available material) * 100
        // For v1: simple hours-based estimate (actual yield requires CAD geometry)
    }

    public string GetLabel() => "MaxCut v1 (Guillotine Optimization)";

    public async Task<ValidationResult> ValidateAsync(...)
    {
        // Check: all jobs have estimatedTimeHours > 0
        // Check: at least 1 day available in plan
        // Return: ValidationResult.Valid or new ValidationResult(errors)
    }
}
```

---

### 2. **FIFOStrategy** (simple)

- **Logic:** First-in-first-out (creation time order)
- **Order:** createdAt asc
- **Yield target:** ~70%
- **Use case:** Emergency/quick scheduling (no optimization)
- **File:** `Application/Strategies/FIFOStrategy.cs`

```csharp
public class FIFOStrategy : IPlanningStrategy
{
    public async Task<IEnumerable<CuttingJob>> ScheduleJobsAsync(...)
    {
        // 1. Sort jobs by createdAt asc
        // 2. Allocate first available slot (first-fit)
        // 3. Return scheduled list
    }

    public string GetLabel() => "FIFO (First-In-First-Out)";
    // ... other methods similar to MaxCutStrategy
}
```

---

### 3. **PriorityStrategy** (deadline-driven)

- **Logic:** By priority (Urgent → High → Normal → Low) + due date
- **Order:** priority desc, dueDate asc
- **Yield target:** ~75%
- **Use case:** Rush jobs + SLA compliance
- **File:** `Application/Strategies/PriorityStrategy.cs`

```csharp
public class PriorityStrategy : IPlanningStrategy
{
    public async Task<IEnumerable<CuttingJob>> ScheduleJobsAsync(...)
    {
        // 1. Sort by priority (Urgent=1, High=2, Normal=3, Low=4) then dueDate asc
        // 2. Allocate first available slot
        // 3. Return scheduled list
    }

    public string GetLabel() => "Priority (By Due Date + Urgency)";
    // ... other methods
}
```

---

### 4. **CustomStrategy** (extension point for v1.5)

- **Logic:** Tenant-specific rules (placeholder for now)
- **Use case:** Future: per-customer custom algorithms
- **File:** `Application/Strategies/CustomStrategy.cs`

```csharp
public class CustomStrategy : IPlanningStrategy
{
    private readonly ITenantCustomRules _rules; // injected for v1.5

    public async Task<IEnumerable<CuttingJob>> ScheduleJobsAsync(...)
    {
        // v1: Fallback to MaxCutStrategy
        // v1.5: Call _rules.GetCustomSchedulingAsync(...)
        return await new MaxCutStrategy().ScheduleJobsAsync(...);
    }

    public string GetLabel() => "Custom (Tenant-Specific)";
}
```

---

## Integration: API & Command Handlers

### 1. **PlanningStrategyFactory** (new)

```csharp
// Application/Strategies/PlanningStrategyFactory.cs

public interface IPlanningStrategyFactory
{
    IPlanningStrategy GetStrategy(string strategyId);
}

public class PlanningStrategyFactory : IPlanningStrategyFactory
{
    public IPlanningStrategy GetStrategy(string strategyId) => strategyId switch
    {
        "maxcut-v1" => new MaxCutStrategy(),
        "fifo" => new FIFOStrategy(),
        "priority" => new PriorityStrategy(),
        "custom" => new CustomStrategy(),
        _ => throw new ArgumentException($"Unknown strategy: {strategyId}")
    };
}
```

**DI Registration:** `Program.cs`
```csharp
services.AddScoped<IPlanningStrategyFactory, PlanningStrategyFactory>();
services.AddScoped<IPlanningStrategy>(sp => 
    sp.GetRequiredService<IPlanningStrategyFactory>()
        .GetStrategy("maxcut-v1")); // default
```

---

### 2. **UpdateCreateCuttingPlanCommand** (modify existing)

**File:** `Application/Commands/CreateCuttingPlan/CreateCuttingPlanCommandHandler.cs`

**Change:** Add strategy execution before returning response

```csharp
public async Task<CreateCuttingPlanResponse> Handle(
    CreateCuttingPlanCommand request,
    CancellationToken ct)
{
    var plan = new CuttingPlan(
        id: Guid.NewGuid(),
        planDate: request.PlanDate,
        planDays: request.PlanDays,
        status: PlanStatus.Draft,
        strategyId: request.StrategyId // NEW: user-selected strategy
    );

    // 1. Generate 14 daily plans
    var dailyPlans = plan.GenerateDailyPlans(plan.PlanDays);

    // 2. Get pending orders (NEW: fetch from Inventory/Joinery)
    var pendingOrders = await _orderRepository.GetPendingOrdersAsync(
        request.TenantId,
        plan.PlanDate,
        plan.PlanDate.AddDays(plan.PlanDays),
        ct);

    // 3. Create cutting jobs from orders (NEW: one job per order)
    var cuttingJobs = pendingOrders
        .Select(o => new CuttingJob(o.Id, o.ScheduledDate, o.Priority, o.EstimatedHours))
        .ToList();

    // 4. Execute strategy (NEW)
    var strategy = _strategyFactory.GetStrategy(request.StrategyId);
    var validationResult = await strategy.ValidateAsync(plan, ct);
    if (!validationResult.IsValid)
        throw new ValidationException(validationResult.Errors);

    var scheduledJobs = await strategy.ScheduleJobsAsync(cuttingJobs, dailyPlans, ct);
    
    // 5. Calculate yield (NEW)
    var yield = strategy.CalculateYield(plan, dailyPlans);

    // 6. Save plan, daily plans, scheduled jobs
    await _repository.AddCuttingPlanAsync(plan, ct);
    // ... cascade-save dailyPlans + cuttingJobs

    return new CreateCuttingPlanResponse(
        plan.Id,
        dailyPlans,
        scheduledJobs.ToList(),
        yield // NEW: include yield in response
    );
}
```

---

### 3. **UpdateGetCuttingPlanResponse** (modify existing)

Add `yield` field:

```csharp
public record GetCuttingPlanResponse(
    Guid PlanId,
    DateTime PlanDate,
    int PlanDays,
    string Status,
    string StrategyId,
    IEnumerable<DailyPlanDto> DailyPlans,
    IEnumerable<CuttingJobDto> Jobs,
    decimal TotalYieldPercent // NEW
);
```

---

## Validation Rules (FluentValidation)

### CreateCuttingPlanCommand Validator (update)

```csharp
public class CreateCuttingPlanCommandValidator : AbstractValidator<CreateCuttingPlanCommand>
{
    public CreateCuttingPlanCommandValidator()
    {
        RuleFor(x => x.PlanDays)
            .InclusiveBetween(7, 90)
            .WithMessage("Plan window must be 7–90 days");

        RuleFor(x => x.StrategyId)
            .Must(s => new[] { "maxcut-v1", "fifo", "priority", "custom" }.Contains(s))
            .WithMessage("Invalid strategy");

        RuleFor(x => x.PlanDate)
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date)
            .WithMessage("Plan date must be >= today");
    }
}
```

---

## Testing Requirements

### Unit Tests (24 tests)

**File:** `Application/Strategies/MaxCutStrategyTests.cs`

- ✅ MaxCutStrategy: 8 tests
  - Sorts jobs by width/height/priority
  - Allocates to first available day
  - Respects capacity limits
  - Handles empty job list
  - Calculates yield correctly
  - Validates plan successfully
  - Rejects invalid plan (no days, no capacity)
  - Respects job priority ordering

- ✅ FIFOStrategy: 4 tests
  - Sorts by createdAt only
  - Lower yield (70% vs 91%)

- ✅ PriorityStrategy: 4 tests
  - Sorts by priority + dueDate
  - Urgent jobs first

- ✅ CustomStrategy: 4 tests
  - Falls back to MaxCut in v1
  - Placeholder for v1.5

- ✅ PlanningStrategyFactory: 4 tests
  - Creates correct strategy instance
  - Throws on unknown strategy
  - Default is maxcut-v1

### Integration Tests (10 tests)

**File:** `Application/Commands/CreateCuttingPlanStrategyTests.cs`

- ✅ Strategy execution in command handler (4 tests)
  - Plan created with strategy
  - Jobs scheduled correctly
  - Yield calculated
  - Response includes yield

- ✅ Yield calculation accuracy (3 tests)
  - MaxCut 91%+
  - FIFO ~70%
  - Priority ~75%

- ✅ Validation in strategy (3 tests)
  - Invalid plan rejected
  - Error message returned
  - Valid plan passes

### API Tests (8 tests)

**File:** `Api/CuttingPlanningStrategyEndpointsTests.cs`

- ✅ POST /api/cutting/planning/ with strategy (4 tests)
  - Request: `{ strategyId: "maxcut-v1", ... }`
  - Response: 201 + { yield }
  - Try all 4 strategies

- ✅ GET /api/cutting/planning/{planId} includes yield (2 tests)
  - Response has `totalYieldPercent`
  - Correct value

- ✅ Error handling (2 tests)
  - 400: Invalid strategyId
  - 409: Plan already scheduled

---

## Deliverables (Phase 2 Complete)

- ✅ 4 strategy implementations (MaxCut, FIFO, Priority, Custom)
- ✅ PlanningStrategyFactory DI integration
- ✅ Yield calculation in command handler
- ✅ Response DTOs updated (yield field)
- ✅ Validation rules enforced
- ✅ 42+ tests passing (unit + integration + API)
- ✅ Postman/curl examples documented (strategy dropdown)
- ✅ Ready for Phase 3 (UI calendar + integration)

---

## Technical Specs

**Location:** `SpaceOS.Modules.Cutting/Application/Strategies/`

**Dependencies:** IRepository, ITenantResolver, IOrderProvider (async)

**Error Handling:**
- 400: Invalid strategyId
- 422: Validation failed (no capacity, no days)
- 409: Plan already executing

**RLS:** Inherited from CuttingPlan aggregate (no new RLS needed)

**Async/Await:** ConfigureAwait(false) on all I/O

---

## Timeline

- **Day 4:** Strategy interfaces + 4 implementations
- **Day 5:** Factory + command handler integration + tests
- **Day 6:** API documentation + E2E validation

Total: 3 days, ~18 developer-hours

---

## Next: Phase 3

After Phase 2 DONE (MSG-CUTTING-027-DONE):
- **Phase 3 inbox message** (UI calendar + Inventory integration)
- **Parallel:** JOINERY Phase 1 (Gyártásilap PDF)
- **Parallel:** INVENTORY Phase 1 (Offcut tracking)

---

**Execute Phase 2 immediately. Report CUTTING-027-DONE when strategy pattern + yield calculation complete and tested.**
