---
id: MSG-ARCHITECT-043
from: architect
to: conductor
type: done
priority: high
status: READ
created: 2026-07-02
ref: MSG-ARCHITECT-040
epic_id: EPIC-JT-CRM
content_hash: db9d4689291501a9f468f08a9f2080cd478b292a7db0e9b07e48b7c5bcda7793
---

# DONE: JoineryTech Backend-Frontend Integration Gap Analysis

## Summary

✅ **ADR-058 completed** — All 8 CRITICAL integration gaps addressed with architectural decisions, decision matrices, and migration path.

**Deliverable:** `/opt/spaceos/docs/architecture/decisions/ADR-058-joinerytech-integration-architecture.md`

---

## Work Completed

### 1. 8 Integration Gaps Analyzed

Each gap evaluated with **3 alternatives** and decision matrix:

| Gap | Decision | Rationale |
|-----|----------|-----------|
| **State Management** (CRITICAL) | TanStack Query | Server authority, minimal boilerplate, phased migration |
| **Authentication** (CRITICAL) | HttpOnly Cookie JWT | XSS protection, OWASP compliance |
| **Real-Time Sync** (HIGH) | HTTP Polling (Phase 1) → WebSocket (Phase 2) | Walking skeleton first, defer complexity |
| **API Contract** (HIGH) | Contract-First OpenAPI | Early validation, parallel dev, code-gen |
| **Error Handling** (HIGH) | Per-Query Handlers + Global Interceptor | Flexible, testable, UX customization |
| **Performance** (HIGH) | Vite + ESBuild | 50% build size reduction (4.2 MB → 2.2 MB) |
| **Validation** (MEDIUM) | Generated Validators from OpenAPI | Auto-sync, single source of truth |
| **Testing** (MEDIUM) | Vitest + Playwright | Fast tests, modern tooling, cross-browser E2E |

### 2. Migration Path Defined

**3-Phase Strategy (20 weeks):**

- **Phase 1 (Weeks 1-4):** Auth + Catalog API, Vite migration, TanStack Query setup
- **Phase 2 (Weeks 5-12):** Transaction state migration (quotes → orders → invoices), Real-time sync
- **Phase 3 (Weeks 13-20):** All 8 modules, remove localStorage, complete cutover

### 3. Phase 1 Implementation Priorities Ranked

**Critical Path (P0):**
1. OpenAPI Spec Writing (3-4 days) — Contract-first design
2. Auth API Endpoints (1 week) — Backend
3. Vite + TanStack Query Setup (1 week) — Frontend
4. JWT HttpOnly Cookie Handling (1 week) — Backend + Frontend

### 4. Risk Assessment Completed

**Critical Risks Identified:**
- State management mismatch → Mitigation: Early spike (Week 2)
- API contract undefined → Mitigation: OpenAPI spec Week 0
- JWT token expiration → Mitigation: Test token refresh edge cases (Week 3)

### 5. Success Metrics Defined

**Phase 1 Targets:**
- API Response Time: <200ms (p95)
- Frontend Build Size: 2.2 MB (from 4.2 MB)
- Test Coverage: ≥80% (auth module)
- Auth Flow Success Rate: 99.5%

---

## Key Architectural Decisions

### Decision 1: TanStack Query for State Management

**Trade-off:**
- ✅ **Gain:** Server authority, optimistic UI, auto-caching
- ❌ **Lose:** Offline capability (acceptable for B2B ERP)
- 📊 **Cost:** $12k-15k (4 weeks × 1 FTE)

**Migration Path:** localStorage → TanStack Query (Phase 1-3, 8 weeks)

### Decision 2: HttpOnly Cookie JWT

**Trade-off:**
- ✅ **Gain:** XSS protection, OWASP compliance
- ❌ **Lose:** Token not accessible for custom flows
- 📊 **Cost:** $6k (2 weeks × 1 FTE)

**Security:** CSRF protection via double-submit cookie pattern

### Decision 3: Contract-First OpenAPI

**Trade-off:**
- ✅ **Gain:** Early validation, parallel dev, code-gen consistency
- ❌ **Lose:** 3-4 days upfront design time
- 📊 **Net Benefit:** $11k-16k saved (avoids $15k-20k rework)

**Code-Gen:** Orval (Frontend) + NSwag (Orchestrator) from ADR-050

---

## Epic Impact

This ADR unblocks **8 JoineryTech EPICS** (all currently PENDING):

- EPIC-JT-CRM (Customer Relationship Management)
- EPIC-JT-CTRL (Kontrolling)
- EPIC-JT-HR (Human Resources)
- EPIC-JT-MAINT (Maintenance)
- EPIC-JT-QA (Quality Assurance)
- EPIC-JT-EHS (Environment, Health, Safety)
- EPIC-JT-DMS (Document Management)
- EPIC-JT-AI (Intelligent Automation)

**Target:** Q3-Q4 2026 production readiness.

---

## Acceptance Criteria (Original Task)

- [x] ADR-058 document created with all 8 gaps addressed
- [x] Decision matrix with 3+ alternatives per gap
- [x] Migration path defined (localStorage → REST)
- [x] Phase 1 implementation priorities ranked
- [x] Risk assessment completed

---

## Files Changed

**New:**
- `/opt/spaceos/docs/architecture/decisions/ADR-058-joinerytech-integration-architecture.md` (600+ lines)

---

## Next Steps (Recommended)

1. **Root Review:** Approve ADR-058 (target: same day)
2. **Backend Review:** Tech feasibility validation (target: 2 days)
3. **Frontend Review:** Implementation plan review (target: 2 days)
4. **Week 0 Start:** OpenAPI spec writing (Architect + Backend + Frontend, 3-4 days)
5. **Phase 1 Start:** Week 1 (Backend + Frontend parallel development)

---

## Notes

**Walking Skeleton Principle:** All decisions prioritize E2E pipeline first, complexity later:
- Phase 1 = HTTP polling (simple), Phase 2 = WebSocket (complex)
- Phase 1 = Auth + Catalog (infrastructure), Phase 2 = Transactions (business logic)
- Contract-First OpenAPI = upfront design, avoids late rework

**5 Golden Rules Alignment:**
- ✅ **Data → Rules → Geometry:** Backend enforces FSM, Frontend displays state
- ✅ **Modular Monolith:** API contract isolates modules
- ✅ **Immutability:** Event-driven architecture (Phase 2-3)
- ✅ **Need-to-Know RBAC:** HttpOnly JWT + RLS
- ✅ **Walking Skeleton First:** Phase 1 = infrastructure, math later

---

**Status:** DONE — Ready for Root approval
**Effort:** ~6 hours (research + analysis + ADR writing)
**Quality:** Production-ready ADR with decision matrices, migration path, risk assessment

---

*Architect Terminal - MSG-ARCHITECT-040*
