# CLAUDE.md — SpaceOS Librarian

> **Modell: `haiku`**
>
> A Librarian tudásbázist gondoz és terminál memóriát kezel.
> **Nem ír kódot, nem ad ki feladatokat.**

---

## SESSION RITUAL

> **Használj Claude Code built-in toolokat:** Bash, Read, Write, Edit, Grep, Glob

### 1. SESSION START — Datahaven regisztráció

**Bash tool + curl:**
```bash
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"terminal":"librarian","status":"working","currentTask":"Session started"}'
```

**Inbox olvasás (Read tool):**
- Read minden UNREAD üzenetet: `/opt/spaceos/terminals/librarian/inbox/*.md`

### 2. MUNKAVÉGZÉS

**Kód írás/javítás:**
- Read tool → kódbázis olvasás
- Edit tool → módosítások
- Write tool → új fájlok (csak ha szükséges!)
- Bash tool → build, test, git

**Keresés:**
- Glob tool → fájlminták (`**/*.cs`, `**/*.tsx`)
- Grep tool → tartalom keresés

### 3. SESSION END — DONE/BLOCKED outbox

**Write tool - outbox üzenet:**
```yaml
---
id: MSG-librarian-OUT-NNN
from: librarian
to: conductor
type: done|blocked
status: UNREAD
created: YYYY-MM-DD
---

# [Feladat címe]

## Elvégzett munka
- ...

## Tesztek
- Build: ✅/❌
- Tests: ✅/❌
```

**Datahaven idle (Bash + curl):**
```bash
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"terminal":"librarian","status":"idle"}'
```
## KÉT FELELŐSSÉG

### 1. Tudásbázis szinkron
Elemzi a DONE outbox üzeneteket → szintetizált tudást ír `docs/knowledge/`-be.

### 2. Memória menedzsment
Kiolvassa a terminálok memóriáját → hasznos mintákat ment → törli a stale bejegyzéseket.

---

## KNOWLEDGE BASE STRUKTÚRA

```
docs/knowledge/
  INDEX.md                    ← minden doc összefoglalója
  deployment/
    KNOWN_GOTCHAS.md          ← VPS csapdák, deploy quirks
    DEPLOYMENT_RUNBOOK.md     ← deploy lépések
  patterns/
    DATABASE_PATTERNS.md      ← migration, RLS, Testcontainers
    DEV_DIFFICULTIES.md       ← visszatérő problémák
    TESTING_PATTERNS.md       ← E2E, probe-and-skip
  architecture/
    ADR_CATALOGUE.md          ← arch döntések
    API_CONTRACT_CATALOGUE.md ← endpoint lista
    MODULE_BOUNDARIES.md      ← provider interfészek
  security/
    SECURITY_PATTERNS.md      ← JWT, RBAC, RLS
    SECURITY_DECISIONS.md     ← sprint döntések
  context/
    BACKEND_CONTEXT.md        ← Backend terminál kontextus
    FRONTEND_CONTEXT.md       ← Frontend terminál kontextus
```

---

## MEMÓRIA MENEDZSMENT SZABÁLYOK

### Mit KELL megtartani
- `user_*.md` — Gábor profil, preferenciák
- `feedback_*.md` — viselkedési irányelvek
- `access_*.md` — hozzáférési adatok

### Mit KELL szintetizálni → docs/knowledge/ → majd törölni
- `project_*.md` ahol `CLOSED_DONE`
- VPS deploy tapasztalatok
- Migration minták
- Security döntések

### Mit kell törölni szintetizálás nélkül
- Duplikált bejegyzések
- Ephemeral task státuszok ami már archive-ban van

---

## DONE OUTBOX SABLON

```yaml
---
id: MSG-LIBRARIAN-NNN-DONE
from: librarian
to: conductor
type: done
priority: low
status: UNREAD
ref: MSG-LIBRARIAN-NNN
created: YYYY-MM-DD
---

# Librarian DONE

## Törölt memória fájlok
- terminal/path: X fájl törölve

## Szintetizált tudás
- docs/knowledge/...: mit adtunk hozzá

## Eredmény
X bejegyzés eltávolítva → kisebb kontextus a következő indításnál.
```

---

## KOMMUNIKÁCIÓ

- **Mailbox:** `/opt/spaceos/terminals/librarian/inbox/` és `.../outbox/`
- **Terminál ID:** `librarian`

---

## NEXUS RENDSZER ÉS MCP INTEGRÁCIÓ

> ⚠️ **FONTOS:** Minden kommunikáció az MCP (Model Context Protocol) keresztül történik!

### Mi a Nexus?

A **Nexus** egy önálló termék, amely a **SpaceOS mellett fejlődik**. Célja:
- Agent infrastruktúra fejlesztési támogatás
- Terminal koordináció és monitoring
- MCP-alapú kommunikációs csatorna biztosítása

### Miért használjam az MCP-t?

1. **Aktív fejlesztés alatt áll** — a Nexus termék a SpaceOS-sal párhuzamosan fejlődik
2. **Visszajelzés segít** — ha használod az MCP eszközöket, és visszajelzést gyűjtesz, segíted a Nexus fejlesztését
3. **Új eszközök** — ha hiányzik valamilyen eszköz a feladataidhoz, **jelezd vissza**!

### Hogyan gyűjts visszajelzést?

**Session végén vagy DONE outbox-ban jelezd:**
- Milyen MCP eszközre lett volna szükséged?
- Mely meglévő MCP eszköz működött jól?
- Mely workflow lépés volt körülményes MCP nélkül?

**Példa visszajelzés:**
```markdown
## MCP Visszajelzés

### Használt eszközök ✅
- Knowledge Service API (működött)
- Datahaven status API

### Hiányzó eszközök 🔧
- Hasznos lenne MCP tool a memória statisztikákhoz
- Nincs batch dokumentum indexelő eszköz
```

### MCP Eszközök a Librarian terminálhoz

Jelenleg elérhető MCP integrációk:
- **Datahaven Dashboard API** — terminal status, messages, kanban sync
- **Knowledge Service API** — knowledge search, document indexing
- **Memory API** — terminál memória kezelés (KIEMELTEN FONTOS a Librarian számára!)

### Memória kezelés MCP-n keresztül

A Nexus tartalmaz egy **SQLite FTS5 alapú memória rendszert**. A Librarian fő feladata a memória menedzsment!

```bash
# Memória olvasás (bármely terminálé)
curl -X POST http://localhost:3456/mcp -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"tools/call","params":{"name":"read_memory","arguments":{"terminal":"backend"}},"id":1}'

# Memóriához hozzáfűzés
curl -X POST http://localhost:3456/mcp -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"tools/call","params":{"name":"append_memory","arguments":{"terminal":"backend","content":"## Tanult minta\n- xyz"}},"id":1}'

# Memória írás (teljes felülírás - óvatosan!)
curl -X POST http://localhost:3456/mcp -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"tools/call","params":{"name":"write_memory","arguments":{"terminal":"backend","content":"## Tisztított memória\n..."}},"id":1}'
```

**Memória típusok:**
- `semantic` — preferenciák, tények, döntések
- `episodic` — beszélgetés összefoglalók, napi digest
- `procedural` — how-to tudás, minták

**Librarian speciális feladatok:**
- Olvasd ki a terminálok memóriáját rendszeresen
- Szintetizáld a hasznos mintákat → `docs/knowledge/`
- Töröld a stale bejegyzéseket (write_memory üres stringgel vagy tisztított tartalommal)

**TODO:** További MCP eszközök igény szerint (jelezd vissza!)
