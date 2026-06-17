---
id: MSG-NEXUS-004
from: root
to: nexus
type: task
priority: high
status: UNREAD
model: haiku
ref: MSG-NEXUS-003
created: 2026-06-17
---

# Nexus — Voyage AI Key Ready: Execute Phase 1 Indexing & Testing

## Situation

✅ VPS operator configured `VOYAGE_API_KEY` in `.env`
✅ Your implementation is 100% ready (embeddings.ts, vectorStore.ts, indexer.ts, server.ts)
✅ ChromaDB running on port 8001
✅ All dependencies installed

**Now:** Execute Phase 1 completion:

---

## Phase 1 Completion Tasks

### TASK 1: Verify Key Availability

```bash
cd /opt/spaceos/spaceos-nexus/knowledge-service

# Check .env has the key
grep "^VOYAGE_API_KEY" .env
# Should output: VOYAGE_API_KEY=pa-XXXXXXX...

# Check it's available in Node process
node -e "console.log('VOYAGE_API_KEY' in process.env ? '✓ Key available' : '✗ Key missing')"
```

---

### TASK 2: Knowledge Base Indexing

```bash
cd /opt/spaceos/spaceos-nexus/knowledge-service

# Run the indexer
npm run index

# Expected output:
# ✓ Reading /opt/spaceos/docs/knowledge/**/*.md
# ✓ Found: 21 documents, ~200 chunks
# ✓ Embedding via Voyage AI (voyage-3-lite, 512 dims)...
# ✓ Storing in ChromaDB (http://localhost:8001)
# ✓ Indexing complete: 200 chunks in DB
```

**Duration:** 2-3 minutes (depends on Voyage AI API latency)

---

### TASK 3: Start Knowledge Service

```bash
cd /opt/spaceos/spaceos-nexus/knowledge-service

# Start the server in dev mode
npm run dev

# Expected output:
# [knowledge-service] listening on port 3456
# ✓ ChromaDB connected
# ✓ Voyage AI initialized
# ✓ Ready to accept requests

# Leave this running (do NOT Ctrl+C yet)
```

**Server should listen on:** `http://localhost:3456`

**Endpoints ready:**
- `GET  /health` — server status
- `GET  /api/knowledge/search?q=...&topK=5` — search documents
- `POST /api/knowledge/index` — re-index knowledge base

---

### TASK 4: Validate with Tests (New Terminal)

Open **another terminal window** and run:

```bash
cd /opt/spaceos/spaceos-nexus/knowledge-service

# Run the validation test script
./scripts/test-rag.sh

# Expected output:
# Test 1: Search for "RLS" → ✓ 5 results found
# Test 2: Search for "ChromaDB" → ✓ 3 results found
# Test 3: Search for "Voyage" → ✓ 2 results found
# Test 4: Search for "migration" → ✓ 4 results found
# Test 5: Search for "deployment" → ✓ 6 results found
#
# Results: 5/5 passing ✅
```

If all 5 tests pass → **Phase 1 is complete** ✅

---

## Troubleshooting

**Issue:** `Error: VOYAGE_API_KEY not found`

**Fix:**
```bash
# Check if key is in .env
grep VOYAGE_API_KEY .env

# If commented or missing, ask ROOT to re-configure VPS
# (Key should start with: pa-)
```

**Issue:** `Error: Could not connect to ChromaDB`

**Fix:**
```bash
# Verify ChromaDB is running
curl http://localhost:8001/api/v1/heartbeat

# If not running, start it in parent directory:
cd /opt/spaceos/spaceos-nexus
docker compose up -d
```

**Issue:** `Error: Network timeout to Voyage AI`

**Possible causes:**
1. VPS internet connectivity issue
2. Voyage API down (rare)
3. Key is invalid or expired

**Fix:**
1. Check VPS internet: `curl https://api.voyageai.com/v1/health` (if available)
2. Regenerate key from https://dash.voyageai.com/
3. Update .env with new key: `echo "VOYAGE_API_KEY=pa-NEW-KEY" >> .env`

---

## Definition of Done

- [ ] VOYAGE_API_KEY available in process.env
- [ ] `npm run index` completes: 200+ chunks embedded
- [ ] `npm run dev` starts: server listening on port 3456
- [ ] `./scripts/test-rag.sh` passes: 5/5 tests green ✅
- [ ] Knowledge Service operational (ready for McpServer integration)

---

## After Phase 1 Complete

Send DONE outbox message to ROOT:

```markdown
Subject: MSG-NEXUS-004-DONE

Status: Phase 1 Complete ✅

- ✓ Indexed 21 documents, 200+ chunks
- ✓ Voyage AI embedding operational
- ✓ ChromaDB vector store populated
- ✓ Express server on port 3456
- ✓ 5/5 validation tests passing

Next: Phase 2 (McpServer activation + Librarian indexing hook)
```

---

## Timeline

- **Now:** You have ~5-10 minutes (from key available → tests passing)
- **After DONE:** ROOT processes message
- **Then:** Phase 2 activation message
- **Finally:** Datahaven/Resonance Knowledge Service operational ✅

---

**Status: READY FOR EXECUTION (when key available)**

🚀 Once this completes, Fázis 2 unlock: McpServer + Librarian sync hook
