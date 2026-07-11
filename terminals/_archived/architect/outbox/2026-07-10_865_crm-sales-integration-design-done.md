---
id: MSG-ARCHITECT-865-DONE
from: architect
to: conductor
type: done
priority: medium
status: READ
ref: MSG-ARCHITECT-865
created: 2026-07-10
content_hash: ec6cb4cc5d9b5a2e4dd6f18eeebde9b6c2e0602a6c680d36b8ab809355f651ac
---

# DONE: CRM → Sales Integration Design (CP-CRM-INTEGRATION)

**Original Task:** MSG-ARCHITECT-865 — CRM → Sales Integration Design Planning
**Epic:** EPIC-JT-CRM
**Checkpoint:** CP-CRM-INTEGRATION
**Resolution Time:** ~55 minutes
**Estimated NWT:** 60 (on target)

---

## Executive Summary

Designed the architectural contract for **CRM → Sales integration** using the **Asynchronous Domain Events with Outbox Pattern**.

**Key Decision:** When an Opportunity converts to a Quote, CRM publishes an `OpportunityConvertedToQuoteEvent`. Sales handles this event asynchronously and publishes `QuoteCreatedFromOpportunityEvent` back to CRM.

**Why this pattern:**
- ✅ Loose coupling (CRM doesn't know Sales internals)
- ✅ Reliability (transactional outbox ensures no event loss)
- ✅ Resilience (CRM succeeds even if Sales is temporarily down)
- ✅ Testability (each handler can be unit tested in isolation)

---

## Deliverables

### 1. ADR Document

**Location:** `/opt/spaceos/docs/architecture/decisions/ADR-063-crm-sales-integration-pattern.md`

**Contents:**
- Integration pattern decision (Event-based with Outbox)
- API contract design (OpenAPI 3.1 fragment)
- Event definitions (3 events: ConvertedToQuote, QuoteCreated, CreationFailed)
- Event flow diagram (Mermaid sequence diagram)
- Data flow specification (what transfers from Opportunity to Quote)
- Error handling strategy (compensating transactions)
- Backend implementation guidance (Phase 1: CRM, Phase 2: Sales)
- Integration test scenarios (5 scenarios)
- Risk assessment with mitigations

### 2. API Contract

**Endpoint:** `POST /api/crm/opportunities/{id}/convert-to-quote`
- Returns 202 Accepted (async initiation)
- Includes conversionId for polling

**Endpoint:** `GET /api/crm/conversions/{conversionId}`
- Poll for conversion status
- Returns quoteId when completed

### 3. Event Flow Diagram

```
User → CRM: ConvertOpportunityToQuote
CRM → CRM: Validate, transition to "Converting"
CRM → Outbox: Save event
Outbox → EventBus: Publish
EventBus → Sales: OpportunityConvertedToQuoteEvent
Sales → Quote: Create
Sales → EventBus: QuoteCreatedFromOpportunityEvent
EventBus → CRM: Update Opportunity.QuoteRef, transition to "Won"
```

### 4. Implementation Guidance

**Phase 1 (CRM): 30 NWT**
1. Extend Opportunity FSM with "Converting" transient state
2. Implement ConvertOpportunityToQuoteCommandHandler
3. Implement QuoteCreated/CreationFailed event handlers

**Phase 2 (Sales): 20-30 NWT**
1. Create Quote.CreateFromOpportunity() factory
2. Implement OpportunityConvertedToQuoteEventHandler

### 5. Integration Test Scenarios

| # | Scenario | Expected Outcome |
|---|----------|------------------|
| 1 | Happy path | Opportunity → Won, Quote created |
| 2 | Idempotent retry | No duplicate Quote |
| 3 | Invalid state | 409 Conflict |
| 4 | Sales failure | Rollback to Negotiation |
| 5 | Timeout (30s) | Auto-rollback |

### 6. Risk Assessment

| Risk | Mitigation |
|------|------------|
| Data loss | Transactional outbox |
| Duplicate Quotes | Idempotency key (ConversionId) |
| Converting stuck | 30s timeout + auto-rollback |

---

## Acceptance Criteria Validation

- [x] ADR document complete with decision + rationale
- [x] API contract design (OpenAPI fragment)
- [x] Event flow diagram (Mermaid)
- [x] Backend implementation guidance
- [x] Integration test scenarios (5 cases)
- [x] Risk assessment with mitigation strategies

---

## Files Delivered

| File | Purpose |
|------|---------|
| `/opt/spaceos/docs/architecture/decisions/ADR-063-crm-sales-integration-pattern.md` | Full ADR document |

---

## Next Steps

1. **Conductor reviews ADR-063**
2. **If approved:** Dispatch to Backend (MSG-BACKEND-XXX)
3. **Backend implements Phase 1** (CRM side) — estimated 30 NWT
4. **Sales module implements Phase 2** when ready — estimated 20-30 NWT

---

## Architecture Decision Summary

| Aspect | Decision |
|--------|----------|
| **Pattern** | Asynchronous Domain Events (MediatR) |
| **Reliability** | Transactional Outbox Pattern |
| **Consistency** | Eventually Consistent |
| **Coupling** | Loose (CRM publishes events, Sales consumes) |
| **Error Handling** | Compensating Transaction + DLQ |
| **Idempotency** | ConversionId key |
| **Timeout** | 30 seconds with auto-rollback |

---

## Architect Sign-Off

**Task:** MSG-ARCHITECT-865 — CRM → Sales Integration Design
**Status:** COMPLETE
**Pattern Selected:** Asynchronous Domain Events with Outbox
**ADR Created:** ADR-063
**Ready for:** Conductor review → Backend dispatch

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>
