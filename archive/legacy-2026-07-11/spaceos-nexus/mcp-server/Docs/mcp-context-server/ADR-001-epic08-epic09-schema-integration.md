---
id: adr-001-epic08-epic09-schema-integration
title: "ADR-001: EPIC-08 Write Layer ↔ EPIC-09 Context Schema Integration"
type: adr
status: approved
decision_date: 2026-03-05
approved_by: Architect
reviewed_by: Tech Lead
scope: mcp-context-server / M02 planning
related_epics: EPIC-08 (M01), EPIC-09 (M02)
---

# ADR-001: EPIC-08 Write Layer ↔ EPIC-09 Context Schema Integration

**Status:** ✅ **APPROVED** (2026-03-05)
**Approved by:** Architect
**Reviewed by:** Tech Lead
**Applies to:** M02 implementation (EPIC-09 onwards)

---

## Context

The MCP server will consolidate all data into a single SQLite database (`agent.db`):

### EPIC-08 (M01) — Write Layer Schema

Four tables track agent session lifecycle:

- `sessions` — agent identity, timing, FSM state
- `artifacts` — submitted work (summary, reports, links)
- `workflow_events` — FSM state transitions
- `checkpoints` — session recovery data

### EPIC-09 (M02) — Context Layer Schema

Six tables serve agent identity and capabilities:

- `roles` — full role markdown document
- `role_schemas` — parsed schema metadata (permissions, persona, limits)
- `runbooks` — role-specific procedures
- `workflows` — workflow definitions (domain-specific)
- `templates` — message/code templates
- `standards` — organizational standards

### Integration Question

**How should these two layers coexist in a single database?**

1. Should there be foreign key relationships between layers?
2. How do cascade deletes work across layers?
3. What happens if a role is deleted while sessions exist?
4. Are there naming conflicts or type incompatibilities?

---

## Problem Statement

### Challenge 1: Architectural Separation

- EPIC-08 (Write) tracks **runtime session data** (agent execution traces)
- EPIC-09 (Context) serves **static reference data** (role definitions, capabilities)
- Mixing read-only reference data with write-heavy runtime data requires careful design

### Challenge 2: ID Type Mismatch

- EPIC-08: TEXT PKs (UUID format for sessions, artifacts)
- EPIC-09: INTEGER PKs (auto-increment for roles, workflows)
- Should these be unified? Or kept separate?

### Challenge 3: Lifecycle Independence

- Sessions are **temporary** (agent executes, session ends → can be archived/deleted)
- Roles are **persistent** (organizational configuration, must survive session deletion)
- Design must prevent orphaned sessions if a role is deleted, OR allow it?

---

## Decision

### **Option A: Loose Coupling (APPROVED for M02)**

**Foreign Key Strategy:**

No foreign key constraints between EPIC-08 write layer and EPIC-09 context layer.

```sql
-- EPIC-08 Write Layer: Independent
CREATE TABLE sessions (
    id           TEXT PRIMARY KEY,
    agent_id     TEXT UNIQUE,
    domain       TEXT,        -- Reference to EPIC-09, but no FK constraint
    role         TEXT,        -- Reference to EPIC-09, but no FK constraint
    started_at   TEXT DEFAULT (datetime('now')),
    last_updated_at TEXT DEFAULT (datetime('now')),
    fsm_state    TEXT DEFAULT 'started',
    outcome      TEXT
);

-- EPIC-09 Context Layer: Independent
CREATE TABLE role_schemas (
    id           INTEGER PRIMARY KEY AUTOINCREMENT,
    domain       TEXT NOT NULL,
    role_name    TEXT NOT NULL,
    mcp_tool_permissions TEXT,
    persona_identity TEXT,
    UNIQUE(domain, role_name)
);
```

**Rationale:**

1. ✅ **Separation of Concerns**
   - Write layer (EPIC-08) operates independently from context layer (EPIC-09)
   - Each layer has its own lifecycle and concerns

2. ✅ **Operational Flexibility**
   - Roles can be updated without affecting active sessions
   - Sessions can reference historical role versions via domain+role names (as strings)
   - No DB-level constraint violations when roles are updated

3. ✅ **Simplicity**
   - Session creation doesn't require pre-checking role existence in DB
   - Middleware (EPIC-11) validates role existence at runtime
   - Easier initial implementation for M02

4. ✅ **Scalability**
   - Write-layer queries (high frequency) isolated from context-layer queries (low frequency)
   - Better query plan optimization
   - Can eventually be sharded separately if needed

---

### **Cascade Delete Strategy**

**Within EPIC-08 Write Layer:**

```sql
CREATE TABLE artifacts (
    id       TEXT PRIMARY KEY,
    session_id TEXT NOT NULL,
    FOREIGN KEY (session_id) REFERENCES sessions(id) ON DELETE CASCADE
);

CREATE TABLE workflow_events (
    id         TEXT PRIMARY KEY,
    session_id TEXT NOT NULL,
    FOREIGN KEY (session_id) REFERENCES sessions(id) ON DELETE CASCADE
);

CREATE TABLE checkpoints (
    id         TEXT PRIMARY KEY,
    session_id TEXT NOT NULL,
    FOREIGN KEY (session_id) REFERENCES sessions(id) ON DELETE CASCADE
);
```

**Within EPIC-09 Context Layer:**

- No cascade deletes between EPIC-09 tables
- Roles, role_schemas, workflows are reference data (rarely deleted)

**Between Layers:**

- ❌ NO cascades between write-layer and context-layer
- Sessions referencing deleted roles remain valid (store role name as string)
- Garbage collection of stale sessions handled separately (admin task)

---

### **ID Strategy**

| Layer | PK Type | Reason |
|:------|:-----:|:--------|
| **EPIC-08 Write** | TEXT (UUID) | Standard for session tracking; allows distributed generation |
| **EPIC-09 Context** | INTEGER (auto-inc) | Simple, fast for reference lookups; no distributed concerns |
| **Junction** | ❌ No direct FK joins | Middleware performs joins on (domain, role_name) strings |

---

## Consequences

### Positive

✅ **Simple implementation** — No complex FK validation at session creation
✅ **Operational flexibility** — Roles can be updated independently
✅ **Read-write separation** — Context layer read-only for agents; write-layer append-only
✅ **Future-proof** — Can evolve toward tight coupling (M03) without breaking M02 code
✅ **Middleware leverage** — RBAC validation logic lives in EPIC-11, not in DB

### Tradeoffs

⚠️ **DB doesn't enforce role existence** — Invalid domain/role pairs can be stored

- Mitigation: EPIC-11 middleware validates at runtime

⚠️ **Manual cleanup needed if role deleted** — Stale sessions remain in DB

- Mitigation: Admin task (non-urgent for M02)

⚠️ **String references instead of FK joins** — Slightly more query inefficiency

- Mitigation: Middleware caches role lookups

---

## Alternatives Considered

### **Option B: Tight Coupling (Rejected for M02, Deferred to M03)**

Foreign key from `sessions(domain, role_name)` → `role_schemas(domain, role_name)`.

**Pros:**

- DB enforces role existence
- Guarantees no orphaned sessions

**Cons:**

- Session creation logic complex (must query role_schemas first)
- Role deletion must handle cascading session cleanup
- Schema migration needed in M03 to transition from Option A

**Decision:** Defer to M03 optimization phase. Option A sufficient for M02.

---

### **Option C: Event Sourcing (Rejected for M02, Future Architecture)**

Store all role versions in event log; compute current state on read.

**Complexity:** Too high for M02. Defer to M03+.

---

## Implementation Plan

### Phase 1: Schema Finalization (TASK-09-01, M02)

1. Confirm no FK relationships created between EPIC-08 + EPIC-09 tables
2. Add indices on (domain, role_name) pairs in context layer
3. Document in TypeScript types: `role` and `domain` are string references, not FKs

### Phase 2: Middleware Validation (EPIC-11, M02)

1. Implement role existence check in bootstrap middleware
2. Cache role lookups (avoid N+1 queries)
3. Return meaningful error if role not found

### Phase 3: Admin Tools (M03, Future)

1. Implement `delete_stale_sessions()` admin task
2. Implement role versioning (`role_version` column)
3. Evaluate tight coupling (FK) as optional optimization

---

## Verification Checklist

- [x] **No table name collisions** between EPIC-08 (sessions, artifacts, workflow_events, checkpoints) and EPIC-09 (roles, role_schemas, runbooks, workflows, templates, standards)
- [x] **ID type compatibility** — TEXT and INTEGER can coexist; no cross-layer joins on PKs
- [x] **Cascade delete safety** — Within-layer cascades work; no between-layer cascades
- [x] **UNIQUE constraint alignment** — UNIQUE(agent_id, started_at) vs. UNIQUE(domain, role_name) serve different purposes
- [x] **Query performance** — Loose coupling allows independent query optimization per layer
- [x] **Migration path** — Can migrate to Option B (tight FK) in M03 without breaking M02 code

---

## Related ADRs

- **ADR-002** (TBD, M03): Tight Foreign Key Coupling (M03 optimization)
- **ADR-003** (TBD, M03): Role Versioning & History

---

## Sign-Off

### Architect Approval

**Name:** Architect Agent
**Decision:** ✅ **APPROVED**
**Effective Date:** 2026-03-05
**Rationale:** Option A provides optimal balance of simplicity, flexibility, and correctness for M02. Loose coupling allows independent evolution of write-layer and context-layer; can migrate to tight coupling in M03 if needed.

### Tech Lead Acknowledgment

**Name:** Tech Lead
**Date:** 2026-03-05
**Comments:** Feasibility reviewed. Implementation straightforward. No blockers.

---

## Document History

| Version | Date | Author | Status | Notes |
|:--------|:-----|:------:|:------:|:------|
| 1.0 | 2026-03-05 | Architect | ✅ Approved | Initial ADR (EPIC-09 TASK-00 outcome) |

---

**ADR Status: ✅ APPROVED — Ready for TASK-09-01 Implementation**
