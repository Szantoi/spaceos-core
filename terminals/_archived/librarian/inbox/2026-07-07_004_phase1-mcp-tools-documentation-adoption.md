---
id: MSG-LIBRARIAN-004
from: root
to: librarian
type: task
priority: medium
status: READ
model: sonnet
ref: MSG-ROOT-014, MSG-BACKEND-173, MSG-ARCHITECT-068
created: 2026-07-07
estimated_nwt: 60
content_hash: 9c8640414f579ae1b48e10881efaa75f15279483761e0331b8fc9b2529a4f849
---

# Phase 1 MCP Tools — Documentation & Adoption Tracking

Backend implements 5 Phase 1 MCP tools (MSG-BACKEND-173). Architect reviews specs (MSG-ARCHITECT-068). **Your role:** Documentation + adoption tracking to ensure tools are discoverable and used.

---

## 5 Tools to Document

1. **Terminal Status Aggregator** (Conductor) — Multi-terminal status query
2. **Dependency Resolver** (Conductor) — Epic/task dependency analysis
3. **Session Context Transfer** (Explorer/Librarian) — Cross-terminal context passing
4. **Component Scaffold** (Frontend) — React hook/component code generation
5. **Domain Pattern Matcher** (Architect) — Pattern search + recommendations

---

## Deliverables

### 1. MCP Tools Catalogue Update

**File:** `docs/knowledge/patterns/MCP_TOOLS_CATALOGUE.md`

**Structure:**
```markdown
# MCP Tools Catalogue — SpaceOS Knowledge Service

## Context Persistence (Implemented 2026-07-07)

### build_session_start_context
**Purpose:** Load session state + STATUS.md + turn count for goal re-anchoring
**Usage:** Session start (first 3 min)
**Input:** terminal: string
**Output:** { statusMd, sessionState, turnCount, saturation }
**Example:** [show code snippet]
**Terminal:** ALL

### get_context_saturation
**Purpose:** Check turn count + threshold status (WARNING/CRITICAL)
**Usage:** Every 10-15 turns
**Input:** terminal: string
**Output:** { turnCount, status: "ok"|"warning"|"critical", thresholds }
**Example:** [show code snippet]
**Terminal:** ALL

... (13 tools total)

---

## Phase 1 Infrastructure Tools (Implemented 2026-07-07)

### get_terminal_status_aggregate
**Purpose:** Query all terminal status (working/idle/blocked) + context saturation
**Usage:** Conductor daily check, Root/Monitor diagnostic
**Input:** format: "summary"|"detailed"|"alerts_only"
**Output:** { summary: {...}, terminals: [...] }
**Example:** [show code snippet]
**Terminal:** Conductor, Root, Monitor

### resolve_dependencies
**Purpose:** Analyze epic/task dependencies, find blockers
**Usage:** Before task dispatch, epic planning
**Input:** epic_id: string, check_blockers: bool
**Output:** { blockedBy, blocks, parallelWith, readyTasks, blockedTasks }
**Example:** [show code snippet]
**Terminal:** Conductor

... (5 tools total)
```

**For each tool, document:**
- **Purpose** — One sentence: what does it do?
- **Usage** — When should terminals use it?
- **Input** — Parameter schema (TypeScript interface)
- **Output** — Return value schema (TypeScript interface)
- **Example** — Code snippet showing typical usage
- **Terminal** — Which terminals should use this tool?

---

### 2. Terminal CLAUDE.md Updates

**Add MCP tool reference section to relevant terminals:**

**Conductor CLAUDE.md:**
```markdown
## MCP TOOLS — CONDUCTOR WORKFLOW

### Daily Status Check
mcp__spaceos-knowledge__get_terminal_status_aggregate
  format: "summary"

### Epic Planning
mcp__spaceos-knowledge__resolve_dependencies
  epic_id: "EPIC-CUTTING-Q3"
  check_blockers: true

### Session Context Transfer (from Explorer)
mcp__spaceos-knowledge__transfer_session_context
  from_terminal: "explorer"
  to_terminal: "librarian"
  context_type: "research_summary"
```

**Frontend CLAUDE.md:**
```markdown
## MCP TOOLS — FRONTEND WORKFLOW

### Component Scaffolding
mcp__spaceos-knowledge__scaffold_component
  component_type: "react_hook"
  name: "useCostBudget"
  api_spec: "openapi.yaml#/components/schemas/CostBudget"
  output_dir: "client/src/hooks/"
```

**Architect CLAUDE.md:**
```markdown
## MCP TOOLS — ARCHITECT WORKFLOW

### Domain Pattern Matching
mcp__spaceos-knowledge__match_domain_pattern
  description: "Track cost breakdown by project phase"
  domain: "kontrolling"
```

**Updates for:**
- Conductor CLAUDE.md (Terminal Status Aggregator, Dependency Resolver, Session Context Transfer)
- Frontend CLAUDE.md (Component Scaffold)
- Architect CLAUDE.md (Domain Pattern Matcher)
- Explorer CLAUDE.md (Session Context Transfer)
- Librarian CLAUDE.md (Session Context Transfer)

---

### 3. Adoption Tracking System

**File:** `docs/knowledge/patterns/MCP_TOOL_ADOPTION.md`

**Track:**
- Which tool is used by which terminal?
- How often is each tool called? (daily/weekly/monthly)
- What's the success rate? (errors vs successful calls)
- Are tools delivering expected ROI?

**Structure:**
```markdown
# MCP Tool Adoption Tracking

## Week 1 (2026-07-07 → 2026-07-14)

### Terminal Status Aggregator
- **Calls:** 42
- **Terminals:** conductor (35), root (5), monitor (2)
- **Success Rate:** 100%
- **Average Response Time:** 120ms
- **ROI:** Saved ~15min/day × 7 days = 105 minutes ✅

### Dependency Resolver
- **Calls:** 18
- **Terminals:** conductor (18)
- **Success Rate:** 94% (1 error: circular dependency detected)
- **Average Response Time:** 85ms
- **ROI:** Saved ~25min/dispatch × 3 dispatches = 75 minutes ✅

... (track all 5 tools)

## Issues & Feedback

- Domain Pattern Matcher: Response time >200ms (vector search slow) — Backend investigating caching
- Component Scaffold: Template path error 2×  — Backend fixed in commit abc123
```

**Data sources:**
- Knowledge Service logs: `/opt/spaceos/logs/sessions/mcp-*.log`
- Terminal outbox DONE messages: count tool usage mentions
- Manual survey: Ask terminals in week 2 "Did you use tool X? Was it helpful?"

---

### 4. Usage Examples & Tutorials

**File:** `docs/knowledge/patterns/MCP_TOOLS_EXAMPLES.md`

**Real-world scenarios:**

**Scenario 1: Conductor Daily Routine**
```markdown
## Morning Status Check

1. Check terminal status:
   mcp__spaceos-knowledge__get_terminal_status_aggregate
     format: "alerts_only"

2. If alerts (e.g., "conductor: turn count >30"):
   - Read session state to re-anchor goal
   - Reset turn count if needed

3. Check epic dependencies:
   mcp__spaceos-knowledge__resolve_dependencies
     epic_id: "EPIC-CUTTING-Q3"
     check_blockers: true

4. Dispatch ready tasks to terminals
```

**Scenario 2: Frontend Component Development**
```markdown
## React Hook Generation from OpenAPI

1. Scaffold hook:
   mcp__spaceos-knowledge__scaffold_component
     component_type: "react_hook"
     name: "useCostBudget"
     api_spec: "openapi.yaml#/components/schemas/CostBudget"
     output_dir: "client/src/hooks/"

2. Review generated files:
   - client/src/hooks/useCostBudget.ts
   - client/src/hooks/__tests__/useCostBudget.test.ts

3. Add business logic, run tests
```

**Scenario 3: Architect Pattern Recommendation**
```markdown
## Find Existing Pattern for New Feature

1. Match pattern:
   mcp__spaceos-knowledge__match_domain_pattern
     description: "Track cost breakdown by project phase"
     domain: "kontrolling"

2. Review recommendations:
   - Use EACCalculationWidget pattern
   - Integrate with KPI Strip
   - Follow dark-first bento layout

3. Reference example code & ADRs
```

---

## Timeline

**Target:** 2 hours (60 NWT)
- Hour 1: MCP Tools Catalogue update (all 18 tools: 13 context persistence + 5 phase 1)
- Hour 2: Terminal CLAUDE.md updates, adoption tracking setup, examples

---

## Acceptance Criteria

- [ ] MCP Tools Catalogue updated with all 18 tools
- [ ] 5 terminal CLAUDE.md files updated with relevant tool sections
- [ ] Adoption tracking system initialized (template ready)
- [ ] Usage examples documented (3+ scenarios)
- [ ] Terminals can discover and use tools easily

---

## References

- **Backend Implementation:** `terminals/backend/inbox/2026-07-07_173_phase1-mcp-tools-implementation.md`
- **Architect Review:** `terminals/architect/inbox/2026-07-07_068_phase1-mcp-tool-spec-review.md`
- **Context Persistence Implementation:** `spaceos-nexus/knowledge-service/src/contextPersistence.ts` (reference)
- **Explorer Research:** `terminals/explorer/outbox/2026-07-07_008_*` and `..._009_*`

---

## Success Criteria

- [ ] Documentation complete
- [ ] Terminals use tools in real workflows (Week 2 verification)
- [ ] Adoption tracking shows ROI (15+ hours saved Week 1)
