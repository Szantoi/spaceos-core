# Week-Based Phase Dispatch Workflow

> **Skill:** Sequential phase dispatch workflow for multi-week project execution
>
> **Forrás:** Explorer Task Research (2026-07-07) — Conductor week-based execution pattern
>
> **Owner:** Conductor (orchestration) ↔ Backend/Frontend/Designer (execution)
>
> **Verzió:** 1.0 (2026-07-07)

---

## CÉLKITŰZÉS

A week-based phase dispatch egy **sequential, priority-driven workflow** multi-week projektek strukturált végrehajtására.

**Probléma:** Nagy projektek (8+ domain, 100+ task) párhuzamos indítása:
- Overload (túl sok párhuzamos task)
- Dependency confusion (mi függ mitől?)
- Status tracking chaos (hol tartunk?)
- Resource conflict (ki dolgozik min?)

**Megoldás:** Week-by-week, phase-by-phase sequential dispatch:
- **Priorities:** 1-6 sorrend, egyértelmű precedencia
- **Trigger conditions:** "After Priority N done → dispatch Priority N+1"
- **ETA calculation:** NWT-based effort estimates
- **Status tracking:** DONE/IN PROGRESS/PENDING real-time

---

## METHODOLOGY

### 1. WEEK PLANNING (Conductor)

**Input:** Project roadmap, domain models, dependency graph

**Output:** Week plan with priority phases:

```yaml
week: 2
project: joinerytech-migration
phases:
  - phase: 1
    priority: 1
    name: "QA Integration Testing"
    dependencies: []  # No blockers
    terminals: [backend]
    estimated_effort: "2-3 NWT"
    trigger: "immediate"

  - phase: 2
    priority: 2
    name: "CRM Integration Testing"
    dependencies: [phase-1]
    terminals: [backend]
    estimated_effort: "3-4 NWT"
    trigger: "after phase-1 DONE"

  - phase: 3-6
    priorities: [3, 4, 5, 6]
    names: ["DMS Application", "HR Application", "Maintenance Application", "QA Application"]
    dependencies: [phase-2]
    terminals: [backend, frontend]
    estimated_effort: "4-6 NWT each"
    trigger: "after phase-2 DONE, sequential or parallel"
```

**Decision Criteria:**
- **Priority 1:** No dependencies, critical path, immediate start
- **Priority 2:** Depends on Priority 1, next in line
- **Priority 3-6:** Parallel execution after Priority 2, or sequential if resource constrained

---

### 2. SEQUENTIAL DISPATCH (Conductor)

**Trigger Logic:**

```bash
# Phase 1 dispatch (immediate)
if [ week_start ]; then
  dispatch_phase priority=1 terminal=backend
fi

# Phase 2 dispatch (after Phase 1 DONE)
if [ phase_1_done_count -eq phase_1_total ]; then
  dispatch_phase priority=2 terminal=backend
fi

# Phase 3-6 dispatch (after Phase 2 DONE)
if [ phase_2_done_count -eq phase_2_total ]; then
  dispatch_phase priority=3,4,5,6 terminal=backend,frontend parallel=true
fi
```

**Message Template:**

```markdown
---
id: MSG-BACKEND-NNN
from: conductor
to: backend
type: task
priority: high
status: UNREAD
model: sonnet
ref: WEEK-2-PHASE-1
created: YYYY-MM-DD
---

# Week 2 Phase 1: QA Integration Testing

## Context
This is **Priority 1** in Week 2 Phase dispatch. No blockers, immediate start.

## Acceptance Criteria
- [ ] FSM tests (5-10 tests)
- [ ] Repository tests (8-15 tests)
- [ ] E2E smoke tests (6-10 tests)
- [ ] RLS validation (3-5 tests)

## Estimated Effort
2-3 NWT (Node Work Time units)

## Next Steps
After DONE, Phase 2 (CRM Integration Testing) will auto-dispatch.

## Dependencies
None (Priority 1)
```

---

### 3. STATUS TRACKING (Conductor)

**Real-time monitoring:**

```bash
# Phase status dashboard
curl -s http://localhost:3456/api/phase-status/week-2

{
  "week": 2,
  "phases": [
    {"priority": 1, "status": "DONE", "terminal": "backend", "completed_at": "2026-07-06T10:30Z"},
    {"priority": 2, "status": "IN PROGRESS", "terminal": "backend", "started_at": "2026-07-06T14:00Z"},
    {"priority": 3, "status": "PENDING", "terminal": "backend", "eta": "2026-07-07T18:00Z"},
    {"priority": 4, "status": "PENDING", "terminal": "frontend", "eta": "2026-07-08T12:00Z"}
  ]
}
```

**Outbox scan:**

```bash
# Count DONE messages for current phase
grep -l "type: done" terminals/backend/outbox/*week2-phase1* | wc -l
```

---

### 4. ETA CALCULATION (Conductor)

**NWT (Node Work Time) conversion:**

| Complexity | NWT | Wall Time (approx) |
|------------|-----|--------------------|
| Simple | 1 NWT | 2-4 hours |
| Medium | 2-3 NWT | 4-8 hours |
| Complex | 4-6 NWT | 1-2 days |
| Epic | 8+ NWT | 3+ days |

**Formula:**

```
ETA = current_time + (remaining_NWT × avg_wall_time_per_NWT)

Example:
Phase 2 = 3 NWT → ETA = now + (3 × 6h) = now + 18h
```

---

### 5. ESCALATION PROTOCOL (Conductor)

**Blocker Detection:**

```yaml
blocker_rules:
  - condition: "phase N DONE count < expected after 24h"
    action: "Send nudge to terminal"

  - condition: "phase N BLOCKED message received"
    action: "Analyze blocker type (technical/infra/business)"
    escalation:
      - L1: Conductor resolves (infra/config)
      - L2: Root consults (architectural decision)
      - L3: External blocker (client/vendor dependency)

  - condition: "phase N stuck >48h"
    action: "Emergency escalation to Root"
```

---

## PHASE TRANSITION CHECKLIST

### Phase Start
- [ ] Verify all dependencies DONE
- [ ] Check terminal availability (not overloaded)
- [ ] Calculate ETA based on NWT estimate
- [ ] Create inbox message with clear acceptance criteria
- [ ] Log phase start timestamp

### Phase In Progress
- [ ] Monitor terminal activity (session active?)
- [ ] Track intermediate updates (progress notes)
- [ ] Respond to questions/blockers within 4h
- [ ] Update ETA if scope changes

### Phase Complete
- [ ] Verify DONE outbox message received
- [ ] Review acceptance criteria (all met?)
- [ ] Archive phase artifacts (docs, code, tests)
- [ ] Trigger next phase dispatch
- [ ] Update week status dashboard

---

## REAL-WORLD EXAMPLE: Week 2 Phase Dispatch (2026-07-06)

**Context:** JoineryTech Migration Week 2 — 6 priorities across 8 domains

### Phase 1 (Priority 1) — QA Integration Testing
**Started:** 2026-07-06 08:00
**Completed:** 2026-07-06 14:30
**Duration:** 6.5 hours (3 NWT)
**Terminal:** Backend
**Result:** DONE — `2026-07-06_153_dms-week2-application-done.md`

**Trigger:** Immediate (week start, no dependencies)

### Phase 2 (Priority 2) — CRM Integration Testing
**Started:** 2026-07-06 15:00 (auto-triggered after Phase 1 DONE)
**Estimated:** 3-4 NWT (18-24h)
**Terminal:** Backend
**Status:** IN PROGRESS

**Trigger:** `phase_1_done_count == 1` → dispatch Phase 2

### Phase 3-6 (Priorities 3-6) — DMS/HR/Maintenance/QA Applications
**Status:** PENDING
**Estimated Start:** After Phase 2 DONE
**Parallel Execution:** Yes (4 terminals if available)
**Estimated Duration:** 4-6 NWT each (1-2 days)

**Trigger:** `phase_2_done_count == 1` → dispatch all priorities 3-6

---

## AUTOMATION CANDIDATES

### Script #1: Auto Phase Transition
**Location:** `scripts/dispatch/auto-phase-transition.sh`
**Trigger:** Cron (every 30 min) or event-driven (on DONE message)

```bash
#!/bin/bash
# Check Phase N DONE count, auto-dispatch Phase N+1

phase1_done=$(grep -l "type: done" terminals/backend/outbox/*week2-phase1* | wc -l)
phase1_total=1

if [ $phase1_done -eq $phase1_total ]; then
  echo "🎯 Phase 1 COMPLETE — Auto-dispatching Phase 2"
  # Create Phase 2 inbox message
fi
```

### Script #2: Phase Status Dashboard
**Location:** `scripts/monitoring/phase-status.sh`
**Output:** JSON dashboard for Datahaven integration

```bash
#!/bin/bash
# Real-time phase status (DONE/IN PROGRESS/PENDING)

curl -s http://localhost:3456/api/phase-status/week-2 | jq
```

---

## SCALING TO WEEK 3, 4, ..., N

**Template Reusability:**

1. Copy Week 2 plan → Week 3 plan
2. Adjust priorities (new domains, new dependencies)
3. Update NWT estimates based on Week 2 actuals
4. Dispatch using same sequential trigger logic

**Example:** Week 3 might have 8 priorities across 10 domains → same methodology applies.

---

## BENEFITS

| Benefit | Impact |
|---------|--------|
| **Predictability** | ETA calculation, status tracking |
| **Scalability** | Reusable for Week 3, 4, ..., N |
| **Resource Control** | Prevents terminal overload (sequential phases) |
| **Dependency Management** | Clear trigger conditions (after X → dispatch Y) |
| **Automation Ready** | Script-friendly (phase transition, status check) |

---

## COORDINATION MATRIX

| Role | Responsibility |
|------|----------------|
| **Conductor** | Week planning, phase dispatch, status tracking, escalation |
| **Backend/Frontend** | Phase execution, DONE reporting, blocker communication |
| **Root** | Blocker resolution (L2/L3), architectural decisions |
| **Librarian** | Knowledge synthesis, skill/pattern documentation |

---

## SAFETY GUARDRAILS

❌ **TILOS:**
- Phase N+1 dispatch amíg Phase N nincs DONE
- Parallel dispatch ha resource constraint van (overload)
- ETA promise amit nem tudsz tartani
- Blocker ignorálás >24h

✅ **JAVASLAT:**
- Mindig NWT-based ETA, soha nem "holnap kész lesz"
- Trigger condition explicit (ne manuális decision)
- Status dashboard real-time (ne stale adatból dolgozz)
- Blocker azonnal eszkalálni ha nem tudsz megoldani

---

## VERZIÓHISTÓRIA

| Verzió | Dátum | Frissítés |
|--------|-------|----------|
| 1.0 | 2026-07-07 | Initial release based on Explorer Task Research (MSG-EXPLORER-TASK-RESEARCH-001) |

---

**Skill Maintainer:** Conductor
**Last Updated:** 2026-07-07
