# INFRA Phase 4-5: MCP Registration & Scanner Integration

**Status:** PLANNING (depends on Phase 2-3 completion)
**Created:** 2026-06-17
**Scope:** INFRA terminal
**Est. Duration:** 1 day (after Phase 2-3 done)

---

## Overview

Phases 4-5 register the Knowledge Service MCP server in Claude settings and integrate the ingestion into the broader scanner pipeline.

### Dependencies
- **Phase 1:** ✅ DDL + schema (INFRA — completed)
- **Phase 2:** ⏳ Node.js ingest script (ORCH — 1.5 days)
- **Phase 3:** ⏳ MCP server TypeScript (ORCH — 2 days)
- **Phase 4:** 🕐 MCP registration (INFRA — 0.5 days)
- **Phase 5:** 🕐 Scanner integration (INFRA — 0.5 days)

---

## Phase 4: MCP Server Registration (0.5 days)

### 4.1 Register in Claude settings.json

**File:** `/home/gabor/.claude/settings.json`

**Current state:**
```json
{
  "permissions": { "defaultMode": "bypassPermissions" },
  "enabledPlugins": { "telegram@claude-plugins-official": true },
  ...
}
```

**Add mcpServers section:**
```json
{
  "permissions": { "defaultMode": "bypassPermissions" },
  "enabledPlugins": { "telegram@claude-plugins-official": true },
  "mcpServers": {
    "spaceos-knowledge": {
      "command": "node",
      "args": ["/opt/spaceos/spaceos-nexus/knowledge-service/src/mcp-server.js"],
      "env": {
        "DB_HOST": "localhost",
        "DB_PORT": "5433",
        "DB_NAME": "spaceos_knowledge",
        "DB_USER": "postgres"
      }
    }
  },
  ...
}
```

### 4.2 Verify MCP Registration

**Test 1: Check if MCP server starts**
```bash
# Start MCP server manually
node /opt/spaceos/spaceos-nexus/knowledge-service/src/mcp-server.js &

# Should log:
# MCP server starting...
# Listening on stdio...
# Connected to spaceos_knowledge DB
```

**Test 2: Check if Claude detects the server**
```bash
# In Claude Code terminal
claude list-mcp-servers
# Should show: spaceos-knowledge ✓
```

**Test 3: Test knowledge_search tool**
```bash
# In Claude Code terminal
claude call-mcp spaceos-knowledge knowledge_search --query="EF Core migration"
# Should return search results
```

### 4.3 Validate Tools

**Tool 1: knowledge_search**
- Input: `query` (required), `source_type`, `category`, `terminal`, `limit`
- Output: JSON array of documents with relevance_rank, snippet, word_count
- Fallback: If DB unavailable, returns grep-based results

**Tool 2: knowledge_read**
- Input: `file_path` (required)
- Output: Full document content + metadata
- Fallback: If DB unavailable, reads from filesystem

**Validation query:**
```bash
curl -X POST http://localhost:3456/api/knowledge/search \
  -H 'Content-Type: application/json' \
  -d '{"q": "VPS deploy", "topK": 3}'
# or via MCP tool call
```

---

## Phase 5: Scanner Integration (0.5 days)

### 5.1 Integrate with Existing Scanner

**Location:** `/opt/spaceos/scripts/pipeline-knowledge-index.sh`

**Current functionality:**
```bash
# Hypothetical existing script
#!/bin/bash
# Index knowledge base into ChromaDB
# ... ChromaDB indexing logic ...
```

**New integration:**
Replace or extend with:

```bash
#!/bin/bash
# Phase 5: Integrated scanner + PostgreSQL ingest

set -e

echo "[INFO] Starting knowledge service scanner..."
TIMESTAMP=$(date -u +%Y-%m-%dT%H:%M:%SZ)
LOG_FILE="/var/log/spaceos-knowledge-scan-${TIMESTAMP}.log"

# Step 1: Run INFRA Phase 2 ingestion (PostgreSQL FTS)
echo "[INFO] Running PostgreSQL ingestion..."
/opt/spaceos/scripts/02-rag-ingest.js >> "$LOG_FILE" 2>&1

# Step 2: Verify database state
echo "[INFO] Verifying database..."
TOTAL_DOCS=$(sudo -u postgres psql -p 5433 -d spaceos_knowledge -t -c "SELECT COUNT(*) FROM knowledge.documents;")
echo "[INFO] Database contains $TOTAL_DOCS documents"

# Step 3: Health check knowledge_search tool (optional)
echo "[INFO] Health check: MCP server status..."
ps aux | grep -q "mcp-server.js" && echo "[OK] MCP server running" || echo "[WARN] MCP server not running"

# Step 4: Log completion
echo "[OK] Knowledge service scan complete at $TIMESTAMP"
echo "[OK] $TOTAL_DOCS documents indexed"

# Step 5: Notification (optional, via existing notification system)
if [ -x /opt/spaceos/scripts/critical-notify.sh ]; then
  /opt/spaceos/scripts/critical-notify.sh "Knowledge Service Scanner" "Indexed $TOTAL_DOCS documents" "info"
fi

exit 0
```

**Register in crontab:**
```bash
# Scan every 6 hours (or sync with librarian)
0 */6 * * * /opt/spaceos/scripts/pipeline-knowledge-index.sh >> /var/log/spaceos-scanner.log 2>&1
```

### 5.2 Librarian Integration (Async)

If librarian (memory cleanup) runs on a schedule, ensure synchronization:

```bash
# In /opt/spaceos/scripts/cron-librarian.sh (after librarian cleanup)
# ...cleanup logic...

# Trigger knowledge ingestion after memory changes
echo "[INFO] Triggering knowledge ingestion after librarian cleanup..."
/opt/spaceos/scripts/02-rag-ingest.js

echo "[OK] Librarian + Knowledge Service in sync"
```

### 5.3 Notifications (Optional)

If critical-notify.sh exists, send status:

```bash
# High-level completion
curl -X POST http://notify.spaceos/api/notification \
  -H "Content-Type: application/json" \
  -d '{
    "service": "knowledge-service",
    "status": "scanner_complete",
    "documents_indexed": 150,
    "timestamp": "'$(date -u +%Y-%m-%dT%H:%M:%SZ)'"
  }'
```

---

## Validation Checklist (Phase 4-5)

### Phase 4: MCP Registration
- [ ] MCP server entry added to `~/.claude/settings.json`
- [ ] MCP server starts without errors (manual test)
- [ ] `claude list-mcp-servers` shows `spaceos-knowledge`
- [ ] `knowledge_search` tool callable and returns results
- [ ] `knowledge_read` tool callable and returns content
- [ ] Graceful fallback if database unavailable

### Phase 5: Scanner Integration
- [ ] `/opt/spaceos/scripts/pipeline-knowledge-index.sh` updated
- [ ] Cron job registered for scanner execution
- [ ] Manual test: scanner runs and indexes documents
- [ ] Log file created at `/var/log/spaceos-knowledge-scan-*.log`
- [ ] Librarian (if applicable) integrated with ingestion
- [ ] Notifications (if applicable) working

---

## Rollback (if needed)

### Remove MCP Server

**Option A: Disable in settings.json**
```json
{
  "mcpServers": {
    "spaceos-knowledge": {
      "disabled": true
    }
  }
}
```

**Option B: Remove entry entirely**
```bash
# Edit ~/.claude/settings.json and delete mcpServers block
```

### Disable Scanner Cron

```bash
# Comment out cron entry
sudo crontab -e
# # 0 */6 * * * /opt/spaceos/scripts/pipeline-knowledge-index.sh
```

### Keep Database (for Phase 2-3 debugging)

```bash
# Database persists; re-enable MCP registration when ready
sudo -u postgres psql -p 5433 -d spaceos_knowledge -c "SELECT COUNT(*) FROM knowledge.documents;"
```

---

## Estimated Timeline

| Phase | Task | Owner | Duration | Status |
|-------|------|-------|----------|--------|
| 1 | DDL + schema | INFRA | 0.5 days | ✅ DONE |
| 2 | Node.js ingest | ORCH | 1.5 days | ⏳ PENDING |
| 3 | MCP server (TS) | ORCH | 2 days | ⏳ PENDING |
| 4 | MCP registration | INFRA | 0.5 days | 🕐 READY |
| 5 | Scanner integration | INFRA | 0.5 days | 🕐 READY |
| **Total** | | | **~5 days** | |

---

## Document References

- **DEPLOYMENT_RUNBOOK.md** — Full procedural guide (all 5 phases)
- **KNOWLEDGE_BASE.md** — Architecture, data model, upgrade path
- **KNOWN_GOTCHAS.md** — Common issues, troubleshooting

---

## Notes for Root

- **Phase 1:** ✅ Executed (INFRA)
- **Phase 2-3:** Awaiting ORCH completion (1.5 + 2 days)
- **Phase 4-5:** Ready to execute as soon as Phase 3 delivers MCP server
- **Full pipeline:** Knowledge Service operational by 2026-06-22 (estimated)

---

**INFRA Terminal — Phase 4-5 ready for execution once Phase 2-3 complete.**
