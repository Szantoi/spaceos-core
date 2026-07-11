# ADR-049: Dual-Session Chat/Work Architecture

**Status:** APPROVED
**Date:** 2026-06-29
**Author:** Root + Gábor
**Reviewed by:** Architect (APPROVE WITH CHANGES)
**Decision:** Phase-based implementation approved

---

## Context

### Jelenlegi problémák

1. **Kommunikáció vs munka ütközés**: Ha egy terminál dolgozik, a Telegram üzenetek megszakítják a munkát
2. **Nincs párhuzamosítás**: Független feladatokat szekvenciálisan kell végrehajtani
3. **Model pazarlás**: Egyszerű kérdésekre Sonnet/Opus válaszol (drága, lassú)
4. **Emberi korlátok projekciója**: A terminálokat 1 emberként kezeljük, pedig LLM-ként skálázhatók

---

## Decision: 3-Rétegű Terminál Architektúra

### Alapelv

> **Ne emberi kapacitásként gondoljunk a terminálokra, hanem skálázható, specializált szolgáltatásokként.**

A terminálok LLM mivoltukból fakadó előnyöket kapnak:
- Sokszorozhatóság (parallel workers)
- Dinamikus tudásbetöltés (domain memories)
- Konzisztens személyiség (identity layer)

### 3-Rétegű Architektúra

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           TERMINAL: backend                              │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ╔═══════════════════════════════════════════════════════════════════╗  │
│  ║  IDENTITY LAYER (személyiség)                                      ║  │
│  ║  ┌───────────────────────────────────────────────────────────────┐ ║  │
│  ║  │  CLAUDE.md = Ki vagyok, hogyan dolgozom                       │ ║  │
│  ║  │  - Fix, konzisztens karakter                                  │ ║  │
│  ║  │  - Stílus, értékek, munkamódszer                              │ ║  │
│  ║  │  - Nem változik session-ök között                             │ ║  │
│  ║  └───────────────────────────────────────────────────────────────┘ ║  │
│  ╚═══════════════════════════════════════════════════════════════════╝  │
│                                    │                                     │
│                                    ▼                                     │
│  ╔═══════════════════════════════════════════════════════════════════╗  │
│  ║  KNOWLEDGE LAYER (tudás)                                           ║  │
│  ║  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐  ║  │
│  ║  │ kernel.mem  │ │ joinery.mem │ │ cutting.mem │ │ orch.mem    │  ║  │
│  ║  └─────────────┘ └─────────────┘ └─────────────┘ └─────────────┘  ║  │
│  ║                                                                    ║  │
│  ║  Dinamikusan betölthető domain memóriák                           ║  │
│  ║  → Feladat alapján választódik ki                                 ║  │
│  ╚═══════════════════════════════════════════════════════════════════╝  │
│                                    │                                     │
│                                    ▼                                     │
│  ╔═══════════════════════════════════════════════════════════════════╗  │
│  ║  WORKER LAYER (végrehajtás)                                        ║  │
│  ║                                                                    ║  │
│  ║  spaceos-backend-chat (Haiku, continuous)                         ║  │
│  ║  ┌───────────────────────────────────────────────────────────┐    ║  │
│  ║  │ - Telegram kommunikáció                                    │    ║  │
│  ║  │ - Gyors válaszok                                           │    ║  │
│  ║  │ - Worker spawn/koordináció                                 │    ║  │
│  ║  └───────────────────────────────────────────────────────────┘    ║  │
│  ║           │                                                        ║  │
│  ║           ▼ spawn                                                  ║  │
│  ║  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐                  ║  │
│  ║  │ work-001    │ │ work-002    │ │ work-003    │  ← parallel      ║  │
│  ║  │ (api impl)  │ │ (tests)     │ │ (prototype) │                  ║  │
│  ║  │ Sonnet      │ │ Sonnet      │ │ Haiku/raw   │                  ║  │
│  ║  └─────────────┘ └─────────────┘ └─────────────┘                  ║  │
│  ║                                                                    ║  │
│  ╚═══════════════════════════════════════════════════════════════════╝  │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### Rétegek részletezése

| Réteg | Típus | Tartalom | Cél |
|-------|-------|----------|-----|
| **Identity** | Fix | CLAUDE.md | Konzisztens személyiség, jobb minőség |
| **Knowledge** | Dinamikus | domain.memory.md | Szükség szerinti tudás betöltés |
| **Worker** | Sokszorozható | tmux sessions | Párhuzamos végrehajtás |

---

## Detailed Design

### 1. Identity Layer

Minden terminálnak fix személyisége van:

```markdown
# terminals/backend/CLAUDE.md

## Személyiség
- Precíz, alapos fejlesztő
- Clean code elveket követ
- Teszteket ír minden funkcióhoz
- Dokumentál

## Munkastílus
- Először megérti a feladatot
- Megtervezi a megoldást
- Implementál + tesztel
- DONE/BLOCKED outbox
```

### 2. Knowledge Layer - Domain Memories

```
terminals/backend/knowledge/
├── kernel.memory.md      ← Kernel domain tudás
├── joinery.memory.md     ← Joinery domain tudás
├── cutting.memory.md     ← Cutting domain tudás
├── orchestrator.memory.md
└── shared.memory.md      ← Cross-domain patterns
```

**Automatikus betöltés feladat alapján:**

```typescript
// knowledge-service/src/knowledgeLoader.ts

function loadDomainKnowledge(task: InboxTask): string[] {
  const memories: string[] = [];

  // Mindig betöltjük a shared-et
  memories.push('shared.memory.md');

  // Domain detekció a task alapján
  if (task.content.includes('Kernel') || task.tags?.includes('kernel')) {
    memories.push('kernel.memory.md');
  }
  if (task.content.includes('Joinery') || task.epic?.startsWith('JOINERY')) {
    memories.push('joinery.memory.md');
  }
  if (task.content.includes('Cutting') || task.epic?.startsWith('CUTTING')) {
    memories.push('cutting.memory.md');
  }

  return memories;
}
```

### 3. Worker Layer - Sokszorozható Sessions

**Session típusok:**

| Session | Model | Mód | Cél |
|---------|-------|-----|-----|
| `{terminal}-chat` | Haiku | Continuous | Telegram, koordináció |
| `{terminal}-work` | Sonnet | Cold | Inbox feladatok |
| `{terminal}-work-NNN` | Sonnet/Opus | Cold | Párhuzamos workerek |
| `{terminal}-raw-NNN` | Haiku | Cold | Gyors prototípus, kísérlet |

**"Raw" workerek:**
- Nincs teljes context betöltés
- Gyors prototípus készítés
- Kísérletezés (3 variáns párhuzamosan)
- Chat session választja ki a legjobbat

```typescript
// Példa: 3 prototípus párhuzamosan
chatSession.spawnRawWorkers({
  count: 3,
  task: 'Implementálj egy drag-drop sortert',
  model: 'haiku',
  selectBest: true  // Chat értékeli és választ
});
```

### 4. Memory Sync - SQLite WAL (Architect CR#1)

```typescript
// knowledge-service/src/memoryStore.ts
import Database from 'better-sqlite3';

export class MemoryStore {
  private db: Database.Database;

  constructor(terminal: string) {
    this.db = new Database(`terminals/${terminal}/memory.db`);
    this.db.pragma('journal_mode = WAL');
    this.db.pragma('busy_timeout = 5000');

    this.db.exec(`
      CREATE TABLE IF NOT EXISTS memory_log (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        section TEXT NOT NULL,
        content TEXT NOT NULL,
        author TEXT NOT NULL,
        timestamp TEXT NOT NULL
      );
      CREATE INDEX IF NOT EXISTS idx_section ON memory_log(section);
    `);
  }

  append(section: 'chat' | 'work' | 'shared', content: string, author: string) {
    const stmt = this.db.prepare(
      'INSERT INTO memory_log (section, content, author, timestamp) VALUES (?, ?, ?, ?)'
    );
    stmt.run(section, content, author, new Date().toISOString());
  }

  read(section: string, limit: number = 50): MemoryEntry[] {
    const stmt = this.db.prepare(
      'SELECT * FROM memory_log WHERE section = ? ORDER BY timestamp DESC LIMIT ?'
    );
    return stmt.all(section, limit);
  }

  // Export to markdown for session start
  exportToMarkdown(): string {
    const sections = ['chat', 'work', 'shared'];
    let md = '# Session Memory\n\n';

    for (const section of sections) {
      const entries = this.read(section, 20);
      md += `## ${section.toUpperCase()}\n\n`;
      for (const entry of entries.reverse()) {
        md += `### ${entry.timestamp} (${entry.author})\n${entry.content}\n\n`;
      }
    }
    return md;
  }
}
```

### 5. Heartbeat Protocol (Architect CR#2)

```typescript
// knowledge-service/src/workerHeartbeat.ts

const HEARTBEAT_INTERVAL = 10_000;  // 10s
const STALL_THRESHOLD = 5 * 60_000; // 5 min
const MAX_EXECUTION_TIME = 12 * 60 * 60_000; // 12 hours

class WorkerMonitor {
  private heartbeats: Map<string, number> = new Map();

  recordHeartbeat(workerId: string) {
    this.heartbeats.set(workerId, Date.now());
  }

  checkForStalls(): StalledWorker[] {
    const stalled: StalledWorker[] = [];
    const now = Date.now();

    for (const [workerId, lastBeat] of this.heartbeats) {
      const stallTime = now - lastBeat;

      if (stallTime > STALL_THRESHOLD) {
        stalled.push({
          workerId,
          stallTime,
          action: stallTime > MAX_EXECUTION_TIME ? 'kill' : 'warn'
        });
      }
    }
    return stalled;
  }

  async handleStalledWorker(worker: StalledWorker) {
    if (worker.action === 'kill') {
      await terminateWorker(worker.workerId);
      await notifyChat(worker.workerId, 'timeout');
    }
  }
}
```

### 6. DAG Validation (Architect CR#3)

```typescript
// knowledge-service/src/dagValidator.ts

function validateDependencies(tasks: WorkTask[]): ValidationResult {
  const graph = new Map<string, string[]>();

  for (const task of tasks) {
    graph.set(task.id, task.depends_on || []);
  }

  // Kahn's algorithm - cycle detection
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
        const newDegree = inDegree.get(task)! - 1;
        inDegree.set(task, newDegree);
        if (newDegree === 0) queue.push(task);
      }
    }
  }

  if (processed !== graph.size) {
    return { valid: false, error: 'Circular dependency detected' };
  }
  return { valid: true };
}

// Dynamic parallel limit (cost-based)
function calculateMaxParallel(terminal: string): number {
  const COST_PER_MINUTE = 0.05;
  const MAX_COST_PER_HOUR = 10;
  const maxByBudget = Math.floor(MAX_COST_PER_HOUR / (COST_PER_MINUTE * 60));
  return Math.min(maxByBudget, 5); // Hard cap: 5
}
```

### 7. Telegram Integration

```typescript
// knowledge-service/src/telegramRouter.ts

interface TelegramMessage {
  chatId: number;
  text: string;
  conversationId?: number;
  terminal?: string;  // Ha explicit címzett van
}

async function routeTelegramMessage(msg: TelegramMessage): Promise<void> {
  // 1. Terminál meghatározása
  const terminal = msg.terminal || detectTerminalFromContext(msg);

  // 2. Chat session-be routing
  const chatSession = `spaceos-${terminal}-chat`;

  // 3. Ha nincs chat session, indítjuk
  if (!isSessionRunning(chatSession)) {
    await startChatSession(terminal);
  }

  // 4. Inject message
  await injectToSession(chatSession, formatTelegramPrompt(msg));
}

function formatTelegramPrompt(msg: TelegramMessage): string {
  return `[TG @${msg.from} conv:${msg.conversationId}] ${msg.text}`;
}
```

---

## Implementation Plan (Phase-Based - Architect CR#4)

### Phase 1: Telegram Chat Sessions (1-2 hét)

**Cél:** Chat session-ök Telegram-ra kötése, 15% költségmegtakarítás

| Task | Leírás | Assignee |
|------|--------|----------|
| 1.1 | SQLite WAL MemoryStore | Backend |
| 1.2 | Chat session indítás logika | Backend |
| 1.3 | Telegram → chat routing | Backend |
| 1.4 | Terminálok Telegram bekötése | Backend |
| 1.5 | Dashboard chat/work badges | Frontend |

**Deliverables:**
- Minden terminálnak van `-chat` session
- Telegram üzenetek chat session-be mennek (Haiku)
- Work session zavartalanul dolgozik

### Phase 2: Work Spawning (2-3 hét)

**Cél:** Chat session tud work task-ot spawnolni

| Task | Leírás |
|------|--------|
| 2.1 | `spawn_work` MCP tool |
| 2.2 | Work session lifecycle |
| 2.3 | Result collection |
| 2.4 | Dependency tracking (single) |
| 2.5 | Domain knowledge auto-load |

### Phase 3: Parallel Workers (2-3 hét)

**Cél:** Párhuzamos work session-ök + raw workerek

| Task | Leírás |
|------|--------|
| 3.1 | Parallel work session support |
| 3.2 | DAG validation |
| 3.3 | Cost-based dynamic limits |
| 3.4 | Raw worker support |
| 3.5 | Best-of-N selection |

---

## Integration Spec (Architect CR#5)

### Conductor Dual-Session

```
Conductor-chat:
  - Telegram koordináció
  - Terminal status monitoring
  - Task dispatching decisions

Conductor-work:
  - Inbox feldolgozás
  - Planning pipeline execution
  - Review coordination
```

### Dashboard UI

```
Terminal Card (új):
┌─────────────────────────────────┐
│  BACKEND                        │
│  ┌─────┐ ┌─────┐ ┌─────┐       │
│  │chat │ │work │ │w-001│       │
│  │ 🟢  │ │ 🟡  │ │ 🟢  │       │
│  └─────┘ └─────┘ └─────┘       │
│  Telegram: 5 msg | Tasks: 2    │
└─────────────────────────────────┘

Legend:
  🟢 Running
  🟡 Idle
  🔴 Error/Stalled
```

### Alerting Thresholds

| Level | Trigger | Action |
|-------|---------|--------|
| Soft | >$3/hour | Log only |
| Hard | >$5/hour | Telegram to Root |
| Critical | >$10/hour | Auto-kill slowest + escalate |

---

## Cost Analysis

### Phase 1 ROI

| Metric | Before | After | Savings |
|--------|--------|-------|---------|
| Telegram (10 msg) | $0.30 (Sonnet) | $0.01 (Haiku) | **97%** |
| Work tasks | $1.50 | $1.50 | 0% |
| Complexity | Baseline | +10% | - |
| **Total** | $1.80 | $1.51 | **15%** |

### Phase 3 ROI (párhuzamosítással)

| Metric | Sequential | Parallel | Gain |
|--------|------------|----------|------|
| 3 independent tasks | 3 hours | 1 hour | **3x faster** |
| Cost | $1.80 | $2.40 | +33% |
| **Value** | - | Time > Money | ✅ |

---

## Migration Path

### Rollout

```
Week 1: conductor-chat (Telegram test)
Week 2: backend-chat, frontend-chat
Week 3: összes terminál chat session
Week 4: Phase 2 kezdés (work spawning)
```

### Backward Compatibility

- `spaceos-{terminal}` legacy session támogatva
- Graceful fallback ha chat crash-el
- Opt-out lehetőség terminálanként

---

## Decision

**APPROVED** - Phase-based implementation

**Signed off by:**
- Root: 2026-06-29
- Architect: 2026-06-29 (with changes incorporated)
- Gábor: 2026-06-29

---

## References

- Architect Review: `terminals/architect/outbox/2026-06-29_027_adr-049-dual-session-review-done.md`
- ADR-046: Cold Start Session Pattern
- ADR-047: Knowledge Service DDD Refactoring
