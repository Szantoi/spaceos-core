---
id: MSG-ARCHITECT-063
from: conductor
to: architect
type: task
priority: high
status: READ
model: opus
epic_id: EPIC-JT-QA
checkpoint_id: CP-QA-BACKEND
created: 2026-07-04
estimated_nwt: 180
content_hash: b2f272e0f8d5b6d811594cfa6adb69a7d746404d8739e2dda92e20f872549373
---

# JoineryTech QA Domain Model — DDD Design Specification

**Epic:** EPIC-JT-QA (Minőségbiztosítás)
**Estimated:** 180 NWT (~6 hours)
**Priority:** High (unblocked epic, production quality control critical)

---

## Context

Az EPIC-JT-QA (Quality Assurance) aktiválása folyamatban. A Contract-First development workflow szerint **minden epic aktiválás ELŐTT** szükséges a domain model tervezése.

**Prototípus:** JoineryTech prototípus volt QA/reklamáció modul (migrálandó)
**Integration:** Production module (blocking on failed inspection)

**Most a Domain Model DDD specifikációt kell elkészítened.**

---

## Deliverables

**File:** `/opt/spaceos/docs/joinerytech/domain/QA_DOMAIN_MODEL.md`

**Minimum tartalom (referencia: HR_DOMAIN_MODEL.md, MAINTENANCE_DOMAIN_MODEL.md):**

### 1. Executive Summary
- QA domain felelősség
- Inspection workflow
- Ticket FSM (reklamáció/garancia/hiánypótlás)
- Production integration (inspection blocking)
- Key design principles (FSM enforcement, immutability, audit trail)

### 2. Aggregate Roots

**QACheckpoint Aggregate:**
- Responsibility: Quality control checkpoint definition (pl. "Festés előtt szemes ellenőrzés")
- Properties: Name, Type (Incoming/InProcess/Final), Criteria, CriticalLevel
- Methods: Create, Update, Deactivate

**Inspection Aggregate:**
- Responsibility: Inspection execution with pass/fail result
- Properties: CheckpointId, InspectorEmployeeId, InspectionDate, Result (Pass/Fail), Notes
- FSM: Planned → InProgress → Completed/Failed
- Methods: Create, Start, Complete, Fail
- Invariants: Cannot complete without InspectorEmployeeId, Failed inspections require FailureNotes

**Ticket Aggregate (Reklamáció):**
- Responsibility: Customer complaint/warranty/repair request workflow
- Properties: TicketType (Warranty/Repair/Missing), Priority, OrderId, ProjectId, CustomerDescription, RootCause, Resolution
- FSM: Reported → Assigned → InProgress → Resolved/Rejected
- Methods: Create, Assign, StartWork, Resolve, Reject, Reopen
- Invariants: Cannot resolve without Resolution notes, Rejection requires RejectionReason

### 3. Value Objects

**InspectionCriteria:**
- Type (Visual/Dimensional/Functional)
- Description
- AcceptanceThreshold (string, e.g., "Max 2mm gap")

**FailureNote:**
- FailureType (Scratch/Gap/Misalignment/etc.)
- Description
- PhotoUrl (optional)

**ResolutionAction:**
- ActionType (Repair/Replace/Refund/NoAction)
- Description
- Cost (Money)

### 4. Enums

**InspectionResult:**
- Pass
- Fail
- Conditional (pass with minor notes)

**TicketType:**
- Warranty (garancia)
- Repair (hiánypótlás)
- Missing (hiányzó alkatrész)

**TicketStatus:**
- Reported
- Assigned
- InProgress
- Resolved
- Rejected

**TicketPriority:**
- Critical (production blocking)
- High
- Medium
- Low

**CheckpointType:**
- Incoming (beszállított anyag)
- InProcess (gyártás közben)
- Final (kiszállítás előtt)

### 5. Domain Services

**InspectionBlockingService:**
- Responsibility: Determine if failed inspection blocks production
- Method: `bool IsProductionBlocked(Inspection inspection, QACheckpoint checkpoint)`
- Logic:
  - Critical checkpoint + Fail result → block production
  - Non-critical checkpoint + Fail result → warning only
- **Production Integration:** This service is queried by Production module!

**TicketRoutingService:**
- Responsibility: Auto-assign tickets based on type and priority
- Method: `EmployeeId? SuggestAssignee(Ticket ticket)`
- Logic: Route to warranty team, repair team, or production manager

**RootCauseAnalysisService:**
- Responsibility: Categorize failure patterns for Pareto analysis
- Method: `IEnumerable<FailureCategory> AnalyzeRootCauses(IEnumerable<Inspection> failedInspections, DateOnly startDate, DateOnly endDate)`

### 6. Domain Events

**Inspection Events:**
- InspectionPlannedEvent
- InspectionStartedEvent
- InspectionCompletedEvent (Pass)
- InspectionFailedEvent (Fail, triggers production blocking check)

**Ticket Events:**
- TicketReportedEvent
- TicketAssignedEvent
- TicketStartedEvent
- TicketResolvedEvent
- TicketRejectedEvent
- TicketReopenedEvent

**Checkpoint Events:**
- CheckpointCreatedEvent
- CheckpointUpdatedEvent
- CheckpointDeactivatedEvent

### 7. Repository Contracts

**IQACheckpointRepository:**
- GetByIdAsync
- GetActiveByTypeAsync (CheckpointType)
- GetCriticalCheckpointsAsync
- AddAsync, UpdateAsync

**IInspectionRepository:**
- GetByIdAsync
- GetByCheckpointAsync
- GetFailedInspectionsAsync (date range, for root cause analysis)
- GetPendingInspectionsAsync
- AddAsync, UpdateAsync

**ITicketRepository:**
- GetByIdAsync
- GetByStatusAsync
- GetByCustomerAsync (OrderId or ProjectId)
- GetOverdueTicketsAsync
- AddAsync, UpdateAsync

### 8. FSM State Machines

**Inspection FSM:**
- Planned → InProgress (Start)
- InProgress → Completed (Complete with Pass result)
- InProgress → Failed (Fail with FailureNotes)
- No backwards transitions

**Ticket FSM:**
- Reported → Assigned (Assign to employee)
- Assigned → InProgress (StartWork)
- InProgress → Resolved (Resolve with ResolutionAction)
- InProgress → Rejected (Reject with RejectionReason)
- Rejected → Reported (Reopen)
- No other backwards transitions

### 9. Integration Boundaries

**Production Integration (CRITICAL!):**
- Production queries `/api/qa/inspections/blocking` to get failed critical inspections
- If any InProgress production order has failed critical inspection → block production
- Inspection.Result = Fail + Checkpoint.CriticalLevel = Critical → production STOP

**Order/Project Integration:**
- Ticket links to OrderId or ProjectId
- Tickets can be created from customer complaints or internal QA findings

**HR Integration:**
- InspectorEmployeeId reference to HR module
- Ticket AssigneeEmployeeId reference to HR module

### 10. Validation Rules

**Inspection:**
- Inspector must be assigned before completing
- Failed inspections require FailureNotes (min 10 characters)
- Cannot fail Planned inspection (must start first)

**Ticket:**
- Resolution requires ResolutionAction and description
- Rejection requires RejectionReason (min 10 characters)
- Cannot resolve Reported ticket (must assign first)

**Checkpoint:**
- Name must be unique per tenant
- Critical checkpoints require explicit CriticalLevel = Critical flag

---

## Production Integration Pattern (CRITICAL!)

```csharp
// Production module calls this to check if production is blocked
GET /api/qa/inspections/blocking?orderId={orderId}

// Returns:
{
  "isBlocked": true,
  "blockingInspections": [
    {
      "inspectionId": "guid",
      "checkpointName": "Festés minőség",
      "failureNotes": "3 db karcolás észlelve",
      "inspectionDate": "2026-07-04"
    }
  ]
}

// Domain Service logic:
public class InspectionBlockingService : IInspectionBlockingService
{
    public bool IsProductionBlocked(Inspection inspection, QACheckpoint checkpoint)
    {
        return inspection.Result == InspectionResult.Fail
            && checkpoint.CriticalLevel == CriticalLevel.Critical;
    }

    public IEnumerable<Inspection> GetBlockingInspections(Guid orderId, IEnumerable<Inspection> inspections)
    {
        return inspections.Where(i =>
            i.OrderId == orderId
            && i.Result == InspectionResult.Fail
            && i.Checkpoint.CriticalLevel == CriticalLevel.Critical);
    }
}
```

---

## Pareto Analysis Pattern (Root Cause)

**Use case:** Kontrolling dashboard "Top 10 minőségi hiba oka"

```csharp
public class RootCauseAnalysisService : IRootCauseAnalysisService
{
    public IEnumerable<FailureCategory> AnalyzeRootCauses(
        IEnumerable<Inspection> failedInspections,
        DateOnly startDate,
        DateOnly endDate)
    {
        var failures = failedInspections
            .Where(i => i.InspectionDate >= startDate && i.InspectionDate <= endDate)
            .SelectMany(i => i.FailureNotes)
            .GroupBy(fn => fn.FailureType)
            .Select(g => new FailureCategory
            {
                Type = g.Key,
                Count = g.Count(),
                Percentage = g.Count() / (decimal)failedInspections.Count() * 100
            })
            .OrderByDescending(fc => fc.Count)
            .ToList();

        return failures;
    }
}
```

---

## Acceptance Criteria

- [ ] QA_DOMAIN_MODEL.md created in `/opt/spaceos/docs/joinerytech/domain/`
- [ ] 3 aggregates defined (QACheckpoint, Inspection, Ticket)
- [ ] 3+ value objects defined (InspectionCriteria, FailureNote, ResolutionAction)
- [ ] 5 enums defined (InspectionResult, TicketType, TicketStatus, TicketPriority, CheckpointType)
- [ ] 3 domain services defined (InspectionBlockingService, TicketRoutingService, RootCauseAnalysisService)
- [ ] 12+ domain events defined
- [ ] 3 repository contracts defined
- [ ] 2 FSM state machines defined (Inspection, Ticket)
- [ ] Production integration pattern documented (blocking inspections)
- [ ] Pareto analysis pattern documented (root cause analysis)
- [ ] Table of Contents with navigation links
- [ ] Implementation Guide section (coding patterns, examples)

---

## Reference Documents

- **HR Domain Model:** `/opt/spaceos/docs/joinerytech/domain/HR_DOMAIN_MODEL.md` (structure template)
- **Maintenance Domain Model:** `/opt/spaceos/docs/joinerytech/domain/MAINTENANCE_DOMAIN_MODEL.md` (FSM patterns)
- **Kontrolling Domain Model:** ADR-055 (integration patterns)
- **DDD Patterns:** `/opt/spaceos/docs/knowledge/patterns/BACKEND_PATTERNS.md`

---

## DONE Outbox Format

**File:** `2026-07-04_NNN_joinerytech-qa-domain-model-done.md`

**Frontmatter:**
```yaml
---
id: MSG-ARCHITECT-063-DONE
from: architect
to: conductor
type: done
status: READ
ref: MSG-ARCHITECT-063
epic_id: EPIC-JT-QA
checkpoint_id: CP-QA-BACKEND
created: YYYY-MM-DD
---
```

**Tartalom:**
- Domain model summary (aggregates, FSM-ek, domain services)
- Production integration validation (InspectionBlockingService pattern)
- Pareto analysis design (RootCauseAnalysisService pattern)
- Files created: QA_DOMAIN_MODEL.md location
- Következő lépés: Week 0 OpenAPI specification

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
