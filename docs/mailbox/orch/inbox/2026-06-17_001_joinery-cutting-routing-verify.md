---
id: MSG-ORCH-001
from: root
to: orch
type: task
priority: high
status: UNREAD
model: haiku
created: 2026-06-17
---

# ORCH-001 — Joinery + Cutting API Routing Verification

## Context

**Phase 2:** FE is integrating Joinery (material requisition) + Cutting (daily plans) endpoints.

**Your Task:** Verify Orchestrator correctly routes these APIs.

---

## Routes to Verify

### Joinery Module (Port 5002)
```
GET  /api/orders/{id}/material-req    → Joinery
GET  /api/orders/{id}/hardware-list   → Joinery
```

### Cutting Module (Port 5004)
```
POST /api/cutting/plans               → Cutting
GET  /api/cutting/plans?date=YYYY-MM-DD → Cutting
```

---

## Verification Checklist

```bash
# 1. Joinery routing (material-req)
curl http://localhost:3000/api/orders/test-order-1/material-req
# Expected: 200 OK, or 404 if order doesn't exist (but request reached Joinery)

# 2. Joinery routing (hardware-list)
curl http://localhost:3000/api/orders/test-order-1/hardware-list
# Expected: 200 OK or 404 (but reached Joinery service)

# 3. Cutting routing (POST plans)
curl -X POST http://localhost:3000/api/cutting/plans \
  -H "Content-Type: application/json" \
  -d '{"date":"2026-06-17","capacity":1000,"orders":["test"]}'
# Expected: 200 OK or validation error (but reached Cutting service)

# 4. Cutting routing (GET plans)
curl http://localhost:3000/api/cutting/plans?date=2026-06-17
# Expected: 200 OK or 404 (but reached Cutting service)
```

---

## If Route Missing

If any route returns 404 or times out:
1. Check Orchestrator router config (`src/api/routes/` or equivalent)
2. Add proxy route if missing:
   ```
   GET  /api/orders/:id/material-req   → proxy to http://joinery:5002
   GET  /api/orders/:id/hardware-list  → proxy to http://joinery:5002
   POST /api/cutting/plans              → proxy to http://cutting:5004
   GET  /api/cutting/plans              → proxy to http://cutting:5004
   ```
3. Verify both services are running (use `systemctl status` or `ps aux`)

---

## Definition of Done

- [x] All 4 routes return response (not 404 or timeout)
- [x] Response content-type is JSON
- [x] No proxy errors in Orchestrator logs

---

**ROOT Approval:** ✅ Verify + fix if needed
**Timeline:** 30 minutes
**Next:** Send DONE with results

🚀 **Routing Verified → FE Ready to Integrate**
