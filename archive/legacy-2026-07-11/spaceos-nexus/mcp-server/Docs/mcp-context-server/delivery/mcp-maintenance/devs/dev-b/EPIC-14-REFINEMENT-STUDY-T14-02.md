---
id: DEV-B-EPIC-14-REFINEMENT-STUDY
title: "Dev B — EPIC-14 Refinement Study: HTTP Transport & Graceful Shutdown"
created: 2026-03-08
type: "parallel-refinement-assignment"
assignee: "Dev B"
duration: "2 days (1-2h/day, non-blocking)"
phase: "Parallel Prep (while waiting for EPIC-11 blocker)"
deadline: "2026-03-10 EOD"
tech-lead-checkpoint: "2026-03-11 (go/no-go decision by 2026-03-14)"
---

# Dev B — EPIC-14 Refinement Study

## 🎯 Executive Summary

**Your Job:** Design + validate EPIC-14 **TASK-14-02** (HTTP Transport + Graceful Shutdown) while waiting for Dev A's TASK-11-01 blocker to clear.

**Timeline:** Saturday 2026-03-09 → Monday 2026-03-10 EOD (2 days at 1-2h/day)

**Outcome:** Tech Lead has complete design validation + PoC to make EPIC-14 go/no-go decision by **2026-03-14 EOD ** (Option A/B/C).

**Your Role:** Deep-dive the spec, build PoC skeleton, identify risks, map QA improvements.

---

## 📋 Context: Why This? Why Now?

### The Situation

1. You're currently **blocked** on Dev A completing TASK-11-01 (FSM Schema) — which unblocks your real work (TASK-11-02 StateTracker).
2. Dev A won't finish until **2026-03-12 EOD**, so you have **3 days of available time** (2026-03-09, 2026-03-10, starting 2026-03-11).
3. Rather than idle for 3 days, you can **parallelize** EPIC-14 refinement work — which is **independent** of EPIC-11.

### The Opportunity

- **EPIC-14 is conditional** on a Tech Lead decision gate (2026-03-14 EOD)
- If we can provide a **complete design + PoC + risk assessment by 2026-03-11**, Tech Lead can make an informed go/no-go call
- Your refinement work **directly unblocks** the decision and the O6 team's planning

### The Constraint

- This is **prep work only** — not full implementation yet
- We're funding this from "idle time" (Dev B waiting for TASK-11-01 blocker)
- If Tech Lead says "no EPIC-14" on 2026-03-14, this prep work becomes tech debt (small cost, high value if we proceed)

---

## 🔥 What You're Building (4 Deliverables)

> **Note:** refinement study tasks completed by 2026-03-09; deliverables produced for Tech Lead review.


### Deliverable 1️⃣: Design Doc — HTTP Transport + Graceful Shutdown

**File:** `dev-b/EPIC-14-REFINEMENT-STUDY-T14-02-DESIGN.md`

**Read This First:**
- [EPIC-14-T14-02-ASSIGNMENT.md](../dev-b/EPIC-14-T14-02-ASSIGNMENT.md) — Base spec
- [EPIC-14-COORDINATION-ROUTER.md](../EPIC-14-COORDINATION-ROUTER.md) — Full epic scope
- [EPIC-14-QA-IMPROVEMENTS-INTEGRATION.md](../EPIC-14-QA-IMPROVEMENTS-INTEGRATION.md) — P0/P1/P2 issues

**Questions to Answer (500-800 words):**

1. **Health Check State Management (P0 Issue)**
   - Current spec: "Implements graceful shutdown with health checks and connection draining"
   - Question: What does "health check state" actually mean? (e.g., HEALTHY → DRAINING → OFFLINE flow?)
   - Your answer: Sketch the state machine + transitions

2. **Graceful Shutdown Lifecycle**
   - What's the actual flow? (Signal received → notify clients → drain connections → shutdown)
   - How do we detect "all work done"? (pending request tracking?)
   - What's the timeout strategy? (hard kill after 30s? configurable?)

3. **Connection Draining**
   - How do we "drain" HTTP connections? (close new connections, let existing finish?)
   - What if a request takes 10 seconds and we have a 5-second drain timeout?
   - How do we communicate this to clients? (HTTP headers? 503 Unavailable?)

4. **Integration Points**
   - Where does HttpTransport sit in the MCP layer? (Below SessionManager? Parallel?)
   - How does it integrate with existing error handling (ErrorResponses)?
   - How does it work with the FSM (EPIC-11)? (Any state dependencies?)

**Acceptance Criteria:**
- [ ] AC-1: 500-800 words, answers all 4 questions
- [ ] AC-2: State machine diagram (ASCII or text description)
- [ ] AC-3: Integration points with EPIC-11 identified
- [ ] AC-4: P0 requirement (health state mgmt) clearly addressed

---

### Deliverable 2️⃣: Proof of Concept Skeleton

**File:** `src/mcp/transports/HttpTransport.ts` (skeleton, no implement)

**What to Build:**

```typescript
// src/mcp/transports/HttpTransport.ts

// Health state enum (P0 requirement)
enum HealthStatus {
  HEALTHY = "healthy",
  DRAINING = "draining",
  OFFLINE = "offline",
}

// Core interface
export interface IHttpTransport {
  // Startup
  start(port: number): Promise<void>;

  // Health & graceful shutdown
  getHealthStatus(): HealthStatus;
  initiateGracefulShutdown(timeoutMs: number): Promise<void>;

  // Request handling
  handleRequest(req: HttpRequest): Promise<HttpResponse>;
}

// Pseudocode skeleton
export class HttpTransport implements IHttpTransport {
  private healthStatus: HealthStatus = HealthStatus.HEALTHY;
  private activeConne1: number = 0; // connection counter

  async initiateGracefulShutdown(timeoutMs: number): Promise<void> {
    // Step 1: Set health status to DRAINING
    this.healthStatus = HealthStatus.DRAINING;

    // Step 2: Stop accepting new connections (but finish active ones)
    // this.server.pause(); // hypothetical

    // Step 3: Wait for all connections to drain (with timeout)
    const drainStart = Date.now();
    while (this.activeConnections > 0 && Date.now() - drainStart < timeoutMs) {
      // Poll active connections
      await new Promise(resolve => setTimeout(resolve, 100));
    }

    // Step 4: Hard close if timeout exceeded
    if (this.activeConnections > 0) {
      console.warn(`Forcing shutdown: ${this.activeConnections} connections still active`);
    }

    // Step 5: Mark offline
    this.healthStatus = HealthStatus.OFFLINE;
  }
}
```

**Acceptance Criteria:**
- [ ] AC-1: File compiles (TypeScript syntax check)
- [ ] AC-2: Key methods stubbed (start, shutdown, health check)
- [ ] AC-3: Health state enum defined
- [ ] AC-4: Pseudocode describes graceful shutdown flow

---

### Deliverable 3️⃣: QA Improvement Mapping (P0 Requirement)

**File:** `dev-b/EPIC-14-REFINEMENT-STUDY-T14-02-QA-MAPPING.md`

**What to Map:**

Reference [EPIC-14-DECISION-GATE-SUMMARY.md](../EPIC-14-DECISION-GATE-SUMMARY.md) Table: P0 Issue = "Graceful shutdown missing health check state mgmt"

Answer These:
1. **P0 Issue Description:** What exactly is missing in current spec?
   - Current spec says: "graceful shutdown with health checks"
   - Missing: explicit health state management + state transitions

2. **Where Does It Live?**
   - In HttpTransport? (healthStatus enum?)
   - In a separate HealthStateManager service?
   - Integrated with SessionManager?

3. **How Do We Test It?**
   - Unit test: Can we set health state to DRAINING?
   - Integration test: Do pending requests finish before going OFFLINE?
   - E2E test: Can we simulate graceful shutdown + restart?

4. **What's the +3h Effort?**
   - Spec says P0 requires "health state handler" (+3h effort)
   - What is this handler? (A middleware? A state machine? A periodic task?)
   - Where in the implementation schedule does it fit? (Part of T14-02 or separate?)

**Acceptance Criteria:**
- [ ] AC-1: P0 requirement clearly understood + mapped
- [ ] AC-2: Implementation location identified (which class/module)
- [ ] AC-3: Test strategy drafted (3-5 test cases)
- [ ] AC-4: Effort estimate (+3h) justified or challenged

---

### Deliverable 4️⃣: Risk Assessment

**File:** `dev-b/EPIC-14-REFINEMENT-STUDY-T14-02-RISKS.md`

**Risk Matrix:**

| Risk | Probability | Impact | Mitigation |
|:-----|:----------:|:------:|:-----------|
| **Race condition: New req arrives during DRAINING** | 🟡 MEDIUM | 🟡 MEDIUM | Load balancer sends 503 before drain starts? |
| **Timeout too short: Requests killed mid-stream** | 🟡 MEDIUM | 🔴 CRITICAL | Make timeout configurable? Min 5s, max 60s? |
| **No way to detect "all work done"** | 🟢 LOW | 🟡 MEDIUM | Track active connections + pending callbacks? |
| **Node.js server internals (http.Server close())** | 🟠 UNKNOWN | 🟡 MEDIUM | Research Node.js graceful shutdown best practices |
| **Testing flakes: timing-dependent tests** | 🟡 MEDIUM | 🟡 MEDIUM | Use deterministic mocks + advance time in tests? |

**Acceptance Criteria:**
- [ ] AC-1: 5-8 risks identified
- [ ] AC-2: Each risk has probability + impact
- [ ] AC-3: Mitigation strategy for each
- [ ] AC-4: Blockers flagged for Tech Lead (if any)

---

## 📚 Reading List & References

### Core Files to Review

1. **EPIC-14 Spec:**
   - [EPIC-14-T14-02-ASSIGNMENT.md](../dev-b/EPIC-14-T14-02-ASSIGNMENT.md) — Full spec (read thoroughly)
   - [EPIC-14-COORDINATION-ROUTER.md](../EPIC-14-COORDINATION-ROUTER.md) — Epic overview

2. **QA Research:**
   - [EPIC-14-DECISION-GATE-SUMMARY.md](../EPIC-14-DECISION-GATE-SUMMARY.md) — P0/P1/P2 issues + options
   - [EPIC-14-QA-IMPROVEMENTS-INTEGRATION.md](../EPIC-14-QA-IMPROVEMENTS-INTEGRATION.md) — Detailed QA findings

3. **Context (MCP Transports):**
   - [EPIC-14-T14-01-ASSIGNMENT.md](../EPIC-14-T14-01-ASSIGNMENT.md) — Transport abstraction (base for T14-02)
   - `src/mcp/mcpServer.ts` — How transports currently work

### External References

- **Node.js Graceful Shutdown:** https://nodejs.org/en/learn/nodejs-cli/graceful-shutdown
- **HTTP Connection Draining:** https://en.wikipedia.org/wiki/Graceful_shutdown#HTTP_server_shutdown
- **Health Check Best Practices:** https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-probes/

---

## ⏱️ Timeline & Checkpoints

### Saturday 2026-03-09 (1-2h)

- [ ] Read all spec files (EPIC-14-T14-02-ASSIGNMENT + QA findings)
- [ ] Sketch health state machine (textual, not diagram yet)
- [ ] Outline design doc (bullet points)

### Sunday 2026-03-10 (1-2h)

- [ ] Write design doc (500-800 words)
- [ ] Create PoC skeleton (TypeScript file, compiles)
- [ ] Draft risk matrix

### Monday 2026-03-11 EOD (checkpoint)

- [ ] All 4 deliverables complete + committed
- [ ] Tech Lead review + feedback session
- [ ] Refinement docs aggregated for decision gate (by 2026-03-14)

---

## ✅ Submission Checklist

When you're done, ensure:

- [ ] Design doc (`EPIC-14-REFINEMENT-STUDY-T14-02-DESIGN.md`) ✅ written + committed
- [ ] PoC skeleton (`src/mcp/transports/HttpTransport.ts`) ✅ compiles
- [ ] QA mapping doc (`EPIC-14-REFINEMENT-STUDY-T14-02-QA-MAPPING.md`) ✅ complete
- [ ] Risk assessment (`EPIC-14-REFINEMENT-STUDY-T14-02-RISKS.md`) ✅ detailed
- [ ] All files in `dev-b/` folder + committed to Git
- [ ] Send Tech Lead summary (email or Slack) by 2026-03-11 EOD

---

## 🎯 Why This Matters (To You & The Team)

1. **You:** Direct input on EPIC-14 design = high visibility + impact
2. **Team:** Complete design validation = faster kickoff on 2026-03-19 (if approved)
3. **Tech Lead:** Better go/no-go decision with actual PoC + risks assessed
4. **Project:** +5-7 days parallel acceleration vs sequential planning

**Tzrg:** This is "downtime leverage" — turning idle waiting time into valuable discovery work. No downside if tech lead says "no EPIC-14", high upside if they say "go"!

---

**Questions?** Reach out to Tech Lead for spec clarifications.

**Ready to Start?** Monday 2026-03-09 09:00, let's ship this! 🚀
