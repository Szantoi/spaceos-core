---
id: MSG-ARCHITECT-049
from: architect
to: root
type: done
priority: critical
status: READ
ref: MSG-ARCHITECT-041
created: 2026-07-02
content_hash: 696aaf9661e6c622fe759df36d603fa8c2fb32b66d69414724e92a5097355e09
---

# ✅ DONE: JoineryTech OpenAPI 3.1 Contract Specification (Phase 1)

**Task:** MSG-ARCHITECT-041 — OpenAPI Contract Specification for Week 0
**Deliverable:** `/opt/spaceos/docs/api/joinerytech-phase1-openapi.yaml`
**Status:** Validated ✅ — Ready for Backend + Frontend Review
**Spec Size:** 1,132 lines, 11 endpoints, 15 schemas

---

## Executive Summary

Successfully created comprehensive OpenAPI 3.1 specification for JoineryTech Phase 1 Critical APIs (Authentication, Catalog, CRM).

**Validation Status:**
- ✅ OpenAPI 3.1 compliant
- ✅ All Phase 1 endpoints documented
- ✅ RFC 7807 Problem Details error handling
- ✅ TypeScript-friendly schemas (no `any` types)
- ✅ JWT HttpOnly cookie security documented
- ⚠️ 2 non-blocking warnings (localhost server, URI format in examples)

---

## Specification Overview

### Modules Covered

| Module | Endpoints | Status |
|--------|-----------|--------|
| **Authentication** | 4 | Complete |
| **Catalog (Read-Only)** | 3 | Complete |
| **CRM (Leads)** | 4 | Complete |
| **Total** | **11** | **✅ Phase 1 Ready** |

### Authentication Module

| Method | Endpoint | Description | Security |
|--------|----------|-------------|----------|
| POST | `/auth/login` | User login, sets HttpOnly cookies | None (public) |
| POST | `/auth/refresh` | Refresh access token | Cookie auth |
| POST | `/auth/logout` | Clear tokens | Cookie auth |
| GET | `/auth/me` | Get current user profile | Cookie auth |

**Key Features:**
- JWT in HttpOnly cookies (XSS protection per ADR-058)
- Token rotation on refresh
- Access token: 15 min expiration
- Refresh token: 7 days (30 days with `rememberMe`)

### Catalog Module (Read-Only)

| Method | Endpoint | Description | Pagination |
|--------|----------|-------------|------------|
| GET | `/catalog/items` | List catalog items | ✅ (page, pageSize, categoryId, search) |
| GET | `/catalog/items/{id}` | Get single catalog item | N/A |
| GET | `/catalog/categories` | List categories (hierarchical) | N/A |

**Key Features:**
- Hierarchical category tree (recursive structure)
- Product specifications as key-value pairs
- Stock quantity (read-only)
- Category filtering

### CRM Module (Phase 1: Leads Only)

| Method | Endpoint | Description | FSM |
|--------|----------|-------------|-----|
| GET | `/crm/leads` | List leads | N/A |
| POST | `/crm/leads` | Create new lead | Initial state: `New` |
| GET | `/crm/leads/{id}` | Get single lead | N/A |
| PATCH | `/crm/leads/{id}/status` | Update lead status | ✅ FSM-validated |

**FSM State Transitions:**
```
New → Contacted, Qualified, Rejected
Contacted → Qualified, Rejected
Qualified → Opportunity (Phase 2)
Rejected → New (reopen)
```

---

## Schema Design

### Core Schemas (15 total)

**Authentication:**
- `LoginResponse` — User profile + expiration timestamp
- `UserProfile` — User ID, email, name, roles, permissions, tenantId

**Catalog:**
- `CatalogItem` — Product details (code, name, price, unit, specifications, stock)
- `CatalogCategory` — Hierarchical categories (id, name, parentId, children)

**CRM:**
- `Lead` — Lead entity (companyName, status, priority, source, estimatedValue)
- `CreateLeadRequest` — Create lead command
- `LeadStatus` — FSM status enum (New, Contacted, Qualified, Rejected)
- `LeadPriority` — Priority enum (Low, Medium, High, Critical)
- `LeadSource` — Source enum (Website, Referral, ColdCall, Exhibition, etc.)

**Shared Value Objects:**
- `Money` — amount, currency (HUF/EUR/USD), formatted display

**Error Handling (RFC 7807):**
- `ProblemDetails` — type, title, status, detail, instance, traceId
- `ValidationProblemDetails` — extends ProblemDetails with `errors` field (field-level validation)

---

## Security Scheme

**Cookie-Based JWT Authentication:**
```yaml
securitySchemes:
  cookieAuth:
    type: apiKey
    in: cookie
    name: access_token
```

- **HttpOnly cookies** — JavaScript cannot access (XSS protection)
- **SameSite=Strict** — CSRF protection
- **Secure flag** — HTTPS only in production
- **Automatic attachment** — Browser sends cookie on every request

All endpoints (except `/auth/login`) require `cookieAuth` security.

---

## Error Handling Pattern (RFC 7807)

All errors follow standardized Problem Details format per ADR-058:

**Example 401 Error:**
```json
{
  "type": "https://api.joinerytech.hu/errors/unauthorized",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Access token is missing or invalid",
  "instance": "/v1/auth/me",
  "traceId": "00-abc123-def456-00"
}
```

**Example 422 Validation Error:**
```json
{
  "type": "https://api.joinerytech.hu/errors/validation-failed",
  "title": "Validation Failed",
  "status": 422,
  "detail": "One or more validation errors occurred",
  "instance": "/v1/crm/leads",
  "traceId": "00-abc123-def456-00",
  "errors": {
    "companyName": ["Company name is required"],
    "email": ["Email format is invalid"]
  }
}
```

**Error Code Coverage:**
- 400 Bad Request (malformed request body)
- 401 Unauthorized (missing/invalid token)
- 403 Forbidden (insufficient permissions)
- 404 Not Found (resource doesn't exist)
- 422 Unprocessable Entity (validation error, FSM violation)
- 500 Internal Server Error (unexpected error)

---

## TypeScript Compatibility

All schemas use TypeScript-friendly types:

| OpenAPI Type | TypeScript Type | Example |
|--------------|----------------|---------|
| `type: string, format: uuid` | `string` (branded UUID) | `"3fa85f64-5717-4562-b3fc-2c963f66afa6"` |
| `type: string, format: email` | `string` (email validation) | `"admin@joinerytech.hu"` |
| `type: string, format: date-time` | `string` (ISO 8601) | `"2026-07-02T10:15:00Z"` |
| `type: number, format: decimal` | `number` | `12500.00` |
| `type: string, enum: [...]` | `"New" \| "Contacted" \| ...` | `"New"` |

**No `any` types** — All schemas have explicit types with format validation.

**Optional fields** — Represented by omission from `required` array (not `nullable: true` in OpenAPI 3.1).

---

## Validation Results

### Redocly CLI Validation

```bash
npx @redocly/cli lint joinerytech-phase1-openapi.yaml
```

**Result:** ✅ **Valid**

**Errors:** 0
**Warnings:** 2 (non-blocking)

1. **Localhost server warning** — Expected for development environment
2. **URI format in example** — Minor, doesn't affect functionality (relative paths in examples)

---

## Code Generation Ready

The specification is ready for code generation per ADR-050:

**Frontend (Orval):**
```bash
cd datahaven-web/client
npx orval --config orval.config.ts
```

**Output:**
- TypeScript API client with TanStack Query hooks
- Full type safety (no `any` types)
- Example: `useLogin()`, `useGetCatalogItems()`, `useCreateLead()`

**Backend (NSwag):**
```bash
cd spaceos-nexus/orchestrator
npx @swashbuckle/nswag studio
```

**Output:**
- C# API client
- .NET 8 compatible models
- Used by Orchestrator to call Kernel APIs

---

## Acceptance Criteria: ✅ ALL MET

- [x] **OpenAPI 3.1 spec file created** with all Phase 1 endpoints — `/opt/spaceos/docs/api/joinerytech-phase1-openapi.yaml` (1,132 lines)
- [x] **All schemas include TypeScript-friendly types** (no `any`) — ✅ All schemas use `format: uuid`, `format: email`, `format: date-time`, enums
- [x] **JWT security scheme documented** (cookie-based flow) — ✅ `cookieAuth` security scheme, HttpOnly cookie pattern
- [x] **Error responses standardized** (ADR-058 error handling pattern) — ✅ RFC 7807 Problem Details (type, title, status, detail, instance, traceId)
- [x] **Spec validates with openapi-validator** (no errors) — ✅ Redocly CLI: 0 errors, 2 non-blocking warnings
- [x] **Documentation preview available** (Redoc or Swagger UI) — ✅ Redoc available via `npx @redocly/cli preview-docs`

---

## Next Steps

### Immediate (Week 0 Review)

1. **Backend review** (2-3 days)
   - Validate .NET 8 ASP.NET Core patterns
   - Confirm RLS enforcement approach
   - Review JWT cookie handling

2. **Frontend review** (2-3 days)
   - Validate React + TanStack Query integration
   - Confirm Orval code-gen output
   - Review error handling UX

3. **Lock spec** (end of Week 0)
   - Version control commit (Git tag `v1.0-phase1-spec`)
   - Freeze contract changes until Phase 1 completion

### Week 1 (Phase 1 Implementation Starts)

**Backend:**
1. Generate .NET models from OpenAPI spec (NSwag)
2. Implement Auth API endpoints (`/auth/*`)
3. Implement Catalog API endpoints (`/catalog/*`)
4. Setup JWT HttpOnly cookie middleware

**Frontend:**
1. Generate TypeScript client with Orval
2. Setup TanStack Query hooks
3. Implement login form + auth flow
4. Test API integration with MSW mocks

---

## Documentation Preview

**Redoc Preview:**
```bash
cd /opt/spaceos/docs/api
npx @redocly/cli preview-docs joinerytech-phase1-openapi.yaml
```

**Opens:** `http://localhost:8080` with interactive API documentation

**Swagger UI Alternative:**
```bash
npx swagger-ui-watcher joinerytech-phase1-openapi.yaml
```

---

## Files Modified

- **Created:** `/opt/spaceos/docs/api/joinerytech-phase1-openapi.yaml` (1,132 lines, validated)

---

## Dependencies

**Blocks:**
- Backend Phase 1 implementation — Cannot start until spec reviewed and locked
- Frontend Phase 1 implementation — Cannot start until spec reviewed and locked
- MSG-ARCHITECT-042 (CRM Domain Model) — Depends on OpenAPI contract for schema validation

**Depends On:**
- ADR-058 (Backend-Frontend Integration Architecture) — ✅ Complete

---

## Technical Highlights

### FSM State Validation Example

Lead status transitions enforced via PATCH endpoint:

```yaml
PATCH /crm/leads/{id}/status
{
  "status": "Contacted",
  "reason": "Spoke with decision maker"
}
```

**Invalid transition (Qualified → New):**
```json
{
  "type": "https://api.joinerytech.hu/errors/invalid-state-transition",
  "title": "Invalid State Transition",
  "status": 422,
  "detail": "Cannot transition from 'Qualified' to 'New'",
  "errors": {
    "status": ["Valid transitions from 'Qualified': Opportunity, Rejected"]
  }
}
```

### Pagination Pattern

```yaml
GET /catalog/items?page=1&pageSize=50&categoryId={uuid}
```

**Response:**
```json
{
  "items": [...],
  "totalCount": 1250,
  "page": 1,
  "pageSize": 50,
  "hasMore": true
}
```

---

## Recommendations

1. **Schedule Backend + Frontend sync meeting** — Ensure both teams review spec together (avoid siloed review)
2. **Create example request/response flows** — Postman collection or similar for manual testing
3. **Setup MSW mocks** — Frontend can start development before Backend implementation
4. **Add contract tests** — Ensure Backend implementation matches spec (Pact or similar)

---

**Status:** ✅ DONE — OpenAPI 3.1 spec validated and ready for Week 0 review
**Next Task:** MSG-ARCHITECT-042 (CRM Domain Model Design)
