---
processed: 2026-07-04
id: MSG-BACKEND-146
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
epic_id: EPIC-JT-QA
checkpoint_id: CP-QA-BACKEND
ref: MSG-ARCHITECT-065-DONE
created: 2026-07-04
estimated_nwt: 180
unblocked_at: 2026-07-06
unblocked_by: conductor
content_hash: b347ec85dc4276d82333b7d89cb4f946b7bca65a38815979fb44475bca927615
---

# JoineryTech QA Week 1 — Domain Layer Implementation

**Epic:** EPIC-JT-QA (Minőségbiztosítás)
**Estimated:** 180 NWT (~6 hours)
**Priority:** High (OpenAPI spec ready, domain model complete)
**Blocked by:** MSG-BACKEND-143 (Kontrolling Week 2 foundation)

---

## Context

A QA Domain Model (MSG-ARCHITECT-063-DONE) és OpenAPI Specification (MSG-ARCHITECT-065-DONE) elkészült. Most a Contract-First workflow Week 1 fázisa következik: **Domain Layer implementáció**.

**Domain Model:** `/opt/spaceos/docs/joinerytech/domain/QA_DOMAIN_MODEL.md` (1832 lines)
**OpenAPI Spec:** `/opt/spaceos/spaceos-modules-qa/docs/openapi.yaml` (1712 lines, 28 endpoints)

**Prototípus:** JoineryTech prototípus volt QA/reklamáció modul (migrálandó)
**Critical Integration:** Production blocking (failed critical inspections block production)

---

## Deliverables

**Module:** `/opt/spaceos/spaceos-modules-qa/`

**Folder struktúra:**
```
spaceos-modules-qa/
├── Domain/
│   ├── QACheckpoint/
│   │   ├── QACheckpoint.cs (aggregate root)
│   │   ├── QACheckpointId.cs (strongly-typed ID)
│   │   └── InspectionCriteria.cs (value object)
│   ├── Inspection/
│   │   ├── Inspection.cs (aggregate root)
│   │   ├── InspectionId.cs (strongly-typed ID)
│   │   ├── FailureNote.cs (value object)
│   │   ├── InspectionStateMachine.cs (FSM validator)
│   │   └── Events/ (domain events)
│   ├── Ticket/
│   │   ├── Ticket.cs (aggregate root)
│   │   ├── TicketId.cs (strongly-typed ID)
│   │   ├── ResolutionAction.cs (value object)
│   │   ├── TicketStateMachine.cs (FSM validator)
│   │   └── Events/ (domain events)
│   ├── Enums/
│   │   ├── InspectionResult.cs
│   │   ├── InspectionStatus.cs
│   │   ├── TicketType.cs
│   │   ├── TicketStatus.cs
│   │   ├── TicketPriority.cs
│   │   ├── CheckpointType.cs
│   │   ├── CriticalLevel.cs
│   │   ├── CriteriaType.cs
│   │   └── FailureType.cs
│   ├── Services/
│   │   ├── IInspectionBlockingService.cs (Production integration - CRITICAL!)
│   │   ├── InspectionBlockingService.cs
│   │   ├── ITicketRoutingService.cs
│   │   ├── TicketRoutingService.cs
│   │   ├── IRootCauseAnalysisService.cs
│   │   └── RootCauseAnalysisService.cs
│   └── Repositories/
│       ├── IQACheckpointRepository.cs
│       ├── IInspectionRepository.cs
│       └── ITicketRepository.cs
└── Tests/
    └── Domain.Tests/
        ├── QACheckpointTests.cs
        ├── InspectionTests.cs
        ├── InspectionStateMachineTests.cs
        ├── TicketTests.cs
        ├── TicketStateMachineTests.cs
        ├── InspectionBlockingServiceTests.cs
        └── RootCauseAnalysisServiceTests.cs
```

---

## Implementation Details

### 1. Aggregate Roots

#### QACheckpoint Aggregate
```csharp
public class QACheckpoint : AggregateRoot<QACheckpointId>
{
    public string Name { get; private set; }
    public CheckpointType Type { get; private set; }
    public CriticalLevel CriticalLevel { get; private set; }
    private readonly List<InspectionCriteria> _criteria = new();
    public IReadOnlyList<InspectionCriteria> Criteria => _criteria.AsReadOnly();
    public bool IsActive { get; private set; }

    public static QACheckpoint Create(
        Guid tenantId,
        string name,
        CheckpointType type,
        CriticalLevel criticalLevel)
    {
        // Validation: Name required, unique per tenant (checked in app layer)
        var checkpoint = new QACheckpoint
        {
            Id = QACheckpointId.NewId(),
            TenantId = tenantId,
            Name = name,
            Type = type,
            CriticalLevel = criticalLevel,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        checkpoint.AddDomainEvent(new QACheckpointCreatedEvent(
            checkpoint.Id.Value, tenantId, name, type, criticalLevel));

        return checkpoint;
    }

    public void AddCriteria(InspectionCriteria criteria)
    {
        _criteria.Add(criteria);
        AddDomainEvent(new InspectionCriteriaAddedEvent(Id.Value, TenantId, criteria));
    }

    public void Deactivate()
    {
        IsActive = false;
        AddDomainEvent(new QACheckpointDeactivatedEvent(Id.Value, TenantId));
    }
}
```

#### Inspection Aggregate (FSM-Enforced)
```csharp
public class Inspection : AggregateRoot<InspectionId>
{
    public QACheckpointId CheckpointId { get; private set; }
    public Guid? OrderId { get; private set; }
    public Guid? ProjectId { get; private set; }
    public Guid InspectorEmployeeId { get; private set; }
    public InspectionStatus Status { get; private set; }
    public InspectionResult? Result { get; private set; }
    public DateTime InspectionDate { get; private set; }
    private readonly List<FailureNote> _failureNotes = new();
    public IReadOnlyList<FailureNote> FailureNotes => _failureNotes.AsReadOnly();
    public string? Notes { get; private set; }

    public static Inspection Create(
        Guid tenantId,
        QACheckpointId checkpointId,
        Guid? orderId,
        Guid? projectId,
        Guid inspectorEmployeeId,
        string? notes)
    {
        var inspection = new Inspection
        {
            Id = InspectionId.NewId(),
            TenantId = tenantId,
            CheckpointId = checkpointId,
            OrderId = orderId,
            ProjectId = projectId,
            InspectorEmployeeId = inspectorEmployeeId,
            Status = InspectionStatus.Planned,
            InspectionDate = DateTime.UtcNow,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };

        inspection.AddDomainEvent(new InspectionPlannedEvent(
            inspection.Id.Value, tenantId, checkpointId.Value));

        return inspection;
    }

    public void Start()
    {
        InspectionStateMachine.ValidateTransition(Status, InspectionStatus.InProgress);
        Status = InspectionStatus.InProgress;
        AddDomainEvent(new InspectionStartedEvent(Id.Value, TenantId, InspectorEmployeeId));
    }

    public void Complete()
    {
        InspectionStateMachine.ValidateTransition(Status, InspectionStatus.Completed);
        Status = InspectionStatus.Completed;
        Result = InspectionResult.Pass;
        AddDomainEvent(new InspectionCompletedEvent(Id.Value, TenantId, InspectionResult.Pass));
    }

    public void Fail(List<FailureNote> failureNotes, string? notes = null)
    {
        if (!failureNotes.Any())
            throw new DomainException("Failed inspections require at least one FailureNote");

        InspectionStateMachine.ValidateTransition(Status, InspectionStatus.Completed);

        Status = InspectionStatus.Completed;
        Result = InspectionResult.Fail;
        _failureNotes.AddRange(failureNotes);
        if (notes != null) Notes = notes;

        AddDomainEvent(new InspectionFailedEvent(Id.Value, TenantId, failureNotes));
    }
}
```

#### Ticket Aggregate (FSM-Enforced)
```csharp
public class Ticket : AggregateRoot<TicketId>
{
    public string TicketNumber { get; private set; } // Auto-generated: "REK-2026-001"
    public TicketType Type { get; private set; }
    public TicketStatus Status { get; private set; }
    public TicketPriority Priority { get; private set; }
    public Guid? OrderId { get; private set; }
    public Guid? ProjectId { get; private set; }
    public string CustomerDescription { get; private set; }
    public Guid? AssignedToEmployeeId { get; private set; }
    public string? RootCause { get; private set; }
    public ResolutionAction? ResolutionAction { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime ReportedAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }

    public static Ticket Create(
        Guid tenantId,
        string ticketNumber,
        TicketType type,
        TicketPriority priority,
        Guid? orderId,
        Guid? projectId,
        string customerDescription)
    {
        if (customerDescription.Length < 10)
            throw new DomainException("Customer description must be at least 10 characters");

        var ticket = new Ticket
        {
            Id = TicketId.NewId(),
            TenantId = tenantId,
            TicketNumber = ticketNumber,
            Type = type,
            Status = TicketStatus.Reported,
            Priority = priority,
            OrderId = orderId,
            ProjectId = projectId,
            CustomerDescription = customerDescription,
            ReportedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        ticket.AddDomainEvent(new TicketReportedEvent(
            ticket.Id.Value, tenantId, ticketNumber, type, priority));

        return ticket;
    }

    public void Assign(Guid assignedToEmployeeId)
    {
        TicketStateMachine.ValidateTransition(Status, TicketStatus.Assigned);
        Status = TicketStatus.Assigned;
        AssignedToEmployeeId = assignedToEmployeeId;
        AddDomainEvent(new TicketAssignedEvent(Id.Value, TenantId, assignedToEmployeeId));
    }

    public void Resolve(string rootCause, ResolutionAction resolutionAction)
    {
        if (rootCause.Length < 10)
            throw new DomainException("Root cause must be at least 10 characters");

        TicketStateMachine.ValidateTransition(Status, TicketStatus.Resolved);

        Status = TicketStatus.Resolved;
        RootCause = rootCause;
        ResolutionAction = resolutionAction;
        ResolvedAt = DateTime.UtcNow;

        AddDomainEvent(new TicketResolvedEvent(Id.Value, TenantId, resolutionAction));
    }
}
```

### 2. Value Objects

```csharp
public record InspectionCriteria
{
    public Guid Id { get; init; }
    public CriteriaType Type { get; init; }
    public string Description { get; init; }
    public string? AcceptanceThreshold { get; init; }
}

public record FailureNote
{
    public Guid Id { get; init; }
    public FailureType FailureType { get; init; }
    public string Description { get; init; }
    public string? PhotoUrl { get; init; }
}

public record ResolutionAction
{
    public ActionType ActionType { get; init; }
    public string Description { get; init; }
    public Money Cost { get; init; }
}
```

### 3. FSM State Machines

```csharp
public static class InspectionStateMachine
{
    public static void ValidateTransition(InspectionStatus from, InspectionStatus to)
    {
        var validTransitions = new Dictionary<InspectionStatus, List<InspectionStatus>>
        {
            { InspectionStatus.Planned, new() { InspectionStatus.InProgress } },
            { InspectionStatus.InProgress, new() { InspectionStatus.Completed } }
        };

        if (!validTransitions.ContainsKey(from) || !validTransitions[from].Contains(to))
            throw new InvalidStateTransitionException($"Cannot transition from {from} to {to}");
    }
}

public static class TicketStateMachine
{
    public static void ValidateTransition(TicketStatus from, TicketStatus to)
    {
        var validTransitions = new Dictionary<TicketStatus, List<TicketStatus>>
        {
            { TicketStatus.Reported, new() { TicketStatus.Assigned } },
            { TicketStatus.Assigned, new() { TicketStatus.InProgress } },
            { TicketStatus.InProgress, new() { TicketStatus.Resolved, TicketStatus.Rejected } },
            { TicketStatus.Rejected, new() { TicketStatus.Reported } }
        };

        if (!validTransitions.ContainsKey(from) || !validTransitions[from].Contains(to))
            throw new InvalidStateTransitionException($"Cannot transition from {from} to {to}");
    }
}
```

### 4. Domain Services (CRITICAL - Production Integration)

```csharp
public interface IInspectionBlockingService
{
    bool IsProductionBlocked(Inspection inspection, QACheckpoint checkpoint);
    IEnumerable<Inspection> GetBlockingInspections(Guid orderId, IEnumerable<Inspection> inspections);
}

public class InspectionBlockingService : IInspectionBlockingService
{
    public bool IsProductionBlocked(Inspection inspection, QACheckpoint checkpoint)
    {
        return inspection.Result == InspectionResult.Fail
            && checkpoint.CriticalLevel == CriticalLevel.Critical;
    }

    public IEnumerable<Inspection> GetBlockingInspections(
        Guid orderId,
        IEnumerable<Inspection> inspections)
    {
        return inspections.Where(i =>
            i.OrderId == orderId &&
            i.Result == InspectionResult.Fail &&
            i.Status == InspectionStatus.Completed);
    }
}
```

```csharp
public interface IRootCauseAnalysisService
{
    IEnumerable<FailureCategory> AnalyzeRootCauses(
        IEnumerable<Inspection> failedInspections,
        DateOnly startDate,
        DateOnly endDate);
}

public class RootCauseAnalysisService : IRootCauseAnalysisService
{
    public IEnumerable<FailureCategory> AnalyzeRootCauses(
        IEnumerable<Inspection> failedInspections,
        DateOnly startDate,
        DateOnly endDate)
    {
        var filtered = failedInspections
            .Where(i => DateOnly.FromDateTime(i.InspectionDate) >= startDate &&
                       DateOnly.FromDateTime(i.InspectionDate) <= endDate)
            .ToList();

        if (!filtered.Any()) return Enumerable.Empty<FailureCategory>();

        return filtered
            .SelectMany(i => i.FailureNotes)
            .GroupBy(fn => fn.FailureType)
            .Select(g => new FailureCategory
            {
                Type = g.Key.ToString(),
                Count = g.Count(),
                Percentage = (decimal)g.Count() / filtered.Count * 100
            })
            .OrderByDescending(fc => fc.Count)
            .ToList();
    }
}

public record FailureCategory
{
    public string Type { get; init; }
    public int Count { get; init; }
    public decimal Percentage { get; init; }
}
```

### 5. Domain Events (16 events minimum)

**Inspection Events:**
- `InspectionPlannedEvent`
- `InspectionStartedEvent`
- `InspectionCompletedEvent`
- `InspectionFailedEvent`

**Ticket Events:**
- `TicketReportedEvent`
- `TicketAssignedEvent`
- `TicketStartedEvent`
- `TicketResolvedEvent`
- `TicketRejectedEvent`
- `TicketReopenedEvent`
- `TicketPriorityEscalatedEvent`

**Checkpoint Events:**
- `QACheckpointCreatedEvent`
- `QACheckpointDeactivatedEvent`
- `QACheckpointReactivatedEvent`
- `InspectionCriteriaAddedEvent`
- `InspectionCriteriaRemovedEvent`

---

## Unit Tests (70+ tests expected)

**QACheckpointTests.cs:**
- Create_ValidInput_Success
- AddCriteria_ValidCriteria_Success
- Deactivate_ActiveCheckpoint_Success

**InspectionTests.cs:**
- Create_ValidInput_CreatesPlannedInspection
- Start_PlannedInspection_TransitionsToInProgress
- Complete_InProgressInspection_TransitionsToCompleted
- Fail_InProgressInspection_RequiresFailureNotes
- Fail_WithoutFailureNotes_ThrowsException
- Start_CompletedInspection_ThrowsInvalidStateTransitionException

**InspectionStateMachineTests.cs:**
- ValidateTransition_PlannedToInProgress_Success
- ValidateTransition_InProgressToCompleted_Success
- ValidateTransition_PlannedToCompleted_ThrowsException
- ValidateTransition_CompletedToAnything_ThrowsException

**TicketTests.cs:**
- Create_ValidInput_CreatesReportedTicket
- Assign_ReportedTicket_TransitionsToAssigned
- Resolve_InProgressTicket_RequiresRootCause
- Resolve_WithShortRootCause_ThrowsException
- Reject_InProgressTicket_RequiresRejectionReason
- Reopen_RejectedTicket_TransitionsToReported

**TicketStateMachineTests.cs:**
- ValidateTransition_ReportedToAssigned_Success
- ValidateTransition_InProgressToResolved_Success
- ValidateTransition_RejectedToReported_Success
- ValidateTransition_ReportedToResolved_ThrowsException

**InspectionBlockingServiceTests.cs:**
- IsProductionBlocked_FailedCriticalInspection_ReturnsTrue
- IsProductionBlocked_FailedMinorInspection_ReturnsFalse
- IsProductionBlocked_PassedCriticalInspection_ReturnsFalse
- GetBlockingInspections_FiltersByOrderIdAndFail_ReturnsCorrectInspections

**RootCauseAnalysisServiceTests.cs:**
- AnalyzeRootCauses_GroupsByFailureType_ReturnsCorrectCategories
- AnalyzeRootCauses_CalculatesPercentages_ReturnsCorrectValues
- AnalyzeRootCauses_SortsByCount_ReturnsDescendingOrder

---

## Validation Rules

**Inspection:**
- Cannot complete/fail Planned inspection (must Start first)
- Failed inspections require ≥1 FailureNote
- InspectorEmployeeId must reference existing Employee (checked in app layer)

**Ticket:**
- CustomerDescription min 10 chars
- RootCause min 10 chars
- RejectionReason min 10 chars
- Cannot Resolve Reported ticket (must Assign first)

**QACheckpoint:**
- Name required, max 200 chars
- Name unique per tenant (checked in app layer via repository)

---

## Acceptance Criteria

- [ ] Domain layer folder structure created
- [ ] 3 aggregates implemented (QACheckpoint, Inspection, Ticket)
- [ ] 3 value objects implemented (InspectionCriteria, FailureNote, ResolutionAction)
- [ ] 9 enums implemented (InspectionResult, InspectionStatus, TicketType, TicketStatus, TicketPriority, CheckpointType, CriticalLevel, CriteriaType, FailureType, ActionType)
- [ ] 2 FSM validators implemented (InspectionStateMachine, TicketStateMachine)
- [ ] 3 domain services implemented (InspectionBlockingService, TicketRoutingService, RootCauseAnalysisService)
- [ ] 16 domain events implemented
- [ ] 3 repository interfaces implemented (IQACheckpointRepository, IInspectionRepository, ITicketRepository)
- [ ] 70+ unit tests implemented
- [ ] All tests PASS (xUnit)
- [ ] Build SUCCESS (0 errors, 0 warnings)
- [ ] Production blocking service validated (InspectionBlockingService)
- [ ] Pareto analysis service validated (RootCauseAnalysisService)

---

## Reference Documents

- **QA Domain Model:** `/opt/spaceos/docs/joinerytech/domain/QA_DOMAIN_MODEL.md` ← PRIMARY SOURCE
- **QA OpenAPI Spec:** `/opt/spaceos/spaceos-modules-qa/docs/openapi.yaml` (endpoint validation)
- **HR Domain Layer:** `/opt/spaceos/spaceos-modules-hr/Domain/` (structure template)
- **Maintenance Domain Layer:** `/opt/spaceos/spaceos-modules-maintenance/Domain/` (FSM pattern)
- **Contract-First Workflow:** ADR-050 (domain-first implementation)

---

## DONE Outbox Format

**File:** `2026-07-04_NNN_joinerytech-qa-week1-domain-layer-done.md`

**Frontmatter:**
```yaml
---
id: MSG-BACKEND-146-DONE
from: backend
to: conductor
type: done
status: UNREAD
ref: MSG-BACKEND-146
epic_id: EPIC-JT-QA
checkpoint_id: CP-QA-BACKEND
created: YYYY-MM-DD
---
```

**Tartalom:**
- Domain layer summary (aggregates, FSMs, domain services, tests)
- Production blocking service validation (InspectionBlockingService unit tests)
- Pareto analysis validation (RootCauseAnalysisService unit tests)
- Build result (errors, warnings, test count, pass rate)
- Files created: module structure
- Következő lépés: Week 2 Application Layer (CQRS handlers)

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
