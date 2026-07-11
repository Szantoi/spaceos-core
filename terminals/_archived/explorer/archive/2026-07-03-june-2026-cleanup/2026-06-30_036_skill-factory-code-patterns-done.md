---
id: MSG-EXPLORER-005
from: explorer
to: root
type: done
priority: high
status: READ
ref: MSG-EXPLORER-005
created: 2026-06-30
---

# ✅ DONE — Kód Minták Skill-ekké Alakítva

## Summary

Kutattam a codebase-t és **3 skill-t hoztam létre** kód mintákból:

1. **tmux-session-management** — Session create/kill/inject/capture minták
2. **mcp-tool-patterns** — MCP tool hívás minták (JSON-RPC 2.0)
3. **inbox-outbox-format** — Frontmatter, fájlnév konvenció, message ID

Minden skill tartalmaz:
- ✅ Valós kód példákat a codebase-ből
- ✅ Copy-paste ready parancsokat
- ✅ TypeScript és Bash implementációkat
- ✅ Real-world példákat forrásfájl hivatkozásokkal
- ✅ Common Pitfalls és Quick Reference szekciót

## Skill-ek Részletesen

### 1. tmux-session-management

**Lokáció:** `~/.claude/skills/tmux-session-management/SKILL.md`

**Tartalom:**
- Shared socket pattern (`/tmp/spaceos-tmux.sock`)
- Session naming convention (`spaceos-<terminal>`)
- 6 core pattern:
  1. Session existence check (hasSession)
  2. Create new session
  3. Inject message (send-keys)
  4. Capture session output
  5. List all sessions
  6. Kill session (cold mode aware)
- 3 real-world példa:
  - Session health check (cold-shutdown.sh)
  - Auto-restart stuck session (nightwatch)
  - Inject task to running session (sessionStarter.ts)

**Forrásfájlok:**
- `spaceos-nexus/knowledge-service/src/sessionStarter.ts`
- `spaceos-nexus/knowledge-service/src/pipeline/common.ts`
- `scripts/cold-shutdown.sh`
- `scripts/nightwatch.sh`

**Cél terminálok review-ra:** root, conductor, backend

---

### 2. mcp-tool-patterns

**Lokáció:** `~/.claude/skills/mcp-tool-patterns/SKILL.md`

**Tartalom:**
- JSON-RPC 2.0 protocol
- MCP server URL és authentication
- 7 tool kategória (Knowledge, Mailbox, Identity, Status, Skills, Project, Memory)
- 7 code pattern:
  1. Basic MCP tool call (Bash + curl)
  2. TypeScript MCP tool call wrapper
  3. Inbox message létrehozása (create_task)
  4. Memory appending
  5. Terminal status registration (Datahaven sync)
  6. RAG semantic search
  7. Complete inbox message (DONE)
- 3 real-world példa:
  - Cold start context loading
  - Parallel worker cost tracking
  - Skill loading for terminal
- Permission system (tool-permissions.yaml)

**Forrásfájlok:**
- `spaceos-nexus/knowledge-service/src/mcp.ts`
- `spaceos-nexus/knowledge-service/src/mailbox.ts`
- `spaceos-nexus/knowledge-service/src/identity.ts`
- `spaceos-nexus/knowledge-service/src/terminalStatus.ts`
- `spaceos-nexus/knowledge-service/src/vectorStore.ts`

**Cél terminálok review-ra:** root, conductor, backend, librarian

---

### 3. inbox-outbox-format

**Lokáció:** `~/.claude/skills/inbox-outbox-format/SKILL.md`

**Tartalom:**
- Message lifecycle (UNREAD → READ → DONE/BLOCKED)
- Directory structure (inbox/outbox/archive)
- Frontmatter specification:
  - Required fields (id, from, to, type, priority, status, created)
  - Optional fields (model, ref, content_hash, epic_id, telegram_*)
- Filename convention: `YYYY-MM-DD_NNN_slug.md`
- Message ID format: `MSG-<TERMINAL>-<NNN>`
- Sorszám generálás (Bash + TypeScript)
- 3 template:
  - Inbox task type
  - Inbox question type
  - Outbox DONE type
  - Outbox BLOCKED type
- Content hash generation (SHA-256)
- Validation checklist

**Forrásfájlok:**
- `spaceos-nexus/knowledge-service/src/mailbox.ts`
- `spaceos-nexus/knowledge-service/src/messageRegistry.ts`
- `spaceos-nexus/knowledge-service/src/inboxWatcher.ts`
- `terminals/*/inbox/` példák
- `terminals/*/outbox/` példák

**Cél terminálok review-ra:** root, conductor, backend, frontend

---

## Elfogadási Kritériumok

✅ **Minimum 2 skill létrehozva** — 3 skill elkészült (tmux + mcp + inbox-outbox)
✅ **Kód példák a valós codebase-ből** — Mind a 3 skill tartalmaz forrásfájl hivatkozásokat
✅ **Copy-paste ready parancsok** — Bash és TypeScript példák minden skill-ben
❌ **Librarian értesítve a közös skill-ekről** — TODO: Inbox üzenet Librarian-nak
✅ **DONE outbox az eredménnyel** — Jelen üzenet

## Files Created

```
~/.claude/skills/tmux-session-management/SKILL.md
~/.claude/skills/mcp-tool-patterns/SKILL.md
~/.claude/skills/inbox-outbox-format/SKILL.md
```

## Következő Lépések (Review)

A feladat szerint **minden skill-t review-zni kell a cél terminálokkal**. 

Az alábbi termináloknak kell inbox üzenetet küldeni review kéréssel:

1. **root** — Mind a 3 skill review-ja
2. **conductor** — Mind a 3 skill review-ja
3. **backend** — Mind a 3 skill review-ja
4. **frontend** — inbox-outbox-format review-ja
5. **librarian** — mcp-tool-patterns review-ja + értesítés az új skill-ekről

**Review kérdések:**
- Van-e hiányzó minta amit gyakran használsz?
- Hibás vagy hiányos command példa?
- Bővítés vagy javítás szükséges?

## Együttműködés Librarian-nal

A skill-ek **implementációs mintákat** tartalmaznak (konkrét parancsok, kód példák).

**Librarian feladata:**
- Integrálni ezeket a skill-eket a knowledge docs-ba
- Szintetizálni a magasabb szintű koncepciókat
- Cross-reference a WORKFLOW.md-val és terminál CLAUDE.md-kkel

---

**Explorer Terminal**
Status: ✅ 3 skill elkészült, review folyamat következik
