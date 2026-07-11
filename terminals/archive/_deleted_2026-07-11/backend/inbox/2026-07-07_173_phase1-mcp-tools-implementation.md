---
id: MSG-BACKEND-173
from: root
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-ROOT-014
created: 2026-07-07
estimated_nwt: 240
content_hash: 4461221502cce3a24ed087b62695ac793a3c01f2b02883a8d32332650ba4ab9b
---

# Phase 1 MCP Tools Implementation (Infrastructure Initiative)

Explorer audit (MSG-ROOT-014) identified **5 critical MCP tools** needed for terminal acceleration. Root approved **Phase 1 Quick Wins** (Week 1-2) implementation.

## 5 Critical Tools — Phase 1

### 1. Terminal Status Aggregator (Conductor)
**ROI:** Eliminates 15min daily checks

```typescript
mcp__spaceos-knowledge__get_terminal_status_aggregate
  format: "summary" | "detailed" | "alerts_only"
```

**Output:**
```json
{
  "summary": {
    "working": ["conductor", "backend"],
    "idle": ["frontend", "architect"],
    "blocked": ["librarian"],
    "warnings": ["conductor: turn count >30"]
  },
  "terminals": [
    {
      "terminal": "conductor",
      "status": "working",
      "currentTask": "MSG-CONDUCTOR-065",
      "turnCount": 35,
      "saturation": "warning",
      "hasUnreadInbox": false,
      "lastActive": "2026-07-07T10:30:00Z"
    }
  ]
}
```

**Implementation:**
- File: `src/pipeline/terminalStatusAggregator.ts`
- Integration: `src/mcp.ts` (tool definition + handler)
- Depends on: existing `get_terminal_status`, `get_context_saturation`

---

### 2. Dependency Resolver (Conductor)
**ROI:** Saves 20-30min per phase

```typescript
mcp__spaceos-knowledge__resolve_dependencies
  epic_id: "EPIC-CUTTING-Q3"
  check_blockers: true
```

**Output:**
```json
{
  "epic": "EPIC-CUTTING-Q3",
  "status": "active",
  "blockedBy": [],
  "blocks": ["EPIC-PORTAL-V2"],
  "parallelWith": ["EPIC-JOINERY-V2"],
  "readyTasks": [
    {"id": "MSG-BACKEND-045", "terminal": "backend", "dependencies": []}
  ],
  "blockedTasks": [
    {"id": "MSG-FRONTEND-066", "terminal": "frontend", "blockedBy": ["MSG-BACKEND-045"]}
  ]
}
```

**Implementation:**
- File: `src/pipeline/dependencyResolver.ts`
- Integration: `src/mcp.ts`
- Depends on: `EPICS.yaml` parser, `TASKS.yaml` parser

---

### 3. Session Context Transfer (Explorer)
**ROI:** 30min per session

```typescript
mcp__spaceos-knowledge__transfer_session_context
  from_terminal: "explorer"
  to_terminal: "librarian"
  context_type: "research_summary" | "code_audit" | "knowledge_synthesis"
  include_files: ["file1.md", "file2.ts"]
```

**Output:**
```json
{
  "success": true,
  "message_id": "MSG-LIBRARIAN-004",
  "summary": "Transferred 3 research findings + 5 code patterns",
  "inbox_file": "terminals/librarian/inbox/2026-07-07_004_context-transfer-explorer.md"
}
```

**Implementation:**
- File: `src/pipeline/sessionContextTransfer.ts`
- Integration: `src/mcp.ts`
- Depends on: `create_task`, `read_terminal_status_md`

---

### 4. Component Scaffold (Frontend)
**ROI:** 2-3 hours/week

```typescript
mcp__spaceos-knowledge__scaffold_component
  component_type: "react_hook" | "react_component" | "api_client"
  name: "useCostBudget"
  api_spec: "openapi.yaml#/components/schemas/CostBudget"
  output_dir: "client/src/hooks/"
```

**Output:**
```json
{
  "success": true,
  "files_created": [
    "client/src/hooks/useCostBudget.ts",
    "client/src/hooks/__tests__/useCostBudget.test.ts"
  ],
  "next_steps": [
    "Review generated hook",
    "Add business logic",
    "Run tests: npm test useCostBudget"
  ]
}
```

**Implementation:**
- File: `src/generators/componentScaffold.ts`
- Templates: `src/generators/templates/react/`
- Integration: `src/mcp.ts`
- Depends on: OpenAPI parser, file writer

---

### 5. Domain Pattern Matcher (Architect)
**ROI:** 2-3 hours/week

```typescript
mcp__spaceos-knowledge__match_domain_pattern
  description: "Track cost breakdown by project phase"
  domain: "kontrolling" | "crm" | "procurement" | "ehs"
```

**Output:**
```json
{
  "pattern": "Cost Breakdown Widget",
  "confidence": 0.92,
  "references": [
    "docs/knowledge/patterns/KONTROLLING_PATTERNS.md",
    "datahaven-web/client/src/components/CostBreakdownChart.tsx"
  ],
  "recommendations": [
    "Use EACCalculationWidget pattern",
    "Integrate with KPI Strip",
    "Follow dark-first bento layout"
  ],
  "example_code": "...",
  "adr_refs": ["ADR-054", "ADR-055"]
}
```

**Implementation:**
- File: `src/pipeline/domainPatternMatcher.ts`
- Integration: `src/mcp.ts`
- Depends on: vector search (ChromaDB), knowledge base indexing

---

## Acceptance Criteria

- [ ] All 5 tools implemented with TypeScript + MCP registration
- [ ] Unit tests written (>90% coverage)
- [ ] Integration tests with mock data
- [ ] Tool documentation added to `docs/knowledge/patterns/MCP_TOOLS_CATALOGUE.md`
- [ ] Conductor/Frontend/Architect successfully use tools in real workflow
- [ ] Performance metrics: <200ms response time for each tool
- [ ] Error handling: graceful degradation if dependencies unavailable

---

## Implementation Order

**Week 1 (Day 1-3):**
1. Terminal Status Aggregator (easiest, immediate value)
2. Dependency Resolver (medium, high impact)
3. Session Context Transfer (medium, workflow improvement)

**Week 2 (Day 4-7):**
4. Component Scaffold (harder, requires templates)
5. Domain Pattern Matcher (hardest, requires vector search)

---

## Technical Notes

**Shared Dependencies:**
- All tools use existing `src/mcp.ts` registration pattern
- Error handling: return `{success: false, error: "..."}` on failure
- Logging: use `log()` from `src/pipeline/common.ts`
- Auth: MCP bearer token (already implemented)

**Testing Strategy:**
- Unit tests: `src/__tests__/unit/`
- Integration tests: `src/__tests__/integration/`
- Manual testing: Conductor/Frontend/Architect real usage

**Deployment:**
- Build: `npm run build`
- Restart service: `fuser -k 3456/tcp && nohup node dist/server.js &`
- Verify: `curl http://localhost:3456/health`

---

## References

- **Explorer Research:** `terminals/explorer/outbox/2026-07-07_008_task-research-skills-scripts-ideas.md`
- **Tool Gap Analysis:** `terminals/explorer/outbox/2026-07-07_009_terminal-tools-gap-analysis-mcp-infra.md`
- **Context Persistence Implementation:** `spaceos-nexus/knowledge-service/src/contextPersistence.ts` (reference pattern)

---

## Next Steps After Phase 1

**Phase 2 (Week 3-5):** Advanced tools (Skill Factory, Code Generator Suite)
**Phase 3 (Week 6-8):** Coordination tools (Parallel Task Dispatch, Epic Progress Tracker)

**Expected Total Impact:** 40-50 hours/week saved across all terminals.
