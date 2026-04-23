---
id: MSG-ROOT-003
from: root
to: root
type: decision
status: UNREAD
created: 2026-04-19
ref: MSG-ROOT-002
---

# SOFT LAUNCH Phase Clarification — Not Yet Live Production

## Context

**Soft Launch ≠ Production Live**
- Soft Launch = Internal testing phase (Doorstar test team)
- No public users yet
- No urgent "production down" pressure
- Issues are discoverable + fixable during test window

---

## Revised Decision Matrix

With this context, **BUG-013** and other issues are **non-critical** until Doorstar uses the system live.

### Path Forward

**Option B is now OPTIMAL:**

```
2026-04-19 08:15 — NOW
  │
  ├─ TESTER runs functional validation (15-20 min)
  │  └─ Full workflow: Dashboard → Suppliers → Inventory → Orders → Chat → Logout
  │  └─ Reports: BUG-013 severity, any NEW issues
  │
  ├─ ROOT reviews TESTER feedback (5 min)
  │  └─ If minor: Known issues → continue
  │  └─ If critical: Assign PORTAL quick fix
  │
  └─ Soft Launch LIVE (Doorstar test team activates)
     └─ Timeline: ~09:00–09:30 CET

```

---

## Action Items

### Immediate (ROOT)

1. **Message TESTER:** "Run full functional test suite (E2E happy path)"
   - Dashboard load
   - Supplier add/list
   - Inventory inbound POST (BUG-003b fix verification)
   - Order creation (BUG-007 fix verification)
   - Chat streaming
   - Logout flow (manual redirect test)
   - Report BUG-013 severity + any showstoppers

2. **Prepare PORTAL contingency:** If BUG-013 is real blocker
   - Quick CSS fix (~5 min)
   - Rebuild + deploy (~5 min)
   - Ready to execute if TESTER reports "critical"

### Parallel (INFRA)

- Monitor Kernel/Orch logs during TESTER run
- Alert if any 5xx errors

### Awaiting (E2E)

- TESTER validation results

---

## Non-Blocking Issues (Can wait)

- **INFRA-030** (KC logout redirect) — needs KC password
- **INFRA-032** (KC token 5 min) — needs KC password
- Both have workarounds, can be fixed post-launch

---

## Timeline Estimate

```
08:15  ROOT decision (now)
08:20  TESTER starts functional validation
08:40  TESTER reports BUG-013 + feedback
08:45  ROOT reviews + decides (portal fix YES/NO)
09:00  Soft Launch LIVE (or +5-10 min if quick fix needed)
```

---

**Status: PROCEED with TESTER validation → Soft Launch GO**
