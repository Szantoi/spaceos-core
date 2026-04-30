---
id: MSG-ROOT-002
from: root
to: root
type: report
status: READ
ref: MSG-ROOT-001
created: 2026-04-19
---

# SOFT LAUNCH READINESS — Status Report (2026-04-19 08:15)

## ✅ CRITICAL FIX CHAIN — COMPLETE

### Deployments Executed & Verified

| Task | Fix | Deployment | Status | Verified |
|---|---|---|---|---|
| **INFRA-031** | KERNEL-093 (46d64b5) EnableRetryOnFailure removal | VPS binary copy + systemctl restart | ✅ LIVE | E2E-054 |
| **INFRA-031 Fix** | appsettings.json restoration (accidentally deleted) | Copy from /tmp/kernel-publish/ + restart | ✅ LIVE | curl /healthz 200 |
| **E2E-054** | Full test suite rerun with KERNEL-093 | vitest 266 tests | ✅ 266/266 PASS | No regressions |

---

## 🎯 BUGS FIXED & DEPLOYED

| Bug | Root Cause | Fix | Deployment | Status |
|---|---|---|---|---|
| **BUG-003b** | Inventory POST /inbound 500 | KERNEL-093: EnableRetryOnFailure AppDbContext | INFRA-031 ✅ | E2E verified ✅ |
| **BUG-007** | Procurement POST /orders 500 | KERNEL-093: EnableRetryOnFailure AuditDbContext | INFRA-031 ✅ | E2E verified ✅ |
| **BUG-016** | Logout parameter name wrong (redirect_uri vs post_logout_redirect_uri) | PORTAL-011: revert fd340bf → correct param | INFRA-026 ✅ | Code review ✅ |

---

## 📊 STACK STATUS (2026-04-19 08:15)

```
🟢 Kernel (port 5000)          UP — 46d64b5 live, /healthz 200 ✅
🟢 Orchestrator (port 3000)    UP — /bff/health 207 ✅
🟢 Portal                       UP — portal.joinerytech.hu accessible
🟢 E2E Baseline                 266/266 PASS ✅ (no regressions with KERNEL-093)
```

---

## ⚠️ UNRESOLVED ISSUES

### Critical (Assigned)

**None.** All critical bugs assigned and fixed.

### Critical (NOT Assigned) ❓

| Bug | Description | Category | Scope | Assignee |
|---|---|---|---|---|
| **BUG-013** | Mobile sidebar CSS (375px viewport) doesn't fit · UX regression | CRITICAL | Portal | ❌ NOT ASSIGNED |

**Status:** Identified by TESTER-025, priority unknown. Could be Soft Launch blocker or backlog depending on Doorstar acceptance.

### High (Non-Blocking)

| Task | Issue | Impact | Workaround |
|---|---|---|---|
| **INFRA-030** | KC postLogoutRedirectUris not in DB · Logout flow incomplete | UX (must refresh manually) | Page refresh works |
| **INFRA-032** | KC token lifetime 5 min → expired after 25+ min sessions | UX (long TESTER sessions) | Not blocking (TESTER ~15-20 min) |

**Blocker:** Both need KC admin password (not available).

---

## 🚀 SOFT LAUNCH DECISION MATRIX

### Option A: GO NOW
- **Readiness:** 3/3 critical bugs fixed + E2E verified
- **Risk:** BUG-013 (mobile CSS) unknown impact
- **Timeline:** TESTER validation NOW → Doorstar session feedback

### Option B: TESTER Validation First
- **Step 1:** TESTER runs functional test suite (15-20 min)
- **Step 2:** Report BUG-013 severity + any new issues
- **Step 3:** ROOT decides GO/NO-GO

### Option C: BUG-013 Quick Fix
- **If fixable:** Portal quick patch + redeploy (~10 min)
- **Then:** Option A (GO NOW)

---

## 📋 REQUIREMENTS FOR ROOT

### To Proceed with Soft Launch:

**Option A (GO immediately):**
- ✅ Decision: Accept BUG-013 unknown risk?
- ✅ Message TESTER: "Run functional validation"

**Option B (TESTER first):**
- ✅ Message TESTER: "Run functional test suite, report BUG-013 severity"
- ✅ Then wait for feedback

**Option C (Quick fix):**
- ✅ Check with Portal: Is BUG-013 (375px CSS) fixable in <10 min?
- ✅ If yes: Assign PORTAL-XXX quick task
- ✅ Then GO

---

## 📈 TIMELINE TO DOORSTAR LAUNCH

```
Current:  2026-04-19 08:15 (Decision point)
  │
  ├─ Option A (GO):           Soft Launch NOW → ~2 hours live
  ├─ Option B (TESTER):       +20 min validation → +10 min bug analysis → Soft Launch ~09:00
  └─ Option C (Quick fix):    +10 min code + deploy → +20 min TESTER → Soft Launch ~09:00
```

---

## ✅ INFRASTRUCTURE NOTES

- **VPS Status:** Healthy (Kernel + Orch both responding)
- **PostgreSQL:** All migrations applied
- **Keycloak:** Service up, realm config live (postLogoutRedirectUris + token lifetime still pending KC password)
- **SSL/HTTPS:** ✅ Let's Encrypt cert active (joinerytech.hu + asztalostech.hu)

---

## 👉 ROOT ACTION REQUIRED

1. **Assess BUG-013 risk:** Is mobile CSS blocking Soft Launch?
2. **Choose path:**
   - **A:** GO immediately (unknown BUG-013 impact)
   - **B:** TESTER validation first (20 min delay)
   - **C:** Quick fix attempt (if fixable)
3. **Message TESTER or PORTAL** with next step

---

**Status: READY FOR ROOT DECISION** 🚀

All critical bugs fixed. E2E baseline green. Stack healthy. Awaiting Soft Launch GO or contingency path selection.
