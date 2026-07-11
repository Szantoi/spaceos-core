---
id: dev-b-task-10-07-prompt
title: "Dev B — TASK-10-07: Performance Load Testing & SLA Validation"
epic: EPIC-10
milestone: M02
duration: 5 hours
start: "2026-03-09 09:00 UTC"
end: "2026-03-10 14:00 UTC"
target_ac: "22/22"
target_tests: "≥10 load tests"
target_sla: "p95 < 50ms @ 50 concurrent"
language: en
---

# 🎯 DEV B — TASK-10-07 Execution Prompt

## Phase 2 Context
- **Date:** 2026-03-09 to 2026-03-10 (5h, parallel with Dev A + C)
- **Team:** Dev A (error handling), Dev B (performance), Dev C (documentation)
- **Standup:** 09:00, 12:00, 18:00 UTC daily
- **Merge Gate:** 14:00 UTC on 2026-03-10 (or before)

---

## 🎯 Your Mission

Build **load testing infrastructure** and **baseline SLA validation** for `bootstrap_agent`:
1. Create load test harness (simulate 10/50/100 concurrent agents)
2. Measure latency percentiles (p50, p95, p99)
3. Lock performance baseline (p95 < 50ms mandatory)
4. Integrate CI/CD regression gate (fail if p95 > baseline × 1.1)
5. Generate performance-baseline.json snapshot for future regression detection

---

## 📋 Acceptance Criteria (AC) — 22 Items

### Load Test Infrastructure (AC-1 through AC-7)

- [ ] **AC-1:** Load test harness created in `src/tests/e2e/load-test.ts`
  - Simulates N concurrent agents calling bootstrap_agent()
  - Parameterized: can run 10, 50, 100, or custom concurrency

- [ ] **AC-2:** Concurrent agent simulation uses `Promise.all()` to execute in parallel
  - Each concurrent agent calls: `bootstrap_agent(domain, role)` with valid parameters
  - No sequential delays between agent starts (true parallelism)

- [ ] **AC-3:** Load test measures latency per request:
  - Start timestamp (process.hrtime() or Date.now())
  - End timestamp
  - Latency in milliseconds

- [ ] **AC-4:** Latency aggregation calculates percentiles:
  - p50 (50th percentile — median)
  - p95 (95th percentile — SLA target)
  - p99 (99th percentile — quality target)
  - min, max, mean

- [ ] **AC-5:** Load test supports configurable workload:
  ```typescript
  // Example
  await runLoadTest({
    concurrency: 50,
    queries_per_agent: 20,
    domain: "engineering",
    role: "backend_developer"
  });
  ```

- [ ] **AC-6:** Load test output includes:
  - Total queries executed
  - Success rate (%)
  - Lock timeout count (SQLite specific)
  - Detailed percentile breakdown

- [ ] **AC-7:** Load test is repeatable:
  - Same parameters → consistent results (±5% variance acceptable)
  - No random failures from test infrastructure (all failures are real)

### Latency Validation (AC-8 through AC-13)

- [ ] **AC-8:** Single query latency < 20ms (p50)
  - Baseline to understand minimum overhead

- [ ] **AC-9:** p95 latency < 50ms @ 50 concurrent agents (MANDATORY SLA)
  - This is the critical performance target
  - If p95 > 50ms: Task fails, requires investigation

- [ ] **AC-10:** p99 latency < 100ms @ 50 concurrent agents (quality target)
  - Acceptable quality threshold for 99th percentile

- [ ] **AC-11:** 100 concurrent agents: p95 latency < 60ms (stress test)
  - Graceful degradation under extreme load
  - Should not crash or timeout

- [ ] **AC-12:** Lock timeout rate < 5% (SQLite WAL mode health)
  - Indicates database lock contention
  - High rate = index/schema performance issue

- [ ] **AC-13:** Zero service errors during load test
  - Bootstrap_agent should not crash under 50+ concurrent load
  - All 1000 queries succeed (or acceptable failure rate < 0.1%)

### Regression Detection (AC-14 through AC-18)

- [ ] **AC-14:** Performance baseline snapshot saved in `performance-baseline.json`
  ```json
  {
    "timestamp": "2026-03-09T09:00:00Z",
    "baseline": {
      "p50_ms": 4.2,
      "p95_ms": 35.1,
      "p99_ms": 71.3,
      "concurrency": 50,
      "total_queries": 1000
    }
  }
  ```

- [ ] **AC-15:** Baseline file committed to git (for regression detection)
  - Path: `test-results/performance-baseline.json`
  - Version controlled for comparison in CI/CD

- [ ] **AC-16:** CI/CD regression gate implemented (GitHub Actions or npm script)
  - Trigger: Every PR that modifies `src/mcp/*` or `src/metadata/*`
  - Check: Run load test, compare p95 to baseline
  - Fail if: p95 > baseline × 1.1 (10% degradation threshold)
  - Pass if: p95 ≤ baseline × 1.1

- [ ] **AC-17:** Regression gate script is `npm run test:performance`
  - Runs load test, compares to baseline, exits with status code

- [ ] **AC-18:** Load test generates detailed metrics file
  - Path: `test-results/load-test-results-{timestamp}.md`
  - Contents: latency breakdown, concurrency level, success rate

### Documentation & CI/CD Integration (AC-19 through AC-22)

- [ ] **AC-19:** Performance SLA documented in `PERFORMANCE-SLA.md`
  - p95 < 50ms, p99 < 100ms targets
  - Monitoring thresholds (healthy, warning, critical)
  - Mitigation steps for breach

- [ ] **AC-20:** Load test readme created (`LOAD-TEST-README.md`)
  - How to run: `npm run test:load`
  - How to update baseline: `npm run test:performance:baseline`
  - Interpretation guide (when to investigate)

- [ ] **AC-21:** GitHub Actions workflow added (`.github/workflows/performance-regression.yml`)
  - Runs on: PR to main, push to main
  - Steps: Run load test, compare baseline, comment on PR
  - Fail if threshold exceeded

- [ ] **AC-22:** All tests passing locally before CI submission
  - `npm test` passes ✅
  - `npm run test:load` runs successfully ✅
  - Load test results within SLA ✅

---

## 🛠️ Implementation Phases

### Phase 1: Load Test Harness (1.5h)

**Goal:** Create `src/tests/e2e/load-test.ts` with concurrent agent simulation

```typescript
// Signature
export interface LoadTestConfig {
  concurrency: number;
  queries_per_agent: number;
  domain?: string;
  role?: string;
}

export interface LoadTestResult {
  total_queries: number;
  success_rate: number;
  latencies: number[];  // Array of ms per query
  percentiles: {
    p50: number;
    p95: number;
    p99: number;
    min: number;
    max: number;
    mean: number;
  };
  lock_timeouts: number;
}

export async function runLoadTest(config: LoadTestConfig): Promise<LoadTestResult>
```

**Deliverable:**
- [ ] Load test harness complete (concurrent agent simulation)
- [ ] Uses Promise.all() for true parallelism
- [ ] Measures & aggregates latencies
- [ ] Calculates percentiles (p50, p95, p99)
- [ ] Unit tests: 3-4 test cases (mock agent, verify percentile calc)

---

### Phase 2: Latency Validation & Baseline (1.5h)

**Goal:** Create baseline measurement (50 concurrent agents) + verify SLA

```typescript
// Main load test runner
async function main() {
  console.log('Starting load test: 50 concurrent agents...');
  
  const result = await runLoadTest({
    concurrency: 50,
    queries_per_agent: 20,
    domain: "engineering",
    role: "backend_developer"
  });
  
  // Validate SLA
  if (result.percentiles.p95 > 50) {
    console.error('❌ SLA VIOLATION: p95 > 50ms');
    process.exit(1);
  }
  
  console.log('✅ SLA PASSED: p95 =', result.percentiles.p95, 'ms');
  
  // Save baseline
  saveBaseline(result, 'test-results/performance-baseline.json');
}
```

**Deliverable:**
- [ ] Baseline measurement recorded (p50, p95, p99)
- [ ] SLA validation logic (p95 < 50ms check)
- [ ] Performance-baseline.json saved
- [ ] Integration tests: 3-4 test cases (stress test @ 100 concurrent)

---

### Phase 3: Regression Detection & CI/CD (1.5h)

**Goal:** Create regression gate + GitHub Actions workflow

**Regression Gate Script:**
```bash
#!/bin/bash
# npm run test:performance

npm run test:load > test-results/load-test-current.md

# Read baseline
BASELINE_P95=$(grep "p95" test-results/performance-baseline.json | awk '{print $2}')

# Read current
CURRENT_P95=$(grep "P95" test-results/load-test-current.md | awk '{print $3}')

# Calculate threshold (baseline × 1.1)
THRESHOLD=$(echo "$BASELINE_P95 * 1.1" | bc)

if (( $(echo "$CURRENT_P95 > $THRESHOLD" | bc -l) )); then
  echo "❌ REGRESSION DETECTED: p95 $CURRENT_P95 > $THRESHOLD (baseline $BASELINE_P95)"
  exit 1
else
  echo "✅ No regression: p95 $CURRENT_P95 <= $THRESHOLD"
  exit 0
fi
```

**GitHub Actions Workflow:**
```yaml
# .github/workflows/performance-regression.yml
name: Performance Regression Check

on: [pull_request, push]

jobs:
  performance:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-node@v3
        with:
          node-version: 18
      - run: npm ci
      - run: npm run test:performance
        continue-on-error: true
      - name: Comment on PR
        if: failure()
        run: |
          echo "⚠️ Performance regression detected. Check workflow logs."
```

**Deliverable:**
- [ ] Regression gate script (`npm run test:performance`)
- [ ] GitHub Actions workflow (`.github/workflows/performance-regression.yml`)
- [ ] Baseline comparison logic (within 10% threshold)
- [ ] PR comment integration (optional, but nice)

---

### Phase 4: Documentation & Testing (0.5h)

**Goal:** Document SLA, README, finalize tests

**PERFORMANCE-SLA.md Creation:**
```markdown
# Performance SLA - JoineryTech MCP Server

## Bootstrap Agent Latency

| Metric | Target | Load | Status |
|:-------|:------:|:----:|:------:|
| p50 | < 20ms | Single | Baseline |
| p95 | < 50ms | 50 concurrent | MANDATORY |
| p99 | < 100ms | 50 concurrent | Quality |

## Monitoring Thresholds

| Intensity | p95 | Action |
|:----------|:---:|:-------|
| Healthy | < 45ms | None |
| Warning | 45-50ms | Investigate |
| Critical | > 50ms | SLA Breach |
```

**LOAD-TEST-README.md Creation:**
```markdown
# Load Testing Guide

## Quick Start

```bash
# Run load test
npm run test:load

# Update baseline
npm run test:performance:baseline

# Check for regression
npm run test:performance
```

## Interpretation

- p95 < 50ms → OK
- 50ms ≤ p95 < 60ms → Warning (investigate)
- p95 ≥ 60ms → SLA Violation (requires fix)
```

**Deliverable:**
- [ ] PERFORMANCE-SLA.md created + integrated
- [ ] LOAD-TEST-README.md created + referenced
- [ ] Load test results file generation
- [ ] E2E tests: 5-7 test cases (various concurrency levels)

---

## 📦 Files to Create/Modify

| File | Type | Lines | Purpose |
|:-----|:----:|:-----:|:--------|
| `src/tests/e2e/load-test.ts` | NEW | ~150 | Load test harness |
| `src/tests/e2e/load-test.test.ts` | NEW | ~120 | Load test validation |
| `test-results/performance-baseline.json` | NEW | ~20 | Baseline snapshot |
| `npm run test:load` | SCRIPT | ~15 | Run load test |
| `npm run test:performance` | SCRIPT | ~20 | Regression check |
| `.github/workflows/performance-regression.yml` | NEW | ~30 | CI/CD automation |
| `Docs/mcp-context-server/PERFORMANCE-SLA.md` | NEW | ~50 | SLA documentation |
| `Docs/LOAD-TEST-README.md` | NEW | ~60 | Load test guide |

---

## 🧪 Test Strategy

### Unit Tests (3-4 tests)
- Percentile calculation correctness (p50, p95, p99)
- Latency aggregation
- Config validation

### Load Tests (5+ tests)
- 10 concurrent agents (baseline verification)
- 50 concurrent agents (SLA validation) ← **CRITICAL**
- 100 concurrent agents (stress test / graceful degradation)
- 1000 queries total (endurance test)
- Lock timeout handling (SQLite WAL mode)

### Regression Tests (2+ tests)
- Compare current to baseline (within ±10%)
- Detect ≥10% degradation
- Report failure with actionable feedback

### CI/CD Tests (2+ tests)
- GitHub Actions workflow trigger
- Baseline comparison on PR
- Success/failure status propagation

---

## 📊 Success Metrics

| Metric | Target | Check |
|:-------|:------:|:-----:|
| AC Completion | 22/22 | [ ] |
| p95 Latency | < 50ms @ 50 concurrent | [ ] |
| p99 Latency | < 100ms @ 50 concurrent | [ ] |
| Baseline Locked | 1 JSON snapshot | [ ] |
| CI/CD Gate | GitHub Actions | [ ] |
| Test Coverage | ≥10 load tests | [ ] |

---

## 🔗 Key References

- [PERFORMANCE-SLA.md](../../Docs/mcp-context-server/PERFORMANCE-SLA.md) — SLA you're validating
- [bootstrap_agent.md](../../Docs/tools/bootstrap_agent.md) — Tool you're testing
- [EPIC-10-ADR.md](../../database/standards/adrs/EPIC-10-ADR.md) — Performance baseline design decision
- [LOAD-TEST-RESULTS-2026-03-06.md](../../LOAD-TEST-RESULTS-2026-03-06.md) — Previous baseline reference

---

## 📞 Daily Standups (Solo Format if Needed)

### Day 1 (2026-03-09)
**Goal:** Load test harness + baseline measurement, 50% SLA validation

**Standup at 09:00 UTC:**
- Yesterday: None (start day)
- Today: Build load test harness (Promise.all, latency measurement, percentiles)
- Blockers: None expected

**Standup at 12:00 UTC:**
- Progress: Load test harness done, running initial baseline test
- Next: SLA validation, ensure p95 < 50ms

**Standup at 18:00 UTC:**
- Status: Baseline measured (p95 ≈ 35ms, well within SLA) ✅
- Tomorrow: Regression detection + CI/CD gate

### Day 2 (2026-03-10)
**Goal:** CI/CD integration complete, all 22 AC verified, ready for merge

**Standup at 09:00 UTC:**
- Yesterday: Baseline locked at p95 ≈ 35ms
- Today: Regression gate + GitHub Actions + documentation
- Blockers: None expected

**Standup at 12:00 UTC:**
- Progress: Regression gate script done, GitHub Actions workflow created
- Next: Documentation (SLA + README), final validation

**Standup at 18:00 UTC (PRE-MERGE CHECK):**
- Status: ALL 22 AC VERIFIED ✅
- p95 latency: 35ms (< 50ms target) ✅
- Baseline locked: performance-baseline.json ✅
- CI/CD gate: GitHub Actions operational ✅
- Ready for merge? YES → Create PR, link to TASK-10-07

---

## 🎬 Quick Start Checklist (Copy-Paste Ready)

```
═══════════════════════════════════════════════════════════
  DEV B — TASK-10-07 QUICK START (5h, 22 AC)
═══════════════════════════════════════════════════════════

[ ] 09:00 Day 1: Read this prompt fully + review TASK-10-07.md
[ ] 09:30 Day 1: Create load-test.ts (concurrent simulation)
[ ] 11:00 Day 1: Implement percentile calculation + latency measurement
[ ] 12:00 Day 1: STANDUP (harness check)
[ ] 12:30 Day 1: Run baseline test (50 concurrent agents)
[ ] 13:30 Day 1: Verify p95 < 50ms SLA + save baseline.json
[ ] 15:00 Day 1: Create load test unit tests
[ ] 18:00 Day 1: STANDUP (baseline checkpoint)
[ ] 18:30 Day 1: Prep CI/CD regression gate

[ ] 09:00 Day 2: Create regression gate script (npm run test:performance)
[ ] 10:00 Day 2: Create GitHub Actions workflow
[ ] 12:00 Day 2: STANDUP (CI/CD check)
[ ] 12:30 Day 2: Create PERFORMANCE-SLA.md + LOAD-TEST-README.md
[ ] 13:30 Day 2: Final AC verification (22/22)
[ ] 14:00 Day 2: Create PR + merge request
[ ] 18:00 Day 2: FINAL STANDUP (merge status)

═══════════════════════════════════════════════════════════
```

---

## 🚀 How to Execute

### Step 1: Branch Creation
```bash
git checkout -b dev-b/task-10-07-performance
```

### Step 2: Skeleton Files
```bash
touch src/tests/e2e/load-test.ts
touch src/tests/e2e/load-test.test.ts
touch .github/workflows/performance-regression.yml
```

### Step 3: Implementation Order (Phase by phase)
1. Load test harness (concurrent, latency measurement)
2. Percentile calculation (p50, p95, p99)
3. Baseline measurement (50 concurrent agents)
4. Regression gate (compare to baseline × 1.1)
5. GitHub Actions workflow
6. Documentation (SLA, README)

### Step 4: Continuous Testing
```bash
npm run test:load
npm run test:performance
npm test -- src/tests/e2e/load-test.test.ts
```

### Step 5: Pre-Merge
```bash
npm test                   # All tests passing?
npm run test:load          # Load test succeeds?
npm run test:performance   # Regression gate passes?
npm run lint              # No TypeScript errors?
npm run build             # Compiles?
```

### Step 6: Create PR
- Title: `feat(TASK-10-07): Performance load testing & SLA validation`
- Description: Link to TASK-10-07.md, baseline p95, AC checklist
- Request review from: Tech Lead

---

## 📚 Implementation Guidance

### Load Test Harness Pattern
```typescript
export async function runLoadTest(config: LoadTestConfig): Promise<LoadTestResult> {
  const latencies: number[] = [];
  
  // Create N concurrent agents
  const agents = Array.from({ length: config.concurrency }, (_, i) => 
    (async () => {
      for (let q = 0; q < config.queries_per_agent; q++) {
        const start = process.hrtime.bigint();
        
        // Call bootstrap_agent
        await bootstrap_agent({
          domain: config.domain || 'engineering',
          role: config.role || 'backend_developer'
        });
        
        const end = process.hrtime.bigint();
        const latencyMs = Number(end - start) / 1_000_000;
        latencies.push(latencyMs);
      }
    })()
  );
  
  // Run all in parallel
  await Promise.all(agents);
  
  // Calculate percentiles
  return {
    total_queries: latencies.length,
    success_rate: 100,
    latencies,
    percentiles: calculatePercentiles(latencies),
    lock_timeouts: 0
  };
}

// Percentile calculation
function calculatePercentiles(latencies: number[]) {
  const sorted = latencies.sort((a, b) => a - b);
  return {
    p50: sorted[Math.floor(sorted.length * 0.50)],
    p95: sorted[Math.floor(sorted.length * 0.95)],
    p99: sorted[Math.floor(sorted.length * 0.99)],
    min: sorted[0],
    max: sorted[sorted.length - 1],
    mean: sorted.reduce((a, b) => a + b, 0) / sorted.length
  };
}
```

### Regression Gate Pattern
```typescript
// Compare to baseline
function checkRegression(currentP95: number, baselineP95: number): boolean {
  const threshold = baselineP95 * 1.1; // 10% degradation threshold
  if (currentP95 > threshold) {
    console.error(`❌ REGRESSION: ${currentP95}ms > ${threshold}ms (baseline: ${baselineP95}ms)`);
    return false;
  }
  console.log(`✅ No regression: ${currentP95}ms <= ${threshold}ms`);
  return true;
}
```

### Baseline JSON Pattern
```json
{
  "timestamp": "2026-03-09T09:00:00Z",
  "baseline": {
    "p50_ms": 4.2,
    "p95_ms": 35.1,
    "p99_ms": 71.3,
    "concurrency": 50,
    "total_queries": 1000,
    "success_rate": 100,
    "lock_timeouts": 0
  }
}
```

---

## ⏱️ Time Budget (5 hours)

| Phase | Time | Actual |
|:------|:----:|:-----:|
| Phase 1: Load Test Harness | 1.5h | [ ] |
| Phase 2: Latency Validation & Baseline | 1.5h | [ ] |
| Phase 3: Regression & CI/CD | 1.5h | [ ] |
| Phase 4: Documentation | 0.5h | [ ] |
| **TOTAL** | **5.0h** | **[ ]** |

---

## ✅ Final Checklist Before Merge

- [ ] All 22 AC verified manually
- [ ] Load test harness complete (Promise.all used)
- [ ] p95 < 50ms verified @ 50 concurrent ✅
- [ ] p99 < 100ms verified @ 50 concurrent ✅
- [ ] performance-baseline.json saved & committed
- [ ] Regression gate script (npm run test:performance)
- [ ] GitHub Actions workflow created
- [ ] PERFORMANCE-SLA.md created
- [ ] LOAD-TEST-README.md created
- [ ] 0 TypeScript errors
- [ ] All tests passing (`npm test`)
- [ ] Load test results file generated
- [ ] Code compiled (`npm run build`)
- [ ] PR created with clear description
- [ ] Tech Lead notified for review

---

**Ready to start? Copy this file, paste into your terminal, and execute!** 🚀

*Last Updated: 2026-03-06*

