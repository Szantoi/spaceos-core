# NEXUS Memory

Utolsó frissítés: 2026-06-20

## Aktuális állapot

**PRODUCTION READY** — HTTPS MCP endpoint publikusan elérhető

### Befejezett fázisok
- **Phase 1**: Knowledge Service (RAG) — ChromaDB + Voyage AI ✅
- **Phase 2**: Mailbox Tools — list_inbox, send_message, submit_done, get_task_status ✅
- **Phase 3**: SSE Live Notifications — subscribe, broadcast, event emitter ✅
- **Phase 4**: MCP Protocol — JSON-RPC 2.0, 6 tool, Claude Code integráció ✅
- **Phase 4.5**: HTTPS + Auth — publikus endpoint, systemd service ✅
- **Phase 5**: TypeScript Nightwatch Scheduler — bash → TS migráció, 2 percenként fut ✅
- **Phase 5.5**: TypeScript Reviewer Pipeline — Anthropic SDK dual review, bash reviewer.sh kiváltva ✅
- **Phase 6**: React Datahaven Frontend — Planning + Projects pages, Gantt chart, workflow viz ✅
- **Phase 6.5**: Dashboard & Kanban APIs — Real-time file system scanning, dual-track kanban ✅

### Service státusz
- **Publikus URL (MCP)**: `https://nexus.joinerytech.hu`
- **Datahaven Dashboard**: `https://datahaven.joinerytech.hu`
- **MCP endpoint**: `https://nexus.joinerytech.hu/mcp`
- **Health check**: `https://nexus.joinerytech.hu/health`
- ChromaDB: `localhost:8001`
- 1106 dokumentum indexelve
- Systemd service: `spaceos-knowledge`
- **Nightwatch Scheduler**: ENABLED (2 percenként, TypeScript pipeline)

## Publikus MCP hozzáférés

### Claude Code konfiguráció (távoli gép)
```json
// ~/.claude/mcp.json vagy projekt .mcp.json
{
  "mcpServers": {
    "spaceos-knowledge": {
      "type": "http",
      "url": "https://nexus.joinerytech.hu/mcp",
      "timeout": 60000,
      "headers": {
        "Authorization": "Bearer IoUpLUgr4v6Mj5lt4u2XD1JOy5iGmVdxne473srMl2o="
      }
    }
  }
}
```

### Teszt curl-lel
```bash
# Health check (no auth)
curl https://nexus.joinerytech.hu/health

# MCP tools list (with auth)
curl -X POST https://nexus.joinerytech.hu/mcp \
  -H 'Authorization: Bearer IoUpLUgr4v6Mj5lt4u2XD1JOy5iGmVdxne473srMl2o=' \
  -H 'Content-Type: application/json' \
  -d '{"jsonrpc":"2.0","method":"tools/list","id":1}'
```

### MCP Tools (23 db)

**Knowledge:**
- `search_knowledge` — RAG keresés a tudásbázisban (1106 doc)

**Mailbox:**
- `list_inbox` — terminál inbox listázása
- `send_message` — üzenet küldés terminálnak
- `submit_done` — DONE outbox létrehozása

**Tasks:**
- `get_task_status` — task státusz lekérdezés

**Identity:**
- `get_identity` — terminál CLAUDE.md + memory lekérdezése
- `list_terminals` — összes SpaceOS terminál listázása (19 db)
- `read_memory` — terminál memory fájl olvasása
- `write_memory` — terminál memory felülírása
- `append_memory` — terminál memory bővítése

**Skills & Workflow:**
- `list_skills` — összes skill listázása (24 db)
- `get_skill` — skill SKILL.md + references olvasása
- `get_workflow` — WORKFLOW.md teljes tartalma
- `get_terminal_setup` — terminál telepítési csomag (CLAUDE.md + skill + MCP config)
- `get_project_context` — project vízió, knowledge index, codebase status

**Terminal Docs:**
- `list_terminal_docs` — 17 terminál docs mappa listázása (port, type, README)
- `get_terminal_docs` — terminál README.md + INDEX.md
- `get_terminals_index` — terminál architektúra INDEX.md

**Terminal Status:**
- `register_working` — terminál WORKING státuszba állítása
- `register_idle` — terminál IDLE státuszba állítása
- `get_terminal_status` — terminál aktuális állapota

**System:**
- `get_capabilities` — elérhető toolok kategóriánként
- `get_service_status` — service health check

## Infrastruktúra

### Systemd service
```bash
sudo systemctl status spaceos-knowledge
sudo systemctl restart spaceos-knowledge
sudo journalctl -u spaceos-knowledge -f
```

### Nginx config
- `/etc/nginx/sites-enabled/nexus-knowledge`
- SSL cert: `/etc/letsencrypt/live/joinerytech.hu/` (includes nexus subdomain)

### Log fájlok
- `/var/log/spaceos/knowledge-service.log`

### Kód struktúra
```
spaceos-nexus/knowledge-service/
├── src/
│   ├── server.ts         ← Express + SSE + MCP mount + Rate Limiting + Graceful Shutdown
│   ├── mcp.ts            ← MCP JSON-RPC protocol handler (23 tools)
│   ├── mailbox.ts        ← Mailbox file operations
│   ├── vectorStore.ts    ← ChromaDB client
│   ├── embeddings.ts     ← Voyage AI / Gemini / Local
│   ├── identity.ts       ← Terminal identity + memory kezelés
│   ├── skills.ts         ← Skill + workflow + terminal setup
│   ├── indexer.ts        ← Markdown indexing
│   ├── inboxWatcher.ts   ← Chokidar fájlfigyelő UNREAD inbox-okra
│   ├── terminalStatus.ts ← WORKING/IDLE állapot tracking
│   ├── sessionStarter.ts ← Tmux session indítás (ALLOWED_TERMINALS whitelist)
│   └── pipeline/         ← Bash → TypeScript migráció [2026-06-20]
│       ├── index.ts      ← Module exports
│       ├── common.ts     ← Shared utilities
│       ├── watchIdle.ts  ← Idle session shutdown
│       ├── watchStuck.ts ← Stuck session nudge
│       ├── watchPriority.ts ← Priority session management
│       ├── reviewer.ts   ← Anthropic SDK dual Haiku review (bash reviewer.sh kiváltva)
│       ├── pipeline.ts   ← Post-review actions (archive, notify, reindex)
│       ├── watchDone.ts  ← DONE → reviewer trigger
│       └── nightwatch.ts ← Main dispatcher
├── .env                  ← API keys + MCP_AUTH_TOKEN (chmod 600!)
```

### Wake-on-inbox rendszer (2026-06-20)

Real-time értesítés termináloknak új inbox üzenetről.

**Komponensek:**
- `inboxWatcher.ts` — Chokidar v5 fájlfigyelő (recursive, polling mode)
- `terminalStatus.ts` — Terminal állapot tracking (WORKING/IDLE)
- `sessionStarter.ts` — Tmux session indítás (model + workdir) [2026-06-20]
- SSE Bridge — Csak IDLE terminálokat ébreszti

**Működés:**
1. Chokidar figyeli `/opt/spaceos/docs/mailbox/*/inbox/` mappákat
2. Új `.md` fájl → YAML frontmatter parse → UNREAD ellenőrzés
3. Ha UNREAD → ellenőrzi terminal WORKING/IDLE állapotot
4. Ha IDLE → SSE wake_up event küldés + tmux session indítás
5. Ha WORKING → nincs értesítés (nem zavarjuk)
6. Session indítás: `tmux -S /tmp/spaceos.tmux new-session -d -s spaceos-<terminal>`
7. Claude indítás: `claude --model <frontmatter model>`

**FONTOS:** Systemd `PrivateTmp=false` szükséges a tmux socket eléréséhez!

**API:**
```bash
# Terminal státusz regisztráció
POST /api/terminal/:terminal/status
  { "state": "working", "taskId": "MSG-XXX" }
  { "state": "idle" }

# SSE subscription (wake-on-inbox)
GET /api/mailbox/:terminal/subscribe
```

**MCP Tools (új):**
- `register_working` — session indításakor hívandó
- `register_idle` — session végén hívandó
- `get_terminal_status` — állapot lekérdezés

### Session Management Scripts (2026-06-20)

Költségoptimalizálás: csak aktív terminálok futnak.

**Új szkriptek:**

| Szkript | Funkció |
|---------|---------|
| `watch-idle.sh` | 15+ perc idle + 0 UNREAD → session leállítás |
| `session-status.sh` | Real-time dashboard (futó sessionök, UNREAD-ek, RAM) |

**watch-idle.sh logika:**
1. Végigmegy futó tmux sessionökön
2. Priority session (root, conductor) → kihagyás
3. Van UNREAD inbox? → kihagyás (van feladata)
4. 15+ perc idle? → graceful shutdown (/exit + kill-session)
5. Telegram értesítés: "💤 TERMINAL leállítva"

**session-status.sh használat:**
```bash
bash /opt/spaceos/scripts/session-status.sh        # Dashboard
bash /opt/spaceos/scripts/session-status.sh --json # API output
```

**watch-stuck.sh javítás:**
- Eltávolítva: "input field" és "empty prompt" detektálás
- Meghagyva: csak "queued-messages" és "model-selector"
- Eredmény: spam értesítések megszűntek

**Nightwatch pipeline:**
```
nightwatch.sh (*/2 cron)
├── watch-priority.sh  → Root/Conductor mindig fut
├── watch-done.sh      → DONE feldolgozás
├── watch-stuck.sh     → Model-selector kezelés
├── watch-inbox.sh     → Inbox-alapú indítás
└── watch-idle.sh      → Költségoptimalizálás [ÚJ]
```

## Datahaven React Dashboard (2026-06-20)

**Phase 6 + 6.5** — Teljes React frontend + Backend API

**🌐 Publikus URL:** https://datahaven.joinerytech.hu

### Frontend komponensek (React 19 + TypeScript 6 + Vite 8)

**Lokáció:** `/opt/spaceos/datahaven-web/client/`

**Implementált pages:**
- `PlanningPage.tsx` (240 lines) — Planning pipeline 5-stage workflow visualization
- `ProjectsPage.tsx` (329 lines) — Gantt chart timeline + list view for projects
- `DashboardPage.tsx` — Real-time metrics dashboard (17 terminals)
- `KanbanPage.tsx` — Dual-track kanban (Discovery + Delivery)

**Funkciók:**
- 8 hónapos Gantt timeline (-2 hó → +6 hó)
- "Today" marker a timeline-on
- Filtering by status + priority
- Auto-refresh minden 60 másodpercben
- Color-coded status badges
- Progress bars és metrics cards

**Build:**
```bash
cd /opt/spaceos/datahaven-web/client
npm run build
# Bundle: 278.25 kB (83.25 kB gzip)
```

### Backend API endpoints

**Lokáció:** `/opt/spaceos/spaceos-nexus/knowledge-service/src/server.ts`

**Új endpoints (Phase 6 + 6.5):**

#### GET /api/planning/items
- Scans `docs/planning/{ideas,queue}/` directories
- Returns planning items with status: idea → selected → debate → consensus → queue
- Metrics: count per stage, last scan timestamp
- ~70 lines, server.ts:672-738

#### GET /api/projects
- Scans `docs/tasks/{active,new,archive}/` directories
- Returns projects with dates, progress, terminal info
- Generates mock milestones
- ~60 lines, server.ts:738-796

#### GET /api/dashboard
- Scans all 17 terminals' mailbox directories
- Returns total inbox/outbox/unread counts
- Per-terminal status with session state (WORKING/IDLE)
- ~80 lines, server.ts:468-545

#### GET /api/kanban/snapshot
- Discovery track: scans planning/{ideas,queue}
- Delivery track: swimlanes per terminal from mailbox
- Real-time WIP counts across 5 stages
- ~80 lines, server.ts:549-631

#### GET /api/kanban/metrics
- Discovery/delivery WIP calculation
- Active session counts from terminal status
- Throughput and cycle time placeholders
- ~35 lines, server.ts:633-668

**Architektúra:**
- File system = real-time database
- Markdown frontmatter parsing (YAML)
- Recursive directory scanning
- Status detection (UNREAD, WORKING, IDLE)

### Tech stack
- **Frontend:** React 19, TypeScript 6, Vite 8, Tailwind CSS 4
- **Backend:** Express.js, Node.js 22, TypeScript
- **Data source:** File system (docs/planning/, docs/tasks/, docs/mailbox/)
- **Build tool:** Vite (HMR + fast builds)

## Következő lépések

### Phase 7: Marvin integráció
1. Planning pipeline Marvin-nel
2. Guardrail service bekötés
3. Advanced metrics (throughput, cycle time)

### Egyéb
- Librarian cron: indexer hívás knowledge sync után
- Haiku scanner: `search_knowledge` tool bekötés
- React komponens tesztek (Vitest)

## Megoldott problémák

### HTTPS + Systemd (2026-06-20)
- DNS rekord: `nexus.joinerytech.hu` → VPS IP
- SSL cert kibővítve certbot-tal (9 domain)
- Systemd `ProtectHome` konfliktus node_modules-sal → relaxált hardening

### MCP TypeScript típusok
- `sendMessage` params type assertion kellett
- `as 'task' | 'question' | 'done' | 'blocked'` minta

### MCP Protocol
- HTTP preferált SSE felett remote serverekhez
- JSON-RPC 2.0 format kötelező
- Bearer token kötelező production-ben

## Session tapasztalatok

### 2026-06-20 Wake-on-inbox + Auto Session Start
- `inboxWatcher.ts` — Chokidar v5 recursive watch + polling mode
- `terminalStatus.ts` — WORKING/IDLE tracking 10 perc timeout-tal
- `sessionStarter.ts` — Tmux session indítás közvetlenül InboxWatcher-ből
- SSE bridge — csak IDLE terminálokat ébreszti + session-t indít
- `watch-idle.sh` — 15+ perc idle + 0 UNREAD → graceful shutdown
- `session-status.sh` — real-time dashboard (human + JSON)
- `watch-stuck.sh` javítás — spam fix (input field már nem stuck)
- **PrivateTmp=false** — systemd nem izolálhatja a /tmp könyvtárat, mert a tmux socket ott van
- Eredmény: ~600MB RAM megtakarítás, költséghatékony működés, automatikus session indítás

### 2026-06-20 Terminal Docs System
- Minden terminálnak saját `docs/terminals/<terminal>/README.md`
- Quick reference: session indítás, parancsok, workflow
- MCP v1.3.0: 3 új tool (list_terminal_docs, get_terminal_docs, get_terminals_index)
- 20 tool összesen 6 kategóriában

### 2026-06-20 HTTPS Deploy
- Rackforest DNS gyorsan propagál (~1 perc)
- certbot `--expand` flag új subdomain-hez
- nginx `proxy_set_header Authorization` kell auth forwarding-hoz
- systemd hardening túl strict node.js-hez

### 2026-06-20 Phase 4
- MCP JSON-RPC egyszerű: initialize → tools/list → tools/call
- HTTP transport preferált távoli szerverhez (nem stdio)
- Bearer token auth opcionális lokálban, kötelező publikusan

### 2026-06-20 Security Audit (4 Agent)
Multi-agent audit végrehajtva: DevOps Expert, Security Expert, Architect, Devil's Advocate.

**Javított P0 hibák:**
- `.env` permission 755→600 ✅
- `sessionStarter.ts` path traversal védelem (ALLOWED_TERMINALS whitelist) ✅

**Befejezett P1/P2 feladatok (2026-06-20):**
- P1: Rate limiting middleware ✅ (100 req/min/IP, health/ready skip)
- P1: `/ready` endpoint ✅ (Kubernetes probe kompatibilis, 503 ha nem ready)
- P2: Graceful shutdown ✅ (SIGTERM/SIGINT handler, 10s timeout)

**Minden audit feladat befejezve (2026-06-20):**
- P1: Input validation Zod-dal ✅ (search, mailbox, terminal params)
- P2: Log rotation config ✅ (`scripts/logrotate.conf`)
- P3: ChromaDB backup script ✅ (`scripts/chromadb-backup.sh`)

**Telepítési lépések (manuális):**
```bash
# Log rotation
sudo cp scripts/logrotate.conf /etc/logrotate.d/spaceos-knowledge

# ChromaDB backup cron (naponta 3:00-kor)
(crontab -l; echo "0 3 * * * /opt/spaceos/spaceos-nexus/knowledge-service/scripts/chromadb-backup.sh") | crontab -
```

**Bash → TypeScript Pipeline Migráció (2026-06-20):**
Új `src/pipeline/` directory 6 TypeScript modullal:
- `common.ts` — shared utilities: SESSIONS, tmux wrappers, telegram, state file ops
- `watchIdle.ts` — idle session shutdown (15+ perc, 0 UNREAD)
- `watchStuck.ts` — stuck session detection + nudge
- `watchPriority.ts` — priority session management (root, conductor)
- `watchDone.ts` — DONE outbox → reviewer trigger
- `nightwatch.ts` — main dispatcher (runs all watch modules)
- `index.ts` — module exports

**Architekturális döntés:**
- Chokidar legyen MASTER (event-driven), nightwatch.sh legyen FALLBACK
- Fokozatos migráció TypeScript-re (Opció A) — ELKEZDVE ✅

**Audit dokumentum:** `spaceos-nexus/SECURITY_AUDIT_2026-06-20.md`

### 2026-06-20 Best Practice — Sub-Agent Orchestráció
**Felhasználói megfigyelés:** A terminálok ritkán használnak sub-agenteket feladataik orchestrálására.

**Javaslat:**
- Komplex feladatoknál indítsanak Explore agenteket
- Code review-nál Security + Architect agent párhuzamosan
- Planning-nél Devil's Advocate challenge
- Több szempont = jobb döntés

**Felhasználói megjegyzés:** "Fontos, hogy a rendszer ne csak a legnagyobb modellekkel legyen hatékony."
- Haiku-kompatibilis workflow-k prioritása
- Kis feladatok, keresések, összefoglalók → haiku
- Kód, elemzés → sonnet
- Architektúra, komplex tervezés → opus

### 2026-06-20 Nightwatch Scheduler Integráció
A TypeScript pipeline modulok (src/pipeline/) integrálva a server.ts-be:

**Aktiválás:**
```bash
# .env-ben vagy systemd unit file-ban
ENABLE_NIGHTWATCH=true
NIGHTWATCH_INTERVAL=120000  # ms (default: 2 perc)
```

**Működés:**
- server.ts induláskor ellenőrzi `ENABLE_NIGHTWATCH` env-t
- Ha true → `startNightwatchScheduler()` indul
- Graceful shutdown-nál `stopNightwatchScheduler()` hívódik
- Amíg nem aktív, a bash cron pipeline marad FALLBACK

**Kód:**
```typescript
// server.ts
import { startNightwatchScheduler, stopNightwatchScheduler } from './pipeline';

// Startup
if (process.env.ENABLE_NIGHTWATCH === 'true') {
  startNightwatchScheduler(intervalMs);
}

// Graceful shutdown
stopNightwatchScheduler();
```

### 2026-06-20 Phase 3
- SSE egyszerűbb mint WebSocket (nincs handshake)
- EventSource API böngészőben natív
