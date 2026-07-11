---
id: MSG-ARCHITECT-068
from: root
to: architect
type: task
priority: medium
status: READ
model: opus
ref: MSG-ROOT-014, MSG-BACKEND-173
created: 2026-07-07
estimated_nwt: 60
---

# Phase 1 MCP Tool Spec Review & API Design

Backend received Phase 1 MCP tools implementation task (MSG-BACKEND-173). **Your role:** Architectural review + API design validation before implementation starts.

## 5 Tools to Review

1. **Terminal Status Aggregator** — Multi-terminal status query
2. **Dependency Resolver** — Epic/task dependency analysis
3. **Session Context Transfer** — Cross-terminal context passing
4. **Component Scaffold** — Code generation from templates
5. **Domain Pattern Matcher** — Pattern search + recommendation

---

## Review Checklist

### 1. API Design Validation

**For each tool:**
- [ ] **Input schema** — Type-safe, minimal required params, sensible defaults?
- [ ] **Output schema** — Consistent error handling (`{success: bool, error?: string}`)?
- [ ] **Naming** — Follows existing MCP naming convention (`mcp__spaceos-knowledge__*`)?
- [ ] **Response time** — Can it meet <200ms target? If not, suggest optimization.

**Example concerns:**
- Domain Pattern Matcher vector search — might be slow (>200ms). Suggest caching?
- Component Scaffold — file I/O heavy. Async all the way? Error handling for write failures?

---

### 2. Dependency Analysis

**Identify:**
- Which tools depend on existing infrastructure? (e.g., EPICS.yaml parser, ChromaDB)
- Which tools require new dependencies? (e.g., OpenAPI parser for Component Scaffold)
- Are there circular dependencies or blocking issues?

**Output:**
```markdown
## Dependency Graph

Terminal Status Aggregator
  → get_terminal_status (existing)
  → get_context_saturation (existing)
  → terminalConfig.ts (existing)

Dependency Resolver
  → EPICS.yaml parser (needs implementation)
  → TASKS.yaml parser (existing)
  → graph/types.ts (existing)

Component Scaffold
  → OpenAPI parser (NEW DEPENDENCY: @apidevtools/swagger-parser)
  → Template engine (NEW: handlebars or ejs?)
  → File writer (fs/promises, existing)

Domain Pattern Matcher
  → ChromaDB vector search (existing)
  → Knowledge base indexing (existing)
  → RAG pipeline (needs enhancement)

Session Context Transfer
  → create_task (existing)
  → read_terminal_status_md (existing)
  → Message routing (existing)
```

---

### 3. Cross-Cutting Concerns

**Security:**
- [ ] Are file paths validated? (prevent path traversal in Component Scaffold)
- [ ] Is MCP auth enforced for all tools?
- [ ] Do tools respect terminal permissions? (e.g., only Conductor can call Dependency Resolver?)

**Error Handling:**
- [ ] Graceful degradation if dependencies unavailable (e.g., ChromaDB down)
- [ ] Timeout handling for slow operations
- [ ] Informative error messages for debugging

**Performance:**
- [ ] Can tools run in parallel? (e.g., Terminal Status Aggregator queries all terminals)
- [ ] Do tools cache results? (e.g., Domain Pattern Matcher cached vector search)
- [ ] Are there rate limits needed? (e.g., Component Scaffold writes files)

**Observability:**
- [ ] Are all tools logged? (`log()` from `common.ts`)
- [ ] Do tools emit metrics? (e.g., response time, success/failure rate)
- [ ] Can we track adoption? (e.g., which terminal calls which tool)

---

### 4. Architectural Patterns

**Check alignment with:**
- [ ] **ADR-041:** Graph-based workflow (Dependency Resolver)
- [ ] **ADR-053:** Checkpoint-based coordination (Session Context Transfer)
- [ ] **ADR-049:** Parallel workers (Terminal Status Aggregator)
- [ ] **Context Persistence:** STATUS.md, .session-state.json integration

**Pattern recommendations:**
- Terminal Status Aggregator → Follow `get_all_context_files_status` pattern
- Dependency Resolver → Reuse graph algorithms from `graph/` modules
- Component Scaffold → Follow code generator patterns (if any exist)
- Domain Pattern Matcher → RAG pattern with caching layer
- Session Context Transfer → Follow mailbox/inbox patterns

---

### 5. Testing Strategy

**Suggest test cases for each tool:**

**Terminal Status Aggregator:**
- [ ] All terminals idle → summary = "all idle"
- [ ] 1 terminal working → summary shows "1 working"
- [ ] 1 terminal context saturation WARNING → alerts include it
- [ ] Service down → graceful error

**Dependency Resolver:**
- [ ] Epic with 0 blockers → ready
- [ ] Epic with 1 blocker (not done) → blocked
- [ ] Epic with circular dependency → error detected
- [ ] Invalid epic ID → error

**Session Context Transfer:**
- [ ] Valid transfer → inbox created, success
- [ ] Invalid terminal → error
- [ ] Missing context files → warning but proceed

**Component Scaffold:**
- [ ] Valid React hook → files created
- [ ] Invalid output dir → error
- [ ] File already exists → prompt overwrite or skip

**Domain Pattern Matcher:**
- [ ] Known pattern → high confidence match
- [ ] Unknown pattern → low confidence, suggest similar
- [ ] Empty query → error

---

## Deliverables

1. **Architecture Review Document** — `docs/architecture/decisions/PHASE1_MCP_TOOLS_REVIEW.md`
   - API design feedback for each tool
   - Dependency analysis
   - Security/performance/observability concerns
   - Architectural alignment check

2. **Recommended Changes** — Markdown list of suggested improvements
   - Critical blockers (must fix before implementation)
   - Nice-to-haves (can defer to Phase 2)

3. **Test Strategy** — `src/__tests__/phase1-tools-test-plan.md`
   - Test cases for each tool
   - Integration test scenarios
   - Performance benchmarks

---

## Timeline

**Target:** 2 hours (60 NWT)
- Hour 1: API design review + dependency analysis
- Hour 2: Cross-cutting concerns + test strategy + documentation

**Output to:** Backend (MSG-BACKEND-173 reference), Root (outbox DONE)

---

## References

- **Backend Task:** `terminals/backend/inbox/2026-07-07_173_phase1-mcp-tools-implementation.md`
- **Explorer Research:** `terminals/explorer/outbox/2026-07-07_008_*` and `..._009_*`
- **Existing MCP Tools:** `spaceos-nexus/knowledge-service/src/contextPersistence.ts` (reference implementation)
- **ADR Catalogue:** `docs/knowledge/architecture/ADR_CATALOGUE.md`

---

## Success Criteria

- [ ] All 5 tools reviewed
- [ ] Critical blockers identified (if any)
- [ ] Architectural alignment validated
- [ ] Test strategy documented
- [ ] Backend can start implementation with confidence
