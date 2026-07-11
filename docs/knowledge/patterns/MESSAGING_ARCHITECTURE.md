# SpaceOS Messaging Architecture — Hybrid Model

**Status:** ACTIVE (Root Decision 2026-07-10)
**Decision:** MSG-ROOT-042 → MSG-LIBRARIAN-028
**Architecture:** Option C — Hybrid (TMB + File-based)

---

## Executive Summary

A SpaceOS két párhuzamos üzenetkezelési rendszert használ:

| Rendszer | Source of Truth | Use Case |
|----------|-----------------|----------|
| **TaskMessageBox (TMB)** | SQLite DB | Új MCP tool fejlesztés, audit trail |
| **File-based Mailbox** | .md files | Nightwatch, legacy compatibility |

**Gradual Transition:** Új fejlesztések TMB-t használnak, régi rendszer fokozatosan kiváltódik.

---

## 1. Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                     HYBRID MESSAGING SYSTEM                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌──────────────────┐          ┌──────────────────────────┐    │
│  │  TaskMessageBox  │          │   File-based Mailbox     │    │
│  │    (TMB DB)      │          │     (.md files)          │    │
│  ├──────────────────┤          ├──────────────────────────┤    │
│  │ SQLite DB        │          │ terminals/*/inbox/       │    │
│  │ Auto-render .md  │  ←sync→  │ terminals/*/outbox/      │    │
│  │ Audit trail      │          │ terminals/*/archive/     │    │
│  │ MCP native       │          │ Git versioned            │    │
│  └──────────────────┘          └──────────────────────────┘    │
│           │                              │                       │
│           ▼                              ▼                       │
│  ┌──────────────────┐          ┌──────────────────────────┐    │
│  │   MCP Tools      │          │   Shell Scripts          │    │
│  │   tmb_*          │          │   nightwatch.sh          │    │
│  │   create_task    │          │   reviewer.sh            │    │
│  └──────────────────┘          └──────────────────────────┘    │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 2. MCP Tools Mapping

### 2.1 TaskMessageBox (TMB) Tools — DB-backed

| Tool | Function | DB Table |
|------|----------|----------|
| `tmb_create_task` | Create task in DB | messages |
| `tmb_read_message` | Read + mark as read | messages |
| `tmb_complete_message` | Complete with DONE/BLOCKED | messages |
| `tmb_append_note` | Add notes to message | message_notes |
| `tmb_get_inbox` | List inbox messages | messages |
| `tmb_get_outbox` | List outbox messages | messages |

**Characteristics:**
- ✅ Native audit trail (created_at, read_at, completed_at)
- ✅ Auto-renders to .md files for compatibility
- ✅ Supports complex queries (status, priority, date range)
- ❌ Currently underutilized (DB mostly empty)

### 2.2 File-based Tools — .md files

| Tool | Function | Path |
|------|----------|------|
| `list_inbox` | List inbox .md files | terminals/*/inbox/ |
| `send_message` | Write .md to inbox | terminals/*/inbox/ |
| `create_task` | Create task .md file | terminals/*/inbox/ |
| `complete_inbox_message` | Mark READ + write outbox | terminals/*/outbox/ |
| `read_inbox_message` | Read .md content | terminals/*/inbox/ |

**Characteristics:**
- ✅ Git versioned (full history)
- ✅ Human readable (direct file access)
- ✅ Nightwatch compatible
- ❌ No native audit trail
- ❌ Manual archive required

### 2.3 Hybrid Tools — Both systems

| Tool | TMB | File | Notes |
|------|-----|------|-------|
| `fetch_task` | ✅ | ✅ | Epic-aware routing |
| `ack_task` | ✅ | ✅ | Task acknowledgment |
| `complete_task` | ✅ | ✅ | Triggers next task |

---

## 3. Use Case Decision Tree

```
Új feature fejlesztés?
    │
    ├── YES: MCP Tool development
    │         │
    │         └── Használj TMB tools-t (tmb_*)
    │
    └── NO: Nightwatch / Pipeline script
              │
              └── Használj File-based tools-t
```

### 3.1 When to Use TMB

- **New MCP tool development** — Native DB integration
- **Audit trail required** — Who read what, when
- **Complex queries** — Filter by status, priority, date
- **Automated workflows** — Programmatic message handling
- **Cross-terminal coordination** — Conductor task dispatch

### 3.2 When to Use File-based

- **Nightwatch scripts** — `watch*.sh` scripts
- **Manual debugging** — Direct file inspection
- **Git history needed** — Version control audit
- **Legacy compatibility** — Existing pipeline scripts
- **Human intervention** — Manual message creation

---

## 4. Transition Guidelines

### 4.1 For New Development

```
┌─────────────────────────────────────────────────────────┐
│  NEW CODE → TMB PREFERRED                               │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  // ✅ RECOMMENDED: Use TMB                              │
│  mcp__spaceos-knowledge__tmb_create_task({               │
│    from: "conductor",                                    │
│    to: "backend",                                        │
│    title: "Implement feature X",                         │
│    description: "...",                                   │
│    priority: "high"                                      │
│  });                                                     │
│                                                          │
│  // ⚠️ LEGACY: File-based (still supported)             │
│  mcp__spaceos-knowledge__create_task({                   │
│    from: "conductor",                                    │
│    to: "backend",                                        │
│    ...                                                   │
│  });                                                     │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

### 4.2 Migration Path (Not Required)

A Root döntése alapján **nincs kötelező migráció**:

- ❌ 1097+ régi üzenet NEM migrálódik
- ✅ Régi üzenetek file-based maradnak
- ✅ Új üzenetek fokozatosan TMB-be kerülnek
- ✅ Auto-archive működik (weekly-memory-cleanup.sh)

### 4.3 Conductor Task Dispatch

**Jelenleg:** File-based (`create_task`)
**Ajánlott:** TMB (`tmb_create_task`) — ha audit trail kell

```typescript
// Conductor dispatch options:

// Option 1: File-based (current)
await mcp.create_task({ to: "backend", ... });

// Option 2: TMB (recommended for new epics)
await mcp.tmb_create_task({ to: "backend", ... });
```

---

## 5. Auto-Archive Policy

**Script:** `scripts/weekly-memory-cleanup.sh`
**Schedule:** Sunday 02:00 AM (cron)
**Retention:** 7 days in outbox, then archive

### Archive Structure

```
terminals/<terminal>/
├── inbox/           # Active messages (UNREAD/READ)
├── outbox/          # Recent DONE/BLOCKED (7 days)
└── archive/
    ├── 2026-06/     # June 2026 messages
    ├── 2026-07-early/  # July 1-3 messages
    └── 2026-07/     # July 4+ messages (future)
```

### Manual Archive (Librarian)

```bash
# Archive messages older than 7 days
mv terminals/*/outbox/2026-06-*.md terminals/*/archive/2026-06/
```

---

## 6. Database Schema (TMB)

**Location:** `spaceos-nexus/knowledge-service/data/taskmessagebox.db`

```sql
-- Messages table
CREATE TABLE messages (
  id TEXT PRIMARY KEY,           -- MSG-BACKEND-042
  from_terminal TEXT NOT NULL,
  to_terminal TEXT NOT NULL,
  type TEXT NOT NULL,            -- task, question, info, blocked
  priority TEXT NOT NULL,        -- critical, high, medium, low
  status TEXT NOT NULL,          -- unread, read, completed, blocked
  title TEXT,
  content TEXT,
  created_at DATETIME,
  read_at DATETIME,
  completed_at DATETIME,
  content_hash TEXT
);

-- Notes table (for append_note)
CREATE TABLE message_notes (
  id INTEGER PRIMARY KEY,
  message_id TEXT REFERENCES messages(id),
  section TEXT,                  -- notes, implementation, feedback, blockers
  content TEXT,
  author TEXT,
  created_at DATETIME
);
```

---

## 7. Monitoring & Observability

### 7.1 TMB Metrics

```bash
# Message count by status
sqlite3 data/taskmessagebox.db "SELECT status, COUNT(*) FROM messages GROUP BY status"

# Unread messages per terminal
sqlite3 data/taskmessagebox.db "SELECT to_terminal, COUNT(*) FROM messages WHERE status='unread' GROUP BY to_terminal"
```

### 7.2 File-based Metrics

```bash
# Outbox count per terminal
find terminals/*/outbox -name "*.md" | cut -d'/' -f2 | sort | uniq -c

# UNREAD inbox messages
grep -rl "status: UNREAD" terminals/*/inbox/
```

---

## 8. References

- **Root Decision:** MSG-ROOT-042 → MSG-LIBRARIAN-028
- **TMB Implementation:** `spaceos-nexus/knowledge-service/src/task-message-box/`
- **File-based Implementation:** `spaceos-nexus/knowledge-service/src/mailbox.ts`
- **Auto-archive Script:** `scripts/weekly-memory-cleanup.sh`
- **Outbox Synthesis:** `docs/knowledge/patterns/OUTBOX_SYNTHESIS_2026-07.md`

---

_Created by Librarian Terminal — 2026-07-10_
_Architecture Decision: Hybrid Model (TMB + File-based)_
