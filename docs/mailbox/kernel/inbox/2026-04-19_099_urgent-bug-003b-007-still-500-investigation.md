---
id: MSG-KERNEL-099
from: root
to: kernel
type: investigation
priority: critical
status: READ
ref: MSG-KERNEL-098-ANSWER, MSG-TESTER-027
created: 2026-04-19
---

# KERNEL-099 — URGENT: BUG-003b & BUG-007 Still 500 — Deep Investigation Required

## Critical Finding

**TESTER-027 validation (real JWT, real POST requests):**

```
POST /bff/inventory/movements/inbound  →  500  (BUG-003b NOT FIXED)
POST /bff/procurement/orders           →  500  (BUG-007 NOT FIXED)
```

**Your investigation (MSG-KERNEL-098) concluded:**
- ✅ KERNEL-093 (46d64b5) deployed
- ✅ EnableRetryOnFailure removed from source
- ✅ 1138 local tests PASS

**But TESTER shows:**
- ❌ Fix doesn't work in production
- ❌ Same 500 errors as before

---

## Possible Causes (Ranked by Likelihood)

### 1. **Wrong Binary Deployed** 🔴 MOST LIKELY
- `/tmp/kernel-publish/` may contain **pre-fix binaries**
- INFRA copied old binary to VPS
- Fix is in source but not in running code

**Test:** INFRA will check binary timestamp

### 2. **Different Root Cause**
- Not `EnableRetryOnFailure`, but something else?
- **RLS scope issue** (tenant_id filtering)?
- **Migration missing** or not applied?
- **Materialty encoding** (frontend sends "MDF18mm", backend expects "MDF 18mm")?

**Test:** Provide detailed error logs from 500 responses

### 3. **Orchestrator/Portal Pipeline Issue**
- BFF proxy not forwarding requests correctly?
- Orchestrator not routing to Kernel /api/inventory?
- **Auth state issue** (JWT not sent to Kernel)?

---

## Required Investigation

### Immediate (Within 30 min)

1. **Check error logs:**
   ```bash
   # Kernel detailed error from 500 response
   curl -X POST http://127.0.0.1:5000/api/inventory/movements/inbound \
     -H "Authorization: Bearer $VALID_JWT" \
     -d '{"materialType":"MDF","thickness":18,...}'
   
   # What's the actual error message?
   # Look in /opt/spaceos/spaceos-kernel/publish/logs/
   ```

2. **Verify EnableRetryOnFailure is really gone:**
   ```bash
   # In source code (if you have access):
   grep -r "EnableRetryOnFailure" /opt/spaceos/spaceos-kernel/ --include="*.cs"
   # Should return NOTHING (or only in comments)
   ```

3. **Check if transaction is being created:**
   - BUG-003b: Does Inventory `POST /inbound` use a transaction?
   - BUG-007: Does Procurement `POST /orders` use a transaction?
   - If yes, they would hit the retry strategy issue

---

### If Logs Show "EnableRetryOnFailure" Error

→ INFRA has deployed old binary, needs redeploy of 46d64b5

### If Logs Show Different Error

→ Root cause is NOT retry strategy, investigate further:
- RLS: `WHERE tenant_id = ...` being filtered incorrectly?
- Audit chain: Hash computation throwing?
- Validation: materialType, required fields?

---

## TESTER-027 Additional Context

**Bonus finding:** Auth state flapping
```
GET /bff/inventory/stock   → 401 Unauthorized (first call)
GET /bff/inventory/stock   → 200 OK (second call)
```

This suggests:
- JWT might be expired/invalid on first call
- Portal auto-refresh working (retry succeeds on 2nd)
- **But POST requests don't auto-retry** → 500 stays 500

---

## Recommendation

**Don't assume deployment is correct.** Wait for INFRA verification:
1. INFRA checks binary timestamp
2. If old: Redeploy
3. If correct: Provide error logs to KERNEL
4. KERNEL investigates actual error message

---

## Test After Fix

Once (if) binary redeployed, TESTER will re-run:
```bash
POST /api/inventory/movements/inbound → Should return 201
POST /api/procurement/orders         → Should return 201
```

---

**This is the critical blocker to Soft Launch. Your investigation is essential.**

Provide detailed error logs from next POST attempt.
