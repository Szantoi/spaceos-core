---
title: "Dev E — TASK-13-04 Assignment Sheet"
subtitle: "Submit Discovery Outcome Tool — Episode Capture + Blockers"
created: 2026-03-08
updated: 2026-03-09
assigned_to: "Dev E"
priority: "P0"
epic: "EPIC-13"
phase: "M02 — Phase 1: Discovery Track Setup"
status: "✅ READY (after TASK-13-03)"
effort_estimate: "12 hours"
ac_count: 3
---

# 🚀 Dev E — TASK-13-04 Assignment

**Task:** TASK-13-04 (Submit Discovery Outcome Tool)
**Epic:** EPIC-13 (Discovery Track Tools)
**Priority:** P0
**Effort Estimate:** 12 hours (~1.5 days)

---

## 🎯 Your Mission

Implement `submit_discovery_outcome()` MCP tool that lets discovery agents capture episode results (validated ideas, learnings, blockers) into EPIC-12 episodic memory with discovery-specific metadata.

**Key Deliverable:** Discovery outcomes stored as episodes with track="discovery" tag, enabling future retrieval + learning.

---

## 📋 Acceptance Criteria (3 AC)

### AC-1: Tool Schema ✅

```
Input: {
  outcome_type: "VALIDATED_IDEA" | "REJECTED_IDEA" | "LEARNING",
  summary: string (max 500 chars),
  blockers: string[] (list of blocking issues),
  phase: "ideation" | "validation" | "iteration",
  artifacts: string[] (artifact IDs to attach)
}
Output: { episode_id, stored_at, track: "discovery" }
```

### AC-2: Episode Storage Integration ✅

- [ ] Uses EPIC-12 `store_experience()` under the hood
- [ ] Sets track="discovery" automatically
- [ ] Captures outcome_type + blockers as metadata
- [ ] Triggers async ChromaDB indexing (EPIC-12 TASK-12-03)

### AC-3: E2E Workflow ✅

- [ ] Discovery agent validates idea
- [ ] Calls `submit_discovery_outcome(type="VALIDATED_IDEA", blockers=[...], ...)`
- [ ] Episode stored + indexed
- [ ] Future `reference_prior_discovery()` finds it

---

## 🛠️ Implementation

```typescript
export async function submit_discovery_outcome(input: {
  outcome_type: "VALIDATED_IDEA" | "REJECTED_IDEA" | "LEARNING";
  summary: string;
  blockers: string[];
  phase: "ideation" | "validation" | "iteration";
  artifacts?: string[];
}): Promise<{ episode_id: string; stored_at: string; track: "discovery" }> {
  // 1. Validate outcome_type + phase access
  // 2. Validate summary length (< 500 chars)
  // 3. Build episode metadata (track, outcome_type, blockers)
  // 4. Call store_experience() with track="discovery"
  // 5. Return episode_id + timestamp
}
```

---

## 📁 Files to Create

- Update `src/mcp/tools/discovery.ts` — Add handler
- `src/tests/unit/DiscoveryOutcome.test.ts` — 8+ tests
- `src/tests/integration/DiscoveryOutcome.integration.test.ts` — E2E with EPIC-12

---

## 📞 Definition of Done

- [ ] All 3 AC passing
- [ ] 8+ unit tests green
- [ ] Integration test with EPIC-12 episodic memory
- [ ] Ready for peer review
