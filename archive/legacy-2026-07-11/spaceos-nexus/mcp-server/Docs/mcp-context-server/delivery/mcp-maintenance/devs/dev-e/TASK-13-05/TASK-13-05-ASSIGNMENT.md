---
title: "Dev E — TASK-13-05 Assignment Sheet"
subtitle: "Phase-Specific Tools & Validation Gates — Enforce DWI States"
created: 2026-03-08
updated: 2026-03-09
assigned_to: "Dev E"
priority: "P1"
epic: "EPIC-13"
phase: "M02 — Phase 1: Discovery Track Setup"
status: "✅ READY (after TASK-13-04)"
effort_estimate: "13 hours"
ac_count: 3
---

# 🚀 Dev E — TASK-13-05 Assignment

**Task:** TASK-13-05 (Phase-Specific Tools & Validation Gates)
**Epic:** EPIC-13 (Discovery Track Tools)
**Priority:** P1
**Effort Estimate:** 13 hours (~1.5 days)

---

## 🎯 Your Mission

Implement **phase-specific tools** that enforce workflow rules:

1. `check_constraints(idea, constraint_set)` — Validate idea against constraints (validation phase)
2. `get_phase_guidance()` — Return phase exit criteria, next steps, tools available
3. Validation gates ensuring ideas don't skip phases

---

## 📋 Acceptance Criteria (3 AC)

### AC-1: check_constraints Tool ✅

- [ ] Input: idea_summary, constraint_set ("technical", "business", "timeline")
- [ ] Output: { violations: [...], go_no_go: boolean, rationale: string }
- [ ] Checks against database constraints for phase
- [ ] Error handling: invalid constraint_set

### AC-2: get_phase_guidance Tool ✅

- [ ] Input: current_phase
- [ ] Output: { exit_criteria: [...], next_phase: string, tools_available: [...] }
- [ ] Guides agents through phase transitions
- [ ] Enforces entrance/exit criteria from TASK-13-01

### AC-3: Phase Validation Gates ✅

- [ ] Agent cannot call `submit_discovery_outcome(phase="iteration")` before validation complete
- [ ] Agent cannot skip phases (ideation → validation → iteration required)
- [ ] Database tracks phase_complete status per session
- [ ] E2E test: enforce phase order

---

## 🛠️ Implementation

- Implement phase state machine in middleware
- Add enter/exit validators per phase
- Wire up tool availability by phase
- 10+ unit tests

---

## 📁 Files to Create

- `src/mcp/tools/discoveryPhaseTools.ts`
- `src/metadata/DiscoveryPhaseTracker.ts` — Phase state machine
- Tests

---

## 📞 Definition of Done

- [ ] All 3 AC passing
- [ ] 10+ unit tests
- [ ] Phase gates enforced
- [ ] Ready for peer review
