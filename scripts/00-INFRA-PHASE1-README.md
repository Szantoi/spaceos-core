# INFRA Phase 1: Knowledge Service Database Setup

**Status:** READY FOR EXECUTION
**Created:** 2026-06-17
**Scope:** INFRA terminal (VPS operations)

---

## Overview

Phase 1 prepares the PostgreSQL `spaceos_knowledge` database for the Knowledge Service ingestion pipeline. This is the foundation for Phase 2-5 which span ORCH (Node.js scripts + MCP server) and INFRA (cron registration).

### Files in this phase:
1. **01-knowledge-schema.sql** — Database DDL (schema, tables, indexes)
2. **02-rag-ingest.js** — Node.js ingestion script (5-hourly cron)

---

## Prerequisites

- PostgreSQL 15+ running on port 5433 (native VPS installation)
- Node.js 20+ (for ingestion script)
- Permissions: `postgres` user for SQL, `root` for cron setup

---

## Execution Steps

### Step 1: Create Database & Schema (SQL)

```bash
# SSH to VPS (109.122.222.198)
# Run DDL script as postgres user
sudo -u postgres psql -p 5433 < /opt/spaceos/scripts/01-knowledge-schema.sql

# Expected output:
# CREATE DATABASE
# CREATE SCHEMA
# CREATE TABLE
# ALTER TABLE
# CREATE POLICY
# CREATE INDEX x5
# === Schema Created ===
# [table definition]
# [index list]
```

**Verification:**
```bash
sudo -u postgres psql -p 5433 -d spaceos_knowledge << 'EOF'
\d knowledge.documents
SELECT COUNT(*) FROM knowledge.documents;
EOF
```

Expected: Table exists, 0 rows initially.

---

### Step 2: Prepare Ingestion Script (Node.js)

**Prerequisite:** Node.js pg module installed

```bash
# Check if pg is available in knowledge-service
cd /opt/spaceos/spaceos-nexus/knowledge-service
npm list pg

# If not installed:
npm install pg
```

**Make ingestion script executable:**
```bash
chmod +x /opt/spaceos/scripts/02-rag-ingest.js
```

**Test ingestion (manual run):**
```bash
# First run to verify connectivity and ingestion
/opt/spaceos/scripts/02-rag-ingest.js

# Expected output:
# [INFO] Starting knowledge base ingestion at 2026-06-17T...
# [INFO] Scanning docs/knowledge/...
# [INFO] Scanning terminal memories...
# [INFO] Found XXX files to ingest
# ✓ docs/knowledge/...
# ✓ docs/knowledge/...
# ...
# 🗑️  Deleted 0 stale documents
# ✅ Ingestion complete: XXX/XXX files successfully indexed
```

---

### Step 3: Verify Database Population

```bash
sudo -u postgres psql -p 5433 -d spaceos_knowledge << 'EOF'
-- Total documents
SELECT COUNT(*) as total FROM knowledge.documents;

-- By source type
SELECT source_type, COUNT(*) FROM knowledge.documents GROUP BY source_type;

-- By category
SELECT category, COUNT(*) FROM knowledge.documents GROUP BY category ORDER BY COUNT DESC;

-- Recent ingestions (last 5)
SELECT file_path, indexed_at FROM knowledge.documents ORDER BY indexed_at DESC LIMIT 5;

-- Index status
\di knowledge.*
EOF
```

**Expected:**
- Total: ~150+ documents (knowledge + terminal memories)
- Source types: knowledge, memory
- Categories: deployment, patterns, context, discovery, etc.
- Indexes: 5 indexes on tsvector, source, category, terminal, updated

---

### Step 4: Register Cron Job (5-hourly)

**Create/update cron entry:**
```bash
# Add to root crontab (requires sudo)
sudo crontab -e

# Add line:
0 */5 * * * /opt/spaceos/scripts/02-rag-ingest.js >> /var/log/spaceos-rag-ingest.log 2>&1

# Verify:
sudo crontab -l | grep rag-ingest
```

**Log rotation (optional but recommended):**
```bash
# Create logrotate config
sudo tee /etc/logrotate.d/spaceos-rag-ingest > /dev/null << 'EOF'
/var/log/spaceos-rag-ingest.log {
    daily
    rotate 7
    compress
    missingok
    notifempty
    create 0640 root root
}
EOF
```

---

## Validation Checklist

- [ ] Database `spaceos_knowledge` created
- [ ] Schema `knowledge` created
- [ ] Table `knowledge.documents` with columns: file_path, source_type, category, terminal, title, content, content_tsvector, content_hash, word_count, indexed_at, updated_at
- [ ] 5 indexes created (tsvector GIN, source, category, terminal, updated)
- [ ] RLS policy `admin_full_access` enabled
- [ ] Ingestion script (02-rag-ingest.js) executable and tested
- [ ] At least 100+ documents indexed
- [ ] Cron job registered (*/5 hours)

---

## Rollback (if needed)

**If something goes wrong:**
```bash
# Backup current state
sudo -u postgres pg_dump -p 5433 -d spaceos_knowledge > /tmp/kb-backup-$(date +%s).sql

# Drop and recreate
sudo -u postgres psql -p 5433 << 'EOF'
DROP DATABASE spaceos_knowledge;
EOF

# Re-run Step 1
sudo -u postgres psql -p 5433 < /opt/spaceos/scripts/01-knowledge-schema.sql

# Re-run Step 2 manually
/opt/spaceos/scripts/02-rag-ingest.js
```

---

## Common Issues

### Issue: "database spaceos_knowledge does not exist"
- **Fix:** Run Step 1 (SQL DDL script)

### Issue: "permission denied" on cron job
- **Fix:** Ensure script is executable: `chmod +x /opt/spaceos/scripts/02-rag-ingest.js`
- **Fix:** Verify cron is running as root: `sudo crontab -l`

### Issue: "pg module not found" in ingestion script
- **Fix:** Install pg module: `cd /opt/spaceos/spaceos-nexus/knowledge-service && npm install pg`

### Issue: Slow ingestion (>5 minutes)
- **Expected:** First run indexes 150+ files, may take 2-3 minutes
- **Subsequent runs:** Incremental, should be <30 seconds

### Issue: MCP server crashes after Phase 1
- **Phase 1 scope:** Database only, not server
- **Phase 3 (ORCH):** MCP server implementation happens in Orchestrator
- **INFRA Phase 4:** MCP registration and cron setup

---

## Dependencies & Blockers

| Phase | Task | Owner | Status |
|-------|------|-------|--------|
| 1 | DDL + Schema | INFRA | ✅ READY |
| 1 | Ingestion script (manual) | INFRA | ✅ READY |
| 2 | ORCH Node.js ingest + MCP | ORCH | ⏳ PENDING |
| 3 | MCP server (TypeScript) | ORCH | ⏳ PENDING |
| 4 | MCP registration + cron | INFRA | ⏳ PENDING (after Phase 2-3) |
| 5 | Scanner integration | INFRA | ⏳ PENDING (after Phase 4) |

---

## Timeline Reference

- **2026-06-17:** Phase 1 preparation (READY)
- **2026-06-18:** Phase 1 execution (INFRA) + Phase 2 start (ORCH)
- **2026-06-18–2026-06-22:** Phases 2-5 implementation
- **2026-06-22:** Knowledge Service operational

---

## Document References

- **DEPLOYMENT_RUNBOOK.md** — Full step-by-step (all phases)
- **KNOWLEDGE_BASE.md** — Architecture + data model
- **KNOWN_GOTCHAS.md** — Common issues (#11-14 Knowledge Service specific)

---

**INFRA Terminal**
Ready for execution. Awaiting ROOT approval to proceed.
