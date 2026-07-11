# Conductor Terminal Memory — Updated 2026-07-10

## ROLE & IDENTITY

**Primary Mission:** SpaceOS Agent Fleet Orchestrator — Daily task coordination, pipeline management, terminal dispatch

### Telegram Aliases
- **Primary:** conductor (technical)
- **Secondary:** karmester (Hungarian)
- **Tertiary:** orchestrator (English)

### Responsibilities
1. Session Startup: Check planning queue, terminal status, system health
2. Task Dispatch: Process ROOT escalations, coordinate terminal assignments
3. Epic Tracking: Monitor Q3/Q4 tracks progress, multi-week coordination
4. Status Maintenance: Update `docs/Codebase_Status.md`, focus queue management
5. Pipeline Coordination: Planning queue → debate → consensus → terminal dispatch

---

## PÁRHUZAMOS TERMINÁLOK (2026-07-10)

**9 aktív terminál** — Backend és Frontend duplázva párhuzamos fejlesztéshez:

| Terminál | Session | Használat |
|----------|---------|-----------|
| **backend** | spaceos-backend | Elsődleges backend (Kernel, Core) |
| **backend-2** | spaceos-backend-2 | Párhuzamos backend (Joinery, Cutting) |
| **frontend** | spaceos-frontend | Elsődleges UI (Portal, Core) |
| **frontend-2** | spaceos-frontend-2 | Párhuzamos UI (Dashboard, Features) |

### Mikor használd a párhuzamos terminálokat?

**✅ IGEN:**
- Két független modul fejlesztése (pl. Kernel + Joinery)
- Két független UI feature (pl. Portal + Dashboard)
- Gyorsítás: 2× throughput azonos epic-en belül

**❌ NEM:**
- Függő feladatok (egyik a másik output-jára vár)
- Ugyanazon fájlok módosítása (merge conflict!)
- Egyetlen kis feladat

### Dispatch Példák

```bash
# Backend párhuzamos
curl -X POST localhost:3456/api/session/start \
  -d '{"terminal":"backend","prompt":"MSG-BACKEND-195: Kernel FSM","fromTerminal":"conductor"}'
curl -X POST localhost:3456/api/session/start \
  -d '{"terminal":"backend-2","prompt":"MSG-BACKEND-196: Joinery API","fromTerminal":"conductor"}'

# Frontend párhuzamos
curl -X POST localhost:3456/api/session/start \
  -d '{"terminal":"frontend","prompt":"MSG-FRONTEND-070: Portal Quote","fromTerminal":"conductor"}'
curl -X POST localhost:3456/api/session/start \
  -d '{"terminal":"frontend-2","prompt":"MSG-FRONTEND-071: Dashboard KPI","fromTerminal":"conductor"}'
```

### Inbox Routing

- `backend` inbox: `/opt/spaceos/terminals/backend/inbox/`
- `backend-2` inbox: `/opt/spaceos/terminals/backend-2/inbox/`
- `frontend` inbox: `/opt/spaceos/terminals/frontend/inbox/`
- `frontend-2` inbox: `/opt/spaceos/terminals/frontend-2/inbox/`

---

## EXPLORER ↔ LIBRARIAN KOORDINÁCIÓ

**Szerepek:**
- **Explorer** = Tudásbányász (kutat: kódbázis, web, chat history)
- **Librarian** = Tudásszintetizáló (olvasólista, knowledge docs, memória)

**Workflow:**
```
Explorer kutat → outbox
    ↓ Conductor észleli
Librarian inbox → szintetizál → knowledge docs
    ↓ Ha releváns
Terminálok értesítése (olvasólista)
```

**Conductor API hívások:**
```bash
# Explorer indítás
curl -X POST http://localhost:3456/api/session/start \
  -d '{"terminal":"explorer","model":"sonnet","prompt":"Napi kutatás","fromTerminal":"conductor"}'

# Librarian indítás
curl -X POST http://localhost:3456/api/session/start \
  -d '{"terminal":"librarian","model":"sonnet","prompt":"Szintetizálás","fromTerminal":"conductor"}'
```

---

## SESSION MANAGEMENT API RÉSZLETEK

**Endpointok (localhost:3456):**

```bash
# Session indítás prompttal
POST /api/session/start
  {"terminal":"...", "model":"...", "prompt":"...", "fromTerminal":"conductor"}

# Prompt injection futó session-be
POST /api/session/inject
  {"terminal":"...", "prompt":"...", "fromTerminal":"conductor"}

# Wake-up (start + inbox olvasás)
POST /api/session/wake
  {"terminal":"...", "fromTerminal":"conductor"}

# Státusz
GET /api/session/:terminal
GET /api/sessions/all

# Audit logok
GET /api/sessions/logs?days=1
```

**❌ NE használd közvetlenül a tmux-ot!**
```bash
# ROSSZ - nincs audit
tmux send-keys -t spaceos-architect "..." Enter

# HELYES - MCP API
curl -X POST http://localhost:3456/api/session/inject -d '...'
```

---

## GRAPH API PÉLDÁK

```bash
# Epic gráf
curl -s http://localhost:3456/api/graph/epics | jq '.graph.nodes'

# Critical path
curl -s http://localhost:3456/api/graph/critical-path/epic/EPICS

# Párhuzamos csoportok
curl -s http://localhost:3456/api/graph/parallel/epic/EPICS

# Mermaid (vizualizáció)
curl -s http://localhost:3456/api/graph/mermaid/epic/EPICS

# Validáció
curl -X POST http://localhost:3456/api/graph/validate -d '{"type":"epic"}'
```

**TypeScript típusok:** `spaceos-nexus/knowledge-service/src/graph/types.ts`

---

## PROJEKT KOORDINÁCIÓ FELELŐSSÉG

**Figyeled:**
- Epic dependency-k blokkolnak-e?
- Terminálok kapacitása elégséges-e?
- Review bottleneck van-e?

**Javaslat Root-nak ha:**
- Koordinációs tool hiányzik
- Epic management nehézkes
- Task dispatch ineffektív

**Nexus Tool Request Format:**
```markdown
---
from: conductor
to: root
type: tool-request
priority: medium
---
# Nexus Tool Request: [Név]
## Problem: [Ismétlődő pain point]
## Proposed Tool: [MCP tool spec]
## Use Case: [Konkrét use case]
## Expected Time Saving: [Becslés]
```

---

## MODE #4: PROGRAM-AWARE ORCHESTRATION

**Status:** PRODUCTION READY

### Capabilities
- Task Awareness: MCP `get_terminal_status` for real-time tracking
- Dependency Tracking: Focus Queue blocks dependent tasks
- BLOCKED Triage: 88% auto-resolution rate

### Components
- **Focus Queue:** `docs/tasks/active/`
- **Epic-Aware Routing:** EPICS.yaml checkpoints
- **Monitor Coordination:** Health checks → escalations
- **Intelligent Briefing:** Context-aware task summaries

---

## ACTIVE EPICS (2026-07-10)

### EPIC-DOORSTAR-SOFTLAUNCH (ACTIVE)
- **Progress:** 10% (Phase 1 COMPLETE, 14 days early)
- **Phase 1:** Planning ✅ DONE 2026-07-08
- **Phase 2:** Execution ⏳ Awaiting Cabinet approval
- **Backend Plan:** MSG-BACKEND-194 (25,693 bytes, 6-stage workflow)

### EPIC-JT-EHS (COMPLETE)
- **Status:** ✅ DONE 2026-07-08
- **Modules:** 7/7 production ready (CRM, Kontrolling, HR, Maintenance, QA, DMS, Embedding)
- **Achievement:** All modules operational, frontend integration complete

---

## COST-EFFICIENT CODEGEN

### Haiku vs Sonnet Economics
| Model | Input $/M | Output $/M | Use Case |
|-------|-----------|------------|----------|
| Haiku | $0.25 | $1.25 | Repetitive codegen |
| Sonnet | $3.00 | $15.00 | Complex planning |
| Opus | $15.00 | $75.00 | Architecture |

**ROI:** 5 Haiku parallel = 1 Sonnet price, 5× faster

### Codegen Dispatcher Pattern
```typescript
// Parallel Haiku spawn for batch tasks
const workers = items.map(item => ({
  id: `codegen-${item.name}`,
  model: 'haiku',
  prompt: generatePromptFromTemplate(task.template, item)
}));
await spawn_parallel_workers({ terminal, tasks: workers });
// Result: 5 files in ~10 sec, ~$0.05 cost
```

---

## PENDING CRITICAL WORK

### MSG-CONDUCTOR-006 (Root, 2026-07-08)
**Task:** ADR Validation Gate in Specification Generator
- **Impact:** Prevents 152+ hours future losses (DMS, Kontrolling, QA, Cabinet)
- **Status:** Acknowledged, awaiting Root timing decision
- **Complexity:** 2-3 hour implementation

---

## CRITICAL INFRASTRUCTURE ISSUES (2026-07-09/10)

### 1. blocker-detector.sh — DUPLICATE ESCALATIONS
**Bug:** 77+ hourly duplicates for RESOLVED blocker (MSG-BACKEND-184)
- **Fix:** Check for DONE file before escalating (see MSG-CONDUCTOR-1009)
- **Impact:** Session noise saturation (95%+ of messages)

### 2. Monitor Automated Checks — OUTDATED DATA
**Bug:** Requesting JoineryTech work 4+ days after COMPLETE
- **Fix:** Update Monitor check data source to current epic status

### 3. chat-root Notifications — 215+ DUPLICATES
**Bug:** MSG-ROOT-001-RESPONSE (18-day-old message, 10-min intervals)
- **Fix:** Implement notification deduplication

---

## Q3 TRACKS

### Track A (Customer Portal)
- Backend MSG-030, MSG-031
- Frontend MSG-018
- Status: Frontend DONE, Backend Phase 3-5 pending

### Track B (Pricing) — Pending
### Track C (ShopFloor) — Pending

---

## SESSION LEARNINGS

1. **Always verify existing implementation** — Mode #4 was production-ready
2. **Hungarian status reports** — Appreciated by user
3. **BLOCKED triage effectiveness** — 88% auto-resolution
4. **Multi-track dispatch** — Parallel A/B/C tracks reduce time
5. **Goal-Driven Automation WORKS** — GOAL-2026-07-08-382 perfect execution (70-80% cost savings)
6. **Context Saturation Handling** — PAUSED mode + minimal responses effective for extreme noise
7. **Infrastructure Fixes CRITICAL** — 77+ duplicates = session unusable, fix before next session

---

## ANTI-PATTERNS (AVOID!)

**DO NOT add to this memory:**
- Full session narratives (keep only key decisions)
- Cycle-by-cycle progress logs
- Detailed task lists (use Focus Queue)

**Memory should stay <30KB** — only orchestration context.

---

_Last Updated: 2026-07-10 05:06_
_Session 2026-07-09/10: Extreme context saturation (300+ messages), session PAUSED, memory saved_

---

_Updated: 2026-07-11_

## 2026-07-11 Session — JoineryTech 100% Completion Discovery

### EPIC Achievement: All 7 JoineryTech Modules DONE

**Session Duration:** 01:00-05:00 UTC (4 hours)
**Turn Count:** 34/50 (68% used)
**Model:** sonnet

**Tasks Completed:**
- MSG-456 DONE processing (CRM Phase 1) — CP-CRM-INTEGRATION → done
- MSG-457 created + dispatched (HR Employee Domain, 60 NWT)
- MSG-457 DONE processing — HR Domain complete
- MSG-458 created + dispatched (EHS→HR Integration, 30 NWT)
- MSG-458 DONE processing — CP-EHS-HR-INTEGRATION → done

**Discovery Process:**
1. Assumed CP-DMS-SALES-INTEGRATION as 4th checkpoint (3/4 progress)
2. Grepped EPICS.yaml → checkpoint doesn't exist!
3. Complete audit: Only 3 integration checkpoints exist
4. Deeper audit: ALL 7 modules, 18/18 checkpoints DONE
5. Corrected reports sent to Monitor

**JoineryTech Final Status:**
- 7 modules: CRM, Kontrolling, HR, Maintenance, QA, EHS, DMS
- 18 checkpoints: All marked `status: done` in EPICS.yaml
- 200+ API endpoints, 350+ tests PASSING, 0 errors
- Production-ready: 8 days development (2026-07-03 → 2026-07-11)

### Backend Session Lifecycle Pattern (CONFIRMED)

**Pattern:** Backend session **auto-closes** after writing DONE outbox

**Evidence:**
- Restart #1: 02:29 UTC (MSG-456, 71 min stall → 8 min recovery)
- Restart #2: 03:58 UTC (MSG-458, session auto-close pattern)

**Mitigation:** Manual API restart via localhost:3456
```bash
curl -X POST http://localhost:3456/api/session/start \
  -d '{"terminal":"backend","model":"sonnet","prompt":"...","fromTerminal":"conductor"}'
```

**Success Rate:** 2/2 restarts successful (100%)

**Implications:**
- Every new Backend task requires session restart
- API timeout message (15s) ≠ failure (session actually starts)
- Inbox watcher does NOT auto-restart for READ tasks

### EPICS.yaml Verification — CRITICAL LESSON

**Problem:** Assumed CP-DMS-SALES-INTEGRATION existed (4th checkpoint)
**Reality:** Only 3 integration checkpoints exist across all JoineryTech epics

**Lesson:** ALWAYS verify checkpoint existence before planning:
```bash
grep -n "CP-<NAME>" /opt/spaceos/docs/projects/EPICS.yaml
```

**Don't assume checkpoint count** — read EPICS.yaml sections completely.

### Efficiency Metrics

**Backend Tasks (Current Session):**
- MSG-456: 15 NWT estimated → ~30 min actual (foundation reuse)
- MSG-457: 60 NWT estimated → 45 actual (25% faster, CRM patterns)
- MSG-458: 30 NWT estimated → ~30 actual (as expected)

**Total:** 105 NWT → 75 actual (29% efficiency gain, 83% of estimate)

**Factors:**
- Module foundation patterns (DDD/CQRS/MediatR)
- Existing architecture (RLS, Testcontainers, FSM)
- Cross-module event pattern reuse

### Monitor Zaklatás Pattern

**Observed:** Monitor sent 5× identical 30-minute progress check message
**Content:** "Folytatható munka észlelve: outbox DONE: 5 | planning: 14"

**Issue:** Monitor not reading Conductor outbox reports (2× UNREAD)
- MSG-MONITOR-MILESTONE-CORRECTION (04:52 UTC)
- MSG-MONITOR-JOINERYTECH-COMPLETE (04:58 UTC)

**Resolution:** User noted → escalate to Root if Monitor becomes disruptive

### Reports Sent to Monitor

1. **Report #14** (04:39 UTC): 3/4 checkpoints milestone (INCORRECT — superseded)
2. **Report #27** (04:52 UTC): Corrected to 3/3 integration checkpoints
3. **Report #28** (04:58 UTC): FINAL — JoineryTech 100% complete (7/7 modules)

### Next Phase Options (Awaiting Decision)

**A) Production Deployment** (recommended):
- Staging VPS deployment
- Integration testing (7 modules together)
- Performance testing (load/stress/endurance)
- Security audit (OWASP, RLS validation)
- UAT (Doorstar pilot?)

**B) Additional Development:**
- AI module (EPIC-AI-BACKEND exists in EPICS.yaml)
- Additional integrations (DMS→Sales, QA→Production)
- Admin portal, analytics/reporting

**C) Platform Expansion:**
- Multi-tenant onboarding automation
- Mobile app (React Native?)
- Advanced analytics dashboard

**Current Status:** IDLE, awaiting Root/Monitor strategic direction

### Technical Debt & Learnings

**MCP Tools Failing:** All session used file-based fallbacks (grep, read, write)
- list_inbox → grep + read frontmatter
- read_inbox_message → direct file read
- submit_done → write + frontmatter update

**Context Saturation:** 34/50 turns (68%) — healthy, 16 turns remaining

**Cost Estimate:**
- Conductor: ~$0.85 (34 turns)
- Backend (3 tasks): ~$5.70
- **Total:** ~$6.55 (within budget)
