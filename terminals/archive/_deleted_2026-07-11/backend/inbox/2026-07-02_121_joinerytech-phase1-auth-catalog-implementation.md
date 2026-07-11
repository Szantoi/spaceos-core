---
id: MSG-BACKEND-121
from: conductor
to: backend
type: task
priority: high
status: INJECTED
injected: 2026-07-03
model: sonnet
ref: MSG-BACKEND-105-DONE
created: 2026-07-02
content_hash: 69b122c3e21959b5bffd04bff13eaf13e67df6180eda2fb2247e65910c5062fa
---

# JoineryTech Phase 1 Implementation — Auth + Catalog API

## Context

Backend Architecture Plan (MSG-BACKEND-105-DONE) elkészült: 5,200+ sor comprehensive design document.

**Architecture Plan:** `/opt/spaceos/docs/joinerytech/BACKEND_ARCHITECTURE_PLAN.md`

Most indítjuk a **Phase 1 implementációt** (4 hét, July-August 2026):
- Authentication (JWT + OAuth 2.0)
- Catalog API (CRUD endpoints)
- Multi-tenant foundation (PostgreSQL RLS)

## Phase 1 Scope

### 1. Authentication & Authorization (Week 1-2)

**1.1 JWT Token Infrastructure**
- Token generation (access + refresh tokens)
- Token validation middleware
- Token payload structure:
  ```json
  {
    "sub": "user_id",
    "tenant_id": "uuid",
    "roles": ["admin", "sales_lead"],
    "account_type": "premium",
    "permissions": ["catalog.read", "catalog.write"],
    "exp": 1234567890
  }
  ```

**1.2 OAuth 2.0 Flow**
- Authorization Code Flow implementation
- Login endpoint: `POST /api/v1/auth/login`
- Refresh endpoint: `POST /api/v1/auth/refresh`
- Logout endpoint: `POST /api/v1/auth/logout`

**1.3 RBAC Model**
- 8 predefined roles (Admin, Sales Lead, Purchasing, Production, Warehouse, HR, Maintenance, QA)
- Permission system (resource.action format)
- Multi-device session management

**1.4 Multi-Tenant Context**
- `tenant_id` injection middleware (sets PostgreSQL GUC)
- Tenant resolution from JWT payload
- RLS policy enforcement

---

### 2. Catalog API (Week 2-3)

**2.1 Database Schema** (`jt_catalog`)

```sql
CREATE SCHEMA jt_catalog;

CREATE TABLE jt_catalog.catalog_items (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id UUID NOT NULL,
  name VARCHAR(255) NOT NULL,
  sku VARCHAR(100),
  category_id UUID REFERENCES jt_catalog.catalog_categories(id),
  description TEXT,
  base_price DECIMAL(12,2),
  status VARCHAR(20) CHECK (status IN ('active', 'discontinued', 'draft')),
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- RLS Policy
ALTER TABLE jt_catalog.catalog_items ENABLE ROW LEVEL SECURITY;

CREATE POLICY tenant_isolation_policy ON jt_catalog.catalog_items
  USING (tenant_id = current_setting('app.tenant_id')::uuid);

-- Indexes
CREATE INDEX idx_catalog_items_tenant_status ON jt_catalog.catalog_items(tenant_id, status);
CREATE INDEX idx_catalog_items_category ON jt_catalog.catalog_items(category_id);
```

**2.2 API Endpoints**

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/v1/catalog/items` | List catalog items (pagination, filter) |
| `GET` | `/api/v1/catalog/items/{id}` | Get catalog item details |
| `POST` | `/api/v1/catalog/items` | Create new catalog item |
| `PUT` | `/api/v1/catalog/items/{id}` | Update catalog item |
| `DELETE` | `/api/v1/catalog/items/{id}` | Soft-delete catalog item |
| `GET` | `/api/v1/catalog/categories` | List categories (tree structure) |
| `POST` | `/api/v1/catalog/categories` | Create category |

**2.3 Minimal API Implementation** (.NET 8)

```csharp
// Program.cs
app.MapGet("/api/v1/catalog/items", async (
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    [FromQuery] string? status = null,
    IMediator mediator) =>
{
    var query = new GetCatalogItemsQuery { Page = page, PageSize = pageSize, Status = status };
    var result = await mediator.Send(query);
    return Results.Ok(result);
})
.RequireAuthorization("catalog.read");

app.MapPost("/api/v1/catalog/items", async (
    [FromBody] CreateCatalogItemRequest request,
    IMediator mediator) =>
{
    var command = new CreateCatalogItemCommand(request);
    var result = await mediator.Send(command);
    return result.IsSuccess ? Results.Created($"/api/v1/catalog/items/{result.Value}", result.Value) : Results.BadRequest(result.Error);
})
.RequireAuthorization("catalog.write");
```

**2.4 CQRS Handlers**

- **Command Handlers:** CreateCatalogItemHandler, UpdateCatalogItemHandler
- **Query Handlers:** GetCatalogItemsHandler, GetCatalogItemByIdHandler
- **Validation:** FluentValidation rules (name required, price > 0, etc.)
- **Repository:** ICatalogRepository with EF Core implementation

---

### 3. PostgreSQL Setup (Week 1)

**3.1 Core Schema** (`jt_core`)

```sql
CREATE SCHEMA jt_core;

CREATE TABLE jt_core.tenants (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name VARCHAR(255) NOT NULL,
  slug VARCHAR(100) UNIQUE NOT NULL,
  status VARCHAR(20) CHECK (status IN ('active', 'suspended', 'trial')),
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE jt_core.users (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id UUID NOT NULL REFERENCES jt_core.tenants(id),
  email VARCHAR(255) UNIQUE NOT NULL,
  password_hash VARCHAR(255) NOT NULL,
  roles JSONB NOT NULL DEFAULT '[]',
  status VARCHAR(20) CHECK (status IN ('active', 'inactive')),
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- RLS Policy
ALTER TABLE jt_core.users ENABLE ROW LEVEL SECURITY;

CREATE POLICY tenant_isolation_policy ON jt_core.users
  USING (tenant_id = current_setting('app.tenant_id')::uuid);
```

**3.2 EF Core Migrations**

```bash
cd /opt/spaceos/backend/SpaceOS.Modules.JoineryTech
dotnet ef migrations add InitialCreate_Phase1_AuthCatalog
dotnet ef database update
```

---

### 4. Testing (Week 3-4)

**4.1 Unit Tests**
- Command/Query handlers
- JWT token generation/validation
- RBAC permission checks
- RLS policy enforcement

**4.2 Integration Tests**
- API endpoint E2E tests
- Multi-tenant isolation verification
- Authentication flow tests

**4.3 Testcontainers**
- PostgreSQL container for integration tests
- In-memory database for unit tests

---

## Deliverables

1. **Code Implementation:**
   - `SpaceOS.Modules.JoineryTech.Domain/` (Catalog aggregate)
   - `SpaceOS.Modules.JoineryTech.Application/` (CQRS handlers)
   - `SpaceOS.Modules.JoineryTech.Infrastructure/` (EF Core, repositories)
   - `SpaceOS.Modules.JoineryTech.Api/` (Minimal API endpoints)

2. **Database:**
   - `jt_core` schema (tenants, users)
   - `jt_catalog` schema (catalog_items, catalog_categories)
   - RLS policies configured
   - Seed data (1 tenant, 5 users, 20 catalog items)

3. **Documentation:**
   - OpenAPI spec (`joinerytech-phase1-openapi.yaml`)
   - Authentication flow diagram
   - Database schema diagram

4. **Tests:**
   - 80%+ code coverage (unit + integration)
   - E2E test suite (Postman collection or REST Client)

---

## Technical Requirements

### .NET 8 Stack
- **CQRS:** MediatR
- **Validation:** FluentValidation
- **ORM:** EF Core 8
- **Auth:** JWT Bearer + Microsoft.Identity
- **Testing:** xUnit + Testcontainers

### PostgreSQL
- **Version:** 14+
- **Extensions:** uuid-ossp, pgcrypto
- **RLS:** Enabled on all tenant-scoped tables

### API Conventions
- **Versioning:** URI path (`/api/v1/`)
- **Pagination:** Offset-based (page + pageSize)
- **Error format:** Problem Details (RFC 7807)
- **Status codes:** 200 (OK), 201 (Created), 400 (Bad Request), 401 (Unauthorized), 403 (Forbidden), 404 (Not Found)

---

## Timeline

| Week | Milestone | Deliverable |
|------|-----------|-------------|
| **Week 1** | Infrastructure setup | PostgreSQL schema, EF Core migrations, seed data |
| **Week 2** | Auth implementation | JWT token system, login/refresh/logout endpoints, RBAC |
| **Week 3** | Catalog API | 5 CRUD endpoints, CQRS handlers, validation |
| **Week 4** | Testing & docs | Unit + integration tests, OpenAPI spec, E2E verification |

**Estimated Duration:** 4 weeks
**Model:** Sonnet (production implementation)
**Priority:** HIGH (Phase 1 critical path)

---

## Success Criteria

- ✅ JWT authentication working (login → access token → API call → refresh)
- ✅ Multi-tenant isolation verified (Tenant A cannot access Tenant B data)
- ✅ Catalog CRUD API working (create/read/update/delete catalog items)
- ✅ RLS policies enforced (PostgreSQL level + application level)
- ✅ Tests pass (80%+ coverage)
- ✅ OpenAPI spec generated and validated

---

## References

- **Architecture Plan:** `/opt/spaceos/docs/joinerytech/BACKEND_ARCHITECTURE_PLAN.md`
- **Frontend Audit:** `/opt/spaceos/docs/joinerytech/AUDIT_UI_PERFORMANCE_A11Y_2026-07-02.md`
- **SpaceOS Kernel patterns:** `/opt/spaceos/backend/SpaceOS.Kernel/` (reference implementation)

---

**Generated:** 2026-07-02T18:00 UTC
**Terminal:** conductor
**Coordination:** JoineryTech Phase 1 Implementation Wave
