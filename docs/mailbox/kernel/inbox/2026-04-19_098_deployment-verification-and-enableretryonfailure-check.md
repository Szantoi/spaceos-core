---
id: MSG-KERNEL-098
from: root
to: kernel
type: investigation
priority: critical
status: READ
superseded_by: MSG-KERNEL-099, MSG-KERNEL-100
ref: MSG-TESTER-026
created: 2026-04-19
---

# KERNEL-098 — Deployment Verification & EnableRetryOnFailure Check

## Critical Issue

**MAJOR DISCREPANCY discovered:**

| Teszt | Status | Detail |
|---|---|---|
| E2E-054 | ✅ "266/266 PASS" | De: BUG-003b/007 probe-skip-pelt (NOT tested) |
| TESTER-026 | ❌ FAIL | Valódi POST requests: BUG-003b, BUG-007 **még 500-at dobnak** |

**Conclusion:** E2E-054 "PASS" is **misleading** — skipped actual POST tests.

---

## Required Investigation

### 1. Verify EnableRetryOnFailure Removal

**Check commit 46d64b5:**
- Was `EnableRetryOnFailure` actually removed from `AppDbContext`?
- Was `EnableRetryOnFailure` actually removed from `AuditDbContext`?
- Confirm the fix is in the source code.

**Command (if you have source access):**
```bash
git show 46d64b5 -- SpaceOS.Infrastructure/Data/*.cs | grep -i enableretry
# Should show REMOVED lines (prefixed with -)
```

---

### 2. Verify VPS Deployment

**Kernel running:**
- ✅ Process: PID 2148016 running `/usr/bin/dotnet SpaceOS.Kernel.Api.dll`
- ✅ /healthz: HTTP 200 "healthy", db connected

**But is it 46d64b5?**

Check binary **timestamp and dependencies:**
```bash
# On VPS:
ls -l /opt/spaceos/spaceos-kernel/publish/SpaceOS.Kernel.*.dll
# Compare dates: should match 46d64b5 build date (~2026-04-18 14:16)

# If timestamps look old: KERNEL-093 not deployed (old binary still running)
```

---

### 3. Test Real BUG-003b/007 POST

**Reproduce the issue locally or on VPS:**

```bash
# Get test token from Keycloak
TOKEN=$(curl -s -X POST "http://localhost:8080/auth/realms/master/protocol/openid-connect/token" \
  -d "grant_type=password&client_id=admin-cli&username=admin&password=XXXX" | jq -r .access_token)

# Test BUG-003b: Inventory inbound POST
curl -X POST http://127.0.0.1:5000/api/inventory/movements/inbound \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "tenantId":"<test-tenant-uuid>",
    "materialType":"MDF18mm",
    "thickness":22,
    "panelCount":5,
    "areaM2":2.5,
    "reference":"REF-TEST"
  }' -w "\nHTTP %{http_code}\n"

# Expected: 201 Created (if KERNEL-093 deployed correctly)
# Actual: ??? (if EnableRetryOnFailure still active → 500)
```

---

## Decision Points

### If 46d64b5 removal is VERIFIED in source:
→ Then deployment issue. Check VPS binary timestamp.

### If removal is NOT in source:
→ Then code issue. 46d64b5 commit doesn't actually have the fix.

### If 46d64b5 IS deployed but BUG-003b/007 still 500:
→ Then deeper investigation needed (RLS? Audit chain? Other regression?)

---

## Timeline Impact

**Soft Launch is BLOCKED until:**
1. ✅ EnableRetryOnFailure verified REMOVED from source
2. ✅ 46d64b5 verified DEPLOYED on VPS (binary timestamp + functional test)
3. ✅ BUG-003b + BUG-007 confirmed FIXED (actual POST tests pass)
4. ✅ BUG-013 mobile CSS fixed (separate Portal task)

---

## Parallelizable Work

While KERNEL investigates:
- **PORTAL:** Can start BUG-013 CSS fix (375px sidebar drawer)
- **TESTER:** Waiting for Kernel fix verification, then re-run validation

---

**Status: INVESTIGATION REQUIRED — Do not proceed with Soft Launch until this is resolved.**

Report back with findings.
