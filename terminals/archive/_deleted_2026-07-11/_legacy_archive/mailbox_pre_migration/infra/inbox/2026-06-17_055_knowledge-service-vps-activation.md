---
id: MSG-INFRA-055
from: root
to: infra
type: task
priority: medium
status: READ
model: haiku
ref: MSG-ROOT-011
created: 2026-06-17
---

# VPS Operáció — Knowledge Service Aktiválás

## Felkérés

A **Nexus terminál** implementálta a **McpServer Knowledge Service-t (Fázis 1)** és **VPS manuális aktiválásra vár**.

**Feladat:** SSH operáció a VPS-en (gabor@109.122.222.198) a Knowledge Service go-live-hoz.

---

## Aktiválási lépések

### 1. VOYAGE_API_KEY beállítása

```bash
# VPS SSH
ssh gabor@109.122.222.198

# .env fájl előkészítése
cd /opt/spaceos/spaceos-nexus/knowledge-service
cp .env.example .env
nano .env

# VOYAGE_API_KEY hozzáadása
# Forrás: https://dash.voyageai.com/api-keys (free tier OK)
# Sor: VOYAGE_API_KEY=sk_... (teljes key)
```

### 2. ChromaDB + Knowledge Service indítása

```bash
# ChromaDB docker container (background)
cd /opt/spaceos/spaceos-nexus
docker compose up -d

# Knowledge service (npm dev mód)
cd knowledge-service
npm install   # ha szükséges
npm run dev

# Output: "Server running on http://localhost:3456"
```

### 3. Validálás (másik SSH terminal-ban)

```bash
# Test script futtatása
cd /opt/spaceos/spaceos-nexus
./scripts/test-rag.sh

# Expected output: 5/5 teszt zöld
# Példa: "✅ Search test: 5 results found in 250ms"
```

---

## Technikai spec

| Komponens | Port | Status |
|---|---|---|
| **ChromaDB** | 8001 | docker container `spaceos_chromadb` |
| **Knowledge Service** | 3456 | npm run dev (foreground) |

**Endpoints (teszteléshez):**
- `GET /health` → `{"status": "ok"}`
- `GET /api/knowledge/search?q=kernel&topK=5` → array of results
- `POST /api/knowledge/index` → reindex documents

---

## Prioritás & Depends

**Prioritás:** MEDIUM
**Depends on:** VOYAGE_API_KEY (free Voyage AI account szükséges)
**Blocks:** RAG_Knowledge_Base_v1 field processing (MSG-LIBRARIAN-002)

---

## Státusz after completion

**Expected result after go-live:**
- ✅ ChromaDB running on port 8001
- ✅ Knowledge Service running on port 3456
- ✅ 440+ documents indexed
- ✅ Semantic search operational (<500ms response)
- ✅ Ready for Librarian cron integration

---

## Notes

- **Dev vs Production:** Currently npm run dev (foreground). Production deployment (systemd service) in Fázis 2.
- **Embedding:** Voyage AI (voyage-3-lite, 512 dim) — no fallback needed if API key valid
- **Index:** Automatic via Librarian cron (5-hourly), can also be manual: `curl -X POST http://localhost:3456/api/knowledge/index`

---

**Root note:** PHASE 3 infrastructure (Marvin + RAG + MCP) depends on this Knowledge Service go-live. Architect waiting for semantic search confirmation before ADR-044 finalization.

Time estimate: 20-30 minutes (SSH + docker + test validation).
