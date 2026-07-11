# SpaceOS Q3 Cutting Expansion - Deployment Checklist

> **Version:** 1.0.0
> **Date:** 2026-06-23
> **Tracks:** A (Customer Portal), B (Pricing), C (ShopFloor Integration)

---

## Pre-Deployment Checklist

### ✅ Code Readiness
- [ ] All Track A/B/C pull requests merged to `main`
- [ ] CI/CD pipeline passed (build + tests)
- [ ] Code review completed and approved
- [ ] Database migrations generated and reviewed

### ✅ Infrastructure Readiness
- [ ] VPS SSH access verified
- [ ] PostgreSQL database backed up (see [Backup Procedure](#database-backup))
- [ ] Nginx configuration backup created
- [ ] Systemd service files prepared
- [ ] Environment variables documented

### ✅ Testing Readiness
- [ ] Unit tests: >90% coverage (Track A/B/C)
- [ ] Integration tests passed locally
- [ ] Smoke test script reviewed and ready

---

## Deployment Steps

### Step 1: Database Backup (CRITICAL)

```bash
# SSH to VPS
ssh spaceos@joinerytech.hu

# Create backup directory
mkdir -p ~/backups/q3-$(date +%Y%m%d)

# Backup database
pg_dump -U spaceos -d spaceos -F c -b -v \
  -f ~/backups/q3-$(date +%Y%m%d)/spaceos-pre-q3.dump

# Verify backup size
ls -lh ~/backups/q3-$(date +%Y%m%d)/
```

**Expected backup size:** 50-200 MB (depends on data volume)

---

### Step 2: Build & Deploy Code

```bash
# Navigate to backend directory
cd /opt/spaceos/backend

# Pull latest code
git fetch origin
git checkout main
git pull origin main

# Verify correct commit
git log -1 --oneline

# Build Track B: Pricing Module (NEW)
cd spaceos-modules-pricing
dotnet build -c Release
dotnet publish -c Release -o bin/Release/net8.0/publish

# Build Track A/C: Cutting Module (UPDATED)
cd ../spaceos-modules-cutting
dotnet build -c Release
dotnet publish -c Release -o bin/Release/net8.0/publish

# Build Identity Module (OperatorPin extension)
cd ../spaceos-modules-identity
dotnet build -c Release
dotnet publish -c Release -o bin/Release/net8.0/publish
```

**Build success criteria:**
- 0 errors
- 0 warnings (or only expected warnings)
- `bin/Release/net8.0/publish` directory created

---

### Step 3: Install Systemd Service (Pricing Module)

```bash
# Copy systemd service file
sudo cp /opt/spaceos/infra/systemd/spaceos-modules-pricing.service \
  /etc/systemd/system/

# Reload systemd daemon
sudo systemctl daemon-reload

# Enable service (auto-start on boot)
sudo systemctl enable spaceos-modules-pricing

# Verify service configuration
systemctl cat spaceos-modules-pricing
```

**Expected output:** Service file content displayed, no errors

---

### Step 4: Nginx Configuration

```bash
# Backup current nginx config
sudo cp /etc/nginx/sites-available/joinerytech.hu \
  /etc/nginx/sites-available/joinerytech.hu.pre-q3-backup

# Add Q3 routes to nginx config
# (Manually merge /opt/spaceos/infra/nginx/sites-available/joinerytech.hu.q3-routes)
sudo nano /etc/nginx/sites-available/joinerytech.hu

# Add rate limit zone to nginx.conf
sudo nano /etc/nginx/nginx.conf
# Add in http {} block:
# limit_req_zone $binary_remote_addr zone=quote_limit:10m rate=10r/h;

# Test nginx configuration
sudo nginx -t

# If test passes, reload nginx
sudo systemctl reload nginx
```

**Nginx test success criteria:**
- `syntax is ok`
- `test is successful`

---

### Step 5: Database Migrations

```bash
# Run migration script
cd /opt/spaceos/scripts
./migrate-q3.sh

# Expected output:
# - Track A: Cutting Module migrations applied
# - Track B: Pricing Module migrations applied
# - Track C: Cutting Module migrations applied (same module as Track A)
# - Identity: OperatorPin extension migration applied
# - ✅ All migrations completed successfully
```

**Migration success criteria:**
- No errors
- All migrations applied
- Post-migration validation passed

**If migration fails:**
```bash
# Check database logs
sudo journalctl -u postgresql -n 50

# Manual rollback (ONLY IF NECESSARY)
./rollback-q3.sh
```

---

### Step 6: Start Services

```bash
# Start Pricing module (NEW service)
sudo systemctl start spaceos-modules-pricing

# Verify service started
sudo systemctl status spaceos-modules-pricing

# Check logs for startup errors
journalctl -u spaceos-modules-pricing -n 50 --no-pager

# Restart Cutting module (UPDATED code)
sudo systemctl restart spaceos-modules-cutting

# Verify service restarted
sudo systemctl status spaceos-modules-cutting

# Check logs
journalctl -u spaceos-modules-cutting -n 50 --no-pager

# Restart Identity module (OperatorPin extension)
sudo systemctl restart spaceos-modules-identity

# Verify
sudo systemctl status spaceos-modules-identity
```

**Service health criteria:**
- Status: `active (running)`
- No error logs in last 50 lines
- Process ID (PID) present

---

### Step 7: Smoke Tests

```bash
# Set environment variables
export ADMIN_TOKEN="<your-admin-jwt-token>"
export BASE_URL="https://joinerytech.hu"
export TEST_WORKSTATION_ID="<workstation-guid>"
export TEST_USER_ID="<user-guid>"
export TEST_OPERATOR_PIN="1234"

# Run smoke tests
cd /opt/spaceos/scripts
./smoke-test-q3.sh
```

**Smoke test success criteria:**
- All 10 tests passed (or skipped with reason)
- 0 failures
- Exit code 0

**If smoke tests fail:**
1. Check service logs: `journalctl -u spaceos-modules-pricing -f`
2. Verify nginx routing: `curl -I https://joinerytech.hu/pricing/health`
3. Check database connectivity: `psql -U spaceos -d spaceos -c "SELECT 1"`

---

### Step 8: Post-Deployment Verification

```bash
# Verify endpoints respond
curl https://joinerytech.hu/pricing/health
curl https://joinerytech.hu/cutting/health
curl https://joinerytech.hu/identity/health

# Verify OpenAPI docs
curl https://joinerytech.hu/pricing/swagger/v1/swagger.json | jq '.info.title'
curl https://joinerytech.hu/cutting/swagger/v1/swagger.json | jq '.info.title'

# Test public quote request (rate limit check)
curl -X POST https://joinerytech.hu/cutting/api/public/quote-requests \
  -H "Content-Type: application/json" \
  -d '{"customerName":"Test","customerEmail":"test@example.com","customerPhone":"+36301234567","pieces":[]}'

# Expected response: 201 Created (first request)
# After 11th request: 429 Too Many Requests
```

---

## Rollback Procedure

**If deployment fails**, execute rollback:

```bash
cd /opt/spaceos/scripts
./rollback-q3.sh
```

See [Q3_ROLLBACK_PLAN.md](Q3_ROLLBACK_PLAN.md) for detailed rollback instructions.

---

## Deployment Timeline

| Step | Duration | Cumulative |
|---|---|---|
| 1. Database Backup | 5 min | 5 min |
| 2. Build & Deploy Code | 10 min | 15 min |
| 3. Systemd Service Install | 5 min | 20 min |
| 4. Nginx Configuration | 10 min | 30 min |
| 5. Database Migrations | 5 min | 35 min |
| 6. Start Services | 5 min | 40 min |
| 7. Smoke Tests | 10 min | 50 min |
| 8. Post-Deployment Verification | 10 min | 60 min |

**Total estimated time:** ~1 hour

---

## Success Criteria

Deployment is considered successful when:

✅ All services running (`spaceos-modules-pricing`, `spaceos-modules-cutting`, `spaceos-modules-identity`)
✅ All smoke tests passed (8+ of 10, skipped tests acceptable)
✅ Health checks return HTTP 200
✅ OpenAPI docs accessible
✅ Rate limit functioning (HTTP 429 after 10 requests)
✅ No error logs in service journals

---

## Contacts & Support

**Deployment Lead:** Backend Terminal
**Escalation:** Root Terminal (strategic issues)
**Documentation:** `/opt/spaceos/docs/deployment/`

**Logs location:**
- Pricing: `journalctl -u spaceos-modules-pricing -f`
- Cutting: `journalctl -u spaceos-modules-cutting -f`
- Identity: `journalctl -u spaceos-modules-identity -f`
- Nginx: `/var/log/nginx/error.log`
