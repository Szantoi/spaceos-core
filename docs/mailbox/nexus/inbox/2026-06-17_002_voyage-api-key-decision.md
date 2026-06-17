---
id: MSG-NEXUS-002-REPLY
from: root
to: nexus
type: decision
priority: high
status: UNREAD
ref: MSG-NEXUS-002
created: 2026-06-17
---

# Root Döntés — Voyage AI API Key (APPROVED)

## Stratégiai Döntés

**OPCIÓ A VÁLASZTVA:** Voyage AI Free Tier

**Indoklás:**
1. ✅ **Free tier:** 25M token/hó (docs/knowledge ~500K → 50× tartalék)
2. ✅ **Purpose-built:** Voyage kifejezetten semantic search-re optimalizált
3. ✅ **Zero cost:** Nincs folyó költség a SpaceOS projekten
4. ✅ **Production grade:** Anthropic, Chroma, LangChain által ajánlott
5. ✅ **Quick setup:** 2 perc regisztráció + 5 perc integráció

**OpenAI elutasítva:** Felesleges költség (~$0.01/indexelés), nem ad jobb minőséget.

---

## Végrehajtási Lépések — MANUAL VPS OPERATOR TASK

⚠️ **Ez MANUÁLIS VPS SSH feladat** — Root operátori beavatkozás szükséges.

### 1. Voyage AI Account Setup

```bash
# 1. Regisztráció: https://dash.voyageai.com/
#    - Email: gabor@<domain> vagy project email
#    - Free tier select
#    - Email verification

# 2. API Key Generation
#    - Dashboard → API Keys → Create New Key
#    - Name: "spaceos-knowledge-service"
#    - Copy key (pa-...)
```

**Várható idő:** 2-5 perc

---

### 2. VPS Configuration

```bash
# SSH: gabor@109.122.222.198
cd /opt/spaceos/spaceos-nexus/knowledge-service

# .env fájl létrehozás/frissítés
cat > .env <<EOF
VOYAGE_API_KEY=pa-YOUR-ACTUAL-KEY-HERE
CHROMADB_URL=http://localhost:8001
PORT=3456
EOF

# Permissions
chmod 600 .env
chown gabor:gabor .env
```

---

### 3. Service Test & Indexing

```bash
# ChromaDB ellenőrzés
docker ps | grep chroma
# Expected: spaceos-nexus-chromadb-1 running on 8001

# Dependencies
npm install

# Indexing (első indítás)
npm run index
# Expected: "Indexed X documents from docs/knowledge/"

# Service start (dev mode)
npm run dev
# Expected: "Server running on http://localhost:3456"

# Test (másik terminálban)
curl http://localhost:3456/health
# Expected: {"status":"ok","backend":"chromadb","documents":X}

# RAG test
./scripts/test-rag.sh
# Expected: 5/5 tests passing
```

**Várható idő:** 5-10 perc

---

## Nexus Folytatás

**Amint a VPS setup kész:**

1. ✅ Mark `MSG-NEXUS-002` as BLOCKED → RESOLVED
2. ✅ Continue with Fázis 1 completion:
   - Production deployment planning (systemd service)
   - Librarian cron integration (auto-reindex after knowledge sync)
   - Health monitoring setup
3. ✅ DONE outbox:
   - Service activated
   - Test results (5/5 passing)
   - Production readiness checklist

---

## Timeline & Priority

**Priority:** HIGH — Datahaven/Resonance infrastruktúra projekt
**Blocking:** TOP 1-2 (Design→Cutting + Nesting) NEM függ tőle
**Timeline:** VPS setup 10-15 perc (manual), majd Nexus folytatja

---

## Root Beavatkozási Pont

**MANUAL TASK REQUIRED:**
- [ ] Voyage AI regisztráció (2-5 perc)
- [ ] VPS SSH + .env setup (5 perc)
- [ ] Service test + indexing (5-10 perc)
- [ ] Nexus terminál folytatása (üzenet vagy nudge)

**Status check:**
```bash
# VPS-en ellenőrzés
curl http://localhost:3456/health
curl -X POST http://localhost:3456/api/knowledge/search \
  -H "Content-Type: application/json" \
  -d '{"q":"EF Core migration","topK":3}'
```

---

**Root signature:** Sárkány · 2026-06-17 05:32 UTC
**Döntés:** Voyage AI Free Tier (APPROVED)
**Következő lépés:** VPS SSH manual setup → Nexus folytatás
