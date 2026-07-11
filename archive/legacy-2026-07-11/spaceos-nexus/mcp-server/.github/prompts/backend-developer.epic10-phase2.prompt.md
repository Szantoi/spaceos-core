---
id: backend-developer-epic10-phase2
type: startup-prompt
for: Backend Developer
epic: EPIC-10
phase: 2
version: 1.0
created: 2026-03-06
effective_date: 2026-03-09
---

# 🚀 Backend Developer Startup Prompt — EPIC-10 Phase 2

**Date:** 2026-03-09
**Your Role:** Backend Developer implementing error handling, performance, and documentation for bootstrap_agent tool
**Total Effort:** 19 hours across 3 tasks (2-2.5 days)
**Team:** 2 developers (parallel execution recommended)

---

## 📌 Context: Where We Are

### ✅ What's Done (Phase 1 — 2026-03-06)

**EPIC-10 Phase 1** is **COMPLETE & PEER REVIEW IN PROGRESS:**
- bootstrap_agent tool: ✅ Registered + working
- BootstrapService: ✅ Payload assembly logic complete (role definitions, workflows, templates)
- SessionManager: ✅ Session UUID v4 generation (crypto-strong, 10k collision tests: 0)
- E2E workflows: ✅ request_task + resume_task intent extraction
- QA Sign-Off: ✅ 5 critical focus areas validated (payload, degradation, performance, injection, session UUID)

**Metrics:**
- AC Coverage: 61/61 ✅
- Tests Passing: 91/91 ✅
- Code Coverage: 81.3% ✅
- Performance p95: 4.1ms (target <50ms, achieved 12x better!) ✅

**Current Status:** Phase 1 code merged to feature branch, awaiting final peer review approval (~2026-03-08).

---

## 🎯 Your Mission: Phase 2 (Quality Gates + Hardening)

**Phase 2 = Error Handling + Performance Validation + Documentation**

After Phase 1 peer review completes & merges to main (expected 2026-03-08), you'll implement:

### 3 Tasks (19 hours total)

| Task | Duration | Focus | Impact |
|:-----|:--------:|:------|:-------|
| **TASK-10-06** | 6h | OWASP input validation + error standardization | Security hardening |
| **TASK-10-07** | 5h | Performance load testing + SLA validation | Reliability assurance |
| **TASK-10-08** | 8h | Documentation (tool guide, ADRs, runbook) | Knowledge transfer |

**Timeline:**
- Sequential (1 dev): 2026-03-09 → 2026-03-12 (2.5 days)
- **Parallel (2 devs, recommended): 2026-03-09 → 2026-03-11 (2 days, save ~1 day!)**

---

## 📂 File Structure You'll Touch

### Core Files (Modify)

```
src/mcp/
├── BootstrapAgent.ts           ← Add validation BEFORE BootstrapService calls
├── BootstrapService.ts         ← Add profiling/metrics (TASK-10-07)
├── ErrorResponses.ts           ← Error code catalog + response format
└── SessionManager.ts           ← (No changes needed)

src/tests/
├── unit/
│   ├── InputValidator.test.ts        ← NEW (TASK-10-06, 12+ tests)
│   └── BootstrapAgent-ErrorHandling.test.ts ← NEW (TASK-10-06, 8+ tests)
└── performance/
    ├── bootstrap-load-test.ts        ← NEW (TASK-10-07, load harness)
    ├── latency-profiler.ts           ← NEW (TASK-10-07, profiling)
    └── bootstrap-performance.test.ts ← NEW (TASK-10-07, SLA assertions)

database/
└── standards/
    ├── ERROR_CODES.md               ← NEW (TASK-10-06, error catalog)
    └── adrs/
        └── EPIC-10-ADR.md           ← NEW (TASK-10-08, design decisions)

Docs/
├── tools/
│   └── bootstrap_agent.md           ← NEW (TASK-10-08, tool guide)
├── PERFORMANCE-SLA.md               ← NEW (TASK-10-07/08, SLA + runbook)
└── implementation-summary/
    ├── TASK-10-06-ErrorHandling-*.md     ← NEW (TASK-10-08)
    └── TASK-10-07-Performance-*.md       ← NEW (TASK-10-08)

.github/workflows/
└── performance-regression.yml       ← NEW (TASK-10-07, CI/CD gate)
```

---

## 🔴 TASK-10-06: Error Handling & OWASP (6 hours)

### What You're Building

**Input validation layer** that prevents injection attacks + **standardized error responses** across the bootstrap_agent tool.

### Acceptance Criteria (20 items)

**Input Validation (5 AC):**
- [ ] Domain validation: `/^[a-z-]+$/` (only lowercase + hyphens)
- [ ] Role validation: `/^[a-z_]+$/` (only lowercase + underscores)
- [ ] Reject invalid patterns with 400 Bad Request
- [ ] Error message doesn't leak encoding details
- [ ] Validation happens BEFORE querying database

**SQL Injection Prevention (3 AC):**
- [ ] BootstrapService uses parameterized queries (AgentDb methods already have this ✅)
- [ ] Error messages never include SQL or query details
- [ ] Attempted injection queries return 400, never execute

**Error Response Format (4 AC):**
- [ ] All errors: `{ success: false, code: "...", message: "...", details?: {...} }`
- [ ] HTTP status codes: 400 (user error), 500 (server error)
- [ ] Error codes documented in `ERROR_CODES.md`
- [ ] No stack traces in production responses

**Error Code Catalog (3 AC):**
- [ ] `invalid_domain` — domain doesn't match regex
- [ ] `invalid_role` — role doesn't match regex
- [ ] `role_not_found` — domain valid but role missing
- [ ] `query_timeout` — database query exceeded timeout
- [ ] `db_connection_error` — connection pool error
- [ ] `data_integrity_error` — schema constraint violation
- All mapped to standard HTTP status codes

**OWASP Injection Testing (5 AC):**
- [ ] SQL injection: `domain: "'; DROP TABLE roles; --"` → 400 ✅
- [ ] Command injection: `role: "$(whoami)"` → 400 ✅
- [ ] XSS: `domain: "<script>alert('xss')</script>"` → 400 ✅
- [ ] Path traversal: `role: "../../../etc/passwd"` → 400 ✅
- [ ] Unicode bypass: `domain: "ａｄｍｉｎ"` (full-width) → 400 ✅
- Test matrix: 15+ injection attacks → 0 successful
- Test coverage: ≥85%

### Deliverables

| File | Type | Lines | Purpose |
|:-----|:----:|:-----:|:--------|
| `src/mcp/InputValidator.ts` | NEW | 40-60 | Validation layer (sync functions) |
| `src/mcp/BootstrapAgent.ts` | MODIFY | +20-30 | Call validator BEFORE BootstrapService |
| `src/mcp/ErrorResponses.ts` | MODIFY | +50-80 | Error code catalog + format helpers |
| `database/standards/ERROR_CODES.md` | NEW | 80-120 | Error reference + HTTP mappings |
| `src/tests/unit/InputValidator.test.ts` | NEW | 150-200 | 12+ validation test cases |
| `src/tests/unit/BootstrapAgent-ErrorHandling.test.ts` | NEW | 120-150 | 8+ error response format tests |

### Implementation Steps

**Step 1: Create InputValidator.ts (1.5h)**
```typescript
// src/mcp/InputValidator.ts
export class InputValidator {
  static validateDomain(domain: string): { valid: boolean; error?: string } {
    if (!/^[a-z-]+$/.test(domain)) {
      return { valid: false, error: 'invalid_domain' };
    }
    return { valid: true };
  }

  static validateRole(role: string): { valid: boolean; error?: string } {
    if (!/^[a-z_]+$/.test(role)) {
      return { valid: false, error: 'invalid_role' };
    }
    return { valid: true };
  }
}
```

**Step 2: Integrate into BootstrapAgent.ts (1.5h)**
```typescript
// In BootstrapAgent tool handler
const domainVal = InputValidator.validateDomain(domain);
if (!domainVal.valid) {
  return { success: false, code: domainVal.error, message: "..." };
}
// ... then call BootstrapService
```

**Step 3: Standardize ErrorResponses.ts (1h)**
```typescript
// src/mcp/ErrorResponses.ts
export const ERROR_CODES = {
  invalid_domain: { status: 400, message: "Domain format invalid" },
  invalid_role: { status: 400, message: "Role format invalid" },
  role_not_found: { status: 404, message: "Role not found" },
  // ... 6+ more codes
};

export function formatError(code: string, details?: any) {
  const meta = ERROR_CODES[code] || ERROR_CODES.unknown;
  return {
    success: false,
    code,
    message: meta.message,
    details
  };
}
```

**Step 4: OWASP Testing (2h)**
- Create test matrix: 15+ injection patterns
- Each test: `bootstrap_agent(malicious_input)` → assert 400 response + no execution
- Coverage: SQL, command, XSS, path traversal, unicode bypass
- Verify: 0 successful injections

### Tests You'll Write (20+ test cases)

```typescript
// src/tests/unit/InputValidator.test.ts
describe('InputValidator', () => {
  test('valid domain: lowercase-hyphens', () => {
    const result = InputValidator.validateDomain('my-domain');
    expect(result.valid).toBe(true);
  });

  test('invalid domain: uppercase', () => {
    const result = InputValidator.validateDomain('MyDomain');
    expect(result.valid).toBe(false);
    expect(result.error).toBe('invalid_domain');
  });

  // ... 10+ more tests
});

// src/tests/unit/BootstrapAgent-ErrorHandling.test.ts
describe('BootstrapAgent - Error Handling', () => {
  test('SQL injection attempt → 400 Bad Request', async () => {
    const response = await bootstrap_agent({
      domain: "'; DROP TABLE roles; --",
      role: 'admin'
    });
    expect(response.success).toBe(false);
    expect(response.code).toBe('invalid_domain');
    // Verify injection never executed (query database state unchanged)
  });

  test('XSS payload → 400 Bad Request', async () => {
    const response = await bootstrap_agent({
      domain: '<script>alert("xss")</script>',
      role: 'admin'
    });
    expect(response.success).toBe(false);
  });

  // ... 15+ more injection tests
});
```

### Definition of Done

- [x] 20/20 AC verified (validation + errors + injection tests)
- [x] 20+ unit tests created + all passing
- [x] Code coverage ≥85%
- [x] 0 TypeScript errors
- [x] `npm test` passes all tests (jest suite)
- [x] Git commit: `"feat: TASK-10-06 Error handling + OWASP validation"`
- [x] Feature branch: `feature/TASK-10-06-error-handling`

---

## 🟡 TASK-10-07: Performance Load Testing (5 hours)

### What You're Building

**Load test framework** that validates bootstrap_agent meets SLA (<50ms p95) + **CI/CD regression detection** to catch performance regressions.

### Acceptance Criteria (22 items)

**Single Query Performance (5 AC):**
- [ ] bootstrap_agent() completes in <50ms p95 (measured 10,000 runs)
- [ ] Latency profiling: Breakdown per subsystem (getRole, createSession, payload assembly)
- [ ] Baseline snapshot: `performance-baseline.json` (p50, p95, p99 latencies)
- [ ] p99 < 100ms (even under peak load)
- [ ] Median latency < 20ms

**Concurrent Load (5 AC):**
- [ ] Load test harness: 10, 50, 100 concurrent agents
- [ ] 60-second steady-state measurement per concurrency level
- [ ] No cascading failures under load
- [ ] Memory stable (no leaks during 60s run)
- [ ] Connection pool stays healthy (max connections used, no timeouts)

**Performance Acceptance Criteria (4 AC):**
- [ ] 10 concurrent: p95 < 50ms ✅
- [ ] 50 concurrent: p95 < 50ms ✅
- [ ] p99 < 100ms at all levels
- [ ] Query latency breakdown: getRole <10ms, createSession <5ms, assembly <5ms

**Regression Detection (5 AC):**
- [ ] Baseline snapshot committed: `src/tests/fixtures/performance-baseline.json`
- [ ] CI/CD workflow: `.github/workflows/performance-regression.yml`
- [ ] Trigger on every PR: Run load test (50 concurrent, 60s)
- [ ] FAIL if p95 > baseline * 1.1 (10% increase threshold)
- [ ] WARN if p95 > baseline * 1.05 (5% warning threshold)

**Documentation (3 AC):**
- [ ] SLA documented: `Docs/PERFORMANCE-SLA.md`
- [ ] Runbook: Troubleshooting + investigation procedures
- [ ] Mitigation guide: What to do if p95 breaches SLA

### Deliverables

| File | Type | Lines | Purpose |
|:-----|:----:|:-----:|:--------|
| `src/tests/performance/bootstrap-load-test.ts` | NEW | 100-150 | Load test harness |
| `src/tests/performance/latency-profiler.ts` | NEW | 50-80 | High-res latency measurement |
| `src/tests/integration/bootstrap-performance.test.ts` | NEW | 80-120 | SLA assertion tests |
| `src/tests/fixtures/performance-baseline.json` | NEW | 15-25 | Baseline metrics snapshot |
| `.github/workflows/performance-regression.yml` | NEW | 60-100 | CI/CD regression gate |
| `Docs/PERFORMANCE-SLA.md` | NEW | 150-200 | SLA + runbook |

### Implementation Steps

**Step 1: Create Load Test Harness (2h)**
```typescript
// src/tests/performance/bootstrap-load-test.ts
export async function runLoadTest(options: {
  concurrency: 10 | 50 | 100;
  duration: number; // seconds
  domain: string;
  role: string;
}) {
  const results = { latencies: [], errors: [], startTime: Date.now() };

  const tasks = Array.from({ length: options.concurrency }, async () => {
    while (Date.now() - results.startTime < options.duration * 1000) {
      const start = performance.now();
      try {
        await bootstrap_agent({ domain: options.domain, role: options.role });
        results.latencies.push(performance.now() - start);
      } catch (err) {
        results.errors.push(err);
      }
    }
  });

  await Promise.all(tasks);
  return calculateStats(results.latencies);
}

function calculateStats(latencies: number[]) {
  const sorted = latencies.sort((a, b) => a - b);
  return {
    min: sorted[0],
    max: sorted[sorted.length - 1],
    median: sorted[Math.floor(sorted.length / 2)],
    p95: sorted[Math.floor(sorted.length * 0.95)],
    p99: sorted[Math.floor(sorted.length * 0.99)]
  };
}
```

**Step 2: Profiling Breakdowns (1.5h)**
- Add timing instrumentation to BootstrapService methods
- Measure: each query, role assembly, template loading, UUID generation
- Create latency report: `{ getRole: 8ms, createSession: 4ms, ...}`

**Step 3: CI/CD Workflow (1h)**
```yaml
# .github/workflows/performance-regression.yml
name: Performance Regression Check
on: [pull_request]

jobs:
  load-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-node@v3
      - run: npm ci
      - run: npm run test:performance 50 -- > results.json
      - name: Check Regression
        run: |
          node -e "
            const baseline = require('./src/tests/fixtures/performance-baseline.json');
            const current = require('./results.json');
            if (current.p95 > baseline.p95 * 1.1) {
              console.error('❌ REGRESSION: p95 exceeded 10% threshold');
              process.exit(1);
            }
          "
```

### Tests You'll Write (10+ test cases)

```typescript
// src/tests/integration/bootstrap-performance.test.ts
describe('Performance SLA', () => {
  test('Single query: p95 < 50ms', async () => {
    const stats = await runLoadTest({
      concurrency: 1,
      duration: 5,
      domain: 'test-domain',
      role: 'explorer'
    });
    expect(stats.p95).toBeLessThan(50);
  });

  test('10 concurrent: p95 < 50ms', async () => {
    const stats = await runLoadTest({
      concurrency: 10,
      duration: 10,
      domain: 'test-domain',
      role: 'explorer'
    });
    expect(stats.p95).toBeLessThan(50);
  });

  test('50 concurrent: p95 < 50ms', async () => {
    const stats = await runLoadTest({
      concurrency: 50,
      duration: 15,
      domain: 'test-domain',
      role: 'explorer'
    });
    expect(stats.p95).toBeLessThan(50);
  });

  // ... more tests
});
```

### Definition of Done

- [x] 22/22 AC verified (single query + concurrent + regression detection)
- [x] Load test framework runs successfully (10, 50, 100 concurrent)
- [x] p95 < 50ms confirmed (at all concurrency levels)
- [x] Baseline snapshot captured + committed
- [x] CI/CD workflow running (triggers on PR)
- [x] 10+ performance tests created + passing
- [x] 0 TypeScript errors
- [x] Git commit: `"perf: TASK-10-07 Load testing + SLA validation"`
- [x] Feature branch: `feature/TASK-10-07-performance`

---

## 🟠 TASK-10-08: Documentation (8 hours)

### What You're Building

**Comprehensive documentation** for EPIC-10 (tool guide, ADRs, operational runbook).

### Deliverables

| Document | Location | Audience | Effort |
|:----------|:---------|:--------:|:------:|
| **Tool Guide** | `Docs/tools/bootstrap_agent.md` | Developers | 2h |
| **Implementation Summaries** | `implementation-summary/TASK-10-06-*.md` + `TASK-10-07-*.md` | Backend team | 2h |
| **EPIC-10 ADR** | `database/standards/adrs/EPIC-10-ADR.md` | Architects | 2h |
| **Operational Runbook** | `Docs/PERFORMANCE-SLA.md` (SLA breach procedures) | Ops team | 2h |

### Sections (Each Document)

**1. Tool Guide (`Docs/tools/bootstrap_agent.md`)**
- API specification (input schema, output schema, error codes)
- Usage examples (happy path, error cases, performance scenarios)
- SLA details (p95 < 50ms)
- Troubleshooting (common issues + solutions)

**2. Implementation Summaries**
- TASK-10-06: Error handling patterns, validation regexes, OWASP test strategy
- TASK-10-07: Performance profiling approach, load test framework, CI/CD regression logic

**3. EPIC-10 ADR (Architecture Decision Record)**
- **Decision 1:** Why UUID v4 for session_id (crypto-strong, no collision risk)
- **Decision 2:** Graceful degradation strategy (missing role → cached fallback)
- **Decision 3:** Error response standardization pattern (never expose internals)
- **Decision 4:** Performance profiling approach (per-query breakdown)

**4. Operational Runbook**
- SLA definition: p95 < 50ms (at 50 concurrent)
- Monitoring setup: Track p95 latency continuously
- SLA breach response: Investigation steps + remediation
- Regression detection: How CI/CD workflow catches regressions

### Definition of Done

- [x] All 4 documents created + properly linked
- [x] No broken references (all links verified)
- [x] Implementation summaries reference code locations (line numbers)
- [x] ADRs follow project convention (database/standards/adrs/)
- [x] Ready for peer review + team knowledge transfer
- [x] Git commit: `"docs: TASK-10-08 EPIC-10 comprehensive documentation"`

---

## 🔄 Coordination: If You're 2 Developers (Recommended)

### Task Split

| Developer | Task | Hours | Timeline |
|:----------|:-----|:-----:|:--------:|
| **Dev A** | TASK-10-06 + TASK-10-08 start | 6h + start docs | 2026-03-09 → 2026-03-11 |
| **Dev B** | TASK-10-07 | 5h | 2026-03-09 → 2026-03-10 |

### Parallel Execution Protocol

**2026-03-09 09:00 — Both devs start simultaneously**
- Dev A: Begin TASK-10-06 (error handling) — no dependencies on Dev B ✅
- Dev B: Begin TASK-10-07 (performance) — no dependencies on Dev A ✅

**2026-03-10 14:00-15:00 — Both tasks likely complete**
- Dev A: Completes TASK-10-06, starts TASK-10-08 (documentation)
- Dev B: Completes TASK-10-07, can assist with documentation or other work

**2026-03-11 17:00 — Phase 2 complete**
- Dev A: Finishes TASK-10-08 documentation
- All 3 PRs ready for peer review

### Daily Standup (5 min, 09:30 each day)

**2026-03-09 09:30:**
- Status: Both devs started
- Blockers: None expected
- Next: Continue phases 1-2 of each task

**2026-03-10 09:30:**
- Status: TASK-10-06 & TASK-10-07 in final phases
- Blockers: Any test failures?
- Next: PR submissions (both can review in parallel)

**2026-03-11 09:30:**
- Status: TASK-10-08 ongoing (documentation)
- Blockers: Any clarifications needed from code authors?
- Next: Final review + merge decision

---

## 🛠️ Setup & Prerequisites

### Before You Start

1. **Ensure Phase 1 Merged**
   ```bash
   git checkout main
   git pull origin main
   # Should see recent commits: bootstrap_agent, BootstrapService, SessionManager
   ```

2. **Install Dependencies**
   ```bash
   npm install
   npm test  # Verify Phase 1 tests still pass (91/91 should show green)
   ```

3. **Create Feature Branches**
   ```bash
   # Dev A (error handling)
   git checkout -b feature/TASK-10-06-error-handling

   # Dev B (performance)
   git checkout -b feature/TASK-10-07-performance
   ```

4. **Review Reference Documentation**
   - Read: `EPIC-10-PHASE-2-ORCHESTRATION_2026-03-06.md` (detailed specs)
   - Read: `EPIC-10-PHASE-2-PARALLEL_2026-03-06.md` (coordination)
   - OWASP Top 10: Know the injection patterns you're testing
   - ChromaDB latency: Baseline from EPIC-09 (est. 0.01ms query time)

### Development Tools

```bash
# Run tests during development
npm test -- src/tests/unit/InputValidator.test.ts  # TASK-10-06
npm test -- src/tests/performance/bootstrap-load-test.ts  # TASK-10-07

# Check coverage
npm test -- --coverage --testPathPattern=InputValidator

# TypeScript type checking
npm run typecheck

# Lint
npm run lint
```

---

## 📋 Definition of Done (All 3 Tasks)

**When ALL true, Phase 2 is DONE:**

- [x] TASK-10-06: 20/20 AC verified + 20+ tests passing + ≥85% coverage
- [x] TASK-10-07: 22/22 AC verified + load tests passing + p95 < 50ms confirmed
- [x] TASK-10-08: 4 documentation deliverables completed + links verified
- [x] All 3 PRs submitted + code reviewed + approved
- [x] 0 TypeScript errors
- [x] All tests passing (aggregate 40+ new tests)
- [x] Peer review sign-off (Backend Dev + Tech Lead)
- [x] Merged to main (2026-03-11 EOD)

**Impact:**
- ✅ EPIC-10 COMPLETE (Phase 1 + Phase 2)
- ✅ EPIC-11 UNBLOCKED (FSM middleware ready to start 2026-03-12) 🚀

---

## 🎯 Success Metrics

| Metric | Target | Phase 1 | Phase 2 (You) |
|:-------|:------:|:-------:|:-------------:|
| AC Coverage | 100% | 61/61 ✅ | 48/48 (you) |
| Tests Passing | 100% | 91/91 ✅ | 40+/40+ (you) |
| Code Coverage | ≥80% | 81.3% ✅ | ≥85% (you) |
| Performance p95 | <50ms | 4.1ms ✅ | <50ms (you, validated) |
| TypeScript Errors | 0 | 0 ✅ | 0 (you) |
| OWASP Injection Prevention | 100% | (Phase 1 basic) | 15+ tests → 0 breaches (you) |

---

## 📞 Escalation & Support

**If Blocked:**
- Merge conflicts? Code review parallel PRs separately (Dev A priority)
- Test failures? Check Phase 1 baseline (91/91 should be green)
- Performance unexpected? Review ChromaDB latency profiling (EPIC-09 reference)
- Unclear AC? Ask Tech Lead (response time <2h expected)

**Reviewers:**
- Backend Dev (code quality, patterns, test coverage): ~2h review
- Tech Lead (security, performance, SLA): ~2.5h review
- Architect (strategic alignment, ADR quality): ~2h review

---

## 🚀 You're Ready!

**When you get the signal (EPIC-10 Phase 1 peer review complete + merge):**

1. ✅ Sync main branch (`git pull origin main`)
2. ✅ Create feature branches (TASK-10-06 & TASK-10-07)
3. ✅ Start TASK-10-06 (error handling) — 6 hours
4. ✅ Start TASK-10-07 (performance) — 5 hours (parallel if 2 devs)
5. ✅ Submit PRs → Peer review → Merge
6. ✅ Complete TASK-10-08 (documentation) — 8 hours
7. ✅ **Phase 2 Done: 2026-03-11 EOD** (or 2026-03-12 if sequential)

**Questions?** Refer back to orchestration docs or ask Tech Lead.

---

**Good luck! You've got this. 💪**

