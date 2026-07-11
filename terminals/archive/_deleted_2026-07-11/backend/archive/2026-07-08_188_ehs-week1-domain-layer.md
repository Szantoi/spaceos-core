---
id: MSG-BACKEND-188
from: conductor
to: backend
type: task
priority: high
status: UNREAD
model: sonnet
ref: MSG-ARCHITECT-073-DONE
created: 2026-07-08
epic_id: EPIC-JT-EHS
estimated_nwt: 120
content_hash: b98eb16a5304c88d75853bd566e5e9b0b7718b64deae8c1319ac8f02529bbd44
---

# EPIC-JT-EHS: Week 1 Domain Layer Implementation

**Epic:** EPIC-JT-EHS (JoineryTech Munkavédelem/Safety)
**Phase:** Week 1 — Domain Layer
**Parallel Track:** EPIC-DOORSTAR-SOFTLAUNCH (planning complete, execution deferred)
**Timeline:** 4-6 hours (120 NWT)

---

## Context: Aggressive Parallel Execution Complete

**VPS Capacity:** 6 cores, 15GB RAM (41% CPU headroom, 60% RAM headroom)

**Architect Deliverables (2026-07-08):**
- ✅ **Track A (EHS):** MSG-ARCHITECT-073 OpenAPI Spec DONE (2.5h, Sonnet)
- ✅ **Track B (Doorstar):** MSG-ARCHITECT-072 Planning DONE (6h, Opus)

**Your Role:** Implement EHS Week 1 Domain Layer (proven Week 1-4 pattern from 6 previous modules)

---

## Task: EHS Domain Layer Implementation

**Scope:** Munkavédelem (Occupational Health & Safety) module — ISO 45001 compliance

### 1. Domain Aggregates (3)

**Reference Spec:** `spaceos-modules-ehs/docs/openapi.yaml` (MSG-ARCHITECT-073)

#### Incident Aggregate

**Root:** Incident
**Owned Entities:**
- IncidentInvestigation (0-1)
- CorrectiveAction (0-n)
- IncidentWitness (0-n)

**Properties:**
```csharp
public class Incident
{
    public Guid IncidentId { get; private set; }
    public Guid TenantId { get; private set; }
    public IncidentType IncidentType { get; private set; }  // enum: Accident, NearMiss, HazardousCondition
    public DateTimeOffset IncidentDate { get; private set; }
    public string Location { get; private set; }
    public string Description { get; private set; }
    public Severity Severity { get; private set; }  // enum: 1-5
    public IncidentStatus Status { get; private set; }  // FSM state
    public Guid ReportedBy { get; private set; }  // EmployeeId FK
    public DateTimeOffset ReportedAt { get; private set; }
    public Guid? InvestigatedBy { get; private set; }  // nullable
    public DateTimeOffset? InvestigatedAt { get; private set; }  // nullable
    public DateTimeOffset? ClosedAt { get; private set; }  // nullable

    // Navigation properties
    public IncidentInvestigation? Investigation { get; private set; }
    public IReadOnlyList<CorrectiveAction> CorrectiveActions => _correctiveActions.AsReadOnly();
    public IReadOnlyList<IncidentWitness> Witnesses => _witnesses.AsReadOnly();
}
```

#### RiskAssessment Aggregate

**Root:** RiskAssessment
**Owned Entity:** RiskControl (0-n) — mitigation measures

**Properties:**
```csharp
public class RiskAssessment
{
    public Guid RiskAssessmentId { get; private set; }
    public Guid TenantId { get; private set; }
    public string HazardDescription { get; private set; }
    public Severity Severity { get; private set; }  // 1-5
    public Likelihood Likelihood { get; private set; }  // 1-5
    public int RiskScore { get; private set; }  // calculated: Severity × Likelihood
    public RiskLevel RiskLevel { get; private set; }  // calculated: Low/Medium/High
    public Guid AssessedBy { get; private set; }  // EmployeeId FK
    public DateTimeOffset AssessedAt { get; private set; }
    public DateTimeOffset ReviewDueDate { get; private set; }
    public RiskStatus Status { get; private set; }  // Active | Archived

    // Navigation
    public IReadOnlyList<RiskControl> Controls => _controls.AsReadOnly();
}
```

#### TrainingRecord Aggregate

**Root:** TrainingRecord (no owned entities)

**Properties:**
```csharp
public class TrainingRecord
{
    public Guid TrainingRecordId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid EmployeeId { get; private set; }  // FK to HR module
    public string TrainingType { get; private set; }  // e.g., "First Aid", "Fire Safety"
    public DateTimeOffset CompletedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }  // nullable
    public string IssuedBy { get; private set; }  // certifying authority
    public string? CertificateNumber { get; private set; }  // nullable
    public TrainingStatus Status { get; private set; }  // Valid | Expiring | Expired
}
```

---

### 2. FSM State Machine (Incident Workflow)

**IncidentStatus Enum:**
```csharp
public enum IncidentStatus
{
    Reported = 1,
    Investigated = 2,
    CorrectiveActionPlanned = 3,
    Closed = 4,
    Reopened = 5
}
```

**FSM Transitions:**
```
Reported → Investigated → CorrectiveActionPlanned → Closed
                                                      ↓
                                                   Reopened
```

**Domain Methods (Incident aggregate):**
```csharp
public void StartInvestigation(Guid investigatedBy)
{
    if (Status != IncidentStatus.Reported)
        throw new InvalidOperationException("Can only investigate reported incidents");

    InvestigatedBy = investigatedBy;
    InvestigatedAt = DateTimeOffset.UtcNow;
    Status = IncidentStatus.Investigated;

    AddDomainEvent(new InvestigationStartedEvent(IncidentId, investigatedBy));
}

public void AddCorrectiveAction(string description, Guid assignedTo, DateTimeOffset dueDate)
{
    if (Status != IncidentStatus.Investigated)
        throw new InvalidOperationException("Corrective actions can only be added to investigated incidents");

    var action = new CorrectiveAction(Guid.NewGuid(), description, assignedTo, dueDate);
    _correctiveActions.Add(action);
    Status = IncidentStatus.CorrectiveActionPlanned;

    AddDomainEvent(new CorrectiveActionPlannedEvent(IncidentId, action.CorrectiveActionId));
}

public void CloseIncident()
{
    if (Status != IncidentStatus.CorrectiveActionPlanned)
        throw new InvalidOperationException("Can only close incidents with corrective actions planned");

    ClosedAt = DateTimeOffset.UtcNow;
    Status = IncidentStatus.Closed;

    AddDomainEvent(new IncidentClosedEvent(IncidentId));
}

public void ReopenIncident()
{
    if (Status != IncidentStatus.Closed)
        throw new InvalidOperationException("Can only reopen closed incidents");

    Status = IncidentStatus.Reopened;

    AddDomainEvent(new IncidentReopenedEvent(IncidentId));
}
```

---

### 3. Domain Methods (5×5 Risk Matrix Logic)

**RiskAssessment aggregate:**
```csharp
public static RiskAssessment Create(
    Guid riskAssessmentId,
    Guid tenantId,
    string hazardDescription,
    Severity severity,
    Likelihood likelihood,
    Guid assessedBy,
    DateTimeOffset reviewDueDate)
{
    var riskScore = CalculateRiskScore(severity, likelihood);
    var riskLevel = CalculateRiskLevel(riskScore);

    var assessment = new RiskAssessment
    {
        RiskAssessmentId = riskAssessmentId,
        TenantId = tenantId,
        HazardDescription = hazardDescription,
        Severity = severity,
        Likelihood = likelihood,
        RiskScore = riskScore,
        RiskLevel = riskLevel,
        AssessedBy = assessedBy,
        AssessedAt = DateTimeOffset.UtcNow,
        ReviewDueDate = reviewDueDate,
        Status = RiskStatus.Active
    };

    assessment.AddDomainEvent(new RiskAssessmentCreatedEvent(riskAssessmentId, riskLevel));
    return assessment;
}

private static int CalculateRiskScore(Severity severity, Likelihood likelihood)
{
    return (int)severity * (int)likelihood;  // 1-25
}

private static RiskLevel CalculateRiskLevel(int riskScore)
{
    return riskScore switch
    {
        >= 1 and <= 5 => RiskLevel.Low,
        >= 6 and <= 12 => RiskLevel.Medium,
        >= 15 and <= 25 => RiskLevel.High,
        _ => throw new ArgumentOutOfRangeException(nameof(riskScore), "RiskScore must be 1-25")
    };
}
```

**TrainingRecord aggregate:**
```csharp
public static TrainingStatus CheckTrainingExpiry(DateTimeOffset? expiresAt)
{
    if (expiresAt == null)
        return TrainingStatus.Valid;  // No expiration

    var daysUntilExpiry = (expiresAt.Value - DateTimeOffset.UtcNow).TotalDays;

    return daysUntilExpiry switch
    {
        > 30 => TrainingStatus.Valid,
        > 0 => TrainingStatus.Expiring,
        _ => TrainingStatus.Expired
    };
}
```

---

### 4. Value Objects & Enums

**IncidentType (enum):**
```csharp
public enum IncidentType
{
    Accident = 1,
    NearMiss = 2,
    HazardousCondition = 3
}
```

**Severity (enum, 1-5 scale):**
```csharp
public enum Severity
{
    Negligible = 1,
    Minor = 2,
    Moderate = 3,
    Major = 4,
    Catastrophic = 5
}
```

**Likelihood (enum, 1-5 scale):**
```csharp
public enum Likelihood
{
    Rare = 1,
    Unlikely = 2,
    Possible = 3,
    Likely = 4,
    AlmostCertain = 5
}
```

**RiskLevel (enum, calculated):**
```csharp
public enum RiskLevel
{
    Low = 1,    // RiskScore 1-5
    Medium = 2, // RiskScore 6-12
    High = 3    // RiskScore 15-25
}
```

**RiskStatus (enum):**
```csharp
public enum RiskStatus
{
    Active = 1,
    Archived = 2
}
```

**TrainingStatus (enum, calculated):**
```csharp
public enum TrainingStatus
{
    Valid = 1,     // >30 days until expiry
    Expiring = 2,  // ≤30 days until expiry
    Expired = 3    // Past expiration
}
```

---

### 5. Domain Events (Minimum Required)

**Incident Events:**
```csharp
public record IncidentReportedEvent(Guid IncidentId, IncidentType IncidentType, Severity Severity);
public record InvestigationStartedEvent(Guid IncidentId, Guid InvestigatedBy);
public record CorrectiveActionPlannedEvent(Guid IncidentId, Guid CorrectiveActionId);
public record IncidentClosedEvent(Guid IncidentId);
public record IncidentReopenedEvent(Guid IncidentId);
```

**RiskAssessment Events:**
```csharp
public record RiskAssessmentCreatedEvent(Guid RiskAssessmentId, RiskLevel RiskLevel);
public record RiskControlAddedEvent(Guid RiskAssessmentId, Guid RiskControlId);
public record RiskAssessmentArchivedEvent(Guid RiskAssessmentId);
```

**TrainingRecord Events:**
```csharp
public record TrainingRecordCreatedEvent(Guid TrainingRecordId, Guid EmployeeId, string TrainingType);
public record TrainingRecordRenewedEvent(Guid TrainingRecordId, DateTimeOffset NewExpiryDate);
```

---

## Success Criteria

1. ✅ **All 3 aggregates implemented** (Incident, RiskAssessment, TrainingRecord)
2. ✅ **FSM state machine functional** (Incident workflow with 5 states, 5 transitions)
3. ✅ **Domain methods implemented:**
   - CalculateRiskScore(severity, likelihood) → int
   - CalculateRiskLevel(riskScore) → RiskLevel enum
   - CheckTrainingExpiry(expiresAt) → TrainingStatus enum
4. ✅ **Value objects & enums complete** (IncidentType, Severity, Likelihood, RiskLevel, TrainingStatus)
5. ✅ **Domain events published** (minimum 11 events across 3 aggregates)
6. ✅ **Unit tests written** (aggregate behavior, FSM transitions, calculation logic)
7. ✅ **Pattern consistency** (matches DMS, HR, Maintenance, QA, CRM, Kontrolling)
8. ✅ **DONE outbox written** with implementation summary

---

## Reference Pattern

**Proven Week 1 Domain Layer pattern (6 modules):**
- DMS: Document aggregate, DocumentStatus FSM
- HR: Employee aggregate, EmploymentStatus FSM
- Maintenance: MaintenanceRequest aggregate, MaintenanceStatus FSM
- QA: QualityInspection aggregate, InspectionStatus FSM
- CRM: Lead/Opportunity aggregates, OpportunityStatus FSM
- Kontrolling: CostBudget aggregate, BudgetStatus FSM

**Your task:** Apply same pattern to EHS aggregates (Incident, RiskAssessment, TrainingRecord)

---

## Files to Create

```
spaceos-modules-ehs/
  SpaceOS.Modules.Ehs.Domain/
    Aggregates/
      IncidentAggregate/
        Incident.cs
        IncidentInvestigation.cs
        CorrectiveAction.cs
        IncidentWitness.cs
        IncidentType.cs (enum)
        IncidentStatus.cs (enum)
        Severity.cs (enum)
      RiskAssessmentAggregate/
        RiskAssessment.cs
        RiskControl.cs
        Likelihood.cs (enum)
        RiskLevel.cs (enum)
        RiskStatus.cs (enum)
      TrainingRecordAggregate/
        TrainingRecord.cs
        TrainingStatus.cs (enum)
    Events/
      IncidentReportedEvent.cs
      InvestigationStartedEvent.cs
      CorrectiveActionPlannedEvent.cs
      IncidentClosedEvent.cs
      IncidentReopenedEvent.cs
      RiskAssessmentCreatedEvent.cs
      RiskControlAddedEvent.cs
      RiskAssessmentArchivedEvent.cs
      TrainingRecordCreatedEvent.cs
      TrainingRecordRenewedEvent.cs
```

---

## Estimated Timeline

**120 NWT (~4-6 hours):**
- 3 aggregates: 60 NWT
- FSM + domain methods: 30 NWT
- Value objects + events: 20 NWT
- Unit tests: 10 NWT

---

**Priority:** HIGH (Week 1 of 4-week module implementation)
**Model:** Sonnet (proven pattern, medium complexity)
**Estimated Time:** 4-6 hours (120 NWT)
**Resource Allocation:** 1 CPU core, 2GB RAM

🚀 Generated by Conductor — EHS Week 1 Domain Layer Dispatch

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
