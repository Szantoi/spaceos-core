# Phase 1 MCP Tools Architecture Review

**Status:** DRAFT
**Date:** 2026-07-07
**Reviewer:** Architect (MSG-ARCHITECT-068)
**For:** Backend (MSG-BACKEND-173), Root (MSG-ROOT-014)
**Estimated Review Time:** 60 NWT

---

## Executive Summary

Architectural review of 5 critical MCP tools for Phase 1 Quick Wins. All tools are **architecturally sound** with **minor improvements recommended**. No critical blockers identified.

**Overall Assessment:** ✅ **APPROVE WITH CHANGES**

| Tool | Status | Critical Issues | ROI |
|------|--------|-----------------|-----|
| Terminal Status Aggregator | ✅ APPROVE | None | High (15min/day saved) |
| Dependency Resolver | ⚠️ APPROVE WITH CHANGES | Missing cycle detection | High (20-30min/phase) |
| Session Context Transfer | ✅ APPROVE | None | High (30min/session) |
| Component Scaffold | ⚠️ APPROVE WITH CHANGES | Path traversal risk | Medium (2-3h/week) |
| Domain Pattern Matcher | ⚠️ APPROVE WITH CHANGES | Performance concern | Medium (2-3h/week) |

---

## 1. Terminal Status Aggregator

### API Design Review

**Input Schema:** ✅ **GOOD**
```typescript
mcp__spaceos-knowledge__get_terminal_status_aggregate
  format: "summary" | "detailed" | "alerts_only"
```

- ✅ Minimal required params (format optional with sensible default)
- ✅ Type-safe enum for format
- ✅ Follows existing MCP naming convention

**Output Schema:** ✅ **GOOD**
```json
{
  "summary": {
    "working": ["conductor", "backend"],
    "idle": ["frontend", "architect"],
    "blocked": ["librarian"],
    "warnings": ["conductor: turn count >30"]
  },
  "terminals": [...]
}
```

- ✅ Consistent with existing `get_terminal_status` pattern
- ✅ Includes saturation warnings (context persistence integration)
- ✅ ISO 8601 timestamp for `lastActive`

**Response Time:** ✅ **<200ms achievable**
- Aggregates 7 terminals (parallel Promise.all)
- Each terminal status read: ~20-30ms
- Total: ~30-50ms (parallel) + 10ms aggregation = **~60ms**

### Recommendations

**Minor improvements:**
1. Add `includeMemoryStats?: boolean` param (optional) for memory usage tracking
2. Cache results for 2 seconds (reduce load for Conductor rapid checks)

**Example cache implementation:**
```typescript
const CACHE_TTL = 2000; // 2 seconds
let cache: { data: any; timestamp: number } | null = null;

if (cache && Date.now() - cache.timestamp < CACHE_TTL) {
  return cache.data;
}
```

---

## 2. Dependency Resolver

### API Design Review

**Input Schema:** ✅ **GOOD**
```typescript
mcp__spaceos-knowledge__resolve_dependencies
  epic_id: "EPIC-CUTTING-Q3"
  check_blockers: true
```

- ✅ Epic-centric (aligns with ADR-041 graph-based workflow)
- ✅ Boolean flag for blocker check (performance optimization)

**Output Schema:** ✅ **GOOD**
```json
{
  "epic": "EPIC-CUTTING-Q3",
  "status": "active",
  "blockedBy": [],
  "blocks": ["EPIC-PORTAL-V2"],
  "parallelWith": ["EPIC-JOINERY-V2"],
  "readyTasks": [...],
  "blockedTasks": [...]
}
```

- ✅ Clear separation: epic-level vs task-level dependencies
- ✅ Aligns with EPICS.yaml structure (`depends_on`, `parallel_with`)

**Response Time:** ⚠️ **<200ms needs optimization**
- EPICS.yaml parse: ~10ms
- TASKS.yaml parse (multiple): ~50-100ms
- Dependency resolution: ~20-50ms
- **Total: ~100-160ms** ✅ (acceptable)

### Critical Issue: Missing Cycle Detection

**Problem:** EPICS.yaml could contain circular dependencies:
```yaml
- id: EPIC-A
  depends_on: ["EPIC-B"]
- id: EPIC-B
  depends_on: ["EPIC-A"]
```

**Impact:** Infinite loop in resolution algorithm

**Solution:** ⚠️ **REQUIRED** — Add cycle detection using topological sort

**Recommended implementation:**
```typescript
import { detectCycles } from './graph/dagValidator';

const cycles = detectCycles(epicGraph);
if (cycles.length > 0) {
  return {
    success: false,
    error: `Circular dependencies detected: ${cycles.join(' -> ')}`
  };
}
```

**Reference:** ADR-041 Graph-Based Workflow Architecture — cycle detection is explicitly mentioned.

### Recommendations

**Critical:**
1. ⚠️ **Add cycle detection** (use existing `dagValidator.ts` module)
2. ⚠️ **Validate epic_id exists** before resolution (prevent undefined behavior)

**Nice-to-have:**
3. Add `include_critical_path?: boolean` param (calculate longest dependency chain)
4. Cache EPICS.yaml parse results (invalidate on file change)

---

## 3. Session Context Transfer

### API Design Review

**Input Schema:** ✅ **GOOD**
```typescript
mcp__spaceos-knowledge__transfer_session_context
  from_terminal: "explorer"
  to_terminal: "librarian"
  context_type: "research_summary" | "code_audit" | "knowledge_synthesis"
  include_files: ["file1.md", "file2.ts"]
```

- ✅ Type-safe enum for context_type
- ✅ Optional file list (lightweight by default)
- ✅ Clear sender/receiver model

**Output Schema:** ✅ **GOOD**
```json
{
  "success": true,
  "message_id": "MSG-LIBRARIAN-004",
  "summary": "Transferred 3 research findings + 5 code patterns",
  "inbox_file": "terminals/librarian/inbox/2026-07-07_004_context-transfer-explorer.md"
}
```

- ✅ Follows existing `create_task` response pattern
- ✅ Returns inbox file path (debuggability)

**Response Time:** ✅ **<200ms achievable**
- Read STATUS.md (from_terminal): ~10ms
- Generate inbox message: ~20ms
- Write inbox file: ~30ms
- **Total: ~60ms** ✅

### Recommendations

**Minor improvements:**
1. Add `priority?: "critical" | "high" | "medium" | "low"` param (default: "medium")
2. Validate `from_terminal !== to_terminal` (prevent self-transfer)
3. Include turn count and context saturation in transfer metadata

**Example metadata addition:**
```typescript
const metadata = {
  turnCount: await readTurnCount(from_terminal),
  saturation: await getContextSaturation(from_terminal),
  transferredAt: new Date().toISOString()
};
```

---

## 4. Component Scaffold

### API Design Review

**Input Schema:** ✅ **GOOD**
```typescript
mcp__spaceos-knowledge__scaffold_component
  component_type: "react_hook" | "react_component" | "api_client"
  name: "useCostBudget"
  api_spec: "openapi.yaml#/components/schemas/CostBudget"
  output_dir: "client/src/hooks/"
```

- ✅ Type-safe enum for component_type
- ✅ OpenAPI reference pattern (aligns with ADR-050 Code Generator Toolchain)
- ⚠️ **Security concern:** `output_dir` could be path traversal attack vector

**Output Schema:** ✅ **GOOD**
```json
{
  "success": true,
  "files_created": [
    "client/src/hooks/useCostBudget.ts",
    "client/src/hooks/__tests__/useCostBudget.test.ts"
  ],
  "next_steps": [...]
}
```

- ✅ Clear action items in `next_steps`
- ✅ Returns created file paths

**Response Time:** ⚠️ **>200ms likely**
- OpenAPI parse: ~50-100ms
- Template rendering: ~20-30ms
- File write (2 files): ~40-60ms
- **Total: ~110-190ms** ✅ (acceptable, but close to threshold)

### Critical Issue: Path Traversal Risk

**Problem:** Unvalidated `output_dir` parameter:
```typescript
// Vulnerable:
await fs.writeFile(path.join(output_dir, filename), content);

// Attack:
output_dir: "../../../../../../etc/"
```

**Solution:** ⚠️ **REQUIRED** — Validate output_dir is within allowed paths

**Recommended implementation:**
```typescript
import path from 'path';

const ALLOWED_ROOTS = [
  '/opt/spaceos/datahaven-web/client/src/',
  '/opt/spaceos/spaceos-nexus/orchestrator/src/'
];

function validateOutputDir(dir: string): boolean {
  const resolved = path.resolve(dir);
  return ALLOWED_ROOTS.some(root => resolved.startsWith(root));
}

if (!validateOutputDir(output_dir)) {
  return { success: false, error: "Invalid output directory" };
}
```

### Recommendations

**Critical:**
1. ⚠️ **Validate output_dir** against whitelist (prevent path traversal)
2. ⚠️ **Handle file exists case** — Add `overwrite?: boolean` param (default: false, prompt user)

**Nice-to-have:**
3. Add dry-run mode (`dry_run?: boolean`) — return generated content without writing
4. Support template customization (e.g., `template_variant?: "datahaven" | "portal"`)

---

## 5. Domain Pattern Matcher

### API Design Review

**Input Schema:** ✅ **GOOD**
```typescript
mcp__spaceos-knowledge__match_domain_pattern
  description: "Track cost breakdown by project phase"
  domain: "kontrolling" | "crm" | "procurement" | "ehs"
```

- ✅ Natural language description (LLM-friendly)
- ✅ Optional domain filter (narrows search space)

**Output Schema:** ✅ **GOOD**
```json
{
  "pattern": "Cost Breakdown Widget",
  "confidence": 0.92,
  "references": [...],
  "recommendations": [...],
  "example_code": "...",
  "adr_refs": ["ADR-054", "ADR-055"]
}
```

- ✅ Confidence score (0.0-1.0) for ranking
- ✅ Actionable recommendations
- ✅ ADR cross-references

**Response Time:** ⚠️ **>200ms very likely**
- Vector embedding generation: ~100-200ms (depending on embedding backend)
- ChromaDB vector search: ~50-150ms
- Knowledge doc retrieval: ~20-50ms
- **Total: ~200-400ms** ⚠️ (exceeds target)

### Performance Concern: Vector Search Latency

**Problem:** Vector search can exceed 200ms target, especially:
- Cold start (embedding model load)
- Large knowledge base (>1000 docs)
- Network latency (if ChromaDB remote)

**Solution:** ⚠️ **RECOMMENDED** — Add caching layer

**Recommended implementation:**
```typescript
import { LRUCache } from 'lru-cache';

const patternCache = new LRUCache<string, PatternMatch>({
  max: 100, // cache top 100 queries
  ttl: 1000 * 60 * 60, // 1 hour
});

const cacheKey = `${description}:${domain}`;
if (patternCache.has(cacheKey)) {
  return patternCache.get(cacheKey);
}

// Perform search...
patternCache.set(cacheKey, result);
```

### Recommendations

**Critical:**
1. ⚠️ **Add LRU cache** (1 hour TTL, 100 entries) — Reduce avg response time to <100ms

**Nice-to-have:**
2. Add `top_k?: number` param (default: 5) — Return multiple matches
3. Add `min_confidence?: number` param (default: 0.7) — Filter low-confidence results
4. Preload embedding model on service start (avoid cold start penalty)

---

## Dependency Analysis

### Dependency Graph

```
Terminal Status Aggregator
  → get_terminal_status (existing)
  → get_context_saturation (existing)
  → terminalConfig.ts (existing)
  ✅ No new dependencies

Dependency Resolver
  → EPICS.yaml parser (⚠️ needs implementation)
  → TASKS.yaml parser (existing: projectTools.ts)
  → graph/types.ts (existing: ADR-041)
  → dagValidator.ts (existing: detectCycles)
  ⚠️ New: EPICS.yaml parser (~50 LOC)

Session Context Transfer
  → create_task (existing)
  → read_terminal_status_md (existing)
  → Message routing (existing: mailbox.ts)
  ✅ No new dependencies

Component Scaffold
  → OpenAPI parser (⚠️ NEW DEPENDENCY: @apidevtools/swagger-parser)
  → Template engine (⚠️ NEW DEPENDENCY: handlebars or ejs)
  → File writer (fs/promises, existing)
  ⚠️ New dependencies: 2 npm packages (~5MB)

Domain Pattern Matcher
  → ChromaDB vector search (existing)
  → Knowledge base indexing (existing)
  → Embedding backend (existing: embeddings.ts)
  ✅ No new dependencies (but needs caching layer)
```

### New Dependencies Required

| Tool | Dependency | Size | License | Risk |
|------|-----------|------|---------|------|
| Dependency Resolver | EPICS.yaml parser | ~50 LOC | N/A (internal) | ✅ Low |
| Component Scaffold | @apidevtools/swagger-parser | ~3MB | MIT | ✅ Low |
| Component Scaffold | handlebars | ~2MB | MIT | ✅ Low |
| Domain Pattern Matcher | lru-cache | ~50KB | ISC | ✅ Low |

**Total new npm dependencies:** 3 packages (~5MB)

### Installation Order (by dependency chain)

1. **Terminal Status Aggregator** (Day 1) — No blockers
2. **Session Context Transfer** (Day 1) — No blockers
3. **Dependency Resolver** (Day 2) — Needs EPICS.yaml parser implementation
4. **Domain Pattern Matcher** (Day 2-3) — Needs cache layer implementation
5. **Component Scaffold** (Day 3-4) — Needs npm packages + template files

---

## Cross-Cutting Concerns

### Security

| Tool | Risk | Mitigation |
|------|------|------------|
| Terminal Status Aggregator | ✅ None | Already uses MCP auth |
| Dependency Resolver | ✅ None | Read-only YAML files |
| Session Context Transfer | ⚠️ Medium | Validate terminal names against whitelist |
| Component Scaffold | 🚨 **HIGH** | **Path traversal attack** — Validate output_dir |
| Domain Pattern Matcher | ✅ None | Read-only vector search |

**Action Required:**
- ⚠️ **Component Scaffold:** Add output_dir validation (CRITICAL)
- ⚠️ **Session Context Transfer:** Validate terminal names (RECOMMENDED)

### Error Handling

All tools must follow existing MCP error pattern:
```typescript
return {
  success: false,
  error: "Descriptive error message for debugging"
};
```

**Specific error scenarios to handle:**
- **Terminal Status Aggregator:** Service down → gracefully skip terminal
- **Dependency Resolver:** Invalid epic ID → return error
- **Session Context Transfer:** Target terminal not found → return error
- **Component Scaffold:** File already exists → prompt overwrite
- **Domain Pattern Matcher:** ChromaDB connection failure → return cached results or error

### Performance

| Tool | Target | Expected | Action |
|------|--------|----------|--------|
| Terminal Status Aggregator | <200ms | ~60ms | ✅ OK |
| Dependency Resolver | <200ms | ~100-160ms | ✅ OK |
| Session Context Transfer | <200ms | ~60ms | ✅ OK |
| Component Scaffold | <200ms | ~110-190ms | ✅ OK |
| Domain Pattern Matcher | <200ms | ~200-400ms | ⚠️ Add cache |

**Optimization priorities:**
1. **Domain Pattern Matcher:** LRU cache (CRITICAL for <200ms)
2. **Dependency Resolver:** Cache EPICS.yaml parse (NICE-TO-HAVE)
3. **Component Scaffold:** Async file writes (NICE-TO-HAVE)

### Observability

All tools must:
- ✅ Use `log()` from `src/pipeline/common.ts`
- ✅ Emit metrics (response time, success/failure rate)
- ✅ Track adoption (which terminal calls which tool)

**Recommended logging format:**
```typescript
import { log } from './pipeline/common';

log(`[MCP Tool] ${toolName} called by ${terminal} — duration: ${elapsed}ms, success: ${success}`);
```

**Metrics to track:**
- Call count per tool (daily/weekly)
- Average response time per tool
- Error rate per tool
- Adoption per terminal (which terminal uses which tool)

---

## Architectural Alignment

### ADR-041: Graph-Based Workflow

**Tool:** Dependency Resolver

✅ **Aligned** — Tool design directly implements ADR-041:
- Uses EPICS.yaml adjacency list representation
- Returns `blockedBy`, `blocks`, `parallelWith` fields
- Supports topological sort (with recommended cycle detection)

**Recommendation:** Reference ADR-041 in tool documentation

---

### ADR-049: Dual Session (Parallel Workers)

**Tool:** Terminal Status Aggregator

✅ **Aligned** — Tool supports ADR-049 worker monitoring:
- Aggregates multiple terminal sessions
- Tracks context saturation (turn count warnings)
- Enables conductor to coordinate parallel workers

**Future enhancement:** Add worker-level status (track individual work sessions within a terminal)

---

### ADR-050: Code Generator Toolchain

**Tool:** Component Scaffold

✅ **Aligned** — Tool extends ADR-050 Phase 1:
- Orval/NSwag generate from OpenAPI (existing)
- Component Scaffold adds React component generation
- Both use OpenAPI as source of truth

**Recommendation:** Consider integrating with existing Orval/NSwag setup (share OpenAPI parser)

---

### Context Persistence Pattern

**Tool:** Session Context Transfer

✅ **Aligned** — Tool integrates with context persistence files:
- Reads STATUS.md, .session-state.json
- Transfers turn count and saturation metadata
- Supports goal re-anchoring workflow

**Recommendation:** Include checkpoint data in transfer (from CHECKPOINTS.md)

---

## Test Strategy

### Unit Tests (per tool)

**Terminal Status Aggregator:**
```typescript
describe('Terminal Status Aggregator', () => {
  test('all terminals idle', async () => {
    const result = await getTerminalStatusAggregate('summary');
    expect(result.summary.working).toEqual([]);
    expect(result.summary.idle).toEqual(['conductor', 'backend', ...]);
  });

  test('1 terminal working', async () => {
    // Mock: conductor status = working
    const result = await getTerminalStatusAggregate('summary');
    expect(result.summary.working).toContain('conductor');
  });

  test('context saturation warning', async () => {
    // Mock: conductor turn count = 35
    const result = await getTerminalStatusAggregate('alerts_only');
    expect(result.summary.warnings).toContain('conductor: turn count >30');
  });

  test('service down graceful degradation', async () => {
    // Mock: get_terminal_status throws
    const result = await getTerminalStatusAggregate('summary');
    expect(result.success).toBe(true); // Gracefully skip unavailable terminal
  });
});
```

**Dependency Resolver:**
```typescript
describe('Dependency Resolver', () => {
  test('epic with 0 blockers', async () => {
    const result = await resolveDependencies('EPIC-READY');
    expect(result.blockedBy).toEqual([]);
    expect(result.status).toBe('active');
  });

  test('epic with 1 blocker (not done)', async () => {
    const result = await resolveDependencies('EPIC-BLOCKED');
    expect(result.blockedBy).toContain('EPIC-KERNEL-STABLE');
    expect(result.status).toBe('blocked');
  });

  test('circular dependency detected', async () => {
    const result = await resolveDependencies('EPIC-CYCLE-A');
    expect(result.success).toBe(false);
    expect(result.error).toContain('Circular dependencies');
  });

  test('invalid epic ID', async () => {
    const result = await resolveDependencies('EPIC-NONEXISTENT');
    expect(result.success).toBe(false);
  });
});
```

**Session Context Transfer:**
```typescript
describe('Session Context Transfer', () => {
  test('valid transfer', async () => {
    const result = await transferSessionContext({
      from_terminal: 'explorer',
      to_terminal: 'librarian',
      context_type: 'research_summary'
    });
    expect(result.success).toBe(true);
    expect(result.message_id).toMatch(/MSG-LIBRARIAN-\d+/);
  });

  test('invalid terminal', async () => {
    const result = await transferSessionContext({
      from_terminal: 'invalid',
      to_terminal: 'librarian',
      context_type: 'research_summary'
    });
    expect(result.success).toBe(false);
  });

  test('missing context files', async () => {
    // Mock: STATUS.md not found
    const result = await transferSessionContext({
      from_terminal: 'explorer',
      to_terminal: 'librarian',
      context_type: 'research_summary'
    });
    expect(result.success).toBe(true); // Proceed with warning
    expect(result.warnings).toContain('STATUS.md not found');
  });
});
```

**Component Scaffold:**
```typescript
describe('Component Scaffold', () => {
  test('valid React hook', async () => {
    const result = await scaffoldComponent({
      component_type: 'react_hook',
      name: 'useCostBudget',
      output_dir: '/opt/spaceos/datahaven-web/client/src/hooks/'
    });
    expect(result.success).toBe(true);
    expect(result.files_created).toHaveLength(2);
  });

  test('invalid output dir', async () => {
    const result = await scaffoldComponent({
      component_type: 'react_hook',
      name: 'useCostBudget',
      output_dir: '../../etc/' // Path traversal attempt
    });
    expect(result.success).toBe(false);
    expect(result.error).toContain('Invalid output directory');
  });

  test('file already exists', async () => {
    // Mock: file exists
    const result = await scaffoldComponent({
      component_type: 'react_hook',
      name: 'useExisting',
      output_dir: '/opt/spaceos/datahaven-web/client/src/hooks/'
    });
    expect(result.success).toBe(false);
    expect(result.error).toContain('already exists');
  });
});
```

**Domain Pattern Matcher:**
```typescript
describe('Domain Pattern Matcher', () => {
  test('known pattern high confidence', async () => {
    const result = await matchDomainPattern({
      description: 'Track cost breakdown by project phase',
      domain: 'kontrolling'
    });
    expect(result.confidence).toBeGreaterThan(0.7);
    expect(result.pattern).toContain('Cost Breakdown');
  });

  test('unknown pattern low confidence', async () => {
    const result = await matchDomainPattern({
      description: 'Alien technology integration',
      domain: 'crm'
    });
    expect(result.confidence).toBeLessThan(0.5);
  });

  test('empty query', async () => {
    const result = await matchDomainPattern({
      description: '',
      domain: 'kontrolling'
    });
    expect(result.success).toBe(false);
  });

  test('cache hit', async () => {
    const result1 = await matchDomainPattern({
      description: 'cost breakdown',
      domain: 'kontrolling'
    });
    const start = Date.now();
    const result2 = await matchDomainPattern({
      description: 'cost breakdown',
      domain: 'kontrolling'
    });
    const elapsed = Date.now() - start;
    expect(elapsed).toBeLessThan(50); // Cache hit should be <50ms
  });
});
```

### Integration Tests

**Scenario 1: Conductor Session Workflow**
```typescript
test('Conductor checks status, resolves dependencies, spawns workers', async () => {
  // 1. Conductor checks terminal status
  const status = await getTerminalStatusAggregate('summary');
  expect(status.summary.idle).toContain('backend');

  // 2. Conductor resolves epic dependencies
  const deps = await resolveDependencies('EPIC-CUTTING-Q3');
  expect(deps.readyTasks).toHaveLength(3);

  // 3. Conductor transfers context to backend
  const transfer = await transferSessionContext({
    from_terminal: 'conductor',
    to_terminal: 'backend',
    context_type: 'code_audit'
  });
  expect(transfer.success).toBe(true);
});
```

**Scenario 2: Frontend Component Scaffolding + Pattern Match**
```typescript
test('Frontend uses pattern matcher, then scaffolds component', async () => {
  // 1. Frontend searches for pattern
  const pattern = await matchDomainPattern({
    description: 'EAC calculation widget',
    domain: 'kontrolling'
  });
  expect(pattern.confidence).toBeGreaterThan(0.8);

  // 2. Frontend scaffolds component based on pattern
  const scaffold = await scaffoldComponent({
    component_type: 'react_component',
    name: 'EACCalculationWidget',
    output_dir: '/opt/spaceos/datahaven-web/client/src/components/kontrolling/'
  });
  expect(scaffold.success).toBe(true);
});
```

### Performance Benchmarks

| Tool | Target | Test |
|------|--------|------|
| Terminal Status Aggregator | <200ms | Aggregate 7 terminals (parallel) |
| Dependency Resolver | <200ms | Resolve epic with 10 task dependencies |
| Session Context Transfer | <200ms | Transfer 3 files context |
| Component Scaffold | <200ms | Generate React hook with tests |
| Domain Pattern Matcher | <200ms | Search 1000 doc knowledge base (cached) |

**Benchmark script:**
```bash
npm run test:benchmark -- --tool=all
```

---

## Critical Recommendations Summary

### Must Fix (Blockers)

1. ⚠️ **Dependency Resolver:** Add cycle detection (use `dagValidator.ts`)
2. 🚨 **Component Scaffold:** Validate output_dir (prevent path traversal)

### Should Fix (Before Production)

3. ⚠️ **Domain Pattern Matcher:** Add LRU cache (1 hour TTL, 100 entries)
4. ⚠️ **Component Scaffold:** Handle file exists case (add `overwrite` param)
5. ⚠️ **Session Context Transfer:** Validate terminal names against whitelist

### Nice-to-Have (Can Defer to Phase 2)

6. Terminal Status Aggregator: Add memory stats
7. Dependency Resolver: Add critical path calculation
8. Session Context Transfer: Include checkpoint data
9. Component Scaffold: Add dry-run mode
10. Domain Pattern Matcher: Add `top_k` param for multiple matches

---

## Conclusion

All 5 Phase 1 MCP tools are **architecturally sound** and align with existing ADRs (ADR-041, ADR-049, ADR-050). Implementation can proceed with confidence.

**2 critical fixes required:**
1. Dependency Resolver: cycle detection
2. Component Scaffold: path validation

**Estimated fix time:** 2-3 hours (Backend can start in parallel)

**Overall Assessment:** ✅ **APPROVE WITH CHANGES**

---

## Next Steps

1. **Backend:** Implement 5 tools following this review
2. **Architect:** Review implementation PR before merge
3. **Root:** Monitor Phase 1 adoption metrics (track ROI)

**References:**
- Backend Task: `terminals/backend/inbox/2026-07-07_173_phase1-mcp-tools-implementation.md`
- ADR-041: Graph-Based Workflow
- ADR-049: Dual Session Architecture
- ADR-050: Code Generator Toolchain
- Context Persistence: `src/contextPersistence.ts`
