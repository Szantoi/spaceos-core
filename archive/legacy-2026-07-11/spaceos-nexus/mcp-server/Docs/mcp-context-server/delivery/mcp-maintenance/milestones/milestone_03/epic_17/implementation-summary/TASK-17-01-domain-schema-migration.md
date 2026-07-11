---
id: TASK-17-01
title: "Domain schema migration: domains table + FK columns"
epic: EPIC-17
completed_by: Backend Developer Agent
date: 2026-03-13
status: COMPLETE
---

# TASK-17-01: Implementation Summary

## What Was Built?

Implemented the foundational domain registry schema for EPIC-17 (Multi-Domain Configuration & Onboarding).
This enables the MCP server to support arbitrary domains without hardcoding, allowing agents to work across
multiple configuration scopes (engineering, management, discovery, etc.) with full isolation.

## Acceptance Criteria Status

- ✅ **AC-1**: `domains` table created with schema: `id (PK)`, `name (UNIQUE)`, `description`, `config_json`, `created_at`, `updated_at`
- ✅ **AC-2**: `roles.domain_id` FK column added (nullable, references `domains.id`)
- ✅ **AC-3**: `sessions.current_domain_id` FK column added (nullable, references `domains.id`)
- ✅ **AC-4**: Migration file created and integrated into `AgentDb.initSchema()`
- ✅ **AC-5**: All FK constraints enforced via `PRAGMA foreign_keys = ON`
- ✅ **AC-6**: Test coverage: 15+ unit tests validating schema, FK constraints, idempotency, and query methods

## Files Created/Modified

| File | Type | Change | Purpose |
|:-----|:-----|:--------|:---------|
| `src/metadata/migrations/008_epic17_domain_schema.sql` | Migration | **CREATED** | Domain registry DDL; creates `domains` table + `idx_domains_name` index |
| `src/mcp/AgentDb.ts` | TypeScript | **MODIFIED** | Loads migration in `initSchema()`; adds ALTER TABLE for FK columns (with try/catch for idempotency) |
| `src/tests/unit/domain-schema.test.ts` | Test | **CREATED** | 15 unit tests covering schema, FK constraints, backward compat, query methods |

## Key Implementation Details

### 1. Migration File: `008_epic17_domain_schema.sql`

- Creates `domains` table (idempotent via `IF NOT EXISTS`)
- Defines schema:
  - `id TEXT PRIMARY KEY` — domain identifier (e.g., 'engineering', 'management')
  - `name TEXT NOT NULL UNIQUE` — human-readable name
  - `description TEXT` — optional description
  - `config_json TEXT` — optional configuration blob (JSON)
  - `created_at TEXT DEFAULT (datetime('now'))` — creation timestamp
  - `updated_at TEXT DEFAULT (datetime('now'))` — last update timestamp
- Creates index `idx_domains_name` for fast lookup by name
- Includes comment explaining FK ALTER TABLE is done in `AgentDb.initSchema()` (SQLite limitation: no IF NOT EXISTS for ALTER TABLE ADD COLUMN)

### 2. AgentDb Schema Initialization

Updated `initSchema()` to:

1. Load and execute `008_epic17_domain_schema.sql`
2. Add FK column to `roles` table (with try/catch for idempotency):

   ```sql
   ALTER TABLE roles ADD COLUMN domain_id TEXT REFERENCES domains(id)
   ```

3. Add FK column to `sessions` table (with try/catch for idempotency):

   ```sql
   ALTER TABLE sessions ADD COLUMN current_domain_id TEXT REFERENCES domains(id)
   ```

**Idempotency strategy**: ALTERs wrapped in try/catch; if column already exists, `SQLITE_ERROR` is caught and ignored.

### 3. Query Methods (Pre-existing, Verified Working)

Confirmed the following AgentDb methods are functional:

- `listRegisteredDomains(): DomainRow[]` — fetch all domains from registry
- `getRegisteredDomain(nameOrId: string): DomainRow | null` — fetch single domain by id or name
- `upsertDomain(name: string, description?: string): void` — insert or replace domain
- Domain-aware query filters (optional `domainId` param):
  - `getRole(domain, role, domainId?)`
  - `getRolesByDomain(domain, domainId?)`
  - `getWorkflow(..., domainId?)`
  - `getTemplate(..., domainId?)`

### 4. Test Coverage: 15 Unit Tests

| Test Category | Test Count | Focus |
|:--|:--|:--|
| Schema validation | 3 | domains table columns, roles/sessions FK columns |
| Idempotency | 1 | double-call safety |
| Backward compatibility | 1 | existing roles have `domain_id IS NULL` |
| Query methods | 5 | listRegisteredDomains(), getRegisteredDomain() by id/name |
| upsertDomain() | 3 | insert, replace (idempotent), null description |
| FK constraints | 2 | valid FK references, constraint violations |
| Index verification | 1 | idx_domains_name exists |

## Technical Decisions

### 1. **Domain ID as TEXT (not UUID)**

- **Rationale**: Simplifies initial onboarding; domains are created from filesystem directory names (`database/roles/engineering/`, etc.)
- **Trade-off**: No guarantee of global uniqueness across deployments; acceptable for single-server context
- **Future**: Migration path to UUID if multi-server support needed

### 2. **Description + config_json as Optional**

- **Rationale**: Minimal payload for bootstrap; configuration can be extended in TASK-17-02 seeder
- **Trade-off**: Initial seed queries won't find domains by config; acceptable for MVP

### 3. **Nullable FK Columns on Roles/Sessions**

- **Rationale**: Backward compatibility; existing roles/sessions don't have domain context (from pre-EPIC-17 data)
- **Trade-off**: Requires nullable checks in app code; handled by domain-filtering methods (TASK-17-05)

### 4. **Try/Catch for ALTER TABLE Idempotency**

- **Rationale**: SQLite doesn't support `IF NOT EXISTS` for ALTER TABLE; standard approach in production code
- **Trade-off**: Suppresses all ALTER errors (not just "column exists"); acceptable for trusted migrations
- **Mitigation**: Error message explicitly documents intent

## Validation Steps Performed

1. ✅ Migration file syntax validated (CREATE TABLE IF NOT EXISTS, INDEX IF NOT EXISTS, PRAGMAs)
2. ✅ AgentDb integration confirmed (initSchema() loads migration + executes ALTERs)
3. ✅ FK constraints tested: valid refs succeed, invalid refs rejected
4. ✅ Backward compat tested: existing roles rows remain valid with `domain_id IS NULL`
5. ✅ Query methods verified functional: listRegisteredDomains(), getRegisteredDomain()
6. ✅ Index verification: idx_domains_name created
7. ✅ Idempotency confirmed: double-call to initSchema() does not throw

## Assumptions & Constraints

- **Constraint**: PRAGMA foreign_keys = ON (enforced in initSchema())
- **Assumption**: Database file exists before initSchema() call (handled by DatabaseConnectionManager)
- **Assumption**: No concurrent ALTER TABLE operations (single server, serial startup)
- **Future task dependency**: TASK-17-02 (seeder) populates domains table from filesystem

## Next Steps

1. **TASK-17-02**: Extend AgentDbSeeder to load `database/roles/` directory structure → populate `domains` table
2. **TASK-17-03**: Extend `bootstrap_agent()` tool to set `current_domain_id` in session
3. **TASK-17-04**: Implement `switch_domain()` and `list_available_domains()` MCP tools
4. **TASK-17-05**: Add domain filtering to existing query methods (WHERE domain_id = ?)
5. **TASK-17-06**: E2E multi-domain isolation test

## Code Quality Metrics

| Metric | Value | Target |
|:--|:--|:--|
| TypeScript errors | 0 | 0 ✅ |
| Test coverage | 15 tests, 20+ assertions | 10+ ✅ |
| Migration idempotency | ✅ Safe to re-run | Required ✅ |
| FK constraint validation | ✅ Tested | Required ✅ |

## Key Learnings

1. **SQLite ALTER TABLE Limitations**: No IF NOT EXISTS clause; must use try/catch in application code
2. **Nullable FK Columns**: Backward compatibility pattern; works well for gradual schema evolution
3. **Domain Registry Decoupling**: Separating `domains` (registry) from `roles.domain` (legacy annotation) enables flexible multi-domain support

## Peer Review Sign-Off

- [ ] Code reviewed (syntax, logic, error handling)
- [ ] Tests validated (all 15 passing, no regressions)
- [ ] Migration file verified (idempotent, no breaking changes)
- [ ] Ready for TASK-17-02 (seeder integration)

---

**Status**: COMPLETE — TASK-17-01 ready for dependency chains TASK-17-02/03/05
