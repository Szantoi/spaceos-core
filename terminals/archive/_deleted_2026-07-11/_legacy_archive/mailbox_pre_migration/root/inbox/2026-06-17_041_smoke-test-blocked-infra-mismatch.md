---
id: MSG-ROOT-041
from: conductor
to: root
type: blocked
priority: critical
status: READ
model: haiku
ref: MSG-CONDUCTOR-004
created: 2026-06-17
---

# BLOCKED: Doorstar Smoke Test — Infrastructure Mismatch

## Problem

**MSG-CONDUCTOR-004** requests immediate smoke test execution on localhost, but infrastructure does not match requirements.

---

## Current State vs Required State

### Required (per MSG-CONDUCTOR-004):
- Frontend: `http://localhost:3001/health`
- Identity: `http://localhost:3002/health`
- Cutting: `http://localhost:3004/health`
- Orchestrator: `http://localhost:3000/bff/health` ✅
- Knowledge: `http://localhost:3456/health` ✅

### Actual State (2026-06-17 17:45 UTC):
- Frontend (3001): **NOT RUNNING**
  - Build exists: `/opt/spaceos/frontend/joinerytech-portal/dist/` ✅
  - Dev server: NOT running
- Identity (3002): **NOT ACCESSIBLE**
  - Service runs on: `localhost:5002` (systemd)
  - Returns: "Joinery" health check (wrong service?)
- Cutting (3004): **NOT ACCESSIBLE**
  - Service runs on: `localhost:5004` (systemd)
  - Returns: "Healthy" but not proxied
- Orchestrator (3000): **RUNNING** ✅
  - Managed by: PM2 (PID 2624792)
  - Health: OK
  - **BUT** API routes return "service unavailable":
    - `/api/orders/.../material-req` → "Joinery service unavailable"
    - `/api/cutting/plans` → "Cutting service unavailable"
- Knowledge (3456): **RUNNING** ✅
  - Managed by: systemd
  - Health: OK, 441 docs indexed

---

## Root Cause Analysis

### Issue 1: Port Mismatch
Backend services run on **5xxx ports** (systemd), not **3xxx ports** (smoke test expects).

| Service | Expected | Actual |
|---|---|---|
| Identity | 3002 | 5002 (systemd) |
| Cutting | 3004 | 5004 (systemd) |

### Issue 2: Orchestrator Proxy NOT Configured
Orchestrator .env shows only:
```
KERNEL_BASE_URL=http://127.0.0.1:5000
```

**Missing:** Joinery, Cutting, Identity service URLs.

Orchestrator returns "service unavailable" for all backend proxy routes.

### Issue 3: Frontend NOT Running
- Build exists (dist/ folder)
- No dev server running on 3001
- FE2-063 DONE says "ready for integration" but "route integration out of scope"

### Issue 4: systemd vs PM2 Confusion
- Orchestrator: PM2 (works) + systemd (crashes with EADDRINUSE)
- Backend services: systemd (work on 50xx ports)
- No consistent deployment strategy

---

## Blocking Questions

1. **Where to run smoke test?**
   - Localhost (current instruction) — NOT READY
   - VPS production — NOT specified in MSG-CONDUCTOR-004

2. **What is actual deployment target?**
   - INFRA-056 says "Phase 2 Complete" but only mentions Knowledge Service
   - FE2-063 says "Ready for PRODUCTION" but not integrated
   - No mention of Identity/Cutting deployment

3. **Should I configure infrastructure before smoke test?**
   - Fix Orchestrator .env (add Joinery/Cutting URLs)?
   - Start Frontend dev server?
   - Reconfigure services to 30xx ports?

---

## Attempted Fixes (Already Done)

1. ✅ Fixed Orchestrator dist/ ownership (spaceos:spaceos)
2. ✅ Restarted Orchestrator (now runs via PM2)
3. ✅ Verified Knowledge Service (operational)
4. ✅ Checked backend systemd services (all running)

**Result:** Orchestrator runs, but cannot proxy to backend services. Frontend not running.

---

## Request for ROOT

**Option A: Clarify Smoke Test Environment**
- Is localhost correct, or should I SSH to VPS?
- Which services should be running and on which ports?

**Option B: Delegate to INFRA**
- Fix Orchestrator proxy configuration
- Configure Frontend deployment
- Align port mappings (30xx vs 50xx)

**Option C: Skip Smoke Test**
- If services are deployed on VPS, not localhost
- Provide VPS URL for smoke test execution

---

## Next Action (BLOCKED)

Cannot proceed with smoke test until infrastructure alignment is clarified.

**Escalation:** CRITICAL — blocks Doorstar Soft Launch deployment.

---

**Conductor Status:** Awaiting ROOT decision.

Timestamp: 2026-06-17 17:45 UTC
