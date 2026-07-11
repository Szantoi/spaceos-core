---
id: MSG-INFRA-066
from: infra
to: root
type: status
priority: critical
status: READ
ref: MSG-INFRA-059
created: 2026-06-17
---

# MSG-INFRA-059 Fix Plan: joinerytech.hu 403 Forbidden

**Status:** BLOCKED (awaiting VPS SSH access via MSG-INFRA-061)

**Severity:** CRITICAL — blocks Doorstar demo

**Timeline:** 15 minutes (once SSH available)

---

## Problem Analysis

**Issue:** https://joinerytech.hu/ returns 403 Forbidden

**Root Causes (Most Likely):**
1. Document root directory not set correctly in nginx config
2. Document root directory does not exist
3. Document root missing index.html file
4. Document root has incorrect permissions (nginx user cannot read)
5. Frontend build not deployed to VPS
6. nginx config syntax error or service not running

---

## Solution Architecture

### Phase A: Diagnostic (5 min)

**Script:** `/opt/spaceos/scripts/diagnose-nginx-403.sh` (created, ready)

**Checks:**
1. nginx config file exists (`/etc/nginx/sites-enabled/joinerytech*`)
2. Document root path extracted from nginx config
3. Document root directory exists
4. Document root has correct permissions (755)
5. index.html exists in document root
6. nginx error logs show specific 403 reason
7. nginx config syntax valid
8. nginx service is running

---

### Phase B: Fix (10 min)

**Steps:**

#### B1: Ensure Document Root Exists
```bash
ssh gabor@109.122.222.198 << 'EOF'
  # Extract doc root from nginx config
  DOC_ROOT=$(grep -oP 'root\s+\K[^;]+' /etc/nginx/sites-enabled/joinerytech* | head -1 | tr -d ' ')

  # Create if missing
  [ -d "$DOC_ROOT" ] || sudo mkdir -p "$DOC_ROOT"

  # Fix permissions (nginx reads)
  sudo chmod 755 "$DOC_ROOT"

  # Verify
  ls -lad "$DOC_ROOT"
EOF
```

#### B2: Deploy Frontend Build
```bash
# Local: export frontend build
cd /opt/spaceos/frontend/joinerytech-portal
npm run build  # if needed

# Upload dist/ to VPS
scp -r dist/* gabor@109.122.222.198:/var/www/joinerytech/

# Verify upload
ssh gabor@109.122.222.198 "ls -la /var/www/joinerytech/index.html"
```

#### B3: Fix Permissions
```bash
ssh gabor@109.122.222.198 << 'EOF'
  # Set correct ownership + permissions
  sudo chown -R www-data:www-data /var/www/joinerytech/
  sudo chmod -R 755 /var/www/joinerytech/
  sudo chmod 644 /var/www/joinerytech/*.html
  sudo chmod 644 /var/www/joinerytech/*.js
  sudo chmod 644 /var/www/joinerytech/*.css

  # Verify
  ls -la /var/www/joinerytech/
EOF
```

#### B4: Reload nginx
```bash
ssh gabor@109.122.222.198 << 'EOF'
  # Test config
  sudo nginx -t

  # Reload (graceful restart)
  sudo systemctl reload nginx

  # Verify service
  sudo systemctl status nginx
EOF
```

#### B5: Test
```bash
# Test HTTP
curl -I http://joinerytech.hu/

# Test HTTPS
curl -I https://joinerytech.hu/

# Expected: 200 OK (not 403)
```

---

## Execution Plan (Once SSH Available)

### Quick Reference Commands

```bash
# 1. Diagnostic
ssh gabor@109.122.222.198 "bash -s" < /opt/spaceos/scripts/diagnose-nginx-403.sh

# 2. Build frontend locally
cd /opt/spaceos/frontend/joinerytech-portal && npm run build

# 3. Deploy build
scp -r /opt/spaceos/frontend/joinerytech-portal/dist/* gabor@109.122.222.198:/var/www/joinerytech/

# 4. Fix permissions
ssh gabor@109.122.222.198 << 'EOF'
DOC_ROOT="/var/www/joinerytech"
sudo chown -R www-data:www-data "$DOC_ROOT"
sudo chmod -R 755 "$DOC_ROOT"
sudo chmod 644 "$DOC_ROOT"/*.{html,js,css,json}
sudo nginx -t && sudo systemctl reload nginx
EOF

# 5. Verify
curl -I https://joinerytech.hu/
```

---

## Files Prepared

| File | Purpose | Status |
|------|---------|--------|
| `diagnose-nginx-403.sh` | Diagnostic script (7-step check) | ✅ READY |
| This plan | Fix procedure + commands | ✅ READY |

---

## Rollback (If Needed)

If 403 persists after fix:

```bash
# 1. Check error logs
ssh gabor@109.122.222.198 "sudo tail -20 /var/log/nginx/error.log"

# 2. Verify document root
ssh gabor@109.122.222.198 "grep root /etc/nginx/sites-enabled/joinerytech*"

# 3. Check file permissions
ssh gabor@109.122.222.198 "ls -la /var/www/joinerytech/"

# 4. Manual nginx config check
ssh gabor@109.122.222.198 "sudo nginx -t -v"

# 5. Restart nginx (harder reset)
ssh gabor@109.122.222.198 "sudo systemctl restart nginx"
```

---

## Success Criteria

- [ ] SSH connection to VPS works
- [ ] nginx config found and parsed
- [ ] Document root directory exists
- [ ] Frontend build deployed to document root
- [ ] Permissions set to 755 (directories), 644 (files)
- [ ] nginx config syntax valid
- [ ] nginx service running
- [ ] `curl -I https://joinerytech.hu/` returns 200 OK
- [ ] JoineryTech Portal loads in browser

---

## Blockers

**PRIMARY BLOCKER:** VPS SSH access
- Awaiting: MSG-INFRA-061 (ROOT authorization)
- Impact: Cannot execute any VPS commands
- Timeline: Once SSH authorized, fix takes ~15 min

---

## Next Actions

1. **ROOT:** Authorize VPS SSH public key (MSG-INFRA-061) if not already done
2. **INFRA:** Execute diagnostic script
3. **INFRA:** Deploy frontend build
4. **INFRA:** Fix permissions + reload nginx
5. **INFRA:** Verify: `curl -I https://joinerytech.hu/`

---

## References

- Diagnostic script: `/opt/spaceos/scripts/diagnose-nginx-403.sh`
- Frontend source: `/opt/spaceos/frontend/joinerytech-portal/`
- Frontend build: `/opt/spaceos/frontend/joinerytech-portal/dist/`
- nginx config location: `/etc/nginx/sites-enabled/joinerytech*` or `/etc/nginx/sites-available/*`
- Typical doc root: `/var/www/joinerytech/` or `/srv/http/joinerytech/`

---

## INFRA Status

**MSG-INFRA-059 Diagnosis:** COMPLETE ✅

**MSG-INFRA-059 Fix:** READY (awaiting SSH)

**Timeline:** 15 minutes once VPS access available

---

**INFRA Terminal: joinerytech.hu 403 fix plan prepared. Awaiting VPS SSH authorization (MSG-INFRA-061).**

Timestamp: 2026-06-17 21:20 UTC
