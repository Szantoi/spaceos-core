---
id: MSG-BACKEND-039
from: conductor
to: backend
type: task
priority: critical
status: READ
model: sonnet
ref: MSG-ARCHITECT-OUT-001
created: 2026-06-23
content_hash: ea54d8c876388ea882d662498b798133bc1ecf1028611dc9eed01f4ea8f8eca0
---

# Week 1: EHS Backend + Security Fixes (11 tasks)

## Epic Context

**CATALOG-EHS-HYBRID** - Week 1 Track B: EHS Risk Assessment backend implementation.

Az Architect v1→v4 review pipeline-t futtatott (DB, Security, Backend review).
**2 CRITICAL + 4 HIGH security fix** azonosítva - **ezek BLOCKING deployment!**

## Architecture Reference

**TELJES ARCHITEKTÚRA:**
`docs/tasks/new/SpaceOS_CatalogEHS_Hybrid_Architecture_FINAL.md`

## Week 1 Backend Tasks (11 × 0.5-2h)

### BE-EHS-001: EHS Module Structure (0.5h)
- Create `spaceos-modules-ehs/` folder structure
- Domain, Application, Infrastructure, Contracts layers
- **Files:** Project structure setup

### BE-EHS-002: RiskAssessment Entity + Factory (1.5h)
- Create `RiskAssessment.cs` entity with factory method
- Domain validation: likelihood/severity range (1-5)
- **Domain rule (v4-M3):** High-risk (score > 15) requires notes
- Compute `DataHash` using SHA256
- **Files:** `Domain/Entities/RiskAssessment.cs`

### BE-EHS-003: DB Migration (v2 Fixes Applied) (1h)
- Create `ehs` schema
- Create `ehs.risk_category` ENUM type (v2-M3 fix)
- Create `ehs.risk_assessments` table with:
  - Soft delete columns (v2-H1 fix)
  - FK constraint ON DELETE RESTRICT (v2-H2 fix)
  - Composite index (organization_id, created_at DESC) (v2-M4 fix)
- Enable RLS policy
- **Files:** `Infrastructure/Persistence/Migrations/0001_create_ehs_module.sql`

### BE-EHS-004: ICurrentUserService (v3-C1 Fix) 🔴 CRITICAL
- Create `ICurrentUserService` interface
- Extract `OrganizationId` from JWT claim `"organization_id"`
- Extract `UserId` from JWT claim `NameIdentifier`
- **CRITICAL:** This prevents RLS policy bypass (v3-C1)
- **Files:** `Kernel/Application/Common/Interfaces/ICurrentUserService.cs`

### BE-EHS-005: TenantIsolationInterceptor (v3-C1 Fix) 🔴 CRITICAL
- Create `TenantIsolationInterceptor` DbConnectionInterceptor
- Set `app.current_organization_id` GUC parameter before every query
- **CRITICAL:** This enforces RLS at DB level (v3-C1)
- Integration test: RLS filters org B's data when org A queries
- **Files:** `Kernel/Infrastructure/Persistence/Interceptors/TenantIsolationInterceptor.cs`

### BE-EHS-006: POST /risk-assessments Endpoint (v3-C1+C2 Fixes) 🔴 CRITICAL (2h)
- Create `CreateRiskAssessmentRequest` DTO (NO audit fields - v3-C2 fix)
- Get `organizationId` from `ICurrentUserService` (v3-C1 fix)
- Get `createdBy` from `ICurrentUserService` (v3-C2 fix)
- Call `RiskAssessment.Create(...)` factory method
- Return `201 Created` with `RiskAssessmentResponse`
- **CRITICAL FIXES:**
  - v3-C1: Org ID from JWT, NOT client request
  - v3-C2: Audit fields server-side only (mass assignment prevention)
- Integration test: Client CANNOT override `created_at` or `data_hash`
- **Files:** `Contracts/Requests/CreateRiskAssessmentRequest.cs`, `Application/Commands/CreateRiskAssessment/`

### BE-EHS-007: FluentValidation (1h)
- Create `CreateRiskAssessmentValidator` class
- Validate likelihood/severity range (1-5)
- Validate mitigation field length (max 2000 chars)
- Sync validation with Zod schema (v4-H2 fix)
- **Files:** `Application/Commands/CreateRiskAssessment/CreateRiskAssessmentValidator.cs`

### BE-EHS-008: GET /risk-assessments/latest Endpoint (0.5h) 🟠 HIGH
- Query latest assessment per organization
- **HIGH FIX (v3-H2):** Validate assessment ownership (org ID match)
- Return 404 for unauthorized (NOT 403 - prevents enumeration)
- **Files:** `Application/Queries/GetLatestRiskAssessment/`

### BE-EHS-009: GET /risk-assessments/history Endpoint (2h) 🟠 HIGH
- Query assessment history with filtering
- **HIGH FIX (v4-H1):** Add pagination (page/pageSize params, default 50, max 100)
- **HIGH FIX (v3-H2):** Validate assessment ownership (org ID match)
- Return pagination metadata (total, page, pageSize)
- **Files:** `Application/Queries/GetRiskAssessmentHistory/`

### BE-EHS-010: Rate Limiting (1h) 🟠 HIGH
- Install `AspNetCoreRateLimit` NuGet package
- Configure rate limits:
  - POST endpoints: 10 requests/min
  - GET endpoints: 100 requests/min
- **HIGH FIX (v3-H4):** Prevents abuse
- Return `429 Too Many Requests` with Retry-After header
- **Files:** `Infrastructure/Extensions/RateLimitingExtensions.cs`

### BE-EHS-011: RFC 7807 Error Responses (1.5h) 🟠 HIGH
- Create `ProblemDetailsFactory` implementation
- Structured error responses (type, title, status, detail, instance)
- **HIGH FIX (v4-H3):** Consistent error handling
- Map domain exceptions to Problem Details
- **Files:** `Infrastructure/Errors/ProblemDetailsFactory.cs`

## Critical Security Fixes (BLOCKING DEPLOYMENT)

### 🔴 CRITICAL (v3 Security Review)

**C1 - RLS Policy Bypass Prevention**
- Tasks: BE-EHS-004, BE-EHS-005, BE-EHS-006
- **Problem:** Client can override `organizationId` in request → bypass RLS
- **Mitigation:**
  1. Remove `organizationId` from request DTO
  2. Get org ID from `ICurrentUserService` (JWT claims)
  3. Set GUC parameter `app.current_organization_id` via interceptor

**C2 - Mass Assignment Vulnerability**
- Tasks: BE-EHS-002, BE-EHS-006
- **Problem:** Client can override audit fields (`created_at`, `data_hash`)
- **Mitigation:**
  1. Remove audit fields from request DTO
  2. Factory method sets audit fields server-side

### 🟠 HIGH (v3 Security + v4 Backend Review)

**H2 (v3) - IDOR on Assessment Endpoints**
- Tasks: BE-EHS-008, BE-EHS-009
- **Mitigation:** Validate assessment ownership (org ID match), return 404 for unauthorized

**H4 (v3) - Rate Limiting**
- Tasks: BE-EHS-010
- **Mitigation:** 10 POST/min, 100 GET/min rate limits

**H1 (v4) - Pagination on History Endpoint**
- Tasks: BE-EHS-009
- **Mitigation:** page/pageSize params (default 50, max 100)

**H3 (v4) - Unstructured Error Responses**
- Tasks: BE-EHS-011
- **Mitigation:** RFC 7807 Problem Details format

## Week 1 Checkpoint Criteria

✅ All 11 tasks DONE
✅ Build 0 errors
✅ All tests PASSED (unit + integration)
✅ **CRITICAL fixes verified:**
  - RLS policy blocks org B data from org A
  - Client CANNOT override audit fields
✅ **HIGH fixes verified:**
  - IDOR test fails (org A cannot read org B's assessments)
  - Rate limiting returns 429 after 10 POST/min
  - Pagination metadata returned
  - Error responses follow RFC 7807

## Implementation Order

**CRITICAL PATH:**
1. BE-EHS-001 (module structure)
2. BE-EHS-002 (entity + factory)
3. BE-EHS-003 (DB migration)
4. BE-EHS-004 (ICurrentUserService) ← CRITICAL C1
5. BE-EHS-005 (TenantIsolationInterceptor) ← CRITICAL C1
6. BE-EHS-006 (POST endpoint) ← CRITICAL C1+C2
7. BE-EHS-007 (FluentValidation)
8. BE-EHS-008 (GET latest) ← HIGH H2
9. BE-EHS-009 (GET history + pagination) ← HIGH H2+H1
10. BE-EHS-010 (rate limiting) ← HIGH H4
11. BE-EHS-011 (error responses) ← HIGH H3

**⚠️ WARNING:** Tasks BE-EHS-004, BE-EHS-005, BE-EHS-006 MUST be implemented before deployment!

## Dependencies

- None - parallel with Frontend Track A (Catalog Filter)

## Testing Strategy

**Unit Tests:**
- RiskAssessment factory method validation
- FluentValidation rules

**Integration Tests:**
- RLS policy enforcement (org isolation)
- IDOR prevention (org A cannot read org B's data)
- Mass assignment prevention (client cannot override audit fields)
- Rate limiting (429 after threshold)
- Pagination metadata

**E2E Tests:**
- Full workflow: Create assessment → Get latest → Get history

## Next Steps

1. Read full architecture: `docs/tasks/new/SpaceOS_CatalogEHS_Hybrid_Architecture_FINAL.md`
2. Implement BE-EHS-001 → BE-EHS-011 sequentially
3. **PRIORITY:** Implement CRITICAL fixes (BE-EHS-004, 005, 006) first
4. Run full test suite
5. DONE outbox when all 11 tasks complete + all tests pass

## Estimated Effort

⏱️ **14 hours** (11 tasks, average 1.3h/task)

---

**Conductor**
2026-06-23
