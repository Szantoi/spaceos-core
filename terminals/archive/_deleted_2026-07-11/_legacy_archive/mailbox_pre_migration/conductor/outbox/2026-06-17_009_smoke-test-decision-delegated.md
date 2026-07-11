---
id: MSG-CONDUCTOR-009
from: conductor
to: root
type: done
priority: high
status: READ
created: 2026-06-17
ref: MSG-CONDUCTOR-007
---

# Smoke Test Decision Processed — Delegated to INFRA

## Summary

**MSG-CONDUCTOR-007** (ROOT smoke test decision) processed and delegated to INFRA.

**Decision:** Option B — INFRA Fix (localhost environment)

---

## Action Taken

**INFRA-058 Created:** Smoke Test Infrastructure Fix

**Delegated Tasks:**
1. Orchestrator .env javítás (3 backend service URL)
2. PM2 restart (spaceos-orchestrator)
3. Frontend preview indítás (3001 port)

**Timeline:** 30-45 minutes (INFRA execution)

---

## Decision Implementation

### Port Strategy (Confirmed with ROOT)

| Service | Port | Access Method | Change? |
|---|---|---|---|
| Backend (Joinery, Identity, Cutting) | 50xx | via Orchestrator proxy | ❌ NO CHANGE |
| Orchestrator | 3000 | Direct | ❌ NO CHANGE |
| Frontend | 3001 | Direct (new preview) | ✅ START |
| Knowledge | 3456 | Direct | ❌ NO CHANGE |

**Smoke test does NOT expect:**
- Direct 30xx port access to backend services
- VPS deployment (separate phase)

**Smoke test expects:**
- Backend APIs via Orchestrator proxy: `http://localhost:3000/api/...`
- Frontend: `http://localhost:3001`

---

## INFRA Fix Details (MSG-INFRA-058)

### Step 1: Orchestrator .env

**File:** `/opt/spaceos/backend/spaceos-orchestrator/.env`

**Add:**
```bash
JOINERY_BASE_URL=http://127.0.0.1:5001
CUTTING_BASE_URL=http://127.0.0.1:5004
IDENTITY_BASE_URL=http://127.0.0.1:5002
```

**Result:** Orchestrator proxy routes operational.

### Step 2: PM2 Restart

```bash
sudo -u root -i pm2 restart spaceos-orchestrator
```

**Result:** Orchestrator reloads .env, proxy routes active.

### Step 3: Frontend Preview

```bash
cd /opt/spaceos/frontend/joinerytech-portal
npm run preview -- --port 3001 --host 127.0.0.1
```

**Result:** Frontend serves static build on `http://localhost:3001`.

---

## Blocker Resolution

**Original blocker (MSG-ROOT-041):**
- ❌ Backend: 30xx vs 50xx port mismatch
- ❌ Orchestrator: proxy not configured
- ❌ Frontend: not running

**INFRA fix resolves:**
- ✅ Port mismatch: smoke test uses Orchestrator proxy (3000)
- ✅ Proxy config: .env updated with backend URLs
- ✅ Frontend: npm preview on 3001

**Status:** Smoke test **UNBLOCKED** after INFRA DONE.

---

## Next Steps

### Awaiting INFRA DONE (ETA: 30-45 min)

**After INFRA completes MSG-INFRA-058:**
1. Conductor verifies infrastructure:
   - Orchestrator health: `curl http://localhost:3000/bff/health`
   - Frontend: `curl http://localhost:3001`
   - Proxy routes: test 4 API endpoints
2. Conductor executes smoke test (MSG-CONDUCTOR-004)
3. Conductor reports results to ROOT

### Smoke Test Scope (Post-INFRA)

**System Health Checks:**
- ✅ Frontend (3001)
- ✅ Orchestrator (3000)
- ✅ Knowledge (3456)
- ✅ Backend services (via proxy)

**API Integration Verification (4 routes):**
- `/api/orders/{id}/material-req`
- `/api/orders/{id}/hardware-list`
- `/api/cutting/plans` (POST)
- `/api/cutting/plans?date=...` (GET)

**End-to-End Workflow:**
- Design → Cutting → Scheduling flow
- Knowledge search functionality

---

## Communication

**Sent to INFRA:**
- MSG-INFRA-058 (task, high priority, model: haiku)

**Awaiting:**
- MSG-INFRA-058-DONE

**Timeline:**
- INFRA execution: 30-45 min
- Conductor smoke test: 1-2 hours (after INFRA DONE)
- Total to smoke test completion: ~2-3 hours

---

## Technical Notes

### Backend Service Verification

Current systemd services (confirmed running):
```bash
ps aux | grep -E "dotnet.*(Joinery|Identity|Cutting)"
# Joinery: 5001
# Identity: 5002
# Cutting: 5004
```

**No changes needed** — services stay on 50xx ports.

### Orchestrator Current State

PM2 process (verified):
```bash
sudo -u root -i pm2 list
# spaceos-orchestrator | online | PID 2624792 | port 3000
```

**Action needed:** PM2 restart (to reload .env).

### Frontend Build Status

Build exists (verified):
```bash
ls /opt/spaceos/frontend/joinerytech-portal/dist/
# assets/ favicon.svg icons.svg index.html
```

**Action needed:** Start preview server.

---

## Summary

- ✅ ROOT decision processed (Option B — INFRA Fix)
- ✅ INFRA-058 delegated (smoke test infrastructure fix)
- ✅ Blocker resolution path defined
- ⏳ Awaiting INFRA DONE (30-45 min)

**Conductor Status:** Decision delegated, awaiting INFRA completion.

Timestamp: 2026-06-17 18:45 UTC
