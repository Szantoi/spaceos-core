---
id: MSG-BACKEND-075
from: root
to: backend
type: task
priority: critical
status: READ
model: sonnet
ref: ADR-049
epic_id: INFRA-DUAL-SESSION
created: 2026-06-29
processed: 2026-06-29
content_hash: 1ec84e7b6526c909d8fb4912947f262f64b5287a6078d72e05f681e84a57e5c3
---

# ADR-049 Phase 1: Dual-Session Infrastructure

## Kontextus

Az ADR-049 (Dual-Session Chat/Work Architecture) jóváhagyva. Phase 1 implementáció indul.

**ADR:** `/opt/spaceos/docs/architecture/decisions/ADR-049-dual-session-chat-work-architecture.md`

## Feladatok

### 1. SQLite WAL MemoryStore (CR#1)

**File:** `spaceos-nexus/knowledge-service/src/memoryStore.ts`

```typescript
import Database from 'better-sqlite3';

export class MemoryStore {
  private db: Database.Database;

  constructor(terminal: string) {
    const dbPath = `${TERMINALS_DIR}/${terminal}/memory.db`;
    this.db = new Database(dbPath);
    this.db.pragma('journal_mode = WAL');
    this.db.pragma('busy_timeout = 5000');

    this.initSchema();
  }

  private initSchema() {
    this.db.exec(`
      CREATE TABLE IF NOT EXISTS memory_log (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        section TEXT NOT NULL CHECK(section IN ('chat', 'work', 'shared')),
        content TEXT NOT NULL,
        author TEXT NOT NULL,
        timestamp TEXT NOT NULL
      );
      CREATE INDEX IF NOT EXISTS idx_section ON memory_log(section);
      CREATE INDEX IF NOT EXISTS idx_timestamp ON memory_log(timestamp);
    `);
  }

  append(section: 'chat' | 'work' | 'shared', content: string, author: string): number {
    const stmt = this.db.prepare(
      'INSERT INTO memory_log (section, content, author, timestamp) VALUES (?, ?, ?, ?)'
    );
    const result = stmt.run(section, content, author, new Date().toISOString());
    return result.lastInsertRowid as number;
  }

  read(section: string, limit: number = 50): MemoryEntry[] {
    const stmt = this.db.prepare(
      'SELECT * FROM memory_log WHERE section = ? ORDER BY timestamp DESC LIMIT ?'
    );
    return stmt.all(section, limit) as MemoryEntry[];
  }

  exportToMarkdown(): string {
    // Session indításhoz markdown export
  }
}

interface MemoryEntry {
  id: number;
  section: string;
  content: string;
  author: string;
  timestamp: string;
}
```

**Dependency:** `npm install better-sqlite3 @types/better-sqlite3`

### 2. Chat Session Starter

**File:** `spaceos-nexus/knowledge-service/src/chatSessionStarter.ts`

Új session típus: `spaceos-{terminal}-chat`

```typescript
export async function startChatSession(terminal: string): Promise<void> {
  const sessionName = `spaceos-${terminal}-chat`;
  const workdir = getTerminalWorkdir(terminal);

  // Haiku model, continuous mode
  const claudeCmd = `claude --model haiku`;

  // Identity + memory betöltés
  const initPrompt = buildChatSessionPrompt(terminal);

  await createTmuxSession(sessionName, workdir);
  await injectPrompt(sessionName, claudeCmd, initPrompt);
}

function buildChatSessionPrompt(terminal: string): string {
  return `Te a ${terminal.toUpperCase()} terminál CHAT session-je vagy.

Feladataid:
1. Telegram üzenetek megválaszolása (gyors, tömör)
2. Státusz riportok
3. Koordináció más terminálokkal
4. Work session spawn (Phase 2-ben)

Olvasd be: CLAUDE.md, MEMORY.md

Ha hosszabb munkát kérnek, válaszolj: "Ezt a work session-nek adom át."`;
}
```

### 3. Telegram Routing Módosítás

**File:** `spaceos-nexus/knowledge-service/src/telegram/telegramRouter.ts`

```typescript
// Jelenlegi: routing work session-be
// Új: routing chat session-be

export async function handleTelegramMessage(msg: TelegramIncoming): Promise<void> {
  const terminal = detectTerminal(msg);
  const chatSession = `spaceos-${terminal}-chat`;

  // Chat session-be inject
  if (!isSessionRunning(chatSession)) {
    await startChatSession(terminal);
  }

  const prompt = `[TG @${msg.from} conv:${msg.conversationId}] ${msg.text}`;
  await injectToSession(chatSession, prompt);
}
```

### 4. Session Naming Refactor

**Jelenlegi:** `spaceos-backend`
**Új:**
- `spaceos-backend-chat` (Haiku, continuous)
- `spaceos-backend-work` (Sonnet, cold)

Frissítendő fájlok:
- `sessionStarter.ts`
- `terminalConfig.ts`
- `inboxWatcher.ts`

## Acceptance Criteria

- [ ] `better-sqlite3` dependency hozzáadva
- [ ] MemoryStore class működik (unit test)
- [ ] Chat session indítható Haiku modellel
- [ ] Telegram üzenetek chat session-be mennek
- [ ] Work session továbbra is működik (backward compatible)
- [ ] Dashboard látja mindkét session típust

## Tesztelés

```bash
# Unit test
npm test -- memoryStore

# Manual test
# 1. Indíts chat session-t
curl -X POST localhost:3456/api/session/start-chat -d '{"terminal":"conductor"}'

# 2. Küldj Telegram üzenetet
# 3. Ellenőrizd hogy chat session válaszol
```

## Határidő

Phase 1 target: 2 nap
