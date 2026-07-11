---
title: "⚡ Dev D — EPIC-12 Quick Start (START TODAY!)"
type: "kickoff-card"
created: 2026-03-11
status: "🚨 START NOW — 09:00 UTC"
effort: "8 hours → TASK-12-01 (Episode Schema)"
---

# ⚡ Dev D — EPIC-12 Quick Start Card

**🚨 START: 2026-03-11 09:00 UTC (TODAY!)**

**Your mission:** Build SQLite episode storage + FTS5 search + ChromaDB semantic search (4 tasks, 4 days)

---

## 🎯 YOUR 5-MINUTE SETUP

### 1. Read These (5 min)
- [DEVELOPER_ASSIGNMENT_DISPATCH_2026-03-11.md](./DEVELOPER_ASSIGNMENT_DISPATCH_2026-03-11.md) — Full context
- [dev-d/EPIC-12-INSTRUCTIONS.md](../coordinator/feedback/STANDUP-TEMPLATE.md) — Your epic brief
- This card (what you're reading now)

### 2. Open Your Files (2 min)
```
📁 dev-d/TASK-12-01/
   ├─ README.md               ← Read first
   ├─ TASK-12-01-ASSIGNMENT.md
   └─ feedback/              ← Where you post standups
```

### 3. Clone the Branch (2 min)
```bash
cd ~/Development/JoineryTech.McpServer
git fetch origin
git checkout -b epic-12-episode-storage
```

### 4. Run Tests (1 min)
```bash
npm test -- src/episodic  # Should fail (not implemented yet!)
```

---

## 📋 TODAY'S TASK: TASK-12-01 (Episode Schema & Storage)

**Duration:** 8 hours
**Deadline:** 2026-03-12 17:00 UTC (tomorrow evening)
**AC Count:** 4 criteria to pass

---

## 🛠️ WHAT YOU'LL BUILD TODAY

### File 1: `src/episodic/types.ts` (NEW)

```typescript
// Define these types:
export interface Episode {
  id: string;           // UUID
  session_id: string;   // links to agent_sessions
  timestamp: Date;
  content: string;      // max 5MB
  metadata: Record<string, unknown>;
  embedding_vector: number[] | null;
}

export interface EpisodeQuery {
  session_id?: string;
  limit?: number;
  after?: Date;
}
```

### File 2: `src/episodic/EpisodeStore.ts` (NEW)

```typescript
import Database from 'better-sqlite3';

export class EpisodeStore {
  private db: Database.Database;

  constructor(dbPath: string = 'agent.db') {
    this.db = new Database(dbPath);
  }

  async store(episode: Episode): Promise<void> {
    // 1. Validate size (< 5MB)
    // 2. INSERT into episodes table
    // 3. Return success
  }

  async getRecent(sessionId: string, limit = 10): Promise<Episode[]> {
    // SELECT * FROM episodes WHERE session_id = ? ORDER BY timestamp DESC LIMIT ?
  }

  async getAll(): Promise<Episode[]> {
    // SELECT * FROM episodes
  }
}
```

### File 3: `database/migrations/003_episodes_table.sql` (NEW)

```sql
CREATE TABLE IF NOT EXISTS episodes (
  id TEXT PRIMARY KEY,
  session_id TEXT NOT NULL,
  timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
  content TEXT NOT NULL,
  metadata JSON,
  embedding_vector BLOB,
  created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY(session_id) REFERENCES agent_sessions(id)
);

CREATE INDEX idx_episodes_session_timestamp
  ON episodes(session_id, timestamp DESC);

CREATE INDEX idx_episodes_content
  ON episodes(content);
```

### File 4: `tests/episodic/EpisodeStore.test.ts` (NEW)

```typescript
import { EpisodeStore } from '../../src/episodic/EpisodeStore';

describe('EpisodeStore', () => {
  let store: EpisodeStore;

  beforeEach(() => {
    store = new EpisodeStore(':memory:'); // in-memory DB for tests
  });

  it('should store an episode', async () => {
    const episode = {
      id: 'ep-1',
      session_id: 'sess-1',
      content: 'Test episode',
      metadata: {},
      embedding_vector: null,
      timestamp: new Date(),
    };
    await store.store(episode);
    // Verify insertion
  });

  it('should validate episode size (max 5MB)', async () => {
    const bigEpisode = {
      id: 'ep-big',
      session_id: 'sess-1',
      content: 'x'.repeat(6 * 1024 * 1024), // 6MB
      metadata: {},
      embedding_vector: null,
      timestamp: new Date(),
    };
    // Should throw error
    await expect(store.store(bigEpisode)).rejects.toThrow();
  });

  // Add 8+ more tests covering:
  // - Retrieve by session_id
  // - Index efficiency
  // - Performance (1000 episodes < 500ms)
});
```

---

## ✅ YOUR AC CHECKLIST (Must-Pass)

- [ ] **AC-1:** episodes table exists in SQLite with correct schema
- [ ] **AC-2:** Two indexes exist: (session_id, timestamp) + content
- [ ] **AC-3:** Size validation: reject episodes > 5MB
- [ ] **AC-4:** Unit tests: ≥10 tests, all green, performance proven

---

## 🎬 ACTION PLAN (Hour-by-Hour)

```
09:00–09:15  Read this card + kickoff task assignment
09:15–09:30  Create types.ts (Episode, EpisodeQuery interfaces)
09:30–10:00  Create EpisodeStore.ts skeleton (initialize DB connection)
10:00–11:00  Implement store() method + validation logic
11:00–11:30  Study WorkflowStateTracker.ts (your pattern reference)
11:30–12:00  Write SQL migration (003_episodes_table.sql)
12:00–12:30  Implement getRecent(), getAll()
12:30–14:00  Lunch break
14:00–15:00  Write unit tests (10+ tests)
15:00–16:00  Run tests, fix failures, optimize performance
16:00–16:30  Verify AC (all 4 passing)
16:30–17:00  Commit + PR + post completion standup
```

---

## 📝 STANDUP TEMPLATE (Post 3x/day)

### 09:00 Morning Standup
```markdown
# Dev D Standup — 2026-03-11 (Morning)

## Status
- [ ] TASK-12-01: 0% (Step 1/8) — Starting types.ts
- Blocker: None
- Confidence: 🟢 Green

## Completed
- (none yet, just started)

## Next 3h Plans
- [ ] types.ts (Episode interface)
- [ ] EpisodeStore constructor + DB connection
- [ ] Study WorkflowStateTracker pattern
```

### 12:00 Midday Standup
```markdown
# Dev D Standup — 2026-03-11 (Midday)

## Status
- [ ] TASK-12-01: 50% (Step 4/8) — Testing query methods
- Blocker: None
- Confidence: 🟢 Green

## Completed
- ✅ types.ts (interface definitions)
- ✅ EpisodeStore skeleton + store() method
- ✅ Migration SQL file created

## Next 3h Plans
- [ ] Unit tests (10+ test cases)
- [ ] Performance validation
- [ ] PR creation
```

### 18:00 Evening Standup
```markdown
# Dev D Standup — 2026-03-11 (Evening)

## Status
- [ ] TASK-12-01: 95% (Step 8/8) — Ready for code review
- Blocker: None
- Confidence: 🟢 Green

## Completed
- ✅ All AC verified (4/4 passing)
- ✅ 12 unit tests (all green)
- ✅ Performance: 1000 episodes in 380ms ✓
- ✅ PR created: https://github.com/repo/pull/XXX

## Completion Report
- See: https://github.com/repo/pull/XXX
- Test coverage: 87%
- Ready for: Tech Lead + Architect review
```

---

## 🚀 GIT WORKFLOW

```bash
# 1. Create your feature branch (already done)
git checkout -b epic-12-episode-storage

# 2. Work on files (types.ts, EpisodeStore.ts, migration, tests)
# ... make commits as you progress

# 3. When ready for review
git add src/episodic/ database/migrations/ tests/episodic/
git commit -m "feat(episodic): Episode storage schema + SQLite integration (Task 12-01)"

# 4. Push to origin
git push -u origin epic-12-episode-storage

# 5. On GitHub: Create PR
#    Title: "EPIC-12-01: Episode Storage (8h)"
#    Reviewers: @techLead, @architect
```

---

## 🔗 KEY REFERENCES

| Resource | Location | Why You Need It |
|:---------|:--------:|:---------|
| WorkflowStateTracker Pattern | `src/metadata/WorkflowStateTracker.ts` | Your SQLite service template |
| Database Schema | `database/schema/` | See existing table patterns |
| Unit Test Examples | `tests/metadata/WorkflowStateTracker.test.ts` | Test structure + better-sqlite3 usage |
| Standup Template | `coordinator/feedback/STANDUP-TEMPLATE.md` | Post-standup format |

---

## 🆘 IF YOU GET STUCK

1. **Schema question?** Read `database/schema/` examples
2. **SQLite/better-sqlite3 question?** Check WorkflowStateTracker.ts
3. **Test question?** Look at existing episodic tests
4. **Design decision?** Post in #architecture, tag Tech Lead
5. **Blocker (> 1h)?** Escalate to Tech Lead immediately (Slack)

---

## ✅ FINAL CHECKLIST

Before posting "COMPLETION" standup:

- [ ] types.ts written + exports correct
- [ ] EpisodeStore.ts implements all methods
- [ ] Migration SQL runs without errors
- [ ] 10+ unit tests written + all green
- [ ] Performance: 1000 episodes insert < 500ms
- [ ] Code committed + PR created
- [ ] Linked to GitHub PR in completion report
- [ ] Awaiting Tech Lead review

---

**🟢 YOU'RE READY TO START. GO CODE! 🚀**

**Questions?** Slack @techLead or reply in #architecture

**Estimated completion:** 2026-03-12 17:00 UTC (tomorrow evening)

