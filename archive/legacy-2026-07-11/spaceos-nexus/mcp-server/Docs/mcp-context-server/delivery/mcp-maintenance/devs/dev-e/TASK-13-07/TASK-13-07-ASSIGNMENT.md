---
title: "Dev E — TASK-13-07 Assignment Sheet"
subtitle: "E2E Integration & Discovery Workflow Validation — Full System Test"
created: 2026-03-08
updated: 2026-03-09
assigned_to: "Dev E"
priority: "P0"
epic: "EPIC-13"
phase: "M02 — Phase 1: Discovery Track Setup"
status: "🔴 BLOCKED BY ALL PRIOR TASKS"

effort_estimate: "14 hours"
ac_count: 3
---

# 🚀 Dev E — TASK-13-07 Assignment

**Task:** TASK-13-07 (E2E Integration & Discovery Workflow Validation)
**Epic:** EPIC-13 (Discovery Track Tools)
**Priority:** P0
**Effort Estimate:** 14 hours (~2 days)

---

## 🎯 Your Mission

**Full end-to-end validation** of the discovery track:

1. Discovery agent bootstrap → gets discovery context
2. Full DWI workflow: ideation → validation → iteration → delivery_handoff
3. All tools work together (request_context, reference_prior_discovery, check_constraints, etc.)
4. Episodic memory integration (EPIC-12) stores + retrieves discovery episodes
5. RBAC enforcement: discovery-only access control

---

## 📋 Acceptance Criteria (3 AC)

### AC-1: Discovery Bootstrap & Context ✅

```
1. bootstrap_agent("discovery", "discovery/architect")
   → track="discovery", role="architect" in session

2. request_context("ideation")
   → Returns ideation templates + tools

3. Phase progress: ideation → validation → iteration → delivery_handoff
   → All phases reachable via proper gatekeeping
```

### AC-2: Full DWI Workflow ✅

```
E2E Test Scenario:
1. Ideation phase: submit_artifact("idea_list", 3 ideas)
   → Exit ideation
2. Validation phase: check_constraints(idea1, "technical")
   → Validate against constraint set
3. Iteration phase: submit_artifact("refined_design", ...)
   → Refine based on validation
4. Delivery_handoff: submit_discovery_outcome(type="HANDOFF", ...)
   → Create task ticket for engineers
```

### AC-3: E2E Tool Integration ✅

- [ ] All 7 discovery tools callable in workflow
- [ ] Episodic memory (EPIC-12) integration works
  - `submit_discovery_outcome()` stores episode + track="discovery"
  - `reference_prior_discovery()` finds discovery episodes
- [ ] RBAC enforcement: discovery agents cannot call delivery tools
- [ ] Blocker tracking captured during workflow
- [ ] Search fallback tested (ChromaDB → FTS5)

---

## 🛠️ Implementation

```typescript
// E2E test scenario (pseudocode)
test('full_discovery_workflow_e2e', async () => {
  // 1. Bootstrap discovery agent
  const session = await bootstrap_agent({
    track: "discovery",
    role: "discovery/architect"
  });
  assert(session.track === "discovery");

  // 2. Ideation phase
  const ideationContext = await request_context("ideation");
  const ideas = await submit_artifact({
    type: "idea_list",
    content: "3 ideas for user feedback system"
  });

  // 3. Validation phase
  const validation = await check_constraints(ideas[0], "technical");
  assert(validation.go_no_go === true);

  // 4. Reference prior discovery
  const priorEpisodes = await reference_prior_discovery("feedback system design");
  assert(priorEpisodes.length > 0);
  assert(priorEpisodes[0].track === "discovery");

  // 5. Iteration phase
  const refined = await submit_artifact({
    type: "refined_design",
    content: "Final design based on feedback"
  });

  // 6. Handoff
  const episode = await submit_discovery_outcome({
    outcome_type: "HANDOFF",
    summary: "Validated user feedback system design",
    blockers: []
  });
  assert(episode.track === "discovery");

  // 7. Verify RBAC enforcement
  try {
    await delivery_tool_call("delivery/tool");
    fail("Should have UNAUTHORIZED error");
  } catch (err) {
    assert(err.code === "UNAUTHORIZED");
  }
});
```

---

## 📁 Files to Create

- `src/tests/e2e/DiscoveryWorkflow.spec.ts` — Full E2E scenarios
- Performance benchmarks
- Integration test with EPIC-12

---

## 📞 Definition of Done

- [ ] All 3 AC passing
- [ ] E2E workflow completes successfully
- [ ] RBAC enforcement validated
- [ ] All 7 discovery tools integrated
- [ ] 100% of EPIC-13 feature complete + tested
- [ ] Ready for EPIC-13 closure + deployment
