---
id: MSG-BACKEND-021
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-ARCHITECT-005-DONE
created: 2026-06-22
content_hash: b0c6c5950f1331a690f7afe675b544f92b5e415a36a257055c86db5c48d8016a
---

# Graph Workflow Phase 1 — EPICS.yaml + Mermaid MVP

## Context

Architect MSG-005-DONE deliverables alapján implement Phase 1 of the Graph-Based Workflow & Project Management Architecture.

**ADR:** `docs/architecture/decisions/ADR-041-graph-based-workflow-architecture.md`
**Phase 1 Plan:** `docs/tasks/new/SpaceOS_Graph_Workflow_Phase1_Architecture_v1.md`
**Timeframe:** 3-4 dev days

---

## Scope

Implement 6 tasks sequentially (dependency chain):

### TASK-001: EPICS.yaml Validator
- Create `src/pipeline/epicsValidator.ts`
- Schema validation (version, epics array)
- Reference integrity (depends_on → existing epic)
- Cycle detection (DAG validation)
- Unit tests (80%+ coverage)

**AC:**
- `validateEpicsYaml(yaml)` function works
- Invalid EPICS.yaml → validation errors
- Cycle → error with cycle path
- Test coverage 80%+

**Ref:** `src/pipeline/yamlValidator.ts` (pattern), `src/graph/operations.ts` (detectCycles)

---

### TASK-002: EPICS Loader & Graph Builder
- Create `src/graph/epicsLoader.ts`
- `loadEpicsYaml(path)` — file read + parse
- `buildEpicGraph(epics)` — EpicDependency[] → WorkflowGraph
- Auto-generate `triggers` from `depends_on` inverse
- Integration test

**AC:**
- EPICS.yaml → WorkflowGraph conversion works
- `computeGraphProperties()` executed
- Cached loading (optional: file watcher)

**Blocked by:** TASK-001

---

### TASK-003: Graph API Routes
- Create `src/api/graphRoutes.ts`
- Implement 5 endpoints:
  1. `GET /api/graph/epics` — all epics + dependency graph
  2. `GET /api/graph/project/:slug` — project-level graph
  3. `POST /api/graph/validate` — YAML validation endpoint
  4. `GET /api/graph/critical-path/:type/:id` — critical path query
  5. `GET /api/graph/mermaid/:type/:id` — Mermaid diagram generation

**AC:**
- All 5 endpoints implemented
- Response schemas documented
- Error handling (404, 400, 500)
- Integration tests

**Blocked by:** TASK-002

---

### TASK-004: STATUS.md Auto-Update Integration
- Extend `src/pipeline/statusUpdater.ts` (or create new module)
- Read EPICS.yaml graph
- Compute epic-level completion (% tasks done)
- Update `docs/Codebase_Status.md` with graph-based epic status
- Add Mermaid diagram embed option

**AC:**
- STATUS.md reflects epic graph state
- Auto-update on task completion
- Mermaid diagram generation working

**Blocked by:** TASK-003

---

### TASK-005: EPICS.yaml Real Data Migration
- Migrate current epic list to EPICS.yaml format
- 9 epics from SpaceOS: Kernel, Joinery, Cutting, Identity, Inventory, Procurement, Sales, Infra, E2E
- Populate `depends_on`, `parallel_with`, `status`, `tasks_yaml` fields
- Validate EPICS.yaml (no cycles, valid references)

**Data source:** `docs/Codebase_Status.md` (current epic status)

**AC:**
- EPICS.yaml contains 9 SpaceOS epics
- All dependency edges defined
- Validation passes
- Graph renders in Mermaid

**Blocked by:** TASK-004

---

### TASK-006: E2E Smoke Test + Documentation
- E2E test scenario:
  1. Load EPICS.yaml
  2. Query /api/graph/epics
  3. Generate Mermaid diagram
  4. Validate critical path computation
- Update `docs/knowledge/architecture/ADR_CATALOGUE.md`
- Create `docs/knowledge/graph/GRAPH_WORKFLOW_USAGE.md` guide

**AC:**
- E2E test passes
- Documentation complete
- Usage examples provided

**Blocked by:** TASK-005

---

## Definition of Done (Cumulative)

Phase 1 is complete when:

- [x] TASK-001: EPICS.yaml validator implemented + tests
- [x] TASK-002: EPICS loader + graph builder working
- [x] TASK-003: 5 API routes implemented + tests
- [x] TASK-004: STATUS.md auto-update integration
- [x] TASK-005: EPICS.yaml migrated (9 SpaceOS epics)
- [x] TASK-006: E2E test + documentation complete
- [x] Build passes (0 TypeScript errors)
- [x] Test suite passes (964+ tests)
- [x] Knowledge service restart successful
- [x] Mermaid diagram renders correctly

---

## Deliverables

**Code:**
- `spaceos-nexus/knowledge-service/src/pipeline/epicsValidator.ts`
- `spaceos-nexus/knowledge-service/src/graph/epicsLoader.ts`
- `spaceos-nexus/knowledge-service/src/api/graphRoutes.ts`
- `spaceos-nexus/knowledge-service/src/pipeline/statusUpdater.ts` (extended)
- `spaceos-nexus/knowledge-service/src/__tests__/e2e/graph.test.ts`

**Data:**
- `docs/projects/EPICS.yaml` (migrated, 9 epics)

**Docs:**
- `docs/knowledge/graph/GRAPH_WORKFLOW_USAGE.md`
- `docs/knowledge/architecture/ADR_CATALOGUE.md` (updated)

**Artifacts:**
- Mermaid diagram of SpaceOS epic dependency graph
- STATUS.md with graph-based epic status

---

## Implementation Notes

### Pre-existing Artifacts (from Architect)

Already available (use these as reference):
- `docs/architecture/decisions/ADR-041-graph-based-workflow-architecture.md`
- `docs/projects/EPICS.yaml` (template)
- `src/graph/types.ts`
- `src/graph/operations.ts`
- `src/graph/mermaidGenerator.ts`
- `src/graph/index.ts`

**Note:** The `src/graph/` TypeScript interfaces are already implemented by Architect. Use them directly.

### Sequential Execution

Tasks have dependency chain:
```
TASK-001 → TASK-002 → TASK-003 → TASK-004 → TASK-005 → TASK-006
```

Implement sequentially, validate each task before moving to next.

### Testing Strategy

- Unit tests: Each module (validator, loader, routes)
- Integration tests: API endpoints + EPICS.yaml loading
- E2E test: Full workflow (load → query → render Mermaid)
- Regression: Existing 964 tests must pass

### Build & Restart

After Phase 1 completion:
```bash
cd spaceos-nexus/knowledge-service
npm run build
npm run test
pm2 restart knowledge-service
```

---

## Risk Mitigation

**Risk 1:** EPICS.yaml schema changes during implementation
- Mitigation: Lock schema in TASK-001, version field tracks changes

**Risk 2:** Mermaid diagram too large for 9 epics
- Mitigation: Subgraph grouping, collapsible nodes in Phase 2

**Risk 3:** Performance (graph computation on every request)
- Mitigation: In-memory cache, file watcher reloads on EPICS.yaml change

---

## Next Steps (Phase 2 — Future)

Phase 2 will add React Flow visualization + Dispatcher integration (auto-dispatch based on graph). This Phase 1 is the foundation.

**Phase 2 triggers:**
- React Flow component (Frontend)
- Dispatcher auto-dispatch using graph critical path
- Real-time graph updates (WebSocket)

---

## Questions / Blockers

If blocked, send BLOCKED outbox message to Conductor with details.

---

**Expected completion:** 2026-06-25 (3-4 dev days)
**Next inbox after DONE:** Phase 2 planning or new consensus from queue
