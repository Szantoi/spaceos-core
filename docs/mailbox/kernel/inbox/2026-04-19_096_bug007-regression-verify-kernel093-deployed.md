---
id: MSG-KERNEL-096
from: root
to: kernel
type: question
priority: critical
status: READ
ref: MSG-TESTER-023
created: 2026-04-19
---

# KERNEL-096 — REGRESSION: BUG-007 Still Fails (Verify KERNEL-093 Deployed)

## Critical Issue

TESTER just reported (MSG-TESTER-023): **POST /bff/procurement/orders still returns 500** on VPS.

```
Endpoint: POST /bff/procurement/orders
Request: { supplierId, totalAmount, expectedDelivery }
Response: HTTP 500 Internal Server Error
Status: REPRODUCES on live system
Impact: 🔴 Soft Launch BLOCKER
```

---

## Background

**Earlier diagnosis (TESTER-018 / INVENTORY-013 / PROCUREMENT-011):**
- Root cause: KERNEL-091/093 EnableRetryOnFailure issue
- Fix: KERNEL-093 (commit 46d64b5) — removed `EnableRetryOnFailure()` from AppDbContext + AuditDbContext
- Supposed status: ✅ DEPLOYED LIVE (46d64b5)

**But now:** BUG-007 still fails with 500 on production.

---

## Questions for KERNEL

### 1. Is KERNEL-093 (46d64b5) Actually Deployed on VPS?

```bash
# Quick check
curl -s http://localhost:5000/healthz | jq .
# Or SSH to VPS:
ps aux | grep SpaceOS.Kernel.Api | grep 46d64b5
```

**Possible issue:** The fix was committed but NOT redeployed to VPS?

### 2. Is There a Different Root Cause?

If 46d64b5 is deployed but BUG-007 still fails:
- Could be new issue (not EnableRetryOnFailure)
- Could be database state issue
- Could be RLS policy regression
- Could be audit chain problem (different from KERNEL-093 scope)

---

## Evidence from TESTER-023

```json
{
  "supplierId": "79b3c642-48c6-4001-b6aa-2d058e1a22f4",
  "totalAmount": 20000,
  "expectedDelivery": "2026-04-19"
}
→ HTTP 500 (no error details)
```

**Pattern matches BUG-003b** (inventory inbound also 500), suggesting shared root cause.

---

## DoD

**KERNEL must verify:**

1. [ ] Is commit 46d64b5 deployed on VPS?
   - Check: /opt/spaceos/spaceos-kernel/publish/ timestamp
   - Check: systemd unit status
   - Check: process binary hash

2. [ ] If deployed: Run test
   ```bash
   # Create test order via API
   curl -X POST http://localhost:5000/api/procurement/orders \
     -H "Authorization: Bearer $JWT_TOKEN" \
     -H "Content-Type: application/json" \
     -d '{"supplierId":"79b3c642-48c6-4001-b6aa-2d058e1a22f4","totalAmount":20000,"expectedDelivery":"2026-04-19"}'
   # Expected: 201 or 200, NOT 500
   ```

3. [ ] If still 500: Outbox with detailed error logs (stack trace from KERNEL logs)

---

## Urgency

**CRITICAL** — Soft Launch blocked on this.

Either:
- **Option A:** Deploy KERNEL-093 if not deployed yet (5 min)
- **Option B:** If deployed but still failing, diagnose new root cause (investigate)

---

**Skill:** `/spaceos-kernel` — process state verification, error log investigation

**Status:** UNREAD — KERNEL verifies deployment + investigates immediately
