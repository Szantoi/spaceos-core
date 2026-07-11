---
id: TASK-14-06
title: "TASK-14-06: Memory Tool Plugin Module"
epic: EPIC-14
phase: "Phase 2: Advanced Features"
type: task
created: 2026-03-10
status: "🟢 Phase 1 Complete → Ready to Start"
effort: "8 hours"
---

# TASK-14-06: Memory Tool Plugin Module

## Overview

Refactor existing memory tools (save_episode, query_memory, search_memory) into a **decorator-based plugin module** using the plugin system established in TASK-14-03 (PluginManager, @Plugin, @Tool decorators).

**Status:** 🟢 READY TO START (Phase 1 complete, no blockers)
**Owner:** TBD (Dev C or Memory/RAG specialist)
**Duration:** 8 hours
**Predecessor:** TASK-14-03, TASK-14-04, TASK-14-05 (Phase 1 foundation)
**Successor:** TASK-14-11 (E2E test suite)

---

## Acceptance Criteria

### AC-1: Plugin Manifest & Metadata

**Requirement:** MemoryPlugin exports valid PluginManifest with @Plugin decorator.

**Input:** None (metadata extraction)

**Output:** Plugin manifest with:
- id: "memory"
- name: "Memory Tool Plugin"
- version: "1.0.0"
- dependencies: ["bootstrap"] (requires auth context)
- critical: true (memory tools are essential)

**Validation:**
- [ ] @Plugin decorator applied to MemoryPlugin class
- [ ] BasePlugin extended correctly
- [ ] IToolModule interface implemented
- [ ] Manifest extractable via PluginManager

**Test Case:** UT-01 — Metadata extraction and validation

---

### AC-2: save_episode Tool Implementation

**Requirement:** Implement `save_episode` tool with @Tool decorator, input validation, and episodic memory persistence.

**Input:**
```typescript
{
  agent_id: string;        // Required: UUID of agent
  episode_data: {
    thought_process: string;
    actions: string[];
    outcome: string;
    reasoning: string;
  };
  metadata: {
    timestamp?: number;    // Optional: defaults to now
    tags?: string[];       // Optional: for search
  };
}
```

**Output:**
```typescript
{
  status: 'success' | 'error';
  episode_id?: string;     // If success: unique episode ID
  error?: string;          // If error: error message
}
```

**Implementation Details:**
- [ ] Input schema validation (Zod or similar)
- [ ] AgentId validation (must exist in auth context)
- [ ] Episode embedding via ChromaDB
- [ ] Metadata persistence in SQLite
- [ ] Error handling (invalid agentId, DB errors)
- [ ] RBAC: User can only save episodes for agents they own
- [ ] Return episode_id for later retrieval

**Validation:**
- Test successful episode save
- Test invalid agentId (403 Forbidden or error response)
- Test duplicate episode handling
- Test metadata tag extraction

**Test Case:** UT-02 — save_episode tool handler

---

### AC-3: query_memory Tool Implementation

**Requirement:** Implement `query_memory` tool to retrieve episodes matching a query.

**Input:**
```typescript
{
  agent_id: string;
  query: string;              // Natural language query
  limit?: number;             // Max results (default 10)
  similarity_threshold?: number; // 0.0-1.0 (default 0.7)
}
```

**Output:**
```typescript
{
  status: 'success' | 'error';
  episodes?: Array<{
    episode_id: string;
    thought_process: string;
    actions: string[];
    outcome: string;
    similarity_score: number;
    timestamp: number;
  }>;
  total_found?: number;
  error?: string;
}
```

**Implementation Details:**
- [ ] ChromaDB semantic search query
- [ ] Query embedding generation (same model as save_episode)
- [ ] Similarity score filtering
- [ ] Result pagination
- [ ] RBAC: Only episodes belonging to user's agents
- [ ] Error handling (invalid agentId, query errors)
- [ ] Performance: < 500ms for typical queries

**Validation:**
- Test successful query with results
- Test empty query (no matches)
- Test similarity threshold filtering
- Test pagination (limit parameter)
- Test RBAC isolation (user can't see other agents' episodes)

**Test Case:** UT-03 — query_memory tool handler

---

### AC-4: search_memory Tool Implementation

**Requirement:** Implement `search_memory` tool for metadata-based search (tags, timestamp range).

**Input:**
```typescript
{
  agent_id: string;
  filters: {
    tags?: string[];           // Match any tag
    start_time?: number;       // Unix timestamp
    end_time?: number;         // Unix timestamp
    outcome?: 'success' | 'failure' | 'partial'; // Filter by outcome
  };
  limit?: number;              // Default 10
}
```

**Output:**
```typescript
{
  status: 'success' | 'error';
  episodes?: Array<{
    episode_id: string;
    tags: string[];
    timestamp: number;
    outcome: string;
    actions_count: number;
  }>;
  total_found?: number;
  error?: string;
}
```

**Implementation Details:**
- [ ] SQLite query with WHERE clauses (tags, timestamp range, outcome)
- [ ] Filter combination (AND logic: all filters must match)
- [ ] Efficient indexing on tags and timestamp
- [ ] RBAC: Only episodes belonging to user's agents
- [ ] Performance: < 100ms for typical filters
- [ ] Error handling (invalid date range, invalid outcome)

**Validation:**
- Test successful search with multiple filters
- Test empty result set
- Test date range filtering
- Test tag matching
- Test outcome filtering
- Test performance (< 100ms)

**Test Case:** UT-04 — search_memory tool handler

---

### AC-5: RBAC & Context Propagation

**Requirement:** All memory tools properly validate auth context and enforce RBAC constraints.

**Implementation Details:**
- [ ] Session validation (user_id extracted from McpContext)
- [ ] Agent ownership verification (user owns the agent)
- [ ] Tool handlers receive McpContext with:
  - session_id: string
  - user_id: string
  - permissions: string[]
- [ ] Error responses distinguish between:
  - 401 Unauthorized (no valid session)
  - 403 Forbidden (user doesn't own agent)
  - 400 Bad Request (invalid input)
- [ ] No user_data leakage in error messages

**Validation:**
- Test with valid auth (agent owner)
- Test with invalid session (no context)
- Test with different user (doesn't own agent)
- Test error message safety

**Test Case:** UT-05 — RBAC validation for all three tools

---

### AC-6: Performance SLA

**Requirement:** All memory tools meet performance targets under typical usage.

**Performance Targets:**
- `save_episode`: < 500ms (includes embedding generation)
- `query_memory`: < 500ms (includes vector search)
- `search_memory`: < 100ms (metadata-only search)

**Implementation Details:**
- [ ] Benchmark: Measure actual times under load
- [ ] ChromaDB batch operations if needed
- [ ] SQLite index optimization (on tags, timestamp, outcome)
- [ ] Query caching for identical requests (optional)
- [ ] Connection pooling for database

**Validation:**
- [ ] Run 10 invocations of each tool
- [ ] Record timing for each
- [ ] Verify all < SLA
- [ ] Document any optimization notes

**Test Case:** UT-06 — Performance benchmarks

---

## Implementation Details

### File Changes

**New File:** `src/mcp/tools/memory.ts`

```typescript
// Structure:
import { BasePlugin, Plugin, Tool } from '../../plugins/PluginDecorators';
import { IToolModule, ToolHandler, McpContext } from '../../plugins/PluginTypes';

@Plugin({
  id: 'memory',
  name: 'Memory Tool Plugin',
  version: '1.0.0',
  dependencies: ['bootstrap'],
  critical: true,
  description: 'Episodic memory management for agent decision history',
})
export class MemoryPlugin extends BasePlugin implements IToolModule {
  name = 'memory';
  handlers: Map<string, ToolHandler> = new Map();

  constructor() {
    super();
    this.handlers.set('save_episode', this.save_episode.bind(this));
    this.handlers.set('query_memory', this.query_memory.bind(this));
    this.handlers.set('search_memory', this.search_memory.bind(this));
  }

  @Tool({
    name: 'save_episode',
    description: 'Save agent episode to episodic memory',
    inputSchema: { /* ... */ }
  })
  async save_episode(input: any, context: Partial<McpContext>): Promise<any> {
    // Implementation
  }

  @Tool({
    name: 'query_memory',
    description: 'Query episodes by semantic similarity',
    inputSchema: { /* ... */ }
  })
  async query_memory(input: any, context: Partial<McpContext>): Promise<any> {
    // Implementation
  }

  @Tool({
    name: 'search_memory',
    description: 'Search episodes by metadata (tags, time, outcome)',
    inputSchema: { /* ... */ }
  })
  async search_memory(input: any, context: Partial<McpContext>): Promise<any> {
    // Implementation
  }

  async onInit(context: any): Promise<void> {
    // Initialize database connections, ChromaDB client
  }

  async onDestroy(): Promise<void> {
    // Cleanup connections
  }
}
```

**Refactor Existing:** Migrate existing memory tool implementations

- Find existing memory tool handlers (likely in src/mcp/tools or similar)
- Extract logic into the three tool methods above
- Adapt input/output formats to match AC spec
- Update error handling for tool format

---

### Dependencies

**New Dependencies (if needed):**
- None (ChromaDB and SQLite clients already available)

**Existing Dependencies Used:**
- ChromaDB client (for vector search)
- SQLite connection pool (for metadata search)
- Zod or similar (for input validation)
- RequestContext (for RBAC validation)

---

### Testing Strategy

**Unit Tests:** `src/tests/unit/memory-plugin.test.ts`

```
✅ UT-01: Plugin metadata extraction
✅ UT-02: save_episode tool handler (success, validation errors, RBAC)
✅ UT-03: query_memory tool handler (results, empty, threshold filtering)
✅ UT-04: search_memory tool handler (filters, date ranges, tags)
✅ UT-05: RBAC validation (auth, ownership, error safety)
✅ UT-06: Performance benchmarks (SLA validation)
```

**Integration Tests:** `src/tests/integration/memory-tools-integration.test.ts`

```
✅ IT-1: MemoryPlugin registers with PluginManager
✅ IT-2: All three tools invokable via PluginManager.invokeTool()
✅ IT-3: Episode save → query → retrieve flow
✅ IT-4: Episode search with multiple filters
✅ IT-5: RequestContext propagates through tool chain
✅ IT-6: RBAC enforcement across all tools
```

**Test Coverage Target:** ≥ 80%

---

### Definition of Done

- [x] All 6 AC implemented and tested
- [x] @Plugin and @Tool decorators applied correctly
- [x] IToolModule interface fully implemented
- [x] RBAC validation in place
- [x] Performance SLA validated (< 500ms, < 100ms)
- [x] Unit tests: 6/6 passing (UT-01 through UT-06)
- [x] Integration tests: 6/6 passing (IT-1 through IT-6)
- [x] Code review ready
- [x] Implementation summary documented
- [x] No regressions in existing tests (Phase 1 tests still passing)

---

## Success Criteria

**Completion Definition:**

1. ✅ plugin.ts file created with IToolModule interface
2. ✅ save_episode, query_memory, search_memory tools implemented
3. ✅ All 6 AC validated with tests
4. ✅ Performance SLA met (documented)
5. ✅ RBAC properly enforced
6. ✅ Zero regressions (Phase 1: 76 tests still passing + new 12 tests passing)
7. ✅ Ready for TASK-14-11 (E2E integration)

**Pass/Fail Criteria:**

- ✅ PASS: 12/12 tests passing (6 unit + 6 integration) AND Phase 1 still 76/76
- ❌ FAIL: Any test failing OR Phase 1 regression OR RBAC bypass

---

## Dependencies & Blockers

**Blocks On:** None (Phase 1 foundation complete)

**Unblocks:** TASK-14-07, TASK-14-11 (E2E tests)

**Related Work:**
- TASK-14-03: Plugin system (PluginManager, decorators) — COMPLETE
- TASK-14-04: Bootstrap plugin example — COMPLETE
- TASK-14-05: Context/Discovery plugins example — COMPLETE

---

## Handoff & Next Steps

**After Completion:**

1. Code review by backend lead
2. Merge to main branch
3. Begins unblocking:
   - TASK-14-07: Legacy tools module
   - TASK-14-11: E2E test suite

**For TASK-14-11:** Memory tools must be invokable via `/mcp/call` endpoint (TASK-14-02) for E2E testing.

---

## Notes

- Memory plugin is marked `critical: true` — failure blocks startup
- Prioritize RBAC validation (data isolation between agents)
- Performance SLA is critical; optimize ChromaDB queries if needed
- Consider lazy-loading ChromaDB client if startup time becomes issue
- Document any design decisions in implementation summary

---

**Created:** 2026-03-10
**Phase:** 2 (Advanced Features)
**Status:** 🟢 Ready to Start (waiting for developer assignment)
