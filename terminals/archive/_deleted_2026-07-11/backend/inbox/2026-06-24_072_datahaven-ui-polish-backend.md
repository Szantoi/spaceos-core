---
id: MSG-BACKEND-072
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: 2026-06-24_consensus_ui-polish-integration.md
epic: EPIC-DATAHAVEN-UI
phase: 3
created: 2026-06-24
content_hash: 0c1816fafe0b7a74098e9f78063232fd870153b3e1e2e75702f8927ff7c60966
---

# Datahaven UI Polish & Integration — Backend Tasks

**Epic:** EPIC-DATAHAVEN-UI Phase 3 (Polish & Integration)
**Priority:** HIGH
**Type:** Performance, Security, Testing, Monitoring
**Estimate:** 3-4 days

---

## Context

Phase 1 (Focus Area Panel) és Phase 2 (Flow Editor) implementálva és DONE.
Most következik a Phase 3: Performance optimization, Security audit, Integration testing, Monitoring, Deployment.

**Dependencies (✅ DONE):**
- Phase 1: Focus Area Panel UI — Frontend DONE
- Phase 2: Flow Editor Phase 1 — Frontend DONE

**Scope:**
- Performance optimization (API response times, caching)
- Security audit (XSS, CSRF, rate limiting, auth)
- E2E integration testing
- Monitoring & metrics
- Deployment (staging + production)

---

## Performance Optimization

### PERF-001: API Response Time Optimization

**Target:** GET endpoints <100ms (p95), PUT endpoints <300ms (p95), Cache hit ratio >80%

**Subtasks:**
- [ ] Profile GET `/api/planning/domain-focus` response time
- [ ] Profile GET `/api/graph/mermaid/epic/EPICS` response time
- [ ] Profile PUT `/api/graph/epics/:id` response time
- [ ] Add cache layer for frequently accessed data:
  - EPICS.yaml → cache for 10 minutes
  - domain-focus.md → cache for 5 minutes
- [ ] Implement cache invalidation on PUT operations
- [ ] Test cache hit ratio (target >80%)

**Implementation:**
- Use in-memory cache (Node.js `Map` or `node-cache` library)
- Cache key: endpoint path + query params
- Cache TTL: 5-10 minutes
- Invalidate on file modification or PUT

**Estimate:** 3-4 hours

---

### PERF-002: EPICS.yaml Incremental Parsing

**Current:** Full YAML parse on every GET request
**Target:** Parse once, cache parsed object, invalidate on file change

**Subtasks:**
- [ ] Add `fs.watch` or manual cache invalidation
- [ ] Store parsed YAML object in cache
- [ ] Benchmark before/after (expect 50-80% speedup)

**Estimate:** 2-3 hours

---

## Security Audit

### SEC-001: XSS Prevention Verification

**Test payloads:**
```
<script>alert("XSS")</script>
<img src=x onerror=alert(1)>
<svg onload=alert(1)>
<iframe src="javascript:alert(1)">
```

**Subtasks:**
- [ ] Test markdown sanitization in PUT `/api/planning/domain-focus`
- [ ] Inject test payloads → verify sanitization removes dangerous tags
- [ ] Test stored XSS (save malicious criteria → reload page)
- [ ] Test reflected XSS (malicious query params)
- [ ] Verify DOMPurify or equivalent is used

**Estimate:** 2 hours

---

### SEC-002: Rate Limiting Verification

**Target:** PUT `/api/planning/domain-focus` → max 10 writes/min per IP

**Subtasks:**
- [ ] Send 11 PUT requests in <1 minute
- [ ] Verify 11th request returns 429 (Too Many Requests)
- [ ] Wait 1 minute → verify rate limit reset
- [ ] Verify rate limit applies per IP (not per user)

**Estimate:** 1 hour

---

### SEC-003: Authentication Verification

**Subtasks:**
- [ ] Test GET `/api/planning/domain-focus` without auth token → 401
- [ ] Test PUT `/api/graph/epics/:id` without auth token → 401
- [ ] Test with invalid token → 401
- [ ] Test with expired token (if applicable) → 401

**Estimate:** 1 hour

---

### SEC-004: CSRF Protection

**Subtasks:**
- [ ] Verify CSRF token required for PUT operations OR
- [ ] Verify CORS policy restricts origin to `https://datahaven.joinerytech.hu`
- [ ] Test PUT without CSRF token → 403 (if CSRF used)
- [ ] Test PUT with invalid CSRF token → 403 (if CSRF used)

**Estimate:** 1 hour

---

## Integration Testing (E2E)

### E2E-001: Focus Area Panel E2E Flow

**Scenario 1: Change Domain**
- [ ] Load Planning page
- [ ] Verify current domain shown in dropdown
- [ ] Select new domain → PUT API called
- [ ] Verify success toast shown
- [ ] Verify criteria updated on page
- [ ] Reload page → verify domain persists

**Scenario 2: Edit Criteria**
- [ ] Click Edit button → textarea shown
- [ ] Modify criteria markdown
- [ ] Click Save → PUT API called
- [ ] Verify success toast shown
- [ ] Verify display mode shown
- [ ] Reload page → verify criteria persists

**Estimate:** 1 hour

---

### E2E-002: Flow/Workflow Editor E2E Flow

**Scenario 1: View Graph**
- [ ] Navigate to Workflow tab
- [ ] Graph loads and renders
- [ ] Verify nodes colored by status (pending/active/done/blocked)
- [ ] Verify dependencies shown as arrows

**Scenario 2: Change Epic Status**
- [ ] Click epic node → details panel opens
- [ ] Change status dropdown → PUT API called
- [ ] Verify graph re-renders with new color
- [ ] Verify details panel updates

**Scenario 3: Add Dependency**
- [ ] Click epic node → details panel opens
- [ ] Click [+ Add Dependency] → modal opens
- [ ] Select dependency epic → PUT API called
- [ ] Verify graph re-renders with new arrow
- [ ] Verify details panel shows new dependency

**Scenario 4: Cycle Detection**
- [ ] Create EPIC-A → Add EPIC-B as dependency
- [ ] Create EPIC-B → Add EPIC-A as dependency
- [ ] Verify error message: "Cycle detected: A → B → A"
- [ ] Verify dependency not added

**Estimate:** 2-3 hours

---

### E2E-003: Cross-Feature Integration

**Scenario: Domain Change Affects Planning**
- [ ] Change domain to "manufacturing"
- [ ] Verify `plan-scan.sh` reads new domain from `domain-focus.md`
- [ ] Verify next consensus uses manufacturing criteria

**Estimate:** 1 hour

---

## Monitoring & Metrics

### MON-001: API Usage Metrics

**Log format:**
```
2026-06-24T14:32:01Z GET /api/planning/domain-focus 200 45ms
2026-06-24T14:32:15Z PUT /api/planning/domain-focus 200 123ms
2026-06-24T14:32:45Z PUT /api/graph/epics/EPIC-CUTTING-Q3 400 12ms (validation_error)
```

**Subtasks:**
- [ ] Add logging for API endpoints (request count, response time, errors)
- [ ] Log to `/opt/spaceos/logs/datahaven/api-metrics.log`
- [ ] Include: timestamp, endpoint, method, status, response_time_ms, error (if any)

**Estimate:** 2 hours

---

### MON-002: Error Tracking

**Subtasks:**
- [ ] Add structured error logging
- [ ] Log validation errors (domain whitelist, status transitions, cycles)
- [ ] Log rate limit hits
- [ ] Log authentication failures

**Estimate:** 1 hour

---

## Deployment

### Deploy-001: Staging Deployment

**Subtasks:**
- [ ] Deploy to staging environment (https://staging.datahaven.joinerytech.hu)
- [ ] Smoke test all features:
  - Focus Area Panel (domain change, criteria edit)
  - Workflow Editor (view graph, change status, add dependency)
- [ ] Run automated E2E tests
- [ ] Verify logs and metrics

**Estimate:** 1 hour

---

### Deploy-002: Production Deployment

**Subtasks:**
- [ ] Deploy to production (https://datahaven.joinerytech.hu)
- [ ] Monitor for 1 hour post-deployment
- [ ] Verify no errors in logs (`/opt/spaceos/logs/datahaven/`)
- [ ] Send announcement to terminals (via Telegram or Root outbox)

**Estimate:** 1 hour

---

### Deploy-003: Rollback Plan (ready, not executed)

**Document rollback steps:**
- [ ] Git revert last commit
- [ ] Restart knowledge-service
- [ ] Verify rollback successful
- [ ] Notify team

**Estimate:** 30 minutes (preparation only)

---

## Definition of Done

- [ ] All PERF tasks complete → API response times meet targets
- [ ] All SEC tasks complete → no XSS/CSRF vulnerabilities, auth verified
- [ ] All E2E scenarios passing
- [ ] Monitoring in place (API metrics + error tracking)
- [ ] Staging deployment successful
- [ ] Production deployment successful
- [ ] No errors in logs for 1 hour post-deployment

---

## Files to Modify/Create

**Modified:**
- `spaceos-nexus/knowledge-service/src/server.ts` (caching, logging)
- `spaceos-nexus/knowledge-service/src/api/planningRoutes.ts` (rate limiting, auth)
- `spaceos-nexus/knowledge-service/src/api/graphRoutes.ts` (caching, logging)

**Created:**
- `/opt/spaceos/logs/datahaven/api-metrics.log` (metrics log)
- Test scripts for E2E scenarios (optional)

---

## Reference

**Consensus:** `docs/planning/queue/2026-06-24_consensus_ui-polish-integration.md`
**Architecture:** `docs/tasks/new/Datahaven_UI_Focus_Flow_Editor_Architecture_v1.md`

---

**Kezdd el a PERF-001 feladattal!** Először profilozz, majd add hozzá a cache layer-t.
