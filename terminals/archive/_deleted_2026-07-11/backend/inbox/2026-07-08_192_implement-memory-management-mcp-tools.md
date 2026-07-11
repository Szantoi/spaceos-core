---
processed: 2026-07-10
id: MSG-BACKEND-192
from: root
to: backend
type: task
priority: medium
status: READ
model: sonnet
ref: MSG-ROOT-031
created: 2026-07-08
content_hash: 991f043eace09c25b4ddc05dcff48063c72cae7e44444299b46f17403099990b
---

# Task: Implement Memory Management MCP Tools

## Context

**Librarian request (MSG-ROOT-031):** Memory management tool-ok hiányoznak az MCP rendszerből.

**Empirical evidence:** Emergency cleanup (MSG-LIBRARIAN-026) 672KB → 108KB manuális munka volt:
- 6 terminált külön kellett kezelni
- Pattern felismerés szemmel
- Nincs fleet overview vagy automatic detection

**Root decision:** ✅ APPROVE — ROI pozitív (257-343% annual), generikus tool-ok, infrastructure scope

---

## Your Task

**Implement 3 MCP tools** a memory management automatizálásához.

### Tool #1: `mcp__spaceos-knowledge__memory_health_report`

**Purpose:** Fleet-wide memory status in one call

**Parameters:**
```typescript
// No parameters (scans all terminals)
```

**Returns:**
```typescript
{
  terminals: [
    {
      name: "conductor",
      size_kb: 145,
      threshold_kb: 200,        // warning threshold
      status: "ok" | "warning" | "critical",  // ok: <80%, warning: 80-95%, critical: >95%
      staleness_days: 5,        // days since last update
      duplicate_ratio: 0.15,    // estimated duplicate content (0.0-1.0)
      suggested_action: "none" | "compress" | "archive" | "cleanup"
    },
    // ... more terminals
  ],
  system_total_kb: 856,
  warnings: ["conductor approaching threshold", "backend has stale content (14 days)"]
}
```

**Implementation:**
- Read all `docs/memory/*.md` files
- Calculate sizes (Buffer.byteLength)
- Detect staleness (file mtime vs current date)
- Estimate duplicates (simple heuristic: repeated patterns >3 occurrences)
- Suggest action based on status + staleness + duplicates

**Use case:** Session start → egyetlen hívás → fleet overview

---

### Tool #2: `mcp__spaceos-knowledge__compress_memory`

**Purpose:** Automatic memory compression with pattern detection

**Parameters:**
```typescript
{
  terminal: string,                                    // required
  strategy: "aggressive" | "moderate" | "conservative",  // compression level
  preserve_sections: string[],                         // section headers to preserve (optional)
  dry_run: boolean                                     // default: true (safety)
}
```

**Strategy levels:**

| Strategy | What gets removed | Preserve |
|----------|------------------|----------|
| **aggressive** | Review logs, cycle logs, completed task references, duplicate patterns | Session rituals, active task refs, key decisions |
| **moderate** | Review logs >7 days, cycle logs >14 days, completed tasks >30 days | Last 7 days review, last 14 days cycle, recent tasks |
| **conservative** | Only cycle logs >30 days, completed tasks >60 days | Everything else |

**Returns:**
```typescript
{
  success: boolean,
  original_size_kb: number,
  compressed_size_kb: number,
  reduction_ratio: number,           // 0.0-1.0 (e.g., 0.85 = 85% reduction)
  archived_content_summary: string,  // what was removed
  preview: string,                   // first 500 chars of compressed memory
  dry_run: boolean                   // was this a dry run?
}
```

**Pattern detection logic:**

```typescript
// Detect and remove these patterns:
const GARBAGE_PATTERNS = [
  /## Nightwatch Cycle #\d+.*?(?=##|$)/gs,           // Nightwatch cycle logs
  /## Review Log.*?(?=##|$)/gs,                      // Review logs
  /\*\*Task:\*\* MSG-\w+-\d+ \(completed.*?\)/g,     // Completed task refs
  /\[DONE\].*?MSG-\w+-\d+.*?(?=\n\n)/gs,             // DONE outbox logs
];

// Preserve these sections (always):
const PRESERVE_HEADERS = [
  "## Session Start Ritual",
  "## Active Tasks",
  "## Key Decisions",
  "## Strategic Context"
];
```

**Implementation:**
1. Read `docs/memory/{terminal}.md`
2. If `dry_run: true`:
   - Apply pattern detection
   - Calculate sizes
   - Return preview WITHOUT writing
3. If `dry_run: false`:
   - Apply compression
   - Backup original to `docs/memory/archive/{terminal}_{date}.md`
   - Write compressed version
   - Return results

**Use case:** Havi maintenance → compress_memory dry_run → review → execute

---

### Tool #3: `mcp__spaceos-knowledge__extract_patterns`

**Purpose:** Cross-terminal pattern mining for knowledge extraction

**Parameters:**
```typescript
{
  terminal: string,                                              // required (or "all" for fleet-wide)
  min_frequency: number,                                         // default: 3 (pattern must appear 3+ times)
  pattern_types: ("workflow" | "decision" | "error_resolution")[]  // what to look for
}
```

**Pattern types:**

| Type | Regex / Heuristic | Example |
|------|-------------------|---------|
| **workflow** | `## Workflow.*?(?=##\|$)` or numbered lists | "1. Read inbox 2. Process 3. Create DONE" |
| **decision** | `Decision:` or `Chosen:` or `APPROVE/REJECT` | "Decision: Option A (automated)" |
| **error_resolution** | `Error:.*?Fix:` or `Problem:.*?Solution:` | "Error: MCP timeout → Fix: retry logic" |

**Returns:**
```typescript
{
  patterns: [
    {
      type: "workflow",
      content: "Session start ritual: 1. Check inbox 2. Review context 3. Plan tasks",
      frequency: 5,                          // appeared 5 times
      terminals: ["conductor", "backend"],   // where it appeared
      suggested_tier: "shared",              // shared | warm | cold
      suggested_doc: "TERMINAL_SESSION_RITUAL.md"  // knowledge doc suggestion
    },
    // ... more patterns
  ],
  total_patterns_found: 12,
  terminals_scanned: ["conductor"]
}
```

**Implementation:**
1. Read memory file(s)
2. Apply regex patterns for each type
3. Count frequencies (deduplicate similar patterns)
4. Suggest tier based on frequency:
   - `>5 occurrences` → **shared** (docs/knowledge/patterns/)
   - `3-5 occurrences` → **warm** (terminal-specific memory)
   - `<3 occurrences` → **cold** (archive)
5. Suggest doc name (heuristic: extract key verbs/nouns)

**Use case:** Quarterly knowledge extraction → extract_patterns → Librarian review → create knowledge docs

---

## Deliverable

**1. Code Implementation:**
- 3 new MCP tools in `spaceos-nexus/knowledge-service/src/memoryTools.ts`
- Integrate into MCP server (`src/mcp.ts`)
- Error handling (file not found, invalid terminal, etc.)

**2. Testing:**
- Unit tests for each tool (`__tests__/memoryTools.test.ts`)
- Test with real memory files (conductor, backend, librarian)
- Dry-run safety verification

**3. Documentation:**
- Update `get_capabilities` to return new tools
- Add JSDoc comments for each function
- Example usage in code comments

**Output Location:** `/opt/spaceos/terminals/backend/outbox/` as DONE message

---

## References

- **Librarian request:** MSG-ROOT-031
- **Emergency cleanup evidence:** MSG-LIBRARIAN-026 (672KB → 108KB)
- **Memory files:** `docs/memory/*.md`
- **MCP integration:** `spaceos-nexus/knowledge-service/src/mcp.ts`

---

## Acceptance Criteria

✅ **memory_health_report** tool implemented and tested (fleet overview)
✅ **compress_memory** tool implemented with dry_run support (safe compression)
✅ **extract_patterns** tool implemented (pattern mining)
✅ **get_capabilities** returns new tools (MCP visibility)
✅ **Librarian can test** new tools successfully

---

**Priority:** MEDIUM (infrastructure improvement, not blocking)
**Timeline Impact:** ~7 hours implementation
**ROI:** 257-343% annual (75-120 min/month time savings)

⚙️ **Root Terminal — Memory Management Tools Implementation Request**
