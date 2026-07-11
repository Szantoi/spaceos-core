# Backend Developer Prompt — M02-M03

**Version:** 2.0 (M02-M03 Milestone Edition)
**Extends:** `backend-developer.core.prompt.md`
**Scope:** Milestone 02-03 (Database-First MCP Foundation)
**EPICs:** EPIC-09 through EPIC-14 (51 concrete tasks)
**Use with Agent:** `.github/agents/backend_developer.core.agent.md` (substitute `${MILESTONE_ROOT}` = `.../milestone_02/`)

---

## [3. Context: M02-M03 Roadmap]

### Project Status
- **Phase:** Milestone 02-03 (Database-First MCP Foundation)
- **Stack:** TypeScript / Node.js / Express / SQLite / ChromaDB
- **M01 Status:** ✅ CLOSED_DONE (Write Layer Foundation)
- **M02 Status:** 🔄 IN_PROGRESS (51 TASK files generated)
- **M03 Status:** 🔮 DEFERRED (task planning for later sprint)

### M02 EPICs (6 total, 51 tasks)
| EPIC | Name | Tasks | Goal |
|:-----|:-----|:-----:|:-----|
| **EPIC-09** | SQLite Foundation | 8 | Schema, seeding, SSOT database |
| **EPIC-10** | bootstrap_agent() | 8 | Session initialization aggregate tool |
| **EPIC-11** | Context Middleware | 8 | Request processing, RBAC, error standardization |
| **EPIC-12** | Episodic Memory | 8 | FTS5 + ChromaDB for experience search |
| **EPIC-13** | Discovery Track | 7 | DWI tools for exploration workflows |
| **EPIC-14** | Modern Transports | 12 | Transport abstraction + plugin architecture |

### Task Assignment Method
1. Read assigned **TASK-XX-YY.md** from `Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_02/epic_XX/tasks/`
2. Review **Acceptance Criteria** (AC) checklist
3. Consult **EPIC-XX/goal.md** for strategic context
4. Reference **EPIC-XX/state.md** for detailed technical specifications

---

## [4. Task Implementation Pattern] — M02 Specifics

### Phase 1: Planning (Chain of Thought)
**Before writing code**, document your approach:

```markdown
## Implementation Approach

**Problem:** [Restate task AC from M02 TASK]
**Scope:** [Files: src/mcp/, src/metadata/, src/rag/, src/roles/, database/]
**Dependencies:** [Upstream EPICs (e.g., EPIC-09 → EPIC-10 → EPIC-11)]
**Strategy:** [High-level steps, integration with MCP protocol]
**Key Decisions:** [Why X pattern for M02?]
**Risks:** [Performance targets, concurrency, schema compatibility]
```

### Phase 2: Code (M02 Module Structure)
```
src/
  mcp/             ← MCP routing & RBAC (EPIC-11)
  metadata/        ← SQLite context schema (EPIC-09)
  rag/             ← Episodic memory storage (EPIC-12)
  roles/           ← Guardrail service (EPIC-11/13)
  tests/           ← Jest + Playwright tests
    unit/
    e2e/
database/
  standards/       ← Architecture decision records
  knowledge/       ← Domain knowledge (backend specifics)
  roles/           ← Role definitions (YAML seeding)
```

### Phase 3: M02 Integration Points
- **EPIC-09 ← EPIC-10 ← EPIC-11 ← [EPIC-12, EPIC-13, EPIC-14]**
- Each task verifies upstream dependencies before proceeding
- All schema changes reconciled with SQLite PRAGMA configuration

---

## [6. M02 Constraints & Testing]

### Database-First Mandate (EPIC-09 Foundation)
- All agent context stored in SQLite (`agent.db`)
- No hardcoded role data in TypeScript → read from `database/roles/`
- Schema validation: FK constraints enabled (`PRAGMA foreign_keys = ON`)
- Performance target: bootstrap query < 50ms

### Testing Scale (M02)
- **Unit tests:** 80%+ coverage (Jest)
- **Integration tests:** Real SQLite, schema integrity validation
- **E2E tests:** MCP protocol end-to-end flows (Playwright)
- **Performance:** Latency benchmarks (bootstrap, episode search, etc.)

### Security Requirements (EPIC-09, EPIC-11)
- Parameterized SQLite queries (better-sqlite3)
- RBAC Filter enforced on all tools (two-track: business/technical)
- Audit logging on all state changes
- Error messages sanitized (no stack traces exposed)

---

## [9. M02 Commands & Environment]

### Development
```bash
# Build M02 MCP server
npm run build

# Start server (stdio transport)
npm run start

# Run M02 tests
npm run test
npm run test:watch
npm run test:e2e

# Verify database
sqlite3 agent.db ".schema"        # View schema
sqlite3 agent.db ".indices"       # View indexes
npm run seed:roles                # Populate database

# Check linting & format
npm run lint
npm run format
```

### M02 EPIC Task References
```bash
# View TASK-XX-YY (e.g., TASK-09-01, TASK-14-12)
cat "Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_02/epic_XX/tasks/TASK-XX-YY.md"

# View EPIC state.md for technical details
cat "Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_02/epic_XX/state.md"

# View EPIC goal.md for strategic context
cat "Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_02/epic_XX/goal.md"
```

### Key M02 Files
| File | Purpose |
|:-----|:--------|
| `src/metadata/WorkflowStateTracker.ts` | SQLite FSM state tracking |
| `src/mcp/RbacFilter.ts` | Two-track RBAC enforcement |
| `src/rag/VectorStore.ts` | ChromaDB episodic memory |
| `database/standards/adrs/ADR-XXXX-sqlite-schema.md` | Schema design decisions |

---

## [M02-Specific: Golden Rules]

1. **SQLite-First:** All agent context lives in `agent.db` (SSOT)
2. **No Hardcoding:** Role data from `database/roles/` (never in TypeScript)
3. **RBAC Mandatory:** All tools gated by role-based access (EPIC-11)
4. **Schema Reconciliation:** Cross-EPIC schema conflicts escalate to Architect
5. **Performance Targets:** Bootstrap < 50ms, search_experience < 500ms
6. **Audit Everything:** State changes logged, errors captured
7. **FK Constraints:** Enabled by default (`PRAGMA foreign_keys = ON`)
8. **WAL Mode:** SQLite write concurrency (`PRAGMA journal_mode = WAL`)
9. **Two-Track:** Business vs. Technical role separation (EPIC-13)
10. **Implementation Summary:** Mandatory per task before mark COMPLETED

---

## [Quick Start: M02 Task Execution]

### Pick Your TASK
📍 **Location:** `Docs/mcp-context-server/delivery/mcp-maintenance/milestones/milestone_02/epic_XX/tasks/TASK-XX-YY.md`

**Recommended Start:** `EPIC-09` (database foundation prerequisite)

### Workflow
1. **Read AC** (Acceptance Criteria from TASK file)
2. **Plan approach** (Chain of Thought, 30 min)
3. **Implement + Test** (80%+ coverage, 4-8 hours)
4. **Create Summary** (Implementation Summary, 30 min)
5. **Get Review** (Tech Lead peer review, 1-2 hours)
6. **Merge & Update** (state.md + task status)

### Dependency Chain (Recommended Order)
```
EPIC-09: SQLite Foundation (8 tasks)
  ↓
EPIC-10: bootstrap_agent() (8 tasks)
  ↓
EPIC-11: Context Middleware (8 tasks)
  ↓
[Parallel]
  EPIC-12: Episodic Memory (8 tasks)
  EPIC-13: Discovery Track (7 tasks)
  ↓
EPIC-14: Modern Transports (12 tasks)
```

---

## [Integration with Core Prompt]

This M02 prompt **extends** `backend-developer.core.prompt.md`:
- ✅ Reuse: Patterns (Refusal, Chain of Thought, Fact Summary)
- ✅ Reuse: Code conventions (TypeScript strictness, error handling)
- ✅ Reuse: Testing standards (80%+ coverage, Jest + Playwright)
- ✅ Reuse: Documentation (Implementation Summary template)
- 🆕 M02-Specific: EPIC structure, database-first mandate, RBAC enforcement

---

**Ready to start M02 development! 🚀**

For generic guidance, reference `backend-developer.core.prompt.md`.
For M02-M03 specifics, use this prompt.
