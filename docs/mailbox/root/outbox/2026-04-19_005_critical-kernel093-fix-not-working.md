---
id: MSG-ROOT-005
from: root
to: root
type: report
priority: critical
status: UNREAD
created: 2026-04-19
ref: MSG-TESTER-027
---

# 🚨 CRITICAL: KERNEL-093 Fix NOT WORKING — Soft Launch BLOCKED

## Summary

**TESTER-027 validation (with valid JWT token) reports:**

```
❌ BUG-003b: POST /bff/inventory/movements/inbound → 500 (NOT FIXED)
❌ BUG-007: POST /bff/procurement/orders → 500 (NOT FIXED)
⚠️ BUG-013: Mobile sidebar (not tested, known blocker)
⚠️ Auth state flapping: 401 → 200 on GET requests
```

**Expected:** 201 Created (if KERNEL-093 46d64b5 deployed)
**Actual:** 500 Internal Server Error (same as before "fix")

---

## Major Discrepancy

**KERNEL-098 investigation concluded:**
- ✅ KERNEL-093 (46d64b5) "deployed"
- ✅ EnableRetryOnFailure "removed from source"
- ✅ VPS binaries "updated Apr 19 07:57"
- ✅ Local tests "1138/1138 PASS"

**BUT TESTER real testing shows:**
- ❌ Fix doesn't work
- ❌ Same 500 errors as before
- ❌ Invalid deployment or wrong binary

---

## Possible Root Causes

### 1. Deployment Incomplete
- Binaries copied, but **wrong binary** (old code)?
- **Orchestrator/Portal BFF not updated** to match Kernel changes?
- **Database migration not applied?**

### 2. Different Root Cause
- Not `EnableRetryOnFailure`, but something else?
- **materialType encoding** ("MDF18mm" vs "MDF 18mm")?
- **RLS/audit chain issue** unrelated to retry strategy?

### 3. Auth State Issue
- **JWT refresh broken** (401 → 200 flapping)?
- **Keycloak session management** problem?

---

## Soft Launch Status

### ❌ **VALIDATION FAILED — DO NOT PROCEED**

**Critical Blockers (3 of 3 still unresolved):**
1. BUG-003b: 500 (FIX DIDN'T WORK)
2. BUG-007: 500 (FIX DIDN'T WORK)
3. BUG-013: Mobile 375px (CSS, not implemented)

**Timeline Impact:**
- ❌ Cannot launch
- ❌ Major regression investigation needed
- ❌ Soft Launch: **ON HOLD INDEFINITELY**

---

## Required Immediate Actions

### KERNEL
1. **Verify 46d64b5 source code** — is EnableRetryOnFailure actually gone?
2. **Check VPS deployment** — are we running correct binary?
3. **Error logs** — POST request detailed error messages
4. **Alternative hypothesis** — if not EnableRetryOnFailure, what else?

### INFRA
1. **Deployment verification:**
   - Binary timestamp check: `/opt/spaceos/spaceos-kernel/publish/SpaceOS.Kernel.Api.dll`
   - Compare against 46d64b5 build date (2026-04-18 17:42)
2. **Systemd journal check:**
   - Look for transaction-related errors
   - EnableRetryOnFailure exceptions?
3. **Consider fresh rebuild + deploy** if current deployment corrupted

### ORCHESTRATOR
1. **Check BFF logs** for 500 errors from Kernel
2. **Verify proxy routes** are correct
3. **Auth token handling** — check JWT refresh logic

### PORTAL
1. **Check client payload** — materialType encoding correct?
2. **Error handling** — what error is returned from BFF?

---

## Recommendation

**STOP all Soft Launch preparation.**

1. **Immediate:** KERNEL + INFRA + ORCH investigate
   - Deploy logs
   - Binary verification
   - Error traces from POST requests
2. **Root cause determination:** Where is the 500 actually coming from?
3. **Fix + redeploy:** Once root cause found
4. **TESTER re-validation:** Only after actual fix confirmed

**Timeline to resume:** Unknown (depends on investigation depth)

---

## Current Situation

Deployment appears to have failed silently:
- Process reports success
- Health checks pass
- But actual POST operations still fail

This suggests either:
- **Wrong binary deployed** (old code still running)
- **Deployment partially applied** (Kernel updated but BFF/DB not)
- **Different root cause** (not retry strategy)

---

**Status: INVESTIGATION REQUIRED — Soft Launch indefinitely blocked**

All terminals (KERNEL, INFRA, ORCH) should focus on root cause analysis.
