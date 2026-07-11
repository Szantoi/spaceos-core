---
id: ops-runbook-database-security
title: "Ops Runbook: Database File Security & Permissions"
type: documentation
epic: EPIC-09
section: "Security & Deployment"
date: 2026-03-06
---

# Ops Runbook: Database File Security & Permissions

## 🔒 Database File Security (TASK-09-04A)

This section documents the production deployment procedure for securing SQLite database files.

---

## Overview

**Objective:** Ensure SQLite database files have correct permissions (0640) on production servers, preventing unauthorized access while maintaining read-only agent connections.

**Security Model:**

- File permissions: `0640` (rw-r-----) — owner read/write, group read, others denied
- Ownership: `_mcp-server:_mcp-server` (dedicated MCP server user)
- Read-only enforcement: PRAGMA `query_only = ON` (duplicate protection layer)

**Why This Matters:**

- Prevents other processes from modifying the database file
- Restricts read access to authorized MCP server processes
- Complements PRAGMA-level read-only enforcement (defense-in-depth)

---

## Deployment Procedure

### Step 1: Create MCP Server User (First Deploy Only)

```bash
# On production server (as root):
sudo useradd -r -s /bin/false -d /nonexistent _mcp-server

# Verify
getent passwd _mcp-server
# Output: _mcp-server:x:1000:1000::/nonexistent:/bin/false
```

### Step 2: Deploy Application & Create Database

```bash
# 1. Pull latest code
cd /opt/mcp-server
git pull origin main

# 2. Install dependencies
npm ci --production

# 3. Run seeder (creates database at database/metadata.db)
npm run seed

# Output should show:
# [AgentDbSeeder] ✓ Seed complete: 360 roles + 120 schemas inserted
```

### Step 3: Secure Database File

```bash
# Run security script (idempotent - safe to re-run)
sudo bash scripts/secure-database-file.sh database/metadata.db

# Script output:
# [secure-database-file.sh] Securing database file permissions...
# [Step 3] Setting file ownership...
# ✓ Ownership set: _mcp-server:_mcp-server
# [Step 4] Setting file permissions...
# ✓ Permissions set: 0640 (rw-r-----)
# [Step 5] Securing WAL-related files...
# ✓ WAL file permissions: 0640
# ✓ Shared memory file permissions: 0640
# ✓ SUCCESS: Database file secured correctly
```

### Step 4: Verify Permissions

```bash
# Check file permissions
ls -la database/metadata.db

# Expected output:
# -rw-r----- 1 _mcp-server _mcp-server 2097152 Mar  6 10:30 database/metadata.db
#  ^^^^^^^^^^
#  0640 permissions (rw-r-----)

# Verify WAL mode files (if exist)
ls -la database/metadata.db-*

# Expected:
# -rw-r----- 1 _mcp-server _mcp-server 8192 Mar  6 10:30 database/metadata.db-shm
# -rw-r----- 1 _mcp-server _mcp-server 32768 Mar  6 10:30 database/metadata.db-wal
```

### Step 5: Start Application

```bash
# Start MCP server as _mcp-server user
sudo systemctl start mcp-server

# Verify running
sudo systemctl status mcp-server
# active (running)

# Check database access
sudo -u _mcp-server npm run test:integration
# All integration tests should pass
```

### Step 6: Post-Deployment Validation

```bash
# In CI/CD pipeline (GitHub Actions):
# File: .github/workflows/database-security.yml
# - Runs after seeder
# - Calls secure-database-file.sh
# - Verifies permissions (0640 or 0600)
# - Blocks deployment if permissions invalid

# Manual validation:
curl -s http://localhost:3000/health
# {"status":"healthy","database":"ready"}
```

---

## Configuration Files

### systemd Service File

**File:** `/etc/systemd/system/mcp-server.service`

```ini
[Unit]
Description=JoineryTech MCP Server
After=network.target

[Service]
Type=simple
User=_mcp-server
Group=_mcp-server
WorkingDirectory=/opt/mcp-server
ExecStart=/usr/bin/node /opt/mcp-server/src/index.ts
Restart=on-failure
RestartSec=10
StandardOutput=journal
StandardError=journal

[Install]
WantedBy=multi-user.target
```

Enable and start:

```bash
sudo systemctl daemon-reload
sudo systemctl enable mcp-server
sudo systemctl start mcp-server
```

### Database File Security Cron Job (Optional)

**File:** `/etc/cron.d/mcp-security`

Verify permissions daily:

```cron
# Daily security check at 2am
0 2 * * * root bash /opt/mcp-server/scripts/secure-database-file.sh /opt/mcp-server/database/metadata.db >> /var/log/mcp-security.log 2>&1
```

---

## Troubleshooting

### Problem: "Permission Denied" Errors

**Symptom:**

```
Error: SQLITE_CANTOPEN: unable to open database file
```

**Diagnosis:**

```bash
# Check database permissions
stat database/metadata.db

# If permissions are 0644 (world readable):
ls -la database/metadata.db
# -rw-r--r-- (WRONG - should be -rw-r-----)
```

**Solution:**

```bash
# Re-run security script
sudo bash scripts/secure-database-file.sh database/metadata.db

# Verify
ls -la database/metadata.db
# Should show: -rw-r----- (0640)
```

### Problem: "MCP Server User Not Found"

**Symptom:**

```
[secure-database-file.sh] WARNING: MCP server user '_mcp-server' not found
```

**Diagnosis:**
MCP server user not created on server

**Solution:**

```bash
# Create user
sudo useradd -r -s /bin/false _mcp-server

# Re-run security script
sudo bash scripts/secure-database-file.sh database/metadata.db
```

### Problem: "WAL File Permissions Mismatch"

**Symptom:**

```
CI/CD: ✗ FAIL: WAL file permissions not secure
```

**Diagnosis:**
WAL file (database/metadata.db-wal) has incorrect permissions (e.g., 0644)

**Solution:**

```bash
# Secure all WAL files
sudo chmod 0640 database/metadata.db-*

# Verify
ls -la database/metadata.db-*
# All should show: -rw-r-----
```

---

## Security Best Practices

### 1. Never Run as root

❌ **Wrong:**

```bash
npm start  # As root - DATABASE WORLD-READABLE
```

✅ **Correct:**

```bash
sudo -u _mcp-server npm start  # As dedicated user
```

### 2. Verify After Every Update

```bash
# After deployment
sudo bash scripts/secure-database-file.sh database/metadata.db

# Verify in CI/CD logs
grep "SUCCESS" /var/log/deployment.log
```

### 3. Backup with Correct Permissions

```bash
# When backing up database
sudo cp database/metadata.db database/metadata.db.backup.$(date +%Y%m%d)

# Secure backup
sudo bash scripts/secure-database-file.sh database/metadata.db.backup.*

# Verify
sudo chmod 0640 database/metadata.db.backup.*
ls -la database/metadata.db.backup.*
```

### 4. Monitor Permission Changes

```bash
# Watch for unexpected permission changes
sudo inotifywait -m database/metadata.db

# Log file access
sudo auditctl -w database/metadata.db -p wa -k database_changes
```

---

## Monitoring & Alerts

### Health Check

**Endpoint:** `GET /health`

```bash
curl http://localhost:3000/health

# Response:
{
  "status": "healthy",
  "database": "ready",
  "permissions": "0640",
  "owner": "_mcp-server:_mcp-server"
}
```

### Log Monitoring

**MCP Server Logs:**

```bash
sudo journalctl -u mcp-server -f

# Look for permission errors:
# ERROR: Permission denied (database file)
# ERROR: SQLITE_CANTOPEN
# WARNING: Database not readable
```

**Security Logs:**

```bash
sudo cat /var/log/mcp-security.log

# Should show:
# [secure-database-file.sh] ✓ SUCCESS: Database file secured correctly
```

---

## Rollback Procedure

If permissions were accidentally changed to insecure state:

```bash
# 1. Stop application
sudo systemctl stop mcp-server

# 2. Check current state
ls -la database/metadata.db
# If showing: -rw-r--r-- (0644) ← WRONG

# 3. Fix permissions
sudo bash scripts/secure-database-file.sh database/metadata.db

# 4. Verify
ls -la database/metadata.db
# Should show: -rw-r----- (0640) ✓

# 5. Restart
sudo systemctl start mcp-server

# 6. Validate
sudo systemctl status mcp-server
# active (running)
```

---

## References

- **TASK-09-04A:** Database File Permissions & CI/CD Integration
- **Security Script:** `scripts/secure-database-file.sh`
- **CI/CD Pipeline:** `.github/workflows/database-security.yml`
- **PRAGMA Read-Only:** See SECURITY-SOLUTIONS-GUIDE.md

---

## Sign-Off

- [ ] Database file secured (0640 permissions)
- [ ] Ownership set to `_mcp-server:_mcp-server`
- [ ] CI/CD validation passing
- [ ] All unit tests passing
- [ ] Health check endpoint responding
- [ ] Logs show "SUCCESS"
- [ ] Ready for production
