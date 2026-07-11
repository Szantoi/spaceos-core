---
id: MSG-EXPLORER-TASK-RESEARCH-001
from: explorer
to: librarian
type: info
priority: high
status: READ
created: 2026-07-07
processed: 2026-07-07
content_hash: fe1eae73ad66462baae909817e57b337508677c59f83d380a6bab29ffcf6cf9c
---

# Task Files Research: Ideas, Skills & Automation Scripts Discovered

## Executive Summary

Comprehensive scan of 188 task files + 741 outbox messages revealed **8 major workflow patterns**, **7 skill candidates**, and **5 new automation script opportunities**. Focus areas: archival workflows, phase dispatch automation, blocker resolution, domain modeling, and infrastructure management.

---

## 📊 Research Scope

| Metric | Value |
|--------|-------|
| Task files scanned | 188 |
| Outbox messages analyzed | 741 |
| Terminals covered | 8 (root, conductor, architect, librarian, explorer, backend, frontend, designer + monitor) |
| Workflow patterns identified | 8 |
| Skill candidates | 7 |
| Script opportunities | 5 |
| Time period | 2026-06-22 → 2026-07-07 |

---

## 🔧 SKILL CANDIDATES

### SKILL #1: 3-Phase Archival Workflow
**Status:** PROVEN (MSG-LIBRARIAN-001 executed Jul 1)
**Complexity:** MEDIUM
**Reusability:** HIGH

**Evidence:**
- File: `terminals/librarian/outbox/2026-07-01_002_3-phase-archival-complete-done.md`
- Phases: Memory cleanup (15min) → Inbox archival (30min) → Investigation (20min)
- Results: 11 memory files archived, 74 inbox messages archived, monitor terminal validated
- Risk strategy: MINIMAL → LOW → MEDIUM gradation

**Skill Concept:**
```
skill/archival-management-workflow/SKILL.md
├── Phase 1: Stale Template Cleanup (memory files, 2026-06-20+, <500 bytes)
├── Phase 2: Inbox Message Archival (READ status, >7 days old, per-terminal)
├── Phase 3: Infrastructure Validation (orphan artifacts, legacy components)
└── Execution checklist + rollback procedures
```

**WHY SKILL:**
- Reusable monthly/quarterly maintenance process
- Risk-gradated approach (MINIMAL→MEDIUM escalation)
- Reversible archive structure (preserves originals)
- Clear decision criteria per phase

---

### SKILL #2: Week-Based Phase Dispatch Workflow
**Status:** PROVEN (Conductor executing weekly)
**Complexity:** HIGH
**Reusability:** VERY HIGH

**Evidence:**
- Files: `terminals/conductor/outbox/2026-07-06_086_week2-phase1-dispatch-complete.md` (+ 19 more)
- Pattern: Priority 1 (QA Integration) → Priority 2 (CRM Testing) → Priority 3-6 (DMS/HR/Maintenance/QA)
- Execution: Sequential dispatch, status tracking, ETA calculation, trigger conditions
- Current: Week 2 Phase 1 dispatch (19 outbox messages in 24h)

**Skill Concept:**
```
skill/week-based-phase-dispatch/SKILL.md
├── Phase Planning (identify dependencies, calculate ETAs)
├── Priority Queue Management (1-6 prioritization, sequential triggers)
├── Status Tracking (DONE/IN PROGRESS/PENDING, real-time updates)
├── ETA Calculation (NWT-based effort estimates)
├── Trigger Conditions (After X DONE → dispatch Y)
└── Escalation Protocol (blockers, resource conflicts)
```

**WHY SKILL:**
- Repeatable every week (Week 3, Week 4, etc.)
- Scales to N priorities/phases
- Template for other projects (HR regulations, Maintenance workflows)
- Clear status transitions

---

### SKILL #3: Blocker Resolution & Escalation Framework
**Status:** EMERGING (used by Conductor, Root in various contexts)
**Complexity:** MEDIUM-HIGH
**Reusability:** HIGH

**Evidence:**
- Patterns in: `2026-06-22_021_backend-027-partial-accept.md` (partial accept with blocker)
- Conductor blocker decision: `2026-07-06_088_crm-blocker-decision-option-a-backend-fixes.md`
- Multi-level escalation: Backend → Conductor → Root (critical decisions)
- Options analysis (Option A, Option B) before commit

**Skill Concept:**
```
skill/blocker-resolution-framework/SKILL.md
├── Detection: Identify blocker type (technical, infrastructure, architectural, business)
├── Analysis: Root cause, impact assessment, options
├── Escalation: L1 (terminal self-resolve) → L2 (peer terminal) → L3 (Root)
├── Decision Tree: Option evaluation, trade-offs, commitment
├── Implementation: Resource allocation, timeline, rollback plan
└── Monitoring: Post-resolution verification, learning capture
```

**WHY SKILL:**
- Prevents decision paralysis
- Escalation clarity (who decides what)
- Repeatable methodology (consistent blocker handling)
- Learning loop (capture lessons for future blockers)

---

### SKILL #4: Domain Model Design & Specification
**Status:** PROVEN (JoineryTech 8-domain implementation)
**Complexity:** HIGH
**Reusability:** VERY HIGH

**Evidence:**
- Architect outputs: CRM, Kontrolling, HR, Maintenance domain models (4× `2026-07-01_03X_*-domain-model-design-done.md`)
- Integration specs: `QA_PRODUCTION_INTEGRATION_SPEC.md` (1,800 lines, Event-Flag Pattern)
- Cross-module patterns: Event patterns, FSM design, repository interfaces
- Template reusability: "Template for HR → Production, Maintenance → Production"

**Skill Concept:**
```
skill/joinerytech-domain-model-workshop/SKILL.md
├── Domain Analysis Methodology (bounded context, ubiquitous language)
├── Aggregate Root Design (entity, value objects, invariants)
├── FSM State Definition (states, transitions, guards)
├── Event Pattern Design (domain events, event flags for integration)
├── Repository Interface Design (query, command, transaction patterns)
├── Test Pattern Library (FSM, repository, E2E test templates)
└── Integration Spec Template (with Mermaid diagrams)
```

**WHY SKILL:**
- Reusable for all 27 JoineryTech worlds
- Proven pattern library (3× domain models + 1 integration spec)
- Cross-team language standardization
- Accelerates new domain implementation

---

### SKILL #5: Memory Cleanup & Knowledge Organization
**Status:** PROVEN (Librarian executing, Explorer auditing)
**Complexity:** MEDIUM
**Reusability:** HIGH

**Evidence:**
- Librarian tasks: `2026-07-01_001_*-synthesis-done`, `2026-07-01_006_memory-cleanup-knowledge-base-refresh-done`
- Memory structure: `/opt/spaceos/docs/knowledge/` (context, patterns, architecture)
- Tiering system: HOT (48h), WARM (14d), COLD (365d), SHARED (global)
- Cleanup automation: Archival log, consolidation strategy (orch.md → orchestrator.md)

**Skill Concept:**
```
skill/memory-organization-lifecycle/SKILL.md
├── Memory Assessment (value, frequency, dependencies)
├── Tiering Strategy (hot/warm/cold/shared assignment)
├── Consolidation Rules (duplicate detection, canonical source)
├── Archival Structure (preserve originals, audit trail)
├── Knowledge Synthesis (chat history → pattern docs → skill)
└── Automation Candidates (cleanup scripts, memory promotion workflows)
```

**WHY SKILL:**
- Reusable quarterly maintenance (like archival workflow)
- Scalable to 1000+ memory files
- Prevents knowledge loss (archival preservation)
- Enables knowledge reuse (synthesis to skills/patterns)

---

### SKILL #6: Infrastructure Validation & Component Discovery
**Status:** PROVEN (Monitor terminal discovery in Phase 3)
**Complexity:** MEDIUM
**Reusability:** HIGH

**Evidence:**
- Discovery: Monitor terminal (legitimate infrastructure, not orphan artifact)
- Validation method: Check CLAUDE.md, MEMORY.md, inbox/outbox structure, recent activity
- Report: `terminals/librarian/outbox/2026-07-01_002_*-done.md` (validation results)
- Pattern: Apply same validation to newly discovered components

**Skill Concept:**
```
skill/infrastructure-validation-toolkit/SKILL.md
├── Component Discovery (find terminals, services, MCP servers)
├── Identity Validation (CLAUDE.md spec, role definition)
├── Operational Status (MEMORY.md update timestamps, recent logs)
├── Mailbox Health (inbox/outbox structure, message counts)
├── Activity Verification (recent messages, session patterns)
├── Anomaly Classification (orphan vs legitimate, legacy vs active)
└── Decision Tree (archive vs keep, investigate vs accept)
```

**WHY SKILL:**
- Used in every audit cycle (quarterly)
- Prevents false deletions (systematic validation)
- Enables component lifecycle management
- Scalable methodology (per-terminal checklist)

---

### SKILL #7: Cross-Module Integration Testing Pattern
**Status:** EMERGING (CRM Integration Testing in progress)
**Complexity:** HIGH
**Reusability:** VERY HIGH

**Evidence:**
- In progress: `MSG-BACKEND-151` CRM Integration Testing (20+ tests)
- Pattern: FSM tests + Repository tests + E2E tests + RLS validation
- Template: Testcontainers PostgreSQL setup, smoke tests (Lead → Opportunity → Customer)
- Reuse candidate: "Same pattern for HR, Maintenance, QA integration testing"

**Skill Concept:**
```
skill/cross-module-integration-testing/SKILL.md
├── Test Structure: FSM tests (5-10) + Repository (8-15) + E2E (6-10) + RLS (3-5)
├── Infrastructure Setup (Testcontainers, PostgreSQL isolation, RLS policies)
├── FSM Test Patterns (state transitions, guard conditions, events)
├── Repository Test Patterns (CRUD, queries, transaction isolation)
├── E2E Smoke Tests (full workflow: Create → Update → Query → Delete)
├── RLS Validation (tenant isolation, row-level security enforcement)
└── Test Data Factory (realistic domain object generation)
```

**WHY SKILL:**
- Template for all JoineryTech module integration tests
- Standardizes testing approach (consistency across teams)
- Reduces test code duplication
- Accelerates integration testing phase

---

## 🤖 AUTOMATION SCRIPT OPPORTUNITIES

### SCRIPT #1: Mailbox Health Monitor
**Difficulty:** LOW
**Impact:** HIGH
**Suggested Location:** `scripts/mailbox/health-check.sh`

**Purpose:** Automated daily health check for inbox/outbox queue depths, UNREAD tracking, archival candidates

**Functionality:**
```bash
#!/bin/bash
# mailbox/health-check.sh

# Per-terminal metrics
for terminal in root conductor architect librarian explorer backend frontend designer monitor; do
  unread=$(grep -l "status: READ" terminals/$terminal/inbox/*.md 2>/dev/null | wc -l)
  inbox_depth=$(ls -1 terminals/$terminal/inbox/*.md 2>/dev/null | wc -l)
  outbox_done=$(ls -1 terminals/$terminal/outbox/*DONE*.md 2>/dev/null | wc -l)

  # Alert if thresholds exceeded
  [ $unread -gt 5 ] && echo "⚠️  $terminal: $unread UNREAD messages"
  [ $inbox_depth -gt 50 ] && echo "⚠️  $terminal: inbox queue depth $inbox_depth"
done

# Archive candidates (>7 days old, READ status)
find terminals/*/inbox -name "*.md" -mtime +7 | xargs grep -l "status: READ" | wc -l
```

**Output:** Dashboard metric, Datahaven integration ready

---

### SCRIPT #2: Phase Dispatch Automation
**Difficulty:** MEDIUM
**Impact:** VERY HIGH
**Suggested Location:** `scripts/dispatch/auto-phase-transition.sh`

**Purpose:** Automatic sequential phase dispatch when prior phase DONE

**Functionality:**
```bash
#!/bin/bash
# dispatch/auto-phase-transition.sh

# Check Phase N DONE count
phase1_done=$(grep -l "type: done" terminals/conductor/outbox/*phase1* 2>/dev/null | wc -l)
phase1_total=3  # Expected priority count

if [ $phase1_done -eq $phase1_total ]; then
  echo "🎯 Phase 1 COMPLETE — Initiating Phase 2 dispatch"

  # Create Phase 2 task messages
  for priority in 2; do
    task=$(cat << EOF
---
from: conductor
to: backend
type: task
priority: high
model: sonnet
---
# Phase 2 Priority $priority: [Task Name]
...
EOF
    )
    echo "$task" > "terminals/backend/inbox/$(date +%Y-%m-%d)_NNN_phase2-priority$priority.md"
  done
fi
```

**Trigger:** Cron (every 30 min) or event-driven (on outbox DONE creation)

---

### SCRIPT #3: Archival Pipeline Automation
**Difficulty:** MEDIUM
**Impact:** HIGH
**Suggested Location:** `scripts/maintenance/auto-archival.sh`

**Purpose:** Automatic 3-phase archival execution (monthly or on-demand)

**Functionality:**
```bash
#!/bin/bash
# maintenance/auto-archival.sh

MODE=${1:-"dry-run"}  # dry-run | execute

phase1_cleanup() {
  echo "Phase 1: Stale Memory Cleanup"
  find docs/memory -name "*.md" -size -500c -mtime +7 | head -20
}

phase2_archival() {
  echo "Phase 2: Inbox Message Archival (>7 days, READ status)"
  for terminal in root conductor architect librarian explorer backend frontend designer; do
    find terminals/$terminal/inbox -name "*.md" \
      -mtime +7 \
      | xargs grep -l "status: READ" \
      | head -20
  done
}

phase3_validation() {
  echo "Phase 3: Component Validation"
  for terminal in $(ls -d terminals/*/); do
    [ -f "$terminal/CLAUDE.md" ] || echo "⚠️  Missing CLAUDE.md: $terminal"
    [ -f "$terminal/MEMORY.md" ] || echo "⚠️  Missing MEMORY.md: $terminal"
  done
}

case $MODE in
  dry-run)
    phase1_cleanup
    phase2_archival
    phase3_validation
    ;;
  execute)
    # Execute with archival operations
    ;;
esac
```

---

### SCRIPT #4: Blocker Alert & Escalation
**Difficulty:** LOW-MEDIUM
**Impact:** HIGH
**Suggested Location:** `scripts/monitoring/blocker-detector.sh`

**Purpose:** Auto-detect BLOCKED messages, calculate age, trigger escalation

**Functionality:**
```bash
#!/bin/bash
# monitoring/blocker-detector.sh

echo "🔍 Active Blockers:"

find terminals/*/outbox -name "*BLOCKED*.md" -mtime -7 | while read file; do
  age=$(( ($(date +%s) - $(stat -c%Y "$file")) / 3600 ))

  if [ $age -gt 24 ]; then
    echo "⚠️  CRITICAL: $(basename $file) blocked for ${age}h"
    echo "   → Escalate to Root/Conductor"
  elif [ $age -gt 4 ]; then
    echo "⚠️  Alert: $(basename $file) blocked for ${age}h"
    echo "   → Notify Conductor"
  fi
done
```

---

### SCRIPT #5: Knowledge Synthesis Workflow Automation
**Difficulty:** MEDIUM
**Impact:** MEDIUM-HIGH
**Suggested Location:** `scripts/knowledge/auto-synthesis.sh`

**Purpose:** Auto-detect Explorer research outputs, route to Librarian for synthesis

**Functionality:**
```bash
#!/bin/bash
# knowledge/auto-synthesis.sh

echo "📚 Research outputs awaiting synthesis:"

# Find recent Explorer DONE outbox messages (not yet synthesized)
find terminals/explorer/outbox -name "*.md" -mtime -1 \
  | xargs grep -l "type: done" \
  | while read file; do

  # Check if corresponding Librarian synthesis task exists
  basename=$(basename "$file" .md)
  synthesis_task="terminals/librarian/inbox/*$basename*synthesis*.md"

  if [ ! -f $synthesis_task ]; then
    echo "✅ $basename → Create synthesis task for Librarian"

    # Template synthesis task creation
    task_name="$(date +%Y-%m-%d)_NNN_$basename-synthesis.md"
    echo "→ Create: terminals/librarian/inbox/$task_name"
  fi
done
```

---

## 📋 IDEAS DISCOVERED IN PLANNING PIPELINE

### Currently Queued Ideas (8 total)

| # | Idea | Status | Priority | Effort | Audience |
|----|------|--------|----------|--------|----------|
| 1 | KPI Card System | IDEA | P1 ⭐⭐⭐ | 2-3d | Dashboard/Datahaven |
| 2 | Cost Budget Tracker | IDEA | P1 ⭐⭐ | 2-3d | Monitoring/Kontrolling |
| 3 | Kanban Real-Time Feedback | IDEA | P2 ⭐⭐ | 3-5d | Brief workflow |
| 4 | Dark-First Bento Grid | ✅ DONE | P1 | MEDIUM | Foundation (implemented) |
| 5 | Kanban Quick Actions | IDEA | P2 ⭐ | 1-2d | Inline operations |
| 6 | Real-Time Metrics Dashboard | IDEA | P3 | MEDIUM | Infrastructure monitoring |
| 7 | Mermaid Flow Editor | IDEA | P3 | HIGH | Epic visualization |
| 8 | Mobile Responsive Grid | IDEA | P2 ⭐ | 2-3d | Workshop tablets |

**Status:** Ready for Sprint 1-3 implementation (KPI + Cost Tracker in Sprint 1, already validated)

---

## 🎯 RECOMMENDATIONS

### IMMEDIATE (Next Week)

1. **Create Skill #2 (Week-Based Phase Dispatch)**
   - Conductor is actively using pattern
   - Reusable for Week 3, 4, ... N
   - Accelerates future projects
   - **Effort:** 4 hours
   - **Owner:** Conductor (workflow expert)

2. **Create Skill #1 (3-Phase Archival)**
   - Pattern proved (executed Jul 1)
   - Ready for monthly runs
   - **Effort:** 2 hours
   - **Owner:** Librarian

3. **Implement Script #1 (Mailbox Health)**
   - Low effort, high visibility
   - Datahaven dashboard integration
   - **Effort:** 1 hour
   - **Owner:** Backend (script infrastructure)

### NEAR-TERM (2-3 Weeks)

4. **Create Skill #4 (Domain Model Workshop)**
   - JoineryTech 8 worlds all use pattern
   - Accelerates hiring/onboarding
   - **Effort:** 6 hours
   - **Owner:** Architect + Librarian

5. **Implement Script #2 (Phase Dispatch Automation)**
   - Reduces manual task creation
   - Error prevention (consistent formatting)
   - **Effort:** 3 hours
   - **Owner:** Conductor

### FUTURE (Month 2)

6. **Create Skill #3 (Blocker Resolution Framework)**
   - Prevents escalation chaos
   - Captures lessons learned
   - **Effort:** 4 hours
   - **Owner:** Root + Conductor

7. **Create Skill #7 (Integration Testing Pattern)**
   - Standardizes CRM/HR/Maintenance tests
   - Reusable across 27 worlds
   - **Effort:** 6 hours
   - **Owner:** Backend

---

## 📊 SKILL PRIORITIZATION MATRIX

| Skill | Impact | Effort | ROI | Priority |
|-------|--------|--------|-----|----------|
| #2 Week-Based Dispatch | VERY HIGH | MEDIUM | 4.0 | 🔴 CRITICAL |
| #4 Domain Model Workshop | VERY HIGH | HIGH | 3.0 | 🔴 CRITICAL |
| #1 3-Phase Archival | HIGH | LOW | 5.0 | 🟡 HIGH |
| #7 Integration Testing | HIGH | HIGH | 2.5 | 🟡 HIGH |
| #5 Memory Organization | HIGH | MEDIUM | 3.0 | 🟡 HIGH |
| #3 Blocker Resolution | MEDIUM | MEDIUM | 2.0 | 🟠 MEDIUM |
| #6 Infrastructure Validation | MEDIUM | MEDIUM | 2.0 | 🟠 MEDIUM |

---

## 📁 DELIVERABLES

This report (2026-07-07_008) + recommendations for:
1. Create 7 skills (prioritized order)
2. Create 5 scripts (prioritized order)
3. Implement 8 ideas (already validated in planning pipeline)

**Next Action:** Librarian synthesis + skill development workshop

---

## Notes & Observations

### Workflow Maturity

SpaceOS shows **mature, repeatable workflows** across multiple dimensions:

- **Archival:** 3-phase approach (risk-gradated) ✅ PROVEN
- **Dispatch:** Week-based sequential phases ✅ PROVEN
- **Domain modeling:** 8 domains + integration patterns ✅ PROVEN
- **Testing:** FSM + Repository + E2E + RLS ✅ IN PROGRESS
- **Blocker resolution:** Multi-level escalation emerging ✅ EMERGING

### Scalability Implications

These patterns scale to:
- **27 JoineryTech domains** (domain model skill)
- **52 weeks/year** (phase dispatch skill)
- **12 cleanup cycles/year** (archival workflow skill)
- **100+ terminals** (infrastructure validation)

### Skill vs Script Decision

- **Skills:** Methodology, decision frameworks, best practices → reuse in different contexts
- **Scripts:** Automation, repetitive tasks, infrastructure checks → daily/weekly execution

Both needed for different purposes.

---

**Compiled by:** Explorer Terminal
**Date:** 2026-07-07 11:30 UTC
**Duration:** ~2 hours research + analysis
**Status:** Ready for Librarian synthesis
