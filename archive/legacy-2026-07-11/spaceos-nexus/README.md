# SpaceOS Nexus — Agent Infrastructure

> **Terminál:** NEXUS (`/opt/spaceos/spaceos-nexus/`)
> **Szerepkör:** Agent koordinációs infrastruktúra fejlesztés
> **Model:** claude --model sonnet

## Áttekintés

A Nexus terminál felelőssége: három agent módszer integrációja egy koherens rendszerré.

```
Módszer 1 — JoineryTech.McpServer  (TypeScript, MCP protokoll, RBAC, RAG, FSM)
Módszer 2 — Marvin                 (Python, Tasks, Threads, explicit orchestráció)
Módszer 3 — SpaceOS bash pipeline  (meglévő: nightwatch, reviewer, planning)
                    ↓
               SpaceOS Nexus
         (önjavító, tudásalapú, szerep-vezérelt
          agent koordinációs réteg)
```

**Termék terminálok (Kernel, FE, Joinery stb.) ÉRINTETLENEK maradnak.**
A Nexus háttérben épít — ha egy Nexus réteg kész, az összes terminál automatikusan profitál belőle.

---

## Projekt Struktúra

```
/opt/spaceos/spaceos-nexus/
├── CLAUDE.md              # Nexus terminál szabályok
├── MEMORY.md              # Session kontextus és tapasztalatok
├── README.md              # Ez a fájl
├── knowledge-service/     # Knowledge Service (Phase 1-2 ✅)
│   ├── src/
│   │   ├── embeddings.ts  # Voyage AI / Gemini / Local fallback
│   │   ├── indexer.ts     # Rekurzív .md scanner + chunking
│   │   ├── vectorStore.ts # ChromaDB client + in-memory fallback
│   │   └── server.ts      # Express REST API (port 3456)
│   ├── dist/              # TypeScript build output
│   ├── logs/              # Service logs
│   ├── package.json       # Dependencies + scripts
│   └── README.md          # Knowledge Service docs
├── mcp-server/            # JoineryTech.McpServer adaptáció
│   ├── src/mcp/tools/
│   │   └── discovery.ts   # discoverySearch tool (Haiku scanner)
│   └── ...
├── marvin/                # Marvin Python orchestrátor (Phase 2+ ⏳)
├── scripts/               # Migrációs + utility szkriptek
└── docker-compose.yml     # ChromaDB service (port 8001)
```

---

## Implementált Fázisok

### ✅ Fázis 1: Knowledge Service + Production Hardening (COMPLETE)

**Befejezve:** 2026-06-17
**Root Approval:** MSG-ROOT-034 (Phase 2 ACCEPTED)

**Komponensek:**
- ✅ **ChromaDB**: Docker container (port 8001, persistent volume)
- ✅ **Knowledge Service**: Express API (port 3456)
- ✅ **Embedding**: Voyage AI (voyage-3-lite, 512-dim) + Gemini + Local fallback
- ✅ **Indexer**: 441 documents from `/opt/spaceos/docs/knowledge/**/*.md`
- ✅ **Systemd**: `spaceos-knowledge.service` (enabled, auto-restart)
- ✅ **Librarian Integration**: Auto-reindex after 5-hourly sync
- ✅ **Haiku Tool**: `discoverySearch` (semantic search, <500ms)

**Endpoints:**
```bash
GET  /health                      # Service status
GET  /api/knowledge/search?q=...  # Query params
POST /api/knowledge/search        # JSON body: { q, topK }
POST /api/knowledge/index         # Manual reindex trigger
```

**Systemd Service:**
```bash
systemctl status spaceos-knowledge
systemctl restart spaceos-knowledge
journalctl -u spaceos-knowledge -f
```

**Health Check:**
```bash
curl http://localhost:3456/health
# {"status":"ok","documents":441,"embeddingBackend":"voyage-ai"}
```

---

### ⏳ Fázis 2: Marvin Planning Pipeline (WAITING)

**Scope:**
- [ ] Marvin telepítés (`pip install marvin`)
- [ ] Agent definíciók: scanner, selector, debater_a, debater_b, synthesizer
- [ ] plan-scan.sh → Marvin Task
- [ ] plan-select.sh → Marvin Task (WebSearch tool)
- [ ] plan-debate.sh → Marvin Tasks (párhuzamos)
- [ ] McpServer tool bekötve Marvin-ba
- [ ] Bash cron kikapcs → Marvin Scheduler

**Status:** Awaiting Root inbox message to begin

---

### ⏳ Fázis 3: Marvin Reviewer + Nightwatch (FUTURE)

**Scope:**
- [ ] reviewer.sh → Marvin Task
- [ ] nightwatch.sh → Marvin Scheduler
- [ ] WorkflowStateTracker bekötés
- [ ] RbacFilter bekötés

**Status:** Waiting for Fázis 2 completion

---

## Knowledge Service Architecture

```
┌─────────────────────────────────────┐
│    Haiku Scanner (Fázis 1)          │
│    ↓ discoverySearch tool           │
├─────────────────────────────────────┤
│   Express Server (port 3456)        │
│   ├─ GET  /health                  │
│   ├─ POST /api/knowledge/search     │ ← Haiku queries
│   └─ POST /api/knowledge/index      │ ← Librarian triggers
├─────────────────────────────────────┤
│   Voyage AI Embeddings              │
│   (voyage-3-lite, 512-dim)          │
├─────────────────────────────────────┤
│   ChromaDB Vector Store             │
│   (port 8001, persistent)           │
├─────────────────────────────────────┤
│   Knowledge Base                    │
│   (/opt/spaceos/docs/knowledge/)    │
│   441 indexed documents             │
└─────────────────────────────────────┘
```

**Integration Flow:**
1. Librarian 5-hourly sync → `pipeline.sh` → `pipeline-knowledge-index.sh` → reindex
2. Haiku scanner → `discoverySearch` tool → POST localhost:3456/api/knowledge/search
3. ChromaDB → persistent vector embeddings

---

## Development

### Knowledge Service

```bash
cd /opt/spaceos/spaceos-nexus/knowledge-service

# Install dependencies
npm install

# Build TypeScript
npm run build

# Development mode (auto-reload)
npm run dev

# Production mode (systemd uses this)
node dist/server.js

# Manual indexing
npm run index

# Test RAG
./scripts/test-rag.sh
```

### ChromaDB

```bash
cd /opt/spaceos/spaceos-nexus

# Start ChromaDB
docker compose up -d

# Stop ChromaDB
docker compose down

# Logs
docker logs spaceos_chromadb -f

# Health check
curl http://localhost:8001/api/v1/heartbeat
```

---

## Configuration

### Environment Variables

**Location:** `/etc/spaceos/knowledge.env`

```bash
VOYAGE_API_KEY=pa-...           # Voyage AI API key (required)
GOOGLE_API_KEY=AIza...          # Google Gemini fallback (optional)
CHROMADB_URL=http://localhost:8001
KNOWLEDGE_BASE_PATH=/opt/spaceos/docs/knowledge
PORT=3456
NODE_ENV=production
```

### Pipeline Config

**Location:** `/opt/spaceos/scripts/pipeline-config.yaml`

```yaml
librarian:
  knowledge_service:
    enabled: true
    url: http://localhost:3456/api/knowledge/index
    source: docs/knowledge
```

---

## Troubleshooting

### Service Won't Start

```bash
# Check systemd status
systemctl status spaceos-knowledge

# Check logs
journalctl -u spaceos-knowledge -n 50

# Check permissions
ls -l /opt/spaceos/spaceos-nexus/knowledge-service/dist/

# Rebuild
cd /opt/spaceos/spaceos-nexus/knowledge-service
npm run build
```

### ChromaDB Connection Failed

```bash
# Check ChromaDB is running
docker ps | grep chroma

# Restart ChromaDB
docker compose -f /opt/spaceos/spaceos-nexus/docker-compose.yml restart

# Check port 8001
ss -tlnp | grep 8001
```

### Voyage API Rate Limit

**Free tier:** 3 RPM (requests per minute) without payment method

**Solution:**
1. Add payment method: https://dashboard.voyageai.com/
2. Or wait for rate limit reset (1 minute)
3. Fallback to Gemini (slower but works)

---

## Testing

### Health Check
```bash
curl http://localhost:3456/health
```

### Semantic Search
```bash
curl -X POST http://localhost:3456/api/knowledge/search \
  -H "Content-Type: application/json" \
  -d '{"q":"RLS PostgreSQL pattern","topK":3}'
```

### Manual Reindex
```bash
curl -X POST http://localhost:3456/api/knowledge/index \
  -H "Content-Type: application/json" \
  -d '{"source":"docs/knowledge"}'
```

### RAG Test Suite
```bash
cd /opt/spaceos/spaceos-nexus/knowledge-service
./scripts/test-rag.sh
# Expected: 5/5 tests passing
```

---

## Production Status

**Current State:** ✅ **PRODUCTION-READY**

- Systemd: active/enabled (auto-restart)
- Documents: 441 indexed
- Uptime: 25h+ (stable)
- Latency: <500ms search
- Integration: Librarian + Haiku

**Deployment:** Part of Doorstar soft launch infrastructure

---

## Dokumentáció

- **CLAUDE.md**: Nexus terminál szabályok és session ritual
- **MEMORY.md**: Session kontextus, tapasztalatok, megoldott problémák
- **knowledge-service/README.md**: Knowledge Service részletes dokumentáció
- **docs/agent-infrastructure/ROADMAP.md**: Fejlesztési ütemterv (Fázis 1-3)

---

## Kapcsolat

**Terminál ID:** NEXUS
**Mailbox:** `/opt/spaceos/docs/mailbox/nexus/`
**Koordináció:** Root terminál
**Model:** claude --model sonnet

---

**Utolsó frissítés:** 2026-06-17
**Státusz:** Fázis 1 COMPLETE & ACCEPTED, Fázis 2 WAITING
