---
id: MSG-ARCHITECT-071
from: root
to: architect
type: task
priority: high
status: READ
model: sonnet
ref: MSG-BACKEND-174
created: 2026-07-08
---

# Task: CRM Specification Alignment with ADR-054

## Context

**Backend is blocked** on CRM Week 2 Application Layer (MSG-BACKEND-174) due to **specification mismatch** with the actual CRM domain model defined in ADR-054.

**Blocker Duration:** 38+ hours (since 2026-07-07 ~14:00 UTC)

**Root Cause:** Task specification was generated from a generic CRUD template without cross-referencing ADR-054, leading to 3 critical conflicts.

---

## The Problem

### Conflict #1: Customer Scope Error

**Current Specification Requests:**
```csharp
CreateCustomerCommand.cs
UpdateCustomerCommand.cs
ArchiveCustomerCommand.cs
GetCustomerByIdQuery.cs
GetAllCustomersQuery.cs
```

**ADR-054 Reality:**
- **Customer is NOT part of CRM module** — it's a separate module
- CRM aggregates: **Lead** and **Opportunity** ONLY
- CRM → Customer integration: `customerId` reference in Opportunity

**Evidence:** ADR-054 lines 24, 360, 417 explicitly state Customer as external integration.

---

### Conflict #2: Update Commands Design Mismatch

**Current Specification Requests:**
```csharp
UpdateLeadCommand.cs          // Generic CRUD update
UpdateOpportunityCommand.cs   // Generic CRUD update
```

**ADR-054 Design:**
```csharp
// Immutable aggregates + FSM transitions (NO generic Update)
Lead.Contact()                     // Specific domain operation
Lead.Qualify()                     // FSM transition
Lead.Disqualify(reason)            // FSM transition
Lead.ConvertToOpportunity(value)   // FSM transition
```

**Backend Already Implemented (ADR-054 compliant):**
- CreateLeadCommand ✅
- ContactLeadCommand ✅
- QualifyLeadCommand ✅
- DisqualifyLeadCommand ✅
- ConvertLeadToOpportunityCommand ✅
- AddLeadActivityCommand ✅
- AddLeadTaskCommand ✅
- CreateOpportunityCommand ✅
- ProposeOpportunityCommand ✅
- NegotiateOpportunityCommand ✅
- WinOpportunityCommand ✅
- LoseOpportunityCommand ✅
- AbandonOpportunityCommand ✅

**Total:** 13 Command Handlers (richer FSM, no generic Update)

---

### Conflict #3: Naming Mismatches

**Current Specification:**
```csharp
MarkAsWonCommand.cs
MarkAsLostCommand.cs
ConvertToCustomerCommand.cs
```

**ADR-054 Implementation:**
```csharp
WinOpportunityCommand.cs       // Already implemented
LoseOpportunityCommand.cs      // Already implemented
ConvertOpportunityToQuoteCommand.cs  // Correct integration
```

---

## Your Task

**Generate an aligned CRM Week 2 Application Layer specification** that:

1. **Respects ADR-054 domain model:**
   - Lead and Opportunity aggregates ONLY
   - Customer as external integration (NOT owned by CRM)
   - FSM-based state transitions (NO generic Update commands)

2. **Provides Backend with:**
   - CQRS command/query structure (aligned with existing Week 1 implementation)
   - Validation rules for each command
   - Event definitions (what events are raised)
   - Integration contracts (CRM → Sales, CRM → Identity, CRM → Customer)

3. **Includes what's already implemented:**
   - Domain Layer (Week 1): Lead + Opportunity aggregates + FSM + ValueObjects ✅
   - Acknowledge existing 13 Command Handlers
   - Specify what's missing (if anything)

4. **Clarifies Week 2 scope:**
   - What Application Layer components need to be added?
   - Query Handlers? Validators? Integration services?
   - Or is Week 2 already complete given existing 13 Command Handlers?

---

## Deliverable

**File:** Aligned specification document (markdown or ADR format)

**Structure:**
1. **Executive Summary** — What changed from original spec, why
2. **Scope Clarification** — What IS and what IS NOT CRM module responsibility
3. **Application Layer Components** — Commands, Queries, Handlers, Validators
4. **Integration Contracts** — CRM → Sales, Identity, Customer
5. **Week 2 Task Definition** — Clear, actionable scope for Backend

**Output Location:** `/opt/spaceos/terminals/architect/outbox/` as DONE message

---

## References

- **ADR-054:** `/opt/spaceos/docs/architecture/decisions/ADR-054-joinerytech-crm-domain-model.md`
- **Backend Blocker:** `/opt/spaceos/terminals/backend/outbox/2026-07-07_180_msg-174-crm-specification-mismatch-blocked.md`
- **Original Specification:** MSG-BACKEND-174 (inbox, may need to be reviewed)

---

## Acceptance Criteria

✅ **Specification aligns with ADR-054** — No Customer scope, FSM transitions, no generic Update
✅ **Backend can proceed immediately** — Clear, actionable task definition
✅ **Cross-referenced with existing implementation** — Acknowledges Week 1 work
✅ **Integration contracts defined** — CRM → Sales/Identity/Customer interfaces
✅ **Root decision:** Approved or requires modification

---

**Priority:** HIGH
**Timeline Impact:** 30-45 minutes (Architect review + spec generation)
**Unblocks:** MSG-BACKEND-174 (38+ hours blocked)

🏛️ **Root Terminal — Architect Alignment Request for CRM Specification**
