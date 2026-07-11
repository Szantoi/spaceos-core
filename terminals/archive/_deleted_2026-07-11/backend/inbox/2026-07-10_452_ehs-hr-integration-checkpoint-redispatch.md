---
id: MSG-BACKEND-452
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: CP-EHS-HR-INTEGRATION
created: 2026-07-10
estimated_nwt: 45
epic_id: EPIC-JT-EHS
checkpoint_id: CP-EHS-HR-INTEGRATION
note: Re-dispatched from Backend-2 (MSG-BACKEND2-002) due to session timeout
content_hash: 2a2ff306539ee6fa20db9804a65a56006b7751403963a082eb25ba214f66fd8b
---

# CP-EHS-HR-INTEGRATION: EHS → HR Integration (Re-dispatch)

**Epic:** EPIC-JT-EHS (Environment, Health & Safety Module)
**Checkpoint:** CP-EHS-HR-INTEGRATION
**Scope:** Training competencies → Employee records synchronization
**Estimate:** 45 NWT (~1.5 hours)

**Note:** Originally dispatched as MSG-BACKEND2-002 to Backend-2 terminal, but session remained idle. Re-dispatched to Backend terminal for reliability.

---

## 🎯 GOAL

Implement cross-module integration: When an Employee completes EHS training (TrainingCompletedEvent), automatically update the HR Employee.CompetencyMatrix.

**Business Value:** Real-time employee competency tracking for compliance and workforce planning.

---

## ✅ ACCEPTANCE CRITERIA

1. **Event Handler Created**
   - `TrainingCompletedEventHandler` in `HR.Application/EventHandlers/`
   - Subscribes to `TrainingCompletedEvent` from EHS module
   - Updates Employee.CompetencyMatrix with new training
   - Records completion date + certification expiry

2. **Integration Test**
   - `EHS_TrainingCompleted_UpdatesEmployeeCompetency.cs`
   - Scenario: Training DONE → Employee competency updated
   - Verify event propagation + competency matrix change

3. **Build Success**
   - 0 errors, 0 warnings
   - All existing tests still pass

4. **Documentation**
   - Update `docs/knowledge/engineering/BACKEND_PATTERNS.md` with integration pattern (follow MSG-BACKEND-451 pattern)

---

## 📋 IMPLEMENTATION GUIDE

### 1. Event Definition (Contracts Module)

**Location:** `/opt/spaceos/backend/spaceos-modules-contracts/SpaceOS.Modules.Contracts/EHS/Events/TrainingCompletedEvent.cs`

Create new EHS/Events folder if not exists.

```csharp
namespace SpaceOS.Modules.Contracts.EHS.Events;

public class TrainingCompletedEvent : ModuleEvent
{
    public Guid EmployeeId { get; init; }
    public Guid TrainingTypeId { get; init; }
    public string TrainingName { get; init; } = string.Empty;
    public string CertificationLevel { get; init; } = string.Empty;
    public DateTime CompletionDate { get; init; }
    public DateTime? CertificationExpiry { get; init; }
}
```

### 2. Event Handler (HR Module)

**Location:** `/opt/spaceos/backend/spaceos-modules-hr/HR.Application/EventHandlers/TrainingCompletedEventHandler.cs`

```csharp
using MediatR;
using SpaceOS.Modules.Contracts.EHS.Events;
using HR.Domain.Employees;

namespace HR.Application.EventHandlers;

public class TrainingCompletedEventHandler : INotificationHandler<TrainingCompletedEvent>
{
    private readonly IEmployeeRepository _repository;

    public TrainingCompletedEventHandler(IEmployeeRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(TrainingCompletedEvent notification, CancellationToken ct)
    {
        var employee = await _repository.GetByIdAsync(notification.EmployeeId, ct);

        if (employee == null)
        {
            // Log warning - Employee not found (possible data sync issue)
            return;
        }

        employee.AddCompetency(
            competencyId: notification.TrainingTypeId,
            competencyName: notification.TrainingName,
            level: notification.CertificationLevel,
            validFrom: notification.CompletionDate,
            validUntil: notification.CertificationExpiry
        );

        await _repository.SaveAsync(employee, ct);
    }
}
```

### 3. Employee Aggregate Enhancement

**Location:** `/opt/spaceos/backend/spaceos-modules-hr/HR.Domain/Employees/Employee.cs`

Add method:
```csharp
public void AddCompetency(
    Guid competencyId,
    string competencyName,
    string level,
    DateTime validFrom,
    DateTime? validUntil)
{
    var competency = new EmployeeCompetency(
        id: Guid.NewGuid(),
        employeeId: Id,
        competencyId: competencyId,
        competencyName: competencyName,
        level: level,
        validFrom: validFrom,
        validUntil: validUntil
    );

    _competencies.Add(competency);

    AddDomainEvent(new EmployeeCompetencyAddedEvent(Id, competencyId));
}
```

Add owned entity if not exists:
```csharp
public class EmployeeCompetency
{
    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid CompetencyId { get; private set; }
    public string CompetencyName { get; private set; } = string.Empty;
    public string Level { get; private set; } = string.Empty;
    public DateTime ValidFrom { get; private set; }
    public DateTime? ValidUntil { get; private set; }

    private EmployeeCompetency() { } // EF Core

    public EmployeeCompetency(Guid id, Guid employeeId, Guid competencyId,
        string competencyName, string level, DateTime validFrom, DateTime? validUntil)
    {
        Id = id;
        EmployeeId = employeeId;
        CompetencyId = competencyId;
        CompetencyName = competencyName;
        Level = level;
        ValidFrom = validFrom;
        ValidUntil = validUntil;
    }
}
```

### 4. EF Core Configuration

**Location:** `/opt/spaceos/backend/spaceos-modules-hr/HR.Infrastructure/Persistence/Configurations/EmployeeConfiguration.cs`

Add owned collection mapping:
```csharp
builder.OwnsMany(e => e.Competencies, competencies =>
{
    competencies.ToTable("employee_competencies");
    competencies.WithOwner().HasForeignKey("employee_id");
    competencies.Property<Guid>("Id").HasColumnName("id");
    competencies.Property<Guid>("CompetencyId").HasColumnName("competency_id");
    competencies.Property<string>("CompetencyName").HasColumnName("competency_name").HasMaxLength(200);
    competencies.Property<string>("Level").HasColumnName("level").HasMaxLength(50);
    competencies.Property<DateTime>("ValidFrom").HasColumnName("valid_from");
    competencies.Property<DateTime?>("ValidUntil").HasColumnName("valid_until");
});
```

### 5. Integration Test

**Location:** `/opt/spaceos/backend/spaceos-modules-hr/HR.Tests/Integration/CrossModule/EHS_TrainingCompleted_UpdatesEmployeeCompetency.cs`

```csharp
using HR.Tests.Integration.Common;
using SpaceOS.Modules.Contracts.EHS.Events;
using MediatR;

namespace HR.Tests.Integration.CrossModule;

public class EHS_TrainingCompleted_UpdatesEmployeeCompetency : IntegrationTestBase
{
    [Fact]
    public async Task TrainingCompleted_AddsCompetencyToEmployee()
    {
        // Arrange
        var employee = await CreateTestEmployee("John Doe");
        var trainingEvent = new TrainingCompletedEvent
        {
            EmployeeId = employee.Id,
            TrainingTypeId = Guid.NewGuid(),
            TrainingName = "Forklift Safety",
            CertificationLevel = "Level 2",
            CompletionDate = DateTime.UtcNow,
            CertificationExpiry = DateTime.UtcNow.AddYears(2)
        };

        // Act
        await Mediator.Publish(trainingEvent);

        // Assert
        var updatedEmployee = await EmployeeRepository.GetByIdAsync(employee.Id, CancellationToken.None);
        Assert.NotNull(updatedEmployee);
        Assert.Contains(updatedEmployee.Competencies, c => c.CompetencyName == "Forklift Safety");
        Assert.Contains(updatedEmployee.Competencies, c => c.Level == "Level 2");
    }

    [Fact]
    public async Task TrainingCompleted_EmployeeNotFound_DoesNotCrash()
    {
        // Arrange
        var trainingEvent = new TrainingCompletedEvent
        {
            EmployeeId = Guid.NewGuid(), // Non-existent
            TrainingTypeId = Guid.NewGuid(),
            TrainingName = "Safety Training",
            CertificationLevel = "Level 1",
            CompletionDate = DateTime.UtcNow
        };

        // Act & Assert - should not throw
        await Mediator.Publish(trainingEvent);
    }
}
```

---

## 📚 CONTEXT

**Pattern Reference:** MSG-BACKEND-451 (Maintenance→Production) used same cross-module integration pattern.

**EHS Module ADR:** (Check if exists - if not, create placeholder note)
**HR Module ADR:** ADR-056
**Event Bus:** MediatR domain events
**Pattern:** Cross-module integration via domain events (NOT direct DB calls)

**Related Messages:**
- EHS Module complete (2026-07-08)
- HR Module complete (2026-07-08)
- MSG-BACKEND-451 DONE (Maintenance→Production pattern reference)

**Unblocked By:** All dependencies complete (EHS + HR modules both done)

---

## 🔍 VERIFICATION CHECKLIST

- [ ] TrainingCompletedEvent created in Contracts module
- [ ] TrainingCompletedEventHandler implements INotificationHandler
- [ ] Employee.AddCompetency() method added
- [ ] EmployeeCompetency owned entity configured
- [ ] EF Core configuration for employee_competencies table
- [ ] Integration test GREEN (training done → competency added)
- [ ] Integration test GREEN (employee not found → no crash)
- [ ] Existing HR tests still pass
- [ ] Build: 0 errors, 0 warnings (or only xUnit ConfigureAwait warnings)
- [ ] BACKEND_PATTERNS.md updated (follow MSG-451 format)

---

## 🚀 NEXT CHECKPOINT

After this DONE → All 3 JoineryTech integration checkpoints complete ✅

---

**Conductor Note:** Re-dispatched from Backend-2 due to session reliability issues. Backend terminal has proven track record with cross-module integrations (MSG-451 success).

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
