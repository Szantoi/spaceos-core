# SpaceOS Rendszerútmutató

**Verzió:** 1.1
**Dátum:** 2026-07-10
**Státusz:** Production Ready

---

## Tartalomjegyzék

1. [Rendszer Áttekintés](#1-rendszer-áttekintés)
2. [Terminál Architektúra](#2-terminál-architektúra)
3. [MCP Tools — API Referencia](#3-mcp-tools--api-referencia)
4. [Mailbox Rendszer](#4-mailbox-rendszer)
5. [Pipeline & Automatizáció](#5-pipeline--automatizáció)
6. [Datahaven Dashboard](#6-datahaven-dashboard)
7. [Session Management](#7-session-management)
8. [Gyakori Műveletek](#8-gyakori-műveletek)
9. [Hibaelhárítás](#9-hibaelhárítás)
10. [NWT Időmérési Rendszer](#10-nwt-időmérési-rendszer)

---

## 1. Rendszer Áttekintés

### Mi a SpaceOS?

A SpaceOS a **magyar faipar digitális gerince** — egy iparspecifikus SaaS platform multi-agent koordinációs rendszerrel.

### Architektúra (4 réteg)

```
L4  Design Portal / JoineryTech   React 18 — brand-specifikus UI-k
L3  Orchestrator (BFF)            Node.js 22 — LLM Tool Calling, API gateway
L2  Modules (Drivers)             .NET 8 — iparági üzleti logika
L1  Kernel                        .NET 8 + PostgreSQL — auth, audit, FSM, escrow
```

### 5 Golden Rule

| # | Szabály |
|---|---|
| 1 | **Data → Rules → Geometry** — frontend rajzol, C# Driver számol |
| 2 | **Modular Monolith** — Kernel interfészen dolgozik |
| 3 | **Immutability & Trust** — SHA-256 hashed audit |
| 4 | **Need-to-Know RBAC** — tenant szeparáció |
| 5 | **Walking Skeleton First** — E2E pipeline előbb |

### Kulcs Komponensek

| Komponens | Port | Leírás |
|-----------|------|--------|
| Knowledge Service | 3456 | MCP server, RAG, mailbox kezelés |
| Datahaven Dashboard | 3457 | Központi monitoring UI |
| ChromaDB | 8000 | Vektor adatbázis |
| Kernel | 5000 | .NET 8 backend |
| Orchestrator | 3000 | Node.js BFF |

---

## 2. Terminál Architektúra

### 12 Aktív Terminál (2 Track)

**Szétválasztás:** Operatív termékfejlesztés vs. Agent infrastruktúra

```
PRIORITY (mindig fut)
  └── ROOT         /opt/spaceos/terminals/root/        ← stratégiai döntések

KOORDINÁTOR
  └── CONDUCTOR    /opt/spaceos/terminals/conductor/   ← feladatkiosztás

SUPPORT
  ├── MONITOR      /opt/spaceos/terminals/monitor/     ← health check
  ├── REVIEWER     /opt/spaceos/terminals/reviewer/    ← DONE review
  ├── ARCHITECT    /opt/spaceos/terminals/architect/   ← arch konzultáció
  ├── LIBRARIAN    /opt/spaceos/terminals/librarian/   ← tudásbázis
  └── EXPLORER     /opt/spaceos/terminals/explorer/    ← codebase kutatás

TERMÉKFEJLESZTÉS (JoineryTech, Doorstar, stb.)
  ├── BACKEND      /opt/spaceos/terminals/backend/     ← .NET + Node.js üzleti logika
  ├── FRONTEND     /opt/spaceos/terminals/frontend/    ← React/TS portal
  └── DESIGNER     /opt/spaceos/terminals/designer/    ← UI/UX

AGENT INFRASTRUKTÚRA (knowledge-service, MCP)
  └── NEXUS        /opt/spaceos/terminals/nexus/       ← agent szerver minőségjavítás

CHAT
  └── CHAT-ROOT    /opt/spaceos/terminals/chat-root/   ← Telegram interface
```

### Két Fejlesztési Track

| Track | Terminálok | Fókusz |
|-------|------------|--------|
| **Termékfejlesztés** | Backend, Frontend, Designer | JoineryTech modulok, Doorstar, üzleti logika |
| **Infrastruktúra** | Nexus | knowledge-service, MCP tools, pipeline, Nightwatch |

### Terminál Könyvtár Struktúra

```
terminals/<terminál>/
  ├── CLAUDE.md      ← terminál identity és szabályok
  ├── MEMORY.md      ← persistent memory
  ├── STATUS.md      ← current state snapshot
  ├── TODO.md        ← aktív feladatok
  ├── inbox/         ← bejövő feladatok (UNREAD → READ)
  ├── outbox/        ← DONE/BLOCKED üzenetek
  ├── archive/       ← lezárt üzenetek
  └── knowledge/     ← domain-specifikus tudás
```

### tmux Session Nevek

| Pattern | Model | Cél |
|---------|-------|-----|
| `spaceos-<role>` | Sonnet | Fő work session |
| `spaceos-<role>-chat` | Haiku | Telegram, gyors válaszok |
| `spaceos-<role>-work-NNN` | Sonnet/Opus | Párhuzamos worker |

---

## 3. MCP Tools — API Referencia

### Knowledge Tools

| Tool | Leírás |
|------|--------|
| `search_knowledge` | Semantic search (4508 doc) |
| `get_skill` | Skill tartalom lekérés |
| `list_skills` | Elérhető skillek |
| `get_workflow` | WORKFLOW.md tartalom |

### Mailbox Tools

| Tool | Leírás |
|------|--------|
| `list_inbox` | Inbox üzenetek listázása |
| `read_inbox_message` | Üzenet olvasása (auto READ) |
| `create_task` | Task létrehozása terminálnak |
| `complete_inbox_message` | Task DONE/BLOCKED lezárása |
| `append_to_message` | Notes/progress hozzáfűzése |

### Identity Tools

| Tool | Leírás |
|------|--------|
| `get_identity` | CLAUDE.md + memory |
| `list_terminals` | Összes terminál |
| `read_memory` | Memory olvasás |
| `write_memory` | Memory írás (felülír!) |
| `append_memory` | Memory bővítés |

### Context Persistence Tools

| Tool | Mikor |
|------|-------|
| `build_session_start_context` | Session start (első 3 perc) |
| `get_context_saturation` | 10-15 turnonként |
| `read_session_state` | Epic/progress check |
| `write_session_state` | Session end |
| `read_terminal_status_md` | Focus check |
| `write_terminal_status_md` | Status update |
| `increment_turn_count` | Turn tracking |
| `reset_turn_count` | Session reset |

### Goal Tools (ADR-059)

| Tool | Leírás |
|------|--------|
| `create_goal` | Goal létrehozás kritériumokkal |
| `list_goals` | Státusz szerinti szűrés |
| `check_goal_criteria` | Kritérium ellenőrzés |
| `trigger_goal` | Goal kiváltás |
| `complete_goal` | Goal lezárás |

### Worker Tools

| Tool | Leírás |
|------|--------|
| `spawn_work_session` | Work session indítás |
| `spawn_parallel_workers` | Párhuzamos workerek |
| `get_worker_status` | Worker állapot + cost |

### Telegram Tools

| Tool | Leírás |
|------|--------|
| `telegram_reply` | Válasz küldés |
| `telegram_broadcast` | Broadcast több terminálnak |
| `get_telegram_history` | Chat history |

---

## 4. Mailbox Rendszer

### Üzenet Formátum

**Fájlnév:** `YYYY-MM-DD_NNN_[slug].md`

```yaml
---
id: MSG-<TERMINAL>-<NNN>
from: root
to: backend
type: task
priority: high
status: UNREAD
model: sonnet
created: 2026-07-10
---

## Feladat címe

[Leírás...]

## Acceptance Criteria

- [ ] AC1
- [ ] AC2
```

### Üzenet Típusok

| Type | Irány | Mikor |
|------|-------|-------|
| `task` | Root/Conductor → Terminal | Feladat kiosztás |
| `question` | Bármely irány | Döntés kell |
| `info` | Bármely irány | Tájékoztatás |
| `blocked` | Terminal → Root | Elakadás |

### Priority Szintek

| Priority | Leírás |
|----------|--------|
| `critical` | Azonnali kezelés |
| `high` | Napi prioritás |
| `medium` | Normál ütemezés |
| `low` | Ha van idő |

### Model Választás

| Model | Mikor |
|-------|-------|
| `haiku` | Kis feladat, keresés, összefoglaló |
| `sonnet` | Kód, napi fejlesztés *(default)* |
| `opus` | Architektúra, komplex tervezés |

---

## 5. Pipeline & Automatizáció

### Nightwatch Pipeline (*/2 cron)

```
nightwatch.sh
  ├── watchPriority   → Root + Conductor MINDIG fut
  ├── watchDone       → DONE → reviewer (2× Haiku) → pipeline
  ├── watchStuck      → Stuck session nudge
  ├── watchInbox      → UNREAD → session start
  └── watchMonitor    → Health check (5 ciklusonként)
```

### DONE Feldolgozás

```
Terminal DONE outbox
  ↓
watchDone detektál
  ↓
Reviewer (2× párhuzamos Haiku)
  ↓
APPROVE/APPROVE → Pipeline (status frissítés, archive)
APPROVE/REJECT  → Conductor dönt
REJECT/REJECT   → Visszadobás
```

### Context Saturation Thresholds

| Turn | Status | Teendő |
|------|--------|--------|
| 0-29 | ✅ OK | Normál működés |
| 30-49 | ⚠️ WARNING | Fókuszálj, olvasd újra a célt |
| ≥50 | 🚨 CRITICAL | Session re-anchor vagy reset |

---

## 6. Datahaven Dashboard

### URL & Auth

- **URL:** https://datahaven.joinerytech.hu
- **Token:** `dev-token-spaceos-dashboard-2026`

### 4 Fő Oldal

| Oldal | URL | Mit mutat |
|-------|-----|-----------|
| Dashboard | `/` | Terminál állapotok, inbox/outbox metrikák |
| Kanban | `/kanban` | Dual-track: Discovery + Delivery |
| Planning | `/planning` | Pipeline: Idea → Consensus → Queue |
| Projects | `/projects` | Gantt timeline |

### API Endpointok

```bash
# Dashboard összefoglaló
curl -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  https://datahaven.joinerytech.hu/api/dashboard

# Terminál státusz regisztráció
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"terminal":"root","status":"working","currentTask":"..."}'
```

---

## 7. Session Management

### Session Indítás

```bash
# 1. tmux session létrehozás
tmux new-session -d -s spaceos-<terminal> -c /opt/spaceos/terminals/<terminal>

# 2. Claude indítás
tmux send-keys -t spaceos-<terminal> "claude --model <model>" C-m

# 3. Prompt küldés (várj 5 sec a Claude indulásra)
sleep 5
tmux send-keys -t spaceos-<terminal> "Olvasd el a CLAUDE.md-t és dolgozd fel az inbox-ot" C-m
```

### MCP Session API

```bash
# Session indítás
curl -X POST http://localhost:3456/api/session/start \
  -H "Content-Type: application/json" \
  -d '{"terminal":"backend","model":"sonnet","prompt":"...","fromTerminal":"root"}'

# Session státusz
curl http://localhost:3456/api/sessions/all

# Terminal working regisztráció
curl -X POST http://localhost:3456/api/terminal/register-working \
  -H "Content-Type: application/json" \
  -d '{"terminal":"backend","taskId":"MSG-BACKEND-100"}'
```

### tmux Parancsok

```bash
# Sessionök listázása
tmux list-sessions

# Session kimenet megtekintése
tmux capture-pane -t spaceos-<terminal> -p | tail -20

# Enter küldés (nudge)
tmux send-keys -t spaceos-<terminal> "" C-m

# Session kilövés
tmux kill-session -t spaceos-<terminal>
```

---

## 8. Gyakori Műveletek

### Task Létrehozás

```bash
# MCP-vel
mcp__spaceos-knowledge__create_task
  from: "root"
  to: "backend"
  title: "Implement XYZ endpoint"
  description: "..."
  priority: "high"
  model: "sonnet"
```

### Task Lezárás

```bash
# MCP-vel
mcp__spaceos-knowledge__complete_inbox_message
  terminal: "backend"
  message_id: "MSG-BACKEND-100"
  status: "done"
  summary: "Implemented XYZ..."
  files_changed: ["src/Endpoints/XyzEndpoint.cs"]
```

### Inbox Ellenőrzés

```bash
# MCP-vel
mcp__spaceos-knowledge__list_inbox
  terminal: "backend"
  status: "UNREAD"

# Bash-sel
grep -rl "status: UNREAD" /opt/spaceos/terminals/backend/inbox/
```

### Knowledge Keresés

```bash
# MCP-vel
mcp__spaceos-knowledge__search_knowledge
  query: "RLS tenant isolation"
  limit: 5

# API-val
curl "http://localhost:3456/api/knowledge/search?q=RLS+tenant"
```

### Service Státusz

```bash
# MCP-vel
mcp__spaceos-knowledge__get_service_status

# API-val
curl http://localhost:3456/health
```

---

## 9. Hibaelhárítás

### Knowledge Service Nem Indul

```bash
# 1. Státusz ellenőrzés
sudo systemctl status spaceos-knowledge

# 2. Log ellenőrzés
sudo journalctl -u spaceos-knowledge -n 50

# 3. Restart
sudo systemctl restart spaceos-knowledge

# 4. Ha DB korrupt
rm /opt/spaceos/spaceos-nexus/knowledge-service/data/message_registry.db
sudo systemctl restart spaceos-knowledge
```

### Session Stuck

```bash
# 1. Nudge (Enter küldés)
tmux send-keys -t spaceos-<terminal> "" C-m

# 2. Ha nem reagál, kill + restart
tmux kill-session -t spaceos-<terminal>
# ... majd újraindítás

# 3. Idle regisztráció
mcp__spaceos-knowledge__register_idle
  terminal: "<terminal>"
```

### Inbox Flooding

```bash
# 1. Fájlok archiválása
cd /opt/spaceos/terminals/<terminal>/inbox
mkdir -p ../archive/2026-07-cleanup
mv *.md ../archive/2026-07-cleanup/

# 2. Root cause keresés (pl. testMode)
grep -r "testMode = true" /opt/spaceos/spaceos-nexus/knowledge-service/src/
```

### KNOWN GOTCHAS (Top 5)

| # | Probléma | Megoldás |
|---|----------|----------|
| 1 | `.js` extension TypeScript importban | Töröld a `.js`-t CommonJS-nél |
| 2 | `testMode = true` productionben | Állítsd `false`-ra |
| 3 | Üres `message_registry.db` | Töröld, restart service |
| 4 | tmux `Enter` literal szövegként | Használj `C-m` vagy külön `send-keys` |
| 5 | Session nem indul MCP-vel | Ellenőrizd a port 3456 elérhető-e |

**Teljes lista:** `docs/knowledge/deployment/KNOWN_GOTCHAS.md`

---

## 10. NWT Időmérési Rendszer

### Mi az NWT?

**1 NWT = 2 perc = 1 Nightwatch ciklus**

Az agent munkát NWT-ben mérjük, nem napokban.

### Skálák

| Skála | NWT | Emberi idő |
|-------|-----|------------|
| TICK | 1 | 2 perc |
| SHORT_TASK | 15 | 30 perc |
| STANDARD_TASK | 30 | 1 óra |
| MEDIUM_FEATURE | 60 | 2 óra |
| LARGE_FEATURE | 120 | 4 óra |
| WORK_DAY | 240 | 8 óra |

### Timeout Konstansok

| Timeout | NWT | Cél |
|---------|-----|-----|
| STUCK_NUDGE | 2 | Session nudge |
| INBOX_NUDGE | 3 | UNREAD figyelmeztetés |
| IDLE_WARNING | 5 | Idle figyelmeztetés |
| TASK_WARNING | 15 | Task túl sokáig |
| TASK_ESCALATE | 60 | Eszkaláció |

### Context Saturation

| Threshold | NWT | Turn | Akció |
|-----------|-----|------|-------|
| WARNING | 15 | ~30 | Figyelmeztetés |
| CRITICAL | 25 | ~50 | Re-anchor |

---

## Appendix A: Gyors Referencia

### Service Portok

| Service | Port |
|---------|------|
| Knowledge Service | 3456 |
| Datahaven Web | 3457 |
| ChromaDB | 8000 |
| Kernel | 5000 |
| Orchestrator | 3000 |

### Fontos Fájlok

| Fájl | Cél |
|------|-----|
| `/opt/spaceos/CLAUDE.md` | Root projekt CLAUDE.md |
| `/opt/spaceos/docs/WORKFLOW.md` | Teljes workflow leírás |
| `/opt/spaceos/docs/Codebase_Status.md` | Modul státuszok |
| `/opt/spaceos/docs/projects/EPICS.yaml` | Epic dependency gráf |
| `/opt/spaceos/docs/knowledge/INDEX.md` | Tudásbázis index |

### Log Fájlok

| Log | Hol |
|-----|-----|
| Nightwatch | `/opt/spaceos/logs/dispatcher/nightwatch.log` |
| Pipeline | `/opt/spaceos/logs/dispatcher/pipeline.log` |
| Session | `/opt/spaceos/logs/sessions/` |

---

*Készítette: Root terminál*
*Frissítve: 2026-07-10*
