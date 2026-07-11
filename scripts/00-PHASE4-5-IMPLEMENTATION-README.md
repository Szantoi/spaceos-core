---
id: PHASE4-5-IMPLEMENTATION-README
date: 2026-06-17
owner: INFRA
status: READY
scope: INFRA Phase 4-5 execution
---

# INFRA Phase 4-5: MCP Registration & Scanner Integration

**Status:** ✅ READY FOR EXECUTION
**Created:** 2026-06-17 19:00 UTC
**Owner:** INFRA Terminal

---

## Overview

Phase 4-5 registers the Knowledge Service MCP server in Claude settings and integrates the knowledge ingestion pipeline into the broader scanner ecosystem.

### Dependencies

| Phase | Task | Owner | Status | Est. Duration |
|-------|------|-------|--------|---------------|
| 1 | DDL schema | INFRA | ✅ COMPLETE | 1 min |
| 2 | Node.js ingest script | ORCH | ⏳ PENDING | 1.5 days |
| 3 | MCP server (TypeScript) | ORCH | ⏳ PENDING | 2 days |
| **4** | **MCP registration** | **INFRA** | **✅ READY** | **0.5 days** |
| **5** | **Scanner integration** | **INFRA** | **✅ READY** | **0.5 days** |

**Prerequisite:** Phase 2-3 must be complete before executing Phase 4-5.

---

## Phase 4: MCP Server Registration (0.5 days)

### What This Does

Phase 4 registers the spaceos-knowledge MCP server in `~/.claude/settings.json`, enabling Claude Code and other Claude tools to discover and use the Knowledge Service.

### Files Created

| File | Purpose | Executable |
|------|---------|-----------|
| `04-phase4-mcp-registration.sh` | Main registration script | ✅ Yes |

### Pre-Execution Checklist

Before running Phase 4, verify:

- [ ] Phase 2 (Node.js ingest script) is complete (`/opt/spaceos/scripts/02-rag-ingest.js` exists)
- [ ] Phase 3 (MCP server) is deployed (`/opt/spaceos/spaceos-nexus/knowledge-service/src/mcp-server.js` exists)
- [ ] `~/.claude/settings.json` exists and is valid JSON
- [ ] `jq` command is available (`which jq`)

### Execution Steps

#### Step 1: Validate Pre-requirements

```bash
# Check if Phase 2-3 files exist
ls -la /opt/spaceos/scripts/02-rag-ingest.js
ls -la /opt/spaceos/spaceos-nexus/knowledge-service/src/mcp-server.js

# Check if jq is installed
which jq

# Validate settings.json
jq empty ~/.claude/settings.json
```

**Expected outcome:** No errors. All files present.

#### Step 2: Run Registration Script

```bash
cd /opt/spaceos/scripts
./04-phase4-mcp-registration.sh
```

**Expected output:**
```
[OK] Pre-flight checks passed
[INFO] Registering MCP server in settings.json...
[INFO] Backup created: /home/gabor/.claude/settings.json.backup.XXXXXXXXX
[OK] MCP server registered: spaceos-knowledge
[OK] MCP server configuration validated
[OK] MCP server started successfully
[OK] Phase 4: MCP Server Registration Complete
```

#### Step 3: Validate Registration

```bash
# Check if server is registered
jq '.mcpServers.["spaceos-knowledge"]' ~/.claude/settings.json

# Expected output:
# {
#   "command": "node",
#   "args": ["/opt/spaceos/spaceos-nexus/knowledge-service/src/mcp-server.js"],
#   "env": {
#     "DB_HOST": "localhost",
#     "DB_PORT": "5433",
#     "DB_NAME": "spaceos_knowledge",
#     "DB_USER": "postgres"
#   }
# }
```

#### Step 4: Test MCP Server Discovery (Optional)

Once registered, Claude Code should discover the server:

```bash
# List available MCP servers (requires Claude Code CLI)
claude list-mcp-servers

# Test knowledge_search tool
claude call-mcp spaceos-knowledge knowledge_search --query="EF Core migration"
```

### Rollback (If Needed)

```bash
# Rollback to previous settings.json
./04-phase4-mcp-registration.sh --rollback

# Verify rollback
jq '.mcpServers' ~/.claude/settings.json
# Should show empty object: {}
```

### Troubleshooting

| Issue | Solution |
|-------|----------|
| `jq: command not found` | Install jq: `apt-get install jq` |
| `settings.json is not valid JSON` | Fix JSON syntax or restore from backup |
| MCP server file not found | Phase 3 (ORCH) not complete; register anyway, server will be deployed later |
| `Permission denied` | Ensure script is executable: `chmod +x 04-phase4-mcp-registration.sh` |

---

## Phase 5: Scanner Integration (0.5 days)

### What This Does

Phase 5 integrates the knowledge ingestion script (`02-rag-ingest.js`) into a unified scanner pipeline that can run on a schedule (cron), on-demand, or after librarian memory cleanup.

### Files Created

| File | Purpose | Executable |
|------|---------|-----------|
| `05-phase5-scanner-integration.sh` | Integration & cron setup script | ✅ Yes |
| `pipeline-knowledge-index.sh` | Main scanner pipeline (updated) | ✅ Yes |

### Pre-Execution Checklist

Before running Phase 5, verify:

- [ ] Phase 2 (ingestion script) is complete (`/opt/spaceos/scripts/02-rag-ingest.js` exists and is executable)
- [ ] PostgreSQL is accessible (`psql -p 5433 -d spaceos_knowledge -c 'SELECT 1;'`)
- [ ] Log directory exists or can be created (`/var/log/spaceos/`)
- [ ] Phase 1 DDL has been executed (database `spaceos_knowledge` exists)

### Execution Steps

#### Step 1: Validate Pre-requirements

```bash
# Check if Phase 2 ingestion script exists
ls -lx /opt/spaceos/scripts/02-rag-ingest.js

# Check if it's executable
[ -x /opt/spaceos/scripts/02-rag-ingest.js ] && echo "✓ Executable" || echo "✗ Not executable"

# Test database connection
sudo -u postgres psql -p 5433 -d spaceos_knowledge -c "SELECT COUNT(*) FROM knowledge.documents;"

# Expected output: A number (document count)
```

**Expected outcome:** Ingestion script exists, is executable, and database is accessible.

#### Step 2: Run Integration Script

```bash
cd /opt/spaceos/scripts
./05-phase5-scanner-integration.sh
```

**Expected output:**
```
[INFO] Running pre-flight checks...
[OK] Pre-flight checks passed
[INFO] Creating/updating scanner script: /opt/spaceos/scripts/pipeline-knowledge-index.sh
[OK] Scanner script created: /opt/spaceos/scripts/pipeline-knowledge-index.sh
[OK] Validating Phase 5 integration...
[OK] Scanner script is valid
[OK] Ingest script is properly integrated
[OK] Database verification logic is present
[WARN] Cron job is not installed (can be added with --setup-cron)
[OK] Phase 5: Scanner Integration Ready
```

#### Step 3: Test Scanner Manually

```bash
# Run scanner manually to validate integration
/opt/spaceos/scripts/pipeline-knowledge-index.sh

# Check logs
tail -20 /var/log/spaceos/knowledge-scanner-*.log

# Expected output:
# [INFO] Starting knowledge service scanner pipeline...
# [INFO] Step 1: Running PostgreSQL ingestion (Phase 2)...
# [OK] Ingestion completed successfully
# [INFO] Step 2: Verifying database state...
# [OK] Database contains XXX documents
# [OK] Pipeline completed successfully
```

#### Step 4: Setup Cron Job (Optional but Recommended)

For automatic scanner execution every 6 hours:

```bash
# Setup cron job
./05-phase5-scanner-integration.sh --setup-cron

# Verify cron job is installed
crontab -l | grep "spaceos-knowledge-scanner"

# Expected output:
# # spaceos-knowledge-scanner
# 0 */6 * * * /opt/spaceos/scripts/pipeline-knowledge-index.sh >> /var/log/spaceos/knowledge-scanner.log 2>&1
```

**Cron Schedule Explanation:**
- Runs at: 00:00, 06:00, 12:00, 18:00 (UTC, every 6 hours)
- Logs to: `/var/log/spaceos/knowledge-scanner.log`
- Auto-rotates per timestamp: `/var/log/spaceos/knowledge-scanner-YYYYMMDD_HHMMSS.log`

#### Step 5: Monitor Scanner Logs

```bash
# View recent scanner logs
tail -f /var/log/spaceos/knowledge-scanner.log

# View document count history
tail /var/log/spaceos/knowledge-scanner-summary.log

# Check latest document count
cat /var/log/spaceos/knowledge-doc-count.txt
```

### Rollback (If Needed)

```bash
# Rollback scanner script and remove cron
./05-phase5-scanner-integration.sh --rollback

# Verify rollback
crontab -l | grep "spaceos-knowledge-scanner" || echo "Cron removed successfully"
```

### Troubleshooting

| Issue | Solution |
|-------|----------|
| `Ingest script not found` | Phase 2 (ORCH) not complete; cannot proceed |
| `Cannot connect to database` | Ensure Phase 1 DDL was executed and PostgreSQL is running on port 5433 |
| `/var/log/spaceos/` permission denied | Create directory: `mkdir -p /var/log/spaceos` |
| Cron job won't install | Check crontab permissions: `sudo crontab -l` |
| Scanner runs but shows 0 documents | Phase 1 DDL or Phase 2 ingestion not working; debug first |

---

## Complete Phase 4-5 Execution (Sequential)

To execute both phases in sequence:

```bash
cd /opt/spaceos/scripts

# Phase 4: Register MCP server
echo "=== Phase 4: MCP Registration ==="
./04-phase4-mcp-registration.sh

# Phase 5: Integrate scanner
echo "=== Phase 5: Scanner Integration ==="
./05-phase5-scanner-integration.sh --setup-cron

# Verify both phases
echo "=== Verification ==="
jq '.mcpServers["spaceos-knowledge"]' ~/.claude/settings.json
crontab -l | grep "spaceos-knowledge-scanner"
/opt/spaceos/scripts/pipeline-knowledge-index.sh --help || true
```

---

## Timeline

| Phase | Task | Duration | Status |
|-------|------|----------|--------|
| 4 | MCP registration | ~10 min | ✅ Ready to execute |
| 5 | Scanner integration | ~15 min | ✅ Ready to execute |
| **Total** | | **~25 min** | |

**Total execution time (both phases): ~25 minutes**

---

## Deliverables Checklist

After executing both phases, verify:

### Phase 4 Completion

- [ ] `~/.claude/settings.json` contains `mcpServers.spaceos-knowledge`
- [ ] MCP server entry has correct command: `node`
- [ ] Environment variables set: `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USER`
- [ ] Backup file created: `~/.claude/settings.json.backup.XXXXXXXXX`

### Phase 5 Completion

- [ ] `/opt/spaceos/scripts/pipeline-knowledge-index.sh` exists and is executable
- [ ] Scanner script calls `/opt/spaceos/scripts/02-rag-ingest.js`
- [ ] Log directory exists: `/var/log/spaceos/`
- [ ] Cron job installed (if requested): `crontab -l | grep spaceos-knowledge-scanner`
- [ ] Manual test passed: Scanner runs without errors

---

## Integration with Existing Systems

### Librarian Integration (If Applicable)

If librarian cleanup runs on a schedule, trigger knowledge ingestion afterward:

```bash
# In librarian cleanup script
/opt/spaceos/scripts/pipeline-knowledge-index.sh
```

### Notification Integration (If Applicable)

Phase 5 scanner calls `/opt/spaceos/scripts/critical-notify.sh` if it exists:

```bash
# Create simple notification script for testing
cat > /opt/spaceos/scripts/critical-notify.sh << 'EOF'
#!/bin/bash
SERVICE="$1"
MESSAGE="$2"
LEVEL="$3"
echo "[$(date -u +%Y-%m-%dT%H:%M:%SZ)] $SERVICE: $MESSAGE ($LEVEL)"
EOF
chmod +x /opt/spaceos/scripts/critical-notify.sh
```

---

## Files & Locations

### Phase 4 Files

| File | Purpose |
|------|---------|
| `/opt/spaceos/scripts/04-phase4-mcp-registration.sh` | Phase 4 main script |
| `~/.claude/settings.json` | Claude settings (modified) |
| `~/.claude/settings.json.backup.*` | Backup (auto-created) |

### Phase 5 Files

| File | Purpose |
|------|---------|
| `/opt/spaceos/scripts/05-phase5-scanner-integration.sh` | Phase 5 main script |
| `/opt/spaceos/scripts/pipeline-knowledge-index.sh` | Scanner pipeline (created/updated) |
| `/opt/spaceos/scripts/02-rag-ingest.js` | Ingestion script (referenced) |
| `/var/log/spaceos/knowledge-scanner-*.log` | Scanner logs |
| `/var/log/spaceos/knowledge-scanner-summary.log` | Summary log |
| `/var/log/spaceos/knowledge-doc-count.txt` | Latest document count |

---

## Next Steps After Phase 4-5

Once both phases complete successfully:

1. **Phase 3 Deployment** (ORCH responsibility)
   - Deploy MCP server to `/opt/spaceos/spaceos-nexus/knowledge-service/src/mcp-server.js`
   - Validate MCP tool discovery

2. **Full Pipeline Testing**
   - Run end-to-end test: DDL → Ingestion → MCP lookup → Search
   - Validate all 6 test categories in smoke test

3. **Production Scheduling**
   - Activate cron job for scheduled scanner runs
   - Monitor logs and document indexing

---

## Key Configuration References

### MCP Server Registration (Phase 4)

```json
{
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
  }
}
```

### Scanner Cron Schedule (Phase 5)

```bash
# Every 6 hours (00:00, 06:00, 12:00, 18:00 UTC)
0 */6 * * * /opt/spaceos/scripts/pipeline-knowledge-index.sh >> /var/log/spaceos/knowledge-scanner.log 2>&1
```

---

## Support & Debugging

### Common Questions

**Q: Can I run Phase 4-5 before Phase 2-3 are complete?**
A: Yes. Phase 4-5 can be prepared and validated. Actual execution depends on Phase 2-3 files existing, but registration and setup can proceed.

**Q: What if the MCP server is not yet deployed?**
A: Phase 4 script warns but proceeds. Once Phase 3 (ORCH) delivers the MCP server file, Claude will auto-discover it.

**Q: How do I test the scanner manually?**
A: Run `/opt/spaceos/scripts/pipeline-knowledge-index.sh` directly. Check logs in `/var/log/spaceos/`.

**Q: How do I disable the cron job?**
A: Run `./05-phase5-scanner-integration.sh --rollback` or manually edit crontab.

### Debug Commands

```bash
# Check MCP registration
jq '.mcpServers' ~/.claude/settings.json

# Check cron job
crontab -l | grep spaceos

# Check database connectivity
sudo -u postgres psql -p 5433 -d spaceos_knowledge -c "SELECT COUNT(*) FROM knowledge.documents;"

# Monitor scanner in real-time
tail -f /var/log/spaceos/knowledge-scanner.log

# Test ingestion script manually
/opt/spaceos/scripts/02-rag-ingest.js

# Test scanner script manually
/opt/spaceos/scripts/pipeline-knowledge-index.sh
```

---

## Status Summary

| Component | Status | Notes |
|-----------|--------|-------|
| Phase 4 script | ✅ READY | Executable, pre-flight checks included |
| Phase 5 script | ✅ READY | Executable, scanner template embedded |
| MCP registration | ✅ READY | Validates settings.json, auto-backup |
| Scanner pipeline | ✅ READY | 5-step pipeline, logging, notifications |
| Cron integration | ✅ READY | 6-hour schedule, error handling |
| Documentation | ✅ COMPLETE | Comprehensive troubleshooting guide |

---

**INFRA Phase 4-5: ✅ IMPLEMENTATION COMPLETE & READY FOR EXECUTION**

**Next Milestone:** Execute Phases 4-5 upon Phase 2-3 completion (ORCH responsibility)

**Timestamp:** 2026-06-17 19:00 UTC
