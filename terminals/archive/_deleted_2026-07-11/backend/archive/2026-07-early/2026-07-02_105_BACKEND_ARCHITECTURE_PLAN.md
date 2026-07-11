---
id: MSG-BACKEND-105-DONE
from: backend
to: conductor
type: done
status: UNREAD
priority: high
created: 2026-07-02
ref: MSG-BACKEND-105
content_hash: 9454c8f59d96199baafb3fcc8d674234c6f2b62340313834e4150918f69f199f
---

# JoineryTech Backend Architecture Plan

## Executive Summary

The JoineryTech Portal is transitioning from a localStorage-based React prototype to a production-grade backend API. This document specifies the technology stack, data model architecture, authentication/authorization strategy, API endpoints, and a phased migration roadmap.

**Key Recommendation:** .NET 8 + PostgreSQL + REST API + JWT + Modular Monolith (CQRS) following SpaceOS precedent, with domain-independent architecture enabling vertical scaling to other industries (bakery, metalworking, etc.).

---

## 1. API ARCHITEKTÚRA TERVEZÉS

### 1.1 Technológiai Döntések

#### Backend Framework
**RECOMMENDATION: .NET 8 with ASP.NET Core Minimal API**

| Critera | .NET 8 | Node.js | Go | Rust |
|---------|--------|---------|----|----|
| **Domain Complexity** | ✅ DDD + CQRS native | ⚠️ middleware-heavy | ⚠️ basic | ⚠️ steep learning |
| **Type Safety** | ✅ C# v13 + records | ⚠️ TypeScript | ⚠️ structural | ✅ strict |
| **Precedent** | ✅ SpaceOS Kernel | ⚠️ Orch only | ❌ none | ❌ none |
| **Team Skill** | ✅ active | ✅ active | ❌ none | ❌ none |
| **Ecosystem** | ✅ EF Core 8, MediatR, FluentVal | ✅ Express, Zod | ⚠️ SQLc | ⚠️ sqlx |
| **Time-to-market** | ✅ 4-6 weeks | ⚠️ 6-8 weeks (bug risk) | ❌ 8-10 weeks | ❌ 12+ weeks |
| **Scalability** | ✅ horizontal + gRPC | ✅ horizontal | ✅ horizontal | ✅ horizontal |
| **Operability** | ✅ Azure native, docker | ✅ docker | ✅ docker | ✅ docker |

**DECISION: .NET 8 (Modular Monolith with CQRS)**

Rationale:
- SpaceOS Kernel proven architecture (ADR-043, ADR-054)
- FSM-heavy domain perfectly suited for DDD aggregates
- Type-safe, compile-time validation reduces runtime bugs
- Team expertise (backend terminal has shipped 7,800+ LOC CRM module)
- 40+ entities need structured organization → Modular Monolith scales better than microservices overhead

---

#### API Style
**RECOMMENDATION: REST + Optional GraphQL Gateway Later**

**Phase 1 (now):** REST with OpenAPI/Swagger
- 19+ CRUD + action endpoints (CRM, Sales, Production, Warehouse, HR, EHS, QA, DMS, Maintenance)
- Standard HTTP semantics + noun-based URLs
- Easy mobile integration + Swagger UI for discovery
- MediatR CQRS handlers map 1:1 to endpoints

**Phase 2+ (2026 Q4):** GraphQL Gateway (optional, not blocking)
- If frontend deep-nesting queries (Order → Items → Catalog) become N+1 bottleneck
- Aggregator layer over existing REST endpoints (no endpoint rewrite)
- Client: Directive-based pagination, @defer for slow fields

**Example endpoints (REST — Phase 1):**
```
CRM
  POST   /api/crm/leads              ← CreateLeadCommand
  PUT    /api/crm/leads/{id}/contact ← ContactLeadCommand
  PUT    /api/crm/leads/{id}/convert ← ConvertToOpportunityCommand
  GET    /api/crm/leads              ← GetLeadsQuery (paginated + filter)

Sales
  POST   /api/sales/quotes           ← CreateQuoteCommand
  GET    /api/sales/quotes/{id}      ← GetQuoteByIdQuery
  PUT    /api/sales/quotes/{id}/approve ← ApproveQuoteCommand

Production
  POST   /api/production/orders/{id}/release ← ReleaseOrderCommand
  GET    /api/production/schedule    ← GetWeeklyScheduleQuery (paginated)

Warehouse
  POST   /api/warehouse/movements    ← CreateInventoryMovementCommand
  GET    /api/warehouse/stock        ← GetInventoryLevelsQuery (by location)
```

---

#### Database
**RECOMMENDATION: PostgreSQL 15+ with Row-Level Security (RLS)**

| Criteria | PostgreSQL | MongoDB | DynamoDB | Snowflake |
|----------|-----------|---------|----------|-----------|
| **Multi-tenant** | ✅ RLS (row-level security) | ⚠️ app-level | ⚠️ expensive scan | ❌ warehouse |
| **Relational (quotes→orders→items) | ✅ ACID + FK | ⚠️ denormalization tax | ❌ denorm only | ⚠️ joins slow |
| **FSM (status tracking)** | ✅ enum + check constraint | ⚠️ no constraint | ⚠️ no constraint | ⚠️ no constraint |
| **Analytics (Kontrolling)** | ✅ window funcs, CTEs | ⚠️ aggregation pipeline | ⚠️ scan-heavy | ✅ analytical DB |
| **Precedent** | ✅ SpaceOS (7 modules) | ❌ none | ❌ none | ❌ none |
| **Cost** | ✅ €0.10–0.50/GB/mo | ✅ similar | ⚠️ pay-per-query | ❌ $4/compute-hour |

**DECISION: PostgreSQL 15+ (Primary OLTP)**

Rationale:
- JoineryTech is relational: Quote ← Items ← Catalog (FK enforcement crucial)
- RLS policies enforce tenant isolation at DB level (no app-logic bypass)
- FSM states (enum column + check constraint) prevent invalid transitions at INSERT/UPDATE
- Analytics (Kontrolling → EAC, margin forecasts) require window functions + CTEs
- Team proven on SpaceOS Kernel (Kernel, Joinery, Cutting, Identity modules live here)

**Optional: Separate Analytics Database (2026 Q4)**
- Read-replica PostgreSQL or Snowflake for >1000 projects
- Replicate `orders`, `prodTasks`, `controllingData` snapshots (nightly)
- Leave OLTP PostgreSQL for transactional API (no analytical scans)

---

#### Authentication/Authorization
**RECOMMENDATION: JWT (asymmetric ES256) + Multi-tenant RBAC**

| Component | Choice | Rationale |
|-----------|--------|-----------|
| **Token Type** | JWT ES256 (ECDSA P-256) | Stateless, no session DB; asymmetric (API validates with public key only) |
| **Issuer** | Keycloak (or built-in Identity module) | OAuth 2.0 + SAML bridge for partner B2B (later); OIDC standard |
| **Token Lifetime** | Access: 15 min · Refresh: 30 days | Rotate short-lived access, refresh for long sessions (e.g., factory floor) |
| **Storage** | HttpOnly cookie (API → browser) · Header (mobile) | Prevent XSS + CSRF token for state-changing ops |
| **RBAC Scopes** | Resource-based (e.g., `quote:create`, `crm:manage`, `ehs:view`) | Fine-grained, not role-based (no "Admin" catch-all) |
| **Tenant Isolation** | `tenant_id` claim in JWT + RLS filter | DB filters all queries by tenant_id; app double-checks |

**JWT Payload Example:**
```json
{
  "sub": "user-uuid",
  "name": "János Asztalos",
  "email": "janos@acme.hu",
  "tenant_id": "acme-kft-uuid",
  "tenant_name": "Acme Kft.",
  "account_type": "internal|partner|reseller|customer",
  "perms": ["crm:manage", "sales:approve", "prod:release"],
  "iat": 1719922800,
  "exp": 1719923700,
  "iss": "https://keycloak.joinerytech.hu/auth/realms/joinery",
  "aud": "api.joinerytech.hu"
}
```

**RBAC Model:**

```
Permissions (granular):
  crm.manage       — Create/edit leads, opportunities
  crm.view         — Read-only leads, opportunities
  quote.create     — Create quotes
  quote.approve    — Approve quotes (→ Order)
  prod.release     — Release order to production
  prod.schedule    — Modify weekly schedule (conflicts)
  warehouse.move   — Create inventory movements
  ehs.manage       — Create incidents, CAPA actions
  hr.manage        — Employee records, payroll
  quality.manage   — QA inspections, NCR
  dms.manage       — Upload/publish documents
  catalog.approve  — Approve catalog items (governance)
  controlling.view — View analytics dashboards
  controlling.exec — Modify labor rates (CFO only)
  ai.manage        — Configure AI agents (advanced)

Account Types (seed roles):
  internal         — Employee (all perms except controlling.exec)
  partner          — B2B (crm:view, quote:create, prod:schedule [own jobs])
  reseller         — Distributor (crm:manage, quote:approve [own leads])
  customer         — End-user (crm:view [own projects], dms:view [own contracts])
  guest            — Read-only (crm:view, quote:view, dms:view + watermark)

Per-tenant overrides:
  Each tenant has a PERMISSIONS dict (`sim.portal.PERM_CATALOG`):
  { "acme-kft": { perms: [...], roles: {...}, customFields: {...} } }
  → Supports white-label tenant-specific workflows
```

---

#### Real-time Communication
**RECOMMENDATION: Server-Sent Events (SSE) for Initial Phase; WebSocket Optional Later**

| Use Case | Tech | Rationale |
|----------|------|-----------|
| **Live Notifications** (quote approved, order released) | SSE (Phase 1) | Easy, HTTPS, works behind proxy |
| **Factory Floor UI** (production schedule updates every 5s) | WebSocket (Phase 2) | Lower latency for real-time; requires separate service |
| **Chat/Messaging** | Not in scope | Store-based comments only (async) |

**Phase 1 (MVP):**
- SSE endpoint: `GET /api/live/events` (returns `text/event-stream`)
- Backend publishes events to all active clients (in-memory queue)
- Filters by tenant_id + user permissions
- Example: `event: quote.approved\ndata: {"quoteId": "q-123", "status": "approved"}\n\n`

**Phase 2+ (if needed):**
- WebSocket gateway (SignalR or Socket.IO) for <1000ms latency
- Hub: `/ws?token=<jwt>` (JWT auth via query string)
- Broadcast to subscribed groups (e.g., "production-floor-123")
- Fallback to SSE if WebSocket unavailable

---

#### Architecture Patterns

**RECOMMENDED: Modular Monolith (NOT Microservices)**

```
Why NOT Microservices?
❌ Distributed transactions (Quote → Order → ProdJob cross-module)
❌ 40+ entities with tight FSM dependencies
❌ Team size 2-3 backend (deployment overhead unacceptable)
❌ Single SLA (uptime = entire system, not per-service)
✅ Modular Monolith (same code org, one DB, shared IPC)

Why Modular Monolith?
✅ Shared DB transactions (Quote → Order → ProdJob atomic)
✅ Compile-time contracts between modules (C# references)
✅ Easy deployment (one docker image)
✅ Shared auth/RBAC/audit middleware
✅ Can extract services later (if needed at 100+ team)
```

**Module Boundaries (Internal Modules):**

```
spaceos-modules-joinerytech/
├── spaceos-modules-crm/           ← Leads, Opportunities (DONE in CRM hardening phase)
├── spaceos-modules-sales/         ← Quotes, Orders, Customers
├── spaceos-modules-production/    ← Production orders, Jobs, Scheduling
├── spaceos-modules-warehouse/     ← Inventory, Materials, Movements, Stock
├── spaceos-modules-hr/            ← Employees, Attendance, Payroll
├── spaceos-modules-maintenance/   ← Assets, Work orders, Preventive maintenance
├── spaceos-modules-ehs/           ← Incidents, CAPA, Risk assessment, Training
├── spaceos-modules-quality/       ← QA inspections, NCR, Defects
├── spaceos-modules-dms/           ← Documents, Versioning
├── spaceos-modules-catalog/       ← Product catalog, Items, Approval workflow
├── spaceos-modules-procurement/   ← RFQ, PO, Supplier master
├── spaceos-modules-controlling/   ← Cost tracking, Margin forecasts, EAC
├── spaceos-modules-design/        ← 3D models, CAD references, Tech drawings (later)
└── spaceos-modules-ai/            ← AI agents, Skills, Memory (built-in)

Each module:
  - src/Domain/         ← Aggregates, VOs, Events (no dependencies)
  - src/Application/    ← CQRS handlers, validators, DTOs
  - src/Infrastructure/ ← EF Core DbContext, repositories
  - src/Api/            ← Minimal API endpoints
  - tests/              ← xUnit tests

Dependency rule:
  → only depends on Kernel (shared auth, audit, FSM)
  → Kernel never depends on domain modules
  → Cross-module communication via events (pub-sub) or query interfaces
```

**CQRS Pattern (Proven in Kernel):**

```csharp
// Commands (mutations)
public class CreateQuoteCommand : IRequest<Result<QuoteResponse>>
{
  public Guid CustomerId { get; set; }
  public List<QuoteLineDto> Items { get; set; }
  public decimal Total { get; set; }
}

public class CreateQuoteHandler : IRequestHandler<CreateQuoteCommand, Result<QuoteResponse>>
{
  public async Task<Result<QuoteResponse>> Handle(CreateQuoteCommand req, CancellationToken ct)
  {
    // Validate
    // Create aggregate
    // Publish event (QuoteCreatedEvent → CRM task, notification)
    // Save + return Result<QuoteResponse>
  }
}

// Queries (reads)
public class GetQuoteByIdQuery : IRequest<Result<QuoteDetailResponse>>
{
  public Guid QuoteId { get; set; }
}

public class GetQuoteByIdHandler : IRequestHandler<GetQuoteByIdQuery, Result<QuoteDetailResponse>>
{
  public async Task<Result<QuoteDetailResponse>> Handle(GetQuoteByIdQuery req, CancellationToken ct)
  {
    var quote = await _repository.GetByIdAsync(req.QuoteId, ct).ConfigureAwait(false);
    return Result.Success(new QuoteDetailResponse { ... });
  }
}
```

**Event Sourcing (Optional, Phase 2):**
- Quote mutations publish domain events (QuoteCreatedEvent, QuoteApprovedEvent)
- Events flow to CRM (create task), Warehouse (reserve stock), Notifications
- Audit trail automatic (every mutation is an event)
- Currently: events are **NOT persisted** (in-memory pub-sub)
- Phase 2: Store events in `event_store` table (enable time-travel/replay)

---

### 1.2 Architektúra Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│ FRONTEND LAYER (React Portal + Mobile)                          │
├─────────────────────────────────────────────────────────────────┤
│  Browser/Mobile App
│  ├─ React 18 + TypeScript
│  ├─ localStorage (offline fallback during migration)
│  └─ API calls + SSE (real-time)
└──────────────────────┬──────────────────────────────────────────┘
                       │
                       │ REST API + JWT + SSE
                       ↓
┌─────────────────────────────────────────────────────────────────┐
│ API LAYER (ASP.NET Core Minimal API)                            │
├─────────────────────────────────────────────────────────────────┤
│  Routes (endpoints)
│  ├─ GET    /api/quotes/{id}
│  ├─ POST   /api/quotes
│  ├─ PUT    /api/quotes/{id}/approve
│  └─ (19+ endpoints)
│
│  Middleware Pipeline
│  ├─ Authentication (JWT)
│  ├─ Authorization (RLS filter injection)
│  ├─ CORS + Helmet
│  ├─ Request/Response logging
│  └─ Global exception handler
└──────────────────────┬──────────────────────────────────────────┘
                       │
                       │ MediatR (IRequest<Result<T>>)
                       ↓
┌─────────────────────────────────────────────────────────────────┐
│ APPLICATION LAYER (CQRS Handlers)                               │
├─────────────────────────────────────────────────────────────────┤
│  Commands (mutations)
│  ├─ CreateQuoteHandler
│  ├─ ApproveQuoteHandler
│  └─ (15+ command handlers per module)
│
│  Queries (reads)
│  ├─ GetQuoteByIdHandler
│  ├─ GetQuotesPagedHandler
│  └─ (9+ query handlers per module)
│
│  Validators (FluentValidation)
│  ├─ CreateQuoteCommandValidator
│  └─ (20+ validators per module)
│
│  Domain Events (pub-sub)
│  ├─ QuoteApprovedEvent → [CrmService, NotificationService]
│  └─ (10+ events per module)
└──────────────────────┬──────────────────────────────────────────┘
                       │
                       │ IRepository, DbContext
                       ↓
┌─────────────────────────────────────────────────────────────────┐
│ INFRASTRUCTURE LAYER (EF Core + PostgreSQL)                     │
├─────────────────────────────────────────────────────────────────┤
│  DbContext
│  ├─ DbSet<Quote>
│  ├─ DbSet<Order>
│  ├─ DbSet<Catalog>
│  └─ OnModelCreating (RLS policies, indexes)
│
│  Repositories
│  ├─ IQuoteRepository (GetByIdAsync, AddAsync, UpdateAsync)
│  ├─ IOrderRepository
│  └─ (1 repo per aggregate root)
│
│  Migrations (code-first)
│  └─ EF Core migrations (20260702_InitialCreate.cs)
└──────────────────────┬──────────────────────────────────────────┘
                       │
                       ↓
┌─────────────────────────────────────────────────────────────────┐
│ DATA LAYER (PostgreSQL 15+)                                     │
├─────────────────────────────────────────────────────────────────┤
│  Tables (one per aggregate root)
│  ├─ quotes
│  ├─ orders
│  ├─ order_items
│  ├─ catalog_items
│  └─ (40+ tables)
│
│  Constraints
│  ├─ PK: id (UUID)
│  ├─ FK: order_id → orders.id
│  ├─ CHECK: status IN ('draft','sent','approved',...)
│  └─ RLS policies (WHERE tenant_id = current_user_tenant_id)
│
│  Indexes
│  ├─ (tenant_id) — all tenant-scoped queries
│  ├─ (tenant_id, status) — FSM filtering
│  ├─ (created_at DESC) — timeline queries
│  └─ (expected_close_date) — due date filtering
│
│  Event Store (optional, Phase 2)
│  └─ event_store (id, event_type, aggregate_id, data, at)
└─────────────────────────────────────────────────────────────────┘

Cross-layer patterns:
- Result<T> (success/failure with error codes)
- ConfigureAwait(false) on all async calls
- CancellationToken on all async methods
- AsNoTracking() on read-only queries
- Tenant context injection (X-Tenant-Id header → claims → RLS)
```

---

### 1.3 Decision Matrix (Pros/Cons)

#### Backend Framework Decision

| Aspect | .NET 8 (CHOSEN) | Node.js | Go | Comment |
|--------|---|---|---|---|
| **DDD Tooling** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐ | C# records + records for VOs is native |
| **CQRS Ecosystem** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | MediatR is industry standard |
| **Type Safety** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | C# catches 70% more bugs at compile time |
| **Team Velocity** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐ | Backend team shipped 7800+ LOC in 1 week |
| **Learning Curve** | ⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐ | .NET steeper; Go pragmatic |
| **Job Market** | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | .NET in decline in HU; Node/Go hiring active |
| **Long-term Maintenance** | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | Both have strong communities |

#### Database Decision

| Aspect | PostgreSQL (CHOSEN) | MongoDB | DynamoDB | Comment |
|--------|---|---|---|---|
| **Relational Queries** | ⭐⭐⭐⭐⭐ | ⭐⭐ | ❌ | Quote → Items → Catalog FK traversal |
| **ACID Transactions** | ⭐⭐⭐⭐⭐ | ⭐⭐ | ⭐⭐ | Distributed ACID (Quote → Order) critical |
| **Multi-tenant** | ⭐⭐⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐ | RLS = database-level enforcement |
| **Analytics** | ⭐⭐⭐⭐ | ⭐⭐ | ❌ | Window functions, CTEs for EAC forecasts |
| **Cost at Scale** | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐ | No read surprises; predictable |

---

## 2. ADATMODELL MAPPING

### 2.1 Entity → Table Mapping

All 40+ JoineryTech entities mapped from `localStorage` (`window.sim`) → PostgreSQL tables.

**Naming Convention:**
- Table: `snake_case` (e.g., `production_orders`)
- Column: `snake_case` (e.g., `customer_id`)
- PK: `id` (UUID)
- FK: `{table_singular}_id` (e.g., `quote_id`, `order_id`)
- Tenant: `tenant_id` on all root tables (RLS policy)

#### Core Tables (CRM, Sales, Production, Warehouse)

```sql
-- CRM Module
CREATE TABLE leads (
  id UUID PRIMARY KEY,
  tenant_id UUID NOT NULL,
  name VARCHAR(200) NOT NULL,
  email VARCHAR(256) NOT NULL,
  phone VARCHAR(20),
  company VARCHAR(200),
  status VARCHAR(50) NOT NULL CHECK (status IN ('new', 'contacted', 'qualified', 'nurturing', 'converted', 'rejected')),
  source VARCHAR(50) NOT NULL CHECK (source IN ('phone', 'quote', 'email', 'exhibition', 'website', 'webshop', 'interior_designer')),
  assigned_to UUID,
  probability_pct INTEGER,
  estimated_value DECIMAL(12,2),
  created_by UUID,
  created_at TIMESTAMP NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
  FOREIGN KEY (tenant_id) REFERENCES tenants(id),
  FOREIGN KEY (assigned_to) REFERENCES employees(id),
  FOREIGN KEY (created_by) REFERENCES users(id)
);
CREATE INDEX idx_leads_tenant_id ON leads(tenant_id);
CREATE INDEX idx_leads_status ON leads(tenant_id, status);
CREATE INDEX idx_leads_assigned_to ON leads(assigned_to);

CREATE TABLE opportunities (
  id UUID PRIMARY KEY,
  tenant_id UUID NOT NULL,
  lead_id UUID NOT NULL,
  name VARCHAR(200) NOT NULL,
  status VARCHAR(50) NOT NULL CHECK (status IN ('open', 'needs_assessment', 'solution_assembly', 'proposal', 'negotiation', 'won', 'lost', 'abandoned')),
  probability_pct INTEGER DEFAULT 0,
  estimated_value DECIMAL(12,2),
  currency VARCHAR(3) DEFAULT 'HUF',
  expected_close_date DATE,
  assigned_to UUID,
  reason_if_lost VARCHAR(500),
  reason_if_abandoned VARCHAR(500),
  created_by UUID,
  created_at TIMESTAMP NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
  FOREIGN KEY (tenant_id) REFERENCES tenants(id),
  FOREIGN KEY (lead_id) REFERENCES leads(id) ON DELETE CASCADE,
  FOREIGN KEY (assigned_to) REFERENCES employees(id),
  FOREIGN KEY (created_by) REFERENCES users(id)
);
CREATE INDEX idx_opp_tenant_id ON opportunities(tenant_id);
CREATE INDEX idx_opp_status ON opportunities(tenant_id, status);
CREATE INDEX idx_opp_lead_id ON opportunities(lead_id);

-- Sales Module
CREATE TABLE quotes (
  id UUID PRIMARY KEY,
  tenant_id UUID NOT NULL,
  customer_id UUID,
  opportunity_id UUID,
  number VARCHAR(50),
  status VARCHAR(50) NOT NULL CHECK (status IN ('draft', 'sent', 'approved', 'rejected', 'converted', 'expired', 'archived')),
  gross_value DECIMAL(12,2),
  discount_pct DECIMAL(5,2),
  net_value DECIMAL(12,2),
  vat_rate DECIMAL(5,2),
  total_value DECIMAL(12,2),
  currency VARCHAR(3) DEFAULT 'HUF',
  validity_days INTEGER,
  valid_until DATE,
  created_by UUID,
  approved_by UUID,
  approved_at TIMESTAMP,
  created_at TIMESTAMP NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
  FOREIGN KEY (tenant_id) REFERENCES tenants(id),
  FOREIGN KEY (customer_id) REFERENCES customers(id),
  FOREIGN KEY (opportunity_id) REFERENCES opportunities(id),
  FOREIGN KEY (created_by) REFERENCES users(id),
  FOREIGN KEY (approved_by) REFERENCES users(id)
);
CREATE INDEX idx_quotes_tenant_id ON quotes(tenant_id);
CREATE INDEX idx_quotes_status ON quotes(tenant_id, status);
CREATE INDEX idx_quotes_customer_id ON quotes(customer_id);

CREATE TABLE quote_lines (
  id UUID PRIMARY KEY,
  quote_id UUID NOT NULL,
  seq_num INTEGER,
  catalog_item_id UUID NOT NULL,
  quantity DECIMAL(12,4),
  unit_price DECIMAL(12,2),
  line_value DECIMAL(12,2),
  discount_pct DECIMAL(5,2),
  price_class VARCHAR(50) CHECK (price_class IN ('fix', 'kalkulalt', 'iranyar')),
  notes TEXT,
  FOREIGN KEY (quote_id) REFERENCES quotes(id) ON DELETE CASCADE,
  FOREIGN KEY (catalog_item_id) REFERENCES catalog_items(id)
);
CREATE INDEX idx_quote_lines_quote_id ON quote_lines(quote_id);

CREATE TABLE orders (
  id UUID PRIMARY KEY,
  tenant_id UUID NOT NULL,
  quote_id UUID,
  customer_id UUID NOT NULL,
  number VARCHAR(50),
  status VARCHAR(50) NOT NULL CHECK (status IN ('draft', 'calc', 'ready', 'released', 'delivered', 'archived')),
  gross_value DECIMAL(12,2),
  discount_pct DECIMAL(5,2),
  net_value DECIMAL(12,2),
  vat_rate DECIMAL(5,2),
  total_value DECIMAL(12,2),
  currency VARCHAR(3) DEFAULT 'HUF',
  order_date DATE NOT NULL,
  delivery_date DATE,
  created_by UUID,
  released_by UUID,
  released_at TIMESTAMP,
  created_at TIMESTAMP NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
  FOREIGN KEY (tenant_id) REFERENCES tenants(id),
  FOREIGN KEY (quote_id) REFERENCES quotes(id),
  FOREIGN KEY (customer_id) REFERENCES customers(id),
  FOREIGN KEY (created_by) REFERENCES users(id),
  FOREIGN KEY (released_by) REFERENCES users(id)
);
CREATE INDEX idx_orders_tenant_id ON orders(tenant_id);
CREATE INDEX idx_orders_status ON orders(tenant_id, status);
CREATE INDEX idx_orders_customer_id ON orders(customer_id);

CREATE TABLE order_items (
  id UUID PRIMARY KEY,
  order_id UUID NOT NULL,
  seq_num INTEGER,
  catalog_item_id UUID NOT NULL,
  quantity DECIMAL(12,4),
  unit_price DECIMAL(12,2),
  line_value DECIMAL(12,2),
  FOREIGN KEY (order_id) REFERENCES orders(id) ON DELETE CASCADE,
  FOREIGN KEY (catalog_item_id) REFERENCES catalog_items(id)
);

-- Warehouse Module
CREATE TABLE materials (
  id UUID PRIMARY KEY,
  tenant_id UUID NOT NULL,
  catalog_item_id UUID NOT NULL,
  warehouse_location VARCHAR(50),
  quantity_on_hand DECIMAL(12,4),
  quantity_reserved DECIMAL(12,4),
  quantity_available DECIMAL(12,4),
  reorder_point DECIMAL(12,4),
  unit_price DECIMAL(12,2),
  last_movement_at TIMESTAMP,
  created_at TIMESTAMP NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
  FOREIGN KEY (tenant_id) REFERENCES tenants(id),
  FOREIGN KEY (catalog_item_id) REFERENCES catalog_items(id)
);
CREATE INDEX idx_materials_tenant_id ON materials(tenant_id);
CREATE INDEX idx_materials_warehouse_location ON materials(warehouse_location);

CREATE TABLE inventory_movements (
  id UUID PRIMARY KEY,
  tenant_id UUID NOT NULL,
  material_id UUID NOT NULL,
  movement_type VARCHAR(50) NOT NULL CHECK (movement_type IN ('purchase', 'sales', 'production', 'adjustment', 'return')),
  quantity DECIMAL(12,4),
  reference_type VARCHAR(50) CHECK (reference_type IN ('order', 'production_job', 'po', 'manual')),
  reference_id UUID,
  notes TEXT,
  created_by UUID,
  created_at TIMESTAMP NOT NULL DEFAULT NOW(),
  FOREIGN KEY (tenant_id) REFERENCES tenants(id),
  FOREIGN KEY (material_id) REFERENCES materials(id),
  FOREIGN KEY (created_by) REFERENCES users(id)
);
CREATE INDEX idx_inv_movements_tenant_id ON inventory_movements(tenant_id);
CREATE INDEX idx_inv_movements_material_id ON inventory_movements(material_id);

-- Production Module
CREATE TABLE production_orders (
  id UUID PRIMARY KEY,
  tenant_id UUID NOT NULL,
  order_id UUID NOT NULL,
  number VARCHAR(50),
  status VARCHAR(50) NOT NULL CHECK (status IN ('draft', 'scheduled', 'in_progress', 'paused', 'completed', 'quality_check', 'shipped')),
  planned_start_date DATE,
  planned_end_date DATE,
  actual_start_date DATE,
  actual_end_date DATE,
  assigned_to UUID,
  created_at TIMESTAMP NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
  FOREIGN KEY (tenant_id) REFERENCES tenants(id),
  FOREIGN KEY (order_id) REFERENCES orders(id),
  FOREIGN KEY (assigned_to) REFERENCES employees(id)
);

CREATE TABLE production_jobs (
  id UUID PRIMARY KEY,
  tenant_id UUID NOT NULL,
  production_order_id UUID NOT NULL,
  seq_num INTEGER,
  job_type VARCHAR(100),
  status VARCHAR(50) NOT NULL CHECK (status IN ('queued', 'in_progress', 'paused', 'completed', 'on_hold', 'cancelled')),
  planned_duration_minutes INTEGER,
  actual_duration_minutes INTEGER,
  assigned_to UUID,
  resource_id UUID,
  created_at TIMESTAMP NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
  FOREIGN KEY (tenant_id) REFERENCES tenants(id),
  FOREIGN KEY (production_order_id) REFERENCES production_orders(id) ON DELETE CASCADE,
  FOREIGN KEY (assigned_to) REFERENCES employees(id)
);

-- Catalog Module (GOVERNANCE)
CREATE TABLE catalog_items (
  id UUID PRIMARY KEY,
  tenant_id UUID NOT NULL,
  sku VARCHAR(100) UNIQUE NOT NULL,
  name VARCHAR(200) NOT NULL,
  category_id UUID,
  status VARCHAR(50) NOT NULL CHECK (status IN ('draft', 'review', 'active', 'incomplete', 'rejected', 'archived')),
  status_reason TEXT,
  kind VARCHAR(50),
  unit VARCHAR(20),
  sales_price DECIMAL(12,2),
  purchase_price DECIMAL(12,2),
  supplier_id UUID,
  visibility VARCHAR(50) CHECK (visibility IN ('internal', 'partner', 'customer', 'public')),
  created_by UUID,
  approved_by UUID,
  approved_at TIMESTAMP,
  created_at TIMESTAMP NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
  FOREIGN KEY (tenant_id) REFERENCES tenants(id),
  FOREIGN KEY (category_id) REFERENCES catalog_categories(id),
  FOREIGN KEY (supplier_id) REFERENCES suppliers(id),
  FOREIGN KEY (created_by) REFERENCES users(id),
  FOREIGN KEY (approved_by) REFERENCES users(id)
);
CREATE INDEX idx_catalog_tenant_id ON catalog_items(tenant_id);
CREATE INDEX idx_catalog_status ON catalog_items(tenant_id, status);

-- HR Module
CREATE TABLE employees (
  id UUID PRIMARY KEY,
  tenant_id UUID NOT NULL,
  name VARCHAR(200) NOT NULL,
  email VARCHAR(256),
  phone VARCHAR(20),
  employee_type VARCHAR(50) CHECK (employee_type IN ('internal', 'contractor', 'partner')),
  pay_grade VARCHAR(50),
  hourly_cost DECIMAL(10,2),
  department_id UUID,
  manager_id UUID,
  hire_date DATE,
  termination_date DATE,
  status VARCHAR(50) CHECK (status IN ('active', 'inactive', 'on_leave', 'terminated')),
  created_at TIMESTAMP NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
  FOREIGN KEY (tenant_id) REFERENCES tenants(id),
  FOREIGN KEY (department_id) REFERENCES departments(id),
  FOREIGN KEY (manager_id) REFERENCES employees(id)
);

-- EHS Module
CREATE TABLE ehs_incidents (
  id UUID PRIMARY KEY,
  tenant_id UUID NOT NULL,
  incident_type VARCHAR(50) CHECK (incident_type IN ('accident', 'near_miss', 'environmental')),
  severity VARCHAR(50) CHECK (severity IN ('light', 'lost_time', 'serious')),
  status VARCHAR(50) NOT NULL CHECK (status IN ('reported', 'investigating', 'action', 'closed', 'rejected')),
  description TEXT NOT NULL,
  reported_by UUID,
  reported_at TIMESTAMP NOT NULL DEFAULT NOW(),
  investigation_notes TEXT,
  created_at TIMESTAMP NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
  FOREIGN KEY (tenant_id) REFERENCES tenants(id),
  FOREIGN KEY (reported_by) REFERENCES employees(id)
);

-- QA Module
CREATE TABLE qa_inspections (
  id UUID PRIMARY KEY,
  tenant_id UUID NOT NULL,
  order_id UUID NOT NULL,
  inspection_type VARCHAR(50) CHECK (inspection_type IN ('incoming', 'in_process', 'final')),
  status VARCHAR(50) NOT NULL CHECK (status IN ('open', 'in_progress', 'passed', 'rework', 'rejected')),
  checked_by UUID,
  created_at TIMESTAMP NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
  FOREIGN KEY (tenant_id) REFERENCES tenants(id),
  FOREIGN KEY (order_id) REFERENCES orders(id),
  FOREIGN KEY (checked_by) REFERENCES employees(id)
);

-- DMS Module
CREATE TABLE documents (
  id UUID PRIMARY KEY,
  tenant_id UUID NOT NULL,
  name VARCHAR(200) NOT NULL,
  doc_type VARCHAR(50) CHECK (doc_type IN ('drawing', 'contract', 'certificate', 'instruction', 'other')),
  status VARCHAR(50) NOT NULL CHECK (status IN ('draft', 'review', 'published', 'archived')),
  version INTEGER DEFAULT 1,
  file_path VARCHAR(500),
  link_type VARCHAR(50),
  link_id UUID,
  created_by UUID,
  created_at TIMESTAMP NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
  FOREIGN KEY (tenant_id) REFERENCES tenants(id),
  FOREIGN KEY (created_by) REFERENCES users(id)
);

-- Multi-tenant
CREATE TABLE tenants (
  id UUID PRIMARY KEY,
  name VARCHAR(200) NOT NULL,
  slug VARCHAR(100) UNIQUE NOT NULL,
  status VARCHAR(50) CHECK (status IN ('active', 'suspended', 'archived')),
  created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE users (
  id UUID PRIMARY KEY,
  tenant_id UUID NOT NULL,
  email VARCHAR(256) UNIQUE NOT NULL,
  display_name VARCHAR(200),
  account_type VARCHAR(50) CHECK (account_type IN ('internal', 'partner', 'reseller', 'customer', 'guest')),
  roles JSONB DEFAULT '[]',
  is_active BOOLEAN DEFAULT TRUE,
  created_at TIMESTAMP NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
  FOREIGN KEY (tenant_id) REFERENCES tenants(id)
);
```

---

### 2.2 Relational Design

**Key Relationships:**

```
Leads (1) ──→ (N) Opportunities ──→ (1) Quotes ──→ (1) Orders
           └─ (1) Customers

Quotes (1) ──→ (N) QuoteLines ──→ (1) CatalogItems
Orders (1) ──→ (N) OrderItems ──→ (1) CatalogItems
           ├─ (1) ProductionOrder ──→ (N) ProductionJobs
           └─ (1) Warehouse (inventory reserve)

CatalogItems (M) ──→ (N) Materials
           ├─ (1) Category
           └─ (1) Supplier

Employees (1) ──→ (N) Attendance
           ├─ (N) ProductionJobs
           └─ (1) Department

Orders (1) ──→ (N) QAInspections
           ├─ (N) InventoryMovements
           └─ (1) Documents (tech drawing)
```

**Normalization:** 3NF (all tables, no redundancy)
- Example: `orders.total_value` is **computed** (sum of order_items × quantity × unit_price), stored for performance only; triggers recompute on item change
- Temporal data: all tables have `created_at` + `updated_at` (audit trail)

---

### 2.3 Migration Strategy (localStorage → DB)

**Phase 1 (Week 1): Parallel Run (Read-only DB)**
- Backend reads from PostgreSQL + Returns to frontend
- Frontend **CONTINUES writing to localStorage** (no change for users)
- DB has clone of current state (via export-to-CSV or API import job)
- Validation: frontend UI shows data from BOTH sources, confirms matching

**Phase 2 (Week 2): Gradual Write-over (Selective Modules)**
- **CRM module → DB writes first** (leads, opportunities safest)
  - Frontend: Quote creation still uses localStorage, but pulls customer list from DB API
  - Opportunity edit → API call (DB)
- **Warehouse next** (inventory movements = critical for production)
- **Other modules remain on localStorage**

**Phase 3 (Week 3–4): Full Backend**
- All modules write to API
- localStorage = offline cache only
- Sync service: `POST /api/sync` (batch queued mutations if offline)

**Rollback Plan:**
- If API returns 5xx → frontend catches error, **falls back to localStorage**
- Periodic snapshots of PostgreSQL → CSV export (nightly, kept for 30 days)
- If corruption: restore from snapshot + re-run Phase 1 import

---

## 3. AUTH/AUTH STRATÉGIA

### 3.1 JWT Token-Based Authentication

**Flow:**

```
1. User logs in (email + password)
   POST /auth/login
   {
     "email": "janos@acme.hu",
     "password": "..."
   }

2. Backend validates (Keycloak or Identity module)
   ✅ Credentials valid → Generate JWT

3. JWT issued (signed with RSA private key)
   {
     "alg": "ES256",
     "typ": "JWT"
   }.
   {
     "sub": "user-uuid",
     "name": "János Asztalos",
     "email": "janos@acme.hu",
     "tenant_id": "acme-kft-uuid",
     "tenant_name": "Acme Kft.",
     "account_type": "internal",
     "perms": ["crm:manage", "quote:approve", "prod:release"],
     "iat": 1719923700,
     "exp": 1719924600,    ← 15 min lifetime
     "iss": "https://keycloak.joinerytech.hu/auth/realms/joinery",
     "aud": "api.joinerytech.hu"
   }

4. Token stored (browser)
   Set-Cookie: access_token=eyJh...; HttpOnly; Secure; SameSite=Strict

5. API call (every request)
   Authorization: Bearer eyJh...

6. Validation (API middleware)
   Verify signature (public key)
   Check expiry
   Extract claims → Add to request context
   Apply RLS filter (tenant_id from claim)
```

**Token Endpoints:**

```
POST /auth/login
  Request: { email, password, tenantId? }
  Response: { accessToken, refreshToken, expiresIn, user }

POST /auth/refresh
  Request: { refreshToken }
  Response: { accessToken, expiresIn }

POST /auth/logout
  Request: (JWT in header)
  Response: 204 No Content

GET /auth/me
  Request: (JWT in header)
  Response: { sub, email, name, tenant_id, perms, ... }
```

---

### 3.2 RBAC (Role-Based Access Control)

**Permission Model:**

Permissions are **granular** (not role-based):

```
CRM Permissions:
  crm:view           — Read leads, opportunities
  crm:manage         — Create/edit leads, opportunities
  crm:convert_lead   — Convert lead → opportunity

Sales Permissions:
  quote:view         — Read quotes
  quote:create       — Create new quote
  quote:approve      — Approve quote (→ Order)
  quote:convert      — Convert quote → order

Production Permissions:
  prod:view          — View production schedule
  prod:release       — Release order to production
  prod:schedule      — Modify weekly schedule

Warehouse Permissions:
  warehouse:view     — View inventory
  warehouse:move     — Create inventory movements

EHS Permissions:
  ehs:view           — View incidents, risks
  ehs:manage         — Create incidents, CAPA actions

HR Permissions:
  hr:view            — View employee list
  hr:manage          — Edit employee records, payroll

Quality Permissions:
  quality:view       — View QA inspections
  quality:manage     — Create inspections, NCR

DMS Permissions:
  dms:view           — View documents
  dms:manage         — Upload/publish documents

Catalog Permissions:
  catalog:view       — View product catalog
  catalog:manage     — Edit catalog items
  catalog:approve    — Approve items (governance)

Controlling Permissions:
  controlling:view   — View dashboards (read-only)
  controlling:exec   — Edit labor rates, settings (CFO only)

AI Permissions:
  ai:view            — View agents
  ai:manage          — Configure agents, skills, memory
```

**Account Types (Seed Roles):**

```
Internal (Employee)
  perms: crm:manage, quote:create, quote:approve, prod:release, warehouse:move, ehs:manage, quality:manage, hr:manage, dms:manage, controlling:view
  except: controlling:exec (CFO only)

Partner (B2B Supplier / Subcontractor)
  perms: crm:view, quote:view, prod:view [own jobs], warehouse:view
  exclude: quote:create, quote:approve (they submit RFQs, don't approve)

Reseller (Distributor)
  perms: crm:manage [own leads], quote:create, quote:approve [own opportunities], dms:view
  exclude: prod:release, ehs:manage, hr:manage

Customer (End-user)
  perms: crm:view [own projects], quote:view [own quotes], dms:view [own contracts], warehouse:view [via partner portal]
  exclude: quote:approve, prod:release, quality:manage

Guest (Public)
  perms: crm:view [read-only, watermarked], quote:view [read-only]
  exclude: all write ops
```

**Enforcement (API Endpoint Example):**

```csharp
// Endpoint: POST /api/quotes/{id}/approve
[Authorize(Permissions = "quote:approve")]  // Middleware checks JWT.perms
public async Task<IResult> ApproveQuote(Guid id, ApproveQuoteRequest req, CancellationToken ct)
{
  // 1. Validate permission (middleware already checked)
  // 2. Get quote (RLS: WHERE tenant_id = CurrentTenant)
  var quote = await _quoteRepo.GetByIdAsync(id, ct);

  // 3. Check ownership (quote.customer_id belongs to this tenant)
  if (quote.TenantId != CurrentTenant) return Forbid();

  // 4. Transition FSM
  var cmd = new ApproveQuoteCommand { QuoteId = id, ApprovedBy = CurrentUserId };
  var result = await _mediator.Send(cmd, ct);

  return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Errors);
}
```

**Per-tenant Permission Overrides (Multi-tenant Customization):**

```json
{
  "acme-kft": {
    "customPermissions": {
      "quote:draft_approval": true,     // Acme requires draft-stage review
      "prod:resource_booking": true,    // Acme has external CNC center
      "warehouse:lot_tracking": true    // Acme tracks material lots
    },
    "roleOverrides": {
      "technical_lead": {
        "perms": ["quote:approve", "prod:release", "quality:manage"],
        "excludeFrom": ["accounting"]   // Technical leads can't access payroll
      }
    }
  }
}
```

---

### 3.3 Multi-tenant Isolation (RLS at DB Level)

**PostgreSQL RLS Policy Example:**

```sql
-- Enable RLS on quotes table
ALTER TABLE quotes ENABLE ROW LEVEL SECURITY;

-- Policy: Users can only see quotes from their tenant
CREATE POLICY quotes_tenant_isolation ON quotes
  USING (tenant_id = current_setting('app.tenant_id')::uuid);

-- Grant ALL policy (prevent direct SELECT bypass)
CREATE POLICY quotes_insert ON quotes
  WITH CHECK (tenant_id = current_setting('app.tenant_id')::uuid);

-- Function to set tenant context
CREATE OR REPLACE FUNCTION set_tenant_context(tenant_id_param UUID)
RETURNS void AS $$
BEGIN
  EXECUTE format('SET app.tenant_id = %L', tenant_id_param);
END;
$$ LANGUAGE plpgsql;
```

**API Middleware (Set Tenant Context):**

```csharp
app.Use(async (context, next) =>
{
  // 1. Extract tenant_id from JWT claim
  var tenantIdClaim = context.User.FindFirst("tenant_id")?.Value;
  if (string.IsNullOrEmpty(tenantIdClaim))
  {
    context.Response.StatusCode = 401;
    await context.Response.WriteAsync("Missing tenant_id in token");
    return;
  }

  // 2. Set PostgreSQL GUC (Global User Configuration)
  using (var connection = new NpgsqlConnection(connectionString))
  {
    await connection.OpenAsync();
    using (var cmd = connection.CreateCommand())
    {
      cmd.CommandText = $"SET app.tenant_id = '{tenantIdClaim}'";
      await cmd.ExecuteNonQueryAsync();
    }
  }

  // 3. Add to request context
  context.Items["tenant_id"] = Guid.Parse(tenantIdClaim);

  await next();
});
```

**Double-Check at Application Layer (Defense-in-Depth):**

```csharp
public class GetQuoteByIdHandler : IRequestHandler<GetQuoteByIdQuery, Result<QuoteResponse>>
{
  public async Task<Result<QuoteResponse>> Handle(GetQuoteByIdQuery req, CancellationToken ct)
  {
    var quote = await _repository.GetByIdAsync(req.QuoteId, ct);

    // Double-check: ensure quote belongs to current tenant
    if (quote?.TenantId != _currentTenantId)
    {
      return Result.Forbidden("Quote not found (tenant mismatch)");
    }

    return Result.Success(new QuoteResponse { ... });
  }
}
```

---

### 3.4 Session Management

**Short-Lived Access Tokens + Refresh Token Pattern:**

```
Access Token:      15 minutes (short-lived, safe in header)
Refresh Token:     30 days (long-lived, stored in HttpOnly cookie)

User logs in:
  → Server issues both tokens
  → Browser stores refresh_token in cookie, access_token in memory

API call:
  → Attach access_token to Authorization header
  → If 401 Unauthorized → POST /auth/refresh with refresh_token
  → Get new access_token
  → Retry request

User logs out:
  → POST /auth/logout (invalidate refresh_token)
  → Clear HttpOnly cookie
  → Clear memory (access_token)

Inactivity:
  → 30-day expiry on refresh_token
  → User automatically logged out
  → Re-login required
```

---

## 4. API ENDPOINTS SPECIFIKÁCIÓ

### 4.1 OpenAPI/Swagger Vázlat (5-10 Sample Endpoints)

**Base URL:** `https://api.joinerytech.hu/api`

#### CRM Endpoints

```yaml
---
openapi: 3.1.0
info:
  title: JoineryTech Backend API
  version: 1.0.0
  description: Production backend for JoineryTech Portal

servers:
  - url: https://api.joinerytech.hu/api

security:
  - bearerAuth: []

components:
  securitySchemes:
    bearerAuth:
      type: http
      scheme: bearer
      bearerFormat: JWT

  schemas:
    ErrorResponse:
      type: object
      properties:
        error:
          type: string
        code:
          type: string
          enum: [INVALID_REQUEST, UNAUTHORIZED, FORBIDDEN, NOT_FOUND, CONFLICT, INTERNAL_ERROR]
        details:
          type: array
          items:
            type: object

    Lead:
      type: object
      properties:
        id:
          type: string
          format: uuid
        name:
          type: string
        email:
          type: string
          format: email
        company:
          type: string
        status:
          type: string
          enum: [new, contacted, qualified, nurturing, converted, rejected]
        source:
          type: string
          enum: [phone, quote, email, exhibition, website, webshop, interior_designer]
        assignedTo:
          type: string
          format: uuid
        probabilityPct:
          type: integer
          minimum: 0
          maximum: 100
        estimatedValue:
          type: number
          format: double
        createdAt:
          type: string
          format: date-time
        updatedAt:
          type: string
          format: date-time

    CreateLeadRequest:
      type: object
      required: [name, email, company, source]
      properties:
        name:
          type: string
          minLength: 1
          maxLength: 200
        email:
          type: string
          format: email
        phone:
          type: string
        company:
          type: string
          maxLength: 200
        source:
          type: string
          enum: [phone, quote, email, exhibition, website, webshop, interior_designer]
        assignedTo:
          type: string
          format: uuid

paths:
  /crm/leads:
    get:
      summary: List all leads
      operationId: getLeads
      tags: [CRM]
      parameters:
        - name: page
          in: query
          schema:
            type: integer
            default: 1
        - name: pageSize
          in: query
          schema:
            type: integer
            default: 20
        - name: status
          in: query
          schema:
            type: string
            enum: [new, contacted, qualified, nurturing, converted, rejected]
        - name: search
          in: query
          schema:
            type: string
            description: Search by name, email, or company
      responses:
        '200':
          description: Paginated list of leads
          content:
            application/json:
              schema:
                type: object
                properties:
                  items:
                    type: array
                    items:
                      $ref: '#/components/schemas/Lead'
                  page:
                    type: integer
                  pageSize:
                    type: integer
                  totalCount:
                    type: integer
        '401':
          description: Unauthorized (missing or invalid token)
        '403':
          description: Forbidden (insufficient permissions)

    post:
      summary: Create a new lead
      operationId: createLead
      tags: [CRM]
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/CreateLeadRequest'
      responses:
        '201':
          description: Lead created
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Lead'
        '400':
          description: Validation error
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ErrorResponse'
        '401':
          description: Unauthorized

  /crm/leads/{leadId}:
    get:
      summary: Get a specific lead
      operationId: getLeadById
      tags: [CRM]
      parameters:
        - name: leadId
          in: path
          required: true
          schema:
            type: string
            format: uuid
      responses:
        '200':
          description: Lead details
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Lead'
        '404':
          description: Lead not found

  /crm/leads/{leadId}/contact:
    put:
      summary: Mark lead as contacted (FSM transition)
      operationId: contactLead
      tags: [CRM]
      parameters:
        - name: leadId
          in: path
          required: true
          schema:
            type: string
            format: uuid
      responses:
        '204':
          description: Lead updated
        '400':
          description: Invalid FSM transition
        '404':
          description: Lead not found

  /crm/leads/{leadId}/convert:
    post:
      summary: Convert lead to opportunity
      operationId: convertLeadToOpportunity
      tags: [CRM]
      parameters:
        - name: leadId
          in: path
          required: true
          schema:
            type: string
            format: uuid
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              required: [estimatedValue, expectedCloseDate]
              properties:
                estimatedValue:
                  type: number
                  format: double
                currency:
                  type: string
                  default: HUF
                expectedCloseDate:
                  type: string
                  format: date
      responses:
        '201':
          description: Opportunity created
          content:
            application/json:
              schema:
                type: object
                properties:
                  opportunityId:
                    type: string
                    format: uuid
        '400':
          description: Validation error

---
# Sales Endpoints

  /sales/quotes:
    post:
      summary: Create a new quote
      operationId: createQuote
      tags: [Sales]
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              required: [customerId, items]
              properties:
                customerId:
                  type: string
                  format: uuid
                opportunityId:
                  type: string
                  format: uuid
                items:
                  type: array
                  items:
                    type: object
                    properties:
                      catalogItemId:
                        type: string
                        format: uuid
                      quantity:
                        type: number
                      unitPrice:
                        type: number
                      discount:
                        type: number
      responses:
        '201':
          description: Quote created
          content:
            application/json:
              schema:
                type: object
                properties:
                  quoteId:
                    type: string
                    format: uuid
                  number:
                    type: string
                  totalValue:
                    type: number

    get:
      summary: List quotes
      operationId: getQuotes
      tags: [Sales]
      parameters:
        - name: page
          in: query
          schema:
            type: integer
        - name: status
          in: query
          schema:
            type: string
            enum: [draft, sent, approved, rejected, converted]
      responses:
        '200':
          description: Paginated quote list

  /sales/quotes/{quoteId}/approve:
    put:
      summary: Approve quote (convert to order)
      operationId: approveQuote
      tags: [Sales]
      parameters:
        - name: quoteId
          in: path
          required: true
          schema:
            type: string
            format: uuid
      responses:
        '200':
          description: Quote approved, order created
          content:
            application/json:
              schema:
                type: object
                properties:
                  orderId:
                    type: string
                    format: uuid
        '400':
          description: Quote cannot be approved (wrong status)
        '404':
          description: Quote not found

---
# Production Endpoints

  /production/orders/{orderId}/release:
    post:
      summary: Release order to production
      operationId: releaseOrder
      tags: [Production]
      parameters:
        - name: orderId
          in: path
          required: true
          schema:
            type: string
            format: uuid
      responses:
        '204':
          description: Order released
        '400':
          description: Order cannot be released (wrong status or calc incomplete)

  /production/schedule:
    get:
      summary: Get weekly production schedule
      operationId: getWeeklySchedule
      tags: [Production]
      parameters:
        - name: weekOffset
          in: query
          schema:
            type: integer
            default: 0
            description: Week offset from today (0=this week, 1=next week)
      responses:
        '200':
          description: Weekly schedule
          content:
            application/json:
              schema:
                type: object
                properties:
                  week:
                    type: string
                    format: date
                  jobs:
                    type: array
                    items:
                      type: object
                      properties:
                        jobId:
                          type: string
                          format: uuid
                        orderId:
                          type: string
                          format: uuid
                        jobType:
                          type: string
                        plannedStart:
                          type: string
                          format: date-time
                        plannedEnd:
                          type: string
                          format: date-time
                        assignedTo:
                          type: string
                          format: uuid
                        duration:
                          type: integer
                          description: Minutes

---
# Warehouse Endpoints

  /warehouse/inventory:
    get:
      summary: Get current inventory levels
      operationId: getInventoryLevels
      tags: [Warehouse]
      parameters:
        - name: location
          in: query
          schema:
            type: string
        - name: search
          in: query
          schema:
            type: string
            description: Search by item name/SKU
      responses:
        '200':
          description: Inventory levels
          content:
            application/json:
              schema:
                type: array
                items:
                  type: object
                  properties:
                    materialId:
                      type: string
                      format: uuid
                    sku:
                      type: string
                    name:
                      type: string
                    quantityOnHand:
                      type: number
                    quantityReserved:
                      type: number
                    quantityAvailable:
                      type: number
                    reorderPoint:
                      type: number
                    lastMovement:
                      type: string
                      format: date-time

  /warehouse/movements:
    post:
      summary: Create inventory movement
      operationId: createInventoryMovement
      tags: [Warehouse]
      requestBody:
        required: true
        content:
          application/json:
            schema:
              type: object
              required: [materialId, movementType, quantity]
              properties:
                materialId:
                  type: string
                  format: uuid
                movementType:
                  type: string
                  enum: [purchase, sales, production, adjustment, return]
                quantity:
                  type: number
                referenceType:
                  type: string
                  enum: [order, production_job, po, manual]
                referenceId:
                  type: string
                  format: uuid
                notes:
                  type: string
      responses:
        '201':
          description: Movement created
        '400':
          description: Validation error

---
```

**HTTP Status Codes:**
- `200 OK` — Successful read/update
- `201 Created` — Resource created
- `204 No Content` — Successful deletion/state change
- `400 Bad Request` — Validation error
- `401 Unauthorized` — Missing/invalid token
- `403 Forbidden` — Insufficient permissions
- `404 Not Found` — Resource not found
- `409 Conflict` — FSM violation or duplicate resource
- `422 Unprocessable Entity` — Business rule violation
- `500 Internal Server Error` — Unexpected error

---

### 4.2 Request/Response Models

**Standard Envelope (Optional but Recommended):**

```json
// Command Response (Success)
{
  "success": true,
  "data": { ... payload ... },
  "timestamp": "2026-07-02T14:30:00Z"
}

// Command Response (Error)
{
  "success": false,
  "error": {
    "code": "QUOTE_INVALID_STATUS",
    "message": "Cannot approve quote with status 'rejected'",
    "details": [
      { "field": "status", "message": "Invalid transition from rejected → approved" }
    ]
  },
  "timestamp": "2026-07-02T14:30:00Z"
}

// Query Response (List)
{
  "data": [
    { "id": "...", "name": "...", ... },
    { "id": "...", "name": "...", ... }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 150,
    "totalPages": 8
  },
  "timestamp": "2026-07-02T14:30:00Z"
}
```

---

### 4.3 Error Handling

**Error Response Structure:**

```json
{
  "success": false,
  "error": {
    "code": "VALIDATION_FAILED",      // Machine-readable
    "message": "Quote validation failed",  // User-readable
    "details": [                      // Field-level errors
      {
        "field": "items[0].quantity",
        "message": "Quantity must be > 0",
        "code": "MIN_VALUE"
      },
      {
        "field": "totalValue",
        "message": "Cannot be less than minimum order value (50000 HUF)",
        "code": "MIN_ORDER_VALUE"
      }
    ]
  }
}
```

**Specific Error Codes:**
- `VALIDATION_FAILED` — Input validation error
- `QUOTE_INVALID_STATUS` — FSM violation
- `LEAD_NOT_FOUND` — Resource not found
- `UNAUTHORIZED` — No token
- `FORBIDDEN` — Missing permission
- `TENANT_MISMATCH` — Accessing another tenant's data
- `DUPLICATE_SKU` — Catalog item SKU already exists
- `INSUFFICIENT_STOCK` — Cannot reserve materials
- `SCHEDULE_CONFLICT` — Resource already booked
- `INTERNAL_ERROR` — Unexpected server error

---

## 5. localStorage → Backend MIGRÁCIÓ ROADMAP

### 5.1 Phased Migration Plan

**Goal:** Complete transition from localStorage to PostgreSQL backend in 4 weeks (July 2–30, 2026).

#### Phase 0 (Week 0 — Now): Infrastructure Setup

**Duration:** 3 days (2026-07-02 to 07-04)

**Deliverables:**
- PostgreSQL 15+ instance (Azure Database for PostgreSQL or self-hosted)
- EF Core DbContext + migrations scripted
- API skeleton (ASP.NET Core + MediatR + Minimal API)
- CI/CD pipeline (GitHub Actions → Docker → staging)
- Keycloak instance (or Identity module) for JWT auth

**Tasks:**
- [ ] Provision PostgreSQL (staging + prod)
- [ ] Create EF Core DbContext with all 40+ entities
- [ ] Generate database schema from code-first migrations
- [ ] Set up RLS policies on all tables
- [ ] Build Minimal API scaffold for CRM, Sales, Warehouse, Production
- [ ] Configure GitHub Actions CI/CD
- [ ] Deploy to staging
- [ ] Test JWT auth flow end-to-end

**Success Criteria:**
- ✅ `dotnet test` all pass
- ✅ Swagger UI accessible on staging
- ✅ `POST /auth/login` returns valid JWT
- ✅ `GET /api/crm/leads` returns 401 without token

---

#### Phase 1 (Week 1 — Jul 8–12): Parallel Read-only

**Duration:** 1 week

**Strategy:** Backend reads from DB, frontend **still writes to localStorage**.

**Deliverables:**
- CRM read endpoints (GET /api/crm/leads, GET /api/crm/opportunities)
- Sales read endpoints (GET /api/sales/quotes, GET /api/sales/orders)
- Warehouse read endpoints (GET /api/warehouse/inventory)
- Production read endpoints (GET /api/production/schedule)
- **Data import job:** Export current localStorage state (CSV), import to PostgreSQL

**Implementation:**
1. Export current `window.sim` state to CSV files (leads, opportunities, quotes, orders, etc.)
2. Run import script: `npm run import-data /path/to/export/`
3. Frontend: Update to fetch customer list, catalog, employees from API (read-only)
   ```javascript
   // Before: const customers = sim.customers;
   // After: const customers = await fetch('/api/sales/customers').then(r => r.json());
   ```
4. Validation UI: Show data from both localStorage and API side-by-side (should match)

**API Endpoints in Scope:**
- GET /api/crm/leads
- GET /api/crm/leads/{id}
- GET /api/crm/opportunities
- GET /api/sales/quotes
- GET /api/sales/orders
- GET /api/warehouse/inventory
- GET /api/production/schedule

**Rollback Plan:** If API is unreachable, frontend continues with cached localStorage (no user impact)

**Success Criteria:**
- ✅ Data import complete (all 40+ entities in PostgreSQL)
- ✅ Frontend reads customer list from API successfully
- ✅ Page load time <3s (with caching)
- ✅ Validation: UI shows matching data from both sources
- ✅ Zero data loss

---

#### Phase 2 (Week 2 — Jul 15–19): Selective Module Write-over

**Duration:** 1 week

**Strategy:** Specific modules transition to API writes; others remain on localStorage.

**Modules to Migrate (Priority Order):**

1. **CRM (Leads & Opportunities)** ✅ SAFEST
   - POST /api/crm/leads
   - PUT /api/crm/leads/{id}/contact
   - PUT /api/crm/leads/{id}/qualify
   - PUT /api/crm/leads/{id}/convert
   - POST /api/crm/opportunities
   - PUT /api/crm/opportunities/{id}/update-status

2. **Warehouse (Inventory)** ✅ CRITICAL for Production
   - POST /api/warehouse/movements (receive goods, consume for production)

3. **Sales (Quotes)** ⚠️ MODERATE RISK (complex FSM)
   - POST /api/sales/quotes
   - PUT /api/sales/quotes/{id}/approve
   - PUT /api/sales/quotes/{id}/reject

4. **Production Orders** ⚠️ HOLD for Week 3 (depends on warehouse working)

**Implementation:**
- Frontend: Dispatch CRM/Quote mutations to API (POST/PUT)
- Error handling: If API fails → fall back to localStorage + queue for retry
- Sync service: Background job retries failed API calls every 5 seconds

```javascript
// Frontend mutation (CRM example)
async function createLead(data) {
  try {
    const response = await fetch('/api/crm/leads', {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${token}` },
      body: JSON.stringify(data)
    });
    if (response.ok) {
      return response.json();  // Lead created in DB
    } else {
      // Fallback: create in localStorage, queue for retry
      sim.addLeadLocal(data);
      syncQueue.push({ type: 'crm.create_lead', data, retries: 0 });
      return { id: uuidv4(), ...data, _pending: true };  // Flag as pending
    }
  } catch (error) {
    sim.addLeadLocal(data);
    syncQueue.push({ type: 'crm.create_lead', data, retries: 0 });
  }
}
```

**Concurrent Features:**
- Notifications: When lead status changes, CRM module publishes event → Notifications service sends email
- Cross-module: Quote approval → creates Order (Sales module) → reserves inventory (Warehouse)

**Rollback Plan:**
- If CRM write fails: Keep in localStorage, auto-retry every 10s
- If Warehouse write fails: Production orders stay in draft (warn user)
- Manual rollback: Export API data → reimport to localStorage (snapshot restore)

**Success Criteria:**
- ✅ All CRM mutations working (create, contact, qualify, convert)
- ✅ Quote create/approve/reject working
- ✅ Inventory movements working
- ✅ Background sync successfully retries failed calls
- ✅ Zero data loss on API failure
- ✅ Notifications sent on state changes

---

#### Phase 3 (Week 3 — Jul 22–26): Full Production Cutover

**Duration:** 1 week

**Strategy:** All modules transition to API; localStorage = offline cache only.

**Modules to Migrate:**
- Production orders + jobs (POST /api/production/orders/{id}/release)
- HR (employees, attendance)
- EHS (incidents, CAPA, risks)
- QA (inspections, NCR)
- DMS (documents)
- Catalog (items, approval)
- Controlling (cost tracking, forecasts)

**Offline Sync Strategy:**
- Service Worker: Cache GET responses
- IndexedDB: Store pending mutations (CREATE/UPDATE/DELETE)
- Sync API: `POST /api/sync` batches all pending operations when online
  ```javascript
  const pendingOps = [
    { op: 'POST', path: '/api/crm/leads', data: {...} },
    { op: 'PUT', path: '/api/crm/leads/id/contact', data: {...} },
    { op: 'POST', path: '/api/warehouse/movements', data: {...} }
  ];
  fetch('/api/sync', { method: 'POST', body: JSON.stringify(pendingOps) });
  ```

**Zero-downtime Cutover:**
1. Deploy API to production (staging validation passed)
2. Frontend detects API health (ping /api/health)
3. If healthy: Use API, sync offline queue
4. If unhealthy: Use localStorage fallback (transparent to user)
5. Monitor error rates (if >5% for 5 min, auto-rollback)

**Parallel Run (48 hours):**
- Both localStorage and API active simultaneously
- Every API write also updates localStorage (fallback)
- Audit log: Compare final state after 48h cutover
- If discrepancies: Debug and resolve before permanent cutover

**Permanent Cutover:**
- Disable localStorage writes (read-only cache)
- Mark localStorage as deprecated in console warning
- Archive localStorage snapshot

**Rollback Plan (if needed):**
- API data snapshot exported to localStorage format
- Frontend reverts to localStorage-only mode
- Manual data reconciliation post-mortem

**Success Criteria:**
- ✅ All modules successfully migrated
- ✅ Zero data loss during cutover
- ✅ <2% error rate on API calls
- ✅ Page load time <3s
- ✅ Offline mode works (sync when online)
- ✅ All tests pass (unit + integration + e2e)

---

#### Phase 4 (Week 4 — Jul 29–Aug 02): Optimization & Decommission

**Duration:** Final week

**Tasks:**
- [ ] Performance tuning: Add indexes for slow queries
- [ ] Caching: Redis for frequently accessed data (catalog, employee list)
- [ ] Analytics: Enable query logging (slow query log, >100ms)
- [ ] Monitoring: Set up APM (Application Performance Monitoring)
- [ ] Documentation: API docs, deployment runbook, troubleshooting guide
- [ ] Decommission: Remove localStorage code (no longer needed)
- [ ] Training: Team knows how to deploy, scale, troubleshoot backend

**Deliverables:**
- Production API v1.0
- API documentation (OpenAPI + Postman collection)
- Deployment runbook
- On-call playbook (what to do if API is down)

**Success Criteria:**
- ✅ API meets SLA (99.5% uptime)
- ✅ No localStorage code in frontend
- ✅ Team trained on backend operations
- ✅ Ready for next feature development

---

### 5.2 Risk Analysis

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|-----------|
| **Data loss during import** | Low | Critical | Validate data consistency post-import; keep localStorage backup |
| **API downtime blocks users** | Medium | High | Offline fallback to localStorage; sync when online |
| **Performance regression (slow queries)** | Medium | High | Load testing before cutover; add indexes preemptively |
| **Auth token expiry during long session** | Low | Medium | Refresh token rotation; re-login prompt |
| **Concurrent writes (optimistic lock conflict)** | Medium | Low | Add version column; use EF Core concurrency tokens |
| **Cross-tenant data leak** | Low | Critical | Double-check RLS policies + application-layer validation |
| **Breaking API changes (v1→v2)** | Low | Medium | Versioning strategy (`/api/v1/`, `/api/v2/`); deprecation warnings |

**Contingency Plans:**
1. **If import fails:** Restart from clean DB; re-export and re-import
2. **If API is down:** Users fall back to localStorage (cached); sync queued
3. **If performance poor:** Disable expensive features (e.g., analytics) temporarily; scale database
4. **If data loss:** Restore from PostgreSQL snapshot (nightly backups)

---

### 5.3 Rollback Plan

**Automatic Rollback Triggers:**
- API response time > 5s (P95)
- Error rate > 5% for 5 minutes
- Database connection failures
- Authentication service unavailable

**Manual Rollback Steps:**
```bash
# 1. Stop API service
systemctl stop joinerytech-api

# 2. Revert frontend to localStorage-only mode
git revert <commit-hash>
npm run build
npm run deploy

# 3. Restore database from backup (if data corruption)
pg_restore --clean --no-owner --verbose -d joinerytech /path/to/backup.dump

# 4. Validate data integrity
npm run validate:db

# 5. Notify users (status page)
curl -X POST https://status.joinerytech.hu/api/incidents \
  -d '{"title":"API Migration Rollback","severity":"critical"}'
```

**Post-Mortem:**
- Analyze logs (API, DB, network)
- Identify root cause
- Update tests to prevent regression
- Re-plan migration with fixes

---

## Summary: Technology Decisions & Rationale

| Decision | Choice | Rationale |
|----------|--------|-----------|
| **Backend Framework** | .NET 8 + ASP.NET Core | DDD + CQRS native; team expertise; proven on SpaceOS |
| **API Style** | REST + OpenAPI | Standard; easy mobile integration; GraphQL optional later |
| **Database** | PostgreSQL 15+ | ACID; RLS; FSM enforcement; team proven; analytics support |
| **Authentication** | JWT ES256 + Keycloak | Stateless; OAuth bridge for B2B partners; OIDC standard |
| **Architecture** | Modular Monolith | Atomic transactions; compile-time safety; easy deployment |
| **Real-time** | SSE (Phase 1) + WebSocket (Phase 2) | Progressive enhancement; no over-engineering |
| **Migration** | 4-week phased approach | Parallel run first; gradual module-by-module cutover; offline fallback |

---

## Next Steps (Handoff to MSG-BACKEND-106 & MSG-BACKEND-107)

1. **MSG-BACKEND-106:** Architect reviews this Backend Architecture Plan → approves OpenAPI spec feasibility for .NET 8
2. **MSG-BACKEND-107:** Backend creates .NET 8 module skeleton (Kernel + Catalog) based on approved architecture
3. **Week 1 (Jul 8–12):** Phase 1 parallel run begins (read-only API)
4. **Week 2 (Jul 15–19):** Phase 2 CRM + Warehouse write-over
5. **Week 3 (Jul 22–26):** Phase 3 full cutover
6. **Week 4 (Jul 29–Aug 02):** Optimization + training

---

**Status:** Backend Architecture Plan COMPLETE
**Created:** 2026-07-02
**Terminal:** backend
**Model:** Sonnet
