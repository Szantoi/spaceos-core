# SpaceOS Nexus — Agent Infrastructure Roadmap

> Három módszer szintézise: JoineryTech.McpServer + Marvin + SpaceOS bash pipeline
> Fejlesztő terminál: NEXUS (`/opt/spaceos/spaceos-nexus/`)
> Tervdokumentum: `docs/tasks/new/SpaceOS_Marvin_McpServer_Migration_v*.md` (Architect készíti)

---

## Fázis 1 — McpServer Knowledge Service
**Státusz:** ✅ KÉSZ (2026-06-17)

- [x] JoineryTech.McpServer klónozás → `spaceos-nexus/mcp-server/`
- [x] `indexKnowledgeBase.ts` adaptáció: `docs/knowledge/**/*.md` olvasás
  - Megvalósítva: `spaceos-nexus/knowledge-service/src/indexer.ts`
  - Minden `.md` fájlt indexel (nem csak `.knowledge.md`)
- [x] Gemini → voyage-3-lite embedding csere
  - Megvalósítva: `spaceos-nexus/knowledge-service/src/embeddings.ts`
  - Priority: Voyage AI → Google Gemini → Local
  - API: `https://api.voyageai.com/v1/embeddings` · Env: `VOYAGE_API_KEY`
- [x] ChromaDB Docker service (`docker-compose.yml`)
  - `spaceos-nexus/docker-compose.yml` · Port: 8001 → Fut (`spaceos_chromadb` konténer)
- [x] Engineering knowledge fájlok átmásolva → `docs/knowledge/engineering/`
- [x] `POST /api/knowledge/search` endpoint implementálva
  - Kész: `spaceos-nexus/knowledge-service/src/server.ts` (port 3456)
  - **1106 dokumentum indexelve**

## Fázis 1.5 — Production Hardening (MSG-NEXUS-009)
**Státusz:** ✅ KÉSZ (2026-06-20)

- [x] Systemd service (`spaceos-knowledge.service`) — auto-restart, journalctl
- [x] Librarian pipeline integration — `pipeline.sh` reindex trigger
- [x] Haiku scanner tool — `search_knowledge` MCP tool elérhető
- [x] HTTPS endpoint: `https://nexus.joinerytech.hu/mcp`
- [x] Bearer token auth
- [x] 20 MCP tool 6 kategóriában

## Fázis 2 — Marvin Planning Pipeline
**Státusz:** VÁRAKOZÁS (Fázis 1 után)

- [ ] Marvin telepítés (`pip install marvin`)
- [ ] Agent definíciók: scanner, selector, debater_a, debater_b, synthesizer
- [ ] plan-scan.sh → Marvin Task
- [ ] plan-select.sh → Marvin Task (WebSearch tool)
- [ ] plan-debate.sh → Marvin Tasks (párhuzamos)
- [ ] McpServer tool bekötve Marvin-ba
- [ ] Bash cron kikapcs → Marvin Scheduler

## Fázis 3 — Marvin Reviewer + Nightwatch
**Státusz:** VÁRAKOZÁS (Slice 2 előtt)

- [ ] reviewer.sh → Marvin Task
- [ ] nightwatch.sh → Marvin Scheduler
- [ ] WorkflowStateTracker bekötés
- [ ] RbacFilter bekötés

---

## Referencia Projektek Összehasonlítása (2026-06-20)

> Négy projekt szintézise: egy saját inspiráció + két külső framework + a jelenlegi SpaceOS.

### Repók

| Projekt | URL | Nyelv | Fókusz |
|---------|-----|-------|--------|
| **JoineryTech.McpServer** | https://github.com/Szantoi/JoineryTech.McpServer | TypeScript | MCP protokoll, RBAC, RAG, FSM, GuardrailService |
| **Marveen** | https://github.com/Szotasz/marveen | TypeScript/Node.js | Claude Code alapú multi-agent fleet, Kanban, Telegram |
| **Marvin** | https://github.com/PrefectHQ/marvin | Python | LLM orchestráció, Tasks, Threads, Prefect integráció |
| **SpaceOS** | (ez a repo) | Bash + Node.js | Mailbox workflow, planning pipeline, terminál architektúra |

---

### JoineryTech.McpServer — Alapító Inspiráció

> **Ez a SpaceOS Nexus architektúra eredeti forrása.** Szántói Gábor saját projektje,
> amelyet a SpaceOS agent infrastruktúra alapjául használunk.

#### Architektúra

```
AI Agent Request
      ↓
┌─────────────────┐
│   RBAC Filter   │  ← Szerepkör alapú tool szűrés
└────────┬────────┘
         ↓
┌─────────────────┐
│   MCP Router    │  ← Model Context Protocol endpoints
└────────┬────────┘
         ↓
┌─────────────────────────────────────────────┐
│              DocumentServer                  │
│  (database/ mappa = single source of truth) │
└─────────────────────────────────────────────┘
         ↓                    ↓
┌─────────────────┐  ┌─────────────────┐
│ GuardrailService│  │WorkflowStateTracker│
│ (LLM compliance)│  │   (FSM/SQLite)     │
└─────────────────┘  └─────────────────────┘
```

#### Kulcs komponensek

| Komponens | Leírás | SpaceOS megfelelő |
|-----------|--------|-------------------|
| **RbacFilter** | Tool visibility szerepkör alapján | `CLAUDE.md` per-terminál |
| **GuardrailService** | Post-execution LLM compliance | `reviewer.sh` (dual Haiku) |
| **WorkflowStateTracker** | FSM + SQLite state | Mailbox státuszok (YAML) |
| **ResourceTracker** | Artifact registry | `Codebase_Status.md` |
| **DocumentServer** | Single source of truth olvasás | `docs/` mappa |
| **VectorStore** | ChromaDB RAG | `spaceos-nexus/knowledge-service/` |

#### Technológiák

- **TypeScript 92.5%** + Express.js
- **MCP protokoll** — AI agent tool interface
- **SQLite** — state és resource tracking
- **ChromaDB** — vector embeddings (RAG)
- **Playwright** — E2E tesztek

#### Kapcsolódó repók

- `JoineryTech.AgentScripts` — PowerShell workflow runner
- `JoineryTech.Flow` — .NET API + React frontend

#### SpaceOS-ba adoptált elemek (Fázis 1)

1. ✅ **ChromaDB VectorStore** → `spaceos-nexus/knowledge-service/`
2. ✅ **Knowledge indexer** → `docs/knowledge/**/*.md` indexelés
3. ⏳ **RBAC pattern** → terminál-specifikus tool access (Fázis 2)
4. ⏳ **GuardrailService** → `reviewer.sh` bővítése LLM compliance-szel (Fázis 3)
5. ⏳ **WorkflowStateTracker** → FSM a mailbox státuszokhoz (Fázis 3)

---

### Feature Mátrix

| Aspektus | **Marveen** | **Marvin** (PrefectHQ) | **SpaceOS** |
|----------|-------------|------------------------|-------------|
| **Task tárolás** | SQLite + Kanban | Runtime/Pydantic | Markdown fájlok (git) |
| **Agent comm** | Telegram/Slack bot | Python API | Mailbox fájlok |
| **Orchestráció** | Tmux sessions | Async Threads | Tmux + cron |
| **Memory** | SQLite FTS5 + vector | Thread context | Git history |
| **Scheduling** | Cron + Heartbeats | Prefect Scheduler | nightwatch.sh |
| **Skills** | Skill Factory (auto) | - | `.claude/skills/` |
| **Dashboard** | Mission Control (:3420) | Prefect UI | Datahaven (:3457) |
| **Type safety** | TypeScript | Pydantic | YAML schema |
| **Audit trail** | SQLite log | - | Git history ✓ |

### Marveen-ből adoptálható ötletek

1. **SQLite + FTS5 + vector search** — gyorsabb mint fájl grep, de megmarad az audit
2. **Heartbeats** — silent monitoring, csak exception-re alert (nightwatch kiegészítés)
3. **Skill Factory** — automatikus skill generálás munkából (retrospective skill)
4. **Vault encryption** — AES-256-GCM credentials (MCP secrets)
5. **Kanban UI** — beépített task board (Datahaven bővítés)

### Marvin-ból adoptálható ötletek

1. **Pydantic Tasks** — type-safe task definíciók a planning pipeline-hoz
2. **Threads** — shared context több task között
3. **Async orchestration** — párhuzamos reviewer/debater futtatás
4. **Prefect observability** — task execution dashboard

### SpaceOS egyedi erősségei (megtartandó)

1. **Git audit trail** — minden döntés verziókövetett, compliance-ready
2. **Planning pipeline** (plan-scan → debate → queue) — strukturált AI döntéshozatal
3. **Dual-reviewer** — két független Haiku vélemény a code review-ra
4. **Mailbox routing** — explicit terminál felelősség és ownership
5. **Human-in-the-loop** — egyszerű kézi beavatkozás (fájl szerkesztés)

---

## Integrációs Döntések (2026-06-20)

> Elemzés alapja: split-brain kockázat elkerülése.
> **Szabály:** Ha az elem **írja az állapotot** → SKIP. Ha csak **olvassa/indexeli** → ADOPT.

### ADOPT ✅ (implementálandó)

| Elem | Forrás | Indoklás | Hol fut |
|------|--------|----------|---------|
| **MCP Server (SpaceOS adaptáció)** | JoineryTech | Claude Code toolok: `search_knowledge`, `list_inbox`, `get_status` | Lokális + VPS |
| **ChromaDB RAG** | JoineryTech | ✅ MÁR KÉSZ. Read-only index a `docs/knowledge/`-ból | Lokális + VPS |
| **Heartbeats monitoring** | Marveen | Silent watch, exception-re alert. Kiegészíti a nightwatch-ot | VPS |
| **Kanban UI** | Marveen | Datahaven bővítés. Vizualizáció, az adat továbbra is fájl | VPS |

### CONSIDER ⚠️ (feltételesen)

| Elem | Forrás | Feltétel |
|------|--------|----------|
| **GuardrailService** | JoineryTech | Csak ha a `reviewer.sh` 2× Haiku nem elég |
| **Async orchestration** | Marvin | Csak ha a plan-debate párhuzamosítás szükséges |
| **Pydantic Tasks** | Marvin | Csak ha type-safety problémává válik |

### SKIP ❌ (over-engineering vagy konfliktus)

| Elem | Forrás | Indoklás |
|------|--------|----------|
| **WorkflowStateTracker (SQLite FSM)** | JoineryTech | Split-brain kockázat. A YAML frontmatter már FSM. |
| **SQLite + FTS5 task storage** | Marveen | Git audit trail elveszne. Fájl-alapú = compliance-ready. |
| **Vault encryption** | Marveen | Túlzás. `.env` + `.gitignore` elegendő. |
| **Prefect Scheduler** | Marvin | Cron működik. Prefect overkill 1-gépes rendszerhez. |
| **RBAC Filter (runtime)** | JoineryTech | A `CLAUDE.md` per-terminál már megoldja. |
| **Skill Factory (auto)** | Marveen | `.claude/skills/` manuális, de működik. Auto-gen kockázatos. |

---

## Multi-Site Architektúra (Lokális + VPS)

> Cél: Lokális gépen fejlesztés, VPS-en éles agent fleet.

```
┌─────────────────────────────────────────────────────────────┐
│                      LOKÁLIS GÉP                            │
│                                                             │
│  Claude Code ←──MCP──→ SpaceOS MCP Server                  │
│       │                      │                              │
│       │                      ├── search_knowledge (RAG)     │
│       │                      ├── list_inbox                 │
│       │                      ├── get_task_status            │
│       │                      └── submit_done                │
│       │                                                     │
│       └──────────────→ git push ────────────────────────────┤
└─────────────────────────────────────────────────────────────┘
                               │
                          GitHub repo
                               │
┌─────────────────────────────────────────────────────────────┐
│                           VPS                               │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  Agent Fleet (tmux)                                 │   │
│  │  • nightwatch.sh + heartbeats                       │   │
│  │  • reviewer.sh (2× Haiku)                           │   │
│  │  • pipeline.sh                                       │   │
│  └─────────────────────────────────────────────────────┘   │
│                         │                                   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  Datahaven Dashboard (:3457)                        │   │
│  │  • Kanban UI (mailbox vizualizáció)                 │   │
│  │  • Planning pipeline view                           │   │
│  │  • Heartbeats status                                │   │
│  └─────────────────────────────────────────────────────┘   │
│                         │                                   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  Knowledge Service (:3456)                          │   │
│  │  • ChromaDB RAG                                     │   │
│  │  • POST /api/knowledge/search                       │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### Szinkronizáció

| Adat | Forrás | Szinkron mód |
|------|--------|--------------|
| Kód, tervdokumentumok | Git | `git push/pull` |
| Mailbox üzenetek | Git | `git push/pull` (fájl-alapú) |
| Knowledge index | ChromaDB | Mindkét oldalon független index, azonos forrás (`docs/knowledge/`) |
| Task státusz | YAML frontmatter | Git-ben verziókövetett |

### Következő lépések

1. **MCP Server adaptáció** — JoineryTech.McpServer → SpaceOS toolok
2. **Lokális ChromaDB** — Docker compose lokálisan is
3. **Heartbeats** — nightwatch.sh kiegészítés exception alertekkel
4. **Kanban UI** — Datahaven `/kanban` route

---

## Centralizált MCP Server Architektúra

> Alternatíva a git-alapú szinkronhoz: VPS-en futó központi MCP Server,
> amelyhez minden kliens (lokális + VPS terminálok) csatlakozik.

```
┌─────────────────────────────────────────────────────────────────┐
│                         VPS (központ)                           │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  SpaceOS MCP Server (:3456)                             │   │
│  │                                                         │   │
│  │  Toolok:                                                │   │
│  │  ├── search_knowledge    (RAG query)                    │   │
│  │  ├── list_inbox          (terminál inbox olvasás)       │   │
│  │  ├── get_task_status     (task állapot)                 │   │
│  │  ├── submit_done         (DONE outbox írás)             │   │
│  │  ├── send_message        (inbox küldés terminálnak)     │   │
│  │  ├── subscribe_inbox     (WebSocket élő figyelés)       │   │
│  │  ├── announce            (broadcast mindenkinek)        │   │
│  │  └── get_codebase_status (projekt állapot)              │   │
│  │                                                         │   │
│  │  + WebSocket/SSE az élő szinkronhoz                     │   │
│  └─────────────────────────────────────────────────────────┘   │
│                              ↑                                  │
│                              │ HTTPS + WSS                      │
└──────────────────────────────┼──────────────────────────────────┘
                               │
        ┌──────────────────────┼──────────────────────┐
        │                      │                      │
        ▼                      ▼                      ▼
┌──────────────┐      ┌──────────────┐      ┌──────────────┐
│ Lokális gép  │      │ VPS terminál │      │ Másik lokál  │
│ (fejlesztő)  │      │ (fleet)      │      │ (másik dev)  │
│              │      │              │      │              │
│ Claude Code  │      │ Claude Code  │      │ Claude Code  │
│     ↓        │      │     ↓        │      │     ↓        │
│ MCP Client ──┼──────┼─► VPS :3456 ◄┼──────┼── MCP Client │
└──────────────┘      └──────────────┘      └──────────────┘
```

### Előnyök vs hátrányok

| Aspektus | Centralizált MCP | Git-alapú szinkron |
|----------|------------------|-------------------|
| **Single source of truth** | ✅ Egy helyen | ✅ Git repo |
| **Kommunikáció** | ✅ `send_message` tool | ⚠️ Fájl + git push |
| **Szinkron sebesség** | ✅ Azonnali | ⚠️ Push/pull ciklus |
| **Élő értesítés** | ✅ WebSocket | ❌ Polling kell |
| **Offline munka** | ❌ Nem működik | ✅ Működik |
| **Audit trail** | ⚠️ Explicit log kell | ✅ Git history |

### MCP Tool Specifikáció

```typescript
// Kommunikációs toolok
interface McpTools {
  // Inbox küldés másik terminálnak
  send_message: {
    to: "root" | "conductor" | "kernel" | "fe" | "joinery" | ...;
    type: "task" | "question" | "done" | "blocked";
    content: string;
    priority: "critical" | "high" | "medium" | "low";
    ref?: string;  // kapcsolódó MSG ID
  };

  // Inbox olvasás
  list_inbox: {
    terminal: string;
    status?: "UNREAD" | "READ" | "all";
  };

  // Élő figyelés (WebSocket subscription)
  subscribe_inbox: {
    terminal: string;
  };

  // Broadcast (mindenkinek)
  announce: {
    message: string;
    priority: "critical" | "info";
  };

  // RAG keresés
  search_knowledge: {
    query: string;
    limit?: number;
  };

  // Task státusz
  get_task_status: {
    task_id?: string;  // ha nincs, összes aktív
  };

  // DONE beküldés
  submit_done: {
    task_id: string;
    summary: string;
    files_changed: string[];
  };
}
```

### Lokális konfiguráció

```json
// ~/.claude/mcp.json (lokális gépen)
{
  "servers": {
    "spaceos": {
      "transport": "sse",
      "url": "https://spaceos-vps.example.com:3456/mcp",
      "headers": {
        "Authorization": "Bearer ${SPACEOS_MCP_TOKEN}"
      }
    }
  }
}
```

### Biztonsági követelmények

1. **HTTPS + WSS** — titkosított kapcsolat
2. **Bearer token** — terminál azonosítás
3. **Rate limiting** — túlterhelés védelem
4. **Audit log** — minden MCP hívás naplózva (git-be sync)

### Implementációs sorrend

1. **Fázis 1** (mostani): Knowledge service (`search_knowledge`) — ✅ KÉSZ
2. **Fázis 2**: Mailbox toolok (`list_inbox`, `send_message`, `submit_done`) — ✅ KÉSZ (2026-06-20)
3. **Fázis 3**: SSE (`subscribe_inbox`, `broadcast`, élő értesítések) — ✅ KÉSZ (2026-06-20)
4. **Fázis 4**: MCP Protocol + Claude Code integráció — ✅ KÉSZ (2026-06-20)
5. **Fázis 5**: Marvin integráció + HTTPS

---

## Operációs megjegyzések

### Tmux terminál indítás — késleltetett Enter

A Claude Code prompt beírása után késleltetni kell az Enter-t, különben beragad:

```bash
# HELYES — 2 másodperc késleltetés
tmux send-keys -t spaceos-nexus "Prompt szöveg" && sleep 2 && tmux send-keys -t spaceos-nexus Enter

# HIBÁS — azonnali Enter beragad
tmux send-keys -t spaceos-nexus "Prompt szöveg" Enter
```
