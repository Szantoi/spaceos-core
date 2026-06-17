---
id: MSG-ROOT-011
from: root
to: root
type: reminder
priority: medium
status: READ
created: 2026-06-17
---

# VPS Activation — Nexus McpServer Knowledge Service

## Összefoglaló

A Nexus terminál implementálta a McpServer Knowledge Service-t (Fázis 1).
**VPS manuális aktiválásra vár** — Root operátori feladat.

**Nexus DONE ref:** `MSG-NEXUS-001` (2026-06-16)

---

## Aktiválási lépések (VPS-en)

### 1. VOYAGE_API_KEY beállítása

```bash
# VPS SSH: gabor@109.122.222.198
cd /opt/spaceos/spaceos-nexus/knowledge-service
cp .env.example .env
nano .env
# VOYAGE_API_KEY=<key from dash.voyageai.com>
```

**API key forrás:** https://dash.voyageai.com/api-keys

---

### 2. ChromaDB + service indítás

```bash
# ChromaDB docker container
cd /opt/spaceos/spaceos-nexus
docker compose up -d

# Knowledge service (dev módban)
cd knowledge-service
npm install   # ha még nem futott
npm run dev

# Teszt (másik terminálban)
./scripts/test-rag.sh
```

**Expected:** 5/5 teszt zöld, dokumentumszám > 0.

---

## Technikai spec

| Komponens | Port | Persistent | Log |
|---|---|---|---|
| ChromaDB | 8001 | `spaceos_chromadb_data` | stdout |
| knowledge-service | 3456 | — | `npm run dev` console |

**Endpoints:**
- `GET  /health` — státusz
- `GET  /api/knowledge/search?q=...&topK=5`
- `POST /api/knowledge/search` `{ q: string, topK?: number }`
- `POST /api/knowledge/index` — újraindexelés

**Embedding:** Voyage AI (voyage-3-lite, 512 dim)
**Vector store:** ChromaDB primary + in-memory fallback

---

## Fázis 2 előkészítés (LATER)

- [ ] Librarian cron: indexer hívás `docs/knowledge/` sync után
- [ ] Haiku scanner: `search_knowledge` tool bekötve
- [ ] Production deployment (systemd service)

---

## Státusz

**ACTIVATION PENDING** — Root manuális VPS SSH szükséges.

**Prioritás:** MEDIUM — Datahaven/Resonance infrastruktúra projekt része, de nem blokkolja a TOP 1-2 implementációt.

---

**Root note:** A knowledge service a terminálok hideg indításához és kontextus építéséhez fontos, de a Consensus PHASE 1 (TOP 1-2) független tőle.
