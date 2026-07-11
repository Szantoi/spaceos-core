---
id: MSG-BACKEND-189
from: conductor
to: backend
type: task
priority: high
status: UNREAD
model: sonnet
epic_id: EPIC-JT-EHS
estimated_nwt: 120
ref: MSG-BACKEND-188
created: 2026-07-08
content_hash: 88892622590fe063ddcb0d70d9e686c55d61d088dd50eb161956088e87d6d07d
---

# EHS Week 2: Application Layer (CQRS Handlers, FluentValidation, DTOs)

**Epic:** EPIC-JT-EHS (JoineryTech Munkavédelem/Safety Module)
**Pattern:** Proven Week 2 Application Layer (6 modules reference)
**Dependencies:** Week 1 Domain Layer ✅ DONE (MSG-BACKEND-188, 34 tests passing)

---

## Task Summary

Implement the **Application Layer** for the EHS module following the established Week 1-4 pattern:
- **CQRS Commands and Queries** (MediatR pipeline)
- **Command/Query Handlers** with domain aggregate orchestration
- **FluentValidation** rules for all commands
- **DTOs** for request/response
- **Application Contracts** (repository interfaces)

**Estimated NWT:** 120 (~4-6 hours)

---

## References

**Week 0 OpenAPI Spec:** `/opt/spaceos/spaceos-modules-ehs/docs/openapi.yaml` (MSG-ARCHITECT-073-DONE)
- 23 endpoints across 3 aggregates
- Incident FSM workflow (5 states)
- 5×5 Risk Matrix calculation
- Training expiry logic

**Week 1 Domain Layer:** `/opt/spaceos/spaceos-modules-ehs/src/Domain/` (MSG-BACKEND-188-DONE)
- 3 aggregates: Incident, RiskAssessment, TrainingRecord
- 11 domain events
- 34 unit tests ✅ PASSING

**Pattern Reference:** Use identical structure from completed modules:
- `/opt/spaceos/spaceos-modules-crm/src/Application/`
- `/opt/spaceos/spaceos-modules-kontrolling/src/Application/`
- `/opt/spaceos/spaceos-modules-hr/src/Application/`
- `/opt/spaceos/spaceos-modules-maintenance/src/Application/`
- `/opt/spaceos/spaceos-modules-qa/src/Application/`
- `/opt/spaceos/spaceos-modules-dms/src/Application/`

---

## Scope: 3 Aggregates × Application Layer

### 1. Incident Application Layer

**Commands (7):**
1. `CreateIncidentCommand` + `CreateIncidentCommandHandler`
   - Input: IncidentType, Severity, Location, Description, ReportedBy
   - Output: IncidentId
   - Domain: `Incident.Create()`

2. `StartInvestigationCommand` + `StartInvestigationCommandHandler`
   - Input: IncidentId, InvestigatedBy, InvestigationNotes
   - FSM Transition: Reported → Investigated
   - Domain: `incident.StartInvestigation()`

3. `AddInvestigationFindingsCommand` + Handler
   - Input: IncidentId, Findings
   - Domain: `incident.AddInvestigationFindings()`

4. `AddCorrectiveActionCommand` + Handler
   - Input: IncidentId, ActionDescription, ResponsiblePerson, DueDate
   - FSM Transition: Investigated → CorrectiveActionPlanned
   - Domain: `incident.AddCorrectiveAction()`

5. `CloseIncidentCommand` + Handler
   - Input: IncidentId, ClosureNotes
   - FSM Transition: CorrectiveActionPlanned → Closed
   - Domain: `incident.CloseIncident()`

6. `ReopenIncidentCommand` + Handler
   - Input: IncidentId, ReopenReason
   - FSM Transition: Closed → Reopened
   - Domain: `incident.ReopenIncident()`

7. `AddWitnessCommand` + Handler
   - Input: IncidentId, WitnessName, WitnessStatement
   - Domain: `incident.AddWitness()`

**Queries (4):**
1. `GetIncidentByIdQuery` + Handler → IncidentDto
2. `ListIncidentsQuery` + Handler → List<IncidentDto>
   - Filters: IncidentType, Status, DateRange, Severity
3. `GetIncidentSummaryQuery` + Handler → IncidentSummaryDto
   - Aggregation: counts by type, severity, status
4. `GetIncidentTrendsQuery` + Handler → IncidentTrendsDto
   - Monthly aggregation for last 12 months

**DTOs (4):**
- `IncidentDto` — full incident details
- `IncidentSummaryDto` — counts by type/severity/status
- `IncidentTrendsDto` — monthly trend data
- `IncidentListItemDto` — list view (lighter)

**FluentValidation (7 validators):**
- `CreateIncidentCommandValidator` — required fields, enum validation
- `StartInvestigationCommandValidator` — non-empty investigatedBy, notes
- `AddInvestigationFindingsCommandValidator` — non-empty findings
- `AddCorrectiveActionCommandValidator` — required action, responsible, due date
- `CloseIncidentCommandValidator` — non-empty closure notes
- `ReopenIncidentCommandValidator` — non-empty reopen reason
- `AddWitnessCommandValidator` — witness name + statement validation

---

### 2. RiskAssessment Application Layer

**Commands (4):**
1. `CreateRiskAssessmentCommand` + Handler
   - Input: Location, Activity, Hazards, Severity, Likelihood
   - Auto-calculation: RiskScore = Severity × Likelihood
   - Auto-calculation: RiskLevel (Low/Medium/High)
   - Domain: `RiskAssessment.Create()`

2. `UpdateRiskAssessmentCommand` + Handler
   - Input: RiskAssessmentId, Hazards, Severity, Likelihood
   - Recalculate: RiskScore, RiskLevel
   - Domain: entity update + recalculation

3. `AddRiskControlCommand` + Handler
   - Input: RiskAssessmentId, ControlMeasure, ResponsiblePerson
   - Domain: `riskAssessment.AddControl()`

4. `ArchiveRiskAssessmentCommand` + Handler
   - Input: RiskAssessmentId
   - Domain: `riskAssessment.Archive()`

**Queries (3):**
1. `GetRiskAssessmentByIdQuery` + Handler → RiskAssessmentDto
2. `ListRiskAssessmentsQuery` + Handler → List<RiskAssessmentDto>
   - Filters: RiskLevel, ReviewDue, Status
3. `GetRiskMatrixQuery` + Handler → RiskMatrixDto
   - 5×5 matrix aggregation (counts by Severity × Likelihood)

**DTOs (3):**
- `RiskAssessmentDto` — full details + controls
- `RiskMatrixDto` — 5×5 matrix cell counts for heat map
- `RiskAssessmentListItemDto` — list view

**FluentValidation (4 validators):**
- `CreateRiskAssessmentCommandValidator` — required fields, enum ranges
- `UpdateRiskAssessmentCommandValidator` — severity/likelihood 1-5
- `AddRiskControlCommandValidator` — non-empty measure + responsible
- `ArchiveRiskAssessmentCommandValidator` — ID validation

---

### 3. TrainingRecord Application Layer

**Commands (2):**
1. `CreateTrainingRecordCommand` + Handler
   - Input: EmployeeId, TrainingType, CompletionDate, ExpiresAt
   - Auto-calculation: TrainingStatus (Valid/Expiring/Expired)
   - Domain: `TrainingRecord.Create()`

2. `RenewTrainingRecordCommand` + Handler
   - Input: TrainingRecordId, NewCompletionDate, NewExpiresAt
   - Immutability: creates NEW record, references old
   - Domain: `trainingRecord.Renew()`

**Queries (3):**
1. `GetTrainingRecordByIdQuery` + Handler → TrainingRecordDto
2. `ListTrainingRecordsQuery` + Handler → List<TrainingRecordDto>
   - Filters: EmployeeId, TrainingStatus, ExpiresAtRange
3. `GetExpiringTrainingsQuery` + Handler → List<TrainingRecordDto>
   - Filter: TrainingStatus = Expiring (≤30 days)

**DTOs (2):**
- `TrainingRecordDto` — full details
- `TrainingRecordListItemDto` — list view

**FluentValidation (2 validators):**
- `CreateTrainingRecordCommandValidator` — required fields, date logic
- `RenewTrainingRecordCommandValidator` — renewal date > completion date

---

## Application Layer Structure

```
SpaceOS.Modules.EHS.Application/
  Incidents/
    Commands/
      CreateIncident/
        CreateIncidentCommand.cs
        CreateIncidentCommandHandler.cs
        CreateIncidentCommandValidator.cs
      StartInvestigation/
        StartInvestigationCommand.cs
        StartInvestigationCommandHandler.cs
        StartInvestigationCommandValidator.cs
      (... 5 more command folders ...)
    Queries/
      GetIncidentById/
        GetIncidentByIdQuery.cs
        GetIncidentByIdQueryHandler.cs
      ListIncidents/
        ListIncidentsQuery.cs
        ListIncidentsQueryHandler.cs
      (... 2 more query folders ...)
    DTOs/
      IncidentDto.cs
      IncidentSummaryDto.cs
      IncidentTrendsDto.cs
      IncidentListItemDto.cs

  RiskAssessments/
    Commands/
      (... 4 command folders ...)
    Queries/
      (... 3 query folders ...)
    DTOs/
      (... 3 DTOs ...)

  TrainingRecords/
    Commands/
      (... 2 command folders ...)
    Queries/
      (... 3 query folders ...)
    DTOs/
      (... 2 DTOs ...)

  Contracts/
    IIncidentRepository.cs
    IRiskAssessmentRepository.cs
    ITrainingRecordRepository.cs
    IEhsNotificationService.cs (optional, for alerts)

  Common/
    Mapping/
      EhsMappingProfile.cs (AutoMapper)
```

---

## Implementation Guidelines

### CQRS Pattern (MediatR)

**Command Example:**
```csharp
public record CreateIncidentCommand(
    IncidentType Type,
    Severity Severity,
    string Location,
    string Description,
    string ReportedBy,
    DateTime OccurredAt,
    Guid TenantId
) : IRequest<Guid>;

public class CreateIncidentCommandHandler : IRequestHandler<CreateIncidentCommand, Guid>
{
    private readonly IIncidentRepository _repository;

    public async Task<Guid> Handle(CreateIncidentCommand request, CancellationToken ct)
    {
        var incident = Incident.Create(
            request.Type,
            request.Severity,
            request.Location,
            request.Description,
            request.ReportedBy,
            request.OccurredAt,
            request.TenantId
        );

        await _repository.AddAsync(incident, ct);
        return incident.Id;
    }
}
```

**Query Example:**
```csharp
public record GetIncidentByIdQuery(Guid IncidentId, Guid TenantId) : IRequest<IncidentDto>;

public class GetIncidentByIdQueryHandler : IRequestHandler<GetIncidentByIdQuery, IncidentDto>
{
    private readonly IIncidentRepository _repository;
    private readonly IMapper _mapper;

    public async Task<IncidentDto> Handle(GetIncidentByIdQuery request, CancellationToken ct)
    {
        var incident = await _repository.GetByIdAsync(request.IncidentId, request.TenantId, ct);
        return _mapper.Map<IncidentDto>(incident);
    }
}
```

### FluentValidation Pattern

```csharp
public class CreateIncidentCommandValidator : AbstractValidator<CreateIncidentCommand>
{
    public CreateIncidentCommandValidator()
    {
        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Invalid incident type");

        RuleFor(x => x.Severity)
            .IsInEnum()
            .InclusiveBetween(Severity.Negligible, Severity.Catastrophic)
            .WithMessage("Severity must be between 1 and 5");

        RuleFor(x => x.Location)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.ReportedBy)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.OccurredAt)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Incident occurrence date cannot be in the future");
    }
}
```

### Repository Contracts

```csharp
public interface IIncidentRepository
{
    Task<Incident?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<List<Incident>> ListAsync(IncidentFilter filter, Guid tenantId, CancellationToken ct = default);
    Task AddAsync(Incident incident, CancellationToken ct = default);
    Task UpdateAsync(Incident incident, CancellationToken ct = default);
}

public record IncidentFilter(
    IncidentType? Type = null,
    IncidentStatus? Status = null,
    DateRange? OccurredAtRange = null,
    Severity? MinSeverity = null
);
```

---

## Acceptance Criteria

**Quality Gates:**
1. ✅ **All commands/queries implement `IRequest<T>` interface**
2. ✅ **All handlers implement `IRequestHandler<TRequest, TResponse>`**
3. ✅ **All commands have FluentValidation validators**
4. ✅ **DTOs use `record` types (immutability)**
5. ✅ **AutoMapper profile maps Domain → DTOs**
6. ✅ **Repository contracts defined (no implementation yet — Week 3)**
7. ✅ **Zero compilation errors, zero warnings**
8. ✅ **Pattern consistency with other 6 modules verified**

**File Counts (estimated):**
- Commands: 13 command classes + 13 handlers + 13 validators = 39 files
- Queries: 10 query classes + 10 handlers = 20 files
- DTOs: ~12 DTOs
- Contracts: 4 interfaces
- Mapping: 1 AutoMapper profile
- **Total:** ~76 files, ~3500-4000 lines

**Testing:**
- **Unit tests NOT required for Week 2** (Application layer tests come in Week 4 with full integration tests)
- Compilation verification only: `dotnet build --no-restore`

---

## Success Criteria

**DONE when:**
1. ✅ All 13 commands + handlers + validators implemented
2. ✅ All 10 queries + handlers implemented
3. ✅ All ~12 DTOs created (record types)
4. ✅ All 4 repository contracts defined
5. ✅ AutoMapper profile created
6. ✅ `dotnet build` → 0 errors, 0 warnings
7. ✅ Pattern consistency verified against 6 reference modules
8. ✅ OpenAPI spec endpoints coverage confirmed (23 endpoints mapped)

**DONE Outbox Checklist:**
- [ ] Summary: "EHS Week 2 Application Layer complete — 13 commands, 10 queries, 12 DTOs, 4 repository contracts, 0 errors"
- [ ] File count: ~76 files
- [ ] Lines written: ~3500-4000 lines
- [ ] Build verification: `dotnet build` output
- [ ] Pattern consistency: verified against CRM/Kontrolling/HR/Maintenance/QA/DMS

---

## Next Steps (Week 3-4)

**Week 3: Infrastructure Layer** (scheduled after Week 2 DONE)
- EF Core DbContext + configurations
- Repository implementations
- RLS policies
- Migrations

**Week 4: API Layer** (scheduled after Week 3 DONE)
- Minimal API endpoints
- MediatR pipeline integration
- Testcontainers integration tests

---

**Priority:** High (EHS Week 2/4 critical path for EPIC-JT-EHS completion)
**Model:** Sonnet (proven Week 2 Application Layer model across 6 modules)
**Estimated NWT:** 120 (~4-6 hours, proven pattern)

📊 **Generated by Conductor** — EHS Week 2 Application Layer Dispatch

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
