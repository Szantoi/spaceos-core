---
processed: 2026-07-10
id: MSG-ARCHITECT-074
from: conductor
to: architect
type: task
priority: medium
status: READ
model: haiku
ref: MSG-BACKEND-194
epic_id: EPIC-DOORSTAR-SOFTLAUNCH
estimated_nwt: 60
created: 2026-07-10
---

# Doorstar Production API Contract Review

**Epic:** EPIC-DOORSTAR-SOFTLAUNCH
**Priority:** MEDIUM (parallel track, non-blocking)
**Estimated:** 60 NWT (~2 hours)
**Backend Ref:** MSG-BACKEND-194 (OpenAPI spec), MSG-BACKEND-196 (Implementation)

---

## 🎯 OBJECTIVE

Review the Doorstar Production API contract for DDD compliance, integration design, and architectural soundness **BEFORE** Backend implementation completes (MSG-BACKEND-196, Day 3-4).

**Parallel Track:** This review runs parallel to Backend implementation (2026-07-10 → 2026-07-14) and provides **early feedback** while there's still time to adjust.

---

## 📋 SCOPE

### 1. OpenAPI Contract Validation

**Source:** MSG-BACKEND-194 (Implementation Plan, Section 1: OpenAPI Contract Draft)

**Review Points:**
- [ ] Endpoint naming consistency (`/api/production/jobs` vs `/api/production/queue`)
- [ ] HTTP verbs correctness (GET/PUT/POST/DELETE semantics)
- [ ] DTOs completeness (ProductionJobDto, WorkflowStepDto, ProductionOverviewDto)
- [ ] Error responses defined (400, 401, 404, 409)
- [ ] Pagination support for listing endpoints (if needed)

**Key Endpoints to Review:**
```
GET  /api/production/jobs                      # List production jobs (műhelyvezető view)
GET  /api/production/jobs/{jobId}              # Job detail (6 STAGE state)
PUT  /api/production/jobs/{jobId}/steps/{stepName}/start    # Start step
PUT  /api/production/jobs/{jobId}/steps/{stepName}/complete # Complete step
GET  /api/production/overview                  # Owner/sales dashboard
```

**Question:** Is `stepName` (string) better than `stepId` (Guid) for URL parameter?
- Pros: Human-readable, REST-like
- Cons: Validation complexity (enum constraint)

---

### 2. DDD Pattern Compliance

**Check Against Existing Modules:**
- CRM (MSG-ARCHITECT-054)
- Kontrolling (MSG-ARCHITECT-055)
- EHS (MSG-ARCHITECT-073)

**Validation:**
- [ ] Aggregate root naming (`ProductionJob` vs `Production`)
- [ ] Value object usage (`ProductionJobId`, `WorkflowStepName`)
- [ ] Entity-owned collection pattern (`WorkflowStep` owned by `ProductionJob`)
- [ ] FSM validation approach (domain layer vs application layer)
- [ ] Event naming convention (`ProductionJobStarted` vs `ProductionStarted`)

**Question:** Should `WorkflowStep` be an entity or a value object?
- Entity: If steps have identity, lifecycle (startedAt, completedAt)
- Value object: If steps are immutable snapshots

**Recommendation needed:** Entity (steps have lifecycle, state transitions)

---

### 3. Integration Point Design

**Inbound Events (MSG-BACKEND-196, Section 3.4):**
```csharp
// OrderConfirmedEventHandler.cs
OrderConfirmed → Auto-create ProductionJob
```
**Review:**
- [ ] Event payload contains necessary data (orderId, customerId, deadline)
- [ ] Handler idempotency (duplicate OrderConfirmed events)
- [ ] Transaction boundary (event handling + aggregate creation)

**Inbound Events (Cutting Service, ADR-038):**
```csharp
// CuttingCompletedEventHandler.cs
CuttingCompleted → Auto-complete "Szabászat" step
```
**Review:**
- [ ] How to map CuttingJobId → ProductionJobId? (correlation needed)
- [ ] What if Cutting completes but ProductionJob not started yet? (race condition)
- [ ] Idempotency (duplicate CuttingCompleted events)

**Outbound Events:**
```csharp
ProductionJobShippingReady → Inventory.ReserveForShipping
ProductionJobShippingReady → Sales notification (Telegram/email)
```
**Review:**
- [ ] Event payload design (minimal vs. full DTO)
- [ ] Retry policy (if Inventory service down)
- [ ] Notification delivery guarantee (at-least-once vs. exactly-once)

---

### 4. Frontend API Contract Alignment

**Frontend:** MSG-FRONTEND-107 DONE (2026-07-10)
**Frontend Types:** `client/src/types/production.ts`

**Compare:**
- [ ] ProductionJobDto matches `ProductionJobDto` type (TypeScript)
- [ ] WorkflowStepDto matches `WorkflowStepDto` type
- [ ] Status enums aligned ("Queued" | "InProgress" | "ShippingReady")
- [ ] Photo upload endpoint signature (`POST .../photo` vs `PUT .../complete` with body)

**Frontend SSE expectations:**
```typescript
// useProductionSSE.ts
- WorkflowStepCompleted event → cache invalidation
- ProductionJobShippingReady event → notification
```
**Review:**
- [ ] SSE event payload structure
- [ ] SSE channel naming (`/api/sse/production`)
- [ ] Event type naming consistency

---

### 5. ADR Creation

**Deliverable:** ADR-0XX: Production Module Architecture

**Sections:**
1. **Context:** Doorstar Soft Launch, Munkamenet.pdf digitalization
2. **Decision:** 6 STAGE FSM, 2-level FSM (ProductionJob + WorkflowStep)
3. **Rationale:**
   - Why 6 STAGE (not 17 micro-phases)? → Simplicity vs. granularity tradeoff
   - Why 2-level FSM? → Aggregate + entity pattern (DDD compliance)
   - Why auto-completion for "Szabászat"? → Cutting service integration (ADR-038)
4. **Consequences:**
   - Pros: Real-time tracking, Viber replacement, owner visibility
   - Cons: Manual steps still required (photo upload, button clicks)
5. **Alternatives Considered:**
   - Single-level FSM (ProductionJob.Status only) → Rejected (too coarse)
   - 17-step workflow → Rejected (complexity, Excel remains single source of truth)
6. **Integration Points:**
   - OrderConfirmed → ProductionJob creation
   - CuttingCompleted → Auto-complete "Szabászat"
   - ShippingReady → Inventory + Sales notification

**File:** `docs/architecture/decisions/ADR-0XX-production-module-architecture.md`

---

## ✅ ACCEPTANCE CRITERIA

### OpenAPI Contract
- [ ] All endpoints reviewed, naming consistent
- [ ] DTOs complete, match Frontend types
- [ ] Error responses defined
- [ ] Pagination considered (if needed)

### DDD Compliance
- [ ] Aggregate root pattern correct
- [ ] Value objects used appropriately
- [ ] FSM validation location decided (domain vs application)
- [ ] Event naming consistent with existing modules

### Integration Design
- [ ] OrderConfirmed handler design validated
- [ ] CuttingCompleted handler design validated (correlation, race conditions)
- [ ] Outbound event payload design reviewed
- [ ] Idempotency strategy documented

### Frontend Alignment
- [ ] DTOs match TypeScript types
- [ ] SSE event structure validated
- [ ] Photo upload endpoint signature confirmed

### ADR
- [ ] ADR-0XX created with full context, decision, rationale, consequences
- [ ] Alternative solutions documented
- [ ] Integration points documented

---

## 📁 REFERENCES

| Document | Location |
|----------|----------|
| Backend Implementation Plan | MSG-BACKEND-194 (2026-07-08, 26KB) |
| Backend Implementation Task | MSG-BACKEND-196 (2026-07-10, 480 NWT) |
| Frontend UI DONE | MSG-FRONTEND-107 (2026-07-10, 15 files) |
| Frontend Types | `datahaven-web/client/src/types/production.ts` |
| Doorstar Domain Spec | `/tmp/doorstar_domain_spec.md` (MSG-ROOT-038) |
| 6 STAGE Workflow | MSG-BACKEND-194, Section 2 |
| CRM Module ADR | ADR-054 |
| EHS Module ADR | (pending, reference MSG-ARCHITECT-073) |
| Cutting Service Integration | ADR-038 |

---

## 🚀 DELIVERABLES

### 1. Review Report
**File:** `/opt/spaceos/terminals/architect/outbox/MSG-ARCHITECT-074-DONE.md`

**Content:**
- OpenAPI contract feedback (approve or suggest changes)
- DDD pattern validation (compliant or adjustments needed)
- Integration design feedback (idempotency, correlation, race conditions)
- Frontend alignment check (DTOs, SSE)

### 2. ADR Document
**File:** `docs/architecture/decisions/ADR-0XX-production-module-architecture.md`

**Template:**
```markdown
# ADR-0XX: Production Module Architecture

## Status
Proposed

## Context
Doorstar Soft Launch — Munkamenet.pdf digitalization...

## Decision
6 STAGE FSM, 2-level aggregate pattern...

## Rationale
...

## Consequences
...

## Alternatives Considered
...
```

---

## ⏱️ TIMELINE

**Start:** 2026-07-11 AM (parallel to Backend Day 2)
**ETA:** 2026-07-11 EOD (~2 hours)

**Parallel Coordination:**
- Backend Day 1-2: Domain + Application layers → Review can provide feedback
- Backend Day 3-4: Infrastructure + API → Review insights still actionable

**Non-Blocking:** If review delayed, Backend continues (low risk)

---

## 🎯 SUCCESS METRICS

- [ ] Review completed in 60 NWT (~2 hours)
- [ ] ADR-0XX created and committed
- [ ] Feedback actionable (Backend can adjust if needed)
- [ ] No blocking issues found (API contract sound)

---

📋 Conductor — MSG-ARCHITECT-074 Task Assignment (2026-07-10)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
