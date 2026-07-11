# ADR-058: JoineryTech Backend-Frontend Integration Architecture

**Státusz:** FINAL
**Dátum:** 2026-07-02
**Döntéshozó:** Architect
**Kutatás:** Explorer (BACKEND_FRONTEND_INTEGRATION_READINESS_2026-07-02)
**Epic:** EPIC-JT-CRM, EPIC-JT-CTRL, EPIC-JT-HR, EPIC-JT-MAINT, EPIC-JT-QA, EPIC-JT-EHS, EPIC-JT-DMS, EPIC-JT-AI

---

## Executive Summary

The JoineryTech production-ready architecture requires integration between:
- **Backend:** .NET 8 REST API (stateless, DDD/FSM, PostgreSQL RLS)
- **Frontend:** React 18 SPA (currently localStorage monolith, 4.2 MB build)

**Critical Finding:** The Backend assumes a stateless REST client, but the Frontend is a stateful localStorage monolith. This architectural mismatch creates **8 critical integration gaps** that must be resolved before Phase 1 development begins.

This ADR provides architectural decisions and migration paths for all 8 gaps.

---

## Kontextus

### Project Background

**JoineryTech** is a production-ready manufacturing ERP for a Hungarian B2B woodworking company. The system spans 8 core modules:
- CRM (Customer Relationship Management)
- Kontrolling (Financial Control)
- HR (Human Resources)
- Maintenance (Equipment & Asset Management)
- QA (Quality Assurance)
- EHS (Environment, Health, Safety)
- DMS (Document Management)
- AI (Intelligent Automation)

**Current State:**
- **Frontend:** 108 JSX components, 9,087-line `app-store.jsx` localStorage monolith, Babel/CDN build (4.2 MB)
- **Backend Plan:** 5,200+ line architecture document (REST API, PostgreSQL RLS, JWT+RBAC, DDD/FSM)
- **Gap:** Backend and Frontend were designed independently; integration assumptions are incompatible

**Target:** Q3-Q4 2026 production readiness for all 8 modules.

### 8 Critical Integration Gaps

| # | Gap | Priority | Impact | Blocker |
|---|-----|----------|--------|---------|
| 1 | **State Management** | CRITICAL | Phase 1-3 | Frontend must become thin client |
| 2 | **Authentication Flow** | CRITICAL | Phase 1 | JWT token handling missing |
| 3 | **Real-Time Sync** | HIGH | Phase 2 | Multi-user consistency |
| 4 | **API Contract Specification** | HIGH | Phase 1 | No shared schema |
| 5 | **Error Handling** | HIGH | Phase 1 | No retry/fallback logic |
| 6 | **Performance Migration** | HIGH | Phase 1 | Monolith blocks code splitting |
| 7 | **Data Validation** | MEDIUM | Phase 1 | Duplicate validation logic |
| 8 | **Testing Strategy** | MEDIUM | Phase 1 | No integration tests |

---

## Döntések és Alternatívák

### Gap 1: State Management Paradigm (CRITICAL)

**Problem:** Frontend maintains complete state locally (`window.sim` + localStorage). Backend expects stateless HTTP client.

#### Alternative A: localStorage Monolith (Status Quo)
**Description:** Keep `window.sim` as single source of truth, sync to API on save.

| Aspect | Evaluation |
|--------|------------|
| **Pros** | No frontend refactor needed, offline capability maintained |
| **Cons** | Race conditions (concurrent edits), stale data on refresh, server cannot enforce FSM |
| **Complexity** | LOW |
| **Risk** | HIGH (data consistency) |
| **Cost** | $0 (no change) |

#### Alternative B: TanStack Query (React Query) + Optimistic Updates
**Description:** Replace localStorage with API-first state management. Cache responses locally with TanStack Query.

| Aspect | Evaluation |
|--------|------------|
| **Pros** | Server is source of truth, optimistic UI for UX, auto-invalidation on mutations |
| **Cons** | Requires frontend refactor (3-4 weeks), no offline mode |
| **Complexity** | MEDIUM |
| **Risk** | MEDIUM (migration path) |
| **Cost** | $12k-15k (4 weeks × 1 FTE) |

#### Alternative C: Redux + API Middleware
**Description:** Replace localStorage with Redux. Use middleware for API sync + offline queue.

| Aspect | Evaluation |
|--------|------------|
| **Pros** | Centralized state, offline queue possible, DevTools support |
| **Cons** | Boilerplate heavy, offline queue adds complexity, doesn't prevent race conditions |
| **Complexity** | HIGH |
| **Risk** | MEDIUM |
| **Cost** | $18k-22k (6 weeks × 1 FTE) |

#### **DECISION: Alternative B (TanStack Query)**

**Rationale:**
1. **Server Authority:** Backend FSM must be source of truth (security + multi-user)
2. **Developer Experience:** TanStack Query has minimal boilerplate vs Redux
3. **Performance:** Automatic caching + background refetching reduces API calls
4. **Migration Path:** Can coexist with localStorage during Phase 1-2 transition

**Trade-offs:**
- **What we gain:** Data consistency, server-enforced FSM, multi-user safety
- **What we lose:** Offline capability (acceptable for B2B ERP)
- **Migration complexity:** localStorage → TanStack Query spans Phase 1-3 (8 weeks)

**Implementation Path:**
- **Week 1-2 (Phase 1):** Introduce TanStack Query, keep localStorage as fallback for non-API entities
- **Week 3-6 (Phase 2):** Migrate core transaction flows (quotes → orders → invoices)
- **Week 7-8 (Phase 3):** Remove localStorage, API-only state

**Risk Mitigation:**
- Early spike (Week 2): Prototype quote approval flow end-to-end
- Fallback strategy: Keep localStorage read-only cache during Phase 1-2

---

### Gap 2: Authentication & Authorization (CRITICAL)

**Problem:** Frontend has zero authentication. Backend requires JWT + RBAC for every request.

#### Alternative A: JWT in localStorage + API Interceptor
**Description:** Store JWT access token in `localStorage`, attach to requests via Axios interceptor.

| Aspect | Evaluation |
|--------|------------|
| **Pros** | Simple implementation, standard pattern, token refresh straightforward |
| **Cons** | XSS vulnerable (token accessible to JavaScript), not OWASP recommended |
| **Complexity** | LOW |
| **Risk** | HIGH (security) |
| **Cost** | $3k (1 week × 1 FTE) |

#### Alternative B: HttpOnly Cookie + SameSite
**Description:** Backend sets JWT in HttpOnly cookie. Browser auto-attaches to requests.

| Aspect | Evaluation |
|--------|------------|
| **Pros** | XSS-proof (JavaScript cannot access), OWASP best practice, CSRF protection with SameSite |
| **Cons** | Requires CORS config, complex for multi-domain scenarios, refresh flow needs CSRF token |
| **Complexity** | MEDIUM |
| **Risk** | LOW (security) |
| **Cost** | $6k (2 weeks × 1 FTE) |

#### Alternative C: Session-Based Auth (Server-Side Storage)
**Description:** Backend stores session in Redis, returns session ID cookie.

| Aspect | Evaluation |
|--------|------------|
| **Pros** | XSS-proof, server can revoke sessions instantly, no token expiration issues |
| **Cons** | Requires Redis infrastructure, scalability concerns (session state), not stateless |
| **Complexity** | HIGH |
| **Risk** | MEDIUM (infra) |
| **Cost** | $12k (4 weeks × 1 FTE + Redis setup) |

#### **DECISION: Alternative B (HttpOnly Cookie)**

**Rationale:**
1. **Security:** XSS protection is critical for ERP with financial data
2. **OWASP Compliance:** HttpOnly + SameSite is industry standard
3. **Statelessness:** JWT remains stateless (no session store needed)
4. **Token Refresh:** Can use sliding expiration with same HttpOnly pattern

**Trade-offs:**
- **What we gain:** XSS protection, CSRF protection, OWASP compliance
- **What we lose:** Slightly more complex CORS config, token not accessible to frontend for custom flows
- **Implementation complexity:** 2 weeks vs 1 week for localStorage

**Implementation Path:**
- **Week 1 (Phase 1):** Backend: JWT generation + HttpOnly cookie setting
- **Week 2:** Frontend: Login flow + CORS config + API client setup
- **Week 3:** Token refresh endpoint + sliding expiration logic
- **Week 4:** RBAC enforcement + permission-based UI rendering

**Risk Mitigation:**
- CSRF token for non-GET requests (double-submit cookie pattern)
- SameSite=Strict for production, SameSite=Lax for dev (localhost cross-origin)

---

### Gap 3: Real-Time Synchronization (HIGH)

**Problem:** Frontend uses synchronous state mutations. Backend needs async event streams for multi-user scenarios.

#### Alternative A: HTTP Long Polling
**Description:** Client polls `/api/v1/events?since=<timestamp>` every 5-10 seconds.

| Aspect | Evaluation |
|--------|------------|
| **Pros** | Simple to implement, works behind firewalls, fallback-friendly |
| **Cons** | Inefficient (wasted requests), 5-10s latency, server load at scale |
| **Complexity** | LOW |
| **Risk** | LOW (technical) |
| **Cost** | $3k (1 week × 1 FTE) |

#### Alternative B: WebSocket Subscriptions
**Description:** Persistent WebSocket connection for real-time events (`wss://api/ws/quotes`).

| Aspect | Evaluation |
|--------|------------|
| **Pros** | Real-time (<100ms latency), efficient (one connection), bidirectional |
| **Cons** | Requires WebSocket server setup, connection management complexity, not HTTP cache-friendly |
| **Complexity** | MEDIUM |
| **Risk** | MEDIUM (infra) |
| **Cost** | $9k (3 weeks × 1 FTE) |

#### Alternative C: Server-Sent Events (SSE)
**Description:** HTTP-based event stream (`/api/v1/events` with `text/event-stream`).

| Aspect | Evaluation |
|--------|------------|
| **Pros** | HTTP-based (works with existing infra), auto-reconnect, simpler than WebSocket |
| **Cons** | Unidirectional (server → client only), limited browser support (6 concurrent SSE/domain) |
| **Complexity** | LOW-MEDIUM |
| **Risk** | LOW |
| **Cost** | $6k (2 weeks × 1 FTE) |

#### **DECISION: Alternative A (HTTP Polling) for Phase 1, Alternative B (WebSocket) for Phase 2**

**Rationale:**
1. **Walking Skeleton First:** Phase 1 focus is auth + catalog, not real-time collaboration
2. **Risk Reduction:** HTTP polling is battle-tested, no infra setup needed
3. **Phase 2 Evaluation:** WebSocket vs SSE decision deferred until multi-user scenarios tested

**Trade-offs:**
- **Phase 1:** Accept 5-10s latency, inefficient polling (acceptable for low traffic)
- **Phase 2:** Invest in WebSocket for real-time (<100ms latency) when needed

**Implementation Path:**
- **Phase 1 (Weeks 1-4):** HTTP polling every 10s for critical entities (quotes, orders)
- **Phase 2 (Weeks 8-12):** WebSocket infrastructure + event subscriptions
- **Phase 3:** Event sourcing + snapshot mechanism

**Risk Mitigation:**
- Phase 1: Polling interval configurable (10s default, 30s for low-priority entities)
- Phase 2: WebSocket fallback to polling if connection fails

---

### Gap 4: API Contract Specification (HIGH)

**Problem:** Backend plan has high-level endpoint visions. Frontend needs detailed request/response contracts.

#### Alternative A: Code-First (Backend writes API, Frontend adapts)
**Description:** Backend implements endpoints, generates OpenAPI from code. Frontend consumes spec.

| Aspect | Evaluation |
|--------|------------|
| **Pros** | Fast to start, no upfront design needed, code is source of truth |
| **Cons** | Frontend blocked until Backend ships, late discovery of contract mismatches, rework likely |
| **Complexity** | LOW |
| **Risk** | HIGH (rework) |
| **Cost** | $0 upfront, $15k-20k rework risk |

#### Alternative B: Contract-First (OpenAPI spec written before code)
**Description:** Architect + Backend + Frontend collaborate on OpenAPI spec. Both teams code-gen from spec.

| Aspect | Evaluation |
|--------|------------|
| **Pros** | Early contract validation, parallel development, code-gen ensures consistency |
| **Cons** | Upfront design time (3-4 days), spec maintenance overhead |
| **Complexity** | MEDIUM |
| **Risk** | LOW (early validation) |
| **Cost** | $4k upfront (spec writing), saves $15k rework |

#### Alternative C: Consumer-Driven Contract Tests (Pact)
**Description:** Frontend writes contract tests defining expected API behavior. Backend validates against tests.

| Aspect | Evaluation |
|--------|------------|
| **Pros** | Frontend defines needs, tests are living documentation, prevents breaking changes |
| **Cons** | Requires Pact infrastructure, learning curve, only catches breaking changes (not design mismatches) |
| **Complexity** | HIGH |
| **Risk** | MEDIUM |
| **Cost** | $12k (4 weeks setup + learning) |

#### **DECISION: Alternative B (Contract-First OpenAPI)**

**Rationale:**
1. **Risk Mitigation:** Early validation prevents $15k-20k rework
2. **Parallel Development:** Frontend mocks API from spec, Backend implements concurrently
3. **Code-Gen:** Orval (Frontend) + NSwag (Orchestrator) auto-generate clients (ADR-050)
4. **Walking Skeleton:** Spec written in Week 0, teams start coding Week 1

**Trade-offs:**
- **What we gain:** Early validation, parallel dev, code-gen consistency
- **What we lose:** 3-4 days upfront design time
- **Net benefit:** $15k-20k rework savings - $4k spec cost = $11k-16k saved

**Implementation Path:**
- **Week 0 (Pre-Phase 1):** Write full OpenAPI 3.1 spec for Auth, Catalog, CRM, Sales modules
- **Week 1-4 (Phase 1):** Backend code-gen from spec, Frontend code-gen from spec, both teams validate
- **Ongoing:** Spec updates as design evolves (version control in Git)

**Risk Mitigation:**
- Spec review with Backend + Frontend before locking (avoid design blindspots)
- OpenAPI validation in CI/CD (catch spec drift)

---

### Gap 5: Error Handling & Resilience (HIGH)

**Problem:** Frontend has no API error recovery. Backend assumes standard HTTP semantics.

#### Alternative A: Global Axios Interceptor
**Description:** Single interceptor handles all errors (401 → refresh token, 500 → retry, etc.).

| Aspect | Evaluation |
|--------|------------|
| **Pros** | Centralized logic, DRY, easy to update global behavior |
| **Cons** | One-size-fits-all (some errors need custom handling), hard to test |
| **Complexity** | LOW |
| **Risk** | MEDIUM (inflexible) |
| **Cost** | $3k (1 week × 1 FTE) |

#### Alternative B: Per-Query Error Handlers (TanStack Query)
**Description:** Each query/mutation defines its own `onError` handler.

| Aspect | Evaluation |
|--------|------------|
| **Pros** | Flexible (custom handling per endpoint), easy to test, co-located with query logic |
| **Cons** | Code duplication (retry logic repeated), inconsistent UX if not standardized |
| **Complexity** | MEDIUM |
| **Risk** | LOW |
| **Cost** | $6k (2 weeks × 1 FTE) |

#### Alternative C: React Error Boundary + Fallback UI
**Description:** React Error Boundary catches rendering errors, shows fallback UI.

| Aspect | Evaluation |
|--------|------------|
| **Pros** | Catches all errors (not just API), UX-friendly fallback, prevents white screen |
| **Cons** | Only catches render errors (not async mutations), limited error context |
| **Complexity** | LOW |
| **Risk** | MEDIUM (incomplete) |
| **Cost** | $2k (0.5 weeks × 1 FTE) |

#### **DECISION: Alternative B (Per-Query Handlers) + Alternative A (Global Interceptor for Auth)**

**Rationale:**
1. **Flexibility:** Different endpoints need different error handling (quote approval vs catalog fetch)
2. **Testability:** Per-query handlers are easier to unit test
3. **Hybrid Approach:** Global interceptor for auth (401 → refresh), per-query for business logic (422 → show error)

**Trade-offs:**
- **What we gain:** Flexible error handling, easy testing, UX customization
- **What we lose:** Some code duplication (mitigated by shared utility functions)

**Implementation Path:**
- **Week 1-2 (Phase 1):** Global interceptor for 401 (token refresh) + 5xx (retry with backoff)
- **Week 3-4:** Per-query error handlers for business errors (400, 422)
- **Week 5-6 (Phase 2):** Standardized error toast library + user-facing messages

**Risk Mitigation:**
- Shared utility functions: `retryWithBackoff()`, `handleValidationError()`, `handleStateInvalidError()`
- Error code enum in TypeScript (prevents typos)

---

### Gap 6: Performance Migration Path (HIGH)

**Problem:** Frontend is 4.2 MB build with 488 KB monolith. Backend assumes modularized state.

#### Alternative A: Keep Babel + CDN (Status Quo)
**Description:** No build tool change. Continue with Babel transpilation + CDN for libraries.

| Aspect | Evaluation |
|--------|------------|
| **Pros** | No refactor needed, existing tooling works |
| **Cons** | 4.2 MB build, no tree-shaking, no code splitting, slow build times |
| **Complexity** | LOW |
| **Risk** | HIGH (performance) |
| **Cost** | $0 (no change) |

#### Alternative B: Vite + ESBuild
**Description:** Migrate to Vite for dev server + build. ESBuild for fast transpilation.

| Aspect | Evaluation |
|--------|------------|
| **Pros** | 50%+ build size reduction (2.0-2.2 MB), code splitting, fast HMR, Tailwind PurgeCSS |
| **Cons** | Migration effort (2-3 weeks), potential breaking changes |
| **Complexity** | MEDIUM |
| **Risk** | LOW (mature tool) |
| **Cost** | $9k (3 weeks × 1 FTE) |

#### Alternative C: Webpack 5 + SWC
**Description:** Migrate to Webpack 5 with SWC loader for faster transpilation.

| Aspect | Evaluation |
|--------|------------|
| **Pros** | Mature ecosystem, more plugins than Vite, incremental adoption possible |
| **Cons** | Slower than Vite, more config complexity, still 3.5 MB builds |
| **Complexity** | HIGH |
| **Risk** | MEDIUM |
| **Cost** | $12k (4 weeks × 1 FTE) |

#### **DECISION: Alternative B (Vite + ESBuild)**

**Rationale:**
1. **Performance:** 50%+ build size reduction (4.2 MB → 2.0-2.2 MB)
2. **Developer Experience:** Fast HMR (<100ms), minimal config
3. **Code Splitting:** Route-based lazy loading (Sales, Procurement, Manufacturing, Design bundles)
4. **Walking Skeleton:** Phase 1 focus = infrastructure, build optimization is foundational

**Trade-offs:**
- **What we gain:** 50% build size reduction, fast dev server, code splitting
- **What we lose:** 3 weeks migration effort
- **Net benefit:** Long-term performance gain, better DX

**Implementation Path:**
- **Week 1 (Phase 1):** Vite setup + build-time transpilation (4.2 MB → 2.8 MB)
- **Week 2:** TailwindCSS PostCSS + PurgeCSS (2.8 MB → 2.2 MB)
- **Week 3-4:** Code splitting + lazy loading (2.2 MB → 1.8-2.0 MB)
- **Week 5-6 (Phase 2):** App-store modularization (9,087 lines → 5 files × 1,800 lines)

**Risk Mitigation:**
- Incremental migration: Vite can coexist with existing Babel build during transition
- Bundlesize CI check: Fail build if bundle exceeds 2.5 MB

---

### Gap 7: Data Validation Alignment (MEDIUM)

**Problem:** Frontend has ad-hoc validation. Backend implements domain rules via FSM + DDD.

#### Alternative A: Server-Only Validation
**Description:** Frontend sends unvalidated data. Backend returns validation errors (400/422).

| Aspect | Evaluation |
|--------|------------|
| **Pros** | Single source of truth (Backend), no duplicate logic, zero divergence risk |
| **Cons** | Poor UX (submit to see errors), network round-trip for validation |
| **Complexity** | LOW |
| **Risk** | LOW (technical), HIGH (UX) |
| **Cost** | $0 (no frontend validation) |

#### Alternative B: Client + Server Validation (Manual Duplication)
**Description:** Frontend implements same validation rules as Backend (manually).

| Aspect | Evaluation |
|--------|------------|
| **Pros** | Good UX (immediate feedback), reduced API calls (invalid data never sent) |
| **Cons** | Duplicate logic (JavaScript + C#), high divergence risk, maintenance burden |
| **Complexity** | LOW |
| **Risk** | HIGH (divergence) |
| **Cost** | $6k (2 weeks × 1 FTE) + ongoing sync cost |

#### Alternative C: Generated Validators from OpenAPI Spec
**Description:** Backend defines validation in OpenAPI spec. Frontend code-gen validators.

| Aspect | Evaluation |
|--------|------------|
| **Pros** | Single source of truth (OpenAPI), auto-sync (spec update → re-generate), good UX |
| **Cons** | OpenAPI validation syntax limited (complex rules need custom logic) |
| **Complexity** | MEDIUM |
| **Risk** | LOW |
| **Cost** | $6k (2 weeks × 1 FTE setup) |

#### **DECISION: Alternative C (Generated Validators) with Async Server Validation for Complex Rules**

**Rationale:**
1. **Single Source of Truth:** OpenAPI spec defines validation rules
2. **Auto-Sync:** Code-gen prevents divergence
3. **Hybrid Approach:** Simple rules (required, length, regex) → generated validators; Complex rules (business logic, DB lookups) → async API validation

**Trade-offs:**
- **What we gain:** Auto-sync, good UX (immediate feedback), single source of truth
- **What we lose:** OpenAPI validation syntax limited (complex rules need async API call)

**Implementation Path:**
- **Week 2-3 (Phase 1):** Define validation rules in OpenAPI spec (required, min/max length, regex patterns)
- **Week 4-6:** Generate TypeScript validators from OpenAPI (`@openapi-generator-plus/typescript-fetch-client`)
- **Week 7-8:** Update frontend forms to use generated validators + async validation for complex rules

**Example:**
```typescript
// Generated validator from OpenAPI
const validateCustomerCode = (code: string): ValidationResult => {
  if (!code) return { valid: false, error: 'Customer code is required' };
  if (code.length > 50) return { valid: false, error: 'Customer code must be ≤ 50 chars' };
  if (!/^[A-Z0-9\-]+$/.test(code)) return { valid: false, error: 'Only uppercase, digits, dashes allowed' };
  return { valid: true };
};

// Async validation for complex rules (DB lookup)
const validateCustomerCodeUnique = async (code: string): Promise<ValidationResult> => {
  const response = await api.post('/quotes/validate', { customerCode: code });
  return response.data; // { valid: true } or { valid: false, error: 'Customer code already exists' }
};
```

**Risk Mitigation:**
- OpenAPI spec review before locking (ensure validation rules are complete)
- Async validation debounced (300ms) to reduce API calls

---

### Gap 8: Testing Strategy (MEDIUM)

**Problem:** Frontend has no tests. Backend plan assumes API contract testing.

#### Alternative A: Manual QA Only
**Description:** No automated tests. Manual testing before each release.

| Aspect | Evaluation |
|--------|------------|
| **Pros** | Zero test infrastructure cost, zero test writing time |
| **Cons** | High regression risk, slow release cycle, no CI/CD validation |
| **Complexity** | LOW |
| **Risk** | HIGH (regressions) |
| **Cost** | $0 upfront, $20k-30k/year QA time |

#### Alternative B: Vitest (Unit/Integration) + Playwright (E2E)
**Description:** Component tests with Vitest + Testing Library. E2E tests with Playwright.

| Aspect | Evaluation |
|--------|------------|
| **Pros** | Fast unit tests (<1s), cross-browser E2E, good DX, modern tooling |
| **Cons** | Setup time (1-2 weeks), test writing time (30% of dev time) |
| **Complexity** | MEDIUM |
| **Risk** | LOW |
| **Cost** | $9k setup (3 weeks × 1 FTE) + 30% dev time ongoing |

#### Alternative C: Jest + Cypress
**Description:** Component tests with Jest + Testing Library. E2E tests with Cypress.

| Aspect | Evaluation |
|--------|------------|
| **Pros** | Mature ecosystem, large community, familiar to most devs |
| **Cons** | Slower than Vitest (Jest config complexity), Cypress paid plan for parallelization |
| **Complexity** | MEDIUM |
| **Risk** | LOW |
| **Cost** | $9k setup (3 weeks × 1 FTE) + Cypress license ($2k/year) |

#### **DECISION: Alternative B (Vitest + Playwright)**

**Rationale:**
1. **Performance:** Vitest is 10x faster than Jest for unit tests
2. **DX:** Vitest has minimal config (uses Vite config), native ESM support
3. **E2E:** Playwright is modern, cross-browser, no paid license needed
4. **Walking Skeleton:** Phase 1 focus = infrastructure, testing is foundational

**Trade-offs:**
- **What we gain:** Fast tests, good DX, cross-browser E2E, free tooling
- **What we lose:** Smaller community vs Jest/Cypress
- **Net benefit:** Performance gain outweighs community size

**Implementation Path:**
- **Week 3-4 (Phase 1):** Vitest setup + example component tests (auth flow)
- **Week 5-6:** E2E Playwright tests for happy paths (login → quote creation → approval)
- **Week 7-8 (Phase 2):** Error scenario testing (401, 422, 500) + coverage goals (≥80% auth module)

**Test Coverage Goals:**

| Phase | Coverage Target | Test Types |
|-------|----------------|------------|
| Phase 1 | ≥80% (auth module) | Component tests (Vitest), E2E (Playwright) |
| Phase 2 | ≥70% (CRM, Sales) | API integration tests (Backend), Component tests (Frontend) |
| Phase 3 | ≥60% (all modules) | Contract tests (OpenAPI validation), E2E (critical paths) |

**Risk Mitigation:**
- CI/CD integration: Tests run on every PR (GitHub Actions)
- Coverage gates: PR blocked if coverage drops below target

---

## Migration Path: localStorage → REST API

### 3-Phase Transition Strategy

#### Phase 1: Infrastructure (Weeks 1-4)
**Goal:** Auth + Catalog API working end-to-end. No transactional state migration.

**Backend:**
- Auth API endpoints (login, refresh, logout)
- Catalog API endpoints (GET /catalog, GET /catalog/{id})
- Permission system (RBAC + RLS in PostgreSQL)
- API Gateway setup (routing + error normalization)

**Frontend:**
- Vite setup + build optimization (4.2 MB → 2.2 MB)
- TanStack Query integration
- JWT token handling (HttpOnly cookie)
- Login form + auth flow
- Error handling library + Toast notifications

**localStorage Scope:**
- **Keep:** Transaction state (quotes, orders, invoices) — localStorage monolith
- **Migrate:** Auth state → API, Catalog state → API

**Exit Criteria:**
- Auth flow works end-to-end (login → JWT → refresh)
- Catalog API returns data correctly
- Frontend can fetch catalog data and display it
- Error responses follow spec; Frontend handles them correctly
- 80% test coverage for auth module

---

#### Phase 2: Transaction State Migration (Weeks 5-12)
**Goal:** Core transaction flows (quotes → orders → invoices) migrated to API.

**Backend:**
- CRM API (customers, contacts)
- Sales API (quotes, orders, invoices)
- Real-time sync (WebSocket or SSE)
- State transition validation (FSM)

**Frontend:**
- TanStack Query for transaction state
- Optimistic UI for mutations
- Real-time event subscriptions
- App-store modularization (9,087 lines → 5 domain files)

**localStorage Scope:**
- **Keep:** Lookup tables (materials, employees, settings) — localStorage cache
- **Migrate:** Quotes, Orders, Invoices → API

**Hybrid Mode:**
- localStorage read-only cache for lookup tables
- API writes for transaction state
- Conflict resolution: Server wins (localStorage invalidated on conflict)

**Exit Criteria:**
- Quote lifecycle works end-to-end (create → approve → order)
- Multi-user editing tested (two users editing same quote)
- Real-time sync working (<500ms latency)
- 70% test coverage for CRM + Sales modules

---

#### Phase 3: Complete Cutover (Weeks 13-20)
**Goal:** Remove localStorage entirely. API-only state.

**Backend:**
- All 8 modules API complete (Kontrolling, HR, Maintenance, QA, EHS, DMS, AI)
- Event sourcing + snapshot mechanism
- Full audit logging

**Frontend:**
- localStorage removed
- API-only state management
- Code splitting by module (lazy loading)
- Performance optimization (Lighthouse score ≥85)

**localStorage Scope:**
- **Remove:** All localStorage usage
- **Replace:** API-only state + TanStack Query cache (in-memory)

**Exit Criteria:**
- All 8 modules working end-to-end
- No localStorage usage
- Lighthouse score ≥85 (Performance)
- 60% test coverage across all modules
- API response time <300ms (p95)

---

## Phase 1 Implementation Priorities (Ranked)

### Critical Path (Must Have for Phase 1 Start)

| Priority | Item | Effort | Owner | Deliverable |
|----------|------|--------|-------|------------|
| **P0** | OpenAPI Spec Writing | 3-4 days | Architect + Backend + Frontend | `/docs/joinerytech/API_SPEC_PHASE1.yaml` |
| **P0** | Auth API Endpoints | 1 week | Backend | `/api/v1/auth/login`, `/refresh`, `/logout` |
| **P0** | Vite + TanStack Query Setup | 1 week | Frontend | Build: 4.2 MB → 2.2 MB, TanStack Query integrated |
| **P0** | JWT HttpOnly Cookie Handling | 1 week | Backend + Frontend | Login flow + token refresh working |

### High Priority (Should Have for Phase 1)

| Priority | Item | Effort | Owner | Deliverable |
|----------|------|--------|-------|------------|
| **P1** | Catalog API Endpoints | 1 week | Backend | `/api/v1/catalog`, `/catalog/{id}` |
| **P1** | Error Handling Framework | 1 week | Frontend | Global interceptor + per-query handlers |
| **P1** | Component Tests (Auth) | 1 week | Frontend | Vitest setup + auth flow tests |
| **P1** | API Contract Tests | 1 week | Backend | OpenAPI validation in CI/CD |

### Medium Priority (Nice to Have for Phase 1)

| Priority | Item | Effort | Owner | Deliverable |
|----------|------|--------|-------|------------|
| **P2** | E2E Tests (Playwright) | 1 week | Frontend | Login → Catalog E2E tests |
| **P2** | HTTP Polling for Events | 0.5 weeks | Frontend | `/api/v1/events` polling every 10s |
| **P2** | Code Splitting | 1 week | Frontend | Route-based lazy loading |

---

## Risk Assessment

### Critical Risks (Could Block Phase 1)

| Risk | Impact | Likelihood | Mitigation |
|------|--------|-----------|-----------|
| **State management mismatch causes data loss** | CRITICAL | HIGH | Early spike: prototype quote approval with TanStack Query (Week 2) |
| **API contract undefined, causes rework** | CRITICAL | MEDIUM | Write OpenAPI spec Week 0; use contract testing in CI/CD |
| **JWT token expiration breaks user sessions** | HIGH | MEDIUM | Implement token refresh early; test rotation edge cases (Week 3) |

### High Risks (Could Delay Phase 1)

| Risk | Impact | Likelihood | Mitigation |
|------|--------|-----------|-----------|
| **App-store monolith blocks performance improvement** | HIGH | MEDIUM | Modularize in parallel with API implementation (Weeks 1-4) |
| **localStorage fallback creates inconsistency** | HIGH | LOW | Phase 1 = Auth only (no transactional state), minimize localStorage scope |
| **Frontend team unfamiliar with TanStack Query** | MEDIUM | MEDIUM | 2-day training + pair programming with senior dev (Week 1) |

### Medium Risks (Could Impact Phase 2)

| Risk | Impact | Likelihood | Mitigation |
|------|--------|-----------|-----------|
| **Real-time sync complexity delayed to Phase 2** | MEDIUM | HIGH | Accept as Phase 2 scope; document HTTP polling strategy for Phase 1 |
| **Validation rule divergence (frontend vs backend)** | MEDIUM | MEDIUM | Generate validators from OpenAPI spec (Week 4-6) |
| **Performance gains not realized by Phase 3** | MEDIUM | MEDIUM | Bundlesize CI check; Lighthouse CI; measure continuously |

---

## Success Metrics

### Phase 1 Success (End of Week 4)

| Metric | Target | Measurement |
|--------|--------|------------|
| **API Response Time** | <200ms (p95) | APM dashboard (Serilog/ELK) |
| **Frontend Build Size** | 2.2 MB (from 4.2 MB) | `npm run build` output |
| **Test Coverage** | ≥80% (auth module) | Vitest coverage report |
| **Auth Flow Success Rate** | 99.5% | Login funnel analytics |
| **Error Handling** | 100% of error codes tested | E2E test results |

### Phase 2 Success (End of Week 12)

| Metric | Target | Measurement |
|--------|--------|------------|
| **API Response Time** | <250ms (p95) across CRM + Sales | APM dashboard |
| **Real-Time Sync Latency** | <500ms event delivery | WebSocket metrics |
| **Multi-User Conflicts** | <1% conflict rate | Error tracking (Sentry) |
| **Test Coverage** | ≥70% (CRM + Sales) | Vitest + Playwright coverage |

### Phase 3 Success (End of Week 20)

| Metric | Target | Measurement |
|--------|--------|------------|
| **API Response Time** | <300ms (p95) across all endpoints | APM dashboard |
| **Frontend Performance** | Lighthouse score ≥85 | Lighthouse CI |
| **Data Consistency** | 0 data loss incidents | Error tracking (Sentry) |
| **Test Coverage** | ≥60% (all modules) | Vitest + Playwright coverage |
| **Offline Capability** | Removed (API-only) | Feature test |

---

## Következmények

### Pozitív

1. **Single Source of Truth:** Backend FSM enforces state transitions (security + consistency)
2. **Multi-User Safety:** Real-time sync prevents race conditions
3. **Performance:** 50%+ build size reduction (4.2 MB → 2.0-2.2 MB)
4. **Security:** XSS protection (HttpOnly cookie), OWASP compliance
5. **Developer Experience:** TanStack Query + Vite = fast dev cycle
6. **Testability:** 80% coverage (auth), 70% (CRM/Sales), 60% (all modules)

### Negatív

1. **Migration Effort:** 20 weeks (Phase 1-3) frontend refactor
2. **Offline Capability Removed:** localStorage → API transition removes offline mode (acceptable for B2B ERP)
3. **Complexity:** WebSocket infrastructure (Phase 2) adds server complexity
4. **Learning Curve:** TanStack Query + Vite (2-day training needed)

### Semleges

1. **Real-Time Strategy Deferred:** Phase 1 = HTTP polling (simple), Phase 2 = WebSocket (complex)
2. **Validation Sync:** OpenAPI-based validation works for simple rules; complex rules need async API calls
3. **Testing Investment:** 30% of dev time for tests (industry standard)

---

## Implementáció

### Week 0: Pre-Phase 1 (Contract-First Design)

**Owner:** Architect + Backend + Frontend

**Tasks:**
1. Write full OpenAPI 3.1 spec for Auth, Catalog, CRM, Sales modules
2. Include request/response examples for 200, 400, 401, 422, 500
3. Share spec with Backend + Frontend teams for feedback
4. Lock spec before implementation starts

**Deliverable:** `/opt/spaceos/docs/joinerytech/API_SPEC_PHASE1.yaml`

**Acceptance Criteria:**
- [ ] All Phase 1 endpoints documented (Auth, Catalog)
- [ ] Error response schemas defined (400, 401, 422, 500)
- [ ] Validation rules specified (required, min/max, regex)
- [ ] Backend + Frontend teams reviewed and approved spec

---

### Phase 1: Core Infrastructure (Weeks 1-4)

**Owner:** Backend + Frontend (parallel workstreams)

**Backend Tasks:**
1. Auth API endpoints (login, refresh, logout)
2. Catalog API endpoints (GET /catalog, GET /catalog/{id})
3. Permission system (RBAC + RLS in PostgreSQL)
4. API Gateway setup (routing + error normalization)
5. API contract tests (OpenAPI validation in CI/CD)

**Frontend Tasks:**
1. Vite setup + build optimization
2. TanStack Query integration
3. JWT token handling (HttpOnly cookie + interceptor)
4. Login form + auth flow
5. Error handling library + Toast notifications
6. Component tests (Vitest) for auth flow

**Acceptance Criteria:**
- [ ] Auth flow works end-to-end (login → JWT → refresh)
- [ ] Catalog API returns data correctly
- [ ] Frontend can fetch catalog data and display it
- [ ] Error responses follow spec; Frontend handles them correctly
- [ ] Logout clears tokens and redirects to login
- [ ] 80% test coverage for auth module
- [ ] API response time <200ms (p95)
- [ ] Frontend build size <2.5 MB

---

### Next Steps (Phase 2-3)

**Phase 2 (Weeks 5-12):** Transaction state migration (quotes → orders → invoices)

**Phase 3 (Weeks 13-20):** Complete cutover (all 8 modules, remove localStorage)

---

## Referencia Dokumentumok

| Document | Location | Purpose |
|----------|----------|---------|
| **Backend Architecture Plan** | `/opt/spaceos/docs/joinerytech/BACKEND_ARCHITECTURE_PLAN.md` | .NET 8 REST API design (5,200+ lines) |
| **Frontend UI/UX Audit** | `/opt/spaceos/docs/joinerytech/AUDIT_UI_PERFORMANCE_A11Y_2026-07-02.md` | Performance, UX, A11y findings |
| **Integration Readiness** | `/opt/spaceos/docs/joinerytech/BACKEND_FRONTEND_INTEGRATION_READINESS_2026-07-02.md` | 8 gap analysis (Explorer research) |
| **ADR-050: Code Generator Toolchain** | `/opt/spaceos/docs/architecture/decisions/ADR-050-code-generator-toolchain.md` | Orval + NSwag setup |
| **SpaceOS Vision Master** | `/opt/spaceos/docs/vision/SpaceOS_Vision_Master.md` | 5 Golden Rules, 4-layer architecture |

---

## Document Sign-Off

**Prepared by:** Architect Terminal
**Date:** 2026-07-02
**Status:** FINAL — Ready for Implementation

**Review Required:**
- [ ] Root review and approval
- [ ] Backend terminal review (tech feasibility)
- [ ] Frontend terminal review (implementation plan)
- [ ] Conductor scheduling (Phase 1 start date)

**Next Steps:**
1. Root approves ADR-058 (target: same day)
2. Backend + Frontend review implementation plan (target: 2 days)
3. Week 0 starts: OpenAPI spec writing (Architect + Backend + Frontend)
4. Phase 1 starts: Week 1 (Backend + Frontend parallel development)

---

**End of ADR-058**
