---
id: TASK-18-06
title: "Quality score computation service"
completed_by: Backend Developer
date: 2026-03-13
status: completed
pr: "#18-06-quality"
---

# TASK-18-06: Quality Score Computation — Implementation Summary

## What Was Built?

Implemented a deterministic, versioned quality score algorithm for episode highlights that blends AI confidence with manual feedback.

- Added schema extensions to persist AI confidence, feedback average, and formula version.
- Implemented `computeQualityScore()` with a stable formula and clamping to [0,1].
- Added `AgentDb.recalculateHighlightQuality()` to recompute and persist the combined score.
- Added a unit test suite covering deterministic output, no-feedback case, feedback weighting, and clamping.

## Acceptance Criteria Status

- [✅] **AC-1:** Formula documented and deterministic
- [✅] **AC-2:** AI-only case returns valid score in [0,1]
- [✅] **AC-3:** AI+feedback case computes weighted/average score
- [✅] **AC-4:** No-feedback case does not fail
- [✅] **AC-5:** Out-of-range inputs are clamped consistently
- [✅] **AC-6:** Unit tests are green (4/4 passing)

## Files Created / Updated

- **`src/metadata/migrations/011_epic18_highlight_quality.sql`** — New migration for score components
- **`src/metadata/qualityScoring.ts`** — Added `computeQualityScore()` and related types
- **`src/mcp/AgentDb.ts`** — Added `getHighlightFeedbackScores()` and `recalculateHighlightQuality()` methods
- **`src/tests/unit/highlight-quality-score.test.ts`** — New unit tests for score computation

## Formula (v1)

```text
aiScore = clamp(ai_score, 0..1)
feedbackAvg = mean(clamp(feedback_scores, 0..1))
feedbackCount = number of feedback rows
w = feedbackCount === 0 ? 0 : min(1, feedbackCount / (feedbackCount + 1))
combined = aiScore * (1 - w) + feedbackAvg * w
```

- With zero feedback, the score equals AI confidence.
- With feedback, the formula softly increases weight as more feedback is provided.
- All values are clamped to [0,1] and rounded to 4 decimal places.

## Key Decisions

- **Persistence:** Storing both `ai_quality_score` and `feedback_quality_score` allows auditing and future formula changes.
- **Stability:** The formula is versioned (`quality_score_version`) to enable migrations later.
- **Safety:** All inputs are clamped to avoid invalid storage and edge-case behavior.

## Tests

- **`AC-1/AC-2`**: AI-only computed score in [0,1]
- **`AC-3`**: Weighted mixture of AI + feedback computed correctly
- **`AC-4`**: No feedback returns AI score
- **`AC-5`**: Out-of-range values clamped and scores computed

All tests pass in the local suite.

## Next Step

Proceed to **TASK-18-07 (tag_episode_quality feedback loop)** which will leverage this scoring engine and ensure user feedback updates highlight scores in real time.
