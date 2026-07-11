# MONITOR — Epic Progress Tracker

## Role: Development Encouragement Protocol

A Monitor terminál **intelligens ösztönzést** ad a Conductornak az épikek végig haladásához.

---

## EPIC-CUTTING-Q3 Lap szabász Modul (Aktív)

### Epic Milestone Path

```
PHASE 1: Foundation (Done)
  ✅ Kernel L1: Auth, Audit, FSM, Escrow
  ✅ Inventory v1: Stock management

PHASE 2: Core Modules (In Progress)
  🔄 Backend Week 2: JWT/OAuth integration
  🔄 Frontend Wave 2: CRM Form Validation
  ⏳ Next: Quote API, Nesting algorithms

PHASE 3: Production Features (Blocked by Phase 2)
  ⏳ CNC Integration
  ⏳ Nesting Optimization
  ⏳ Lapszabász UI

PHASE 4: Doorstar Soft Launch (Blocked by Phase 3)
  ⏳ Customer Portal
  ⏳ ShopFloor Kiosk
```

### Current Status (2026-07-04 08:27)

| Component | Status | Progress | Blocker | Next |
|-----------|--------|----------|---------|------|
| **Backend Week 2** | 🔄 IN PROGRESS | 80% | 12 compilation errors | Fix errors (1-2h) → Week 3 |
| **Frontend Wave 2** | 🔄 IN PROGRESS | 60% | Phase 1 (Form Validation) | Complete Phase 1-3 (6-9h) → Week 3 |
| **Nesting Algorithms** | ⏳ PENDING | 0% | Blocked by FE Wave 2 | After FE complete → Start |
| **CNC Integration** | ⏳ PENDING | 0% | Blocked by Nesting | Queue for Week 4 |
| **Quote API** | ⏳ PENDING | 0% | Blocked by Backend Week 2 | Start after Week 2 done |

---

## Monitor Intervention Points

### Intervention 1: Le-stop Detection (CONFIGURABLE)
**Trigger:** Conductor idle >X minutes + current task >Y% complete
- **Parameters from MONITOR-CONFIG.yaml:**
  - `phase_transition.conductor_idle_timeout_minutes` (default: 120)
  - `phase_transition.progress_threshold_percent` (default: 90)
- **Dynamic:** Not hard-coded, can be changed anytime in config

**Action:** Send "Folytatható munka — EPIC-CUTTING-Q3 Phase 3 ready" inbox

**Template:**
```markdown
---
from: monitor
to: conductor
type: task
priority: high
---

# Ösztönzés: EPIC-CUTTING-Q3 Következő Fázis Kiadva

## Befejezett
✅ Backend Week 2 compilation errors fixed
✅ Frontend Wave 2 Form Validation (Phase 1-3) complete

## READY Kiadásra
### Phase 3 Start:
1. **Backend MSG-BACKEND-124:** Nesting Algorithms implementation
2. **Backend MSG-BACKEND-125:** CNC Integration stub
3. **Frontend MSG-FRONTEND-101:** Quote Preview UI

## Javasolt Sorrend
1. Nesting Algorithms first (longest, critical path)
2. Quote API parallel
3. CNC Integration after Nesting

## Koordináció
Ezek a feladatok az EPIC-CUTTING-Q3 függőségeken alapulnak.
Kiadásra готов! ✅
```

### Intervention 2: Missing Critical Path
**Trigger:** Nesting Algorithms still pending after 48 hours
**Action:** Escalate to Root: "EPIC-CUTTING-Q3 critical path blocker"

---

## Conductor Workflow States

### State 1: ACTIVE (Working on assigned task)
- ✅ Current: Backend Week 2 errors fix + FE Wave 2 Phase 1-3
- 🟢 Continue monitoring
- ⏰ Check progress every 2 hours

### State 2: IDLE (Task complete, waiting for new assignment)
- 🛑 Triggers Monitor intervention
- 📊 Analyze EPIC state → Propose next 2-3 tasks
- 📤 Send "Ösztönzés" message

### State 3: STUCK (Idle >120min without explanation)
- 🚨 Escalate to Root
- 📋 Provide gap analysis + recommendations

---

## Monitor Health Check Enhancements (Cycle 23+)

### Enhanced Checklist
```bash
1. EPICS.yaml — Active epic status
2. Checkpoints — Progress tracking
3. Conductor — State machine (ACTIVE/IDLE/STUCK)
   ├─ Last outbox message timestamp
   ├─ Time since last output
   ├─ Task assignment status
4. BLOCKED messages — Threshold check
5. Nightwatch — Activity check

PLUS:

6. Epic Gap Analysis
   ├─ Completed phases (count)
   ├─ Current phase progress (%)
   ├─ Pending phases (list)
   ├─ Critical path next 3 tasks

7. Development Encouragement Logic
   ├─ If Conductor IDLE + >2 phases pending → SEND ÖSZTÖNZÉS
   ├─ If Conductor ACTIVE + >80% progress → MONITOR
   ├─ If Conductor STUCK >120min → ESCALATE ROOT
```

---

## Integration: MCP Task Creation

When Monitor detects le-stop:

```bash
mcp__spaceos-knowledge__create_task \
  --from monitor \
  --to conductor \
  --title "Ösztönzés: EPIC-CUTTING-Q3 Phase 3 Kiadásra Ready" \
  --priority high \
  --description "..."
```

---

## Expected Conductor Response Pattern

1. **Receives Monitor ösztönzés** ✅
2. **Analyzes proposed tasks** ✅
3. **Creates backend/frontend inbox messages** ✅
4. **Updates focus queue** ✅
5. **Continues pálya progression** ✅

---

## SUCCESS CRITERIA

✅ Epic phases complete in sequence
✅ No >2h gaps between task assignment
✅ Conductor proactively assigns next phase
✅ EPIC-CUTTING-Q3 reaches Phase 4 (Soft Launch) by target date

---

**Updated:** 2026-07-04
**Status:** Ready for cycle 23+ with enhanced monitoring
**Mode:** Hot — Project-aware development encouragement active
