---
id: EPIC-14-QA-TEST-STRATEGY
title: "EPIC-14 Modern MCP Transports — Comprehensive QA Test Strategy"
type: qa-test-strategy
epic: EPIC-14
milestone: M02
date: 2026-03-08
targetCoverage: "85%+"
status: "✅ STRATEGY_READY"
---

# EPIC-14 QA Test Strategy — Modern MCP Transports

**Prepared By:** QA Tester Agent
**Date:** 2026-03-08
**Test Scope:** 65+ test cases across unit, integration, E2E, chaos
**Target Coverage:** 85%+
**Estimated Effort:** 40-50 hours (includes test implementation + execution)

---

## QA Test Objectives

1. ✅ **Transport Abstraction Layer:** Both stdio + HTTP transports work identically for tool calls (zero behavior delta)
2. ✅ **Graceful Shutdown:** Server shuts down cleanly ≤2s, connections drain properly
3. ✅ **Plugin System:** Dynamic loading + dependency resolution works; error recovery prevents crashes
4. ✅ **Resource Templates:** URI resolution + versioning + caching works correctly
5. ✅ **Debouncing Notifications:** Event aggregation reduces notification volume 50%+
6. ✅ **Error Handling:** Transport-specific errors properly classified + actionable

---

## Test Matrix

### UNIT TESTS (30 test cases)

**Category 1: Transport Factory & Configuration (8 tests)**

| Test ID | Scenario | Input | Expected | Pass Criteria |
|:--------|:---------|:-----:|:--------:|:-------------:|
| UT-01 | Valid stdio config | `{type: "stdio"}` | Transport created | ✓ Instance returned |
| UT-02 | Valid HTTP config | `{type: "http", port: 8080}` | HTTPTransport(8080) | ✓ Instance + port set |
| UT-03 | Invalid transport type | `{type: "htp"}` | ConfigError thrown | ✓ Error code = CONFIG_INVALID |
| UT-04 | HTTP port < 1 (negative values) | `{type: "http", port: -1}` | ConfigError thrown | ✓ Validation rejects |
| UT-04a | HTTP port 0 (ephemeral) | `{type: "http", port: 0}` | HTTPTransport created | ✓ Defaults to ephemeral port |
| UT-05 | HTTP port > 65535 | `{type: "http", port: 99999}` | ConfigError thrown | ✓ Validation rejects |
| UT-06 | Missing port for HTTP | `{type: "http"}` | HTTPTransport(default:3000) | ✓ Default port used |
| UT-07 | Transport state enum | Transport created | State = INITIALIZING | ✓ Initial state correct |
| UT-08 | Enum prevents typos | `TransportType.STDIIO` | (compile-time error) | ✓ TypeScript catches |

**Category 2: Plugin Loader & Dependencies (10 tests)**

| Test ID | Scenario | Input | Expected | Pass Criteria |
|:--------|:---------|:-----:|:--------:|:-------------:|
| UT-09 | Load single plugin | `loadPlugin("bootstrap")` | Plugin registered | ✓ bootstrap tools available |
| UT-10 | Plugin dependency met | `{name: "memory", deps: ["bootstrap"]}` | Loads bootstrap first | ✓ No circular dep error |
| UT-11 | Circular dependency | `{a: {deps: [b]}, b: {deps: [a]}}` | CircularDependencyError | ✓ Error thrown immediately |
| UT-12 | Missing dependency | `{name: "discovery", deps: ["undefined"]}` | PluginLoadError | ✓ No fallback, fails fast |
| UT-13 | Plugin versioning | `loadPlugin("bootstrap@1.2.0")` | v1.2.0 loaded | ✓ Version respected |
| UT-14 | Version mismatch | `loadPlugin("discovery@3.0")` (v2 installed) | VersionConflictError | ✓ Error thrown |
| UT-15 | Plugin onInit hook | Plugin loaded → onInit() called | Setup completes | ✓ Hook invoked, no timeout |
| UT-16 | Plugin onDestroy hook | Unload plugin → onDestroy() | Cleanup completes | ✓ Hook invoked cleanly |
| UT-17 | Plugin registry | 50 tools loaded | registerTool() called 50x | ✓ All tools available |
| UT-18 | Plugin health status | getPluginStatus() | {healthy: [...], failed: [...]} | ✓ Correct lists returned |

**Category 3: Graceful Shutdown State Machine (8 tests)**

| Test ID | Scenario | Input | Expected | Pass Criteria |
|:--------|:---------|:-----:|:--------:|:-------------:|
| UT-19 | Initial state | Server starts | State = CONNECTED | ✓ Ready for requests |
| UT-20 | SIGTERM flag set | Send SIGTERM | shuttingDown=true | ✓ Flag flipped <100ms |
| UT-21 | Shutdown timeout | Wait 30s for requests | Remaining requests killed | ✓ Force close after 30s |
| UT-22 | Active connections tracked | 5 requests in flight | activeConnections.size=5 | ✓ Count accurate |
| UT-23 | Idle connection close | No request for 5s during shutdown | Connection destroyed | ✓ Idle closed early |
| UT-24 | Connection draining | 3 requests finish mid-shutdown | All complete before exit | ✓ No orphaned requests |
| UT-25 | Health check HEALTHY | Server running | /health → {status: "HEALTHY"} | ✓ 200 OK + status field |
| UT-26 | Health check SHUTTING_DOWN | Shutdown in progress | /health → {status: "SHUTTING_DOWN"} | ✓ 503 + status field |

**Category 4: Notification Debouncing (4 tests)**

| Test ID | Scenario | Input | Expected | Pass Criteria |
|:--------|:---------|:-----:|:--------:|:-------------:|
| UT-27 | Rapid events aggregated | 100 TOOL_REGISTERED events in 50ms | 1 notification sent | ✓ Aggregated (99% reduction) |
| UT-28 | Critical event not debounced | 1 CRITICAL_ERROR event | Sent immediately | ✓ Immediate send (no debounce) |
| UT-29 | Debounce timer reset | Event @ 0ms, event @ 50ms → timer resets | Notification @ 150ms | ✓ Timer resets per event |
| UT-30 | Manual flush | Call flush() during debounce | Notification sent immediately | ✓ Flush forces send |

---

### INTEGRATION TESTS (15 test cases)

**Category 1: Cross-Transport Consistency (6 tests)**

| Test ID | Scenario | Execution | Expected | Pass Criteria |
|:---------|:---------|:----------|:--------:|:-------------:|
| IT-01 | Same tool call via stdio | Call `getRoles()` via stdio | Returns all roles | ✓ Match baseline |
| IT-02 | Same tool call via HTTP | Call `getRoles()` via HTTP:8080 | Returns all roles | ✓ Match stdio result |
| IT-03 | Tool call result consistency | REST /tools/getRoles vs stdio | Field-for-field identical | ✓ No delta |
| IT-04 | Error propagation stdin | Tool throws "INVALID_ROLE" | Client receives error | ✓ Error passed through |
| IT-05 | Error propagation HTTP | Tool throws error via HTTP | HTTP 400 + error JSON | ✓ Proper HTTP mapping |
| IT-06 | Concurrent requests (mixed) | 10 stdio + 10 HTTP requests | All finish successfully | ✓ No interference |

**Category 2: Plugin Lifecycle Integration (4 tests)**

| Test ID | Scenario | Execution | Expected | Pass Criteria |
|:---------|:---------|:----------|:--------:|:-------------:|
| IT-07 | Plugin onInit side effects | Load plugin → database migration runs | Migration succeeds | ✓ onInit called + completed |
| IT-08 | Plugin to plugin ordering | Load discovery (depends on bootstrap) | bootstrap loaded first | ✓ Dependency respected |
| IT-09 | Optional plugin failure | Load memory (optional) → fails | Server starts without memory | ✓ Graceful degradation |
| IT-10 | Plugin health after error | Optional plugin fails → getPluginStatus() | Status shows [bootstrap, discovery], failed: [memory] | ✓ Correct lists |

**Category 3: Resource Template Integration (3 tests)**

| Test ID | Scenario | Execution | Expected | Pass Criteria |
|:---------|:---------|:----------|:--------:|:-------------:|
| IT-11 | Resource URI validation | Resolve `resource://discovery/invalid_phase` | ResourceNotFoundError | ✓ Validation enforced |
| IT-12 | Resource caching | Resolve same URI 10x | DB queried once (others from cache) | ✓ Cache hit verified |
| IT-13 | Resource versioning | Resolve `resource://discovery/ideation?version=1` vs `?version=2` | Different responses | ✓ Version routing works |

**Category 4: Transport Error Context (2 tests)**

| Test ID | Scenario | Execution | Expected | Pass Criteria |
|:---------|:---------|:----------|:--------:|:-------------:|
| IT-14 | Stdio EPIPE error | Pipe broken mid-transmission | diagnoseError() returns EPIPE | ✓ Code = "EPIPE" |
| IT-15 | HTTP PORT_IN_USE error | Port 8080 already bound | diagnoseError() returns PORT_IN_USE | ✓ Code = "PORT_IN_USE" |

---

### END-TO-END TESTS (12 test cases)

**Category 1: Full Shutdown Flow (5 tests)**

| Test ID | Scenario | Execution | Expected | Pass Criteria |
|:--------|:----------|:----------|:---------:|:----------------:|
| E2E-01 | SIGTERM → 503 → drain → exit | 1. Send SIGTERM 2. Check /health 3. Wait for drains 4. Observe exit | Total time ≤2s | ✓ Graceful shutdown SLA |
| E2E-02 | Keep-alive drain (HTTP) | SIGTERM + keep-alive connection open | Connection closes cleanly (Connection: close header) | ✓ No hanging socket |
| E2E-03 | Pending request completes | Start slow request (5s) → SIGTERM → request completes | Request completes before process exit | ✓ Drain waits for pending |
| E2E-04 | Timeout expires | Start 100s request → SIGTERM → wait 30s | Request force-closed at 30s (not 100s) | ✓ Timeout enforced |
| E2E-05 | Multiple transports shutdown | Start both stdio + HTTP → SIGTERM | Both cleanly close | ✓ No orphaned transports |

**Category 2: Full Plugin Workflow (4 tests)**

| Test ID | Scenario | Execution | Expected | Pass Criteria |
|:--------|:----------|:----------|:---------:|:----------------:|
| E2E-06 | Seeder loads 1000 roles | Seeder runs → plugins lifecycle hooks invoked | 1. bootstrap onInit 2. discovery onInit 3. roles loaded → 1 notification sent | ✓ Full workflow works |
| E2E-07 | Plugin failure recovery | Plugin X fails → optional → server continues → admin reloads plugin | Server starts without X, then X available after reload | ✓ Partial recovery works |
| E2E-08 | Cross-plugin communication | discovery tool calls context.getRoleContext() | Returns from context plugin | ✓ Plugin-to-plugin works |
| E2E-09 | Plugin versioning conflict | Deploy plugin v2 with breaking changes | v1 requests fail gracefully; upgraded clients get v2 | ✓ Version safety |

**Category 3: Cross-Transport Tool Execution (2 tests)**

| Test ID | Scenario | Execution | Expected | Pass Criteria |
|:--------|:----------|:----------|:---------:|:----------------:|
| E2E-10 | Complex tool call via stdio | Call `createEpisode({...detailed nested data...})` | Episode created with all fields intact | ✓ No data loss |
| E2E-11 | Complex tool call via HTTP | Same call via POST /tools/createEpisode | Episode identical to stdio version | ✓ Field-for-field match |

**Category 4: Error Recovery (1 test)**

| Test ID | Scenario | Execution | Expected | Pass Criteria |
|:--------|:----------|:----------|:---------:|:----------------:|
| E2E-12 | Transport fails + recovers | Simulate network failure (/dev/null block) → auto-retry → success | Tool request retried (up to 3x), eventually succeeds | ✓ Retry logic works |

---

### CHAOS & STRESS TESTS (8 test cases)

**Category 1: Connection Stress (3 tests)**

| Test ID | Scenario | Load | Expected | Pass Criteria |
|:--------|:----------|:-----:|:--------:|:-------------:|
| CHAOS-01 | 100 concurrent HTTP requests | 100 concurrent GET /tools | All 100 complete in <5s | ✓ Throughput ≥20 req/s |
| CHAOS-02 | Keep-alive connection limit | 50 keep-alive connections, 51st arrives | 51st queued or rejected properly | ✓ No crash, error code set |
| CHAOS-03 | Connection spike + SIGTERM | Ramp to 100 concurrent + SIGTERM | Graceful drain with active conns, exit <2s | ✓ Drain under load works |

**Category 2: Event Aggregation Stress (2 tests)**

| Test ID | Scenario | Load | Expected | Pass Criteria |
|:--------|:----------|:-----:|:--------:|:-------------:|
| CHAOS-04 | 10000 events in 1s | Rapid TOOL_REGISTERED events | Notifications aggregated, final ratio ≥50% | ✓ Aggregation holds under load |
| CHAOS-05 | Mixed event types | 5000 regular + 50 CRITICAL_ERROR | CRITICAL_ERROR sent individually; regulars aggregated | ✓ Classification respected |

**Category 3: Plugin Load Stress (2 tests)**

| Test ID | Scenario | Load | Expected | Pass Criteria |
|:--------|:----------|:-----:|:--------:|:-------------:|
| CHAOS-06 | Load all 7 plugins in parallel | Parallel import() of bootstrap, context, discovery, memory, legacy, admin, sampling | All loaded within 500ms | ✓ No serialization bottleneck |
| CHAOS-07 | Plugin load with cache miss | Cold start (no cache) → load all plugins | Repeated loads use cached module metadata | ✓ 2nd load ≥2x faster |

**Category 4: Resource Cache Stress (1 test)**

| Test ID | Scenario | Load | Expected | Pass Criteria |
|:--------|:----------|:-----:|:--------:|:-------------:|
| CHAOS-08 | 1000 resource URI lookups | Repeated resolves of resource://discovery/* | Cache hit rate ≥80% | ✓ Cache effectiveness proven |

---

## Test Data Requirements

### Seed Data

- **Roles:** 100 roles across 5 domains (engineering, management, discovery, explorer, agentops)
- **Episodes:** 50 episodes for memory cache testing
- **Workflows:** 20 discovery workflows for resource testing

### Cleanup Strategy

- ✅ Each test: Fresh database transaction (rollback after completion)
- ✅ Transport tests: Socket cleanup (no TIME_WAIT sockets hanging)
- ✅ Plugin tests: Module cache reset (reload fresh for isolation)

---

## Coverage Targets & Acceptance Criteria

| Component | Target | Baseline | Status |
|:----------|:------:|:--------:|:------:|
| Transport API | 85%+ | TBD | 🔄 To Calculate |
| Plugin Loader | 90%+ | TBD | 🔄 To Calculate |
| Graceful Shutdown | 88%+ | TBD | 🔄 To Calculate |
| Error Handling | 80%+ | TBD | 🔄 To Calculate |
| **OVERALL** | **85%+** | **TBD** | 🔄 To Calculate |

**Go/No-Go:** Coverage ≥85% for all components, no P0 failures, flaky tests <3%

---

## Test Execution Schedule

### Week 1 (2026-03-31 to 2026-04-03, Dev Days 1-5)

- **Mon 3/31:** Unit tests (Categories 1-4): 30 tests, ~2+1h dev write + run
- **Tue 4/01:** Integration tests (Categories 1-3): 15 tests, ~1+0.5h
- **Wed 4/02:** E2E tests (Categories 1-4): 12 tests, ~1.5+1h
- **Thu 4/03:** Chaos tests (all 8), ~1+1h

### Week 2 (2026-04-07 to 2026-04-09, Dev Days 6-8)

- **Mon 4/07:** Coverage gap analysis; add missing tests
- **Tue 4/08:** Final regression run; document issues
- **Wed 4/09:** QA sign-off + release readiness confirmation

---

## Risk Mitigations

### Risk 1: Cross-Transport Behavior Inconsistency

**Mitigation:** IT-01-06 (cross-transport consistency matrix) must achieve 100% field match. If delta detected, escalate before merge.

### Risk 2: Graceful Shutdown SLA Miss (>2s)

**Mitigation:** E2E-01-05 stress tests with 100+ concurrent conns. If SLA misses, profile for bottleneck (connection close, database query, etc.)

### Risk 3: Plugin Circular Dependencies Not Detected

**Mitigation:** UT-11 forces circular dependency scenario. If test passes but detection misses, plugin loader unsafe for production.

### Risk 4: Resource Cache Stale Reads

**Mitigation:** IT-13 + CHAOS-08 verify cache invalidation. If stale data observed, enforce TTL reduction or manual flush on schema change.

---

## QA Sign-Off Criteria

**Before EPIC-14 closure, ALL of the following must pass:**

- [ ] ✅ 30 unit tests PASS
- [ ] ✅ 15 integration tests PASS
- [ ] ✅ 12 E2E tests PASS
- [ ] ✅ 8 chaos tests PASS
- [ ] ✅ **Total: 65 tests PASS** (0 failures, flaky <3%)
- [ ] ✅ Coverage ≥85% on transport + plugin modules
- [ ] ✅ Graceful shutdown SLA ≤2s (measured, not assumed)
- [ ] ✅ Debouncing efficiency ≥50% (events aggregated)
- [ ] ✅ Cross-transport behavior delta = 0%
- [ ] ✅ No P0 issues (critical path blockers)
- [ ] ✅ ≤3 P1 issues (must fix before release)
- [ ] ✅ Performance regressions <5% vs baseline

**QA Sign-Off Statement:** "EPIC-14 APPROVED for production" OR "EPIC-14 BLOCKED: issues [list]"

---

## Test Tooling & Frameworks

- **Unit/Integration:** Jest (existing, familiar)
- **E2E:** Playwright (existing, web automation)
- **Chaos:** Custom Node.js scripts (socket manipulation, timing injection)
- **Metrics:** Prometheus (optional, helpful for debounce efficiency)

---

## Success Scenario (Example Report)

```
EPIC-14 QA Test Report — 2026-04-09
=====================================

Unit Tests: 30/30 PASS ✅
Integration Tests: 15/15 PASS ✅
E2E Tests: 12/12 PASS ✅
Chaos Tests: 8/8 PASS ✅

TOTAL: 65/65 PASS (100%) ✅

Coverage Report:
  Transport API: 87% ✅
  Plugin Loader: 91% ✅
  Graceful Shutdown: 89% ✅
  Error Handling: 84% ✅
  Overall: 88% ✅

Performance:
  Graceful Shutdown SLA: 1.2s (target: ≤2s) ✅
  Debouncing Efficiency: 62% reduction (target: ≥50%) ✅
  Cross-Transport Consistency: 0% delta ✅

Critical Issues: 0 ✅
High Issues: 0 ✅
Medium Issues: 1 (resource cache TTL recommendation, non-blocking)

QA Sign-Off: ✅ APPROVED FOR PRODUCTION

Signed: QA Tester Agent
Date: 2026-04-09
```

---

**Document:** EPIC-14-QA-TEST-STRATEGY
**Status:** ✅ **STRATEGY_READY**
**Prepared by:** QA Tester Agent
**Date:** 2026-03-08
