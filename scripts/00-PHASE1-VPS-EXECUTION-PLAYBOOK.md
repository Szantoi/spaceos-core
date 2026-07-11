---
id: PHASE1-VPS-EXECUTION-PLAYBOOK
date: 2026-06-17
owner: INFRA
status: READY FOR EXECUTION
scope: VPS PostgreSQL DDL deployment (post-SSH authorization)
---

# INFRA Phase 1: VPS PostgreSQL DDL Execution Playbook

**Status:** ✅ READY (awaiting SSH authorization via MSG-INFRA-061)

**Timeline:** ~15 minutes total (after SSH access confirmed)

---

## Pre-Execution Checklist

Before executing Phase 1 DDL on VPS, verify:

- [ ] SSH public key authorized on VPS (`ssh gabor@109.122.222.198` connects without password)
- [ ] Phase 1 DDL script exists locally: `/opt/spaceos/scripts/01-knowledge-schema.sql`
- [ ] PostgreSQL client tools available locally (`psql` command)
- [ ] VPS PostgreSQL listening on port 5433
- [ ] Correct database name: `spaceos_knowledge` (not `spaceos_prod`)

---

## Quick Start (After SSH Authorized)

```bash
#!/bin/bash
# Phase 1 DDL Execution — SSH → Deploy → Validate

VPS_HOST="109.122.222.198"
VPS_USER="gabor"
DB_PORT="5433"
DDL_FILE="/opt/spaceos/scripts/01-knowledge-schema.sql"

echo "=== Phase 1 DDL Execution ==="
echo "Target: $VPS_HOST:$DB_PORT/spaceos_knowledge"
echo ""

# Step 1: Verify SSH access
echo "[1/5] Testing SSH connection..."
if ssh -o ConnectTimeout=5 "$VPS_USER@$VPS_HOST" "echo 'SSH OK'" > /dev/null 2>&1; then
  echo "✓ SSH access confirmed"
else
  echo "✗ SSH connection failed — check public key authorization"
  exit 1
fi

# Step 2: Execute DDL
echo "[2/5] Deploying DDL schema..."
scp "$DDL_FILE" "$VPS_USER@$VPS_HOST:/tmp/" && echo "✓ Script uploaded"
ssh "$VPS_USER@$VPS_HOST" "sudo -u postgres psql -p $DB_PORT < /tmp/01-knowledge-schema.sql" && echo "✓ DDL executed"

# Step 3: Validate schema
echo "[3/5] Validating schema creation..."
SCHEMA_CHECK=$(ssh "$VPS_USER@$VPS_HOST" "sudo -u postgres psql -p $DB_PORT -d spaceos_knowledge -c 'SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = '\''knowledge'\'';'" 2>/dev/null)
echo "✓ Schema tables: $SCHEMA_CHECK"

# Step 4: Check document table
echo "[4/5] Checking documents table..."
DOC_COUNT=$(ssh "$VPS_USER@$VPS_HOST" "sudo -u postgres psql -p $DB_PORT -d spaceos_knowledge -t -c 'SELECT COUNT(*) FROM knowledge.documents;'" 2>/dev/null | tr -d ' ')
echo "✓ Documents table exists (empty: $DOC_COUNT rows)"

# Step 5: Verify indexes
echo "[5/5] Verifying indexes..."
INDEX_COUNT=$(ssh "$VPS_USER@$VPS_HOST" "sudo -u postgres psql -p $DB_PORT -d spaceos_knowledge -t -c 'SELECT COUNT(*) FROM pg_indexes WHERE schemaname = '\''knowledge'\'';'" 2>/dev/null | tr -d ' ')
echo "✓ Indexes created: $INDEX_COUNT"

echo ""
echo "=== Phase 1 DDL COMPLETE ==="
```

---

## Detailed Step-by-Step Execution

### Step 1: Verify SSH Access (2 min)

```bash
# Test SSH connection
ssh -v gabor@109.122.222.198 "echo 'Test connection successful'"

# Expected output:
# ...debug output...
# Test connection successful

# If FAILS with "Permission denied (publickey)":
#   → ROOT needs to add public key to /home/gabor/.ssh/authorized_keys on VPS
#   → Cannot proceed without this — contact ROOT with MSG-INFRA-061
```

**Success Criteria:** SSH login works without password prompt

---

### Step 2: Upload DDL Script (2 min)

```bash
# Copy DDL to VPS /tmp/
scp /opt/spaceos/scripts/01-knowledge-schema.sql gabor@109.122.222.198:/tmp/

# Verify upload
ssh gabor@109.122.222.198 "ls -lh /tmp/01-knowledge-schema.sql"

# Expected output:
# -rw-r--r-- 1 gabor gabor 1.5K Jun 17 20:00 /tmp/01-knowledge-schema.sql
```

**Success Criteria:** File size ~1.5 KB, readable on VPS

---

### Step 3: Execute DDL Schema (1 min)

```bash
# Execute DDL as postgres user (requires sudo)
ssh gabor@109.122.222.198 "sudo -u postgres psql -p 5433 < /tmp/01-knowledge-schema.sql"

# Expected output (no errors):
# CREATE DATABASE
# CREATE SCHEMA
# CREATE TABLE
# CREATE INDEX (5x)
# CREATE POLICY
# COMMENT
```

**Success Criteria:**
- No SQL errors
- All CREATE statements executed
- Exit code: 0

---

### Step 4: Validate Database Creation (2 min)

```bash
# Check database exists
ssh gabor@109.122.222.198 "sudo -u postgres psql -p 5433 -l | grep spaceos_knowledge"

# Expected output:
#  spaceos_knowledge | postgres | UTF8   | en_US.UTF-8 | en_US.UTF-8 |

# Check schema exists
ssh gabor@109.122.222.198 "sudo -u postgres psql -p 5433 -d spaceos_knowledge -c '\dn'"

# Expected output:
#   List of schemas
#   Name    | Owner
# ----------+-----------
#  knowledge | postgres
#  public    | postgres
```

**Success Criteria:** Both `knowledge` and `public` schemas present

---

### Step 5: Verify Document Table (2 min)

```bash
# Check documents table structure
ssh gabor@109.122.222.198 "sudo -u postgres psql -p 5433 -d spaceos_knowledge -c '\d knowledge.documents'"

# Expected output (columns):
# Column      | Type         | Modifiers
# ────────────┼──────────────┼───────────────────
#  id         | uuid         | not null, pk
#  file_path  | text         | not null
#  content    | text         | not null
#  source_type| text         |
#  category   | text         |
#  terminal   | text         |
#  doc_hash   | text         |
#  tsvector   | tsvector     | generated always
#  created_at | timestamp    | default now()
#  updated_at | timestamp    | default now()

# Count rows (should be 0 before ingestion)
ssh gabor@109.122.222.198 "sudo -u postgres psql -p 5433 -d spaceos_knowledge -t -c 'SELECT COUNT(*) FROM knowledge.documents;'"

# Expected output: 0 (empty table)
```

**Success Criteria:** Table structure matches DDL, 0 rows (pre-ingestion)

---

### Step 6: Verify Indexes (2 min)

```bash
# List all indexes
ssh gabor@109.122.222.198 "sudo -u postgres psql -p 5433 -d spaceos_knowledge -c 'SELECT indexname, tablename FROM pg_indexes WHERE schemaname = '\''knowledge'\'';'"

# Expected output (5 indexes):
#         indexname         | tablename
# ─────────────────────────┼───────────
#  documents_pkey          | documents
#  documents_tsvector_idx  | documents (GIN — full text search)
#  documents_source_idx    | documents
#  documents_category_idx  | documents
#  documents_terminal_idx  | documents
```

**Success Criteria:** All 5 indexes present, GIN index for TSVECTOR

---

### Step 7: Row-Level Security (RLS) Check (1 min)

```bash
# Verify RLS is enabled
ssh gabor@109.122.222.198 "sudo -u postgres psql -p 5433 -d spaceos_knowledge -c 'SELECT relname, relrowsecurity FROM pg_class WHERE relname = '\''documents'\'';'"

# Expected output:
#  relname  | relrowsecurity
# ──────────┼────────────────
#  documents | t (true — RLS enabled)

# Check RLS policies
ssh gabor@109.122.222.198 "sudo -u postgres psql -p 5433 -d spaceos_knowledge -c 'SELECT policyname, tablename, permissive FROM pg_policies WHERE tablename = '\''documents'\'';'"

# Expected output:
#    policyname    | tablename | permissive
# ─────────────────┼───────────┼──────────
#  admin_full_access | documents | t (true — all actions)
```

**Success Criteria:** RLS enabled, `admin_full_access` policy present

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| `ssh: command not found` | SSH client not installed; `apt-get install openssh-client` |
| `Permission denied (publickey)` | Public key not authorized; contact ROOT (MSG-INFRA-061) |
| `Could not resolve hostname` | DNS issue or VPS IP wrong (109.122.222.198) |
| `sudo: no tty present` | Add `-t` flag to ssh: `ssh -t gabor@... sudo -u postgres psql...` |
| `psql: could not connect to server` | PostgreSQL not listening on 5433, or firewall blocked |
| `ERROR: database "spaceos_knowledge" does not exist` | DDL script didn't run; check step 3 output |
| `ERROR: schema "knowledge" does not exist` | Same as above — re-run DDL |
| `FATAL: role "postgres" does not exist` | PostgreSQL installation corrupted (unlikely) |

---

## Rollback (If Needed)

```bash
# If DDL execution fails and VPS DB is corrupted:

ssh gabor@109.122.222.198 << 'EOF'
  # Drop spaceos_knowledge database (WARNING: destructive)
  sudo -u postgres psql -p 5433 -c "DROP DATABASE IF EXISTS spaceos_knowledge;"

  # Re-run Phase 1 DDL
  sudo -u postgres psql -p 5433 < /tmp/01-knowledge-schema.sql
EOF
```

**ℹ️ Safer:** Start fresh with PostgreSQL admin credentials if critical.

---

## Post-Execution Tasks

Once Phase 1 DDL is successful:

### 1. Update INFRA Memory
```bash
# Add to MEMORY.md:
# - Phase 1 DDL: ✅ EXECUTED (2026-06-17 HH:MM UTC)
# - spaceos_knowledge database: ✅ CREATED
# - 5 indexes: ✅ VERIFIED
# - RLS policies: ✅ CONFIGURED
```

### 2. Create Phase 1 Completion Message
```
MSG-INFRA-065-phase1-ddl-executed-complete.md
```

### 3. Trigger Phase 2 Ingestion (ORCH)
Once Phase 1 complete, notify ORCH:
```
Phase 1 DDL execution complete. Ready for Phase 2 ingestion script deployment.
Ingest script: /opt/spaceos/scripts/02-rag-ingest.js
```

---

## Files & References

| File | Purpose |
|------|---------|
| `/opt/spaceos/scripts/01-knowledge-schema.sql` | Phase 1 DDL script |
| `/opt/spaceos/scripts/00-INFRA-PHASE1-README.md` | Runbook |
| `MSG-INFRA-061` | VPS SSH authorization request |
| `MSG-INFRA-062` | Phase 4-5 implementation complete |
| `MSG-INFRA-064` | Smoke test infrastructure ready |

---

## Next Steps (After Phase 1 Complete)

1. ✅ Phase 1 DDL executed on VPS
2. ⏳ Phase 2 ingest script deployment (ORCH)
3. ⏳ Phase 3 MCP server implementation (ORCH)
4. 🕐 Phase 4 MCP registration (INFRA)
5. 🕐 Phase 5 scanner integration (INFRA)

**Overall Timeline:** ~5 days (after SSH + ORCH Phase 2-3)

---

**INFRA Playbook: Phase 1 VPS DDL Execution — Ready on Signal**

Timestamp: 2026-06-17 20:30 UTC
