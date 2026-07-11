---
created: 2026-06-24
selected_by: architect
status: ready_for_dispatch
epic: EPIC-DATAHAVEN-UI
domain_focus: all
phase: 3
estimate_days: 3-5
depends_on: [focus-area-panel, flow-editor-phase1]
parallel_with: []
ref: Datahaven_UI_Focus_Flow_Editor_Architecture_v1.md
---

# SpaceOS Planning — Datahaven UI Polish & Integration

## Epic: EPIC-DATAHAVEN-UI
## Phase: 3 of 3 (Polish & Integration)

---

## Executive Summary

A **Polish & Integration** fázis lezárja a Datahaven UI két új komponensének fejlesztését:
- Focus Area Panel (Phase 1)
- Flow/Workflow Editor (Phase 2)

**Scope:**
- Performance optimization (bundle size, load time)
- Security audit (XSS, CSRF, rate limiting verification)
- Cross-browser testing (Chrome, Firefox, Safari, Edge)
- Documentation (API docs, user guide)
- Final integration testing (E2E scenarios)

---

## Performance Optimization (Backend Terminal)

### PERF-001: API Response Time Optimization

**Subtasks:**
- [ ] Profile GET /api/planning/domain-focus response time
- [ ] Profile GET /api/graph/mermaid/epic/EPICS response time
- [ ] Profile PUT /api/graph/epics/:id response time
- [ ] Add cache layer for frequently accessed data (EPICS.yaml, domain-focus.md)
- [ ] Set cache TTL (5 minutes for planning data, 10 minutes for graph data)
- [ ] Add cache invalidation on PUT operations

**Performance Targets:**
- GET endpoints: <100ms (p95)
- PUT endpoints: <300ms (p95)
- Cache hit ratio: >80%

**Estimate:** 3-4 hours

---

### PERF-002: EPICS.yaml Incremental Parsing

**Subtasks:**
- [ ] Currently: full YAML parse on every GET
- [ ] Optimize: parse once, cache parsed object
- [ ] Invalidate cache on file modification (fs.watch or manual invalidation)

**Estimate:** 2-3 hours

---

## Performance Optimization (Frontend Terminal)

### PERF-003: Frontend Bundle Size Reduction

**Subtasks:**
- [ ] Measure current bundle size (marked.js + mermaid.js + app code)
- [ ] Consider lazy loading Mermaid.js (only load on Workflow tab open)
- [ ] Minify custom JS files
- [ ] Use marked.min.js instead of marked.js
- [ ] Verify CDN versions are latest (better caching)

**Bundle Size Targets:**
- Initial load: <500KB (compressed)
- Mermaid.js: lazy loaded only when needed

**Estimate:** 2-3 hours

---

### PERF-004: Planning Page Load Time

**Subtasks:**
- [ ] Measure current page load time (Lighthouse audit)
- [ ] Target: <1.5 seconds for initial render
- [ ] Defer non-critical JS (analytics, etc.)
- [ ] Optimize CSS delivery (inline critical CSS)
- [ ] Add loading skeletons for async content

**Estimate:** 2-3 hours

---

## Security Audit (Backend Terminal)

### SEC-001: XSS Prevention Verification

**Subtasks:**
- [ ] Test markdown sanitization in PUT /api/planning/domain-focus
- [ ] Inject test payloads: `<script>alert(1)</script>`, `<img src=x onerror=alert(1)>`
- [ ] Verify sanitization removes dangerous tags
- [ ] Test stored XSS (save malicious criteria → reload page)
- [ ] Test reflected XSS (malicious query params)

**Test Cases:**
```
Payload 1: <script>alert("XSS")</script>
Payload 2: <img src=x onerror=alert(1)>
Payload 3: <svg onload=alert(1)>
Payload 4: <iframe src="javascript:alert(1)">
```

**Estimate:** 2 hours

---

### SEC-002: Rate Limiting Verification

**Subtasks:**
- [ ] Test PUT /api/planning/domain-focus rate limit (10 writes/min)
- [ ] Send 11 requests in <1 minute → verify 11th returns 429
- [ ] Test rate limit reset after 1 minute
- [ ] Test rate limit applies per IP (not per user)

**Estimate:** 1 hour

---

### SEC-003: Authentication Verification

**Subtasks:**
- [ ] Test GET /api/planning/domain-focus without auth token → 401
- [ ] Test PUT /api/graph/epics/:id without auth token → 401
- [ ] Test with invalid token → 401
- [ ] Test with expired token (if applicable) → 401

**Estimate:** 1 hour

---

### SEC-004: CSRF Protection

**Subtasks:**
- [ ] Verify CSRF token required for PUT operations
- [ ] Test PUT without CSRF token → 403
- [ ] Test PUT with invalid CSRF token → 403
- [ ] (OR: CORS policy restricts origin to https://datahaven.joinerytech.hu)

**Estimate:** 1 hour

---

## Cross-Browser Testing (Frontend Terminal)

### TEST-001: Browser Compatibility Matrix

**Target Browsers:**
- Chrome 120+ (desktop + mobile)
- Firefox 120+ (desktop)
- Safari 17+ (desktop + mobile)
- Edge 120+ (desktop)

**Test Cases:**
| Feature | Chrome | Firefox | Safari | Edge |
|---------|--------|---------|--------|------|
| Focus Area Panel loads | ✅ | ? | ? | ? |
| Domain dropdown works | ✅ | ? | ? | ? |
| Criteria edit/save works | ✅ | ? | ? | ? |
| Workflow tab loads | ✅ | ? | ? | ? |
| Mermaid graph renders | ✅ | ? | ? | ? |
| Epic details panel works | ✅ | ? | ? | ? |
| Status change works | ✅ | ? | ? | ? |

**Estimate:** 3-4 hours (1 hour per browser)

---

### TEST-002: Mobile Responsiveness

**Subtasks:**
- [ ] Test Focus Area Panel on mobile (320px, 375px, 768px widths)
- [ ] Verify Workflow editor shows "Desktop required" message on mobile
- [ ] Test touch interactions (dropdown, buttons)
- [ ] Test keyboard navigation (tab, enter, escape)

**Estimate:** 2 hours

---

## Documentation (Librarian Terminal)

### DOC-001: API Documentation

**File:** `docs/knowledge/api/DATAHAVEN_PLANNING_API.md`

**Subtasks:**
- [ ] Document GET /api/planning/domain-focus
- [ ] Document PUT /api/planning/domain-focus
- [ ] Document PUT /api/graph/epics/:id
- [ ] Include request/response examples
- [ ] Include error codes and messages
- [ ] Include rate limiting rules
- [ ] Include authentication requirements

**Estimate:** 2-3 hours

---

### DOC-002: User Guide

**File:** `docs/knowledge/datahaven/PLANNING_UI_USER_GUIDE.md`

**Subtasks:**
- [ ] How to use Focus Area Panel (screenshots)
- [ ] How to change planning domain
- [ ] How to edit domain criteria
- [ ] How to use Flow/Workflow Editor
- [ ] How to view epic dependencies
- [ ] How to change epic status
- [ ] How to add/remove dependencies
- [ ] Common error messages and solutions

**Estimate:** 2-3 hours

---

### DOC-003: Architecture Decision Record

**File:** `docs/knowledge/architecture/ADR-048-Datahaven-UI-Planning-Components.md`

**Subtasks:**
- [ ] Document decision context (why these components)
- [ ] Document placement decisions (Planning page)
- [ ] Document technology choices (Mermaid.js)
- [ ] Document trade-offs (polling vs SSE, YAML vs DB)
- [ ] Document alternatives considered (React Flow, D3.js)

**Estimate:** 1-2 hours

---

## Integration Testing (E2E)

### E2E-001: Focus Area Panel E2E Flow

**Scenario 1: Change Domain**
- [ ] Load Planning page
- [ ] Current domain shown in dropdown
- [ ] Select new domain → PUT API called
- [ ] Success toast shown
- [ ] Criteria updated on page
- [ ] Reload page → domain persists

**Scenario 2: Edit Criteria**
- [ ] Click Edit button → textarea shown
- [ ] Modify criteria markdown
- [ ] Click Save → PUT API called
- [ ] Success toast shown
- [ ] Display mode shown
- [ ] Reload page → criteria persists

**Estimate:** 1 hour

---

### E2E-002: Flow/Workflow Editor E2E Flow

**Scenario 1: View Graph**
- [ ] Navigate to Workflow tab
- [ ] Graph loads and renders
- [ ] Nodes colored by status (pending/active/done/blocked)
- [ ] Dependencies shown as arrows

**Scenario 2: Change Epic Status**
- [ ] Click epic node → details panel opens
- [ ] Change status dropdown → PUT API called
- [ ] Graph re-renders with new color
- [ ] Details panel updates

**Scenario 3: Add Dependency**
- [ ] Click epic node → details panel opens
- [ ] Click [+ Add Dependency] → modal opens
- [ ] Select dependency epic → PUT API called
- [ ] Graph re-renders with new arrow
- [ ] Details panel shows new dependency

**Scenario 4: Attempt Cycle**
- [ ] Select EPIC-A → Add EPIC-B as dependency
- [ ] Select EPIC-B → Add EPIC-A as dependency
- [ ] Error message shown: "Cycle detected: A → B → A"
- [ ] Dependency not added

**Estimate:** 2-3 hours

---

### E2E-003: Cross-Feature Integration

**Scenario: Domain Change Affects Planning**
- [ ] Change domain to "manufacturing"
- [ ] Verify plan-scan.sh reads new domain from domain-focus.md
- [ ] Verify next consensus uses manufacturing criteria

**Estimate:** 1 hour

---

## Monitoring & Metrics (Backend Terminal)

### MON-001: API Usage Metrics

**Subtasks:**
- [ ] Add logging for API endpoints (request count, response time, errors)
- [ ] Log to `/opt/spaceos/logs/datahaven/api-metrics.log`
- [ ] Include: timestamp, endpoint, method, status, response_time_ms

**Log Format:**
```
2026-06-24T14:32:01Z GET /api/planning/domain-focus 200 45ms
2026-06-24T14:32:15Z PUT /api/planning/domain-focus 200 123ms
2026-06-24T14:32:45Z PUT /api/graph/epics/EPIC-CUTTING-Q3 400 12ms (validation_error)
```

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

## Final Checklist

### Pre-Deployment Checklist

- [ ] All Phase 1 tasks (Focus Area Panel) completed ✅
- [ ] All Phase 2 tasks (Flow Editor) completed ✅
- [ ] Performance targets met (page load <1.5s, API <300ms)
- [ ] Security audit passed (XSS, rate limiting, auth)
- [ ] Cross-browser testing passed (Chrome, Firefox, Safari, Edge)
- [ ] Mobile responsiveness verified
- [ ] API documentation complete
- [ ] User guide complete
- [ ] E2E scenarios passed
- [ ] Monitoring in place

---

## Deployment Plan

### Deploy-001: Staging Deployment

**Subtasks:**
- [ ] Deploy to staging environment (https://staging.datahaven.joinerytech.hu)
- [ ] Smoke test all features
- [ ] Run automated E2E tests
- [ ] Verify logs and metrics

**Estimate:** 1 hour

---

### Deploy-002: Production Deployment

**Subtasks:**
- [ ] Deploy to production (https://datahaven.joinerytech.hu)
- [ ] Monitor for 1 hour post-deployment
- [ ] Verify no errors in logs
- [ ] Send announcement to terminals (via Telegram)

**Estimate:** 1 hour

---

### Deploy-003: Rollback Plan

**If deployment fails:**
- [ ] Git revert last commit
- [ ] Restart knowledge-service
- [ ] Verify rollback successful
- [ ] Notify team

**Estimate:** 30 minutes

---

## Success Criteria

- ✅ Page load time <1.5 seconds (p95)
- ✅ API response time <300ms (p95)
- ✅ Zero XSS vulnerabilities
- ✅ Zero CSRF vulnerabilities
- ✅ 100% browser compatibility (Chrome, Firefox, Safari, Edge)
- ✅ Mobile responsiveness verified
- ✅ API documentation complete
- ✅ User guide complete
- ✅ E2E tests passing

---

## Dependencies

- **Phase 1 (Focus Area Panel):** Must be DONE
- **Phase 2 (Flow Editor Phase 1):** Must be DONE
- **Librarian terminal:** For documentation tasks

---

## Estimated Timeline

| Task Category | Owner | Estimate |
|---------------|-------|----------|
| Performance Optimization | Backend | 1 day |
| Performance Optimization | Frontend | 1 day |
| Security Audit | Backend | 0.5 day |
| Cross-Browser Testing | Frontend | 0.5 day |
| Documentation | Librarian | 1 day |
| Integration Testing | Backend + Frontend | 0.5 day |
| Monitoring | Backend | 0.5 day |
| Deployment | Backend | 0.5 day |
| **Total** | | **3-5 days** |

---

## Risk Mitigation

- **Browser compatibility issues:** Test early and often, have fallbacks for CSS features
- **Performance regressions:** Baseline metrics before optimization, compare after
- **Deployment failures:** Have rollback plan ready, deploy during low-traffic window

---

**END OF PLAN**
