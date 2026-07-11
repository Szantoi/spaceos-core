---
id: MSG-MONITOR-005
from: root
to: monitor
type: task
priority: high
status: READ
injected: 2026-07-02
model: sonnet
ref: MSG-MONITOR-004
created: 2026-07-02
---

# Intelligent Conductor Briefing System — Mode #4 Context-Aware Wakup

Monitor, **kritikus fejlesztés** a Mode #4 (Structured Program Execution) működéséhez.

## 🎯 Problem Statement

**Jelenleg:**
- Conductor wake-up → UNREAD inbox üzenetek olvasása
- **NINCS kontextus** hogy mi történt az előző session óta
- **NINCS láthatóság** hogy hol tart a program (EPICS.yaml)
- Conductor "hidegindulás" = elveszett kontextus

**Következmény:**
- Conductor letér a programról
- Felesleges munka (már kész dolgok újracsinálása)
- Missed checkpoints (nem tudja hogy checkpoint teljesült-e)

---

## ✅ Megoldás: Intelligent Conductor Briefing

**Minden Conductor wake-up előtt** (UNREAD inbox detection vagy scheduled nudge):

Monitor generál egy **BRIEFING üzenetet** Conductor inbox-ba:

```
Conductor-nak BRIEFING:
1. Mi történt az előző session óta (outbox summary)
2. Hol tart a program (EPICS.yaml active epic + checkpoints)
3. Mi a következő prioritás (következő checkpoint vagy task)
4. Blokkolók státusza (BLOCKED messages triage)
```

---

## 📋 Briefing Message Template

**File:** `terminals/conductor/inbox/YYYY-MM-DD_NNN_monitor-briefing-contextual-wakeup.md`

**Frontmatter:**
```yaml
---
id: MSG-CONDUCTOR-NNN
from: monitor
to: conductor
type: briefing
priority: medium
status: READ
injected: 2026-07-02
model: haiku
created: YYYY-MM-DD
briefing_interval: 2h  # Időköz az előző briefing óta
---
```

**Content struktúra:**

```markdown
# Conductor Briefing — [TIMESTAMP]

## 📊 Program Status (Mode #4)

**Active Epic:** EPIC-GRAPH-WORKFLOW
**Progress:** 2/5 checkpoints completed (40%)
**Status:** 🟢 ON TRACK

### Checkpoint Status
- [x] CP-001: Flow editor Datahaven complete
- [x] CP-002: Datahaven UI Phase 6 complete
- [ ] CP-003: JoineryTech port (IN PROGRESS) ← **NEXT PRIORITY**
- [ ] CP-004: Multi-module testing
- [ ] CP-005: Production deployment

---

## 🔄 Recent Activity (Last 2h)

### Terminals Working
- ✅ Backend: CRM Week 2 complete (MSG-BACKEND-103 → DONE)
- ⏸️ Frontend: Idle (waiting for design spec)
- ⚠️ Backend: NuGet fix pending (infrastructure blocker)

### Decisions Made
- Root: Backend manual review bypass authorized (MSG-CONDUCTOR-064)
- Root: Mode #4 infrastructure development tasked (MSG-CONDUCTOR-065)

### Outbox Summary (since last briefing)
- 3 DONE messages reviewed
- 1 BLOCKED escalated to Root
- 0 new inbox tasks arrived

---

## 🎯 Next Priority Actions

### 🔴 CRITICAL (DO FIRST)
1. **MSG-CONDUCTOR-065: Mode #4 program-awareness implementation**
   - Task: Implement epicManager.ts + checkpointTracker.ts
   - Estimate: 4-7 óra
   - Why critical: Enables automatic EPICS.yaml tracking

2. **Backend NuGet fix verification**
   - Check: MSG-CONDUCTOR-064 completion state
   - If done → unblock Backend Week 3 tasks

### 🟠 HIGH (DO NEXT)
3. **BLOCKED triage** (21 messages)
   - Categorize: review timeout vs infra vs dependency
   - Escalate business blockers to Root

### 🟢 MEDIUM
4. **Checkpoint CP-003 progress check**
   - Frontend: JoineryTech port work started?
   - If not → nudge or assign

---

## ⚠️ Blockers & Alerts

### Active Blockers
- **Backend build:** NuGet timeout (infrastructure, Root handling)
- **Frontend:** OpenAPI spec review >13h blocked

### Mode #4 Awareness
- Planning queue ÜRES = **NORMAL** (Mode #4 nem használ planning pipeline-t)
- Pipeline log old = **NORMAL** (planning-specific, nem releváns)

---

## 📌 Conductor Action Items

**Recommendation:**
1. Process MSG-CONDUCTOR-065 FIRST (Mode #4 infrastructure)
2. Verify Backend NuGet fix (unblock Week 3)
3. Triage BLOCKED messages (30-60 min)
4. Check CP-003 progress (Frontend JoineryTech port)

**Session goal:** Make progress on Mode #4 infrastructure + unblock Backend

---

**Briefing generated:** [TIMESTAMP]
**Next briefing:** +2h or on significant event
**Program state:** EPICS.yaml active epic tracked
```

---

## 🔧 Implementation Spec

### Monitor Health Check Logic (Módosítás)

**File:** `src/pipeline/watchMonitor.ts`

**Új funkció:** `generateConductorBriefing()`

```typescript
interface BriefingData {
  activeEpic: Epic | null;
  checkpointProgress: CheckpointStatus[];
  recentOutbox: OutboxSummary[];
  recentDecisions: Decision[];
  nextPriority: PriorityAction[];
  blockers: BlockerAlert[];
  modeContext: ModeContext;
}

async function generateConductorBriefing(): Promise<string> {
  // 1. Load EPICS.yaml active epic
  const activeEpic = await loadActiveEpic();

  // 2. Check checkpoint completion
  const checkpoints = await getCheckpointStatus(activeEpic);

  // 3. Recent terminal activity (last 2h)
  const recentOutbox = await getRecentOutbox(2 * 60 * 60 * 1000); // 2h

  // 4. Root decisions (from Root outbox)
  const recentDecisions = await getRootDecisions(2 * 60 * 60 * 1000);

  // 5. Next priority (from checkpoint + inbox)
  const nextPriority = await determinePriority(activeEpic, checkpoints);

  // 6. Active blockers
  const blockers = await getActiveBlockers();

  // 7. Mode context (Mode #4 specific guidance)
  const modeContext = getModeContext();

  // 8. Generate markdown
  return formatBriefingMessage({
    activeEpic,
    checkpoints,
    recentOutbox,
    recentDecisions,
    nextPriority,
    blockers,
    modeContext,
  });
}
```

### Trigger Logic

**Mikor generálódik briefing:**

1. **Conductor wake-up detection** (UNREAD inbox vagy nudge előtt)
   ```typescript
   if (conductorHasUnreadInbox() || conductorNudgeScheduled()) {
     await generateAndSendBriefing('conductor');
   }
   ```

2. **Scheduled briefing** (2 óránként ha Conductor aktív)
   ```typescript
   if (conductorSessionActive() && timeSinceLastBriefing() > 2 * 60 * 60 * 1000) {
     await generateAndSendBriefing('conductor');
   }
   ```

3. **Significant event** (checkpoint completed, epic státusz change)
   ```typescript
   eventBus.on('checkpoint_completed', async (checkpoint) => {
     await generateAndSendBriefing('conductor', { trigger: 'checkpoint', checkpoint });
   });
   ```

---

## 🎯 Expected Benefits

### Before Briefing System
- ❌ Conductor "hidegindulás" minden wake-up-kor
- ❌ NINCS kontextus mi történt
- ❌ NINCS EPICS.yaml program láthatóság
- ❌ Eltérés a programtól (nem tudja mi a priority)

### After Briefing System
- ✅ Conductor **azonnal látja** hol tart a program
- ✅ **Kontextus** az előző session óta (outbox, decisions)
- ✅ **Prioritás** egyértelmű (következő checkpoint)
- ✅ **Blokkolók** láthatóak (triage vagy wait)
- ✅ **Mode #4 guidance** (false alerts elkerülése)

---

## 📋 Acceptance Criteria

**Monitor implementation DONE amikor:**

1. ✅ `generateConductorBriefing()` function létezik watchMonitor.ts-ben
2. ✅ Briefing trigger logic implementálva (wake-up, scheduled, event-based)
3. ✅ EPICS.yaml parsing + checkpoint status tracking működik
4. ✅ Recent outbox/decision aggregation működik
5. ✅ Priority determination logic helyes (checkpoint-based)
6. ✅ Briefing markdown formatting jó (human-readable + actionable)
7. ✅ Conductor inbox-ba küldés automatikus

**Test scenario:**
```
1. Conductor idle
2. New UNREAD inbox érkezik
3. Monitor generates briefing → terminals/conductor/inbox/
4. Conductor wake-up → reads briefing FIRST
5. Conductor processes tasks with full context
```

---

## 🔗 Related

- **MSG-CONDUCTOR-065:** Mode #4 program-awareness implementation
- **MSG-MONITOR-004:** Mode #4 context correction
- **ADR-053:** Checkpoint-based coordination
- **MEMORY.md:** Mode #4 paradigma (lines 3-162)

---

## ⏱️ Timeline

**Priority:** HIGH (Mode #4 critical component)
**Estimate:** 2-3 óra implementation
**Dependencies:**
- EPICS.yaml parsing (reuse from MSG-CONDUCTOR-065)
- Outbox aggregation (existing mailbox.ts)

**Parallel work:** Conductor MSG-CONDUCTOR-065 implementation

---

**Session mode:** Normal — Monitor dolgozd fel amikor Conductor program-awareness kész
**Model:** Sonnet (markdown generation + aggregation logic)
**Ref:** Mode #4 Structured Program Execution infrastructure
