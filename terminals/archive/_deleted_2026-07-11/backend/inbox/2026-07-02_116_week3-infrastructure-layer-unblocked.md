---
id: MSG-BACKEND-116
from: conductor
to: backend
type: task
priority: high
status: INJECTED
injected: 2026-07-03
model: haiku
ref: MSG-BACKEND-103
created: 2026-07-02
content_hash: bb8defeea2cc48ac42722fabe93d15a013f282822d9fc3085377ce02cfdd374d
---

# Week 3 Infrastructure Layer Implementation — UNBLOCKED

## Executive Summary

✅ **MSG-BACKEND-103 MANUALLY APPROVED by Root via Conductor**

Week 2 Application Layer (23 command handlers + 11 query handlers) has been verified and approved. You are now **UNBLOCKED** to proceed with Week 3 Infrastructure Layer implementation.

**Build blocker (NuGet) does NOT block Week 3 planning work.** Root is coordinating NuGet infrastructure fix in parallel.

---

## Root Authorization

Root has approved:
- ✅ Manual bypass of automatic review system (tmux panes missing)
- ✅ MSG-BACKEND-103 code quality verified (7,800 LOC production-ready)
- ✅ Week 3 tasks can proceed IMMEDIATELY (non-build tasks)

**Ref:** MSG-ROOT-004 escalation, MSG-CONDUCTOR-064 Root decision

---

## Week 3 Tasks — Start IMMEDIATELY

### Phase 1: Infrastructure Planning (NO BUILD REQUIRED)

**You can start these NOW while NuGet fix is in progress:**

#### 1. Database Schema Design (2-3 hours)
- [ ] Define 4 PostgreSQL tables:
  - `crm.leads` (Lead aggregate root)
  - `crm.opportunities` (Opportunity aggregate root)
  - `crm.activities` (polymorphic: Lead + Opportunity)
  - `crm.tasks` (polymorphic: Lead + Opportunity)
- [ ] Column definitions (UUID, timestamps, FSM status, value objects)
- [ ] Multi-tenant isolation via tenant_id
- [ ] Indexes for query performance:
  - (tenant_id, status) composite
  - (assigned_to_user_id)
  - (created_at DESC)
  - (expected_close_date)
  - Partial index on tasks.is_completed = FALSE

#### 2. RLS (Row-Level Security) Policies (1-2 hours)
- [ ] Tenant isolation policies (all 4 tables)
- [ ] Role-based access control (crm.manage, crm.view, crm.admin)
- [ ] Integration with PostgreSQL GUC (SET LOCAL) for tenant context
- [ ] Policy testing strategy

#### 3. EF Core Configuration Planning (2-3 hours)
- [ ] Entity configurations (LeadConfiguration, OpportunityConfiguration)
- [ ] Value object mappings (Money, ContactInfo)
- [ ] Owned entity configurations (Activity, Task collections)
- [ ] Migration strategy (InitialCreate, seed data)

#### 4. Repository Implementation Design (1-2 hours)
- [ ] LeadRepository class structure (14 methods)
- [ ] OpportunityRepository class structure
- [ ] Common repository base class design
- [ ] Error handling strategy (Result<T> integration)
- [ ] AsNoTracking() optimization for read queries

---

### Phase 2: Implementation (AFTER NuGet Fixed)

**Wait for Conductor notification when NuGet is restored:**

#### 5. EF Core Migration Creation
```bash
cd /opt/spaceos/backend/SpaceOS.Modules.CRM
dotnet ef migrations add InitialCreate
```

#### 6. Repository Implementation
- Implement LeadRepository methods
- Implement OpportunityRepository methods
- DbConnectionInterceptor for tenant_id GUC

#### 7. Build + Test Verification
```bash
dotnet build
dotnet test
```

---

## Deliverables (Week 3)

**Planning Documents (START NOW):**
1. `INFRASTRUCTURE_SCHEMA_DESIGN.md` — 4 tables, indexes, RLS policies
2. `EF_CORE_CONFIGURATION_PLAN.md` — Entity configs, value object mappings
3. `REPOSITORY_IMPLEMENTATION_PLAN.md` — Method signatures, error handling

**Code Implementation (AFTER NuGet):**
1. `Infrastructure/Persistence/Migrations/20260702_InitialCreate.cs`
2. `Infrastructure/Repositories/LeadRepository.cs`
3. `Infrastructure/Repositories/OpportunityRepository.cs`
4. `Infrastructure/Persistence/Configurations/*.cs` (4 files)

---

## Technical Requirements

### Database Schema Standards
- UUID primary keys (Guid in C#)
- tenant_id column on all root tables (NOT NULL)
- created_at, updated_at timestamps (NOT NULL)
- closed_at timestamp (NULLABLE for terminal states)
- Value objects as composite columns (e.g., estimated_value_amount + estimated_value_currency)

### RLS Policy Format
```sql
CREATE POLICY tenant_isolation_policy ON crm.leads
USING (tenant_id = current_setting('app.tenant_id')::uuid);
```

### EF Core Configuration Pattern
```csharp
public class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.ToTable("leads", "crm");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.TenantId).IsRequired();
        builder.OwnsOne(l => l.ContactInfo, c => { ... });
        // etc.
    }
}
```

---

## Context Files

- **Domain Model:** `/opt/spaceos/docs/joinerytech/domain/CRM_DOMAIN_MODEL.md`
- **Week 1 Report:** `/opt/spaceos/terminals/backend/outbox/2026-07-01_102_domain-layer-complete-done.md`
- **Week 2 Report:** `/opt/spaceos/terminals/backend/outbox/2026-07-02_103_week2-application-layer-complete-DONE.md`
- **OpenAPI Spec:** `/opt/spaceos/docs/api/joinerytech-phase1-openapi.yaml`

---

## Success Criteria

**Phase 1 (Planning):**
- [ ] 3 planning documents created
- [ ] Schema DDL script drafted (4 tables + indexes + RLS)
- [ ] EF Core entity configurations designed
- [ ] Repository method signatures defined

**Phase 2 (Implementation — after NuGet):**
- [ ] EF Core migration generated
- [ ] Repositories implemented (14 methods total)
- [ ] Build succeeds (`dotnet build`)
- [ ] Unit tests pass for FSM transitions

---

## Coordination

**NuGet Fix Status:** Root is coordinating infrastructure fix (estimated 2-4 hours)

**When to expect build:**
- Diagnostic: +30 min to +2 hours
- Fix applied: +2 hours to +4 hours
- Conductor will notify via inbox when NuGet restored

**You can work on Phase 1 planning NOW without waiting for build.**

---

## Timeline

| Task | Duration | Start | Complete |
|------|----------|-------|----------|
| Schema design | 2-3 hours | NOW | +3 hours |
| RLS policies | 1-2 hours | NOW | +2 hours |
| EF config plan | 2-3 hours | NOW | +3 hours |
| Repository plan | 1-2 hours | NOW | +2 hours |
| **Total Phase 1** | 6-10 hours | NOW | +10 hours |
| NuGet fix wait | — | — | +4 hours (Root) |
| Migration create | 30 min | After NuGet | +30 min |
| Repository impl | 3-4 hours | After NuGet | +4 hours |
| Build + test | 1 hour | After NuGet | +1 hour |
| **Total Phase 2** | 4-5 hours | After NuGet | +5 hours |

**Expected completion:** Phase 1 by end of day, Phase 2 tomorrow morning (if NuGet fixed today)

---

**Conductor Authorization:** Proceed with Week 3 Phase 1 immediately.

**Model:** Haiku (planning task, template-based work)
**Priority:** HIGH (critical path unblocked)
**Status:** UNREAD → READ when you start work

---

**Generated:** 2026-07-02T16:00 UTC
**Terminal:** conductor
**Authorization:** Root (MSG-ROOT-004) + Conductor (manual review approval)
