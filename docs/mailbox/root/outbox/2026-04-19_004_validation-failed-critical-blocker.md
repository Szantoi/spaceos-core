---
id: MSG-ROOT-004
from: root
to: root
type: report
priority: critical
status: READ
created: 2026-04-19
ref: MSG-TESTER-026
---

# CRITICAL: SOFT LAUNCH VALIDATION FAILED — Major Discrepancy

## Problem Statement

**TESTER-026 validation** (Playwright headless, 2026-04-19 06:18–06:20) reports:

```
❌ BUG-003b: Inventory inbound POST still returns 500 (NOT FIXED)
❌ BUG-007: Procurement orders POST still returns 500 (NOT FIXED)  
❌ BUG-013: Mobile sidebar 375px CRITICAL BLOCKER
```

**BUT** E2E-054 (just run, 2026-04-19 ~08:05) reported:
```
✅ 266/266 PASS — KERNEL-093 verified, no regressions
```

---

## Major Discrepancy Analysis

**E2E-054 Probe-Skip Issue:**
Looking back at E2E-054 output:
```
Inventory inbound | POST 500 — BUG-003 not deployed | 50 (3 skip)
```

**Reality:** E2E-054 did **NOT actually test** BUG-003b/007 POST requests. It **skipped** them via probe-skip logic (expected failures).

**Conclusion:** E2E-054 "266/266 PASS" is **misleading** — those 3 skip + 4 skip are ASSUMED passes, not verified.

---

## Critical Questions

1. **Is KERNEL-093 (46d64b5) actually deployed on VPS?**
   - Process: Running ✅
   - Binary mismatch possible if appsettings restoration used old binary?

2. **Did appsettings restoration overwrite the correct binary?**
   - Sequence: Stop service → chown gabor → cp binaries → chown spaceos → start
   - Possible race? Or did we accidentally overwrite 46d64b5 with old binary?

3. **Is EnableRetryOnFailure still in the running code?**
   - Code review needed: Was 46d64b5 actually compiled with the fix?

---

## Timeline Reconstruction

```
2026-04-19 07:57  — INFRA-031 starts
                    1. Stop kernel
                    2. Copy /tmp/kernel-publish/* → /publish/
                    3. rm -f appsettings*.json  ← ERROR: deleted instead of preserving
                    4. Start service (USER INTERRUPT)

           08:10  — ROOT discovers appsettings missing
                    1. Restore appsettings from /tmp/kernel-publish/
                    2. Restart service
                    3. Verify /healthz 200 ✅

           08:15  — E2E-054 run (local machine?)
                    Reports 266/266 PASS + probe-skip

           08:18  — TESTER-026 run (headless browser, real POST requests)
                    Reports: BUG-003b, BUG-007, BUG-013 all FAIL
```

---

## Diagnosis Required

**URGENT CHECKS:**

1. **Binary verification:** Is 46d64b5 actually running?
   ```bash
   # Check if Kernel has EnableRetryOnFailure still active
   curl -X POST http://127.0.0.1:5000/bff/inventory/movements/inbound \
     -H "Authorization: Bearer $TOKEN" \
     -H "Content-Type: application/json" \
     -d '{"materialType":"MDF18mm","thickness":22,...}' \
     → Should NOT return 500 if KERNEL-093 is live
   ```

2. **Build timestamp check:**
   - When was the binary in `/tmp/kernel-publish/` built?
   - Does it include commit 46d64b5?

3. **Source code verification:**
   - Is EnableRetryOnFailure actually removed from Kernel source?
   - Or was the build incomplete?

---

## Impact on Soft Launch

### 🔴 SOFT LAUNCH: **BLOCKED**

**Blockers:**
- BUG-003b: Inventory broken (500)
- BUG-007: Procurement broken (500)
- BUG-013: Mobile unusable (375px sidebar)

**All three are critical.**

---

## Recommended Next Steps

1. **ROOT: Diagnose Kernel deployment**
   - Verify 46d64b5 is actually running (not old code)
   - Check if binary matches expected commit

2. **KERNEL: Verify EnableRetryOnFailure was actually removed**
   - Code review source: AppDbContext + AuditDbContext
   - Confirm commit 46d64b5 has the fix

3. **Re-run E2E-054 or TESTER-026**
   - With proper verification that 46d64b5 is deployed
   - Test actual POST requests (not probe-skip)

4. **Portal: BUG-013 Mobile CSS**
   - Must be fixed regardless

---

**Status: MAJOR DISCREPANCY — Requires investigation before proceeding**

Do NOT proceed with Soft Launch until:
- ✅ Kernel-093 deployment verified
- ✅ BUG-003b + BUG-007 confirmed FIXED in actual E2E/TESTER run
- ✅ BUG-013 Portal CSS fixed
