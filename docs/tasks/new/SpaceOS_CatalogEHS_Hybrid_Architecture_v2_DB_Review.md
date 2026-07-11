# SpaceOS Catalog + EHS Hybrid Architecture (v2 DB REVIEW)

**Status:** REVIEW
**Created:** 2026-06-23
**Reviewer:** Architect (DB Schema Specialist perspective)
**Base Document:** SpaceOS_CatalogEHS_Hybrid_Architecture_v1.md

---

## Review Summary

This document reviews the database schema design from v1 architecture draft. The review focuses on:
- Schema normalization and design patterns
- Index strategy and query performance
- Row-Level Security (RLS) policies
- Data integrity constraints
- Migration strategy and backwards compatibility

---

## Findings Overview

| Severity | Count | Category |
|---|---|---|
| 🔴 CRITICAL | 0 | — |
| 🟠 HIGH | 3 | Audit trail, cascading delete, user reference |
| 🟡 MEDIUM | 4 | FK constraints, indexing, enum types |
| 🟢 LOW | 3 | Partitioning, composite indexes |

---

## 🟠 HIGH Severity Findings

### H1: Missing soft delete pattern for audit compliance

**Location:** `ehs.risk_assessments` table
**Issue:** No `deleted_at` column for soft delete pattern

**Current schema:**
```sql
CREATE TABLE ehs.risk_assessments (
    id UUID PRIMARY KEY,
    -- ... fields ...
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by VARCHAR(255) NOT NULL
);
```

**Problem:**
ISO 45001 compliance requires audit trail of ALL risk assessments, including deleted ones. Hard deletes (via CASCADE or manual DELETE) violate audit requirements.

**Recommended fix:**
```sql
CREATE TABLE ehs.risk_assessments (
    id UUID PRIMARY KEY,
    -- ... existing fields ...
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by VARCHAR(255) NOT NULL,

    -- ADD soft delete columns
    deleted_at TIMESTAMPTZ NULL,
    deleted_by VARCHAR(255) NULL,

    -- ADD version for optimistic locking (optional but recommended)
    version INT NOT NULL DEFAULT 1
);

-- Partial index for active records only
CREATE INDEX idx_risk_assessments_active
    ON ehs.risk_assessments(organization_id, created_at DESC)
    WHERE deleted_at IS NULL;

-- Index for deleted records (audit queries)
CREATE INDEX idx_risk_assessments_deleted
    ON ehs.risk_assessments(deleted_at DESC)
    WHERE deleted_at IS NOT NULL;
```

**Impact if not fixed:**
- ❌ Audit trail broken if records deleted
- ❌ Cannot restore accidentally deleted assessments
- ❌ Compliance violation (ISO 45001 requires permanent records)

**Recommendation:** **MUST FIX** before production deployment

---

### H2: Dangerous cascading delete on organization FK

**Location:** `ehs.risk_assessments` table
**Issue:** `ON DELETE CASCADE` on `organization_id` FK

**Current schema:**
```sql
CONSTRAINT fk_organization FOREIGN KEY (organization_id)
    REFERENCES kernel.organizations(id) ON DELETE CASCADE
```

**Problem:**
If an organization is accidentally deleted (or malicious delete), ALL risk assessments for that org are permanently lost. This is a **data loss risk** in multi-tenant SaaS.

**Recommended fix:**
```sql
-- Change to RESTRICT (prevent org deletion if assessments exist)
CONSTRAINT fk_organization FOREIGN KEY (organization_id)
    REFERENCES kernel.organizations(id) ON DELETE RESTRICT

-- OR implement soft delete on organizations table (preferred)
-- Then risk_assessments inherits soft delete via RLS policy filtering
```

**Alternative approach (if org soft delete exists):**
```sql
-- RLS policy automatically filters deleted orgs
CREATE POLICY risk_assessments_tenant_isolation ON ehs.risk_assessments
    USING (
        organization_id = current_setting('app.current_organization_id')::INT
        AND organization_id IN (
            SELECT id FROM kernel.organizations WHERE deleted_at IS NULL
        )
    );
```

**Impact if not fixed:**
- ❌ Accidental org deletion = data loss (no recovery)
- ❌ Violates "Immutability & Trust" golden rule

**Recommendation:** **MUST FIX** - change to `RESTRICT` or soft delete

---

### H3: User reference not enforced via FK constraint

**Location:** `ehs.risk_assessments` table
**Issue:** `created_by` is VARCHAR(255) without FK to users table

**Current schema:**
```sql
created_by VARCHAR(255) NOT NULL
```

**Problem:**
- No referential integrity - can store invalid user IDs
- Orphaned records if user is deleted
- Cannot JOIN to users table for audit reports

**Recommended fix (Option A - FK constraint):**
```sql
-- If auth.users table exists in same DB
created_by UUID NOT NULL,  -- or INT if user ID is integer

CONSTRAINT fk_created_by FOREIGN KEY (created_by)
    REFERENCES auth.users(id) ON DELETE SET NULL  -- or RESTRICT
```

**Recommended fix (Option B - External auth provider):**
If using Keycloak/Auth0 (external user store), keep VARCHAR but add validation:
```sql
-- Add CHECK constraint for format validation
created_by VARCHAR(255) NOT NULL CHECK (created_by ~ '^[a-f0-9-]{36}$'),  -- UUID format

-- Add application-level validation in EHS module
public class RiskAssessment {
    public void ValidateCreatedBy(IUserRepository userRepo) {
        if (!userRepo.UserExists(CreatedBy)) {
            throw new InvalidOperationException($"User {CreatedBy} does not exist");
        }
    }
}
```

**Impact if not fixed:**
- 🟡 Audit reports cannot resolve user names
- 🟡 Orphaned records if user deleted from auth provider

**Recommendation:** **SHOULD FIX** - add FK if users table exists, else validate at app level

---

## 🟡 MEDIUM Severity Findings

### M1: Missing FK constraint for assessment_id

**Location:** `ehs.risk_assessments` table
**Issue:** `assessment_id INT NULL` has no FK constraint

**Current schema:**
```sql
assessment_id INT NULL  -- FK to future ehs.assessments table
```

**Problem:**
- No referential integrity - can store invalid assessment IDs
- Migration complexity when `ehs.assessments` table is created later

**Recommended fix:**
```sql
-- Option 1: Create skeleton ehs.assessments table NOW
CREATE TABLE ehs.assessments (
    id SERIAL PRIMARY KEY,
    organization_id INT NOT NULL,
    title VARCHAR(255) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT fk_assessment_org FOREIGN KEY (organization_id)
        REFERENCES kernel.organizations(id) ON DELETE RESTRICT
);

-- Then add FK constraint
ALTER TABLE ehs.risk_assessments
    ADD CONSTRAINT fk_assessment FOREIGN KEY (assessment_id)
        REFERENCES ehs.assessments(id) ON DELETE CASCADE;

-- Option 2: Add TODO comment in migration + create later
-- In migration file:
-- TODO: Add FK constraint when ehs.assessments table is created
-- ALTER TABLE ehs.risk_assessments ADD CONSTRAINT fk_assessment ...
```

**Recommendation:** **Option 1 preferred** - create skeleton table to avoid data integrity issues

---

### M2: Missing index on data_hash for integrity checks

**Location:** `ehs.risk_assessments` table
**Issue:** No index on `data_hash` column

**Current schema:**
```sql
data_hash VARCHAR(64) NOT NULL
-- No index
```

**Problem:**
Integrity verification queries will be slow:
```sql
-- Slow without index (full table scan)
SELECT * FROM ehs.risk_assessments
WHERE data_hash = 'abc123...';
```

**Recommended fix:**
```sql
CREATE INDEX idx_risk_assessments_data_hash
    ON ehs.risk_assessments(data_hash);
```

**Impact if not fixed:**
- 🟡 Slow integrity verification queries
- 🟡 Duplicate detection inefficient

**Recommendation:** **SHOULD ADD** if integrity checks are frequent

---

### M3: Category should be ENUM or lookup table

**Location:** `ehs.risk_assessments` table
**Issue:** `category VARCHAR(50)` allows arbitrary strings

**Current schema:**
```sql
category VARCHAR(50) NOT NULL
```

**Problem:**
- Typos allowed ("machinery" vs "Machinery" vs "machinary")
- No validation of allowed categories
- Cannot query distinct categories efficiently

**Recommended fix (Option A - PostgreSQL ENUM):**
```sql
-- Create ENUM type
CREATE TYPE ehs.risk_category AS ENUM (
    'machinery',
    'chemical',
    'ergonomic',
    'electrical',
    'fire',
    'environmental'
);

-- Use in table
ALTER TABLE ehs.risk_assessments
    ALTER COLUMN category TYPE ehs.risk_category USING category::ehs.risk_category;
```

**Recommended fix (Option B - Lookup table):**
```sql
-- Create lookup table
CREATE TABLE ehs.risk_categories (
    id SERIAL PRIMARY KEY,
    code VARCHAR(50) UNIQUE NOT NULL,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    active BOOLEAN NOT NULL DEFAULT TRUE
);

-- Seed data
INSERT INTO ehs.risk_categories (code, name) VALUES
    ('machinery', 'Machinery & Equipment'),
    ('chemical', 'Chemical Hazards'),
    ('ergonomic', 'Ergonomic Risks'),
    ('electrical', 'Electrical Safety'),
    ('fire', 'Fire & Explosion'),
    ('environmental', 'Environmental Hazards');

-- Add FK constraint
ALTER TABLE ehs.risk_assessments
    ADD COLUMN category_id INT NOT NULL,
    ADD CONSTRAINT fk_category FOREIGN KEY (category_id)
        REFERENCES ehs.risk_categories(id);

-- Drop old column
ALTER TABLE ehs.risk_assessments DROP COLUMN category;
```

**Trade-offs:**
| Approach | Pros | Cons |
|---|---|---|
| **ENUM** | Simple, type-safe, small storage | Hard to modify (requires migration) |
| **Lookup table** | Easy to add categories, i18n support | More JOINs, complex queries |
| **VARCHAR** | Flexible, no schema change | Typos, no validation |

**Recommendation:** **ENUM for v1** (simple, fixed categories). Switch to lookup table if categories become dynamic.

---

### M4: Missing composite index for common queries

**Location:** `ehs.risk_assessments` table
**Issue:** Separate indexes on `organization_id` and `created_at` instead of composite

**Current schema:**
```sql
CREATE INDEX idx_risk_assessments_org ON ehs.risk_assessments(organization_id);
CREATE INDEX idx_risk_assessments_created_at ON ehs.risk_assessments(created_at DESC);
```

**Problem:**
Common query pattern (recent assessments for org) doesn't use optimal index:
```sql
SELECT * FROM ehs.risk_assessments
WHERE organization_id = 123
ORDER BY created_at DESC
LIMIT 10;
```
PostgreSQL will use EITHER org index OR created_at index, not both efficiently.

**Recommended fix:**
```sql
-- Replace with composite index (covers both columns)
CREATE INDEX idx_risk_assessments_org_created
    ON ehs.risk_assessments(organization_id, created_at DESC);

-- Keep created_at index only if global queries are common
-- (e.g., admin view of ALL orgs' recent assessments)
CREATE INDEX idx_risk_assessments_created_at
    ON ehs.risk_assessments(created_at DESC);
```

**Index usage analysis:**
```sql
-- Query A: org-scoped recent assessments (USES composite)
SELECT * FROM ehs.risk_assessments
WHERE organization_id = 123
ORDER BY created_at DESC;

-- Query B: global recent assessments (USES created_at index)
SELECT * FROM ehs.risk_assessments
ORDER BY created_at DESC
LIMIT 100;

-- Query C: org-scoped category filter (USES composite + filter)
SELECT * FROM ehs.risk_assessments
WHERE organization_id = 123 AND category = 'machinery'
ORDER BY created_at DESC;
```

**Recommendation:** **ADD composite index** for typical org-scoped queries

---

## 🟢 LOW Severity Findings

### L1: No partitioning strategy for high-volume tables

**Location:** `ehs.risk_assessments` table
**Issue:** No table partitioning defined

**Impact:**
For high-volume deployments (>100k assessments/year), query performance degrades without partitioning.

**Recommended fix (future optimization):**
```sql
-- Create partitioned table (requires PostgreSQL 10+)
CREATE TABLE ehs.risk_assessments_partitioned (
    id UUID NOT NULL,
    -- ... all fields ...
    created_at TIMESTAMPTZ NOT NULL
) PARTITION BY RANGE (created_at);

-- Create yearly partitions
CREATE TABLE ehs.risk_assessments_2026 PARTITION OF ehs.risk_assessments_partitioned
    FOR VALUES FROM ('2026-01-01') TO ('2027-01-01');

CREATE TABLE ehs.risk_assessments_2027 PARTITION OF ehs.risk_assessments_partitioned
    FOR VALUES FROM ('2027-01-01') TO ('2028-01-01');

-- Automatic partition creation script (cronjob)
-- CREATE TABLE ehs.risk_assessments_{year} FOR VALUES FROM ...
```

**When to implement:**
- ⏱️ Defer to Phase 2 (after 50k records)
- Monitor query performance quarterly
- Plan migration strategy (pt-online-schema-change equivalent for PostgreSQL)

**Recommendation:** **DEFER** - not needed for v1 MVP

---

### L2: Catalog search_keywords column needs GIN index

**Location:** `catalog.items` table
**Issue:** `search_keywords TEXT[]` has no GIN index

**Current schema:**
```sql
ALTER TABLE catalog.items
    ADD COLUMN search_keywords TEXT[] NULL;
```

**Problem:**
Array searches without GIN index are slow:
```sql
-- Slow without index (sequential scan)
SELECT * FROM catalog.items
WHERE search_keywords @> ARRAY['wood', 'panel'];
```

**Recommended fix:**
```sql
-- Add GIN index for array containment queries
CREATE INDEX idx_catalog_items_search_keywords
    ON catalog.items USING GIN (search_keywords);
```

**Index usage:**
```sql
-- Fast with GIN index
SELECT * FROM catalog.items
WHERE search_keywords @> ARRAY['wood'];  -- containment

-- Also fast
SELECT * FROM catalog.items
WHERE search_keywords && ARRAY['wood', 'panel'];  -- overlap
```

**Recommendation:** **ADD if implementing backend search** (Week 2 optional)

---

### L3: RLS policy performance for multi-tenant queries

**Location:** `ehs.risk_assessments` RLS policy
**Issue:** `current_setting()` function call in RLS policy

**Current policy:**
```sql
CREATE POLICY risk_assessments_tenant_isolation ON ehs.risk_assessments
    USING (organization_id = current_setting('app.current_organization_id')::INT);
```

**Performance consideration:**
`current_setting()` is evaluated for EVERY row. For large result sets, this can be slow.

**Optimization:**
```sql
-- Option 1: Use prepared statement parameter (faster)
-- Set organization_id in connection pool:
-- SET app.current_organization_id = 123;

-- Option 2: Use SECURITY DEFINER function (cache result)
CREATE FUNCTION ehs.get_current_org_id() RETURNS INT AS $$
    SELECT current_setting('app.current_organization_id', true)::INT;
$$ LANGUAGE SQL STABLE SECURITY DEFINER;

CREATE POLICY risk_assessments_tenant_isolation ON ehs.risk_assessments
    USING (organization_id = ehs.get_current_org_id());
```

**Benchmark recommendation:**
- Test with >10k rows
- Compare query plan with/without STABLE function
- Monitor execution time in production

**Recommendation:** **MONITOR** - optimize if slow queries detected

---

## Approved Schema Changes (v2 → v1 diff)

Based on HIGH/MEDIUM findings, the following changes are **REQUIRED** for v2:

### Change 1: Add soft delete columns

```sql
ALTER TABLE ehs.risk_assessments
    ADD COLUMN deleted_at TIMESTAMPTZ NULL,
    ADD COLUMN deleted_by VARCHAR(255) NULL,
    ADD COLUMN version INT NOT NULL DEFAULT 1;
```

### Change 2: Fix organization FK constraint

```sql
ALTER TABLE ehs.risk_assessments
    DROP CONSTRAINT fk_organization;

ALTER TABLE ehs.risk_assessments
    ADD CONSTRAINT fk_organization FOREIGN KEY (organization_id)
        REFERENCES kernel.organizations(id) ON DELETE RESTRICT;
```

### Change 3: Create skeleton ehs.assessments table

```sql
CREATE TABLE ehs.assessments (
    id SERIAL PRIMARY KEY,
    organization_id INT NOT NULL,
    title VARCHAR(255) NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'draft',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by VARCHAR(255) NOT NULL,
    deleted_at TIMESTAMPTZ NULL,
    deleted_by VARCHAR(255) NULL,

    CONSTRAINT fk_assessment_org FOREIGN KEY (organization_id)
        REFERENCES kernel.organizations(id) ON DELETE RESTRICT
);

ALTER TABLE ehs.risk_assessments
    ADD CONSTRAINT fk_assessment FOREIGN KEY (assessment_id)
        REFERENCES ehs.assessments(id) ON DELETE CASCADE;
```

### Change 4: Add composite index

```sql
DROP INDEX idx_risk_assessments_org;
DROP INDEX idx_risk_assessments_created_at;

CREATE INDEX idx_risk_assessments_org_created
    ON ehs.risk_assessments(organization_id, created_at DESC)
    WHERE deleted_at IS NULL;

CREATE INDEX idx_risk_assessments_created_at
    ON ehs.risk_assessments(created_at DESC)
    WHERE deleted_at IS NULL;
```

### Change 5: Convert category to ENUM

```sql
CREATE TYPE ehs.risk_category AS ENUM (
    'machinery',
    'chemical',
    'ergonomic',
    'electrical',
    'fire',
    'environmental',
    'biological',
    'physical',
    'psychosocial',
    'other'
);

ALTER TABLE ehs.risk_assessments
    ALTER COLUMN category TYPE ehs.risk_category
        USING category::ehs.risk_category;
```

---

## Migration Strategy

### Migration file: `0001_create_ehs_module.sql`

```sql
-- =========================================
-- EHS Module - Risk Assessment Schema v2
-- Migration: 0001_create_ehs_module
-- Date: 2026-06-23
-- =========================================

BEGIN;

-- 1. Create schema
CREATE SCHEMA IF NOT EXISTS ehs;

-- 2. Create ENUM type for risk categories
CREATE TYPE ehs.risk_category AS ENUM (
    'machinery',
    'chemical',
    'ergonomic',
    'electrical',
    'fire',
    'environmental',
    'biological',
    'physical',
    'psychosocial',
    'other'
);

-- 3. Create assessments table (parent)
CREATE TABLE ehs.assessments (
    id SERIAL PRIMARY KEY,
    organization_id INT NOT NULL,
    title VARCHAR(255) NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'draft',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by VARCHAR(255) NOT NULL,
    deleted_at TIMESTAMPTZ NULL,
    deleted_by VARCHAR(255) NULL,

    CONSTRAINT fk_assessment_org FOREIGN KEY (organization_id)
        REFERENCES kernel.organizations(id) ON DELETE RESTRICT
);

-- 4. Create risk_assessments table
CREATE TABLE ehs.risk_assessments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    assessment_id INT NULL,
    organization_id INT NOT NULL,
    likelihood_before INT NOT NULL CHECK (likelihood_before BETWEEN 1 AND 5),
    severity_before INT NOT NULL CHECK (severity_before BETWEEN 1 AND 5),
    likelihood_after INT NULL CHECK (likelihood_after BETWEEN 1 AND 5),
    severity_after INT NULL CHECK (severity_after BETWEEN 1 AND 5),
    category ehs.risk_category NOT NULL,
    notes TEXT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by VARCHAR(255) NOT NULL,
    data_hash VARCHAR(64) NOT NULL,

    -- Soft delete columns (H1 fix)
    deleted_at TIMESTAMPTZ NULL,
    deleted_by VARCHAR(255) NULL,
    version INT NOT NULL DEFAULT 1,

    -- Constraints
    CONSTRAINT fk_organization FOREIGN KEY (organization_id)
        REFERENCES kernel.organizations(id) ON DELETE RESTRICT,  -- H2 fix
    CONSTRAINT fk_assessment FOREIGN KEY (assessment_id)
        REFERENCES ehs.assessments(id) ON DELETE CASCADE,  -- M1 fix
    CONSTRAINT chk_after_values CHECK (
        (likelihood_after IS NULL AND severity_after IS NULL) OR
        (likelihood_after IS NOT NULL AND severity_after IS NOT NULL)
    )
);

-- 5. Create indexes (M4 fix - composite index)
CREATE INDEX idx_risk_assessments_org_created
    ON ehs.risk_assessments(organization_id, created_at DESC)
    WHERE deleted_at IS NULL;

CREATE INDEX idx_risk_assessments_created_at
    ON ehs.risk_assessments(created_at DESC)
    WHERE deleted_at IS NULL;

CREATE INDEX idx_risk_assessments_category
    ON ehs.risk_assessments(category);

CREATE INDEX idx_risk_assessments_assessment_id
    ON ehs.risk_assessments(assessment_id)
    WHERE assessment_id IS NOT NULL;

CREATE INDEX idx_risk_assessments_deleted
    ON ehs.risk_assessments(deleted_at DESC)
    WHERE deleted_at IS NOT NULL;

-- 6. Row-Level Security (RLS)
ALTER TABLE ehs.risk_assessments ENABLE ROW LEVEL SECURITY;

CREATE POLICY risk_assessments_tenant_isolation ON ehs.risk_assessments
    USING (organization_id = current_setting('app.current_organization_id')::INT);

ALTER TABLE ehs.assessments ENABLE ROW LEVEL SECURITY;

CREATE POLICY assessments_tenant_isolation ON ehs.assessments
    USING (organization_id = current_setting('app.current_organization_id')::INT);

COMMIT;
```

### Rollback migration: `0001_rollback_ehs_module.sql`

```sql
BEGIN;

DROP POLICY IF EXISTS risk_assessments_tenant_isolation ON ehs.risk_assessments;
DROP POLICY IF EXISTS assessments_tenant_isolation ON ehs.assessments;

DROP TABLE IF EXISTS ehs.risk_assessments CASCADE;
DROP TABLE IF EXISTS ehs.assessments CASCADE;
DROP TYPE IF EXISTS ehs.risk_category CASCADE;
DROP SCHEMA IF EXISTS ehs CASCADE;

COMMIT;
```

---

## Testing Checklist

- [ ] **Schema creation:** Verify migration runs without errors
- [ ] **FK constraints:** Test CASCADE/RESTRICT behavior on org deletion
- [ ] **CHECK constraints:** Verify likelihood/severity range validation (1-5)
- [ ] **Soft delete:** Test deleted_at filtering in queries
- [ ] **RLS policy:** Test tenant isolation (org A cannot see org B's data)
- [ ] **Composite index usage:** EXPLAIN ANALYZE on common queries
- [ ] **ENUM type:** Test category validation (reject invalid values)

---

## Performance Benchmarks (Post-Deployment)

Run these queries after 10k+ records to validate index performance:

```sql
-- Benchmark 1: Org-scoped recent assessments (should use composite index)
EXPLAIN ANALYZE
SELECT * FROM ehs.risk_assessments
WHERE organization_id = 123
ORDER BY created_at DESC
LIMIT 10;

-- Benchmark 2: Category filtering (should use category index + composite)
EXPLAIN ANALYZE
SELECT * FROM ehs.risk_assessments
WHERE organization_id = 123 AND category = 'machinery'
ORDER BY created_at DESC;

-- Benchmark 3: Soft delete filtering (should use partial index)
EXPLAIN ANALYZE
SELECT COUNT(*) FROM ehs.risk_assessments
WHERE deleted_at IS NULL;
```

**Target performance:**
- Query 1: < 5ms (indexed seek)
- Query 2: < 10ms (index intersection)
- Query 3: < 2ms (index-only scan)

---

## v2 Review Status: ✅ APPROVED WITH CHANGES

**Summary:**
The v1 schema design is **SOLID** but requires **5 critical fixes** before implementation:

1. ✅ Add soft delete columns (audit compliance)
2. ✅ Fix organization FK constraint (prevent data loss)
3. ✅ Create skeleton ehs.assessments table (referential integrity)
4. ✅ Add composite index (query performance)
5. ✅ Convert category to ENUM (data validation)

**Next step:** Proceed to **v3 Security Review** with updated schema.

---

**END OF v2 DB REVIEW**
