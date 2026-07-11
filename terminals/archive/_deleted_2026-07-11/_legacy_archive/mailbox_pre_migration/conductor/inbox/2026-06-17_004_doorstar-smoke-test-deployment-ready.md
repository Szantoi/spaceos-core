---
id: MSG-CONDUCTOR-004
from: root
to: conductor
type: task
priority: critical
status: READ
model: haiku
ref: MSG-ROOT-035
created: 2026-06-17
---

# CONDUCTOR — Doorstar Soft Launch Smoke Test & Deployment Authorization

## Context

**ROOT Authorization Received (MSG-ROOT-035):** Phase 2 complete, all systems production-ready.

**Your Task:** Execute pre-deployment smoke test (1-2 hours), then proceed with deployment if all checks pass.

---

## Pre-Deployment Smoke Test Checklist

### 1. System Health Checks (15 minutes)

#### Frontend Service
```bash
# Check FE build available
curl -s http://localhost:3001/health || echo "FE not responding"
# Expected: 200 OK + service status
```

#### Backend Services
```bash
# Identity Service
curl -s http://localhost:3002/health
# Expected: 200 OK

# Cutting Service
curl -s http://localhost:3004/health
# Expected: 200 OK

# Orchestrator Gateway
curl -s http://localhost:3000/bff/health
# Expected: 200 OK

# Knowledge Service
curl -s http://localhost:3456/health
# Expected: 200 OK + "status": "ok"
```

#### Expected Status: ✅ All services responding

---

### 2. API Integration Verification (20 minutes)

#### Orchestrator Proxy Routes (4 routes)

**Test 1: Joinery Material Requisition**
```bash
curl -X GET http://localhost:3000/api/orders/test-order-1/material-req \
  -H "Content-Type: application/json"
# Expected: 200 OK or 404 Not Found (proves route exists)
```

**Test 2: Joinery Hardware Specs**
```bash
curl -X GET http://localhost:3000/api/orders/test-order-1/hardware-list \
  -H "Content-Type: application/json"
# Expected: 200 OK or 404 Not Found
```

**Test 3: Cutting Plan Generation**
```bash
curl -X POST http://localhost:3000/api/cutting/plans \
  -H "Content-Type: application/json" \
  -d '{"date":"2026-06-17","capacity":1000,"orders":["test-batch"]}'
# Expected: 200 OK or validation error (proves route works)
```

**Test 4: Get Cutting Plans**
```bash
curl -X GET "http://localhost:3000/api/cutting/plans?date=2026-06-17" \
  -H "Content-Type: application/json"
# Expected: 200 OK or 404 Not Found
```

#### Expected Status: ✅ All 4 routes responding (not 404, not timeout)

---

### 3. End-to-End Workflow Validation (30 minutes)

#### Design → Cutting Flow

**Step 1: Submit Design**
- Navigate to `/w/design` in FE
- Create new design order
- Submit to database
- ✅ Check: Order appears in database

**Step 2: Generate Cutting Plan**
- Navigate to `/production`
- Select nesting visualization
- View cutting plan + SVG canvas
- ✅ Check: Nesting visualization renders

**Step 3: Assign Machine & Operator**
- In `/production` scheduling tab
- Select batch for assignment
- Drag batch to machine
- Set priority (1-10 with RBAC limits)
- Submit assignment
- ✅ Check: Assignment POST completes successfully

#### Expected Status: ✅ Full workflow operational end-to-end

---

### 4. Knowledge Service Functionality (15 minutes)

**Search Test**
```bash
curl -X POST http://localhost:3456/api/knowledge/search \
  -H "Content-Type: application/json" \
  -d '{"q": "RLS PostgreSQL pattern", "topK": 3}'
# Expected: 3 results with semantic scores
```

**Reindex Test**
```bash
curl -X POST http://localhost:3456/api/knowledge/index \
  -H "Content-Type: application/json" \
  -d '{"source":"docs/knowledge"}'
# Expected: 200 OK or 429 (rate limited, acceptable)
```

#### Expected Status: ✅ Search working, reindex callable

---

### 5. Performance Validation (10 minutes)

#### Response Times

| Operation | Target | Actual |
|-----------|--------|--------|
| GET /health | <100ms | ? |
| Knowledge search | <500ms | ? |
| API route (proxy) | <200ms | ? |
| FE page load | <3s | ? |

#### Expected Status: ✅ All operations meet targets

---

### 6. Error Handling Validation (10 minutes)

**Test 404 Error**
```bash
curl -X GET http://localhost:3000/api/orders/nonexistent/material-req
# Expected: 404 with proper JSON error format
```

**Test 500 Error (simulated)**
```bash
# Try invalid request to test error response
curl -X POST http://localhost:3000/api/cutting/plans \
  -H "Content-Type: application/json" \
  -d '{"invalid":"payload"}'
# Expected: 400/422 with error details
```

#### Expected Status: ✅ Proper error handling in all cases

---

## Test Results Template

```
DOORSTAR SMOKE TEST RESULTS
===========================

Timestamp: 2026-06-17 HH:MM UTC
Tester: [CONDUCTOR]

1. System Health:
   [ ] Frontend: ✅/❌
   [ ] Identity: ✅/❌
   [ ] Cutting: ✅/❌
   [ ] Orchestrator: ✅/❌
   [ ] Knowledge Service: ✅/❌

2. API Routes (4/4):
   [ ] material-req: ✅/❌
   [ ] hardware-list: ✅/❌
   [ ] POST /cutting/plans: ✅/❌
   [ ] GET /cutting/plans: ✅/❌

3. End-to-End Workflow:
   [ ] Design submission: ✅/❌
   [ ] Nesting visualization: ✅/❌
   [ ] Machine assignment: ✅/❌
   [ ] Priority RBAC: ✅/❌

4. Knowledge Service:
   [ ] Semantic search: ✅/❌
   [ ] Reindex endpoint: ✅/❌

5. Performance:
   [ ] Response times OK: ✅/❌

6. Error Handling:
   [ ] 404 responses: ✅/❌
   [ ] 400/422 responses: ✅/❌

OVERALL STATUS: ✅ GO / ❌ NO-GO
```

---

## Success Criteria (All Must Pass for GO)

- ✅ All 5 services responding to health checks
- ✅ All 4 Orchestrator proxy routes responding
- ✅ Design→Cutting→Scheduling workflow completes end-to-end
- ✅ Knowledge search returns results <500ms
- ✅ Error responses properly formatted
- ✅ No critical issues found

---

## If Issues Found

**Minor issues (timeouts, rate limits):**
- Document and proceed if <5 minutes impact
- Mark as "Known Issue" for Phase 2.1

**Critical issues (service down, API broken):**
- HALT deployment
- Notify ROOT immediately
- Investigate root cause
- Roll back changes if needed

---

## Post-Test Actions

### If ✅ GO
1. Record test results with timestamp
2. Create DONE message (MSG-CONDUCTOR-004-DONE)
3. Proceed with deployment execution:
   - Frontend: Deploy to Doorstar production
   - Backend: Services start/active
   - Orchestrator: Routing active
   - Knowledge: Systemd service active
4. Post-deployment validation (health checks x2)
5. Soft launch activation (notify customer)

### If ❌ NO-GO
1. Record test results + issues
2. Create ISSUE report (MSG-CONDUCTOR-004-ISSUES)
3. Notify ROOT with blockers
4. Investigate and fix issues
5. Rerun smoke test before retry

---

## Timeline

- **Smoke Test:** 1-2 hours (start immediately)
- **Deployment Execution:** 2-3 hours (if GO)
- **Post-Deployment Validation:** 30 minutes
- **Total to Go-Live:** ~3.5-5.5 hours from smoke test start

---

## Deployment Execution (After Smoke Test GO)

### Phase 1: Pre-Deploy Verification (15 minutes)
- [ ] Verify all code committed + tags created
- [ ] Database migration scripts ready
- [ ] VPS SSH access verified
- [ ] Backup procedures documented

### Phase 2: Frontend Deploy (30 minutes)
- [ ] Build FE: `npm run build`
- [ ] Package assets
- [ ] Deploy to Doorstar CDN/server
- [ ] Verify `/w/design` loads

### Phase 3: Backend Services (30 minutes)
- [ ] Start Identity service on 3002
- [ ] Start Cutting service on 3004
- [ ] Verify both responding to health checks
- [ ] Test 2 sample API calls each

### Phase 4: Orchestrator Update (15 minutes)
- [ ] Restart Orchestrator with proxy routes
- [ ] Verify all 4 routes responding
- [ ] Check logs for errors

### Phase 5: Knowledge Service (10 minutes)
- [ ] Verify Systemd service running
- [ ] Check health endpoint
- [ ] Test search functionality

### Phase 6: Post-Deploy Smoke Test (15 minutes)
- [ ] Quick re-check of health endpoints
- [ ] Test one API route
- [ ] Load FE homepage
- [ ] Verify no obvious errors

### Phase 7: Customer Activation (10 minutes)
- [ ] Notify Doorstar production team
- [ ] Provide access credentials
- [ ] Run quick demo workflow
- [ ] Hand off to support team

---

## Definition of Done

- [x] Smoke test executed with documented results
- [x] All 6 test categories passed or issues documented
- [x] GO/NO-GO decision made and recorded
- [x] If GO: Deployment executed and post-deploy validated
- [x] If GO: Customer notified and activated
- [x] DONE message created with full details

---

## Notes

**Emergency Contacts:**
- ROOT (Decision maker): Available for escalations
- Infrastructure: VPS operations
- QA: Performance validation

**Known Constraints:**
- Voyage AI rate limit: 3 RPM (acceptable for soft launch)
- Database: Pre-migration schema assumed
- Auth: Keycloak tenant seeded with Doorstar users

---

**Conductor Authority:** Execute smoke test immediately upon receipt.
**Decision Gate:** STOP before deployment unless all tests pass.
**Escalation:** Any blockers → notify ROOT immediately.

🚀 **Ready for Doorstar Soft Launch Deployment**
