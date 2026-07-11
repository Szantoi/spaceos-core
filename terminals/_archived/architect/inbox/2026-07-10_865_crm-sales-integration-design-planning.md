---
id: MSG-ARCHITECT-865
from: conductor
to: architect
type: task
priority: medium
status: READ
model: opus
ref: CP-CRM-INTEGRATION
created: 2026-07-10
estimated_nwt: 60
epic_id: EPIC-JT-CRM
checkpoint_id: CP-CRM-INTEGRATION
content_hash: 8df12543187f1f5f54f41192ac72666783823f3746aaf3b99a6a77ecbd0d67a6
---

# CP-CRM-INTEGRATION: CRM → Sales Integration Design

**Epic:** EPIC-JT-CRM (Customer Relationship Management Module)
**Checkpoint:** CP-CRM-INTEGRATION
**Scope:** Opportunity.ConvertToQuote() → Sales API contract design
**Estimate:** 60 NWT (~2 hours planning)

---

## 🎯 GOAL

Design the architectural contract for CRM → Sales integration: When an Opportunity converts to a Quote, how should the handoff work between modules?

**Business Value:** Seamless sales pipeline from lead → opportunity → quote → order.

---

## ✅ DELIVERABLES

1. **ADR Document**
   - Location: `docs/knowledge/architecture/decisions/ADR-XXX-crm-sales-integration.md`
   - Decision: Event-based vs. API call vs. Saga pattern
   - Rationale: Why this pattern fits SpaceOS module boundaries

2. **API Contract Design**
   - Sales API endpoint spec (OpenAPI fragment)
   - Request/Response DTOs
   - Validation rules
   - Error handling (409 Conflict, 400 Bad Request)

3. **Implementation Guidance**
   - Event flow diagram (Mermaid)
   - Backend task specification (for future dispatch)
   - Integration test scenarios

4. **Risk Assessment**
   - Data consistency concerns
   - Transaction boundaries
   - Rollback scenarios

---

## 📋 CONTEXT

### CRM Module Status

**ADR-054:** CRM domain model
- Opportunity aggregate with FSM (Draft → Qualified → Quoted → Won/Lost)
- `Opportunity.ConvertToQuote()` method exists but NOT implemented
- Event: `OpportunityConvertedToQuoteEvent` defined but not used

**Current Implementation:**
```csharp
public class Opportunity : AggregateRoot
{
    public OpportunityStatus Status { get; private set; }

    public void ConvertToQuote()
    {
        if (Status != OpportunityStatus.Qualified)
            throw new InvalidOperationException("Only qualified opportunities can convert");

        Status = OpportunityStatus.Quoted;

        // TODO: Trigger Sales module quote creation
        AddDomainEvent(new OpportunityConvertedToQuoteEvent(Id, CustomerContact, LineItems));
    }
}
```

### Sales Module Status

**Unknown:** No ADR exists yet for Sales module.

**Assumption:** Sales module will have:
- Quote aggregate
- QuoteRequest API endpoint (POST /api/sales/quotes)
- Quote FSM (Draft → Sent → Accepted → Converted → Expired)

### Integration Patterns Available

1. **Domain Events (MediatR)** — What we use for Maintenance→Production
2. **API Calls (HTTP)** — Cross-bounded-context integration
3. **Saga Pattern (Orchestration)** — Multi-step transaction coordination

---

## 🤔 DESIGN QUESTIONS

### 1. Synchronous vs. Asynchronous?

**Synchronous (API call):**
- ✅ Immediate feedback (Quote created with ID)
- ✅ Simpler error handling
- ❌ Coupling between modules
- ❌ Availability dependency

**Asynchronous (Domain Event):**
- ✅ Loose coupling
- ✅ No availability dependency
- ❌ Eventually consistent
- ❌ Complex error handling (compensation)

**Your Recommendation:**
- Which pattern and why?
- How does this fit SpaceOS module boundaries?

### 2. Data Ownership

**Question:** Who owns the Quote after creation?
- Sales module (Quote aggregate is Sales bounded context)
- CRM keeps reference (QuoteId) but cannot modify Quote

**Question:** What data flows from Opportunity → Quote?
- Customer contact info
- Line items (products, quantities, prices)
- Special terms/conditions
- Sales rep assignment

### 3. Transaction Boundaries

**Scenario:** ConvertToQuote() fails halfway through
- Opportunity status changed to "Quoted"
- Sales API call fails (network error, validation error)

**Question:** Rollback strategy?
- Compensating transaction (Opportunity.RevertToQualified())?
- Idempotent retry (include correlation ID)?
- Manual intervention (admin dashboard)?

### 4. Validation Rules

**Question:** Who validates the conversion?
- CRM: Business rules (opportunity must be qualified, have line items)
- Sales: Technical rules (products exist, pricing valid)

**Question:** What happens if Sales rejects?
- 409 Conflict: Quote already exists for this Opportunity
- 400 Bad Request: Invalid line items
- 503 Service Unavailable: Sales module down

---

## 📚 REFERENCE MATERIALS

**Existing ADRs:**
- ADR-054: CRM Domain Model (`docs/knowledge/architecture/decisions/ADR-054-joinerytech-crm-domain-model.md`)
- ADR-041: Graph-Based Workflow (for Saga pattern reference)
- ADR-047: Knowledge Service DDD Refactoring (module boundaries)

**Existing Integration:**
- MSG-BACKEND-451: Maintenance→Production (domain event pattern)
- MSG-BACKEND2-002: EHS→HR (domain event pattern)

**SpaceOS Architecture:**
- Layer 2: Modules (Drivers) — CRM and Sales are both L2 modules
- Layer 1: Kernel — provides shared interfaces (IParametricProduct, etc.)

---

## 🔍 DELIVERABLE FORMAT

### ADR-XXX-crm-sales-integration.md

```markdown
# ADR-XXX: CRM → Sales Integration Pattern

## Status
Proposed

## Context
[Describe the integration need, module boundaries, existing patterns]

## Decision
We will use [PATTERN] because [RATIONALE].

## Consequences
### Positive
- [Benefit 1]
- [Benefit 2]

### Negative
- [Risk 1 + mitigation]
- [Risk 2 + mitigation]

## Implementation Guidance
### API Contract
[OpenAPI spec fragment]

### Event Flow
[Mermaid diagram]

### Backend Task Specification
[What Backend terminal needs to implement]

### Integration Test Scenarios
1. Happy path: Qualified opportunity → Quote created
2. Error path: Sales API unavailable → Retry logic
3. Edge case: Duplicate conversion attempt → Idempotency
```

---

## 🚀 SUCCESS CRITERIA

- [ ] ADR document complete with decision + rationale
- [ ] API contract design (OpenAPI fragment)
- [ ] Event flow diagram (Mermaid)
- [ ] Backend implementation guidance
- [ ] Integration test scenarios (3-5 cases)
- [ ] Risk assessment with mitigation strategies

---

## 📋 NEXT STEPS AFTER THIS DONE

1. Conductor reviews ADR
2. If approved → Backend dispatch (MSG-BACKEND-XXX implementation)
3. If changes needed → Architect revision cycle

**Estimated Backend Implementation:** 60 NWT after ADR approval (~2 hours)

---

**Conductor Note:** This is architectural planning ONLY. No code implementation required. Focus on design decisions, patterns, and API contracts.

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
