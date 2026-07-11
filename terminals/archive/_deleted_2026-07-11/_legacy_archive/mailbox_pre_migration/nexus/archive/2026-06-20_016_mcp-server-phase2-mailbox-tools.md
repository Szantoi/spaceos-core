---
id: MSG-NEXUS-016
from: root
to: nexus
type: task
priority: high
status: READ
model: sonnet
ref: ROADMAP.md
created: 2026-06-20
---

# MCP Server Fázis 2 — Mailbox Toolok Implementálása

## Kontextus

A Fázis 1 (`search_knowledge` RAG) kész. Most a Fázis 2 következik: mailbox kommunikációs toolok.

**Olvasd el először:** `docs/agent-infrastructure/ROADMAP.md` — "Centralizált MCP Server Architektúra" szekció

## Feladat

Bővítsd a `spaceos-nexus/knowledge-service/` szolgáltatást az alábbi MCP toolokkal:

### 1. `list_inbox` tool

```typescript
// GET /api/mailbox/:terminal/inbox
// Params: status=UNREAD|READ|all (optional)
list_inbox: {
  terminal: string;       // "kernel", "fe", "conductor", stb.
  status?: "UNREAD" | "READ" | "all";
}

// Response: array of inbox messages with frontmatter parsed
```

### 2. `send_message` tool

```typescript
// POST /api/mailbox/:terminal/inbox
send_message: {
  to: string;             // target terminal
  type: "task" | "question" | "done" | "blocked";
  content: string;        // markdown body
  priority: "critical" | "high" | "medium" | "low";
  ref?: string;           // related MSG ID
  model?: "haiku" | "sonnet" | "opus";
}

// Creates new file: docs/mailbox/<to>/inbox/YYYY-MM-DD_NNN_<slug>.md
// Returns: { id: "MSG-<TERMINAL>-NNN", path: "..." }
```

### 3. `submit_done` tool

```typescript
// POST /api/mailbox/:terminal/outbox
submit_done: {
  from: string;           // sender terminal
  task_id: string;        // original MSG ID
  summary: string;        // what was done
  files_changed: string[];
}

// Creates outbox message with type: done
```

### 4. `get_task_status` tool

```typescript
// GET /api/tasks/status
// Optional: ?task_id=MSG-XXX-NNN
get_task_status: {
  task_id?: string;       // if omitted, returns all active
}

// Reads from docs/tasks/active/ and docs/tasks/new/
```

## Implementációs útmutató

1. **Új fájl:** `spaceos-nexus/knowledge-service/src/mailbox.ts`
   - YAML frontmatter parser (gray-matter npm)
   - Auto MSG ID generálás (következő sorszám lekérdezés)
   - Fájl írás `docs/mailbox/` mappába

2. **Router bővítés:** `src/server.ts`
   - `/api/mailbox/*` routes
   - `/api/tasks/*` routes

3. **Teszt script:** `spaceos-nexus/scripts/test-mailbox.sh`
   - curl példák minden endpointra

## Korlátozások

- **Csak fájlokat ír** — a git commit/push NEM automatikus (azt a nightwatch csinálja)
- **Nem módosít meglévő fájlt** — csak új fájlokat hoz létre
- **Audit trail megmarad** — minden üzenet verziókövetett lesz git-ben

## DONE kritérium

- [ ] `list_inbox` működik, YAML frontmatter-t parse-ol
- [ ] `send_message` létrehozza a fájlt helyes formátummal
- [ ] `submit_done` outbox üzenetet ír
- [ ] `get_task_status` olvassa a tasks mappát
- [ ] Teszt script sikeres
- [ ] README.md frissítve az új endpointokkal

## Referencia

- Meglévő kód: `spaceos-nexus/knowledge-service/src/server.ts`
- Mailbox formátum: `docs/mailbox/*/inbox/*.md` (bármelyik minta)
- ROADMAP: `docs/agent-infrastructure/ROADMAP.md`
