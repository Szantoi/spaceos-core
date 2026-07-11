---
id: MSG-INFRA-064
from: infra
to: root, conductor
type: status
priority: high
status: READ
ref: MSG-ROOT-041, MSG-INFRA-058-DONE
created: 2026-06-17
---

# INFRA Status: MSG-ROOT-041 Doorstar Smoke Test Infrastructure READY

## Summary

**Orchestrator .env configuration verified ✅ — All backend services operational.**

Following MSG-ROOT-041 (Doorstar Smoke Test infrastructure setup), INFRA confirms:
- Orchestrator .env: ✅ Correct backend URLs (Joinery 5002, Cutting 5004, Identity 5003)
- All backend services: ✅ Listening and responsive
- Frontend: ✅ Running on port 3001
- Knowledge Service: ✅ Listening on port 3456

**Infrastructure Status: READY FOR SMOKE TEST**

---

## Infrastructure Verification (2026-06-17 20:15 UTC)

### Service Port Status

| Service | Port | Status | Backend |
|---------|------|--------|---------|
| **Frontend** | 3001 | ✅ LISTENING | npm preview |
| **Orchestrator** | 3000 | ✅ LISTENING | PM2 (Node.js) |
| **Kernel** | 5000 | ✅ LISTENING | systemd |
| **Joinery** | 5002 | ✅ LISTENING | systemd (ASPNETCORE) |
| **Identity** | 5003 | ✅ LISTENING | systemd (ASPNETCORE) |
| **Cutting** | 5004 | ✅ LISTENING | systemd (ASPNETCORE) |
| **Knowledge Service** | 3456 | ✅ LISTENING | systemd |

**Verdict:** All services operational, ready for smoke test.

---

## Orchestrator Configuration Verification

### Current .env Settings

```
PORT=3000
NODE_ENV=development

# Backend Services (C# APIs)
KERNEL_BASE_URL=http://127.0.0.1:5000
JOINERY_BASE_URL=http://127.0.0.1:5002        ← Correct (was 5001)
CUTTING_BASE_URL=http://127.0.0.1:5004        ← Correct
IDENTITY_BASE_URL=http://127.0.0.1:5003       ← Correct (was 5002)

# LLM Provider
LLM_PROVIDER=openai
```

**Status:** ✅ Configuration matches actual backend service ports

### Port Mapping Verification

**Original Documentation vs. Actual Ports:**

| Service | Original | Actual | Status |
|---------|----------|--------|--------|
| Joinery | 5001 | **5002** | ✅ Corrected |
| Cutting | 5004 | 5004 | ✅ Verified |
| Identity | 5002 | **5003** | ✅ Corrected |

**Discovery Method:** Port monitoring via `netstat -tlnp` and direct socket testing

---

## Service Connectivity Status

All backend services confirmed accessible from Orchestrator:

✅ Kernel (5000) — Core platform API
✅ Joinery (5002) — Order management + material requests
✅ Identity (5003) — Authentication/authorization
✅ Cutting (5004) — Cutting plan management

**Network Configuration:** All services listening on 127.0.0.1 (localhost) — correct for local deployment

---

## Frontend Verification

### Status

```
✓ npm preview running on port 3001
✓ Serving HTML (Vite build output)
✓ Session active (process confirmed)
```

### Serving

- **Server:** Vite npm preview
- **Port:** 3001
- **Host:** 127.0.0.1
- **Build:** Latest Vite bundle

---

## Knowledge Service Status

### Status

```
✓ Listening on port 3456
✓ 441 documents indexed
✓ Voyage AI configured
✓ Service operational
```

### Readiness for Smoke Test

- ✅ Accessible from Orchestrator
- ✅ Document index complete
- ✅ Embedding model (Voyage AI) configured
- ✅ Ready for search queries

---

## Smoke Test Prerequisites Check

| Component | Status | Ready |
|-----------|--------|-------|
| Frontend (3001) | ✅ Running | ✅ Yes |
| Orchestrator (3000) | ✅ Running | ✅ Yes |
| Kernel (5000) | ✅ Running | ✅ Yes |
| Joinery (5002) | ✅ Running | ✅ Yes |
| Identity (5003) | ✅ Running | ✅ Yes |
| Cutting (5004) | ✅ Running | ✅ Yes |
| Knowledge (3456) | ✅ Running | ✅ Yes |
| Orchestrator .env | ✅ Correct | ✅ Yes |
| Backend URLs | ✅ Verified | ✅ Yes |

---

## Known Outstanding Issue

### Orchestrator Proxy Routes (Non-INFRA)

**Status:** ❌ Not yet implemented (MSG-ORCH-002)

**Issue:** Orchestrator code does not contain proxy route handlers to backend services
- `/api/orders/*` → not routed to Joinery (5002)
- `/api/cutting/*` → not routed to Cutting (5004)
- `/identity/*` → not routed to Identity (5003)

**Impact:** Smoke test cannot proceed until proxy routes are implemented
**Owner:** ORCH terminal (code implementation)
**Timeline:** Awaiting MSG-ORCH-002 completion

**INFRA Status:** Infrastructure prerequisites complete; code-level implementation required

---

## What INFRA Completed (MSG-ROOT-041)

1. ✅ **Orchestrator .env Configuration**
   - Updated JOINERY_BASE_URL to correct port (5002)
   - Verified IDENTITY_BASE_URL port (5003)
   - Confirmed CUTTING_BASE_URL (5004)

2. ✅ **Backend Service Verification**
   - All 4 C# backend services confirmed listening
   - Port mapping validated against actual service configuration
   - Network connectivity verified

3. ✅ **Frontend Launch**
   - npm preview server confirmed running on port 3001
   - Vite bundle serving correctly
   - Process verified active

4. ✅ **Knowledge Service Validation**
   - Confirmed listening on port 3456
   - 441 documents indexed (verified)
   - Voyage AI embeddings configured

---

## Remaining Work (Not INFRA)

### MSG-ORCH-002: Implement Orchestrator Proxy Routes

**Required:** Express.js proxy middleware or fetch-based route handlers

**Routes Needed (minimum):**
```javascript
// Joinery proxy
app.get('/api/orders/:id/material-req', proxyTo('http://127.0.0.1:5002'))
app.get('/api/orders/:id/hardware-list', proxyTo('http://127.0.0.1:5002'))

// Cutting proxy
app.get('/api/cutting/plans', proxyTo('http://127.0.0.1:5004'))
app.post('/api/cutting/plans', proxyTo('http://127.0.0.1:5004'))

// Identity proxy
app.all('/identity/*', proxyTo('http://127.0.0.1:5003'))
```

**Estimated Duration:** 30-60 minutes (ORCH)

---

## Next Steps

### For Conductor (Smoke Test)

**Wait for:** MSG-ORCH-002 completion (proxy routes)

Once ORCH implements proxy routes:
1. Restart Orchestrator (PM2)
2. Execute smoke test (MSG-CONDUCTOR-004)
3. Validate all 6 test categories

### For INFRA

No further action required for smoke test infrastructure.

**Current Status:** All INFRA prerequisites complete ✅

---

## Timeline Estimate

**Current State:**
- INFRA: ✅ Complete (infrastructure ready)
- ORCH: ⏳ In Progress (proxy route implementation, ~30-60 min)
- Smoke Test: ⏳ Blocked (waiting for proxy routes)

**Unblock Timeline:**
- ORCH finishes proxy routes → ORCH-002 DONE
- Conductor runs smoke test → 1-2 hours (test execution)
- GO/NO-GO decision → Final approval

**Full Smoke Test Completion:** After ORCH-002 + test execution (~2-3 hours from ORCH start)

---

## Documentation References

- **Original Request:** MSG-ROOT-041 (Doorstar smoke test setup)
- **INFRA Phase Completion:** MSG-INFRA-058-DONE (infrastructure fixes)
- **New Blocker:** MSG-ORCH-002 (proxy route implementation)

---

## Verification Commands

```bash
# All services listening
netstat -tlnp | grep -E "3000|3001|3456|5[0-4]{3}"

# Frontend response
curl -I http://localhost:3001

# Orchestrator port (no health endpoint yet)
timeout 2 bash -c "echo > /dev/tcp/127.0.0.1/3000" && echo "OK"

# Knowledge Service
timeout 2 bash -c "echo > /dev/tcp/127.0.0.1/3456" && echo "OK"
```

---

## Status Summary

| Component | INFRA Responsibility | Status | Blocker |
|-----------|----------------------|--------|---------|
| Port configuration | ✅ Yes | ✅ COMPLETE | None |
| Backend services | ✅ Yes | ✅ COMPLETE | None |
| Frontend launch | ✅ Yes | ✅ COMPLETE | None |
| Orchestrator .env | ✅ Yes | ✅ COMPLETE | None |
| Proxy route implementation | ❌ No (ORCH) | ❌ MISSING | MSG-ORCH-002 |

**INFRA Verdict: ✅ COMPLETE — All infrastructure prerequisites satisfied for smoke test**

**Next Blocker:** ORCH proxy route implementation (MSG-ORCH-002)

---

**INFRA Terminal: Doorstar smoke test infrastructure ready. Awaiting ORCH-002 code implementation.**

Timestamp: 2026-06-17 20:15 UTC
