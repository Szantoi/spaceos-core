---
id: TASK-13-05
title: "Phase-Specific Tools & Validation Gates"
epic: EPIC-13
completed_by: Dev E
date: 2026-03-12
pr: [#NNN]
---

# TASK-13-05: Implementation Summary

## What Was Built?

Implemented phase-specific discovery tools and ordering gates:

- `check_constraints(idea_summary, constraint_set)` validates ideas against technical/business/timeline constraints and returns violations + go/no-go.
- `get_phase_guidance(current_phase)` returns exit criteria, next phase, and available tools.
- Phase-order gate in `submit_discovery_outcome` enforces ideation -> validation -> iteration progression.

The implementation uses `DiscoveryPhaseTracker` when a DB-backed context is available, with an in-memory fallback to avoid hard coupling in lightweight test/plugin scenarios.

## Acceptance Criteria Status

- [✅] AC-1: `check_constraints` input/output and invalid set handling implemented.
- [✅] AC-2: `get_phase_guidance` returns structured guidance and next-step mapping.
- [✅] AC-3: phase gate prevents submitting iteration outcomes before validation completion.

## Files Created/Modified

- `src/mcp/tools/discovery.ts`
- `src/metadata/DiscoveryPhaseTracker.ts`
- `src/tests/unit/DiscoveryTools.test.ts`
- `src/tests/unit/DiscoveryPhaseTracker.test.ts`

## Tests Added/Updated

- Unit tests for constraint evaluation and phase guidance output.
- Unit tests for phase progression enforcement and gating behavior.

## Technical Decisions

1. Keep phase-state validation in a dedicated tracker (`DiscoveryPhaseTracker`) for consistency with metadata persistence patterns.
2. Provide in-memory completion fallback in plugin runtime to keep tests deterministic and avoid unnecessary DB setup overhead.
3. Return structured business errors (`success: false`, `error.code`, `error.message`) instead of throwing for gate violations.

## Peer Review Sign-Off

- [ ] Code reviewed
- [ ] Tests validated
- [ ] Ready for deployment
