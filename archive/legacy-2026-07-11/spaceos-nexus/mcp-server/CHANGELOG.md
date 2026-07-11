# Changelog — JoineryTech MCP Server

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [M02 - 2026-03-05] Context Layer Integration (EPIC-09)

### [M02 - 2026-03-11] Legacy Tools Adapter (EPIC-14 Phase 2)
- Introduced `LegacyPlugin` to wrap old file-based MCP tools with decorators.
- Legacy wrappers log deprecation warnings; removal planned for v2.0 (2026-06-01).
- Added migration guide `docs/LEGACY-TOOLS-MIGRATION.md`.


### 🎉 **Major Features**

#### **Context Schema Layer (6 tables)**
- **Roles Table**: Domain-scoped agent personas (backend_developer, tech_lead, explorer, architect)
- **Role Schemas Table**: MCP tool permissions & input/output schemas per role
- **Runbooks Table**: Step-by-step workflows for agents following a specific role
- **Workflows Table**: FSM workflow definitions (feature delivery, incident response, etc.)
- **Templates Table**: Document templates for artifact generation (implementation summaries, test reports)
- **Standards Table**: Organizational standards, ADRs, and best practices

#### **AgentDb Service Class**
- Central single-entry point for all SQLite operations (EPIC-08 write layer + EPIC-09 context layer)
- Type-safe query builders with prepared statement caching
- 11 query methods: `getRole()`, `getRolesByDomain()`, `getRoleSchema()`, `getRunbook()`, `getWorkflow()`, `getWorkflowsByRole()`, `getTemplate()`, `getTemplatesByRole()`, `getStandard()`, `getAllStandards()`
- Foreign key enforcement & PRAGMA configuration (WAL mode, FULL synchronous)
- Idempotent schema initialization (safe to call multiple times)

#### **Seeder Script (AgentDbSeeder)**
- Recursive directory scanning for YAML role definitions
- Automatic JSON schema conversion
- Idempotent INSERT OR REPLACE logic
- Full integration with CLI for batch population

#### **Express App Integration**
- AgentDb initialized at startup with error handling (SIGTERM graceful shutdown)
- Dependency injection into MCP routers (`mcpRouter`, `mcpServer`)
- Logging structured for Prometheus scraping
- Environment-driven configuration (AGENT_DB_PATH support)

### ✅ **Acceptance Criteria Met**

| TASK | Criteria | Status |
|:-----|:---------|:-------|
| **TASK-09-01** | ✅ Schema DDL + TypeScript types | Complete |
| **TASK-09-02** | ✅ AgentDb service 473 lines | Complete |
| **TASK-09-03** | ✅ Seeder idempotency | Complete |
| **TASK-09-04** | ✅ Express DI integration | Complete |
| **TASK-09-05** | ✅ Unit tests (115/115 passing) | Complete |

### 🔧 **Technical Improvements**

#### **Error Handling**
- ✅ Startup error handling for `agentDb.initSchema()` with `process.exit(1)` on failure
- ✅ Query getter methods now wrapped in try-catch with structured logging
- ✅ Removed redundant type assertions (`as X | undefined || null` → `result ?? null`)

#### **Database Reliability**
- ✅ PRAGMA configuration: `foreign_keys=ON`, `journal_mode=WAL`, `synchronous=FULL`
- ✅ Prepared statement caching via `db.prepare()` (SQL injection safe)
- ✅ In-memory SQLite `:memory:` for unit tests (115 tests passing)
- ✅ Persistent test databases in `.test-databases/` directory

#### **Graceful Shutdown**
- ✅ Signal handlers for `SIGTERM` and `SIGINT`
- ✅ Database connection cleanup on process termination
- ✅ Prevents WAL transaction corruption during server restarts

#### **TypeScript Compliance**
- ✅ `strict: true` mode enforced in `tsconfig.json`
- ✅ Type-safe interfaces exported (no `any` types)
- ✅ Null safety: explicit `null` returns instead of `undefined`

### 🗂️ **Files Modified**

| File | Change | Impact |
|:-----|:-------|:-------|
| `src/mcp/AgentDb.ts` | +473 lines (new) | Central database service |
| `src/metadata/ContextSchemaInitializer.ts` | +77 lines (new) | Migration runner |
| `src/metadata/migrations/003_epic09_context_schema.sql` | +145 lines (new) | Context schema DDL |
| `src/AgentDbSeeder.ts` | +361 lines (new) | YAML to SQLite seeder |
| `src/index.ts` | +15 lines (modified) | AgentDb init + graceful shutdown |
| `src/mcp/mcpRouter.ts` | +1 line (modified) | AgentDb DI parameter |
| `src/mcp/mcpServer.ts` | +1 line (modified) | AgentDb DI parameter |
| `src/tests/unit/AgentDb.test.ts` | +528 lines (new) | 19+ unit tests |
| `src/tests/unit/ContextSchema.test.ts` | +200+ lines (new) | Schema validation tests |
| `src/tests/unit/seed-agent-db.test.ts` | +361 lines (new) | Seeder tests |
| `database/standards/adrs/ADR-004.md` | +200+ lines (new) | Loose coupling decision |

### 📊 **Test Coverage**

- **Unit Tests**: 115/115 passing (100%)
- **Schema Initialization**: Idempotency verified
- **Query Methods**: All 11 getters tested with in-memory DB
- **Foreign Key Constraints**: Enforcement verified
- **Integration**: E2E seeder validation with real temp files
- **Type Safety**: No `any` types; full strict mode coverage

### 📝 **Documentation**

- ✅ EPIC-09-FINAL-STATUS.md — Delivery summary
- ✅ ADR-004-epic08-epic09-schema-integration.md — Architectural decision
- ✅ Implementation summaries in task files (TASK-09-01 through TASK-09-05)
- ✅ Inline JSDoc comments for all AgentDb methods
- ✅ CHANGELOG.md (this file) — Release notes

### 🚀 **Deployment Notes**

#### **Database Migration**
- No manual migration required; `agentDb.initSchema()` auto-creates tables at startup
- Migrations are idempotent (CREATE TABLE IF NOT EXISTS)
- Existing EPIC-08 write-layer schema coexists without conflicts

#### **Configuration**
- **Env Var**: `AGENT_DB_PATH` (optional; defaults to `database/metadata.db`)
- **Startup Error Handling**: App exits with code 1 if AgentDb initialization fails
- **Graceful Shutdown**: SIGTERM/SIGINT captured; database closed cleanly

#### **Compatibility**
- ✅ Backward compatible with EPIC-08 write layer
- ✅ No breaking changes to existing Express routes
- ✅ Loose coupling strategy (no FK between layers)

### ⚠️ **Known Limitations**

- **Query Timeouts**: No `.timeout()` configured for long-running queries; see FUTURE for hardening
- **Transaction Boundaries**: Multi-step migrations not wrapped in explicit transactions; both DDLs must succeed independently
- **Result Type Pattern**: Current implementation uses throw-based errors; modern Result<T, E> pattern not yet adopted
- **Connection Pooling**: SQLite does not support connection pooling; WAL mode provides concurrent reads

### 🔮 **Planned Improvements (M03+)**

1. **EPIC-10 (Agent Bootstrap)**: Use AgentDb to load agent roles on startup
2. **EPIC-11 (RBAC Middleware)**: Integrate role queries into request context validation
3. **EPIC-12+ (RBAC Enforcement)**: Wire role permissions into MCP tool access control
4. **Result<T, E> Refactoring**: Migrate from throw-based to explicit error type
5. **Query Timeout Protection**: Add `.defaultTimeout()` configuration
6. **Transaction Wrapper**: Batch multiple DDL statements in explicit transaction

---

## [M01 - 2026-02-XX] Write Layer (EPIC-08)

### 🎉 **Features**

- Write-layer schema for agent sessions, artifacts, workflow events, and checkpoints
- SessionManager service with role-based session tracking
- ResourceTracker for artifact lifecycle management
- SQLite backend with WAL mode and FK enforcement

### ✅ **Status**: Delivered

---

## **Versioning**

- **M02 Release**: EPIC-09 Context Layer Integration (2026-03-05)
- **M01 Release**: EPIC-08 Write Layer (2026-02-XX)

---

## **Contributors**

- Backend Developer Agent
- Architect Agent
- Tech Lead Agent
