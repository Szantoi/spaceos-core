---
id: MSG-BACKEND-008
from: root
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: ADR-046
created: 2026-06-21
content_hash: 3a238076df599666a0aae58b52c32b254c6ce968d51bd293213ac715055a244b
---

# ADR-046 Track B: Session Lifecycle Hooks

## Kontextus

Az ADR-046 (Marveen Cold Start Strategy) alapján implementáld a **Track B: Session Lifecycle Hooks** komponenst.

**ADR dokumentum:** `docs/adr/ADR-046_marveen-cold-start-strategy.md`
**Függőség:** Track A (Memory Tier) — párhuzamosan fejleszthető, de Track A-t használja

## Feladat

Hozd létre a `sessionHooks.ts` modult és integráld a `sessionStarter.ts`-be:

### B.1 — sessionHooks.ts modul

```typescript
// src/sessionHooks.ts

interface SessionStartContext {
  terminal: string;
  taskId?: string;
  inboxMessageId?: string;
}

interface SessionStartResult {
  contextInjected: boolean;
  memoriesLoaded: number;
  hotMemories: TieredMemory[];
  warmMemories: TieredMemory[];
  sharedMemories: TieredMemory[];
  contextTokens: number;
  contextMarkdown: string;
}

export async function buildStartContext(ctx: SessionStartContext): Promise<SessionStartResult>
```

### B.2 — buildStartContext() implementáció

1. Lekérdezi a hot + warm memóriákat (terminal-specifikus)
2. Lekérdezi a shared memóriákat (globális)
3. Összegyűjti a releváns knowledge doc-okat
4. Épít egy context markdown-ot (max 5K token)
5. Visszaadja a SessionStartResult-ot

### B.3 — handleSessionEnd() implementáció

```typescript
interface SessionEndContext {
  terminal: string;
  endReason: 'done' | 'blocked' | 'timeout' | 'handoff' | 'crash';
  taskId?: string;
  summary?: string;
}

interface SessionEndResult {
  memoriesSaved: number;
  retrospectiveTriggered: boolean;
  handoffGenerated: boolean;
  sessionId: number;
}

export async function handleSessionEnd(ctx: SessionEndContext): Promise<SessionEndResult>
```

### B.4 — Integráció sessionStarter.ts-be

Módosítsd a `startTerminalSession()` függvényt:

```typescript
// Session indítás után, claude parancs előtt:
const startContext = await buildStartContext({ terminal, inboxMessageId: messageId });
// Inject context into session
injectMessageToSession(sessionName, startContext.contextMarkdown);
```

### B.5 — session_history tábla

```sql
CREATE TABLE IF NOT EXISTS session_history (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  terminal TEXT NOT NULL,
  started_at TEXT NOT NULL,
  ended_at TEXT,
  end_reason TEXT,
  task_id TEXT,
  memories_injected INTEGER NOT NULL DEFAULT 0,
  memories_created INTEGER NOT NULL DEFAULT 0
);
```

### B.6 — Integration tests

Írj teszteket: `__tests__/sessionHooks.test.ts`

## Fájlok

| Fájl | Művelet |
|---|---|
| `src/sessionHooks.ts` | CREATE — session lifecycle hooks |
| `src/sessionStarter.ts` | MODIFY — integrate hooks |
| `__tests__/sessionHooks.test.ts` | CREATE — integration tests |

## Definition of Done

- [ ] `sessionHooks.ts` modul létrehozva
- [ ] `buildStartContext()` implementálva és tesztelve
- [ ] `handleSessionEnd()` implementálva
- [ ] `sessionStarter.ts` integrálva a hook-okkal
- [ ] session_history tábla létrehozva
- [ ] Integration tesztek zöldek
- [ ] Build PASS
- [ ] DONE outbox üzenet elküldve

## Referenciák

- ADR-046: `docs/adr/ADR-046_marveen-cold-start-strategy.md` (Section 2.3, 3.2)
- sessionStarter: `spaceos-nexus/knowledge-service/src/sessionStarter.ts`
- Prototípus referencia: `spaceos-nexus/knowledge-service/src/coldStart.prototype.ts`
- Marveen skills: `~/.claude/skills/retrospective/SKILL.md`, `~/.claude/skills/handoff/SKILL.md`
