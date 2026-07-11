---
id: MSG-BACKEND-106
from: conductor
to: backend
type: task
priority: high
status: INJECTED
injected: 2026-07-03
model: sonnet
epic_id: EPIC-JT-CRM
created: 2026-07-02
content_hash: 973164a1324926f20b4ecfdedef0f7a5ed257daaa332d1e5cd0a160159dfd000
---

# OpenAPI Spec Review — JoineryTech Phase 1 (Week 0)

# OpenAPI Spec Review Request — JoineryTech Phase 1

## Context

Architect is designing the OpenAPI 3.1 contract specification for JoineryTech Phase 1 (MSG-ARCHITECT-041).

**Your role:** Review the spec for .NET 8 / ASP.NET Core feasibility.

---

## Timeline

- **Day 1-3:** Architect drafts OpenAPI spec
- **Day 4:** Backend review (this task)
- **Day 4:** Frontend review (parallel)
- **Day 5:** Final adjustments and approval

---

## Review Checklist

### 1. .NET 8 / ASP.NET Core Compatibility

- [ ] All endpoints map to standard ASP.NET Core controller patterns
- [ ] Request/response schemas are compatible with System.Text.Json
- [ ] No patterns that require custom middleware or complex serialization

### 2. Authentication Flow

- [ ] JWT HttpOnly cookie pattern is implementable with ASP.NET Core Identity
- [ ] Refresh token flow follows Microsoft.AspNetCore.Authentication.JwtBearer patterns
- [ ] CSRF protection via cookie is feasible (ADR-058 double-submit cookie)

### 3. PostgreSQL RLS Integration

- [ ] All endpoints support RLS context (tenant_id, user_id via GUC)
- [ ] No endpoints bypass RLS accidentally
- [ ] Bulk operations respect RLS policies

### 4. Error Handling

- [ ] Error response schema matches existing Kernel patterns
- [ ] 401/403 distinction is clear (authentication vs authorization)
- [ ] 500 errors don't leak sensitive information

### 5. Performance Considerations

- [ ] No N+1 query patterns in endpoint design
- [ ] Pagination parameters for list endpoints
- [ ] Response size is reasonable (<1 MB per request)

---

## Deliverable

**Outbox message:** Backend review feedback (approve with minor suggestions or request changes)

**Format:**
- ✅ Approve: "All endpoints feasible, minor suggestions in comments"
- ⚠️ Request Changes: "3 endpoints need redesign (details below)"

---

## References

- ADR-058: JoineryTech Backend-Frontend Integration Architecture
- MSG-ARCHITECT-041: OpenAPI Spec Task
- EPIC-JT-CRM: Customer Relationship Management epic


## Acceptance Criteria

- [ ] Review completed within 1 day of spec availability
- [ ] All 5 checklist areas addressed
- [ ] Clear approve/request-changes decision
- [ ] Specific feedback for any issues found
