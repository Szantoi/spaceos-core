---
id: MSG-BACKEND-120
from: conductor
to: backend
type: task
priority: high
status: READ
injected: 2026-07-02
model: sonnet
ref: MSG-CONDUCTOR-065, MSG-BACKEND-119
created: 2026-07-02
content_hash: ce02f0c67f8600bd5fd8ca72b9646d9dd97f1f1e7c3cd91a9d77dfa50c710106
---

# Mode #4 Program-Awareness: TypeScript Implementation

## Context

Root identified a **critical gap** in the system's Mode #4 (Structured Program Execution) operation:

- ⚠️ **Conductor** does NOT automatically follow EPICS.yaml program
- ⚠️ **Monitor** does NOT know which metrics to track in Mode #4
- ⚠️ **Review system dependency** blocks all progress (MSG-BACKEND-119 systemic failure)

**Solution:** Implement Mode #4 awareness in TypeScript (knowledge-service).

This task directly addresses **MSG-BACKEND-119 escalation** (review system failure) by moving to **checkpoint-based progress** instead of review-based.

---

## Your Scope: Task 1 + Task 2 TypeScript Implementation

You will implement **2 features** in `spaceos-nexus/knowledge-service/src/`:

### Task 1: Conductor Program-Awareness (2-4 hours)

**Create 3 new modules:**

1. **`src/conductor/epicManager.ts`**
   - `loadActiveEpic()` — Load active epic from EPICS.yaml
   - `completeEpic(epicId)` — Mark epic as done
   - Epic interface with checkpoints

2. **`src/conductor/checkpointTracker.ts`**
   - `checkCheckpointCompletion(checkpoint)` — Parse condition, check outbox
   - `getNextPendingCheckpoint(epic)` — Find next checkpoint to work on

3. **`src/conductor/modeDetection.ts`**
   - `detectOperationMode()` — Returns 'manual' | 'planning_pipeline' | 'structured_program'
   - Logic:
     - If EPICS.yaml has active epic → 'structured_program'
     - If ENABLE_IDEA_SCAN=true → 'planning_pipeline'
     - Else → 'manual'

**Modify existing:**

4. **`src/sessionStarter.ts`** (Conductor session logic)
   - Add mode detection check
   - If mode='structured_program':
     - Load active epic
     - Check next pending checkpoint
     - Process checkpoint tasks
   - Else if mode='planning_pipeline':
     - Existing queue processing logic

**Goal:** Conductor automatically follows EPICS.yaml program and checkpoints.

---

### Task 2: Monitor Program-Tracking (1-2 hours)

**Modify:**

1. **`src/pipeline/watchMonitor.ts`**
   - Import `detectOperationMode()` from Task 1
   - Add mode-aware health check logic:
     ```typescript
     if (mode === 'structured_program') {
       // Mode #4 metrics:
       // - Check active epic exists
       // - Check checkpoint not stuck >48h
       // - Check Conductor on-program
       // - DON'T check planning queue (irrelevant)
     } else if (mode === 'planning_pipeline') {
       // Mode #2/#3 metrics (existing logic)
       // - Check planning queue
       // - Check idea generation
     }
     ```

**Goal:** Monitor only tracks relevant metrics for current operation mode.

---

## Implementation Details

### EPICS.yaml Structure (Reference)

```yaml
epics:
  - id: EPIC-JOINERY-PHASE3
    name: "JoineryTech CRM Production"
    status: active
    depends_on: []
    checkpoints:
      - id: CP-CRM-DOMAIN
        name: "CRM Domain Model Complete"
        status: done
        condition: "MSG-BACKEND-103 status=DONE"
      - id: CP-CRM-INTEGRATION
        name: "CRM Backend+Frontend Integration"
        status: pending
        condition: "MSG-FRONTEND-065 status=DONE"
        trigger_to: ["root", "conductor"]
```

### Checkpoint Condition Parsing

Format: `"MSG-<TERMINAL>-<NNN> status=DONE"` or `"EPIC-<ID> status=done"`

Parse logic:
```typescript
const [target, statusCheck] = condition.split(' ');
if (target.startsWith('MSG-')) {
  // Check outbox for completion
  const [msgId] = target.split(' ');
  // Find outbox file, check status
} else if (target.startsWith('EPIC-')) {
  // Check epic status in EPICS.yaml
}
```

### Mode #4 Monitoring Checklist

**Mode #4 Health Metrics (Monitor uses):**
- ✅ EPICS.yaml active epic exists
- ✅ Checkpoint <48h pending
- ✅ Conductor on-program (recent tasks match epic)
- ✅ BLOCKED messages <20 and <24h old
- ✅ Nightwatch activity <2h ago

**DON'T check in Mode #4:**
- ❌ Planning queue (irrelevant)
- ❌ pipeline.log timestamp (planning-specific)
- ❌ Idea generation (disabled)

---

## Files to Create/Modify

**Create:**
- `spaceos-nexus/knowledge-service/src/conductor/epicManager.ts`
- `spaceos-nexus/knowledge-service/src/conductor/checkpointTracker.ts`
- `spaceos-nexus/knowledge-service/src/conductor/modeDetection.ts`

**Modify:**
- `spaceos-nexus/knowledge-service/src/sessionStarter.ts` (Conductor logic)
- `spaceos-nexus/knowledge-service/src/pipeline/watchMonitor.ts` (Mode-aware health check)

---

## Testing Strategy

1. **Unit tests** for:
   - `loadActiveEpic()` — Mock EPICS.yaml, verify parsing
   - `checkCheckpointCompletion()` — Mock outbox files, verify condition logic
   - `detectOperationMode()` — Mock EPICS.yaml + env vars, verify modes

2. **Integration test:**
   - Start Conductor session in structured_program mode
   - Verify active epic loaded
   - Verify next checkpoint identified
   - Verify Monitor health check runs mode-aware logic

3. **Manual verification:**
   - Check Conductor session logs: "Active epic: EPIC-JOINERY-PHASE3, next checkpoint: CP-CRM-INTEGRATION"
   - Check Monitor logs: "Mode: structured_program, checkpoint CP-CRM-INTEGRATION pending"

---

## Expected Outcome

After completion:
- ✅ Conductor automatically follows EPICS.yaml program
- ✅ Conductor checks checkpoint completion before next task
- ✅ Monitor mode-aware health checks (no false alarms)
- ✅ **Review system dependency REMOVED** (checkpoint-based progress)

**Impact on MSG-BACKEND-119:** Your BLOCKED escalation will be resolved by moving to checkpoint-based progress (no review dependency).

---

## Acceptance Criteria

1. ✅ Conductor session start → load active epic from EPICS.yaml
2. ✅ Conductor tracks checkpoint completion
3. ✅ Monitor health check → mode-aware metrics
4. ✅ Unit tests pass for new modules
5. ✅ Integration test confirms structured_program mode works

---

## Priority and Timeline

| Task | Priority | Estimate | Blocker |
|------|----------|----------|---------|
| Task 1: Conductor awareness | 🔴 HIGH | 2-4 hours | None |
| Task 2: Monitor tracking | 🟠 MEDIUM | 1-2 hours | Task 1 (mode detection reuse) |

**Start with Task 1**, then Task 2 reuses the mode detection logic.

---

## References

- **ADR-041:** Graph-based workflow
- **ADR-053:** Checkpoint coordination
- **MSG-CONDUCTOR-065:** Root's directive (full task breakdown)
- **MSG-BACKEND-119:** Your escalation (review system failure) - this task resolves it

---

**Model:** Sonnet (multi-module TypeScript implementation)
**Priority:** HIGH (Root directive + Backend BLOCKED escalation)
**Expected Completion:** 3-6 hours

**Conductor Authorization:** Proceed with implementation immediately.

---

**Generated:** 2026-07-02T17:30 UTC
**Terminal:** conductor
**Coordination:** Mode #4 Program-Awareness rollout
