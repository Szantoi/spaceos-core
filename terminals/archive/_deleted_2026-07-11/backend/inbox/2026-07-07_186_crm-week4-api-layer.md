---
id: MSG-BACKEND-186
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-BACKEND-183
created: 2026-07-07
epic_id: EPIC-CUTTING-Q3
checkpoint_id: CP-CUTTING-WEEK4-API
estimated_nwt: 40
content_hash: f44b47c4476b45e335f0d613e3f2a32ba806eba67b6177e3f02b088a42afb950
completed: 2026-07-08
---

# CRM Week 4 API Layer Implementation

**Epic:** EPIC-CUTTING-Q3 (JoineryTech Phase 1-4 Full Stack)
**Checkpoint:** CP-CUTTING-WEEK4-API
**Module:** CRM (Customer Relationship Management)
**Phase:** Week 4 — API Layer

---

## 🎯 Objective

Implement **Minimal API endpoints** for the CRM module with full CQRS/MediatR pattern, covering:
- Lead CRUD operations (+ owned collections: Activities, Tasks)
- Opportunity CRUD operations (+ owned collections: Activities, Tasks)
- Lead → Opportunity conversion (business workflow)
- Opportunity close (Won/Lost)
- Request/Response DTOs
- FluentValidation rules
- API integration tests with Testcontainers + authentication

**Expected Time:** 40 NWT (~80 minutes)

**Pattern Reuse:** DMS + HR + Maintenance + QA Week 4 API patterns

**Reference ADR:** ADR-054 (CRM Domain Model — Lead + Opportunity, NO Customer)

---

## 📦 Deliverables

### 1. **Application Layer** — CQRS Commands & Queries

**Commands (Write operations):**
```
Application/Commands/
├── CreateLeadCommand.cs
├── UpdateLeadCommand.cs
├── AddLeadActivityCommand.cs          # Owned collection: Activities
├── AddLeadTaskCommand.cs              # Owned collection: Tasks
├── ConvertLeadToOpportunityCommand.cs # Business workflow
├── CreateOpportunityCommand.cs
├── UpdateOpportunityCommand.cs
├── AddOpportunityActivityCommand.cs   # Owned collection
├── AddOpportunityTaskCommand.cs       # Owned collection
└── CloseOpportunityCommand.cs         # Won/Lost
```

**Queries (Read operations):**
```
Application/Queries/
├── GetLeadQuery.cs
├── ListLeadsQuery.cs
├── GetLeadActivitiesQuery.cs          # Owned collection query
├── GetLeadTasksQuery.cs               # Owned collection query
├── GetOpportunityQuery.cs
├── ListOpportunitiesQuery.cs
├── GetOpportunityActivitiesQuery.cs
└── GetOpportunityTasksQuery.cs
```

**Handlers:** 18 total (10 command + 8 query handlers)

**DTOs:**
```
Application/DTOs/
├── LeadDto.cs                         # Includes ContactInfo, Activities[], Tasks[]
├── LeadListDto.cs
├── LeadActivityDto.cs                 # From owned collection
├── LeadTaskDto.cs
├── OpportunityDto.cs                  # Includes ContactInfo, Money (EstimatedValue), Activities[], Tasks[]
├── OpportunityListDto.cs
├── OpportunityActivityDto.cs
└── OpportunityTaskDto.cs
```

**Validators:**
```
Application/Validators/
├── CreateLeadCommandValidator.cs
├── UpdateLeadCommandValidator.cs
├── AddLeadActivityCommandValidator.cs
├── ConvertLeadToOpportunityCommandValidator.cs
├── CreateOpportunityCommandValidator.cs
├── UpdateOpportunityCommandValidator.cs
└── CloseOpportunityCommandValidator.cs
```

---

### 2. **API Layer** — Minimal API Endpoints

**File:** `Api/Endpoints/CRMEndpoints.cs`

**Endpoint Groups:**

**Lead Endpoints:**
```csharp
// Lead Management
POST   /api/crm/leads                  → CreateLeadCommand
GET    /api/crm/leads                  → ListLeadsQuery (with filtering)
GET    /api/crm/leads/{id}             → GetLeadQuery
PUT    /api/crm/leads/{id}             → UpdateLeadCommand
POST   /api/crm/leads/{id}/activities  → AddLeadActivityCommand
POST   /api/crm/leads/{id}/tasks       → AddLeadTaskCommand
POST   /api/crm/leads/{id}/convert     → ConvertLeadToOpportunityCommand
```

**Opportunity Endpoints:**
```csharp
// Opportunity Management
POST   /api/crm/opportunities                  → CreateOpportunityCommand
GET    /api/crm/opportunities                  → ListOpportunitiesQuery
GET    /api/crm/opportunities/{id}             → GetOpportunityQuery
PUT    /api/crm/opportunities/{id}             → UpdateOpportunityCommand
POST   /api/crm/opportunities/{id}/activities  → AddOpportunityActivityCommand
POST   /api/crm/opportunities/{id}/tasks       → AddOpportunityTaskCommand
POST   /api/crm/opportunities/{id}/close       → CloseOpportunityCommand (Won/Lost)
```

**Total:** 14 endpoints

**Authentication:** JWT Bearer (pattern from Identity module)

**Multi-Tenancy:** RLS via `TenantDbConnectionInterceptor` (tenant_id from JWT claims)

---

### 3. **Integration Tests** — Testcontainers

**File:** `tests/Integration/Api/CRMEndpointsTests.cs`

**Test Scenarios (Minimum 7):**
1. ✅ CreateLead_ReturnsCreatedLead
2. ✅ ListLeads_WithFiltering_ReturnsFilteredResults
3. ✅ AddLeadActivity_AddsToOwnedCollection
4. ✅ ConvertLeadToOpportunity_CreatesOpportunityAndMarksLeadConverted
5. ✅ CreateOpportunity_ReturnsCreatedOpportunity
6. ✅ CloseOpportunity_Won_UpdatesStatus
7. ✅ CloseOpportunity_Lost_UpdatesStatus

**Test Infrastructure:**
- Testcontainers PostgreSQL 16 Alpine
- JWT authentication setup
- Multi-tenant test data
- IAsyncLifetime per test class

---

## ✅ Acceptance Criteria

### Build & Compile
- ✅ Build SUCCESS (0 errors)
- ✅ Warnings ≤ 3 (nullable reference warnings acceptable)

### API Endpoints
- ✅ 14 endpoints implemented (Lead: 7, Opportunity: 7)
- ✅ All endpoints return correct HTTP status codes (200, 201, 400, 404)
- ✅ Request/Response DTOs validated with FluentValidation

### Integration Tests
- ✅ Minimum 7 test scenarios PASS
- ✅ Testcontainers PostgreSQL setup working
- ✅ JWT authentication tested
- ✅ Multi-tenancy isolation tested

### Pattern Compliance
- ✅ CQRS/MediatR pattern (command/query separation)
- ✅ Minimal API pattern (endpoint registration)
- ✅ Repository pattern (via Application Layer)
- ✅ Multi-tenancy (RLS via interceptor)
- ✅ Owned collections (Activities, Tasks separate tables)

### ADR-054 Compliance
- ✅ Lead + Opportunity aggregates ONLY (NO Customer in CRM module)
- ✅ ContactInfo value object (Email, Phone)
- ✅ Money value object (EstimatedValue)
- ✅ Activities + Tasks owned collections

---

## 📚 Reference Documentation

**ADR-054:** CRM Domain Model (Lead, Opportunity, NO Customer)
- Lead aggregate: Source, Status, ContactInfo, Activities[], Tasks[]
- Opportunity aggregate: Lead reference, EstimatedValue (Money), ExpectedCloseDate, Stage, ContactInfo, Activities[], Tasks[]
- Customer is SEPARATE module (not part of CRM Week 1-4)

**Pattern Reuse:**
- DMS Week 4 API: Minimal API pattern, authentication
- HR Week 4 API: Owned collection endpoints (Absences)
- Maintenance Week 4 API: Multi-entity CRUD pattern
- QA Week 4 API: FSM state transitions (InProgress → Completed)

**Owned Collection Pattern:**
- Activities: Separate table `lead_activities`, `opportunity_activities`
- Tasks: Separate table `lead_tasks`, `opportunity_tasks`
- No separate repository — loaded via `.Include()`

---

## 🔗 Dependencies

**Week 1 (Domain Layer):** ✅ DONE (MSG-BACKEND-141)
**Week 2 (Application Layer):** ✅ DONE (MSG-BACKEND-143)
**Week 3 (Infrastructure Layer):** 🟡 PARTIAL (MSG-BACKEND-183 — core complete, migrations + tests deferred)

**Can proceed:** Yes (API Layer depends on Application Layer, not full Infrastructure)

---

## 📊 Effort Estimation

| Component | NWT | Notes |
|-----------|-----|-------|
| Commands + Handlers | 12 | 10 commands × 1.2 NWT average |
| Queries + Handlers | 8 | 8 queries × 1 NWT average |
| DTOs + Validators | 8 | 7 DTOs + 7 validators |
| API Endpoints | 7 | 14 endpoints × 0.5 NWT (pattern reuse) |
| Integration Tests | 5 | 7 scenarios × 0.7 NWT |
| **TOTAL** | **40 NWT** | **~80 minutes** |

**Acceleration:** 60 NWT → 40 NWT (33% faster via pattern reuse)

---

## 🎯 Strategic Context

**CRM Week 4 API = 5th of 6 modules** in JoineryTech Phase 1-4 Full Stack.

**Epic Progress After Completion:**
- Week 1: 6/6 DONE (100%)
- Week 2: 6/6 DONE (100%)
- Week 3: 3/6 DONE + 3/6 PARTIAL (~75%)
- Week 4: 5/6 DONE (CRM + DMS, HR, Maintenance, QA)

**Expected Epic Progress:** ~85% (only Kontrolling Week 4 API remaining)

---

**Priority:** High (Week 4 API completion critical for epic milestone)
**Model:** Sonnet (CQRS + API pattern implementation)

🤖 Generated by Conductor

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
