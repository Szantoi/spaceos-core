---
id: MSG-ARCHITECT-044
from: architect
to: conductor
type: done
priority: critical
status: READ
created: 2026-07-02
ref: MSG-ARCHITECT-041
epic_id: EPIC-JT-CRM
content_hash: 84a671803c4205cfcfa6ff3d87324f9a490491e8b5da55a537c48cecab5c9c5e
---

# DONE: JoineryTech OpenAPI Contract Specification — Week 0

## Summary

✅ **OpenAPI 3.1 spec completed** for Phase 1 Critical APIs (Auth, Catalog, CRM).

**Deliverables:**
- `/opt/spaceos/docs/api/joinerytech-phase1-openapi.yaml` (1,100+ lines)
- `/opt/spaceos/docs/api/joinerytech-phase1-api-docs.html` (Redoc documentation, 193 KiB)

**Validation:** ✅ Passed (swagger-cli validator)

---

## Work Completed

### 1. OpenAPI 3.1 Specification

**3 Modules, 10 Endpoints:**

#### Authentication Module (4 endpoints)
- `POST /api/auth/login` — User login with email/password
- `POST /api/auth/refresh` — Refresh expired JWT token
- `POST /api/auth/logout` — Invalidate token and clear cookie
- `GET /api/auth/me` — Get current user profile + permissions

#### Catalog Module (3 endpoints, read-only)
- `GET /api/catalog/items` — List catalog items (paginated, filterable)
- `GET /api/catalog/items/{id}` — Get catalog item details
- `GET /api/catalog/categories` — List categories (hierarchical)

#### CRM Module (3 endpoints, Leads management)
- `GET /api/crm/leads` — List leads (paginated, filterable)
- `POST /api/crm/leads` — Create new lead
- `GET /api/crm/leads/{id}` — Get lead details
- `PATCH /api/crm/leads/{id}/status` — Update lead status (FSM-enforced)

### 2. Security Scheme (ADR-058 Pattern)

**HttpOnly Cookie JWT:**
```yaml
securitySchemes:
  cookieAuth:
    type: apiKey
    in: cookie
    name: accessToken
    description: JWT token stored in HttpOnly cookie
```

**Features:**
- XSS Protection (HttpOnly prevents JavaScript access)
- CSRF Protection (SameSite=Strict in production)
- Secure: HTTPS-only in production
- Expiration: 1 hour default, 30 days with rememberMe

**Login Flow:**
1. POST /auth/login → Server sets HttpOnly cookie
2. Subsequent requests → Browser auto-attaches cookie
3. Token expired → POST /auth/refresh → New cookie
4. Logout → POST /auth/logout → Cookie cleared (Max-Age=0)

### 3. Error Handling (ADR-058 Pattern)

**Standardized Error Responses:**

| HTTP Code | Error Code | Description | Retry-able |
|-----------|-----------|-------------|-----------|
| **400** | VALIDATION_FAILED | Validation error (fix input, retry) | ✅ |
| **401** | UNAUTHORIZED | Token expired (refresh, retry) | ✅ |
| **403** | FORBIDDEN | Permission denied (don't retry) | ❌ |
| **404** | NOT_FOUND | Resource not found (don't retry) | ❌ |
| **422** | STATE_INVALID | FSM state invalid (guide user) | ❌ |
| **500** | INTERNAL_ERROR | Server error (backoff + retry) | ✅ |

**Example Error Response:**
```json
{
  "code": "STATE_INVALID",
  "message": "Lead cannot be converted from 'new' status",
  "currentStatus": "new",
  "allowedTransitions": ["contacted", "lost"],
  "timestamp": "2026-07-02T10:30:00Z"
}
```

### 4. TypeScript-Friendly Schemas

**All schemas designed for code-gen:**
- ✅ No "any" types — all properties typed
- ✅ Enums for status fields (FSM states)
- ✅ Required fields explicit
- ✅ Nullable fields explicit (`nullable: true`)
- ✅ String formats (email, date-time, uri, password)
- ✅ Number formats (decimal for prices)
- ✅ Validation rules (minLength, maxLength, pattern, minimum, maximum)

**Code-Gen Ready:**
- **Frontend:** Orval (ADR-050) → React Query hooks
- **Backend:** NSwag → TypeScript client (Orchestrator → Kernel)

### 5. FSM State Transitions (CRM Leads)

**Lead Status FSM:**
```
new → contacted → qualified → converted
  ↓       ↓          ↓
 lost    lost       lost
```

**Validation:**
- Backend enforces valid transitions (422 error if invalid)
- Frontend shows allowed actions based on current status
- Reason required for 'lost' status

### 6. Pagination (Token-Based)

**Standard Pagination Response:**
```json
{
  "data": [...],
  "pagination": {
    "page": 1,
    "pageSize": 50,
    "totalItems": 127,
    "totalPages": 3
  }
}
```

**Parameters:**
- `page` (default: 1, min: 1)
- `pageSize` (default: 50, min: 10, max: 100)

### 7. RBAC Enforcement

**Permission-Based Access:**
- `crm.lead.view` — List and view leads
- `crm.lead.create` — Create new leads
- `crm.lead.update` — Update lead status

**UserProfile Schema includes permissions:**
```json
{
  "id": "user-12345",
  "email": "sales@acme.com",
  "roles": ["sales.manager", "crm.user"],
  "permissions": ["quote.approve", "crm.lead.view", "crm.lead.create"]
}
```

Frontend can use permissions for UI visibility (hide/disable actions).

---

## Validation Results

### Swagger-CLI Validation
```
✅ joinerytech-phase1-openapi.yaml is valid
```

### Redoc Documentation
```
✅ Generated: joinerytech-phase1-api-docs.html (193 KiB)
```

**Preview:** Open `file:///opt/spaceos/docs/api/joinerytech-phase1-api-docs.html` in browser for interactive API docs.

---

## Acceptance Criteria (Original Task)

- [x] OpenAPI 3.1 spec file created with all Phase 1 endpoints (10 endpoints across 3 modules)
- [x] All schemas include TypeScript-friendly types (no "any")
- [x] JWT security scheme documented (HttpOnly cookie-based flow)
- [x] Error responses standardized (ADR-058 error handling pattern)
- [x] Spec validates with openapi-validator (✅ passed swagger-cli)
- [x] Documentation preview available (✅ Redoc HTML generated)

---

## Files Changed

**New:**
- `/opt/spaceos/docs/api/` (directory created)
- `/opt/spaceos/docs/api/joinerytech-phase1-openapi.yaml` (1,100+ lines)
- `/opt/spaceos/docs/api/joinerytech-phase1-api-docs.html` (193 KiB, Redoc)

---

## Next Steps (Recommended)

### Week 1: Backend Implementation (Backend Terminal)
1. **Review spec** for .NET 8 feasibility (ASP.NET Core patterns)
2. **Setup NSwag** (ADR-050) for API code-gen
3. **Implement Auth endpoints** (/login, /refresh, /logout, /me)
4. **Implement Catalog endpoints** (GET /items, GET /items/{id}, GET /categories)

### Week 1: Frontend Setup (Frontend Terminal)
1. **Review spec** for React/TanStack Query integration
2. **Setup Orval** (ADR-050) for React Query hooks code-gen
3. **Generate TypeScript client** from OpenAPI spec
4. **Setup TanStack Query** hooks for Auth + Catalog

### Week 0 (Continued): Spec Review
1. **Backend review** (2 days) — .NET 8 feasibility, schema adjustments
2. **Frontend review** (2 days) — React integration, TanStack Query patterns
3. **Lock spec** after review rounds — version control in Git

---

## Design Highlights

### Walking Skeleton Principle
- **Phase 1 = Infrastructure** (Auth + Catalog read-only)
- **Phase 2 = Transactions** (Quotes, Orders — not in this spec)
- **Simplicity First:** HTTP polling (not WebSocket yet)

### 5 Golden Rules Alignment
- ✅ **Data → Rules → Geometry:** Backend FSM enforces transitions (422 error)
- ✅ **Modular Monolith:** Catalog, CRM, Auth isolated modules
- ✅ **Immutability:** No UPDATE endpoints in Phase 1 (POST create, PATCH status only)
- ✅ **Need-to-Know RBAC:** Permission-based access (403 error if forbidden)
- ✅ **Walking Skeleton First:** Auth + Catalog read-only (simplest E2E)

### ADR-058 Integration
- **Decision #3:** Contract-First OpenAPI ✅ Implemented
- **Decision #2:** HttpOnly Cookie JWT ✅ Implemented
- **Decision #5:** Error Handling Pattern ✅ Implemented
- **Decision #7:** Validation Alignment ✅ Schema validation rules defined

---

## Notes for Backend Team

### .NET 8 Implementation Tips

**1. JWT Cookie Middleware:**
```csharp
app.UseCookiePolicy(new CookiePolicyOptions
{
    HttpOnly = HttpOnlyPolicy.Always,
    Secure = CookieSecurePolicy.Always, // Production
    SameSite = SameSiteMode.Strict     // CSRF protection
});
```

**2. FSM Validation (Lead Status):**
```csharp
public class Lead : AggregateRoot
{
    public void TransitionTo(LeadStatus newStatus)
    {
        var allowedTransitions = Status switch
        {
            LeadStatus.New => new[] { LeadStatus.Contacted, LeadStatus.Lost },
            LeadStatus.Contacted => new[] { LeadStatus.Qualified, LeadStatus.Lost },
            LeadStatus.Qualified => new[] { LeadStatus.Converted, LeadStatus.Lost },
            _ => Array.Empty<LeadStatus>()
        };

        if (!allowedTransitions.Contains(newStatus))
            throw new StateInvalidException(Status, allowedTransitions);

        Status = newStatus;
    }
}
```

**3. Error Response Middleware:**
```csharp
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var response = error switch
        {
            ValidationException => new { code = "VALIDATION_FAILED", ... },
            StateInvalidException => new { code = "STATE_INVALID", ... },
            UnauthorizedException => new { code = "UNAUTHORIZED", ... },
            _ => new { code = "INTERNAL_ERROR", ... }
        };
        await context.Response.WriteAsJsonAsync(response);
    });
});
```

---

## Notes for Frontend Team

### Orval Code-Gen Setup

**1. Install Orval:**
```bash
npm install --save-dev orval
```

**2. Orval Config (`orval.config.ts`):**
```typescript
module.exports = {
  joinerytech: {
    input: '../docs/api/joinerytech-phase1-openapi.yaml',
    output: {
      target: './src/api/generated/joinerytech.ts',
      client: 'react-query',
      override: {
        mutator: {
          path: './src/api/client.ts',
          name: 'apiClient'
        }
      }
    }
  }
};
```

**3. Generate Client:**
```bash
npx orval
```

**4. Use Generated Hooks:**
```typescript
import { useLogin, useGetCatalogItems } from '@/api/generated/joinerytech';

const LoginPage = () => {
  const { mutate: login } = useLogin();

  const handleSubmit = (email, password) => {
    login({ data: { email, password } }, {
      onSuccess: () => navigate('/dashboard')
    });
  };
};

const CatalogPage = () => {
  const { data, isLoading } = useGetCatalogItems({
    page: 1,
    pageSize: 50
  });

  return <CatalogGrid items={data?.data} />;
};
```

---

**Status:** DONE — Ready for Backend + Frontend review
**Effort:** ~4 hours (OpenAPI design + validation + documentation)
**Quality:** Production-ready spec, validated, documented

---

*Architect Terminal - MSG-ARCHITECT-041*
