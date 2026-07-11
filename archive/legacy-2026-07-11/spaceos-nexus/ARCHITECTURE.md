# SpaceOS Nexus — Architektúra és Szolgáltatások

## Szolgáltatások Áttekintése

```
┌─────────────────────────────────────────────────────────────────────┐
│                        SPACEOS NEXUS                                │
│                   Agent Infrastruktúra Layer                        │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌─────────────────────┐    ┌─────────────────────┐                │
│  │  Knowledge Service  │    │   Datahaven Web     │                │
│  │    (Port 3456)      │    │    (Port 3457)      │                │
│  │  TypeScript/Express │    │  Express + Static   │                │
│  └─────────┬───────────┘    └─────────┬───────────┘                │
│            │                          │                             │
│            ▼                          ▼                             │
│  ┌─────────────────────┐    ┌─────────────────────┐                │
│  │     ChromaDB        │    │   SQLite (stats)    │                │
│  │    (Port 8001)      │    │                     │                │
│  │   Docker Container  │    │                     │                │
│  └─────────────────────┘    └─────────────────────┘                │
│                                                                     │
│  ┌─────────────────────┐    ┌─────────────────────┐                │
│  │  Datahaven Telegram │    │   Bash Pipeline     │                │
│  │    (Python Bot)     │    │   (Cron Scripts)    │                │
│  └─────────────────────┘    └─────────────────────┘                │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

## 1. Knowledge Service (spaceos-knowledge)

**Port:** 3456
**Technológia:** TypeScript, Express, ChromaDB, Voyage AI
**Systemd:** `spaceos-knowledge.service`
**Publikus URL:** `https://nexus.joinerytech.hu`

### Funkciók

| Modul | Fájl | Leírás |
|-------|------|--------|
| RAG Search | `vectorStore.ts` | ChromaDB semantic search |
| Embeddings | `embeddings.ts` | Voyage AI voyage-3-lite |
| Indexer | `indexer.ts` | Markdown → vector indexelés |
| Mailbox | `mailbox.ts` | Inbox/outbox file operations |
| MCP Protocol | `mcp.ts` | 23 JSON-RPC tool |
| Identity | `identity.ts` | Terminal CLAUDE.md + memory |
| Skills | `skills.ts` | Skill.md + workflow kezelés |
| Inbox Watcher | `inboxWatcher.ts` | Chokidar fájlfigyelő |
| Terminal Status | `terminalStatus.ts` | WORKING/IDLE tracking |
| Session Starter | `sessionStarter.ts` | Tmux session indítás |

### API Endpoints

```
GET  /health                           → Service health check
GET  /api/knowledge/search?q=...       → RAG keresés
POST /api/knowledge/search             → RAG keresés (body)
POST /api/knowledge/index              → Re-indexelés

GET  /api/mailbox/:terminal/inbox      → Inbox lista
POST /api/mailbox/:terminal/inbox      → Üzenet küldés
POST /api/mailbox/:terminal/outbox     → DONE beküldés
GET  /api/mailbox/:terminal/subscribe  → SSE subscription

POST /api/terminal/:terminal/status    → WORKING/IDLE regisztráció
GET  /api/terminal/:terminal/status    → Státusz lekérdezés

POST /mcp                              → MCP JSON-RPC (23 tool)
```

### Kommunikáció

```
Knowledge Service
    │
    ├──► ChromaDB (localhost:8001) — vector storage
    │
    ├──► Voyage AI API — embedding generálás
    │
    ├──► docs/mailbox/**/inbox/ — fájl figyelés (Chokidar)
    │
    ├──► /tmp/spaceos.tmux — tmux session indítás
    │
    └──► Telegram API — wake-on-inbox értesítések
```

---

## 2. ChromaDB

**Port:** 8001
**Technológia:** Python, Docker
**Container:** `chromadb/chroma:latest`

### Indítás
```bash
docker run -d --name chromadb -p 8001:8000 \
  -v /opt/spaceos/spaceos-nexus/chromadb-data:/chroma/chroma \
  chromadb/chroma:latest
```

### Adatok
- Collection: `spaceos-knowledge`
- Dokumentumok: ~1106 db (docs/knowledge/**)
- Embedding: voyage-3-lite (1024 dim)

---

## 3. Datahaven Web

**Port:** 3457
**Technológia:** Express, SQLite, Static HTML
**Nincs systemd** — manuális indítás

### Funkciók
- Dashboard (index.html)
- Kanban board (kanban.html)
- Planning view (planning.html)
- Projects view (projects.html)

### API
```
GET  /health         → Health check
GET  /api/stats      → SQLite stats
GET  /api/daemons    → Daemon állapotok
GET  /api/messages   → Üzenetek
GET  /api/knowledge  → Knowledge proxy
```

### Kommunikáció
```
Datahaven Web
    │
    ├──► Knowledge Service (localhost:3456) — proxy
    │
    └──► SQLite DB (data/datahaven.db) — stats, messages
```

---

## 4. Datahaven Telegram Bot

**Technológia:** Python, python-telegram-bot
**Nincs systemd** — manuális indítás

### Funkciók
- `/status` — rendszer állapot
- `/sessions` — tmux sessionök
- `/search <query>` — knowledge keresés
- `/send <terminal> <message>` — üzenet küldés

### Kommunikáció
```
Telegram Bot
    │
    ├──► Telegram API — bot messages
    │
    ├──► Knowledge Service (localhost:3456) — search, mailbox
    │
    └──► /tmp/spaceos.tmux — session lista
```

---

## 5. Bash Pipeline (Cron Scripts)

**Lokáció:** `/opt/spaceos/scripts/`
**Cron:** nightwatch.sh */2 perc

### Szkriptek

| Szkript | Funkció | Hívja |
|---------|---------|-------|
| `nightwatch.sh` | Fő dispatcher | cron */2 |
| `watch-priority.sh` | Root/Conductor fut | nightwatch |
| `watch-done.sh` | DONE feldolgozás | nightwatch |
| `watch-stuck.sh` | Stuck session fix | nightwatch |
| `watch-inbox.sh` | Inbox-alapú indítás | nightwatch |
| `watch-idle.sh` | Idle session stop | nightwatch |
| `reviewer.sh` | Haiku review | watch-done |
| `pipeline.sh` | README frissítés | reviewer |

### Kommunikáció
```
Bash Pipeline
    │
    ├──► /tmp/spaceos.tmux — session kezelés
    │
    ├──► docs/mailbox/**/inbox/ — fájl műveletek
    │
    ├──► docs/mailbox/**/outbox/ — DONE olvasás
    │
    └──► Telegram API — értesítések
```

---

## Teljes Kommunikációs Flow

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           KÜLSŐ RENDSZEREK                              │
├─────────────────────────────────────────────────────────────────────────┤
│  Telegram API          Voyage AI API          Claude Code (MCP)         │
│       │                      │                       │                  │
│       ▼                      ▼                       ▼                  │
│  ┌─────────┐          ┌─────────────┐         ┌─────────────┐          │
│  │Telegram │          │  Knowledge  │◄────────│   Claude    │          │
│  │   Bot   │─────────►│   Service   │         │   Session   │          │
│  └─────────┘          └──────┬──────┘         └──────┬──────┘          │
│                              │                       │                  │
│                              ▼                       │                  │
│                        ┌──────────┐                  │                  │
│                        │ ChromaDB │                  │                  │
│                        └──────────┘                  │                  │
│                              │                       │                  │
├──────────────────────────────┼───────────────────────┼──────────────────┤
│                           FÁJLRENDSZER                                  │
├──────────────────────────────┼───────────────────────┼──────────────────┤
│                              ▼                       ▼                  │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │                    docs/mailbox/*/inbox/                          │  │
│  │                    docs/mailbox/*/outbox/                         │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                              │                                          │
│                              ▼                                          │
│  ┌─────────────┐      ┌─────────────┐      ┌─────────────┐            │
│  │  Chokidar   │      │   Cron      │      │   Claude    │            │
│  │  Watcher    │      │  Pipeline   │      │  Sessions   │            │
│  └──────┬──────┘      └──────┬──────┘      └──────┬──────┘            │
│         │                    │                    │                    │
│         ▼                    ▼                    ▼                    │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │                    /tmp/spaceos.tmux                              │  │
│  │              (Tmux Socket — Session Management)                   │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Wake-on-Inbox Flow

```
1. Fájl létrehozás
   docs/mailbox/<terminal>/inbox/2026-XX-XX_NNN_task.md
                    │
                    ▼
2. InboxWatcher (Chokidar) detektálja
   [InboxWatcher] File detected: ...
                    │
                    ▼
3. YAML frontmatter parse
   status: UNREAD → folytatás
   status: READ   → skip
                    │
                    ▼
4. Terminal státusz ellenőrzés
   WORKING → skip (nem zavarjuk)
   IDLE    → folytatás
                    │
                    ▼
5. SSE broadcast + Session indítás
   [SSE] Wake-up sent to <terminal>
   [SessionStarter] Starting spaceos-<terminal>
                    │
                    ▼
6. Tmux session + Claude
   tmux -S /tmp/spaceos.tmux new-session -d -s spaceos-<terminal>
   claude --model <frontmatter model>
                    │
                    ▼
7. Telegram értesítés
   🚀 TERMINAL wake-on-inbox
```

---

## Session Lifecycle

```
┌─────────────────────────────────────────────────────────────────┐
│                      SESSION LIFECYCLE                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   ┌─────────┐     inbox UNREAD     ┌──────────┐                │
│   │  IDLE   │ ──────────────────► │ STARTING │                │
│   │(no tmux)│                      │(new sess)│                │
│   └────▲────┘                      └────┬─────┘                │
│        │                                │                       │
│        │                                ▼                       │
│        │      15 min idle         ┌──────────┐                 │
│        │ ◄─────── + ────────────  │ WORKING  │                 │
│        │      0 UNREAD            │(claude)  │                 │
│        │                          └────┬─────┘                 │
│        │                               │                        │
│        │                               ▼                        │
│        │      reviewer OK         ┌──────────┐                 │
│        └───────────────────────── │   DONE   │                 │
│                                   │(outbox)  │                 │
│                                   └──────────┘                 │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## Konfigurációs Fájlok

| Fájl | Leírás |
|------|--------|
| `knowledge-service/.env` | API kulcsok, MCP token |
| `/etc/systemd/system/spaceos-knowledge.service` | Systemd config |
| `/etc/nginx/sites-enabled/nexus-knowledge` | Nginx proxy |
| `scripts/plan-config.yaml` | Planning pipeline config |
| `scripts/reviewer-config.yaml` | Reviewer config |

---

## Portok Összefoglaló

| Port | Szolgáltatás | Publikus |
|------|--------------|----------|
| 3456 | Knowledge Service | https://nexus.joinerytech.hu |
| 3457 | Datahaven Web | Nem (localhost) |
| 8001 | ChromaDB | Nem (Docker) |

---

## Systemd Szolgáltatások

```bash
# Knowledge Service
sudo systemctl status spaceos-knowledge
sudo systemctl restart spaceos-knowledge
sudo journalctl -u spaceos-knowledge -f

# ChromaDB (Docker)
docker ps | grep chromadb
docker logs chromadb -f
```

---

## Hibakeresés

### Service nem indul
```bash
sudo journalctl -u spaceos-knowledge -n 50
tail -50 /var/log/spaceos/knowledge-service.log
```

### Session nem indul
```bash
# Ellenőrizd a PrivateTmp beállítást
grep PrivateTmp /etc/systemd/system/spaceos-knowledge.service
# Kell: PrivateTmp=false
```

### ChromaDB nem elérhető
```bash
docker ps | grep chromadb
curl http://localhost:8001/api/v1/heartbeat
```
