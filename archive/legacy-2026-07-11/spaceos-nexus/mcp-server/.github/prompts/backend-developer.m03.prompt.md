# Backend Developer Prompt — M03

**Version:** 1.0 (M03 Milestone Edition)
**Extends:** `backend-developer.core.prompt.md`
**Scope:** Milestone 03 (Full Maturity — Multi-Domain & Self-Reflection)
**EPICs:** EPIC-15 through EPIC-18 (Full maturity of DB-first architecture)
**Use with Agent:** `.github/agents/backend_developer.core.agent.md` (substitute `${MILESTONE_ROOT}` = `.../milestone_03/`)

---

## [3. Context: M03 Roadmap]

### Project Status
- **Phase:** Milestone 03 (Full Maturity — Multi-Domain & Self-Reflection)
- **Stack:** TypeScript / Node.js / Express / SQLite / ChromaDB (FROM M02)
- **M01 Status:** ✅ CLOSED_DONE (Write Layer Foundation)
- **M02 Status:** ✅ CLOSED_DONE (Database-First MCP Foundation)
- **M03 Status:** 🔄 IN_PROGRESS (Legacy Modernization + Multi-Domain)

### M03 EPICs (4 total, ~35 tasks — estimate based on pattern)

| EPIC | Name | Focus | Goal |
|:-----|:-----|:------|:-----|
| **EPIC-15** | PM Query Tools | P1 — Essential | Read-only project management state queries (context-server layer) |
| **EPIC-16** | Legacy Tool Refactor | P0 — Foundational | File-based tools → DB-wrapper migration; maintain API compatibility |
| **EPIC-17** | Multi-Domain Configuration | P1 — Important | Domain-agnostic role + knowledge swapping; multi-domain seeding |
| **EPIC-18** | Self-Reflection & Memory Quality | P2 — Future-Focused | Episodic highlights + continuous learning loop |

### M03 Success Criteria (Done-Candidate)
- [ ] `get_project_state(project_id)`, `get_next_tasks(agent_id)` MCP tools live and queryable (EPIC-15)
- [ ] All `get_role`, `get_workflow`, `get_template`, `get_core` tools read from `agent.db` (EPIC-16)
- [ ] Filesystem removal possible — no broken APIs (EPIC-16)
- [ ] Existing E2E tests pass unchanged (EPIC-16)
- [ ] Multi-domain seeder: single branch loads multiple domains (EPIC-17)
- [ ] `generate_episode_highlights()` MCP tool functional (EPIC-18)
- [ ] `reflect_session()` tool generates 3+ `episode_highlight` records (EPIC-18)
- [ ] Knowledge Base (ChromaDB) synced with `agent.db` + episodic memory (EPIC-18)

### Task Assignment Method
1. Read assigned **TASK-XX-YY.md** from `Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_03/epic_XX/tasks/`
2. Review **Acceptance Criteria** (AC) checklist
3. Consult **EPIC-XX/goal.md** for strategic context
4. Reference **EPIC-XX/state.md** for detailed technical specifications
5. **Dependencies:** Tasks depend on M02 completion (M02 EPICs CLOSED_DONE)

---

## [4. Task Implementation Pattern] — M03 Specifics

### Phase 1: Planning (Chain of Thought)
**Before writing code**, document your approach:

```markdown
## Implementation Approach

**Problem:** [Restate task AC from M03 TASK]
**M03 Context:** [How does this task modernize/extend M02? Legacy tool fix? New query? Multi-domain support?]
**Scope:** [Files: src/mcp/, database/, migrations/]
**M02 Dependencies:** [Which M02 EPICs are prerequisites?]
**Strategy:** [High-level steps, migration path for legacy tools, multi-domain implications]
**Key Decisions:** [Why this approach? Legacy compat? DB-first principle?]
**Risks:** [Backward compatibility, migration data loss, performance impact]
```

### Phase 2: Code (M03 Module Structure — Builds on M02)
```
src/
  mcp/
    mcpRouter.ts                 ← [EPIC-15/EPIC-16] PM query tools registration
    DocumentServer.ts            ← [EPIC-16] File-based doc server → DB wrapper
  metadata/
    WorkflowStateTracker.ts       ← [EPIC-17] Domain-agnostic state tracking
  rag/
    VectorStore.ts               ← [EPIC-18] Episode highlights indexing
  roles/
    GuardrailService.ts          ← [EPIC-17] Multi-domain guardrail
database/
  migrations/                    ← [EPIC-16/17] Schema changes (tools table, episodes table)
  knowledge/                     ← [EPIC-17] Domain-specific knowledge bases
  roles/                         ← [EPIC-17] Multi-domain role definitions
  standards/
    adrs/                        ← [EPIC-16] ADR: Legacy → DB migration strategy
```

### Phase 3: Legacy Tool Migration Pattern (EPIC-16)
```typescript
// BEFORE (File-based):
const getRoleFromFile = () => require('database/roles/technical/lead.yml');

// AFTER (DB-first):
const getRoleFromDB = () => db.prepare('SELECT * FROM roles WHERE id = ?').get('technical/lead');

// Wrapper ensures no API breaks:
const getRole = () => getRoleFromDB(); // Falls back to file if DB row missing
```

### Phase 4: Multi-Domain Support Pattern (EPIC-17)
- Domain column in all schema tables: `roles.domain_id`, `episodes.domain_id`, `knowledge.domain_id`
- Seeding: Load multiple domain YAML→ seed single `agent.db` with domain_id prefix
- Query filtering: **All queries must add `WHERE domain_id = ?` filter** (EPIC-17 requirement)

### Phase 5: Episodic Highlights Pattern (EPIC-18)
```typescript
// Episodic memory reflection:
const generateHighlights = () => {
  // 1. Query recent episodes from ChromaDB
  // 2. Identify high-value learnings (success rate, complexity, novel patterns)
  // 3. Create episode_highlight records in SQLite
  // 4. Register highlights in ChromaDB for next session
};
```

---

## [6. M03 Constraints & Guardrails]

### Backward Compatibility (EPIC-16 Mandate)
- **Golden Rule:** No breaking API changes
- All legacy file-based tool calls must continue working
- DB wrappers provide same interface as file readers
- Existing E2E tests must pass unchanged

### Multi-Domain Safety (EPIC-17 Requirement)
- **Domain Isolation:** Every query filters by `WHERE domain_id = ?`
- No cross-domain data leakage
- Role definitions segregated by domain (RBAC still per-domain)
- Seeding verifies domain uniqueness before insert

### Migration Validation (EPIC-16)
- **Before deploying:** Filesystem → DB migration script tested
- No data loss during transition
- Performance regression flag if query > 50ms (same M02 target)
- Rollback procedure documented (if DB fails, file fallback available)

### Episodic Memory Quality (EPIC-18)
- Highlights must meet quality threshold: >70% accuracy, >2 references
- Reflection runs at session end (not in hot path)
- ChromaDB sync checks for conflicts (resolve by latest-write-wins)

---

## [9. M03 Commands & Environment]

### Development

```bash
# Build M03 server (inherits M02 build)
npm run build

# Start server with M03 features
npm run start

# Run M03 tests
npm run test
npm run test:watch
npm run test:e2e

# M03-specific: Verify multi-domain setup
npm run seed:domains                  # Load multi-domain test data
npm run verify:legacy-tools           # Check file→DB wrapper compat
npm run verify:multi-domain           # Validate domain isolation

# M03-specific: Episodic memory
npm run reflect:session               # Generate episode highlights
npm run analyze:memory                # Analyze ChromaDB index health

# Database
sqlite3 agent.db "SELECT domain_id, COUNT(*) FROM roles GROUP BY domain_id;"  # Verify domain data
sqlite3 agent.db "SELECT * FROM episode_highlights LIMIT 10;"                 # View highlights
```

### M03 EPIC Task References

```bash
# View TASK-15-XX, TASK-16-XX, TASK-17-XX, TASK-18-XX files
cat "Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_03/epic_XX/tasks/TASK-XX-YY.md"

# View EPIC state.md for M03 progress
cat "Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_03/epic_XX/state.md"

# View EPIC goal.md for M03 strategic context
cat "Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_03/epic_XX/goal.md"
```

### Key M03 Files

| File | Purpose | EPIC |
|:-----|:--------|:-----|
| `src/mcp/mcpRouter.ts` | PM query tool registration | EPIC-15 |
| `src/mcp/DocumentServer.ts` | Legacy tool wrapper (file → DB) | EPIC-16 |
| `database/migrations/M03_*.sql` | Schema: tools table, episodes, domain columns | EPIC-16/17 |
| `database/knowledge/<domain>/` | Domain-specific knowledge bases | EPIC-17 |
| `src/rag/VectorStore.ts` | Episode highlights indexing (ChromaDB) | EPIC-18 |
| `database/standards/adrs/ADR-XXXX-legacy-migration.md` | Migration strategy & rollback | EPIC-16 |

---

## [M03-Specific: Golden Rules]

1. **Multi-Domain First:** All schema additions must include `domain_id` column (EPIC-17)
2. **Backward Compatibility:** No breaking API changes (EPIC-16)
3. **DB-First Query Pattern:** `SELECT * FROM table WHERE domain_id = ?` (EPIC-17)
4. **File Wrapper Strategy:** Legacy file tools wrapped by DB layer, not removed (EPIC-16)
5. **Episodic Reflection:** Non-blocking async at session end (EPIC-18)
6. **ChromaDB Sync:** Version all highlights; resolve conflicts by latest-write-wins (EPIC-18)
7. **Migration Testing:** Before deploying, verify file→DB transition data integrity (EPIC-16)
8. **Query Performance:** Legacy tool wrappers must maintain M02 latency targets (< 50ms)
9. **Domain Isolation:** Zero cross-domain leakage in queries; validate in E2E tests (EPIC-17)
10. **Implementation Summary:** Mandatory per task before COMPLETED state (M02 carryover)

---

## [Quick Start: M03 Task Execution]

### Pick Your TASK

📍 **Location:** `Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_03/epic_XX/tasks/TASK-XX-YY.md`

**Recommended Start:** `EPIC-16` (Legacy tool refactor unblocks everything else)

### Workflow

1. **Read AC** (Acceptance Criteria from TASK file)
2. **Check M02 Dependency** (Ensure M02 EPICs CLOSED_DONE)
3. **Plan Approach** (Chain of Thought, 30 min)
4. **Implement + Test** (80%+ coverage, 4-8 hours)
5. **Verify Backward Compat** (Run legacy tool tests, verify E2E unchanged)
6. **Create Summary** (Implementation Summary, 30 min)
7. **Get Review** (Tech Lead peer review, 1-2 hours)
8. **Merge & Update** (state.md + task status)

### Dependency Chain (Recommended Order)

```
[M02 Complete] ✅
  ↓
EPIC-16: Legacy Tool Refactor (8 tasks) — BLOCKS everything
  ↓
[Parallel]
  EPIC-15: PM Query Tools (8 tasks)
  EPIC-17: Multi-Domain Configuration (8 tasks)
  ↓
EPIC-18: Self-Reflection & Memory (10 tasks)
```

---

## [Integration with Core Prompt]

This M03 prompt **extends** `backend-developer.core.prompt.md`:

- ✅ Reuse: Patterns (Refusal, Chain of Thought, Fact Summary)
- ✅ Reuse: Code conventions (TypeScript strictness, error handling)
- ✅ Reuse: Testing standards (80%+ coverage, Jest + Playwright)
- ✅ Reuse: Documentation (Implementation Summary template)
- ✅ Reuse: Database fundamentals from M02
- 🆕 M03-Specific: Multi-domain queries, legacy tool migration, episodic reflection

---

## [M02 → M03 Continuity]

**What M03 assumes M02 provides:**
- ✅ `agent.db` running with SSOT role data
- ✅ RBAC filter enforced on all tools
- ✅ SQLite schema with role, permission, workflow tables
- ✅ ChromaDB episodic memory operational
- ✅ bootstrap_agent() aggregate tool functional
- ✅ Context middleware + error standardization in place

**What M03 adds on top:**
- 🆕 `domain_id` column: extend all M02 tables (roles, permissions, workflows, knowledge)
- 🆕 Legacy tools: file→DB wrapper layer (no API breaks)
- 🆕 PM query tools: read-only project state queries
- 🆕 Episodic highlights: session reflection + continuous learning

---

**Ready to start M03 development! 🚀**

For generic guidance, reference `backend-developer.core.prompt.md`.
For M02 database patterns, review `backend-developer.m02.prompt.md`.
For M03-specific multi-domain & legacy migration details, use this prompt.
