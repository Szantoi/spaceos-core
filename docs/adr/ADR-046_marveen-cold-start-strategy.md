# ADR-046 — Marveen Cold Start Strategy for SpaceOS Nexus

> **Status:** ✅ IMPLEMENTED — 2026-06-21
> **Architect:** Gábor (Founder) · **Drafted by:** Architect (Claude Opus 4.5 session)
> **Implemented by:** Backend terminal (Claude Sonnet) + Root (Claude Opus 4.5)
> **Scope:** Major Feature · 4 implementation tracks · ~4-6 dev days
> **Supersedes:** — · **Related:** ADR-044 (Knowledge Service), ADR-045 (MCP Tools)
> **Source:** Marveen Project (https://github.com/Szotasz/marveen)

---

## 1. Context

### 1.1 Problem Statement

SpaceOS agent sessions currently operate with **full context carry-over** through tmux session persistence. This creates several issues:

| Issue | Impact |
|---|---|
| **Context pollution** | Old context accumulates, degrading response quality |
| **Memory bloat** | Large context windows increase token costs and latency |
| **Session fragility** | Crash recovery requires manual context reconstruction |
| **Cross-session amnesia** | Lessons learned in one session don't transfer to new ones |

### 1.2 Marveen Solution

The Marveen project (https://github.com/Szotasz/marveen) pioneered an alternative: **cold start with intelligent memory**. Instead of carrying full conversation history, sessions start fresh and reconstruct context from a tiered memory system.

```
Traditional Approach:          Marveen Approach:
┌─────────────────────┐       ┌─────────────────────┐
│ Session Start       │       │ Session Start       │
│    ↓                │       │    ↓                │
│ Load full history   │       │ Load CLAUDE.md      │
│ (10K-100K tokens)   │       │    ↓                │
│    ↓                │       │ Query Memory API    │
│ Continue work       │       │ (hot + warm tiers)  │
│    ↓                │       │    ↓                │
│ Context grows       │       │ Build context       │
│    ↓                │       │ (~2K-5K tokens)     │
│ Session crash       │       │    ↓                │
│    ↓                │       │ Work                │
│ Manual recovery     │       │    ↓                │
└─────────────────────┘       │ /retrospective or   │
                              │ /handoff on exit    │
                              │    ↓                │
                              │ Next session: fresh │
                              └─────────────────────┘
```

### 1.3 What Exists Today (2026-06-21)

| Component | Status | Location |
|---|---|---|
| `memoryStore.ts` FTS5-based memory | ✅ DONE | `spaceos-nexus/knowledge-service/src/pipeline/memoryStore.ts` |
| Memory types (semantic/episodic/procedural) | ✅ DONE | In memoryStore.ts |
| MCP tools (read_memory, write_memory, append_memory) | ✅ DONE | `spaceos-nexus/knowledge-service/src/mcp.ts` |
| Salience decay + cleanup | ✅ DONE | In memoryStore.ts |
| Marveen skills (retrospective, handoff) | ✅ DONE | `~/.claude/skills/` |
| Session start hooks | ❌ MISSING | **Track B** |
| Memory tier management (hot/warm/cold/shared) | ❌ PARTIAL | **Track A** |
| Session end hooks | ❌ MISSING | **Track B** |
| Daily digest generation | ❌ MISSING | **Track C** |
| Dashboard integration | ❌ MISSING | **Track D** |

### 1.4 Gap Analysis

The current `memoryStore.ts` provides basic FTS5 search but lacks:
1. **Tier semantics** — No hot/warm/cold/shared tier with decay policies
2. **Session lifecycle hooks** — No automatic context injection at start
3. **Retrospective integration** — No connection to skill proposal system
4. **Handoff integration** — No structured context transfer mechanism

---

## 2. Decision

### 2.1 Core Architecture Decisions

| Question | Decision | Rationale |
|---|---|---|
| **Session model** | **Cold start** with memory injection | Cleaner context, crash-resilient |
| **Memory tiers** | **4 tiers**: hot (24-48h), warm (1-2w), cold (long), shared (eternal) | Matches Marveen proven model |
| **Decay strategy** | **Salience-based** with configurable thresholds | Automatic pruning, manual override |
| **Context budget** | **5K tokens max** for session start context | Balance: quality vs cost |
| **Session hooks** | **Pre-start** (inject) + **Post-end** (save) | Automated lifecycle |
| **Skill integration** | **MCP tools** for retrospective/handoff | Consistent interface |

### 2.2 Memory Tier Semantics

```typescript
type MemoryTier = 'hot' | 'warm' | 'cold' | 'shared';

interface TierPolicy {
  tier: MemoryTier;
  maxAge: string;          // e.g., "48h", "14d", "365d", "forever"
  decayRate: number;       // salience decay per day (0.0-1.0)
  autoPromote: boolean;    // can auto-promote to higher tier?
  terminalScoped: boolean; // terminal-specific or global?
}

const TIER_POLICIES: Record<MemoryTier, TierPolicy> = {
  hot: {
    tier: 'hot',
    maxAge: '48h',
    decayRate: 0.15,       // 15% per day — aggressive decay
    autoPromote: true,     // can promote to warm if accessed frequently
    terminalScoped: true   // per-terminal
  },
  warm: {
    tier: 'warm',
    maxAge: '14d',
    decayRate: 0.05,       // 5% per day
    autoPromote: true,     // can promote to cold if high salience
    terminalScoped: true
  },
  cold: {
    tier: 'cold',
    maxAge: '365d',
    decayRate: 0.01,       // 1% per day — slow decay
    autoPromote: false,    // manual only
    terminalScoped: true
  },
  shared: {
    tier: 'shared',
    maxAge: 'forever',
    decayRate: 0,          // no decay
    autoPromote: false,
    terminalScoped: false  // global, cross-terminal
  }
};
```

### 2.3 Session Lifecycle Hooks

```
┌────────────────────────────────────────────────────────────────┐
│                    SESSION LIFECYCLE                           │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│  ┌─────────────┐                                               │
│  │  TRIGGER    │  InboxWatcher detects UNREAD                  │
│  └──────┬──────┘                                               │
│         ▼                                                      │
│  ┌─────────────┐                                               │
│  │ PRE-START   │  1. Load CLAUDE.md + NEXUS_USAGE.md           │
│  │   HOOK      │  2. Query hot + warm memories (terminal)      │
│  │             │  3. Query shared memories (global)            │
│  │             │  4. Build context (≤5K tokens)                │
│  │             │  5. Inject context into session prompt        │
│  └──────┬──────┘                                               │
│         ▼                                                      │
│  ┌─────────────┐                                               │
│  │  SESSION    │  Agent works on task                          │
│  │   ACTIVE    │  - Uses memory search as needed               │
│  │             │  - Saves hot memories inline                  │
│  └──────┬──────┘                                               │
│         ▼                                                      │
│  ┌─────────────┐                                               │
│  │ POST-END    │  1. Detect session end (DONE/BLOCKED/timeout) │
│  │   HOOK      │  2. Auto-save hot memory (task summary)       │
│  │             │  3. Offer /retrospective if significant work  │
│  │             │  4. Generate /handoff if context limit        │
│  │             │  5. Register idle status                      │
│  └─────────────┘                                               │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

### 2.4 Rejected Alternatives

| Alternative | Why Rejected |
|---|---|
| **A) Full session replay** | Token-expensive, context pollution, fragile |
| **B) Simple file-based memory** | No search, no decay, no tier semantics |
| **C) External vector DB only** | Semantic search alone misses recent context; hybrid needed |
| **D) Manual memory management** | Cognitive load on agents, inconsistent adoption |

---

## 3. Technical Specification

### 3.1 Database Schema Extension

```sql
-- Extend memories table with tier support
ALTER TABLE memories ADD COLUMN tier TEXT NOT NULL DEFAULT 'hot'
  CHECK (tier IN ('hot', 'warm', 'cold', 'shared'));

ALTER TABLE memories ADD COLUMN promoted_from TEXT;  -- previous tier
ALTER TABLE memories ADD COLUMN promotion_count INTEGER NOT NULL DEFAULT 0;
ALTER TABLE memories ADD COLUMN last_promotion_at TEXT;

-- Shared memories table (cross-terminal)
CREATE TABLE IF NOT EXISTS shared_memories (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  content TEXT NOT NULL,
  keywords TEXT NOT NULL DEFAULT '',
  context TEXT,
  salience REAL NOT NULL DEFAULT 0.7,
  created_by TEXT NOT NULL,  -- terminal that created
  created_at TEXT NOT NULL DEFAULT (datetime('now')),
  accessed_at TEXT NOT NULL DEFAULT (datetime('now')),
  access_count INTEGER NOT NULL DEFAULT 0
);

CREATE VIRTUAL TABLE IF NOT EXISTS shared_memories_fts USING fts5(
  content,
  keywords,
  content='shared_memories',
  content_rowid='id'
);

-- Session history table (for retrospective analysis)
CREATE TABLE IF NOT EXISTS session_history (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  terminal TEXT NOT NULL,
  started_at TEXT NOT NULL,
  ended_at TEXT,
  end_reason TEXT,  -- 'done', 'blocked', 'timeout', 'handoff', 'crash'
  task_id TEXT,
  memories_injected INTEGER NOT NULL DEFAULT 0,
  memories_created INTEGER NOT NULL DEFAULT 0,
  tool_calls INTEGER NOT NULL DEFAULT 0,
  had_corrections BOOLEAN NOT NULL DEFAULT 0,
  retrospective_done BOOLEAN NOT NULL DEFAULT 0
);

CREATE INDEX idx_session_history_terminal ON session_history(terminal);
CREATE INDEX idx_session_history_started ON session_history(started_at DESC);
```

### 3.2 TypeScript Interfaces

```typescript
// ─── Memory Tier Types ───────────────────────────────────────────────────────

export type MemoryTier = 'hot' | 'warm' | 'cold' | 'shared';

export interface TieredMemory {
  id: number;
  tier: MemoryTier;
  type: MemoryType;
  source: MemorySource;
  content: string;
  keywords: string;
  terminal?: string;
  context?: string;
  salience: number;
  accessCount: number;
  createdAt: string;
  accessedAt: string;
  expiresAt?: string;
  promotedFrom?: MemoryTier;
  promotionCount: number;
  lastPromotionAt?: string;
}

export interface MemorySaveInput {
  tier: MemoryTier;
  type: MemoryType;
  source: MemorySource;
  content: string;
  keywords?: string;
  terminal?: string;
  context?: string;
  salience?: number;
}

// ─── Session Lifecycle Types ─────────────────────────────────────────────────

export interface SessionStartContext {
  terminal: string;
  taskId?: string;
  inboxMessageId?: string;
}

export interface SessionStartResult {
  contextInjected: boolean;
  memoriesLoaded: number;
  hotMemories: TieredMemory[];
  warmMemories: TieredMemory[];
  sharedMemories: TieredMemory[];
  contextTokens: number;
  contextMarkdown: string;
}

export interface SessionEndContext {
  terminal: string;
  endReason: 'done' | 'blocked' | 'timeout' | 'handoff' | 'crash';
  taskId?: string;
  summary?: string;
  hadCorrections: boolean;
  toolCallCount: number;
}

export interface SessionEndResult {
  memoriesSaved: number;
  retrospectiveTriggered: boolean;
  handoffGenerated: boolean;
  sessionId: number;
}

// ─── Retrospective Types ─────────────────────────────────────────────────────

export interface RetrospectiveProposal {
  type: 'skill' | 'memory' | 'workflow';
  action: 'create' | 'patch' | 'delete' | 'save' | 'retier';
  target: string;
  reason: string;
  content?: string;
  newTier?: MemoryTier;
}

export interface RetrospectiveResult {
  sessionSummary: string;
  proposals: RetrospectiveProposal[];
  approved: boolean;
  executedCount: number;
}

// ─── Handoff Types ───────────────────────────────────────────────────────────

export interface HandoffDocument {
  purpose: string;
  from: string;
  to: string;
  generatedAt: string;
  goal: string;
  currentProgress: string[];
  whatWorked: string[];
  whatDidntWork: string[];
  nextSteps: string[];
  relatedMemories: TieredMemory[];
  kanbanCards?: string[];
}

// ─── Daily Digest Types ──────────────────────────────────────────────────────

export interface DailyDigest {
  date: string;
  terminal: string;
  sessionsCount: number;
  tasksCompleted: string[];
  memoriesCreated: number;
  memoriesPromoted: number;
  memoriesDecayed: number;
  keyLearnings: string[];
  aiSummary: string;
}
```

### 3.3 API Endpoints

```typescript
// ─── Memory Tier Management ──────────────────────────────────────────────────

// GET /api/memories/tiered?terminal=backend&tiers=hot,warm&limit=20
// Returns memories filtered by tier

// POST /api/memories/save
// Body: MemorySaveInput
// Saves memory with tier assignment

// POST /api/memories/{id}/promote
// Body: { newTier: MemoryTier, reason: string }
// Promotes memory to higher tier

// POST /api/memories/decay
// Runs decay on all memories, returns count affected

// ─── Session Lifecycle ───────────────────────────────────────────────────────

// POST /api/session/start-context
// Body: SessionStartContext
// Returns: SessionStartResult
// Builds and returns startup context for injection

// POST /api/session/end
// Body: SessionEndContext
// Returns: SessionEndResult
// Handles session end: saves memory, triggers retrospective

// ─── Retrospective Integration ───────────────────────────────────────────────

// POST /api/retrospective/analyze
// Body: { terminal: string, sessionId: number }
// Returns: RetrospectiveProposal[]

// POST /api/retrospective/execute
// Body: { proposals: RetrospectiveProposal[], approved: string[] }
// Executes approved proposals

// ─── Handoff Integration ─────────────────────────────────────────────────────

// POST /api/handoff/generate
// Body: { terminal: string, purpose: string, target?: string }
// Returns: HandoffDocument

// ─── Daily Digest ────────────────────────────────────────────────────────────

// POST /api/digest/generate
// Body: { terminal: string, date?: string }
// Returns: DailyDigest

// GET /api/digest/{terminal}/{date}
// Returns stored digest for date
```

### 3.4 MCP Tool Extensions

```typescript
const NEW_MCP_TOOLS = [
  // Memory tier tools
  {
    name: 'save_tiered_memory',
    description: 'Save memory with explicit tier assignment (hot/warm/cold/shared)',
    inputSchema: {
      type: 'object',
      properties: {
        tier: { type: 'string', enum: ['hot', 'warm', 'cold', 'shared'] },
        type: { type: 'string', enum: ['semantic', 'episodic', 'procedural'] },
        content: { type: 'string' },
        terminal: { type: 'string' },
        context: { type: 'string' },
      },
      required: ['tier', 'type', 'content'],
    },
  },
  {
    name: 'promote_memory',
    description: 'Promote memory to a higher tier (e.g., hot→warm, warm→cold)',
    inputSchema: {
      type: 'object',
      properties: {
        memoryId: { type: 'number' },
        newTier: { type: 'string', enum: ['warm', 'cold', 'shared'] },
        reason: { type: 'string' },
      },
      required: ['memoryId', 'newTier', 'reason'],
    },
  },
  {
    name: 'get_session_context',
    description: 'Get startup context for a terminal session (hot+warm+shared memories)',
    inputSchema: {
      type: 'object',
      properties: {
        terminal: { type: 'string' },
        maxTokens: { type: 'number', default: 5000 },
      },
      required: ['terminal'],
    },
  },

  // Retrospective tools
  {
    name: 'run_retrospective',
    description: 'Analyze session and generate skill/memory/workflow proposals',
    inputSchema: {
      type: 'object',
      properties: {
        terminal: { type: 'string' },
        scope: { type: 'string', enum: ['session', 'last-task', 'last-hour'] },
        focus: { type: 'string', enum: ['skills', 'memory', 'workflow', 'all'] },
      },
      required: ['terminal'],
    },
  },
  {
    name: 'apply_retrospective',
    description: 'Apply approved retrospective proposals',
    inputSchema: {
      type: 'object',
      properties: {
        terminal: { type: 'string' },
        approvedProposals: { type: 'array', items: { type: 'number' } },
      },
      required: ['terminal', 'approvedProposals'],
    },
  },

  // Handoff tools
  {
    name: 'generate_handoff',
    description: 'Generate HANDOFF.md for session/task transfer',
    inputSchema: {
      type: 'object',
      properties: {
        terminal: { type: 'string' },
        purpose: { type: 'string' },
        target: { type: 'string' },
        output: { type: 'string', enum: ['file', 'message'] },
      },
      required: ['terminal', 'purpose'],
    },
  },

  // Digest tools
  {
    name: 'generate_daily_digest',
    description: 'Generate daily summary of terminal activity and learnings',
    inputSchema: {
      type: 'object',
      properties: {
        terminal: { type: 'string' },
        date: { type: 'string', format: 'date' },
      },
      required: ['terminal'],
    },
  },
];
```

---

## 4. Implementation Plan

### Track A: Memory Tier Management (~1.5 dev days)

**Goal:** Extend memoryStore with tier semantics and decay policies.

| Step | Task | Files |
|---|---|---|
| A.1 | Add `tier` column to memories table | `memoryStore.ts` |
| A.2 | Implement tier-aware save/query | `memoryStore.ts` |
| A.3 | Implement tier promotion logic | `memoryStore.ts` |
| A.4 | Implement tier-specific decay rates | `memoryStore.ts` |
| A.5 | Add shared_memories table | `memoryStore.ts` |
| A.6 | Unit tests for tier operations | `__tests__/memoryStore.test.ts` |

**Output:** `memoryStore.ts` extended with tier support.

### Track B: Session Lifecycle Hooks (~1.5 dev days)

**Goal:** Implement pre-start and post-end hooks in sessionStarter.

| Step | Task | Files |
|---|---|---|
| B.1 | Create `sessionHooks.ts` module | `src/sessionHooks.ts` (new) |
| B.2 | Implement `buildStartContext()` | `sessionHooks.ts` |
| B.3 | Implement `handleSessionEnd()` | `sessionHooks.ts` |
| B.4 | Integrate hooks into `sessionStarter.ts` | `sessionStarter.ts` |
| B.5 | Add session_history table | `sessionHooks.ts` |
| B.6 | Integration tests | `__tests__/sessionHooks.test.ts` |

**Output:** Automatic context injection on start, memory save on end.

### Track C: Retrospective/Handoff Integration (~1 dev day)

**Goal:** Connect Marveen skills to Nexus MCP.

| Step | Task | Files |
|---|---|---|
| C.1 | Create `retrospective.ts` module | `src/retrospective.ts` (new) |
| C.2 | Implement proposal generation | `retrospective.ts` |
| C.3 | Implement proposal execution | `retrospective.ts` |
| C.4 | Create `handoff.ts` module | `src/handoff.ts` (new) |
| C.5 | Implement handoff document generation | `handoff.ts` |
| C.6 | Add MCP tools to mcp.ts | `mcp.ts` |

**Output:** MCP tools for retrospective and handoff.

### Track D: Dashboard Integration (~1 dev day)

**Goal:** Expose memory/session data to Datahaven.

| Step | Task | Files |
|---|---|---|
| D.1 | Add memory tier endpoint to REST API | `src/server.ts` |
| D.2 | Add session history endpoint | `src/server.ts` |
| D.3 | Implement daily digest generation | `src/digest.ts` (new) |
| D.4 | Integrate with Datahaven SSE | Update `server.ts` |
| D.5 | Add cron job for daily digest | `scripts/daily-digest.sh` |

**Output:** Dashboard shows memory stats, session history, daily digests.

---

## 5. Trade-off Analysis

### 5.1 Token Cost

| Scenario | Cold Start | Traditional |
|---|---|---|
| Session start | ~2-5K tokens (context) | ~10-50K tokens (history) |
| Per-turn | Normal | Normal |
| Session end | ~500 tokens (memory save) | 0 |
| **Total** | **Lower** | Higher |

**Net savings:** 50-80% on long sessions.

### 5.2 Latency

| Operation | Impact |
|---|---|
| Session start | +200-500ms (memory query + context build) |
| Session end | +100-300ms (memory save) |
| **Acceptable:** Yes — better than history load |

### 5.3 Storage

| Component | Growth |
|---|---|
| SQLite memory DB | ~10KB per memory, auto-decay |
| Session history | ~1KB per session |
| Daily digests | ~5KB per terminal per day |
| **Total:** ~1-5MB per terminal per month |

### 5.4 Risk Analysis

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Context too short | Medium | Task failure | Configurable budget, manual override |
| Memory decay too aggressive | Low | Lost context | Adjustable decay rates, promotion |
| Session end not detected | Medium | Memory loss | Multiple end triggers, timeout fallback |
| Retrospective overhead | Low | Token cost | Optional, user-triggered |

---

## 6. Migration Strategy

### Phase 1: Parallel Operation (Week 1)
- Deploy new memory tier system alongside existing
- Don't enforce cold start yet
- Collect metrics on memory usage

### Phase 2: Opt-in Cold Start (Week 2)
- Add `coldStart: boolean` to terminal config
- Enable for 1-2 low-risk terminals (explorer, librarian)
- Monitor quality and adjust

### Phase 3: Default Cold Start (Week 3+)
- Enable cold start for all non-priority terminals
- Priority terminals (root, conductor) remain warm start
- Full dashboard integration

---

## 7. Success Metrics

| Metric | Target | Measurement |
|---|---|---|
| Token cost per session | -50% | Compare before/after |
| Session start latency | <1s | Timer in hooks |
| Memory recall quality | >80% relevant | Manual review |
| Agent productivity | No decrease | Task completion rate |
| Crash recovery time | <30s | Session history |

---

## 8. References

- Marveen project: https://github.com/Szotasz/marveen
- Marveen retrospective skill: `~/.claude/skills/retrospective/SKILL.md`
- Marveen handoff skill: `~/.claude/skills/handoff/SKILL.md`
- Existing memoryStore: `spaceos-nexus/knowledge-service/src/pipeline/memoryStore.ts`
- Session starter: `spaceos-nexus/knowledge-service/src/sessionStarter.ts`
- MCP tools: `spaceos-nexus/knowledge-service/src/mcp.ts`
- NEXUS_USAGE.md: `spaceos-nexus/NEXUS_USAGE.md`

---

## 9. Implementation Status (2026-06-21)

### 9.1 Track Completion Summary

| Track | Component | Status | Key Files |
|---|---|---|---|
| **A** | Memory Tier Management | ✅ DONE | `memoryStore.ts` (hot/warm/cold/shared tier, decay, promotion) |
| **B** | Session Lifecycle Hooks | ✅ DONE | `sessionHooks.ts`, `sessionStarter.ts` (cold start injection) |
| **C** | Retrospective/Handoff | ✅ DONE | `retrospective.ts`, `handoff.ts`, `mcp.ts` (7 MCP tools) |
| **D** | Dashboard REST API | ✅ DONE | `digest.ts`, `server.ts` (8 REST endpoints) |

### 9.2 New Files Created

| File | Lines | Purpose |
|---|---|---|
| `src/sessionHooks.ts` | ~250 | Session lifecycle hooks (buildStartContext, handleSessionEnd) |
| `src/retrospective.ts` | 422 | Retrospective analysis and proposal execution |
| `src/handoff.ts` | 246 | HANDOFF.md document generation |
| `src/digest.ts` | 258 | Daily digest generation |
| `src/pipeline/immediatePipeline.ts` | 156 | Hybrid API immediate trigger |

### 9.3 Files Modified

| File | Changes |
|---|---|
| `src/pipeline/memoryStore.ts` | +tier column, tier-aware queries, tier promotion, decay policies |
| `src/sessionStarter.ts` | +buildStartContext integration, cold start injection |
| `src/mcp.ts` | +7 MCP tools (save_tiered_memory, promote_memory, get_session_context, run_retrospective, apply_retrospective, generate_handoff, generate_daily_digest) |
| `src/server.ts` | +8 REST endpoints, hybrid API trigger integration |

---

## 10. Hybrid API Immediate Trigger Pattern

### 10.1 Design Rationale

**Problem:** A Marveen-stílusú agent kommunikációban a DONE üzenetek feldolgozása periodikus fájl-scannelésen alapult (`watch-done.sh` cron */2 perc). Ez lassú és nem skálázódik.

**Megoldás (user javaslat):** Hybrid minta — agent API-n küldi a DONE-t, szerver írja a fájlt (artifact megmarad) ÉS azonnal triggereli a pipeline-t. A fájl-alapú watcher backup védelemnek megmarad.

### 10.2 Architecture

```
┌──────────────────────────────────────────────────────────────────────┐
│                    HYBRID API IMMEDIATE TRIGGER                       │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  Agent (Claude session)                                              │
│    │                                                                 │
│    ▼                                                                 │
│  POST /api/mailbox/:terminal/outbox                                  │
│    │                                                                 │
│    ├──► submitDone()                                                 │
│    │      └──► Fájlt ír (artifact megmarad)                         │
│    │           terminals/:terminal/outbox/YYYY-MM-DD_NNN_slug.md    │
│    │                                                                 │
│    ├──► SSE event emit                                               │
│    │      └──► broadcastToTerminal('root', 'done_submitted', ...)   │
│    │                                                                 │
│    └──► triggerImmediatePipelineAsync()  ← AZONNALI TRIGGER         │
│           │                                                          │
│           ├──► handleDoneReview()                                    │
│           │      └──► Dual Haiku review (APPROVE/REJECT)            │
│           │                                                          │
│           ├──► [if APPROVED] runPipeline()                          │
│           │      ├──► Archive (status: UNREAD → READ)               │
│           │      └──► Telegram notification                         │
│           │                                                          │
│           ├──► routeNextTerminal()                                   │
│           │      └──► Meghatározza a következő terminált            │
│           │                                                          │
│           ├──► createNextInbox()                                     │
│           │      └──► Inbox fájl a cél terminálnak                  │
│           │                                                          │
│           └──► injectToSession()                                     │
│                  └──► tmux send-keys (ha a terminál fut)            │
│                                                                      │
│  ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─  │
│                                                                      │
│  BACKUP: watchDone() (periodikus scan)                               │
│    └──► Fájl-alapú watcher (ha API trigger nem sikerül)             │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

### 10.3 Key Functions

```typescript
// src/pipeline/immediatePipeline.ts

/**
 * Fire-and-forget wrapper for API usage
 * Doesn't block the response, logs results
 */
export function triggerImmediatePipelineAsync(
  donePath: string,
  ctx: TaskContext
): void;

/**
 * Main pipeline trigger
 * Returns detailed result for monitoring
 */
export async function triggerImmediatePipeline(
  donePath: string,
  ctx: TaskContext
): Promise<ImmediateResult>;

interface ImmediateResult {
  triggered: boolean;
  reviewed: boolean;
  approved: boolean;
  pipelineRan: boolean;
  nextTerminal?: string;
  nextInboxPath?: string;
  injected: boolean;
  error?: string;
}
```

### 10.4 Benefits

| Benefit | Description |
|---|---|
| **Artifact megmarad** | Fájl-alapú artifact logoláshoz, audithoz, elemzéshez |
| **Azonnali trigger** | Nem kell várni a periodikus scan-re (2 percenként) |
| **Szerver routing** | Nem az agent dönti el a célt, hanem a szerver algoritmus |
| **Non-blocking** | API azonnal visszatér, pipeline háttérben fut |
| **Fallback védelem** | File watcher backup ha API trigger nem sikerül |

### 10.5 Usage in server.ts

```typescript
// server.ts line 412-419

// ADR-046: Hybrid API immediate trigger
// Fire-and-forget: runs review + pipeline in background
// File artifact already written, this just triggers immediate processing
triggerImmediatePipelineAsync(result.path, {
  from: terminal,
  taskId: task_id,
  summary,
});
```

---

## 11. Operational Notes

### 11.1 Service Management

```bash
# Build
cd /opt/spaceos/spaceos-nexus/knowledge-service
npm run build

# Start
npm start

# Health check
curl http://localhost:3456/health

# View logs
tail -f /tmp/knowledge-service.log
```

### 11.2 Memory Tier API

```bash
# Query memories by tier
curl "http://localhost:3456/api/memories/tiered?terminal=backend&tiers=hot,warm&limit=10"

# Save tiered memory
curl -X POST http://localhost:3456/api/memories/save \
  -H "Content-Type: application/json" \
  -d '{"tier":"hot","type":"semantic","source":"agent","content":"...","terminal":"backend"}'

# Promote memory
curl -X POST http://localhost:3456/api/memories/123/promote \
  -H "Content-Type: application/json" \
  -d '{"newTier":"warm","reason":"Frequently accessed"}'
```

### 11.3 Session Lifecycle API

```bash
# Get cold start context
curl -X POST http://localhost:3456/api/session/start-context \
  -H "Content-Type: application/json" \
  -d '{"terminal":"backend","taskId":"MSG-BACKEND-007"}'

# Handle session end
curl -X POST http://localhost:3456/api/session/end \
  -H "Content-Type: application/json" \
  -d '{"terminal":"backend","endReason":"done","taskId":"MSG-BACKEND-007"}'

# Get session history
curl "http://localhost:3456/api/session/history?terminal=backend&limit=10"
```

### 11.4 Daily Digest API

```bash
# Generate daily digest
curl -X POST http://localhost:3456/api/digest/generate \
  -H "Content-Type: application/json" \
  -d '{"terminal":"backend","date":"2026-06-21"}'

# Get stored digest
curl "http://localhost:3456/api/digest/backend/2026-06-21"
```

---

## 12. Future Enhancements

| Enhancement | Priority | Description |
|---|---|---|
| **Dashboard UI** | Medium | React komponensek a REST API-hoz |
| **Memory analytics** | Low | Tier distribution, decay rates, promotion history |
| **Cross-terminal memory sharing** | Medium | Shared tier használata koordinációra |
| **Automatic retrospective trigger** | Low | Session end után automatikus retrospective ha >X tool call |
