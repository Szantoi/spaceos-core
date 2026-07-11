---
id: MSG-BACKEND-162
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
epic_id: EPIC-JT-QA
checkpoint_id: CP-QA-APPLICATION
estimated_nwt: 160
created: 2026-07-07
ref: MSG-CONDUCTOR-096
content_hash: 47737002c14ccd63813e2470ce175dfc0d55719a9e90bb5782039688344d337a
---

# QA Week 2 Application Layer — CQRS + API

## Context

**Domain Layer:** ✅ DONE (MSG-BACKEND-146, 73 tests PASS)

**QA modul domain:**
- 3 aggregates: QACheckpoint (~160 LOC), Inspection (~200 LOC), Ticket (~260 LOC)
- FSM: InspectionStatus (Planned → InProgress → Completed), TicketStatus (Reported → Assigned → InProgress → Resolved/Rejected)
- Enums: InspectionResult, TicketType, TicketPriority, CriticalLevel, CheckpointType, etc.
- Value Objects: InspectionCriteria, FailureNote, ResolutionAction, Money
- Domain Services: InspectionBlockingService (CRITICAL!), TicketRoutingService, RootCauseAnalysisService
- Repository Contracts: IQACheckpointRepository, IInspectionRepository, ITicketRepository

**Production Integration:** InspectionBlockingService.IsProductionBlocked() → Production module uses this!

**Pattern:** Follow DMS/HR/Maintenance Week 2 CQRS pattern (MediatR + FluentValidation + Minimal API)

---

## Task: Application Layer Implementation

### Phase 1: Commands (QACheckpoint) — 4 Commands + 4 Handlers

**QACheckpoint Commands:**
```csharp
// 1. CreateQACheckpointCommand
public record CreateQACheckpointCommand(
    string Name,                // max 200 chars
    CheckpointType Type,        // Final, InProcess
    CriticalLevel CriticalLevel, // Critical, Major, Minor
    string? Description,        // optional, max 1000 chars
    Guid TenantId
) : IRequest<Result<QACheckpointId>>;

// Handler: QACheckpoint.Create(name, type, criticalLevel, description, tenantId)

// 2. UpdateQACheckpointCommand
public record UpdateQACheckpointCommand(
    QACheckpointId CheckpointId,
    string Name,                // max 200 chars
    string? Description,        // optional, max 1000 chars
    Guid TenantId
) : IRequest<Result>;

// Handler: checkpoint.Update(name, description)

// 3. DeactivateQACheckpointCommand
public record DeactivateQACheckpointCommand(
    QACheckpointId CheckpointId,
    string Reason,              // max 500 chars
    Guid TenantId
) : IRequest<Result>;

// Handler: checkpoint.Deactivate(reason)

// 4. ReactivateQACheckpointCommand
public record ReactivateQACheckpointCommand(
    QACheckpointId CheckpointId,
    Guid TenantId
) : IRequest<Result>;

// Handler: checkpoint.Reactivate()
```

### Phase 2: Commands (Inspection) — 5 Commands + 5 Handlers

**Inspection Commands:**
```csharp
// 5. PlanInspectionCommand
public record PlanInspectionCommand(
    QACheckpointId CheckpointId,
    Guid OrderId,               // production order
    Guid? ProjectId,            // optional project link
    Guid InspectorId,           // user who will inspect
    DateTime ScheduledDate,
    Guid TenantId
) : IRequest<Result<InspectionId>>;

// Handler: Inspection.Plan(checkpointId, orderId, projectId, inspectorId, scheduledDate, tenantId)

// 6. StartInspectionCommand
public record StartInspectionCommand(
    InspectionId InspectionId,
    Guid TenantId
) : IRequest<Result>;

// Handler: inspection.Start()

// 7. CompleteInspectionWithPassCommand
public record CompleteInspectionWithPassCommand(
    InspectionId InspectionId,
    string? Notes,              // optional, max 2000 chars
    Guid TenantId
) : IRequest<Result>;

// Handler: inspection.CompleteWithPass(notes)

// 8. CompleteInspectionWithFailCommand
public record CompleteInspectionWithFailCommand(
    InspectionId InspectionId,
    FailureType FailureType,    // enum: Dimensional, Material, Cosmetic, etc.
    string FailureDescription,  // max 2000 chars
    string? CorrectiveAction,   // optional, max 2000 chars
    Guid TenantId
) : IRequest<Result>;

// Handler: inspection.CompleteWithFail(failureNote)
// FailureNote: new FailureNote(failureType, failureDescription, correctiveAction)

// 9. UpdateInspectionNotesCommand
public record UpdateInspectionNotesCommand(
    InspectionId InspectionId,
    string Notes,               // max 2000 chars
    Guid TenantId
) : IRequest<Result>;

// Handler: inspection.UpdateNotes(notes)
// Note: Only allowed for audit trail, result is immutable
```

### Phase 3: Commands (Ticket) — 7 Commands + 7 Handlers

**Ticket Commands:**
```csharp
// 10. ReportTicketCommand
public record ReportTicketCommand(
    TicketType Type,            // Warranty, Complaint, ChangeRequest
    TicketPriority Priority,    // Low, Medium, High, Critical
    string Title,               // max 200 chars
    string Description,         // max 2000 chars
    Guid? OrderId,              // optional order link
    Guid? CustomerId,           // optional customer link
    Guid TenantId
) : IRequest<Result<TicketId>>;

// Handler: Ticket.Report(type, priority, title, description, orderId, customerId, tenantId)

// 11. AssignTicketCommand
public record AssignTicketCommand(
    TicketId TicketId,
    Guid AssigneeId,            // user who will handle
    Guid TenantId
) : IRequest<Result>;

// Handler: ticket.Assign(assigneeId)

// 12. StartTicketCommand
public record StartTicketCommand(
    TicketId TicketId,
    Guid TenantId
) : IRequest<Result>;

// Handler: ticket.Start()

// 13. ResolveTicketCommand
public record ResolveTicketCommand(
    TicketId TicketId,
    ActionType ActionType,      // enum: Repair, Replace, Refund, Explanation
    string ActionDescription,   // max 2000 chars
    decimal? Cost,              // optional cost (Money)
    Guid TenantId
) : IRequest<Result>;

// Handler: ticket.Resolve(resolutionAction)
// ResolutionAction: new ResolutionAction(actionType, actionDescription, Money.FromDecimal(cost))

// 14. RejectTicketCommand
public record RejectTicketCommand(
    TicketId TicketId,
    string Reason,              // max 500 chars
    Guid TenantId
) : IRequest<Result>;

// Handler: ticket.Reject(reason)

// 15. ReopenTicketCommand
public record ReopenTicketCommand(
    TicketId TicketId,
    string Reason,              // max 500 chars
    Guid TenantId
) : IRequest<Result>;

// Handler: ticket.Reopen(reason)

// 16. EscalateTicketPriorityCommand
public record EscalateTicketPriorityCommand(
    TicketId TicketId,
    TicketPriority NewPriority, // escalate to higher priority
    string Reason,              // max 500 chars
    Guid TenantId
) : IRequest<Result>;

// Handler: ticket.EscalatePriority(newPriority, reason)
```

### Phase 4: Queries (QACheckpoint) — 3 Queries + 3 Handlers

```csharp
// 1. GetQACheckpointQuery
public record GetQACheckpointQuery(
    QACheckpointId CheckpointId,
    Guid TenantId
) : IRequest<Result<QACheckpointDto>>;

// Handler: IQACheckpointRepository.GetByIdAsync(checkpointId, tenantId)

// 2. GetActiveQACheckpointsQuery
public record GetActiveQACheckpointsQuery(
    CheckpointType? Type,       // optional filter
    Guid TenantId
) : IRequest<Result<QACheckpointDto[]>>;

// Handler: IQACheckpointRepository.GetActiveCheckpointsAsync(tenantId)
// Filter by Type if provided

// 3. GetQACheckpointsByTypeQuery
public record GetQACheckpointsByTypeQuery(
    CheckpointType Type,        // Final | InProcess
    Guid TenantId
) : IRequest<Result<QACheckpointDto[]>>;

// Handler: IQACheckpointRepository.GetByTypeAsync(type, tenantId)
```

### Phase 5: Queries (Inspection) — 6 Queries + 6 Handlers

```csharp
// 4. GetInspectionQuery
public record GetInspectionQuery(
    InspectionId InspectionId,
    Guid TenantId
) : IRequest<Result<InspectionDto>>;

// Handler: IInspectionRepository.GetByIdAsync(inspectionId, tenantId)

// 5. GetInspectionsQuery
public record GetInspectionsQuery(
    InspectionStatus? Status,   // optional filter
    InspectionResult? Result,   // optional filter
    int Page = 1,
    int PageSize = 20,
    Guid TenantId
) : IRequest<Result<InspectionListDto[]>>;

// Handler: IInspectionRepository with filters + pagination

// 6. GetPendingInspectionsQuery
public record GetPendingInspectionsQuery(
    Guid TenantId
) : IRequest<Result<InspectionDto[]>>;

// Handler: IInspectionRepository where Status == Planned

// 7. GetOrderInspectionsQuery
public record GetOrderInspectionsQuery(
    Guid OrderId,
    Guid TenantId
) : IRequest<Result<InspectionDto[]>>;

// Handler: IInspectionRepository.GetByOrderIdAsync(orderId, tenantId)

// 8. GetBlockingInspectionsQuery (CRITICAL: Production Integration)
public record GetBlockingInspectionsQuery(
    Guid OrderId,
    Guid TenantId
) : IRequest<Result<InspectionDto[]>>;

// Handler: IInspectionBlockingService.GetBlockingInspections(orderId, tenantId)
// Used by Production module to check if order can proceed

// 9. GetFailedInspectionsQuery
public record GetFailedInspectionsQuery(
    DateTime FromDate,
    DateTime ToDate,
    Guid TenantId
) : IRequest<Result<InspectionDto[]>>;

// Handler: IInspectionRepository where Result == Fail AND CompletedAt in range
```

### Phase 6: Queries (Ticket) — 6 Queries + 6 Handlers

```csharp
// 10. GetTicketQuery
public record GetTicketQuery(
    TicketId TicketId,
    Guid TenantId
) : IRequest<Result<TicketDto>>;

// Handler: ITicketRepository.GetByIdAsync(ticketId, tenantId)

// 11. GetTicketsQuery
public record GetTicketsQuery(
    TicketStatus? Status,       // optional filter
    TicketType? Type,           // optional filter
    TicketPriority? Priority,   // optional filter
    int Page = 1,
    int PageSize = 20,
    Guid TenantId
) : IRequest<Result<TicketListDto[]>>;

// Handler: ITicketRepository with filters + pagination

// 12. GetUnassignedTicketsQuery
public record GetUnassignedTicketsQuery(
    Guid TenantId
) : IRequest<Result<TicketDto[]>>;

// Handler: ITicketRepository where Status == Reported

// 13. GetAssigneeWorkloadQuery
public record GetAssigneeWorkloadQuery(
    Guid AssigneeId,
    Guid TenantId
) : IRequest<Result<TicketDto[]>>;

// Handler: ITicketRepository.GetAssigneeWorkloadAsync(assigneeId, tenantId)

// 14. GetWarrantyTicketsQuery
public record GetWarrantyTicketsQuery(
    Guid TenantId
) : IRequest<Result<TicketDto[]>>;

// Handler: ITicketRepository where Type == Warranty

// 15. GetRootCauseAnalysisQuery (CRITICAL: Pareto Analysis)
public record GetRootCauseAnalysisQuery(
    DateTime FromDate,
    DateTime ToDate,
    Guid TenantId
) : IRequest<Result<FailureAnalysisResultDto>>;

// Handler: IRootCauseAnalysisService.AnalyzeRootCauses(fromDate, toDate, tenantId)
// Returns Pareto 80/20 analysis
```

### Phase 7: FluentValidation Validators — 16 Validators

**QACheckpoint Validators:**
```csharp
// 1. CreateQACheckpointValidator
RuleFor(x => x.Name).NotEmpty().Length(1, 200);
RuleFor(x => x.Type).IsInEnum();
RuleFor(x => x.CriticalLevel).IsInEnum();
RuleFor(x => x.Description).MaximumLength(1000);

// 2. UpdateQACheckpointValidator
RuleFor(x => x.Name).NotEmpty().Length(1, 200);
RuleFor(x => x.Description).MaximumLength(1000);

// 3. DeactivateQACheckpointValidator
RuleFor(x => x.Reason).NotEmpty().Length(1, 500);

// 4. ReactivateQACheckpointValidator
// No special validation (CheckpointId + TenantId only)
```

**Inspection Validators:**
```csharp
// 5. PlanInspectionValidator
RuleFor(x => x.ScheduledDate).GreaterThanOrEqualTo(DateTime.Today).WithMessage("Scheduled date cannot be in the past");
RuleFor(x => x.InspectorId).NotEmpty();

// 6. StartInspectionValidator
// No special validation (InspectionId + TenantId only)

// 7. CompleteInspectionWithPassValidator
RuleFor(x => x.Notes).MaximumLength(2000);

// 8. CompleteInspectionWithFailValidator
RuleFor(x => x.FailureType).IsInEnum();
RuleFor(x => x.FailureDescription).NotEmpty().Length(1, 2000);
RuleFor(x => x.CorrectiveAction).MaximumLength(2000);

// 9. UpdateInspectionNotesValidator
RuleFor(x => x.Notes).NotEmpty().Length(1, 2000);
```

**Ticket Validators:**
```csharp
// 10. ReportTicketValidator
RuleFor(x => x.Type).IsInEnum();
RuleFor(x => x.Priority).IsInEnum();
RuleFor(x => x.Title).NotEmpty().Length(1, 200);
RuleFor(x => x.Description).NotEmpty().Length(1, 2000);

// 11. AssignTicketValidator
RuleFor(x => x.AssigneeId).NotEmpty();

// 12. StartTicketValidator
// No special validation (TicketId + TenantId only)

// 13. ResolveTicketValidator
RuleFor(x => x.ActionType).IsInEnum();
RuleFor(x => x.ActionDescription).NotEmpty().Length(1, 2000);
RuleFor(x => x.Cost).GreaterThanOrEqualTo(0).When(x => x.Cost.HasValue);

// 14. RejectTicketValidator
RuleFor(x => x.Reason).NotEmpty().Length(1, 500);

// 15. ReopenTicketValidator
RuleFor(x => x.Reason).NotEmpty().Length(1, 500);

// 16. EscalateTicketPriorityValidator
RuleFor(x => x.NewPriority).IsInEnum();
RuleFor(x => x.Reason).NotEmpty().Length(1, 500);
```

### Phase 8: DTOs — 14 DTOs

**Request DTOs (16):**
- CreateQACheckpointDto, UpdateQACheckpointDto, DeactivateQACheckpointDto, ReactivateQACheckpointDto
- PlanInspectionDto, StartInspectionDto, CompleteInspectionWithPassDto, CompleteInspectionWithFailDto, UpdateInspectionNotesDto
- ReportTicketDto, AssignTicketDto, StartTicketDto, ResolveTicketDto, RejectTicketDto, ReopenTicketDto, EscalateTicketPriorityDto

**Response DTOs (10):**
```csharp
// QACheckpointDto
public record QACheckpointDto(
    Guid Id,
    string Name,
    CheckpointType Type,
    CriticalLevel CriticalLevel,
    string? Description,
    bool IsActive,
    InspectionCriteriaDto[] Criteria,
    DateTime CreatedAt
);

// InspectionCriteriaDto
public record InspectionCriteriaDto(
    CriteriaType Type,
    string Description,
    string? AcceptableRange
);

// InspectionDto
public record InspectionDto(
    Guid Id,
    Guid CheckpointId,
    string CheckpointName,      // denormalized
    Guid OrderId,
    Guid? ProjectId,
    Guid InspectorId,
    InspectionStatus Status,
    InspectionResult Result,
    DateTime ScheduledDate,
    DateTime? CompletedAt,
    FailureNoteDto? FailureNote,
    string? Notes,
    DateTime CreatedAt
);

// InspectionListDto (lighter for pagination)
public record InspectionListDto(
    Guid Id,
    Guid CheckpointId,
    string CheckpointName,
    Guid OrderId,
    InspectionStatus Status,
    InspectionResult Result,
    DateTime ScheduledDate
);

// FailureNoteDto
public record FailureNoteDto(
    FailureType Type,
    string Description,
    string? CorrectiveAction
);

// TicketDto
public record TicketDto(
    Guid Id,
    TicketType Type,
    TicketPriority Priority,
    TicketStatus Status,
    string Title,
    string Description,
    Guid? OrderId,
    Guid? CustomerId,
    Guid? AssigneeId,
    ResolutionActionDto? Resolution,
    DateTime? ReportedAt,
    DateTime? ResolvedAt,
    DateTime CreatedAt
);

// TicketListDto (lighter for pagination)
public record TicketListDto(
    Guid Id,
    TicketType Type,
    TicketPriority Priority,
    TicketStatus Status,
    string Title,
    DateTime CreatedAt
);

// ResolutionActionDto
public record ResolutionActionDto(
    ActionType Type,
    string Description,
    decimal? Cost
);

// FailureAnalysisResultDto (Pareto analysis)
public record FailureAnalysisResultDto(
    FailureCategoryDto[] Categories,
    int TotalFailures,
    DateTime AnalyzedFrom,
    DateTime AnalyzedTo
);

// FailureCategoryDto
public record FailureCategoryDto(
    FailureType Type,
    int Count,
    decimal Percentage,
    decimal CumulativePercentage
);
```

### Phase 9: API Endpoints — 30 Endpoints (Deferred to Host)

**QACheckpoint Endpoints (7):**
```
POST   /api/qa/checkpoints                     CreateQACheckpoint
GET    /api/qa/checkpoints/{id}                GetQACheckpoint
PUT    /api/qa/checkpoints/{id}                UpdateQACheckpoint
POST   /api/qa/checkpoints/{id}/deactivate     DeactivateQACheckpoint
POST   /api/qa/checkpoints/{id}/reactivate     ReactivateQACheckpoint
GET    /api/qa/checkpoints/active              GetActiveQACheckpoints
GET    /api/qa/checkpoints/by-type/{type}      GetQACheckpointsByType
```

**Inspection Endpoints (11):**
```
POST   /api/qa/inspections                     PlanInspection
GET    /api/qa/inspections/{id}                GetInspection
GET    /api/qa/inspections                     GetInspections (pagination + filters)
GET    /api/qa/inspections/pending             GetPendingInspections
GET    /api/qa/inspections/order/{orderId}     GetOrderInspections
GET    /api/qa/inspections/blocking/{orderId}  GetBlockingInspections (Production!)
GET    /api/qa/inspections/failed              GetFailedInspections
POST   /api/qa/inspections/{id}/start          StartInspection
POST   /api/qa/inspections/{id}/complete-pass  CompleteInspectionWithPass
POST   /api/qa/inspections/{id}/complete-fail  CompleteInspectionWithFail
PUT    /api/qa/inspections/{id}/notes          UpdateInspectionNotes
```

**Ticket Endpoints (12):**
```
POST   /api/qa/tickets                         ReportTicket
GET    /api/qa/tickets/{id}                    GetTicket
GET    /api/qa/tickets                         GetTickets (pagination + filters)
GET    /api/qa/tickets/unassigned              GetUnassignedTickets
GET    /api/qa/tickets/warranty                GetWarrantyTickets
GET    /api/qa/tickets/assignee/{id}/workload  GetAssigneeWorkload
GET    /api/qa/tickets/root-cause-analysis     GetRootCauseAnalysis (Pareto!)
POST   /api/qa/tickets/{id}/assign             AssignTicket
POST   /api/qa/tickets/{id}/start              StartTicket
POST   /api/qa/tickets/{id}/resolve            ResolveTicket
POST   /api/qa/tickets/{id}/reject             RejectTicket
POST   /api/qa/tickets/{id}/reopen             ReopenTicket
POST   /api/qa/tickets/{id}/escalate           EscalateTicketPriority
```

**Note:** API endpoints deferred to host project (like HR/Maintenance Week 2 pattern).

### Phase 10: Integration Tests — 45+ Tests (Deferred to Host)

**Test Categories:**
- QACheckpoint lifecycle tests (create, update, deactivate, reactivate)
- Inspection FSM tests (Planned → InProgress → Completed)
- Inspection completion tests (Pass vs Fail)
- Production blocking tests (CRITICAL: GetBlockingInspections)
- Ticket FSM tests (Reported → Assigned → InProgress → Resolved/Rejected)
- Ticket escalation tests (warranty auto-escalate after 24h)
- Root cause analysis tests (Pareto 80/20)
- Validation tests (FluentValidation rules)

**Note:** Integration tests with Testcontainers deferred to host project.

---

## Acceptance Criteria

✅ **48 CQRS handlers implemented** (16 commands + 15 queries × 2 handlers each = 62 files)
✅ **16 FluentValidation validators**
✅ **14 DTOs** (16 request + 10 response, including nested DTOs)
⚠️ **30 API endpoints** (deferred to host)
⚠️ **45+ integration tests** (deferred to host)
✅ **Build: 0 errors, 0 warnings**
⚠️ **OpenAPI spec** (requires API endpoints in host)

---

## NuGet Dependencies

```xml
<PackageReference Include="MediatR" Version="12.4.1" />
<PackageReference Include="Ardalis.Result" Version="10.1.0" />
<PackageReference Include="FluentValidation" Version="11.10.0" />
```

---

## Critical Implementation Notes

### 1. Production Blocking Integration (CRITICAL!)

**GetBlockingInspectionsQuery** must use InspectionBlockingService:

```csharp
// In GetBlockingInspectionsQueryHandler:
var inspections = await _inspectionRepository.GetByOrderIdAsync(orderId, tenantId);
var blockingInspections = _inspectionBlockingService.GetBlockingInspections(inspections);

return blockingInspections.Select(i => new InspectionDto(...)).ToArray();
```

**Production module uses this to check if order can proceed!**

### 2. FSM Enforcement

**InspectionStatusTransitions:**
```
Planned → InProgress → Completed (terminal state)
```

**TicketStatusTransitions:**
```
Reported → Assigned | Rejected
Assigned → InProgress | Rejected
InProgress → Resolved | Rejected
Resolved → Reopened
Rejected → Reopened
Reopened → Assigned
```

### 3. Pareto Analysis (Root Cause Analysis)

**IRootCauseAnalysisService.AnalyzeRootCauses():**
- Returns failures sorted by frequency (descending)
- Calculates cumulative percentages
- Identifies top 20% failure types causing 80% of issues
- Used for quality improvement prioritization

### 4. Ticket Auto-Escalation

**TicketRoutingService.CalculatePriorityBoost():**
- Warranty tickets auto-escalate after 24 hours
- Priority: Low → Medium, Medium → High, High → Critical
- Logic: `if (ticket.Type == Warranty && (DateTime.Now - ticket.ReportedAt).Hours > 24) { boost++; }`

### 5. Value Object Patterns

**FailureNote:**
```csharp
var failureNote = new FailureNote(
    failureType,
    failureDescription,
    correctiveAction
);
```

**ResolutionAction:**
```csharp
var resolutionAction = new ResolutionAction(
    actionType,
    actionDescription,
    Money.FromDecimal(cost ?? 0)
);
```

---

## Security Patterns

✅ **FluentValidation** input validation on all commands
✅ **Domain invariants** enforced through aggregate methods
✅ **FSM state transitions** validated (InspectionStatusTransitions, TicketStatusTransitions)
✅ **Repository interfaces** for data access (RLS ready)
✅ **Result pattern** for error handling (Ardalis.Result)
✅ **TenantId** mandatory in all commands/queries

---

## Build & Test

```bash
cd /opt/spaceos/backend/spaceos-modules/spaceos-modules-qa
dotnet build

# Expected:
# Build succeeded.
#     0 Warning(s)
#     0 Error(s)
```

**Note:** Integration tests deferred to host project (like HR/Maintenance Week 2 pattern).

---

## Next Steps (Week 3)

1. **Infrastructure Layer** — EF Core repositories, DbContext, migrations
2. **API Layer** — Minimal API endpoints in host project
3. **Integration Tests** — E2E tests with Testcontainers

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
