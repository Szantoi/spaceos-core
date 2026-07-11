---
title: "🚀 Dev A — EPIC-14 Phase 1 Kickoff"
subtitle: "Transport Abstraction Sprint (March 18-21, 2026)"
created: 2026-03-09
epic: "EPIC-14"
phase: "Phase 1"
owner: "Dev A"
team: "Dev A (Lead), Dev C (Support)"
decision_warrant: "TECH-LEAD-EPIC-14-DECISION-WARRANT_2026-03-09.md ✅ APPROVED"
---

# 🚀 Dev A — EPIC-14 Phase 1 Sprint Kickoff

**Sprint Duration:** March 18-21, 2026 (4.5 calendar days)
**Your Role:** Dev A (Critical Path Lead)
**Your Tasks:** 3 core tasks (TASK-14-01, TASK-14-02, TASK-14-05) = 36 hours effort
**Success Criteria:** All 3 tasks complete, 80%+ test coverage, EPIC-11 agents working unchanged

---

## 📋 Executive Summary

After completing EPIC-11 (FSM, RBAC, Error Handling) and EPIC-10 (OWASP validation, error standardization),
you're ready for **EPIC-14: Modern MCP Transports & Tool Plugin System**.

Your mission in **Phase 1** is to build the **transport abstraction foundation** that enables:

- **Multiple modes:** Stdio (default, embedded) + HTTP (remote agents)
- **Zero regression:** Existing agents continue working via stdio
- **Enterprise patterns:** Clean architecture for future scaling
- **Modular design:** Tools will be organized into plugin system (Phase 2)

Your **3 sequential critical-path tasks**:

1. **TASK-14-01 (12h):** Transport Interface & Factory (foundation)
2. **TASK-14-02 (12h):** HTTP Transport Implementation (productivity)
3. **TASK-14-05 (12h):** Stdio Transport Implementation (backward compat)

After you finish, Dev C starts **Tool Plugin System** (TASK-14-03, 14-04+) which depends on stable transports.

---

## 🎯 Your Objectives

### ✅ Objective 1: Build Clean Transport Abstraction

- [ ] Design `ITransport` interface (initialize, shutdown, isHealthy, getTransportInfo)
- [ ] Create `TransportFactory` that reads `MCP_TRANSPORT` env var (stdio | http)
- [ ] Ensure both transports implement the interface consistently
- **Success Metric:** Factory creates correct transport type based on env var

### ✅ Objective 2: Implement HTTP Production Transport

- [ ] Extend HTTPTransport PoC → production-grade
- [ ] Setup Express server with CORS + health check endpoint
- [ ] Implement graceful shutdown (200ms request draining)
- [ ] Connection pooling + monitoring
- **Success Metric:** HTTP transport listens on port 3000, health check responds <100ms

### ✅ Objective 3: Implement Stdio Production Transport

- [ ] Extend StdioTransport PoC → production-grade
- [ ] JSON-RPC 2.0 message parsing + error handling
- [ ] Terminal mode detection + warning
- [ ] Graceful shutdown (SIGTERM, SIGINT)
- **Success Metric:** EPIC-11 agents work unchanged via stdio, no regressions

### ✅ Objective 4: Achieve 80%+ Test Coverage

- [ ] Unit tests: Factory, config, message parsing, errors
- [ ] Integration tests: Transport + tool invocation works
- [ ] E2E tests: Stdio + HTTP modes verified end-to-end
- **Success Metric:** Coverage report >80%, all tests green

### ✅ Objective 5: Documentation & Knowledge Transfer

- [ ] Create architecture doc: Transport abstraction design + rationale
- [ ] Setup guides: Deployment options for stdio vs. HTTP
- [ ] Examples: JSON-RPC 2.0 message format, error responses
- **Success Metric:** New dev can spin up server via both transports in <10 min

---

## 📅 Sprint Schedule

### Tuesday 2026-03-18 (Day 1) — TASK-14-01

**Transport Abstraction & Architecture Foundation**

```
9:00–11:30  Step 1-3: Understand task, review PoC, design
11:30–14:00 Step 4: Create interface + factory
14:00–14:30 Step 5: Error diagnoser
14:30–16:30 Step 6: Integrate into MCP server
16:30–17:00 Step 7: Write tests
17:00–17:30 Step 8: Documentation + PR
18:00       EOD Checkpoint: ITransport interface stable, factory tested
```

**Deliverables:**

- ✅ `src/mcp/transport/ITransport.ts` (interface, error enum)
- ✅ `src/mcp/transport/TransportFactory.ts` (factory pattern)
- ✅ `src/mcp/transport/ErrorDiagnoser.ts` (error handling)
- ✅ Unit tests + integration with MCP server
- ✅ PR: `TASK-14-01: Transport abstraction foundation`

**Success Emoji:** 🟢 When factory creates correct transport type based on `MCP_TRANSPORT` env

---

### Wednesday 2026-03-19 (Day 2) — TASK-14-02

**HTTP Transport Implementation & Health Check**

```
9:00–10:00  Step 1-2: Review task, understand HTTP PoC gap
10:00–11:00 Step 3: Design HTTP config (port, host, CORS)
11:00–14:00 Step 4: Implement HTTP server (Express setup)
14:00–15:00 Step 5: Health check endpoint
15:00–16:00 Step 6: Write tests (unit + integration)
16:00–17:00 Step 7: Documentation + setup guide
17:00–17:30 Step 8: Code review + merge
18:00       EOD Checkpoint: HTTP transport listening, health check responds
```

**Deliverables:**

- ✅ `src/mcp/transports/httpTransport.ts` (production-grade)
- ✅ `src/mcp/transports/httpTransportConfig.ts` (config interface)
- ✅ Health check endpoint (`GET /health`)
- ✅ Connection pooling + monitoring + graceful shutdown
- ✅ Unit + integration tests (80%+)
- ✅ PR: `TASK-14-02: HTTP transport production implementation`

**Success Emoji:** 🟢 When `curl http://localhost:3000/health` returns `{ status: 'healthy', uptime }`

---

### Thursday 2026-03-20 (Day 3) — TASK-14-05

**Stdio Transport Implementation (Default Embedded Mode)**

```
9:00–10:00  Step 1-2: Review task, understand stdio PoC gap
10:00–11:00 Step 3: Design JSON-RPC handler
11:00–14:00 Step 4: Implement stdio transport (readline + JSON-RPC)
14:00–15:00 Step 5: Graceful shutdown (SIGTERM, SIGINT)
15:00–16:00 Step 6: Write tests (unit + integration)
16:00–17:00 Step 7: Documentation + examples
17:00–17:30 Step 8: Code review + merge verification
18:00       EOD Checkpoint: EPIC-11 agents work unchanged, no regressions
```

**Deliverables:**

- ✅ `src/mcp/transports/stdioTransport.ts` (production-grade)
- ✅ `src/mcp/transports/jsonRpcHandler.ts` (JSON-RPC 2.0 parsing)
- ✅ Terminal mode detection + graceful shutdown
- ✅ Unit + integration tests (80%+)
- ✅ Backward compat verified: EPIC-11 agents work unchanged
- ✅ PR: `TASK-14-05: Stdio transport production implementation`

**Success Emoji:** 🟢 When `echo '{"jsonrpc":"2.0",...}' | node mcp-server.js` works end-to-end

---

### Friday 2026-03-21 (Day 4) — Sprint Completion & Handoff

```
9:00–10:00  Code review feedback incorporation
10:00–11:00 Final test run (all 3 tasks + backward compat)
11:00–12:00 Team retrospective + learnings documentation
12:00–13:00 Handoff to Dev C (TASK-14-03: Tool plugin system waitlist)
13:00–14:00 Buffer for escalations or test failures
14:00       Final checkpoint: Phase 1 COMPLETE ✅
```

**Deliverables:**

- ✅ All PR reviews resolved
- ✅ Sprint completion summary
- ✅ Handoff package for Dev C (architecture overview + gotchas)
- ✅ Updated implementation summary

**Success Emoji:** 🟢 When all 3 PRs merged to main, tests passing, Phase 1 marked COMPLETE

---

## 📊 Task Breakdown & AC Summary

### TASK-14-01: Transport Abstraction Foundation

| Aspect | Details |
|:-------|:--------|
| **Files to Create** | ITransport.ts, TransportFactory.ts, ErrorDiagnoser.ts |
| **Files to Modify** | src/mcp/index.ts (integrate factory) |
| **AC Count** | 28 (interface design, config, factory, integration, error handling, compat, testing) |
| **Coverage Target** | 80%+ |
| **Timeline** | 12h (Tue 2026-03-18) |
| **Blocker Risk** | 🟢 LOW (straightforward interface + factory pattern) |

**Critical AC to Focus On:**

- AC-14-01-01 to -06: Interface design (6 AC)
- AC-14-01-07 to -11: Env var configuration (5 AC)
- AC-14-01-12 to -16: Factory logic (5 AC)

---

### TASK-14-02: HTTP Transport Implementation

| Aspect | Details |
|:-------|:--------|
| **Files to Create** | httpTransportConfig.ts, tests, setup docs |
| **Files to Modify** | httpTransport.ts (PoC → production) |
| **AC Count** | 24 (server setup, health check, CORS, shutdown, connection mgmt, testing) |
| **Coverage Target** | 80%+ |
| **Timeline** | 12h (Wed 2026-03-19) |
| **Blocker Risk** | 🟡 MEDIUM (Express setup, async shutdown, connection tracking) |

**Critical AC to Focus On:**

- AC-14-02-01 to -06: Server bootstrap (6 AC)
- AC-14-02-07 to -11: Health check (5 AC)
- AC-14-02-15 to -18: Graceful shutdown (4 AC) — *tricky* (200ms drain timeout)

---

### TASK-14-05: Stdio Transport Implementation

| Aspect | Details |
|:-------|:--------|
| **Files to Create** | jsonRpcHandler.ts, tests, specs doc |
| **Files to Modify** | stdioTransport.ts (PoC → production) |
| **AC Count** | 26 (initialization, JSON-RPC parsing, errors, TTY detection, shutdown, compat, testing) |
| **Coverage Target** | 80%+ |
| **Timeline** | 12h (Thu 2026-03-20) |
| **Blocker Risk** | 🔴 HIGH (backward compat is critical—no failures allowed) |

**Critical AC to Focus On:**

- AC-14-05-01 to -04: Initialization (4 AC)
- AC-14-05-05 to -09: JSON-RPC parsing (5 AC) — *tricky* (exactly matches MCP spec)
- AC-14-05-23 to -24: Backward compatibility (2 AC) — *verify directly with EPIC-11 tests*

---

## 🧭 Key Decision Points

### Decision 1: Transport Interface Design

**Question:** Should `ITransport` be async everywhere, or only where needed?
**Decision:** Async for all operations (initialize, shutdown, isHealthy, getTransportInfo) for consistency.
**Rationale:** Easier testing, mock transports, future HTTP client modes.

### Decision 2: HTTP Server Framework

**Question:** Express vs. Fastify vs. Raw Node.js?
**Decision:** Express (already in node_modules, familiar, sufficient for needs).
**Rationale:** No performance-critical constraints; familiarity > micro-optimization.

### Decision 3: Error Codes Approach

**Question:** Custom error codes vs. standard MCP error codes?
**Decision:** Custom enum (`TransportError`) for transport-specific; JSON-RPC standard for messaging.
**Rationale:** Clean separation: transport layer ≠ JSON-RPC application layer.

### Decision 4: Graceful Shutdown Timeout

**Question:** How long to wait for in-flight requests?
**Decision:** 200ms for HTTP, immediate for stdio (stdin.close()).
**Rationale:** HTTP may have concurrent requests; stdio is single-stream, can close immediately.

---

## 🆘 Potential Gotchas & Mitigations

### Gotcha 1: Broken EADDRINUSE Error Handling

**Problem:** If port already bound, error code differs across OS.
**Solution:** Use `ErrorDiagnoser` to map error → `PORT_IN_USE`, provide remediation.
**Test:** Run `lsof -i :3000` to verify port, then test error path.

### Gotcha 2: Stdio Terminal vs. Pipe Mode

**Problem:** If run in terminal (TTY), readline may behave unexpectedly.
**Solution:** Detect `process.stdin.isTTY`, log warning, recommend pipe.
**Test:** Run manually in terminal, then with `| cat`, verify difference.

### Gotcha 3: JSON-RPC Spec Compliance

**Problem:** Slight deviations from spec will break remote agents.
**Solution:** Reference <https://www.jsonrpc.org/specification>, test with real agents.
**Test:** Use EPIC-11 agent tests as verification (they use JSON-RPC).

### Gotcha 4: Connection Draining Timeout

**Problem:** 200ms may be too short for slow networks or heavy requests.
**Solution:** Monitor in tests; if flaky, increase to 500ms (acceptable for M02).
**Test:** Simulate slow client, measure actual drain time, verify no data loss.

### Gotcha 5: Backward Compatibility Regression

**Problem:** Stdio changes break EPIC-11 agents silently.
**Solution:** Run EPIC-11 agent test suite before marking TASK-14-05 done.
**Test:** `npm run test -- src/tests/epic-11-backward-compat.test.ts` (create this if missing).

---

## 🧪 Testing Checklist (Mandatory)

### Pre-Commit Testing

Before committing each task:

```bash
# Unit tests (must pass + 80%+)
npm run test -- src/tests/unit/transport*.test.ts

# Integration tests (must pass)
npm run test -- src/tests/integration/transport*.test.ts

# Linting (must pass)
npm run lint src/mcp/transport* src/mcp/transports*

# Type check (must pass—no 'any' allowed)
npx tsc --noEmit

# Coverage report (must be >80%)
npm run test:coverage -- src/mcp/transport* src/mcp/transports*
```

### Pre-Merge Testing (After PR reviews)

```bash
# Full test suite (including E2E)
npm run test

# Backward compat check (CRITICAL for T14-05)
npm run test:epic-11-compat

# Manual smoke test (TASK-14-05)
echo '{"jsonrpc":"2.0","method":"bootstrap_agent","params":{},"id":1}' \
  | MCP_TRANSPORT=stdio node src/index.ts

# Manual smoke test (TASK-14-02)
MCP_TRANSPORT=http npm run server &
sleep 2
curl http://localhost:3000/health
```

---

## 📞 Support & Escalation

### When to Ask For Help

| Situation | Who | Channel |
|:----------|:----|:--------|
| Design question (interface design, architecture) | Tech Lead | Slack + standup |
| Blocked by EPIC-11 tests failing | Architect | GitHub issue |
| Port/network issues | DevOps / Tech Lead | Slack |
| JSON-RPC spec question | Tech Lead | Docs + Slack |
| Backward compat regression | Architect + QA | GitHub issue + test review |
| Time pressure (falling behind sprint) | Tech Lead | Standup + re-prioritization |

### Success Criterion for Asking for Help

Don't wait until blocked—escalate early if:

- ❌ You don't understand test failure after 30 min debugging
- ❌ You're unclear on AC interpretation
- ❌ Code review feedback contradicts task spec
- ❌ EPIC-11 agent tests fail (regression risk)

---

## 📈 Success Metrics

| Metric | Target | How to Verify |
|:-------|:-------|:--------------|
| **All 28 AC (T14-01) passing** | 100% | AC checklist in assignment sheet |
| **All 24 AC (T14-02) passing** | 100% | Health check e2e test |
| **All 26 AC (T14-05) passing** | 100% | EPIC-11 backward compat tests |
| **Unit test coverage** | 80%+ | `npm run test:coverage` |
| **Integration tests passing** | 100% | `npm run test:integration` |
| **E2E tests passing** | 100% | `npm run test:e2e` |
| **Zero regressions** | 0 critical bugs | EPIC-11 agent tests unchanged |
| **Code review approved** | 1 approval | Tech Lead review |
| **Merged to main** | ✅ | GitHub main branch |
| **Phase 1 complete** | By 2026-03-21 EOD | Sprint board + status update |

---

## 📚 Reference Materials

### Specs & Standards

- 📖 [MCP Specification (modelcontextprotocol.io)](https://spec.modelcontextprotocol.io/)
- 📖 [JSON-RPC 2.0 Spec (jsonrpc.org)](https://www.jsonrpc.org/specification)
- 📖 [MCP SDK: Transports](https://modelcontextprotocol.io/docs/concepts/transports)

### Internal Documentation

- 📄 `EPIC-14-goal.md` — EPIC vision + principles
- 📄 `EPIC-14-state.md` — Detailed task breakdown
- 📄 `TECH-LEAD-EPIC-14-DECISION-WARRANT_2026-03-09.md` — Decision + rationale
- 📄 `database/standards/03-agent-system/mcp.standards.md` — MCP best practices

### Your Task Files (Detailed Specs)

- 📋 `TASK-14-01-ASSIGNMENT.md` — Transport abstraction (28 AC)
- 📋 `TASK-14-02-ASSIGNMENT.md` — HTTP transport (24 AC)
- 📋 `TASK-14-05-ASSIGNMENT.md` — Stdio transport (26 AC)

### Code Examples

- 💻 `src/mcp/transports/HTTPTransport.ts` (PoC—extend this)
- 💻 `src/mcp/transports/StdioTransport.ts` (PoC—extend this)
- 💻 `src/mcp/index.ts` (main server file—integrate factory here)

---

## 🎓 Knowledge Transfer from EPIC-11

You've already mastered:

- ✅ SQLite schema design (TASK-11-01) → Apply to connection pooling DB (if needed)
- ✅ TypeScript strict mode (TASK-11-02, -03) → Enforce no `any` in transports
- ✅ Error handling patterns (TASK-11-07 + EPIC-10) → Use ErrorDiagnoser for transport errors
- ✅ Test-driven development (all EPIC-11 tasks) → 80%+ coverage non-negotiable
- ✅ Security mindset (EPIC-10: OWASP) → Input validation on HTTP headers, CORS checks

**Apply these patterns to EPIC-14 Phase 1.**

---

## 🏁 Finish Line

### When TASK-14-05 is Done (Friday 2026-03-21 EOD)

- ✅ 3 transport tasks complete (36 hours delivered)
- ✅ 78+ AC verified passing (28 + 24 + 26)
- ✅ 80%+ test coverage across all transports
- ✅ Zero EPIC-11 regressions (backward compat confirmed)
- ✅ Clean, documented code ready for Dev C to build upon
- ✅ Tech Lead review + approval
- ✅ All PRs merged to main

### Dev C Starts (Monday 2026-03-24 or earlier)

- 🚀 Dev C assumes stable transport layer
- 🚀 Dev C begins TASK-14-03: Tool plugin system (refactor tools into modules)
- 🚀 Your work becomes the foundation for Enterprise MCP patterns

---

## 📞 Questions?

Before starting, verify:

- [ ] You have access to all 3 task assignment sheets
- [ ] You've read EPIC-14 goal.md + state.md for context
- [ ] You understand the backward compat requirement (EPIC-11 agents must work unchanged)
- [ ] You've reviewed the existing PoC files (HTTPTransport.ts, StdioTransport.ts)
- [ ] You know the sprint timeline (Tue-Fri, 4.5 days, 36 hours effort)

**Message Tech Lead if anything is unclear.** 🚀

---

> **🎉 You've completed EPIC-11. EPIC-14 Phase 1 is your next mountain. You've got this.** 🎉

**Start Date:** Tuesday 2026-03-18 @ 9:00 UTC
**Your First Task:** Read TASK-14-01-ASSIGNMENT.md (this file links to it)
**Your Goal:** Transport abstraction foundation by Wednesday EOD
**Good luck! 🚀**
