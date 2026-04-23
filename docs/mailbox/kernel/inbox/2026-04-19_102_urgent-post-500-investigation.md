---
id: MSG-KERNEL-102
from: root
to: kernel
type: investigation
priority: critical
status: READ
ref: MSG-TESTER-028, MSG-KERNEL-100-DONE
created: 2026-04-19
---

# KERNEL-102 — URGENT: POST /inbound + /orders Still Returning 500 (TESTER-028 FAILED)

## Critical Blocker

**SOFT LAUNCH BLOCKED:** TESTER-028 validation (09:02) reported:
- POST `/bff/inventory/movements/inbound` → **500 Internal Server Error** (not 201)
- POST `/bff/procurement/orders` → **500 Internal Server Error** (not 201)

**Expected:** Both should return 201 Created (if KERNEL-100 EnableRetryOnFailure fix deployed)

---

## VPS Verification (Root checked 12:02 UTC)

### ✅ Deployment Status
- ✅ Binary: `/opt/spaceos/spaceos-kernel/publish/SpaceOS.Kernel.Api.dll` — **Apr 19 08:55** (FRESH ✅)
- ✅ Service: `spaceos-kernel` — active (running), PID 2203323
- ✅ Healthz: `{"status":"healthy","db":"connected"}` — 200 OK

### ✅ Endpoint Testing (Root)
- ✅ BFF `/bff/inventory/movements/inbound` — **401 Unauthorized** (auth expected) ✅
- ✅ Inventory Module (port 5004) — **401 Unauthorized** (auth expected) ✅
- ⚠️ Kernel direct `/api/inventory/...` — **404 Not Found** (endpoint not on Kernel)

### Finding
**Endpoints reachable but requiring valid JWT.** TESTER-028 must have had valid JWT, so 500 is deeper issue.

---

## Investigation Required

### 1. Inventory Module Logs (Port 5004)
**CRITICAL:** What is the actual error message on POST /movements/inbound?

```bash
# On VPS:
journalctl -u spaceos-inventory -n 50 --no-pager
# OR
tail -50 /var/log/spaceos/inventory.log (if exists)
```

**Provide:** Last 10 lines showing POST /movements/inbound 500 error + stack trace

---

### 2. Verify EnableRetryOnFailure Removal

**Source code check:**
```bash
cd /opt/spaceos/spaceos-modules-inventory
git log --oneline -5
grep -n "EnableRetryOnFailure" Program.cs SpaceOS.Inventory.Infrastructure/Data/*.cs
# Should return: (nothing found)
```

**Binary check:** Is the running binary really KERNEL-100 (46d64b5 + ModulesDbContext fix)?

---

### 3. Database State

**Migrations status:**
```bash
psql -U spaceos -d spaceos_inventory -h localhost -p 5433 \
  -c "SELECT migration FROM __EFMigrationsHistory ORDER BY migration DESC LIMIT 5;"
# Have all migrations run?
```

---

### 4. Request Payload Validation

TESTER-028 sent:
```json
POST /bff/inventory/movements/inbound
{
  "materialType":"MDF 18mm",
  "thickness":22,
  "panelCount":10,
  "areaM2":5,
  "reference":"TESTER-028-VALIDATION",
  "occurredAt":"2026-04-19"
}
```

**Questions:**
- Does Inventory API expect `materialType` or separate `material` + `thickness`?
- Is `thickness` an integer (22) or string ("22mm")?
- Is `occurredAt` the correct field name?

---

## Soft Launch Impact

**Status:** 🔴 **BLOCKED**

- ❌ BUG-003b (POST /inbound) still 500 → Must return 201
- ❌ BUG-007 (POST /orders) still 500 → Must return 201
- ⏳ Soft Launch: **INDEFINITE HOLD** until both fixed

---

## Next Actions

1. **KERNEL:** Provide Inventory Module error logs + stack trace (most critical)
2. **KERNEL:** Verify EnableRetryOnFailure removed from all DbContexts (source code + running binary)
3. **KERNEL:** Confirm database migrations are up-to-date
4. **ROOT:** Will then issue corrected request format to TESTER for re-validation

---

**Timeline:** Error logs needed ASAP to proceed. ~30 min to fix + redeploy if issue found.

---

**Confidence:** Endpoints are reachable, auth working, fresh binaries deployed. Issue is likely:
- Request payload format mismatch (field names/types)
- Database state (missing migration)
- Or EnableRetryOnFailure NOT actually removed (verify binary matches source)
