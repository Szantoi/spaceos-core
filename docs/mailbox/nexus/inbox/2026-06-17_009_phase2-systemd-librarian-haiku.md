---
id: MSG-NEXUS-009
from: root
to: nexus
type: task
priority: high
status: UNREAD
model: sonnet
created: 2026-06-17
---

# Nexus Phase 2 — Systemd Hardening + Librarian + Haiku Integration

## Context

**Nexus Phase 1 is LIVE:** Knowledge Service operational on port 3456, 25 documents indexed, Voyage AI embedding active.

**Phase 2 Mission:** Production hardening + automation integration.

---

## Scope (1-2 days)

### 1. Systemd Service Hardening (4-6 hours)

**Goal:** Convert nohup background process → systemd service with auto-restart.

**Requirements:**
- Create `/etc/systemd/system/spaceos-knowledge.service`
- Service user: `spaceos` (existing)
- Working directory: `/opt/spaceos/spaceos-nexus/knowledge-service`
- Start command: `npm run dev` (or equivalent production build)
- Auto-restart on failure: `Restart=always`
- Restart delay: `RestartSec=10s`
- Log forwarding: `Type=simple` → journalctl integration
- Environment variables: Load from `.env` file

**Validation:**
```bash
systemctl start spaceos-knowledge
systemctl status spaceos-knowledge
journalctl -u spaceos-knowledge -f  # Verify logs flowing
curl http://localhost:3456/health   # Verify service running
```

**DoD:**
- [x] Systemd unit file created + enabled
- [x] Service auto-starts on VPS boot
- [x] Health endpoint responds within 30s of boot
- [x] Logs appear in journalctl
- [x] Restart on failure works (kill process, verify auto-restart)

---

### 2. Librarian 5-Hourly Auto-Indexing Integration (3-4 hours)

**Goal:** Hook Knowledge Service indexing into Librarian's 5-hourly memory sync.

**Current State:**
- Librarian runs every 5 hours: `scripts/cron-librarian.sh`
- Cleans up stale memory files from `~/.claude/projects/*/memory/`
- Produces DONE message → pipeline updates docs/knowledge/

**Phase 2 Addition:**
After Librarian completes memory sync, trigger Knowledge Service reindex:
```bash
# In cron-librarian.sh, after memory cleanup:
echo "Triggering Knowledge Service reindex..."
curl -X POST http://localhost:3456/api/knowledge/index \
  -H "Content-Type: application/json" \
  -d '{"source":"docs/knowledge"}'
```

**Requirements:**
- Librarian checks if `/opt/spaceos/docs/knowledge/` has changed (mtime)
- If changed: POST to Knowledge Service reindex endpoint
- Endpoint: `POST /api/knowledge/index` with optional source param
- Retry logic: 3 attempts, 10s delay
- Log results to Librarian DONE message

**Validation:**
```bash
# Manually trigger
curl -X POST http://localhost:3456/api/knowledge/index \
  -H "Content-Type: application/json" \
  -d '{"source":"docs/knowledge"}'
# Expected: 200 OK, message: "Indexing completed"

# Verify new docs were indexed
curl "http://localhost:3456/api/knowledge/search?q=test"
```

**DoD:**
- [x] Reindex endpoint implemented (`POST /api/knowledge/index`)
- [x] Librarian detects `docs/knowledge/` changes
- [x] Automatic reindex triggered after Librarian completes
- [x] Retry logic handles transient failures
- [x] Librarian DONE message includes reindex status

---

### 3. Haiku Scanner Tool Integration (2-3 hours)

**Goal:** Give Haiku scanner access to Knowledge Service via new tool.

**Tool Definition:**
```yaml
name: search_knowledge
description: "Search SpaceOS knowledge base for relevant documentation"
input_schema:
  type: object
  properties:
    query:
      type: string
      description: "Search query (e.g., 'RLS pattern', 'deployment gotcha')"
    topK:
      type: integer
      default: 3
      description: "Number of results (1-10)"
  required: [query]
execute: |
  curl -s "http://localhost:3456/api/knowledge/search?q={query}&topK={topK}" | jq .results
```

**Integration Point:**
- Add to Haiku scanner's tool list (wherever scanner tools are defined)
- Available during terminal cold-start context building
- Used for: "Find relevant patterns" during implementation

**Example Usage:**
```
User: "Show me RLS patterns for PostgreSQL"
Haiku Scanner: search_knowledge(query="RLS PostgreSQL pattern", topK=3)
Knowledge Service: [
  { text: "ADR-004: RLS via tenant filtering...", score: 0.87 },
  { text: "DATABASE_PATTERNS.md: RLS SQL example...", score: 0.82 },
  ...
]
```

**Validation:**
- [x] Tool callable during scanner execution
- [x] Returns properly formatted results
- [x] Latency <1s (Knowledge Service responds <500ms)
- [x] Handles empty results gracefully

**DoD:**
- [x] Tool defined + registered in scanner
- [x] End-to-end test: query → response
- [x] Documented in scanner tools list
- [x] Error handling for connection failures

---

## Definition of Done

- [x] Systemd service running on VPS, auto-restart verified
- [x] Librarian reindex integration active, tested
- [x] Haiku scanner tool callable + returning results
- [x] All 3 components tested end-to-end
- [x] Documentation updated (systemd unit, reindex flow, tool docs)

---

## Timeline

| Task | Duration | Status |
|------|----------|--------|
| Systemd service | 4-6h | → Start now |
| Librarian integration | 3-4h | → Parallel |
| Haiku tool | 2-3h | → Parallel |
| Testing + docs | 1-2h | → Final |
| **TOTAL** | **10-15h** | **1-2 days** |

---

## Success Metrics

1. Systemd service: `systemctl status spaceos-knowledge` → Active ✅
2. Librarian cron: Next 5-hour run reindexes docs/knowledge/ ✅
3. Haiku tool: `search_knowledge(query="test")` → Results in <1s ✅

---

## Reference

- Current Knowledge Service: Port 3456, Express.js, running with `npm run dev`
- Voyage AI API key: Configured in `.env`
- Documents indexed: 25 (will expand with Librarian auto-indexing)

---

**ROOT Approval:** ✅ Phase 2 infrastructure track
**ETA:** 2026-06-18 or 2026-06-19
**Next:** Send DONE when complete

🚀 **Nexus Phase 2: Knowledge Service Hardening & Integration**
