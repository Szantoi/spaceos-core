---
id: MSG-ARCHITECT-027-DONE
from: architect
to: root
type: done
priority: high
status: READ
ref: MSG-ARCHITECT-027
created: 2026-06-29
content_hash: 84d32c087cff795b33e0c525f6f8c88a404ab319b5fdcc7d3bc98db4b4f56194
---

# ✅ ADR-049 Dual-Session Architecture — REVIEW COMPLETE

## VERDICT: **APPROVE WITH CHANGES** (Critical Modifications Required)

Az ADR-049 koncepciója sound — chat/work separation költséghatékony és teljesítményt javít. **Azonban 4 kritikus blocker van, amelyeket a megvalósítás előtt meg kell oldani.**

---

## 1️⃣ MEMORY SYNC STRATEGY — ⚠️ RISKY

### Jelenlegi Megoldás: File Locking (`fs-ext/flock`)

**❌ Problémák:**

1. **Process Crash Lock Alatt** — Ha a work session crash-el az flock() alatt:
   - Lock nem oldódik fel automatikusan
   - Chat session deadlock-ba kerül
   - Recovery időt vár (nincs timeout definiálva)
   - **Risk Level:** HIGH

   ```typescript
   // CURRENT (problémás)
   async function appendToMemory(terminal: string, section: string, content: string) {
     const fd = await fs.open(memoryPath, 'r+');
     await flock(fd, 'ex');  // Process crash = lock soha nem jön vissza!
     try {
       // ... edit ...
     } finally {
       await flock(fd, 'un');
     }
   }
   ```

2. **Cross-Platform Kompatibilitás** — `fs-ext` UNIX-only
   - Windows deployment failure risk

3. **High Contention Scenario** — Ha 3+ work session + chat egyszerre ír
   - Lock wait time → performance degrade
   - Lock poisoning lehetséges (cascade failures)

### ✅ Ajánlott Megoldás: SQLite WAL Mode

```typescript
// SQLite-alapú memory store
import Database from 'better-sqlite3';

class MemoryStore {
  private db: Database.Database;

  constructor(terminal: string) {
    this.db = new Database(`terminals/${terminal}/memory.db`);
    // Enable WAL mode — atomic writes, crash-safe
    this.db.pragma('journal_mode = WAL');
    this.db.pragma('busy_timeout = 5000');
  }

  appendSection(section: 'chat' | 'work' | 'shared', content: string) {
    const stmt = this.db.prepare(
      'INSERT INTO memory_log (section, content, timestamp) VALUES (?, ?, ?)'
    );
    // Atomic write — SQLite handles crashes
    stmt.run(section, content, new Date().toISOString());
  }

  readSection(section: string): string[] {
    const stmt = this.db.prepare(
      'SELECT content FROM memory_log WHERE section = ? ORDER BY timestamp DESC LIMIT 100'
    );
    return stmt.all(section).map(r => r.content);
  }
}
```

**Előnyök:**
- ✅ Crash-safe (WAL journaling)
- ✅ ACID transactions
- ✅ Cross-platform
- ✅ Schema versioning (future-proof)

**Hátrányok:**
- Extra .db fájl (de SQLite lightweight)
- Query latency (negligible: <1ms)

### 🔧 CHANGE REQUEST #1

**Helyettesítsd az fs-ext file locking-ot SQLite WAL mode-dal:**
```yaml
Location: spaceos-nexus/knowledge-service/src/memorySync.ts
Change: flock → better-sqlite3 with WAL
Test: Unit tests for crash recovery + concurrent writes
```

---

## 2️⃣ SESSION LIFECYCLE — ⚠️ NEEDS ROBUST RECOVERY

### Jelenlegi Megoldás: Chat Auto-Restart + Memory Sync

**❌ Problémák:**

1. **Orphan Work Session Cleanup** — Ha work session crash-el:
   - Memória nem íródott meg
   - Chat nem tudja, hogy WORK-001 failed
   - Max 30m runtime limit? Nem definiálva
   - **Risk Level:** MEDIUM

   ```
   Scenario: WORK-001 long-running (5 hours)
   - Process crash után 4.9 óra múlva
   - Chat session nem tud róla
   - Memória sync timeout nélkül vár
   - Dashboard "Working..." marad örökcsinálva
   ```

2. **Partial Failure Handling** — Mi történik ha DONE outbox írása crash-el?
   - Task tekinthető completed-nek?
   - Duplicate DONE submission?
   - Nem dokumentálva

3. **Memory Consistency** — Ha chat és work egyszerre írnak
   - ADR: "Work wins, chat retry" — de:
   - Retry logic nem documentálva
   - Exponential backoff? Max retries?

### ✅ Szükséges Protokoll: Heartbeat + Timeout

```typescript
// Heartbeat writer (work session)
async function workHeartbeat(workSessionId: string) {
  while (isRunning) {
    memoryStore.recordHeartbeat(workSessionId, new Date());
    await sleep(10000); // 10s heartbeat
  }
}

// Orphan detector (chat session)
async function detectOrphanWorkSessions() {
  const allWorkSessions = getRunningWorkSessions();
  for (const session of allWorkSessions) {
    const lastHeartbeat = memoryStore.getLastHeartbeat(session.id);
    const stallTime = Date.now() - lastHeartbeat;

    if (stallTime > 5 * 60 * 1000) { // 5 minute stall
      // Kill the session gracefully
      terminateWorkSession(session.id);
      // Notify chat
      spawnChatNotification({
        type: 'work-timeout',
        workSessionId: session.id,
        actionRequired: 'handle result gracefully'
      });
    }
  }
}
```

### 🔧 CHANGE REQUEST #2

**Definiálj Heartbeat Protocol + Timeout Strategy:**
```yaml
Location: ADR-049, section 5 (Session Lifecycle)
Add:
  - Heartbeat interval: 10s (work → memory)
  - Stall threshold: 5 minutes
  - Timeout action: graceful kill + memory flush
  - Max execution time: 12 hours per work session
Test: Simulate crash scenarios + orphan cleanup
```

---

## 3️⃣ PÁRHUZAMOSÍTÁS — ⚠️ DEADLOCK RISK

### Jelenlegi Megoldás: Max 3 Parallel Sessions + `depends_on`

**❌ Problémák:**

1. **Circular Dependency Detection Missing**
   ```
   Scenario:
   WORK-001: depends_on = ['WORK-002']
   WORK-002: depends_on = ['WORK-001']

   Result: Both sessions wait forever (deadlock!)
   ADR: No cycle detection mentioned
   ```

2. **Max 3 Limit Arbitrary** — De mi ha:
   - 4 independent tasks érkeznek?
   - Queue-ban maradnak szükségtelen
   - Cost-based limit lenne jobb (pl: max 3 parallel OR max $X/session cost)

3. **Dependency Failure Handling** — Ha WORK-001 BLOCKED:
   - WORK-002 (depends_on: WORK-001) waiting forever?
   - Automatic escalation? Manual intervention?
   - ADR: Nem dokumentálva

### ✅ Szükséges: DAG Validation + Dynamic Limits

```typescript
// Cycle detection (Topological sort)
function validateDependencyGraph(tasks: SpawnWorkTask[]): ValidationResult {
  const graph = new Map<string, string[]>();

  for (const task of tasks) {
    graph.set(task.task_id, task.depends_on || []);
  }

  // Kahn's algorithm for cycle detection
  const inDegree = new Map<string, number>();
  const queue: string[] = [];

  for (const [task, deps] of graph) {
    inDegree.set(task, deps.length);
    if (deps.length === 0) queue.push(task);
  }

  let processed = 0;
  while (queue.length > 0) {
    const current = queue.shift()!;
    processed++;

    for (const [task, deps] of graph) {
      if (deps.includes(current)) {
        inDegree.set(task, inDegree.get(task)! - 1);
        if (inDegree.get(task) === 0) queue.push(task);
      }
    }
  }

  // Cycle detected if not all tasks were processed
  if (processed !== graph.size) {
    return { valid: false, error: 'Circular dependency detected' };
  }
  return { valid: true };
}

// Dynamic parallel limit (cost-aware)
function calculateMaxParallel(chatSession: ChatSession): number {
  const costPerMinute = 0.05; // ~$0.05/min per Sonnet session
  const maxCostPerHour = 10; // $10/hour budget

  const maxConcurrentByBudget = Math.floor(maxCostPerHour / (costPerMinute * 60));
  return Math.min(maxConcurrentByBudget, 5); // Hard cap: 5 parallel
}
```

### 🔧 CHANGE REQUEST #3

**Adja a DAG Validation + Dynamic Parallel Limits:**
```yaml
Location: ADR-049, section 3 (Párhuzamosítás)
Add:
  - Cycle detection algorithm (Kahn's algorithm)
  - Failure cascade handling (blocked task → dependent tasks)
  - Dynamic parallel limit (cost-based, not fixed 3)
  - Max hard limit: 5 parallel per terminal
Test: Unit tests for circular dependencies + cost calculations
```

---

## 4️⃣ COST/BENEFIT — ⚠️ MARGINAL ROI, HIGH COMPLEXITY

### Költségbecslés — ADR Revision Szükséges

**Az ADR ábra félrevezetõ:**

```
Jelenlegi (az ADR szerint):
- 10 Telegram msg: 10×Sonnet = $0.30  ← TÉVES!
- Párhuzamos tasks: N/A

Valódi költség (Dual-session):
- 10 Telegram msg: 10×Haiku = $0.01   ✅ Sparing: $0.29
- 5 munka task: 5×Sonnet = $1.50      ✅ Same
- 2 parallel task (chat spawned): 2×Sonnet = $0.60  ← EXTRA!
- Total: $2.10 vs $1.80 (jelenlegi)
```

**Scenario: 3 párhuzamos task:**
```
Cost: $0.01 (Telegram) + $0.60×3 (parallel Sonnet) = $1.81
vs Jelenlegi szekvenciális: $1.80

Result: Csak 1% költséghatékony!
De 50% gyorsabb? → Trade-off kell lemérni
```

**Komplexitás költsége:**
- Development: +20% (dual-session MCP tools, memory sync, recovery logic)
- Operations: +30% (debugging, monitoring, crash recovery)
- Training: +15% (new concepts for all terminal operators)
- **Total hidden cost: ~65% complexity overhead**

### ✅ Ajánlás: Simpler Interim Solution

**Phase 1 (simpler, gyorsabb MVP):**
1. Chat session csak Telegram-hez (no work spawning)
2. Memory: File-based (nem szinkronizál)
3. Work session: Current single-session keep
4. ROI: Telegram savings (10-15% költség csökkentés, 0% komplexitás)

**Phase 2 (ha Phase 1 proves value):**
1. Chat → work spawning
2. Dependency tracking (no parallelization yet)
3. SQLite memory store

**Phase 3 (future):**
1. Parallel work sessions
2. Dynamic cost limits

### 🔧 CHANGE REQUEST #4

**Revizió: Phase-based Rollout + Simpler Phase 1:**
```yaml
Location: ADR-049, section "Costs & Considerations" + Implementation Plan
Change:
  - Phase 1: Chat session Telegram-only (no spawning)
  - Phase 2: Work spawning + dependency tracking (single parallel OK)
  - Phase 3: Full parallelization (cost-aware)
  - ROI target Phase 1: 15% cost savings, <10% complexity increase
```

---

## 5️⃣ INTEGRATION — ⚠️ INCOMPLETE DESIGN

### Jelenlegi: "Conductor integration" — Vagué

**❌ Hiányzások:**

1. **Conductor Dual-Session Support** — Nem dokumentálva:
   - Conductor chat session mit csinál?
   - Conductor work session terminálákat kezel?
   - Role separation?

   ```
   Kérdés: Conductor dual-session vagy single?
   - Ha dual: Chat-ből spawn-ol? Hogy koordinál?
   - Ha single: Bottleneck az orchestration-nél?
   ```

2. **Dashboard UI** — "show chat/work sessions" — de:
   - Current: terminál → status badge
   - New: terminál → 1 chat + N work sessions
   - Timeline/Gantt complexity: 10x
   - Mockup? Wireframe? Spec?

3. **Monitoring/Alerting** — ">$5/óra alert" — de:
   - $5 over what period? (1 hour? 1 day?)
   - Automatic action? (kill sessions? escalate?)
   - Per-terminal vagy global budget?

### ✅ Szükséges: Integration Spec Document

```
Új ADR annex: "ADR-049-Appendix-Integration.md"

## Conductor Dual-Session Coordination

1. Conductor Chat Session
   - Input: Telegram, internal chat
   - Output: spawn_work calls
   - Model: Haiku
   - Role: Mini-orchestrator for own work sessions

2. Coordinator Pattern
   - Conductor chat: "Backend needs review, spawn review task"
   - Conductor work: Processes regular inbox tasks
   - Parallel: Chat can spawn work-001 while work-002 running

## Dashboard Updates

1. Session View
   - Terminál card: 1 chat badge + N work badges
   - Color: green (running) / yellow (waiting) / red (error)

2. Timeline/Gantt
   - Y-axis: terminal-chat, terminal-work-001, terminal-work-002, ...
   - X-axis: time
   - Duration blocks colored by status

3. Cost Tracker
   - Per-terminal: running cost ($X/session)
   - Global: total cost ($Y/hour)
   - Alert: if >$5/hour → red banner + option to kill sessions

## Alerting Strategy

1. Budget Alert Levels
   - Soft warning: >$3/hour (logging only)
   - Hard warning: >$5/hour (Telegram to Root)
   - Critical: >$10/hour (auto-kill slowest session + escalate)

2. Session Timeout Alerts
   - Stalled work session (>5min no heartbeat): Kill + log
   - Memory sync timeout (>1s): Retry with backoff
   - Chat session crash: Auto-restart + notify
```

### 🔧 CHANGE REQUEST #5

**Írd meg az Integration Appendix-et (or expand section 5):**
```yaml
Location: ADR-049 (new) or existing section 5
Content:
  - Conductor dual-session coordination protocol
  - Dashboard UI spec + mockup references
  - Monitoring/alerting thresholds + actions
  - Backward compatibility strategy (legacy single-session fallback)
Test: Integration tests with Conductor + Dashboard
```

---

## SUMMARY — Architecture Soundness Assessment

| Aspekt | Rating | Verdict |
|--------|--------|---------|
| **Concept** | ⭐⭐⭐⭐ | Solid — separation of concerns |
| **Memory Sync** | ⭐⭐ | Risky — needs SQLite WAL |
| **Crash Recovery** | ⭐⭐ | Incomplete — needs heartbeat protocol |
| **Parallelization** | ⭐⭐⭐ | Good idea, risky impl — needs DAG validation |
| **Cost Analysis** | ⭐⭐ | Incomplete — ROI unclear for Phase 1 |
| **Integration** | ⭐⭐ | Vague — needs detailed spec |
| **Overall** | ⭐⭐⭐ | Implementable with fixes |

---

## RECOMMENDATION

### ✅ APPROVE ADR-049 WITH MANDATORY CHANGES

**条件:**

1. **Implement all 5 Change Requests** before development starts
2. **Phase-based rollout** (simpler Phase 1)
3. **Integration spec** (Conductor + Dashboard)
4. **Crash recovery protocol** (heartbeat + timeout)

### 🔄 ALTERNATIVE (if changes deemed too heavyweight)

**Reject ADR-049, implement simpler interim:**
- Chat session Telegram-only (no work spawning)
- Keep current work session single
- Revisit parallelization in 6 months after chat stabilizes

### 💬 For Root

**My recommendation:** Approve with changes. The dual-session concept is architecturally sound, but implementation details need hardening before coding starts. Phase-based approach reduces risk significantly.

**Timeline estimate:**
- Changes + testing: 3-4 days
- Phase 1 implementation: 2-3 weeks
- Phase 2: 1-2 weeks after Phase 1 stable

---

**Architect Sign-off:** Ready for implementation once changes are incorporated.
