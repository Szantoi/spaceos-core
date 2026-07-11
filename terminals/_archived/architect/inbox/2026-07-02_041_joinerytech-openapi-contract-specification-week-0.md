---
id: MSG-ARCHITECT-041
from: conductor
to: architect
type: task
priority: critical
status: COMPLETED
injected: 2026-07-02
model: opus
epic_id: EPIC-JT-CRM
created: 2026-07-02
content_hash: 30c76545c441bc2ddd09ba467c5e64d6a3ab8cb55e13a716111cb6cfac83d905
---

# JoineryTech OpenAPI Contract Specification — Week 0

# JoineryTech OpenAPI Contract Specification — Week 0

## Context

ADR-058 is approved. The critical path for JoineryTech Phase 1 implementation starts with **Contract-First OpenAPI specification**.

**Ref:** ADR-058 Decision #3 — Contract-First OpenAPI for early validation, parallel dev, and code-gen consistency.

**Epic:** EPIC-JT-CRM (primary), affects all 8 JT epics

---

## Task

Design OpenAPI 3.1 specification for **Phase 1 Critical APIs**:

### 1. Authentication Module
- POST /api/auth/login
- POST /api/auth/refresh
- POST /api/auth/logout
- GET /api/auth/me

### 2. Catalog Module (Read-Only)
- GET /api/catalog/items
- GET /api/catalog/items/{id}
- GET /api/catalog/categories

### 3. CRM Module (Phase 1 Subset)
- GET /api/crm/leads
- POST /api/crm/leads
- GET /api/crm/leads/{id}
- PATCH /api/crm/leads/{id}/status

---

## Deliverables

1. **OpenAPI Spec File:** `/opt/spaceos/docs/api/joinerytech-phase1-openapi.yaml`
   - All endpoints with request/response schemas
   - Error codes (400, 401, 403, 404, 500)
   - JWT security scheme (HttpOnly cookie pattern)
   - Examples for each endpoint

2. **API Documentation:** Auto-generated from OpenAPI spec using Redoc or Swagger UI

3. **Validation:** Run openapi-validator to ensure spec is valid

---

## Acceptance Criteria

- [ ] OpenAPI 3.1 spec file created with all Phase 1 endpoints
- [ ] All schemas include TypeScript-friendly types (no "any")
- [ ] JWT security scheme documented (cookie-based flow)
- [ ] Error responses standardized (ADR-058 error handling pattern)
- [ ] Spec validates with openapi-validator (no errors)
- [ ] Documentation preview available (Redoc or Swagger UI)

---

## Coordination

This is a **collaborative task**:
- **Architect:** Design contract, schemas, security flow
- **Backend:** Review for .NET 8 feasibility (ASP.NET Core patterns)
- **Frontend:** Review for React/TanStack Query integration

**Target:** 3-4 days (Architect lead, Backend + Frontend review rounds)

---

## Next Steps (After Completion)

1. Backend: Implement API endpoints from spec (Week 1-2)
2. Frontend: Generate TypeScript client with Orval (ADR-050)
3. Frontend: Setup TanStack Query hooks (Week 1)

---

## References

- ADR-058: JoineryTech Backend-Frontend Integration Architecture
- ADR-050: Code Generator Toolchain (Orval + NSwag)
- EPIC-JT-CRM: Customer Relationship Management epic


## Acceptance Criteria

- [ ] OpenAPI 3.1 spec file created with all Phase 1 endpoints
- [ ] All schemas include TypeScript-friendly types (no any)
- [ ] JWT security scheme documented (cookie-based flow)
- [ ] Error responses standardized (ADR-058 error handling pattern)
- [ ] Spec validates with openapi-validator (no errors)
- [ ] Documentation preview available (Redoc or Swagger UI)
