---
id: MSG-BACKEND-458
from: conductor
to: backend
type: task
priority: high
status: DONE
model: sonnet
ref: MSG-BACKEND-457
epic_id: EPIC-JT-HR
checkpoint_id: CP-EHS-HR-INTEGRATION
created: 2026-07-11
estimated_nwt: 30
content_hash: 2421c9fdf6f4d8156258bc7c4ce9cbcc7db195ec99ce15a0de065153cf473635
---

# EHS→HR Integration Event Handlers — Cross-Module Integration

**Predecessor:** MSG-BACKEND-457 (HR Employee Domain - DONE)
**Context:** CP-EHS-HR-INTEGRATION checkpoint completion
**Priority:** HIGH (completes integration checkpoint)

---

## Background

MSG-BACKEND-457 successfully implemented the HR Employee Domain infrastructure:
- ✅ Employee aggregate + EmployeeCompetency owned entity
- ✅ EmployeeRepository + EF Core configuration
- ✅ Database migration (hr.employees, hr.employee_competencies)
- ✅ 4 integration tests PASSING

**Foundation Already Exists (Salvaged from MSG-452):**
- ✅ `TrainingCompletedEvent.cs` (Application/Contracts)
- ✅ `TrainingCompletedEventHandler.cs` (Application/EventHandlers)
- ✅ `IEmployeeRepository.cs` interface

**Still Missing (This Task):**
- ❌ Event registration in DI container
- ❌ Integration tests (Event → Employee.CompetencyMatrix update)
- ❌ E2E test (EHS training complete → HR competency added)

---

## Scope (30 NWT)

### 1. Verify/Fix TrainingCompletedEventHandler (5 NWT)
**File:** `spaceos-modules-hr/src/Application/EventHandlers/TrainingCompletedEventHandler.cs`

**Requirements:**
```csharp
public class TrainingCompletedEventHandler : INotificationHandler<TrainingCompletedEvent>
{
    private readonly IEmployeeRepository _employeeRepository;

    public async Task Handle(TrainingCompletedEvent notification, CancellationToken ct)
    {
        // 1. Fetch Employee by ID (notification.EmployeeId)
        var employee = await _employeeRepository.GetByIdAsync(notification.EmployeeId, ct);
        if (employee == null) throw new NotFoundException($"Employee {notification.EmployeeId} not found");

        // 2. Add competency to Employee aggregate
        employee.AddCompetency(
            notification.CompetencyCode,
            notification.Level,
            notification.AcquiredDate,
            notification.ExpiryDate
        );

        // 3. Persist via repository
        await _employeeRepository.SaveAsync(employee, ct);
    }
}
```

### 2. Event Registration in DI (5 NWT)
**File:** `spaceos-modules-hr/src/Infrastructure/DependencyInjection.cs`

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddHrModule(this IServiceCollection services)
    {
        // DbContext
        services.AddDbContext<HrDbContext>();

        // Repositories
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();

        // MediatR event handlers (auto-registration)
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(TrainingCompletedEventHandler).Assembly);
        });

        return services;
    }
}
```

### 3. Integration Test: Event → Competency (10 NWT)
**File:** `spaceos-modules-hr/tests/Integration/TrainingCompletedEventHandler_Tests.cs`

```csharp
public class TrainingCompletedEventHandler_Tests : IClassFixture<HrTestContainerFixture>
{
    private readonly HrDbContext _context;
    private readonly IEmployeeRepository _repository;
    private readonly IMediator _mediator;

    [Fact]
    public async Task Handle_ValidEvent_AddsCompetencyToEmployee()
    {
        // Arrange
        var employee = new Employee(Guid.NewGuid(), "John Doe", "Welder", DateTime.UtcNow);
        await _repository.SaveAsync(employee, CancellationToken.None);

        var eventData = new TrainingCompletedEvent
        {
            EmployeeId = employee.Id,
            CompetencyCode = "WELDING_CERT",
            Level = 3,
            AcquiredDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddYears(3)
        };

        // Act
        await _mediator.Publish(eventData, CancellationToken.None);

        // Assert
        var updated = await _repository.GetByIdAsync(employee.Id, CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Single(updated.CompetencyMatrix);
        Assert.Equal("WELDING_CERT", updated.CompetencyMatrix.First().CompetencyCode);
        Assert.Equal(3, updated.CompetencyMatrix.First().Level);
    }

    [Fact]
    public async Task Handle_EmployeeNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var eventData = new TrainingCompletedEvent
        {
            EmployeeId = Guid.NewGuid(), // Non-existent
            CompetencyCode = "SAFETY_CERT",
            Level = 5,
            AcquiredDate = DateTime.UtcNow
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(async () =>
        {
            await _mediator.Publish(eventData, CancellationToken.None);
        });
    }

    [Fact]
    public async Task Handle_DuplicateCompetency_UpdatesExisting()
    {
        // Arrange
        var employee = new Employee(Guid.NewGuid(), "Jane Smith", "Technician", DateTime.UtcNow);
        employee.AddCompetency("FORKLIFT_CERT", 2, DateTime.UtcNow, null);
        await _repository.SaveAsync(employee, CancellationToken.None);

        var eventData = new TrainingCompletedEvent
        {
            EmployeeId = employee.Id,
            CompetencyCode = "FORKLIFT_CERT",
            Level = 4, // Upgrade from 2 to 4
            AcquiredDate = DateTime.UtcNow.AddMonths(6)
        };

        // Act
        await _mediator.Publish(eventData, CancellationToken.None);

        // Assert
        var updated = await _repository.GetByIdAsync(employee.Id, CancellationToken.None);
        Assert.Single(updated.CompetencyMatrix);
        Assert.Equal(4, updated.CompetencyMatrix.First().Level); // Upgraded
    }
}
```

### 4. E2E Test: EHS → HR Flow (10 NWT)
**File:** `spaceos-modules-hr/tests/E2E/EhsHrIntegration_E2E_Tests.cs`

```csharp
public class EhsHrIntegration_E2E_Tests : IClassFixture<IntegrationTestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly IServiceProvider _serviceProvider;

    [Fact]
    public async Task TrainingCompletion_FullFlow_UpdatesEmployeeCompetency()
    {
        // Arrange: Create Employee in HR module
        var employeeId = Guid.NewGuid();
        var hrRepository = _serviceProvider.GetRequiredService<IEmployeeRepository>();
        var employee = new Employee(employeeId, "Bob Wilson", "Operator", DateTime.UtcNow);
        await hrRepository.SaveAsync(employee, CancellationToken.None);

        // Act: Simulate EHS training completion event (via MediatR)
        var mediator = _serviceProvider.GetRequiredService<IMediator>();
        var trainingEvent = new TrainingCompletedEvent
        {
            EmployeeId = employeeId,
            CompetencyCode = "SAFETY_TRAINING_2026",
            Level = 5,
            AcquiredDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddYears(1)
        };

        await mediator.Publish(trainingEvent, CancellationToken.None);

        // Assert: Verify competency added to Employee
        var updated = await hrRepository.GetByIdAsync(employeeId, CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Single(updated.CompetencyMatrix);

        var competency = updated.CompetencyMatrix.First();
        Assert.Equal("SAFETY_TRAINING_2026", competency.CompetencyCode);
        Assert.Equal(5, competency.Level);
        Assert.True((competency.ExpiryDate - DateTime.UtcNow).Value.TotalDays > 360);
    }

    [Fact]
    public async Task TrainingCompletion_MultipleEvents_AccumulatesCompetencies()
    {
        // Arrange
        var employeeId = Guid.NewGuid();
        var hrRepository = _serviceProvider.GetRequiredService<IEmployeeRepository>();
        var employee = new Employee(employeeId, "Alice Brown", "Supervisor", DateTime.UtcNow);
        await hrRepository.SaveAsync(employee, CancellationToken.None);

        var mediator = _serviceProvider.GetRequiredService<IMediator>();

        // Act: Simulate 3 training completions
        var trainings = new[]
        {
            new TrainingCompletedEvent
            {
                EmployeeId = employeeId,
                CompetencyCode = "FIRST_AID",
                Level = 3,
                AcquiredDate = DateTime.UtcNow
            },
            new TrainingCompletedEvent
            {
                EmployeeId = employeeId,
                CompetencyCode = "FIRE_SAFETY",
                Level = 4,
                AcquiredDate = DateTime.UtcNow.AddDays(1)
            },
            new TrainingCompletedEvent
            {
                EmployeeId = employeeId,
                CompetencyCode = "HAZMAT_HANDLING",
                Level = 5,
                AcquiredDate = DateTime.UtcNow.AddDays(2)
            }
        };

        foreach (var training in trainings)
        {
            await mediator.Publish(training, CancellationToken.None);
        }

        // Assert
        var updated = await hrRepository.GetByIdAsync(employeeId, CancellationToken.None);
        Assert.Equal(3, updated.CompetencyMatrix.Count());
        Assert.Contains(updated.CompetencyMatrix, c => c.CompetencyCode == "FIRST_AID");
        Assert.Contains(updated.CompetencyMatrix, c => c.CompetencyCode == "FIRE_SAFETY");
        Assert.Contains(updated.CompetencyMatrix, c => c.CompetencyCode == "HAZMAT_HANDLING");
    }
}
```

---

## Acceptance Criteria

- [ ] TrainingCompletedEventHandler verified/fixed (fetches Employee, adds competency, persists)
- [ ] DependencyInjection.cs created with MediatR auto-registration
- [ ] Integration tests (3 tests): ValidEvent, EmployeeNotFound, DuplicateCompetency
- [ ] E2E tests (2 tests): FullFlow, MultipleEvents
- [ ] All tests PASS (5 tests total)
- [ ] `dotnet build spaceos-modules-hr/` succeeds (0 errors)

---

## Security Checklist

- [ ] No sensitive data in event payload (competency codes are non-sensitive)
- [ ] Repository layer handles RLS (tenant isolation)
- [ ] NotFoundException thrown for invalid EmployeeId (prevents enumeration)
- [ ] Idempotency handled (duplicate competency code updates level)

---

## Files to Create/Modify

### Create:
1. `src/Infrastructure/DependencyInjection.cs`
2. `tests/Integration/TrainingCompletedEventHandler_Tests.cs`
3. `tests/E2E/EhsHrIntegration_E2E_Tests.cs`

### Modify:
4. `src/Application/EventHandlers/TrainingCompletedEventHandler.cs` (verify/fix)
5. `src/Application/Contracts/TrainingCompletedEvent.cs` (ensure all properties exist)

---

## Dependencies

**Pre-existing (from MSG-457):**
- ✅ Employee aggregate + AddCompetency method
- ✅ EmployeeRepository + SaveAsync
- ✅ HrDbContext + EF Core migration

**External:**
- MediatR 12.0+
- Testcontainers.PostgreSQL
- xUnit v3 + FluentAssertions

---

## Build & Test Commands

```bash
cd /opt/spaceos/backend/spaceos-modules/spaceos-modules-hr

# Build
dotnet build src/SpaceOS.Modules.HR.csproj

# Run integration tests
dotnet test tests/SpaceOS.Modules.HR.Tests.csproj --filter TrainingCompletedEventHandler

# Run E2E tests
dotnet test tests/SpaceOS.Modules.HR.Tests.csproj --filter EhsHrIntegration_E2E

# Run all tests
dotnet test tests/SpaceOS.Modules.HR.Tests.csproj
```

---

## Next Steps After Completion

1. **CP-EHS-HR-INTEGRATION checkpoint update → DONE** in EPICS.yaml
   - Update line 523: `status: pending` → `status: done`

2. **Milestone report to Monitor**
   - 3/4 checkpoints complete
   - CP-DMS-SALES-INTEGRATION ready to start

3. **CP-DMS-SALES-INTEGRATION planning**
   - DMS → Sales document linking
   - Next integration checkpoint

---

**Estimated Timeline:** 30 NWT (~1 hour)
**Priority:** HIGH (completes CP-EHS-HR-INTEGRATION)
**Complexity:** MEDIUM (event handler + cross-module integration tests)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
