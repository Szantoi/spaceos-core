---
title: "Dev D — TASK-12-01 Assignment Sheet"
subtitle: "Episode Storage — SQLite Schema + store_experience() MCP Tool"
created: 2026-03-08
updated: 2026-03-09
assigned_to: "Dev D"
priority: "P0"
epic: "EPIC-12"
phase: "M02 — Phase 1: Core Functionality"
status: "🟢 READY FOR ASSIGNMENT"
effort_estimate: "15-17 hours"
ac_count: 4
---

# 🚀 Dev D — TASK-12-01 Assignment

**Task:** TASK-12-01 (Episode Storage — SQLite Schema + store_experience() MCP Tool)
**Epic:** EPIC-12 (Episodic Memory Layer: Session storage + Semantic search)
**Phase:** M02 Phase 1 — Core Functionality
**Priority:** P0 (foundation for all memory features)
**Effort Estimate:** 15-17 hours (2 days)

---

## 🎯 Your Mission

Implement the **foundational episode storage system** in SQLite. This task creates:

- `episodes` table (session storage with mandatory fields + constraints)
- `store_experience()` MCP tool (persist agent sessions as episodes)
- Supporting indexes for TASK-12-02/03 (FTS5 & ChromaDB)
- EpisodeService layer (retrieve, query episodes)
- 10+ unit tests covering happy path, errors, edge cases

This is the **critical foundation** for TASK-12-02 (FTS5 search) and TASK-12-03 (ChromaDB semantic search).

**Why This Matters:**

- **Learning System:** Episodes stored = future agents can learn from past work
- **Session Resumption:** Pause/resume workflows with full context
- **Audit Trail:** Immutable record of agent decision-making
- **Performance Requirement:** Must handle 5MB payloads + query < 10ms

---

## 📋 What You'll Build

### 1. SQLite Schema (episodes table)

```sql
CREATE TABLE episodes (
  id TEXT PRIMARY KEY,
  session_id TEXT NOT NULL,
  domain TEXT NOT NULL,          -- e.g., "discovery", "engineering", "testing"
  track TEXT NOT NULL,           -- e.g., "user-story-1", "component-x", "bug-fix-y"
  phase TEXT NOT NULL,           -- e.g., "ideation", "implementation", "review"
  tool_calls_json TEXT,          -- JSON: [{ tool: "search", args: {...}, result: {...} }, ...]
  artifacts_json TEXT,           -- JSON: [{ type: "document", path: "...", hash: "..." }, ...]
  outcome_summary TEXT NOT NULL, -- Plain text: "Found 3 design patterns, chose React"
  created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Index for common queries (domain + track + session filtering)
CREATE INDEX idx_episodes_domain_track_session
  ON episodes(domain, track, session_id);

-- Index for session-based queries (TASK-12-02 will search differently)
CREATE INDEX idx_episodes_session
  ON episodes(session_id);
```

**Why this structure?**

- `id`: Unique episode identifier (UUID recommended)
- `session_id`: Link to agent session (FK to sessions table from EPIC-09)
- `domain`: Semantic category (helps filtering)
- `track`: Execution context (parallel vs sequential)
- `phase`: State within track (ideation → impl → review)
- `tool_calls_json`: Detailed record of what happened
- `artifacts_json`: Outputs created (for audit trail)
- `outcome_summary`: Human-readable summary (for semantic search in TASK-12-03)
- Created indexes for common query patterns (see TASK-12-02/03)

---

### 2. TypeScript Service (EpisodeStore)

```typescript
// src/episodic/EpisodeStore.ts

import { Database } from "better-sqlite3";

export interface Episode {
  id: string;
  sessionId: string;
  domain: string;
  track: string;
  phase: string;
  toolCalls: unknown[];
  artifacts: unknown[];
  outcomeSummary: string;
  createdAt: Date;
}

export class EpisodeStore {
  constructor(private db: Database) {
    this.initializeSchema();
  }

  private initializeSchema(): void {
    // Create table if not exists (idempotent)
    this.db.exec(`
      CREATE TABLE IF NOT EXISTS episodes (
        id TEXT PRIMARY KEY,
        session_id TEXT NOT NULL,
        domain TEXT NOT NULL,
        track TEXT NOT NULL,
        phase TEXT NOT NULL,
        tool_calls_json TEXT,
        artifacts_json TEXT,
        outcome_summary TEXT NOT NULL,
        created_at DATETIME DEFAULT CURRENT_TIMESTAMP
      );

      CREATE INDEX IF NOT EXISTS idx_episodes_domain_track_session
        ON episodes(domain, track, session_id);

      CREATE INDEX IF NOT EXISTS idx_episodes_session
        ON episodes(session_id);
    `);
  }

  async storeExperience(params: {
    sessionId: string;
    domain: string;
    track: string;
    phase: string;
    toolCalls?: unknown[];
    artifacts?: unknown[];
    outcomeSummary: string;
  }): Promise<{ episodeId: string; createdAt: Date }> {
    const episodeId = this.generateEpisodeId(); // UUID v4

    // AC-3: Enforce size limit (max 5MB total)
    const totalSize = JSON.stringify(params).length;
    if (totalSize > 5 * 1024 * 1024) {
      throw new Error(`Episode payload exceeds 5MB limit (got ${totalSize} bytes)`);
    }

    const stmt = this.db.prepare(`
      INSERT INTO episodes (
        id, session_id, domain, track, phase,
        tool_calls_json, artifacts_json, outcome_summary
      ) VALUES (?, ?, ?, ?, ?, ?, ?, ?)
    `);

    stmt.run(
      episodeId,
      params.sessionId,
      params.domain,
      params.track,
      params.phase,
      JSON.stringify(params.toolCalls || []),
      JSON.stringify(params.artifacts || []),
      params.outcomeSummary
    );

    return {
      episodeId,
      createdAt: new Date(),
    };
  }

  async getEpisode(episodeId: string): Promise<Episode | undefined> {
    const stmt = this.db.prepare(`
      SELECT
        id, session_id, domain, track, phase,
        tool_calls_json, artifacts_json, outcome_summary, created_at
      FROM episodes
      WHERE id = ?
    `);

    const row = stmt.get(episodeId) as any;
    if (!row) return undefined;

    return {
      id: row.id,
      sessionId: row.session_id,
      domain: row.domain,
      track: row.track,
      phase: row.phase,
      toolCalls: JSON.parse(row.tool_calls_json || "[]"),
      artifacts: JSON.parse(row.artifacts_json || "[]"),
      outcomeSummary: row.outcome_summary,
      createdAt: new Date(row.created_at),
    };
  }

  async getEpisodesBySession(sessionId: string): Promise<Episode[]> {
    const stmt = this.db.prepare(`
      SELECT
        id, session_id, domain, track, phase,
        tool_calls_json, artifacts_json, outcome_summary, created_at
      FROM episodes
      WHERE session_id = ?
      ORDER BY created_at DESC
    `);

    const rows = stmt.all(sessionId) as any[];
    return rows.map(row => ({
      id: row.id,
      sessionId: row.session_id,
      domain: row.domain,
      track: row.track,
      phase: row.phase,
      toolCalls: JSON.parse(row.tool_calls_json || "[]"),
      artifacts: JSON.parse(row.artifacts_json || "[]"),
      outcomeSummary: row.outcome_summary,
      createdAt: new Date(row.created_at),
    }));
  }

  private generateEpisodeId(): string {
    // UUID v4 or timestamp-based ID
    // Recommendation: use `crypto.randomUUID()` (Node.js 15.7+)
    return `ep_${Date.now()}_${Math.random().toString(36).substring(7)}`;
  }
}
```

---

### 3. MCP Tool (store_experience)

```typescript
// In mcpRouter or a new mcpTool
// Tool definition for MCP schema

{
  name: "store_experience",
  description: "Record an agent experience (episode) for later retrieval and learning",
  inputSchema: {
    type: "object",
    properties: {
      session_id: { type: "string", description: "Session ID from agent context" },
      domain: {
        type: "string",
        enum: ["discovery", "engineering", "testing", "deployment"],
        description: "Problem domain"
      },
      track: {
        type: "string",
        description: "Execution context (e.g., 'user-story-1', 'bug-fix-xyz')"
      },
      phase: {
        type: "string",
        enum: ["ideation", "implementation", "review", "refinement"],
        description: "Phase within track"
      },
      tool_calls: {
        type: "array",
        description: "Tool calls made during this phase (optional)",
        items: { type: "object" }
      },
      artifacts: {
        type: "array",
        description: "Artifacts created (optional)",
        items: { type: "object" }
      },
      outcome_summary: {
        type: "string",
        description: "What was accomplished (1-500 chars)"
      }
    },
    required: ["session_id", "domain", "track", "phase", "outcome_summary"]
  }
}
```

**Tool Handler:**

```typescript
// MCP server handler
mcpRouter.tool("store_experience", async (params) => {
  const result = await episodeStore.storeExperience({
    sessionId: params.session_id,
    domain: params.domain,
    track: params.track,
    phase: params.phase,
    toolCalls: params.tool_calls,
    artifacts: params.artifacts,
    outcomeSummary: params.outcome_summary,
  });

  return {
    type: "text",
    text: JSON.stringify(result, null, 2),
  };
});
```

---

## ✅ Acceptance Criteria (4 AC Total)

### AC-1: Schema Created ✅

- [ ] `episodes` table exists in agent.db
- [ ] All 9 columns present (id, session_id, domain, track, phase, tool_calls_json, artifacts_json, outcome_summary, created_at)
- [ ] Proper data types (TEXT, DATETIME)
- [ ] Primary key: `id`
- [ ] Two indexes created and working

### AC-2: Index Performance ✅

- [ ] Index on (domain, track, session_id) exists
- [ ] Index on (session_id) exists
- [ ] Query by domain+track+session: < 10ms on 1000 episodes ✅
- [ ] Query by session_id: < 5ms on 1000 episodes ✅
- [ ] Performance verified with benchmark test

### AC-3: Size Enforcement ✅

- [ ] Max payload: 5MB per episode
- [ ] Validation enforced in `storeExperience()`
- [ ] Error thrown if exceeded: `episode_size_exceeded`
- [ ] Error message: "Episode payload exceeds 5MB limit (got X bytes)"
- [ ] Unit test: insert 5MB episode (pass), 5.1MB episode (fail)

### AC-4: MCP Tool Functional ✅

- [ ] `store_experience` tool registered in MCP router
- [ ] Input schema validated (required fields)
- [ ] Tool called: stores episode + returns `{ episodeId, createdAt }`
- [ ] Types exported: Episode interface for TASK-12-02
- [ ] 10+ unit tests passing

---

## 📂 Your Deliverables

| Artifact | Location | Type | What |
|:---------|:---------|:-----|:-----|
| EpisodeStore class | `src/episodic/EpisodeStore.ts` | TypeScript | Schema init + insert/get methods |
| Episode interface | `src/episodic/types.ts` | TypeScript | Export Episode type for TASK-12-02 |
| MCP tool handler | `src/mcp/mcpRouter.ts` (or new file) | TypeScript | `store_experience` tool |
| Migration script | `src/episodic/migrations/003_episodes.sql` | SQL | Idempotent migration |
| Unit tests | `src/tests/unit/episode.schema.test.ts` | TypeScript | 10+ test cases |
| Implementation summary | `Docs/.../implementation-summary/TASK-12-01-implementation-summary.md` | Markdown | What you built + decisions |

---

## 🎓 Resources & References

| Resource | Location | Purpose |
|:---------|:---------|:---------|
| **EPIC-12 Router** | [EPIC-12-COORDINATION-ROUTER.md](../EPIC-12-COORDINATION-ROUTER.md) | Full specification |
| **agent.db schema** | [database/agent.db](../../../../database/agent.db) | Where you'll add table |
| **EPIC-09 patterns** | [database/standards/01-delivery/EPIC-09/](../../../../database/standards/01-delivery/EPIC-09/) | Migration framework |
| **TypeScript setup** | [tsconfig.json](../../../../tsconfig.json) | Strict mode config |
| **MCP tool patterns** | [src/mcp/mcpRouter.ts](../../../../src/mcp/mcpRouter.ts) | How to register tools |

---

## 🔄 Development Workflow

### Step 1: Study (45 min)

- [ ] Read this assignment (full understanding)
- [ ] Review agent.db schema (understand existing tables)
- [ ] Review EPIC-09 migration framework
- [ ] Sketch design: episodes table structure + queries

### Step 2: Design (30 min)

- [ ] Draft EpisodeStore class
- [ ] Draft MCP tool input/output schema
- [ ] Plan unit tests (schema, size limits, insert/retrieve)

### Step 3: Implement (5h)

- [ ] Create migration script (idempotent)
- [ ] Implement EpisodeStore.ts
- [ ] Register MCP tool
- [ ] Write unit tests (10+ cases)

### Step 4: Validate (1h 15m)

- [ ] Schema exists + indexes working
- [ ] Size validation functional
- [ ] MCP tool works (store + return)
- [ ] 10+ tests passing
- [ ] Performance verified (<10ms queries)
- [ ] TypeScript: strict mode, no errors

### Step 5: Submit (30 min)

- [ ] Create implementation summary
- [ ] Commit to branch (task/epic12-01-episode-schema)
- [ ] Submit completion report
- [ ] Link to PR

**Total: 8 hours**

---

## 📝 Daily Standup Template

Submit to: `feedback/DEV-D-TASK-12-01-STANDUP-[DATE].md`

```markdown
---
# Dev D — TASK-12-01 Standup [Date]

## ✅ Completed Today
- [What you finished: AC count, sections]
- Example: "Migration script + EpisodeStore.ts complete, AC-1/2/3 verified"

## 🔄 In Progress
- [Current section: design | implementation | testing]

## 🚫 Blockers
- [None | or list issues]

## 📅 Tomorrow's Plan
- [What's next]

## 🔗 PR/Commit
- [Branch: task/epic12-01-episode-schema]
- [Latest commit: ...]
```

---

## ⏰ Timeline

```
2026-03-11 (Day 4) — TASK-12-01 Start
  │
  ├─ Study + Design (1.25h)
  ├─ Implementation (5h)
  ├─ Testing + Validation (1.75h)
  │
  └─ 2026-03-11 18:00 UTC: Submit completion

2026-03-12 (Day 5) — TASK-12-02 Start
  │
  └─ Build on TASK-12-01 (FTS5 search table)
```

---

## 🎯 Success Criteria

**You're done when:**

- ✅ All 4 AC verified
- ✅ `episodes` table created + indexed
- ✅ Size validation working
- ✅ `store_experience` MCP tool functional
- ✅ 10+ unit tests passing
- ✅ Episode interface exported
- ✅ TypeScript: strict mode, no `any`
- ✅ Peer review passed

---

## 💡 Pro Tips

- **Migration Framework:** Follow EPIC-09 patterns (idempotent, versioned)
- **Index Verification:** Use `EXPLAIN QUERY PLAN` to verify indexes are used
- **Size Limit:** Remember 5MB is total JSON payload, not just outcome_summary
- **UUID Generation:** Node.js 15.7+ has `crypto.randomUUID()`
- **JSON Storage:** SQLite handles JSON well, but document why you're storing JSON (extensibility for TASK-12-02/03)

---

## 📞 Questions?

**Before Starting:**

- Understand EPIC-09 migration patterns
- Understand MCP tool registration
- Understand SQLite indexes

**During Development:**

- Blocker? Post immediately (don't wait for standup)
- AC unclear? Post question + reference number

---

## 🚀 Ready?

✅ **Prerequisites:** EPIC-09 ✅ (agent.db available)
✅ **Instructions:** Read this sheet + resources above
✅ **Timeline:** 8 hours (2026-03-11)
✅ **Solo Track:** Yes (independent path, parallel EPIC-11)

**LET'S BUILD EPISODIC MEMORY!** 💾

---

**Assignment Date:** 2026-03-08
**Start Date:** 2026-03-11
**Solo Track:** Yes (EPIC-12 Phase 1)
**Status:** 🟢 READY FOR ASSIGNMENT
**Next Task (After Completion):** TASK-12-02 (FTS5 Full-Text Search)
