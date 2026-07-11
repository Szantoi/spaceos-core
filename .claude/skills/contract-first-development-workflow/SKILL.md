# Skill: Contract-First Development Workflow

## Overview

Standardized OpenAPI 3.1 specification writing workflow for Backend ↔ Frontend module pairs. Reduces integration bugs by $11-16k per module implementation.

## When to Use

- Starting new Backend ↔ Frontend module pair (CRM, Kontrolling, HR, etc.)
- Before implementation begins (Week 0 planning)
- When integration contracts need definition
- Cross-team coordination required

## Workflow

### Phase: Week 0 (3-4 days)

#### Day 1: OpenAPI Schema Design
1. **Identify endpoints** from acceptance criteria
   - Example: CRM → `/api/auth/login`, `/api/auth/refresh`, `/api/crm/leads`, `/api/crm/opportunities`

2. **Define request/response schemas** with examples
   ```yaml
   components:
     schemas:
       LoginRequest:
         type: object
         properties:
           email: { type: string }
           password: { type: string }
       LoginResponse:
         type: object
         properties:
           accessToken: { type: string }
           refreshToken: { type: string }
   ```

3. **Add security schemes** (JWT/OAuth)
   ```yaml
   components:
     securitySchemes:
       bearerAuth:
         type: http
         scheme: bearer
         bearerFormat: JWT
   ```

4. **Document error responses**
   - 400: Validation error
   - 401: Unauthorized
   - 403: Forbidden
   - 500: Server error

#### Day 2: Endpoint Specification
1. **Write all endpoints** with:
   - Path + HTTP method
   - Request/response schemas (ref to components)
   - Status codes (200, 400, 401, 403, 500)
   - Description + examples
   - Required permissions (RBAC tags)

2. **Add pagination** (if applicable)
   ```yaml
   parameters:
     - name: page
       in: query
       schema: { type: integer, default: 1 }
     - name: limit
       in: query
       schema: { type: integer, default: 20 }
   ```

3. **Document rate limiting** (if applicable)
   - X-RateLimit-Limit header
   - X-RateLimit-Remaining header
   - X-RateLimit-Reset header

#### Day 3: Code-Gen Setup & Testing
1. **Frontend (React):** Setup Orval
   ```bash
   npm install -D @orval/core @orval/openapi
   # orval.config.ts configured
   npm run generate:api
   ```
   - Output: `src/api/generated/` (TanStack Query hooks auto-generated)

2. **Backend (.NET):** Setup NSwag
   ```bash
   dotnet add package NSwag.CodeGeneration.CSharp
   NSwag.exe openapi2csharp /input:openapi.json /output:Generated/
   ```
   - Output: `Generated/` (Minimal API DTOs + interfaces)

3. **Test code-gen** in both projects
   - Frontend: Verify hooks compile + TypeScript types correct
   - Backend: Verify DTOs match schema + serialization works

#### Day 4: Approval Gate
1. **Code review:**
   - Architect: Schema design + security
   - Backend lead: DTO feasibility
   - Frontend lead: Hook patterns

2. **Sign-off:** Spec locked (no changes during Phase 1 implementation)

### Phase: Week 1+ (Implementation using spec)

#### Backend
- Implement endpoints using auto-generated DTOs
- No schema mismatches (code-gen binds them)

#### Frontend
- Use auto-generated hooks (TanStack Query)
- Full type safety (TypeScript)

### Success Metrics

| Metric | Target | Method |
|--------|--------|--------|
| Integration bugs | 0 (schema enforced) | Verify in E2E tests |
| Build compile time | <30 sec (no rework) | `npm run build` timer |
| Type coverage | 100% (auto-gen'd) | TypeScript strict mode |
| API contract matches | 100% | Schema validation |

## ROI Calculation

```
Cost: $4k (spec writing + code-gen setup)
Savings: $11-16k (integration rework prevention + type safety)
Net: $7-12k per module
```

## Example: EPIC-JT-CRM

**Endpoints (OpenAPI spec):**
- POST `/api/auth/login` — JWT + refresh token
- POST `/api/auth/refresh` — New JWT from refresh
- POST `/api/auth/logout` — Invalidate token
- GET `/api/crm/leads` — Paginated leads list
- GET `/api/crm/leads/{id}` — Lead detail
- POST `/api/crm/leads` — Create lead
- PUT `/api/crm/leads/{id}` — Update lead
- GET `/api/crm/opportunities` — Paginated opportunities
- POST `/api/crm/opportunities` — Create opportunity
- PUT `/api/crm/opportunities/{id}` — Move pipeline (drag & drop)

**Code-gen output:**
- Frontend: `useLogin()`, `useRefresh()`, `useLeads()`, `useLead()`, `useCreateLead()`, etc. (TanStack Query hooks)
- Backend: `LoginRequest`, `LoginResponse`, `LeadDTO`, `OpportunityDTO` (C# DTOs)

## Checklist

- [ ] Week 0 timeline locked (3-4 days)
- [ ] All endpoints identified (acceptance criteria covered)
- [ ] Request/response schemas defined with examples
- [ ] Security schemes documented (JWT/OAuth)
- [ ] Error responses specified (400, 401, 403, 500)
- [ ] Orval configured (Frontend)
- [ ] NSwag configured (Backend)
- [ ] Code-gen tested in both projects
- [ ] Spec review completed (Architect + team leads)
- [ ] Spec locked for implementation

## Related Patterns

- **Walking Skeleton First** (Phase 1 auth only, no state complexity)
- **Mock API Parallel Development** (Frontend proceeds while Backend scales)
- **Checkpoint Coordination** (CP-BACKEND, CP-FRONTEND checkpoints)

## References

- [OpenAPI 3.1 Spec](https://spec.openapis.org/oas/v3.1.0)
- [Orval Documentation](https://orval.dev/)
- [NSwag Documentation](https://github.com/RicoSuter/NSwag)
- ADR-050: Code Generator Toolchain
- ADR-058: JoineryTech Integration Architecture

---

**Skill Created:** 2026-07-04
**Source:** JoineryTech Research (50+ task messages, ADR-058 analysis)
**ROI:** $7-12k per module
