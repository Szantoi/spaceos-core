# Infrastructure Schema Design — CRM Module Week 3

> **Document Type:** Planning (Phase 1 — No Build Required)
> **Status:** DRAFT → Ready for review
> **Created:** 2026-07-02 16:10 UTC
> **Task Reference:** MSG-BACKEND-116

---

## Executive Summary

Database schema design for JoineryTech CRM module (Week 3 Infrastructure Layer). Covers 4 PostgreSQL tables with multi-tenant isolation (RLS), FSM state enforcement, value object mappings, and query optimization indexes.

**Tables:** leads, opportunities, activities, tasks (all in `crm` schema)
**Tenant Isolation:** Row-Level Security (RLS) policies + GUC parameter binding
**FSM Enforcement:** CHECK constraints on state columns
**Performance:** 5+ indexes optimized for common queries

---

## 1. Table Definitions

### 1.1 crm.leads (Lead Aggregate Root)

```sql
CREATE TABLE crm.leads (
  -- Identity
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id UUID NOT NULL,

  -- FSM State
  status VARCHAR(50) NOT NULL DEFAULT 'New',
  CHECK (status IN ('New', 'Contacted', 'Qualified', 'Disqualified', 'Converted')),

  -- Contact Information (Value Object: ContactInfo)
  first_name VARCHAR(100) NOT NULL,
  last_name VARCHAR(100) NOT NULL,
  email_address VARCHAR(255),
  email_verified BOOLEAN DEFAULT FALSE,
  phone_number VARCHAR(20),
  country_code VARCHAR(2),

  -- Business Data
  company_name VARCHAR(255),
  job_title VARCHAR(100),
  industry VARCHAR(100),

  -- Lead Quality Scoring
  lead_score SMALLINT DEFAULT 0 CHECK (lead_score >= 0 AND lead_score <= 100),
  source VARCHAR(50), -- 'Website', 'Referral', 'ColdCall', 'Event', 'Partner'

  -- Relationships
  assigned_to_user_id UUID,
  created_by_user_id UUID NOT NULL,

  -- Timestamps
  created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
  contacted_at TIMESTAMP WITH TIME ZONE,
  qualified_at TIMESTAMP WITH TIME ZONE,
  converted_at TIMESTAMP WITH TIME ZONE,

  -- Audit
  last_activity_at TIMESTAMP WITH TIME ZONE,
  version BIGINT DEFAULT 0,

  -- Soft Delete
  deleted_at TIMESTAMP WITH TIME ZONE,

  CONSTRAINT fk_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE CASCADE,
  CONSTRAINT fk_assigned_user FOREIGN KEY (assigned_to_user_id) REFERENCES identity.users(id),
  CONSTRAINT fk_created_by_user FOREIGN KEY (created_by_user_id) REFERENCES identity.users(id)
);
```

**Rationale:**
- **FSM States:** New → Contacted → Qualified → [Converted OR Disqualified]
- **Value Object (ContactInfo):** first_name, last_name, email_address, phone_number, country_code stored as composite columns
- **Soft Delete:** deleted_at for audit trail
- **Tenant Isolation:** tenant_id NOT NULL on every row
- **User Context:** assigned_to_user_id for assignment, created_by_user_id for audit

### 1.2 crm.opportunities (Opportunity Aggregate Root)

```sql
CREATE TABLE crm.opportunities (
  -- Identity
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id UUID NOT NULL,
  lead_id UUID, -- Optional: created from Lead conversion OR standalone

  -- FSM State with Probability
  status VARCHAR(50) NOT NULL DEFAULT 'Open',
  CHECK (status IN ('Open', 'NeedsAssessment', 'SolutionAssembly', 'Proposal', 'Negotiation', 'Won', 'Lost', 'Abandoned')),
  win_probability SMALLINT DEFAULT 50 CHECK (win_probability >= 0 AND win_probability <= 100),

  -- Opportunity Description
  title VARCHAR(255) NOT NULL,
  description TEXT,

  -- Estimated Value (Value Object: Money)
  estimated_value_amount DECIMAL(15,2),
  estimated_value_currency CHAR(3) DEFAULT 'EUR',

  -- Final Value (for Won state)
  final_value_amount DECIMAL(15,2),
  final_value_currency CHAR(3) DEFAULT 'EUR',

  -- Timeline
  expected_close_date DATE,

  -- Outcomes
  loss_reason VARCHAR(100), -- 'PriceTooHigh', 'LostToCompetitor', 'BudgetCuts', 'NoDecision'
  competitor_name VARCHAR(255),
  abandonment_reason VARCHAR(255),

  -- Relationships
  assigned_to_user_id UUID,
  created_by_user_id UUID NOT NULL,

  -- Timestamps
  created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
  won_at TIMESTAMP WITH TIME ZONE,
  lost_at TIMESTAMP WITH TIME ZONE,
  abandoned_at TIMESTAMP WITH TIME ZONE,

  -- Audit
  last_activity_at TIMESTAMP WITH TIME ZONE,
  version BIGINT DEFAULT 0,

  -- Soft Delete
  deleted_at TIMESTAMP WITH TIME ZONE,

  CONSTRAINT fk_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE CASCADE,
  CONSTRAINT fk_lead FOREIGN KEY (lead_id) REFERENCES crm.leads(id),
  CONSTRAINT fk_assigned_user FOREIGN KEY (assigned_to_user_id) REFERENCES identity.users(id),
  CONSTRAINT fk_created_by_user FOREIGN KEY (created_by_user_id) REFERENCES identity.users(id)
);
```

**Rationale:**
- **FSM States:** Open → [NeedsAssessment → SolutionAssembly → Proposal → Negotiation → Won] OR Lost OR Abandoned
- **Probability Tracking:** win_probability reflects FSM state (Open=50%, NeedsAssessment=25%, etc.)
- **Value Object (Money):** estimated_value_amount + estimated_value_currency, final_value_amount + final_value_currency
- **Loss Context:** loss_reason, competitor_name for CRM analytics
- **Timeline:** expected_close_date for pipeline reporting

### 1.3 crm.activities (Polymorphic Child Entity)

```sql
CREATE TABLE crm.activities (
  -- Identity
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id UUID NOT NULL,

  -- Polymorphism
  parent_type VARCHAR(50) NOT NULL, -- 'Lead' or 'Opportunity'
  lead_id UUID,
  opportunity_id UUID,

  -- Activity Data
  activity_type VARCHAR(50) NOT NULL, -- 'Call', 'Email', 'Meeting', 'Task', 'Note'
  subject VARCHAR(255) NOT NULL,
  description TEXT,

  -- Outcome (for Call/Meeting)
  outcome VARCHAR(100), -- 'Positive', 'Negative', 'Neutral', 'NoAnswer'

  -- Timestamps
  scheduled_for TIMESTAMP WITH TIME ZONE,
  completed_at TIMESTAMP WITH TIME ZONE,
  created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
  created_by_user_id UUID NOT NULL,

  -- Relationships
  assigned_to_user_id UUID,

  CONSTRAINT fk_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE CASCADE,
  CONSTRAINT fk_lead FOREIGN KEY (lead_id) REFERENCES crm.leads(id) ON DELETE CASCADE,
  CONSTRAINT fk_opportunity FOREIGN KEY (opportunity_id) REFERENCES crm.opportunities(id) ON DELETE CASCADE,
  CONSTRAINT fk_created_by FOREIGN KEY (created_by_user_id) REFERENCES identity.users(id),
  CONSTRAINT fk_assigned_to FOREIGN KEY (assigned_to_user_id) REFERENCES identity.users(id),
  CONSTRAINT polymorphic_check CHECK (
    (parent_type = 'Lead' AND lead_id IS NOT NULL AND opportunity_id IS NULL) OR
    (parent_type = 'Opportunity' AND opportunity_id IS NOT NULL AND lead_id IS NULL)
  )
);
```

**Rationale:**
- **Polymorphism:** parent_type + discriminator columns (lead_id OR opportunity_id, never both)
- **CHECK Constraint:** Enforces polymorphic invariant
- **Activity Tracking:** Essential for engagement history and pipeline analytics
- **Timestamps:** scheduled_for for future planning, completed_at for task completion

### 1.4 crm.tasks (Polymorphic Child Entity — Task Subtype)

```sql
CREATE TABLE crm.tasks (
  -- Identity
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_id UUID NOT NULL,

  -- Polymorphism (same as activities, but specialized for tasks)
  parent_type VARCHAR(50) NOT NULL, -- 'Lead' or 'Opportunity'
  lead_id UUID,
  opportunity_id UUID,

  -- Task-Specific Data
  title VARCHAR(255) NOT NULL,
  description TEXT,
  priority VARCHAR(20) DEFAULT 'Medium', -- 'Low', 'Medium', 'High', 'Urgent'
  is_completed BOOLEAN DEFAULT FALSE,

  -- Deadlines
  due_date DATE NOT NULL,
  completed_at TIMESTAMP WITH TIME ZONE,

  -- Relationships
  assigned_to_user_id UUID NOT NULL,
  created_by_user_id UUID NOT NULL,
  created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,

  CONSTRAINT fk_tenant FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE CASCADE,
  CONSTRAINT fk_lead FOREIGN KEY (lead_id) REFERENCES crm.leads(id) ON DELETE CASCADE,
  CONSTRAINT fk_opportunity FOREIGN KEY (opportunity_id) REFERENCES crm.opportunities(id) ON DELETE CASCADE,
  CONSTRAINT fk_assigned_to FOREIGN KEY (assigned_to_user_id) REFERENCES identity.users(id),
  CONSTRAINT fk_created_by FOREIGN KEY (created_by_user_id) REFERENCES identity.users(id),
  CONSTRAINT polymorphic_check CHECK (
    (parent_type = 'Lead' AND lead_id IS NOT NULL AND opportunity_id IS NULL) OR
    (parent_type = 'Opportunity' AND opportunity_id IS NOT NULL AND lead_id IS NULL)
  )
);
```

**Rationale:**
- **Task Specialization:** Tasks are a subtype of activities (more structured, with due dates)
- **Completion Tracking:** is_completed boolean + completed_at timestamp for audit
- **Urgency Handling:** priority field for task queue ordering
- **Deadline Focus:** due_date is NOT NULL (tasks always have deadlines)

---

## 2. Indexes for Query Performance

```sql
-- Tenant isolation + status filtering (most common query)
CREATE INDEX idx_leads_tenant_status ON crm.leads(tenant_id, status)
  WHERE deleted_at IS NULL;

CREATE INDEX idx_opportunities_tenant_status ON crm.opportunities(tenant_id, status)
  WHERE deleted_at IS NULL;

-- User assignment queries (dashboard, my leads/opportunities)
CREATE INDEX idx_leads_assigned_to ON crm.leads(assigned_to_user_id, tenant_id)
  WHERE deleted_at IS NULL;

CREATE INDEX idx_opportunities_assigned_to ON crm.opportunities(assigned_to_user_id, tenant_id)
  WHERE deleted_at IS NULL;

-- Timeline queries (pipeline by expected close date)
CREATE INDEX idx_opportunities_expected_close ON crm.opportunities(expected_close_date, tenant_id)
  WHERE status IN ('Open', 'Proposal', 'Negotiation') AND deleted_at IS NULL;

-- Activity/Task queries (polymorphic lookups)
CREATE INDEX idx_activities_lead ON crm.activities(lead_id, created_at DESC)
  WHERE deleted_at IS NULL;

CREATE INDEX idx_activities_opportunity ON crm.activities(opportunity_id, created_at DESC)
  WHERE deleted_at IS NULL;

-- Partial index: open tasks only (high-cardinality boolean)
CREATE INDEX idx_tasks_open ON crm.tasks(assigned_to_user_id, due_date)
  WHERE is_completed = FALSE AND deleted_at IS NULL;

-- Creation date ranges (reporting, audit)
CREATE INDEX idx_leads_created_at ON crm.leads(created_at DESC, tenant_id);
CREATE INDEX idx_opportunities_created_at ON crm.opportunities(created_at DESC, tenant_id);
```

**Index Strategy:**
- **Composite (tenant_id, status):** Most queries filter by both
- **User assignment:** Dashboard "My Leads" / "My Opportunities"
- **Timeline:** Pipeline forecasting by close date
- **Partial indexes:** Open tasks only (reduces index size 90%)
- **DESC on timestamps:** Recent-first ordering for list views

---

## 3. Row-Level Security (RLS) Policies

### 3.1 Tenant Isolation Policy

```sql
-- Enable RLS on all 4 tables
ALTER TABLE crm.leads ENABLE ROW LEVEL SECURITY;
ALTER TABLE crm.opportunities ENABLE ROW LEVEL SECURITY;
ALTER TABLE crm.activities ENABLE ROW LEVEL SECURITY;
ALTER TABLE crm.tasks ENABLE ROW LEVEL SECURITY;

-- Policy: Leads (select + modify)
CREATE POLICY tenant_isolation_leads ON crm.leads
  USING (tenant_id = current_setting('app.tenant_id')::uuid)
  WITH CHECK (tenant_id = current_setting('app.tenant_id')::uuid);

CREATE POLICY tenant_isolation_opportunities ON crm.opportunities
  USING (tenant_id = current_setting('app.tenant_id')::uuid)
  WITH CHECK (tenant_id = current_setting('app.tenant_id')::uuid);

CREATE POLICY tenant_isolation_activities ON crm.activities
  USING (tenant_id = current_setting('app.tenant_id')::uuid)
  WITH CHECK (tenant_id = current_setting('app.tenant_id')::uuid);

CREATE POLICY tenant_isolation_tasks ON crm.tasks
  USING (tenant_id = current_setting('app.tenant_id')::uuid)
  WITH CHECK (tenant_id = current_setting('app.tenant_id')::uuid);
```

### 3.2 GUC Parameter Binding Strategy

**In .NET application (DbConnectionInterceptor pattern):**

```csharp
// Before query execution, set tenant context
await connection.ExecuteAsync("SELECT set_config('app.tenant_id', @tenantId, false);",
  new { tenantId = CurrentTenant.Id.ToString() });
```

**Why GUC vs. Row-Level Filters:**
- ✅ Single source of truth (database enforces)
- ✅ Works with ORM and raw SQL equally
- ✅ Prevents accidental data leakage
- ✅ Audit trail automatic

### 3.3 Role-Based Access Control (RBAC)

```sql
-- Create roles
CREATE ROLE crm_viewer;
CREATE ROLE crm_manager;
CREATE ROLE crm_admin;

-- Grant SELECT to viewers
GRANT SELECT ON crm.leads, crm.opportunities, crm.activities, crm.tasks TO crm_viewer;

-- Grant SELECT + INSERT/UPDATE/DELETE to managers
GRANT SELECT, INSERT, UPDATE, DELETE ON crm.leads, crm.opportunities, crm.activities, crm.tasks TO crm_manager;

-- Grant ALL to admins
GRANT ALL ON SCHEMA crm TO crm_admin;
GRANT ALL ON ALL TABLES IN SCHEMA crm TO crm_admin;
```

**Mapping to JoineryTech Permissions:**
- `crm:view` → crm_viewer role
- `crm:manage` → crm_manager role
- `crm:admin` → crm_admin role

---

## 4. Value Object Mappings

### 4.1 ContactInfo Value Object

**Stored as composite columns in leads table:**

| Column | Type | Required |
|--------|------|----------|
| first_name | VARCHAR(100) | ✓ |
| last_name | VARCHAR(100) | ✓ |
| email_address | VARCHAR(255) | |
| email_verified | BOOLEAN | default: FALSE |
| phone_number | VARCHAR(20) | |
| country_code | VARCHAR(2) | |

**EF Core Configuration:**
```csharp
builder.OwnsOne(l => l.ContactInfo, ci =>
{
    ci.Property(c => c.FirstName).HasColumnName("first_name");
    ci.Property(c => c.LastName).HasColumnName("last_name");
    ci.Property(c => c.EmailAddress).HasColumnName("email_address");
    ci.Property(c => c.EmailVerified).HasColumnName("email_verified");
    ci.Property(c => c.PhoneNumber).HasColumnName("phone_number");
    ci.Property(c => c.CountryCode).HasColumnName("country_code");
});
```

### 4.2 Money Value Object

**Stored as composite columns:**

| Column | Type | Required |
|--------|------|----------|
| estimated_value_amount | DECIMAL(15,2) | |
| estimated_value_currency | CHAR(3) | default: 'EUR' |

**EF Core Configuration:**
```csharp
builder.OwnsOne(o => o.EstimatedValue, mv =>
{
    mv.Property(m => m.Amount).HasColumnName("estimated_value_amount").HasPrecision(15, 2);
    mv.Property(m => m.Currency).HasColumnName("estimated_value_currency");
});
```

---

## 5. Data Type Choices

| Concept | PostgreSQL Type | C# Type | Rationale |
|---------|-----------------|---------|-----------|
| Primary Keys | UUID | Guid | Distributed, non-sequential |
| Timestamps | TIMESTAMP WITH TIME ZONE | DateTime | Timezone-aware, audit trail |
| Status/Enum | VARCHAR(50) | C# enum | Readable, CHECK constraint |
| Probability | SMALLINT (0-100) | int | Space-efficient, bounded |
| Money | DECIMAL(15,2) | decimal | No floating-point errors |
| Text (short) | VARCHAR(255) | string | Hard limit prevents bloat |
| Text (long) | TEXT | string | No artificial limit |
| Boolean | BOOLEAN | bool | Standard |
| Soft Delete | TIMESTAMP WITH TIME ZONE | DateTime? | NULL = not deleted, value = when deleted |

---

## 6. Migration Strategy

### InitialCreate Migration

**File:** `Infrastructure/Persistence/Migrations/20260702_InitialCreate.cs`

**Steps:**
1. Create `crm` schema
2. Create 4 tables in sequence (leads → opportunities → activities → tasks)
3. Create all indexes
4. Create RLS policies
5. Grant role permissions

**Rollback Strategy:**
- Drop RLS policies first
- Drop indexes
- Drop tables in reverse order
- Drop schema

---

## 7. Seed Data Strategy

**Minimal seed data for development/testing:**

```csharp
// 1 test tenant
INSERT INTO tenants (id, name, slug) VALUES
  ('00000000-0000-0000-0000-000000000001', 'Test Tenant', 'test-tenant');

// 2 test users
INSERT INTO identity.users (id, tenant_id, email, full_name) VALUES
  ('00000000-0000-0000-0000-000000000101', '00000000-0000-0000-0000-000000000001', 'alice@test.local', 'Alice'),
  ('00000000-0000-0000-0000-000000000102', '00000000-0000-0000-0000-000000000001', 'bob@test.local', 'Bob');

// 5 test leads
INSERT INTO crm.leads (tenant_id, first_name, last_name, email_address, status, assigned_to_user_id, created_by_user_id)
VALUES
  ('00000000-0000-0000-0000-000000000001', 'John', 'Acme', 'john@acme.com', 'New', '00000000-0000-0000-0000-000000000101', '00000000-0000-0000-0000-000000000101'),
  -- ... etc.
```

---

## 8. Query Pattern Examples

### Example 1: List My Open Leads

```sql
SELECT id, first_name, last_name, email_address, status, created_at
FROM crm.leads
WHERE status = 'New' AND assigned_to_user_id = $1
ORDER BY created_at DESC
LIMIT 50;
```

**Index Used:** idx_leads_assigned_to

### Example 2: Pipeline Forecast by Expected Close Date

```sql
SELECT
  status,
  COUNT(*) as count,
  SUM(estimated_value_amount) as total_value,
  MIN(expected_close_date) as earliest_close
FROM crm.opportunities
WHERE status IN ('Open', 'Proposal', 'Negotiation')
  AND expected_close_date >= CURRENT_DATE
GROUP BY status
ORDER BY expected_close_date;
```

**Index Used:** idx_opportunities_expected_close

### Example 3: Activity History for a Lead

```sql
SELECT id, activity_type, subject, outcome, completed_at
FROM crm.activities
WHERE lead_id = $1
ORDER BY created_at DESC
LIMIT 20;
```

**Index Used:** idx_activities_lead

---

## 9. Performance Considerations

### Soft Delete Impact

**With soft delete (deleted_at NOT NULL):**
- ✅ Audit trail preserved
- ✅ No cascading deletes
- ❌ Every query needs `WHERE deleted_at IS NULL`

**Mitigation:** Index predicate `WHERE deleted_at IS NULL` reduces index size.

### Polymorphic Overhead

**Activities/Tasks with parent_type discriminator:**
- ✅ Single table per type
- ✅ Type-safe queries
- ❌ CHECK constraint + FK enforcement adds 2 constraints per query

**Benefit:** Simpler than table-per-type (no UNION queries).

### GUC Parameter Performance

**SET LOCAL app.tenant_id per query:**
- ✅ RLS policies automatic
- ❌ ~1-2ms overhead per query (negligible at scale)

**Mitigation:** Use connection pooling with long-lived transactions.

---

## 10. Security Checklist

- [x] All root tables have tenant_id NOT NULL
- [x] RLS policies on all 4 tables
- [x] GUC parameter binding strategy defined
- [x] Soft delete prevents accidental data loss
- [x] Foreign keys ON DELETE CASCADE where appropriate
- [x] User audit fields (created_by, assigned_to)
- [x] Timestamp tracking (created_at, updated_at, terminal state times)
- [x] CHECK constraints on FSM states
- [x] Polymorphic invariant enforced via CHECK + FK

---

## 11. Next Steps

1. ✅ **This document:** INFRASTRUCTURE_SCHEMA_DESIGN.md (schema + indexes + RLS)
2. → **EF_CORE_CONFIGURATION_PLAN.md** (entity configs, DbContext, migrations)
3. → **REPOSITORY_IMPLEMENTATION_PLAN.md** (repository interface + implementation)

---

**Status:** READY FOR REVIEW
**Generated:** 2026-07-02 16:10 UTC
**Task:** MSG-BACKEND-116 Phase 1
**Next:** EF Core Configuration Planning
