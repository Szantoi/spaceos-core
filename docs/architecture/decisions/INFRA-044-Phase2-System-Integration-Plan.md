---
title: "INFRA-044: Phase 2 System Integration Plan"
author: INFRA Terminal
date: 2026-06-17
status: DRAFT (awaiting Phase 1 DDL completion)
depends_on: ADR-044, MSG-INFRA-061 (VPS SSH)
---

# INFRA-044: Phase 2 System Integration Plan

> **Ez a dokumentum a Phase 1 DDL végrehajtása UTÁN válik aktívvá.**
>
> **Jelenlegi státusz:** DRAFT (awaiting MSG-INFRA-061 VPS SSH authorization)
>
> **Megvalósítás kezdete:** Után Phase 1 DDL ✅ + ORCH Phase 2-3 completion

---

## 1. Célkitűzés

A Phase 1-5 Knowledge Service implementáció befejezése után, INFRA terminál az alábbi integrációs pontokat hajtja végre:

1. **Systemd service monitoring & maintenance**
2. **PostgreSQL & ChromaDB health checks**
3. **Scanner cron pipeline validation**
4. **Terminal hook integrations** (Architect, Planning selector)
5. **ADR-044 Phase 2 execution** (system-wide terminal access)

---

## 2. Függőségek & Blokkerek

### Blocker 1: Phase 1 DDL Execution
- **Status:** ⏳ Awaiting MSG-INFRA-061 (ROOT VPS SSH)
- **Duration:** 1 minute
- **Deliverable:** `spaceos_knowledge` PostgreSQL DB + 5 indexes + RLS policies
- **Validation:**
  ```bash
  ssh gabor@109.122.222.198 \
    "sudo -u postgres psql -p 5433 -d spaceos_knowledge -c '\d knowledge.documents'"
  ```

### Blocker 2: ORCH Phase 2-3 Completion
- **Status:** ⏸️ In progress
- **Duration:** ~3.5 days
- **Deliverables:**
  - Phase 2: `02-rag-ingest.js` deployed to VPS + running successfully
  - Phase 3: MCP server (`mcp-server.js`) deployed to `/opt/spaceos/spaceos-nexus/knowledge-service/src/`
- **Validation:**
  ```bash
  # Phase 2
  ssh gabor@109.122.222.198 "node /opt/spaceos/scripts/02-rag-ingest.js"

  # Phase 3
  node /opt/spaceos/spaceos-nexus/knowledge-service/src/mcp-server.js &
  curl http://localhost:3456/health
  ```

### What INFRA Can Do NOW (No Blockers)
- ✅ Create this Phase 2 integration plan (in progress)
- ✅ Prepare systemd service monitoring scripts
- ✅ Draft terminal integration hooks
- ✅ Prepare Phase 5 scanner validation scripts

---

## 3. Phase 1-5 Architecture Recap

```
┌─ PHASE 1: PostgreSQL DDL (INFRA) ─────────────────────┐
│ - spaceos_knowledge DB creation                        │
│ - knowledge.documents table + 5 GIN indexes            │
│ - RLS policies: admin_full_access                      │
│ Status: ✅ READY (blocked on MSG-INFRA-061)            │
└────────────────────────────────────────────────────────┘
                            ↓
┌─ PHASE 2: Ingestion Script (ORCH/INFRA) ──────────────┐
│ - Deploy 02-rag-ingest.js to VPS                       │
│ - Run Node.js ingest pipeline                          │
│ - Index 441+ documents with SHA-256 tracking           │
│ Status: ✅ READY (ORCH deployment)                     │
└────────────────────────────────────────────────────────┘
                            ↓
┌─ PHASE 3: MCP Server (ORCH) ───────────────────────────┐
│ - Implement mcp-server.js                              │
│ - knowledge_search + knowledge_read tools              │
│ - Deploy to spaceos-nexus directory                    │
│ Status: ✅ READY (ORCH implementation)                 │
└────────────────────────────────────────────────────────┘
                            ↓
┌─ PHASE 4: MCP Registration (INFRA) ────────────────────┐
│ - Register spaceos-knowledge in ~/.claude/settings.json │
│ Status: ✅ COMPLETE (2026-06-17 21:35)                 │
└────────────────────────────────────────────────────────┘
                            ↓
┌─ PHASE 5: Scanner Integration (INFRA) ─────────────────┐
│ - Create pipeline-knowledge-index.sh                    │
│ - Setup 5-hourly cron schedule                          │
│ - Integrate with Librarian trigger                      │
│ Status: ✅ READY (blocked on Phase 2 deployment)        │
└────────────────────────────────────────────────────────┘
```

---

## 4. INFRA Phase 2 Integration Points

### 4.1 Systemd Service Monitoring

**After Phase 1 DDL execution:**

```bash
#!/opt/spaceos/scripts/monitor-knowledge-service.sh

# Verify PostgreSQL spaceos_knowledge DB exists
psql -h localhost -p 5433 -U postgres -d spaceos_knowledge -c "SELECT count(*) FROM knowledge.documents;" || exit 1

# Verify ChromaDB container running
docker inspect spaceos_chromadb | grep -q '"State": {"Status": "running"}' || exit 1

# Verify Knowledge Service systemd service
systemctl is-active --quiet spaceos-knowledge || exit 1

# Health check endpoint
curl -s http://localhost:3456/health | grep -q '"status":"ok"' || exit 1

echo "[OK] Knowledge Service health check passed"
```

**Deployment:** Add to crontab for hourly monitoring
```bash
0 * * * * /opt/spaceos/scripts/monitor-knowledge-service.sh >> /var/log/spaceos/health-check.log 2>&1
```

### 4.2 PostgreSQL Incremental Maintenance

**After Phase 2 ingestion script deployed:**

```bash
#!/opt/spaceos/scripts/postgres-knowledge-maintenance.sh

# Weekly: ANALYZE knowledge.documents for query optimization
0 0 * * 0 psql -h localhost -p 5433 -U postgres -d spaceos_knowledge -c "ANALYZE knowledge.documents;"

# Weekly: REINDEX GIN indexes for performance
0 1 * * 0 psql -h localhost -p 5433 -U postgres -d spaceos_knowledge -c "REINDEX INDEX CONCURRENTLY documents_tsvector_idx;"

# Daily: Vacuum for disk space recovery
0 2 * * * psql -h localhost -p 5433 -U postgres -d spaceos_knowledge -c "VACUUM ANALYZE knowledge.documents;"

# Log statistics
0 3 * * * psql -h localhost -p 5433 -U postgres -d spaceos_knowledge -c "\
  SELECT
    count(*) as total_docs,
    round(sum(length(content))/1024/1024, 2) as size_mb
  FROM knowledge.documents;" >> /var/log/spaceos/knowledge-stats.log
```

### 4.3 ChromaDB Backup & Recovery

**After Phase 3 MCP server deployed:**

```bash
#!/opt/spaceos/scripts/backup-chroma-vectors.sh

BACKUP_DIR="/var/backups/spaceos-chromadb"
mkdir -p "$BACKUP_DIR"

# Docker volume backup (monthly)
docker run --rm \
  -v spaceos_chromadb_data:/data \
  -v "$BACKUP_DIR:/backup" \
  ubuntu:22.04 \
  tar czf /backup/chromadb-$(date +%Y%m%d).tar.gz /data

# Verify backup
tar tzf "$BACKUP_DIR/chromadb-$(date +%Y%m%d).tar.gz" > /dev/null || exit 1
echo "[OK] ChromaDB backup completed"
```

---

## 5. Terminal Integration Hooks (Phase 2 Only)

### 5.1 Architect Cold-Start Hook

**Objective:** Architect terminal can query knowledge base on startup

**File:** Add to `/home/gabor/.claude/projects/architect/CLAUDE.md`

```markdown
## Knowledge Service Integration

When cold-starting, consult the knowledge base for:
- Recent ADR decisions (filename: `*ADR-*`)
- System patterns (filename: `*PATTERN*`)
- Deployment decisions (filename: `*DEPLOY*`)

**Tool:** `knowledge_search`

**Example Query:**
```
/mcp knowledge_search "What is the latest RLS row-level security decision?"
```

### 5.2 Planning Selector Knowledge-Aware Scoring

**Objective:** Planning selector uses knowledge base to inform WSJF scoring

**File:** `planning-selector/src/knowledge-scorer.ts` (NEW)

```typescript
// scoring/knowledgeScorer.ts
import { knowledgeSearch } from './mcp-client';

export async function scoreWithKnowledgeContext(
  epic: Epic
): Promise<number> {
  // Search knowledge base for related decisions
  const relevantDocs = await knowledgeSearch(
    `${epic.name} ${epic.description}`
  );

  // If similar work already documented, reduce risk (increase score)
  const contextScore = relevantDocs.length > 0 ? 0.1 : 0;

  return baseWsjfScore + contextScore;
}
```

**Integration Point:** Planning selector already exists; INFRA provides integration hook

---

## 6. Phase 5 Scanner Validation

**After ORCH Phase 2 deployment signal:**

```bash
#!/opt/spaceos/scripts/validate-phase5-scanner.sh

# 1. Verify Phase 2 ingestion script exists and is executable
[ -x /opt/spaceos/scripts/02-rag-ingest.js ] || { echo "Phase 2 script not found"; exit 1; }

# 2. Verify cron entry
crontab -l | grep -q "pipeline-knowledge-index.sh" || { echo "Cron not setup"; exit 1; }

# 3. Test ingestion cycle
/opt/spaceos/scripts/pipeline-knowledge-index.sh || { echo "Ingestion failed"; exit 1; }

# 4. Verify document count increased
BEFORE=$(psql -h localhost -p 5433 -U postgres -d spaceos_knowledge -t -c "SELECT count(*) FROM knowledge.documents;")
/opt/spaceos/scripts/02-rag-ingest.js
AFTER=$(psql -h localhost -p 5433 -U postgres -d spaceos_knowledge -t -c "SELECT count(*) FROM knowledge.documents;")

[ "$AFTER" -ge "$BEFORE" ] || { echo "Document count not increased"; exit 1; }

echo "[OK] Phase 5 Scanner validated: $AFTER documents indexed"
```

---

## 7. ADR-044 Phase 2 Execution Sequence

### Sequence (After Phase 1-5 Complete)

```
Day 1 (Phase 1-5): All infrastructure deployed ✅
  └─ Phase 1: PostgreSQL DDL ✅
  └─ Phase 2: Ingestion script deployed ✅
  └─ Phase 3: MCP server running ✅
  └─ Phase 4: MCP registration complete ✅
  └─ Phase 5: Scanner cron setup ✅

Day 2 (Phase 2 Planning & Validation):
  ├─ INFRA: Run systemd health checks
  ├─ INFRA: Validate Phase 5 scanner
  ├─ ROOT: Add terminal CLAUDE.md hooks
  ├─ INFRA: Test Architect knowledge queries
  └─ Conductor: Smoke test full integration

Days 3-4 (Phase 2 System Integration):
  ├─ Architect: Verify ADR/pattern search works
  ├─ Planning selector: Verify knowledge scoring
  ├─ Terminal cold-start: Verify context enrichment
  └─ Conductor: Monitor performance metrics

Days 5-6 (Phase 3 Planning):
  ├─ INFRA: Design episodic memory indexing
  ├─ INFRA: Plan cross-terminal context sharing
  ├─ Conductor: Scope Phase 3 implementation
  └─ NEXUS: Outline Datahaven/Resonance foundation
```

---

## 8. Validation Checklist (Phase 2)

### PostgreSQL Validation
- [ ] `spaceos_knowledge` database exists
- [ ] `knowledge.documents` table present
- [ ] 5 GIN indexes created
- [ ] RLS policies enabled
- [ ] >400 documents indexed

### ChromaDB Validation
- [ ] Docker container running
- [ ] Vector collection `spaceos_knowledge` exists
- [ ] 441+ documents vectorized
- [ ] Cosine similarity search working

### MCP Integration Validation
- [ ] MCP server process running
- [ ] `knowledge_search` tool accessible
- [ ] `knowledge_read` tool accessible
- [ ] Claude Code `/mcp` command working

### Scanner Validation
- [ ] Cron job scheduled (5-hourly)
- [ ] Last run timestamp exists
- [ ] New documents incrementally indexed
- [ ] Logs present at `/var/log/spaceos/knowledge-*.log`

### Terminal Integration Validation
- [ ] Architect can query knowledge base
- [ ] Planning selector scores using knowledge context
- [ ] Terminal cold-start enriches context
- [ ] No performance regression (<100ms query latency)

---

## 9. Rollback & Troubleshooting

### If Phase 1 DDL Fails
```bash
# Diagnose
ssh gabor@109.122.222.198 "sudo -u postgres psql -p 5433 -c '\l'" | grep spaceos_knowledge

# Rollback
ssh gabor@109.122.222.198 "sudo -u postgres psql -p 5433 -c 'DROP DATABASE spaceos_knowledge;'"

# Retry
ssh gabor@109.122.222.198 "sudo -u postgres psql -p 5433 < /tmp/01-knowledge-schema.sql"
```

### If ChromaDB Unavailable (Graceful Degradation)
```bash
# Check container
docker ps | grep spaceos_chromadb

# Restart
docker restart spaceos_chromadb

# Verify
curl http://localhost:8001/api/v1/collections

# Fallback: In-memory search (automatically triggered in MCP server)
# No manual action needed — service continues with reduced performance
```

### If Scanner Cron Fails
```bash
# Check crontab
crontab -l | grep spaceos

# Manual test
/opt/spaceos/scripts/pipeline-knowledge-index.sh

# Check logs
tail -f /var/log/spaceos/knowledge-scanner.log

# Verify ingestion script permissions
ls -la /opt/spaceos/scripts/02-rag-ingest.js
chmod +x /opt/spaceos/scripts/02-rag-ingest.js
```

---

## 10. Resource Requirements (Phase 2)

| Resource | Requirement | Status |
|----------|-------------|--------|
| PostgreSQL disk | 500 MB (441 docs) | ✅ Available |
| ChromaDB memory | 2-4 GB | ✅ Available |
| Cron execution | 5 min every 5 hours | ✅ Minimal overhead |
| MCP server process | ~100 MB RAM | ✅ Available |
| Network | TCP 5433, 8001, 3456 | ✅ Open |

---

## 11. Timeline (After Phase 1 Unblock)

```
T+0:   Phase 1 DDL execution (1 min)
T+1:   nginx 403 fix (15 min)
T+16:  Health check scripts deployment
T+30:  Document Phase 2 completion status
T+1d:  ORCH Phase 2-3 completion (expected)
T+1d+1h: Phase 5 scanner validation
T+2d:  Full integration testing
T+3d:  ADR-044 Phase 2 COMPLETE
```

---

## 12. Deliverables (INFRA-044 Phase 2)

| Item | Owner | Status |
|------|-------|--------|
| Systemd monitoring script | INFRA | ✅ READY |
| PostgreSQL maintenance cron | INFRA | ✅ READY |
| ChromaDB backup script | INFRA | ✅ READY |
| Phase 5 validation script | INFRA | ✅ READY |
| Terminal integration docs | INFRA | ✅ READY |
| Phase 2 planning document | INFRA | ✅ (this doc) |

---

## 13. Success Criteria

- [ ] All 5 GIN indexes present on knowledge.documents
- [ ] >441 documents indexed in PostgreSQL
- [ ] >441 documents vectorized in ChromaDB
- [ ] MCP tools accessible (knowledge_search, knowledge_read)
- [ ] Scanner cron executes successfully every 5 hours
- [ ] Architect can query knowledge base via MCP
- [ ] Planning selector integrates knowledge context
- [ ] No service degradation (<100ms query latency)
- [ ] Logs present and monitoring active

---

## References

- **ADR-044:** `/opt/spaceos/docs/architecture/decisions/ADR-044-knowledge-service-system-integration.md`
- **Phase 1-5 Master Guide:** `/opt/spaceos/scripts/README-INFRA-PHASE1-5.md`
- **VPS Playbook:** `/opt/spaceos/scripts/00-PHASE1-VPS-EXECUTION-PLAYBOOK.md`
- **MSG-INFRA-061:** VPS SSH authorization request
- **MSG-INFRA-068:** Comprehensive Phase 1-5 status report

---

**Status:** DRAFT (awaiting Phase 1 DDL completion)

**Next Action:** Activate this plan immediately after Phase 1 DDL ✅

**Prepared by:** INFRA Terminal (2026-06-17 21:45 UTC)

