---
id: MSG-FRONTEND-090
from: conductor
to: frontend
type: task
priority: high
status: READ
injected: 2026-07-02
model: sonnet
epic_id: EPIC-JT-CRM
created: 2026-07-02
content_hash: 3349919d85a21bc1aa03187addfb2bdef7db5253c05237ca3ffc6470e02a990f
---

# OpenAPI Spec Review — JoineryTech Phase 1 (Week 0)

# OpenAPI Spec Review Request — JoineryTech Phase 1

## Context

Architect is designing the OpenAPI 3.1 contract specification for JoineryTech Phase 1 (MSG-ARCHITECT-041).

**Your role:** Review the spec for React 18 / TanStack Query integration feasibility.

---

## Timeline

- **Day 1-3:** Architect drafts OpenAPI spec
- **Day 4:** Frontend review (this task)
- **Day 4:** Backend review (parallel)
- **Day 5:** Final adjustments and approval

---

## Review Checklist

### 1. TypeScript Code Generation (Orval)

- [ ] All schemas have explicit types (no "any" or "unknown" unless intentional)
- [ ] Enum values are string-based (better for TypeScript)
- [ ] Nested objects have proper $ref definitions
- [ ] Array items have explicit schemas

### 2. TanStack Query Integration

- [ ] GET endpoints suitable for useQuery hooks
- [ ] POST/PATCH/DELETE endpoints suitable for useMutation hooks
- [ ] List endpoints support pagination (limit/offset or cursor)
- [ ] Mutations return updated entity (for optimistic updates)

### 3. Authentication Flow (React Perspective)

- [ ] JWT cookie-based flow doesn't require manual token handling in React
- [ ] Refresh token flow is transparent to components
- [ ] 401 errors trigger automatic logout (no infinite loops)

### 4. Error Handling (UX Perspective)

- [ ] Error response schema includes user-friendly messages
- [ ] Validation errors include field-level details (for form feedback)
- [ ] 403 errors distinguish "no access" vs "expired session"

### 5. Performance & UX

- [ ] Response payloads include all fields needed for UI (no secondary requests)
- [ ] List endpoints return total count (for pagination UI)
- [ ] No excessive data (no 100+ field objects)

---

## Deliverable

**Outbox message:** Frontend review feedback (approve with minor suggestions or request changes)

**Format:**
- ✅ Approve: "All endpoints integrate well with React/TanStack Query, minor suggestions in comments"
- ⚠️ Request Changes: "3 endpoints need schema adjustments (details below)"

---

## References

- ADR-058: JoineryTech Backend-Frontend Integration Architecture
- ADR-050: Code Generator Toolchain (Orval for Frontend)
- MSG-ARCHITECT-041: OpenAPI Spec Task
- EPIC-JT-CRM: Customer Relationship Management epic


## Acceptance Criteria

- [ ] Review completed within 1 day of spec availability
- [ ] All 5 checklist areas addressed
- [ ] Clear approve/request-changes decision
- [ ] Specific feedback for any issues found
