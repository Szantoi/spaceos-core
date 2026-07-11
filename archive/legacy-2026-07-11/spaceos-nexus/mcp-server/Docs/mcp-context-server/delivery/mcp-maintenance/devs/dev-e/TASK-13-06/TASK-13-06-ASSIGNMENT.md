---
title: "Dev E — TASK-13-06 Assignment Sheet"
subtitle: "Blocker Tracking & Search Fallback — Handle Discovery Failures"
created: 2026-03-08
updated: 2026-03-09
assigned_to: "Dev E"
priority: "P1"
epic: "EPIC-13"
phase: "M02 — Phase 1: Discovery Track Setup"
status: "✅ READY (after TASK-13-05)"
effort_estimate: "11 hours"
ac_count: 3
---

# 🚀 Dev E — TASK-13-06 Assignment

**Task:** TASK-13-06 (Blocker Tracking & Search Fallback)
**Epic:** EPIC-13 (Discovery Track Tools)
**Priority:** P1
**Effort Estimate:** 11 hours (~1.5 days)

---

## 🎯 Your Mission

Implement **blocker tracking** (document issues preventing progress) and **search fallback** (if semantic search fails, use keyword search).

**Key Features:**

1. `track_blocker(blocker_text, severity, blocking_phase)` — Log blocking issue
2. Fallback: If ChromaDB semantic search fails, re-route to FTS5 keyword search
3. Dashboard: Show blockers per discovery session

---

## 📋 Acceptance Criteria (3 AC)

### AC-1: Blocker Tracking ✅

- [ ] `blockers` table created in agent.db
- [ ] Each blocker has: id, session_id, phase, severity, text, created_at
- [ ] `track_blocker()` MCP tool captures blockers
- [ ] Can query blockers per session/phase

### AC-2: Search Fallback ✅

- [ ] When ChromaDB semantic search fails/times out: fall back to FTS5
- [ ] `reference_prior_discovery()` tries ChromaDB first, then FTS5
- [ ] Performance: fallback < 50ms (FTS5 SLA)
- [ ] No silent failures: log fallback event

### AC-3: E2E Blocker Dashboard ✅

- [ ] Show blockers per discovery session
- [ ] Filter by severity (HIGH, MEDIUM, LOW)
- [ ] Integration test: track blocker → query it

---

## 🛠️ Implementation

- Create `blockers` table migration
- Implement `track_blocker()` tool
- Add search fallback logic to `reference_prior_discovery()`
- 8+ unit tests

---

## 📞 Definition of Done

- [ ] All 3 AC passing
- [ ] 8+ unit tests
- [ ] Search fallback tested
- [ ] Ready for peer review
