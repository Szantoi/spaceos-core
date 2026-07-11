---
id: MSG-BACKEND2-002
from: conductor
to: backend-2
type: task
priority: high
status: UNREAD
model: sonnet
ref: CP-EHS-HR-INTEGRATION
created: 2026-07-10
estimated_nwt: 45
epic_id: EPIC-JT-EHS
checkpoint_id: CP-EHS-HR-INTEGRATION
content_hash: 9184491f5d6a3ff42b23012e8bb5f818a2985a84270798e90649e45a21963eb7
---

# CP-EHS-HR-INTEGRATION: EHS → HR Integration

**Epic:** EPIC-JT-EHS (Environment, Health & Safety Module)
**Checkpoint:** CP-EHS-HR-INTEGRATION
**Scope:** Training competencies → Employee records synchronization
**Estimate:** 45 NWT (~1.5 hours)

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
   - Update `docs/knowledge/architecture/MODULE_BOUNDARIES.md` with integration pattern

---

## 📋 IMPLEMENTATION GUIDE

### 1. Event Handler (Domain Event Pattern)

**Location:** `/opt/spaceos/backend/spaceos-modules-hr/HR.Application/EventHandlers/TrainingCompletedEventHandler.cs`

```csharp
public class TrainingCompletedEventHandler : INotificationHandler<TrainingCompletedEvent>
{
    private readonly IEmployeeRepository _repository;

    public async Task Handle(TrainingCompletedEvent notification, CancellationToken ct)
    {
        var employee = await _repository.GetByIdAsync(notification.EmployeeId, ct);

        if (employee == null)
        {
            throw new NotFoundException($"Employee {notification.EmployeeId} not found");
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

### 2. Employee Aggregate Enhancement

Add method to `Employee` aggregate:
```csharp
public void AddCompetency(
    Guid competencyId,
    string competencyName,
    string level,
    DateTime validFrom,
    DateTime? validUntil)
{
    // Add to CompetencyMatrix (owned collection)
    var competency = new EmployeeCompetency(
        competencyId, competencyName, level, validFrom, validUntil);

    _competencies.Add(competency);

    AddDomainEvent(new EmployeeCompetencyAddedEvent(Id, competencyId));
}
```

### 3. Integration Test

**Location:** `/opt/spaceos/backend/spaceos-modules-hr/HR.Tests/Integration/CrossModule/EHS_TrainingCompleted_UpdatesEmployeeCompetency.cs`

**Scenario:**
1. Create Employee (EMPLOYEE-123, competencies: [])
2. Publish TrainingCompletedEvent (employeeId: EMPLOYEE-123, training: "Forklift Safety")
3. Assert: Employee.CompetencyMatrix contains "Forklift Safety"
4. Assert: Competency.ValidFrom == event.CompletionDate
5. Assert: Competency.ValidUntil == event.CertificationExpiry

---

## 📚 CONTEXT

**EHS Module ADR:** ADR (to be referenced from existing EHS docs)
**HR Module ADR:** ADR-056
**Event Bus:** MediatR domain events
**Pattern:** Cross-module integration via domain events (NOT direct DB calls)

**Related Messages:**
- EHS Module complete (2026-07-08)
- HR Module complete (2026-07-08)

**Unblocked By:** All dependencies complete (EHS + HR modules both done)

---

## 🔍 VERIFICATION CHECKLIST

- [ ] TrainingCompletedEventHandler implements INotificationHandler
- [ ] Employee.AddCompetency() method added
- [ ] Integration test GREEN (training done → competency added)
- [ ] Existing HR tests still pass
- [ ] Build: 0 errors, 0 warnings
- [ ] MODULE_BOUNDARIES.md updated with integration pattern

---

## 🚀 NEXT CHECKPOINT

After this DONE → CP-CRM-INTEGRATION (requires Architect planning first)

---

**Conductor Note:** Parallel dispatch with MSG-BACKEND-451 (Maintenance→Production integration). Estimated completion: 1.5 hours from now (2026-07-10 ~23:20 UTC).

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
