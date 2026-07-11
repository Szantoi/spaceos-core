# SpaceOS Q3 Cutting Expansion - Rollback Plan

> **Version:** 1.0.0
> **Date:** 2026-06-23
> **Purpose:** Emergency rollback procedure for Q3 deployment

---

## When to Rollback

Execute rollback if ANY of these conditions occur:

🚨 **Critical Issues:**
- Core business functionality broken (orders can't be placed)
- Data corruption or loss detected
- Security vulnerability exposed
- Multiple services failing repeatedly

⚠️ **Moderate Issues:**
- >50% of smoke tests failing
- High error rate (>100 errors/hour)
- Performance degradation >3x baseline
- Database migration failure with data integrity impact

✅ **Do NOT rollback for:**
- Single feature bugs (can be hotfixed)
- UI/UX issues (non-blocking)
- Minor performance issues
- Rate limit configuration tweaks

---

## Rollback Decision Matrix

| Issue | Severity | Rollback? | Alternative |
|---|---|---|---|
| Pricing calculation wrong | HIGH | ✅ YES | Hotfix if <10% impact |
| Public quote form broken | HIGH | ✅ YES | Disable endpoint temporarily |
| Kiosk login not working | MEDIUM | ⚠️ MAYBE | Fallback to manual assignment |
| Rate limit too strict | LOW | ❌ NO | Adjust nginx config |
| OpenAPI docs 404 | LOW | ❌ NO | Redeploy docs only |

---

## Automated Rollback Script

```bash
# Execute from VPS
cd /opt/spaceos/scripts
./rollback-q3.sh
```

**Script performs:**
1. ✅ Stops Pricing module service
2. ✅ Disables Pricing service auto-start
3. ✅ Reverts nginx configuration
4. ✅ Optionally reverts database schema
5. ✅ Restarts Cutting module (previous version)

**Execution time:** ~5 minutes (excluding DB rollback)

---

## Manual Rollback Procedure

### Step 1: Stop Services

```bash
# Stop Pricing module (Q3 Track B)
sudo systemctl stop spaceos-modules-pricing
sudo systemctl disable spaceos-modules-pricing

# Verify stopped
sudo systemctl status spaceos-modules-pricing
```

---

### Step 2: Revert Nginx Configuration

```bash
# Restore pre-Q3 backup
sudo cp /etc/nginx/sites-available/joinerytech.hu.pre-q3-backup \
       /etc/nginx/sites-available/joinerytech.hu

# Test config
sudo nginx -t

# Reload nginx
sudo systemctl reload nginx

# Verify nginx running
sudo systemctl status nginx
```

**Verification:**
```bash
curl -I https://joinerytech.hu/pricing/health
# Expected: 404 Not Found (route removed)

curl -I https://joinerytech.hu/cutting/health
# Expected: 200 OK (still working)
```

---

### Step 3: Revert Database Schema (OPTIONAL, DESTRUCTIVE)

⚠️ **WARNING:** This step is DESTRUCTIVE and may cause DATA LOSS.

**Option A: Drop Pricing Schema (Track B only)**

```bash
psql -U spaceos -d spaceos

-- Drop Pricing schema
DROP SCHEMA IF EXISTS spaceos_pricing CASCADE;

-- Verify
\dn spaceos_pricing
-- Expected: No results

\q
```

**Data loss:** ALL pricing data (price lists, material prices, rules)

---

**Option B: Revert Cutting Module Migrations (Track A/C)**

```bash
cd /opt/spaceos/backend/spaceos-modules-cutting

# List migrations
dotnet ef migrations list

# Revert to pre-Q3 migration (example: AddBatchAssignmentFields)
dotnet ef database update AddBatchAssignmentFields --no-build

# Verify
psql -U spaceos -d spaceos -c "\d spaceos_cutting.machine_queues"
# Expected: Table does not exist
```

**Data loss:**
- Public quote requests (Track A)
- Machine queues (Track C)
- Operator sessions (Track C)
- Batch production tracking data (Track C)

---

**Option C: Revert Identity OperatorPin Column**

```bash
psql -U spaceos -d spaceos

-- Drop OperatorPin column
ALTER TABLE spaceos_identity.spaceos_users
DROP COLUMN IF EXISTS operator_pin;

-- Verify
\d spaceos_identity.spaceos_users
-- Expected: No operator_pin column

\q
```

**Data loss:** All operator PIN configurations

---

**Option D: Full Database Restore (SAFEST)**

```bash
# Stop all services
sudo systemctl stop spaceos-modules-*

# Restore from backup
pg_restore -U spaceos -d spaceos --clean \
  ~/backups/q3-20260623/spaceos-pre-q3.dump

# Restart services
sudo systemctl start spaceos-modules-cutting
sudo systemctl start spaceos-modules-identity
```

**Data loss:** ALL data created after backup (quotes, sessions, price lists)

---

### Step 4: Revert Code (Git)

```bash
cd /opt/spaceos/backend

# Find commit before Q3 merge
git log --oneline -10

# Checkout previous commit (example)
git checkout abc123def

# Rebuild services
cd spaceos-modules-cutting
dotnet build -c Release
dotnet publish -c Release -o bin/Release/net8.0/publish

cd ../spaceos-modules-identity
dotnet build -c Release
dotnet publish -c Release -o bin/Release/net8.0/publish
```

---

### Step 5: Restart Services

```bash
# Restart Cutting module
sudo systemctl restart spaceos-modules-cutting

# Verify
sudo systemctl status spaceos-modules-cutting
journalctl -u spaceos-modules-cutting -n 20 --no-pager

# Restart Identity module
sudo systemctl restart spaceos-modules-identity

# Verify
sudo systemctl status spaceos-modules-identity
```

---

### Step 6: Verify Rollback Success

```bash
# Core endpoints still working
curl https://joinerytech.hu/cutting/health
# Expected: 200 OK

curl https://joinerytech.hu/identity/health
# Expected: 200 OK

# Q3 endpoints removed
curl https://joinerytech.hu/pricing/health
# Expected: 404 Not Found

curl -X POST https://joinerytech.hu/cutting/api/public/quote-requests
# Expected: 404 Not Found (if Track A reverted)

# Check service logs
journalctl -u spaceos-modules-cutting -n 50 --no-pager | grep -i error
# Expected: No critical errors
```

---

## Post-Rollback Actions

### Immediate (Within 1 hour)

- [ ] Notify stakeholders (Conductor, Root)
- [ ] Document rollback reason
- [ ] Preserve error logs for analysis
- [ ] Update deployment status

### Within 24 hours

- [ ] Root cause analysis (RCA)
- [ ] Create bugfix tasks
- [ ] Plan re-deployment
- [ ] Review deployment process

### Within 1 week

- [ ] Fix identified issues
- [ ] Re-test in staging
- [ ] Update deployment checklist
- [ ] Schedule re-deployment

---

## Rollback Communication Template

```
Subject: [ROLLBACK] SpaceOS Q3 Cutting Expansion

Date: 2026-06-23
Time: 14:30 UTC
Duration: ~10 minutes
Affected Services: Pricing Module, Cutting Module (partial)

Reason for Rollback:
[Brief description - e.g., "Critical bug in price calculation causing incorrect quotes"]

Actions Taken:
✅ Pricing service stopped
✅ Nginx config reverted
✅ Database schema reverted (Pricing only)
✅ Services restarted and verified

Current Status:
✅ Core functionality restored
✅ Pre-Q3 features working normally
❌ Q3 features unavailable (as expected)

Next Steps:
1. Root cause analysis (Due: 2026-06-24)
2. Bugfix deployment (ETA: TBD)
3. Post-mortem meeting (Scheduled: TBD)

Impact:
- Customer Portal: Unavailable (fallback to email/phone quotes)
- Pricing Engine: Reverted to manual pricing
- ShopFloor Kiosk: Unavailable (fallback to manual assignment)

Questions: contact Backend Terminal
```

---

## Rollback Testing (Pre-Deployment)

**Before deploying to production**, test rollback procedure in staging:

```bash
# Staging environment
cd /opt/spaceos-staging/scripts
./rollback-q3.sh

# Verify rollback success
./smoke-test-pre-q3.sh  # Should pass
./smoke-test-q3.sh      # Should fail (Q3 features removed)
```

---

## Emergency Contacts

| Role | Contact | Escalation |
|---|---|---|
| Backend Terminal | Primary | Immediate |
| Conductor | Secondary | Within 1 hour |
| Root | Escalation | Critical only |

**Escalation criteria:**
- Data loss >1000 records
- Downtime >1 hour
- Security breach
- Regulatory compliance issue

---

**Last updated:** 2026-06-23
**Owner:** Backend Terminal
**Reviewed by:** Conductor
