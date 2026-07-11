## 🧠 CONTEXT PERSISTENCE — MCP TOOLS (2026-07-07)

> **Új MCP eszközök a Goal Drift Prevention támogatására!**
> Használd őket a context window kezelésére és a fókusz megőrzésére.

---

### MIÉRT HOZTUK LÉTRE? — Elméleti Alap

**Probléma:** Long-running agent sessionök során **goal drift** lép fel — a terminál "elfelejti" az eredeti célt.

**5 Failure Mode azonosítva:**

1. **Subtask Overfocus** — Részletbe merülés, fő cél elhanyagolása
2. **Context Dilution** — Túl sok információ, elvész a fő cél
3. **Inherited Drift** — DONE outbox-ok eltérítik az irányt
4. **Long Horizon Loss** — Hosszú epic-eknél elvész az end-state látképe
5. **Milestone Blindness** — Nem ismeri fel mikor van kész

**6 Solution Pattern implementálva:**

1. **STATUS.md** — Current state snapshot (system_status, current_focus, recent_actions, next_steps)
2. **.session-state.json** — Cross-session goal recovery (epicId, progress, checkpoints, last task)
3. **.turn-count** — Context saturation tracking (WARNING >30, CRITICAL >50 turn)
4. **CHECKPOINTS.md** — Milestone tracking (GO/NO-GO decision points)
5. **Goal Re-Anchoring** — Session start context loading
6. **Dense Milestone Feedback** — Epic progress explicit frissítés

**Context Saturation Thresholds:**
- **0-29 turn:** ✅ OK — Normál működés
- **30-49 turn:** ⚠️ WARNING — Goal drift veszély, fókuszálj!
- **≥50 turn:** 🚨 CRITICAL — Auto re-anchor vagy session reset kötelező!

**Implementáció:** 13 új MCP tool a context persistence file-ok kezelésére.

**Referencia:**
- `docs/knowledge/patterns/GOAL_PERSISTENCE_PATTERNS.md`
- `docs/knowledge/patterns/TERMINAL_CONTEXT_PERSISTENCE_FILES.md`
- `spaceos-nexus/knowledge-service/src/contextPersistence.ts`

---

### SESSION START RITUAL (KÖTELEZŐ!)

**Minden session elején (első 3-5 percben):**

```typescript
// 1. Session context betöltése (automatikus goal re-anchoring)
mcp__spaceos-knowledge__build_session_start_context
  terminal: "<terminal-neve>"

// 2. Context saturation ellenőrzés
mcp__spaceos-knowledge__get_context_saturation
  terminal: "<terminal-neve>"
```

**Mit kapsz:**
- **STATUS.md snapshot** — Mi volt az utolsó állapot? Mi a current focus?
- **Session state** — Melyik epic, melyik checkpoint, mennyi a progress?
- **Turn count + threshold** — Hány turn volt, milyen közel vagy a WARNING/CRITICAL-hoz?

**Példa output:**
```json
{
  "terminal": "conductor",
  "turnCount": 13,
  "status": "ok",  // "ok" | "warning" | "critical"
  "thresholds": {
    "warning": 30,
    "critical": 50
  },
  "sessionState": {
    "epicId": "EPIC-CUTTING-Q3",
    "epicProgress": 25,
    "nextCheckpointId": "CP-KERNEL-FSM",
    "lastActiveTask": "MSG-BACKEND-045"
  },
  "statusMd": {
    "system_status": "in_progress",
    "current_focus": "Kernel FSM implementation",
    "recent_actions": ["..."],
    "next_steps": ["..."]
  }
}
```

**Ha WARNING vagy CRITICAL:**
```typescript
// Újraolvassuk a fő célt
mcp__spaceos-knowledge__read_session_state
  terminal: "<terminal-neve>"

// STATUS.md explicit check: mi volt a focus?
mcp__spaceos-knowledge__read_terminal_status_md
  terminal: "<terminal-neve>"
```

---

### DURING WORK — FÓKUSZ TRACKING

**Minden 10-15 turn után (vagy major milestone után):**

```typescript
// Turn count increment (manuális vagy automatikus)
mcp__spaceos-knowledge__increment_turn_count
  terminal: "<terminal-neve>"
  amount: 1

// Context saturation check
mcp__spaceos-knowledge__get_context_saturation
  terminal: "<terminal-neve>"
```

**Threshold Action Table:**

| Turn Count | Status | Teendő |
|------------|--------|--------|
| **0-29** | ✅ OK | Normál működés |
| **30-49** | ⚠️ WARNING | **FÓKUSZÁLJ!** Térj vissza az epic fő céljához. Ne merülj új részletekbe. Olvasd újra a STATUS.md-t! |
| **≥50** | 🚨 CRITICAL | **STOP!** Session re-anchor kérése Monitor-tól vagy summary mentés + új session indítás. |

**WARNING esetén (30-49 turn):**
```typescript
// 1. Mi volt a fő cél? (session state)
const state = await mcp__spaceos-knowledge__read_session_state
  terminal: "<terminal-neve>"

// 2. Mi volt az utolsó focus? (STATUS.md)
const status = await mcp__spaceos-knowledge__read_terminal_status_md
  terminal: "<terminal-neve>"

// 3. Következő checkpoint? (milestone tracking)
const checkpoints = await mcp__spaceos-knowledge__read_checkpoints_md
  terminal: "<terminal-neve>"

// → Térj vissza a fő célhoz! Ne merülj részletekbe!
```

**CRITICAL esetén (≥50 turn):**
```typescript
// 1. Session state mentés (cross-session recovery)
mcp__spaceos-knowledge__write_session_state
  terminal: "<terminal-neve>"
  epic_id: "EPIC-CUTTING-Q3"
  epic_progress: 35
  next_checkpoint_id: "CP-INTEGRATION-TEST"
  last_active_task: "MSG-BACKEND-045"
  completed_checkpoints: ["CP-KERNEL-FSM"]

// 2. STATUS.md snapshot mentés
mcp__spaceos-knowledge__write_terminal_status_md
  terminal: "<terminal-neve>"
  system_status: "in_progress"
  current_focus: "Kernel FSM integration testing"
  recent_actions: ["Completed FSM implementation", "Started integration tests"]
  next_steps: ["Complete test suite", "Frontend integration"]

// 3. Monitor-nak escalation
mcp__spaceos-knowledge__send_message
  to: "monitor"
  type: "info"
  content: "Context saturation CRITICAL (≥50 turn). Re-anchoring vagy új session kérése."
  priority: "high"

// 4. Turn count reset (ha új session)
mcp__spaceos-knowledge__reset_turn_count
  terminal: "<terminal-neve>"
```

---

### MAJOR DECISION ELŐTT — STATUS CHECK

**Mielőtt:**
- Új epic-hez kezdesz
- Terminálnak task-ot adsz ki
- Strategic döntést hozol
- Cross-terminal koordinációt indítasz

**Ellenőrizd:**

```typescript
// 1. Current focus mi volt?
mcp__spaceos-knowledge__read_terminal_status_md
  terminal: "<terminal-neve>"

// 2. Session state aktív?
mcp__spaceos-knowledge__read_session_state
  terminal: "<terminal-neve>"

// 3. Checkpoint-ok állapota
mcp__spaceos-knowledge__read_checkpoints_md
  terminal: "<terminal-neve>"

// 4. Context saturation check
mcp__spaceos-knowledge__get_context_saturation
  terminal: "<terminal-neve>"
```

**Miért fontos?**
- **Goal Drift Prevention** — Ne térj el az aktív epic-től!
- **Subtask Overfocus** — Ne optimalizálj túl részfeladatokat!
- **Context Dilution** — Ne veszítsd el a fő célt!
- **Milestone Awareness** — Tudd hol tartasz!

---

### SESSION END — STATE PERSISTENCE (KÖTELEZŐ!)

**Session lezárás előtt (utolsó 5 percben):**

```typescript
// 1. STATUS.md snapshot frissítés
mcp__spaceos-knowledge__write_terminal_status_md
  terminal: "<terminal-neve>"
  system_status: "operational"      // operational | in_progress | paused | blocked
  current_focus: "MSG-BACKEND-045: Kernel FSM implementation"
  recent_actions: [
    "Completed Kernel FSM state machine",
    "Started integration tests",
    "Blocked on mock API endpoint"
  ]
  next_steps: [
    "Wait for Frontend mock API completion",
    "Continue integration test suite",
    "Review FSM edge cases"
  ]

// 2. Session state mentés (cross-session recovery)
mcp__spaceos-knowledge__write_session_state
  terminal: "<terminal-neve>"
  epic_id: "EPIC-CUTTING-Q3"
  epic_progress: 35                  // % progress
  next_checkpoint_id: "CP-INTEGRATION-TEST"
  last_active_task: "MSG-BACKEND-045"
  completed_checkpoints: ["CP-KERNEL-FSM", "CP-DOMAIN-MODEL"]

// 3. Turn count reset (ha új session következik)
mcp__spaceos-knowledge__reset_turn_count
  terminal: "<terminal-neve>"
```

**Miért kötelező?**
- **Cross-session goal recovery** — A következő session tudja folytatni!
- **Goal re-anchoring** — Nem vész el az epic fókusz!
- **Progress tracking** — Milestone visibility!

---

### CHECKPOINT MANAGEMENT

**Új checkpoint hozzáadása (Conductor/Root):**

```typescript
mcp__spaceos-knowledge__append_checkpoint_to_md
  terminal: "<terminal-neve>"
  date: "2026-07-10"
  name: "Kernel FSM Complete"
  decision: "GO/NO-GO"
  evaluation_criteria: [
    "All FSM states implemented",
    "Unit tests pass (>95%)",
    "Integration with Orchestrator ready"
  ]
  go_actions: ["Proceed to Orchestrator integration"]
  no_go_actions: ["Fix FSM edge cases", "Add missing transitions"]
  refs: ["MSG-BACKEND-045", "EPIC-CUTTING-Q3"]
```

**Checkpoint státusz check:**

```typescript
mcp__spaceos-knowledge__read_checkpoints_md
  terminal: "<terminal-neve>"
```

**Checkpoint-ok célja:**
- **Milestone tracking** — Hol tartunk az epic-ben?
- **GO/NO-GO decision points** — Mehetünk tovább vagy vissza kell lépni?
- **Progress visibility** — Explicit haladás követés!

---

### DIAGNOSTIC — ÖSSZES TERMINÁL OVERVIEW

**Root/Monitor használja:**

```typescript
// Minden terminál context files státusza
mcp__spaceos-knowledge__get_all_context_files_status

// Output:
[
  {
    "terminal": "conductor",
    "hasStatus": true,
    "hasSessionState": true,
    "hasTurnCount": true,
    "hasCheckpoints": true,
    "turnCount": 13,
    "sessionState": {
      "epicId": "EPIC-CUTTING-Q3",
      "epicProgress": 25,
      "nextCheckpointId": "CP-KERNEL-FSM"
    }
  },
  {
    "terminal": "backend",
    "hasStatus": false,
    "hasSessionState": false,
    "turnCount": 0
  }
]
```

**Use case:**
- Melyik terminál van **goal drift** veszélyben? (turnCount >30)
- Melyik terminálnak nincs session state? (hasSessionState: false)
- Melyik terminál session-je túl hosszú? (turnCount >50 → re-anchor!)

---

### BEST PRACTICES

1. **Session start: MINDIG** `build_session_start_context` — Ne kezdj munkát goal re-anchoring nélkül!
2. **Every 10-15 turns: CHECK** `get_context_saturation` — Ne várd meg a CRITICAL-t!
3. **Before major decision: READ** `read_session_state` + `read_terminal_status_md` — Ellenőrizd a fókuszt!
4. **Session end: WRITE** `write_session_state` + `write_terminal_status_md` — A következő session hálás lesz!
5. **Checkpoint milestones: APPEND** `append_checkpoint_to_md` — Track progress explicitly!

---

### ANTI-PATTERNS (NE CSINÁLD!)

❌ **Session start goal re-anchoring nélkül** — Goal drift garantált!
❌ **Turn count ignorálás** — >50 turn után már minden context diluted.
❌ **Session state mentés nélküli lezárás** — A következő session elveszett.
❌ **STATUS.md nem frissítése** — "Mi volt a fókusz?" → senki nem tudja.
❌ **Checkpoint-ok nélküli epic** — Progress tracking lehetetlen.
❌ **WARNING threshold ignorálás** — 30-49 turn = goal drift veszély!

---

### MCP TOOL REFERENCE

| Tool | Használat | Mikor |
|------|-----------|-------|
| `build_session_start_context` | Session start context | **Session start (első 3 perc)** |
| `get_context_saturation` | Turn count + threshold | **Every 10-15 turns** |
| `read_session_state` | Epic + progress + checkpoints | **Session start, decision előtt** |
| `write_session_state` | Session state save | **Session end, CRITICAL** |
| `read_terminal_status_md` | Current focus snapshot | **Session start, decision előtt** |
| `write_terminal_status_md` | STATUS.md update | **Session end, milestone** |
| `increment_turn_count` | Turn tracking | **Every 10-15 turns** |
| `reset_turn_count` | Turn reset | **Session end (ha új session)** |
| `read_checkpoints_md` | Checkpoint list | **Session start, progress check** |
| `append_checkpoint_to_md` | Add new checkpoint | **Milestone planning** |
| `get_context_files_status` | Single terminal overview | **Diagnostic** |
| `get_all_context_files_status` | All terminals overview | **Root/Monitor diagnostic** |

---

**Referencia:**
- `docs/knowledge/patterns/GOAL_PERSISTENCE_PATTERNS.md` — 5 failure mode, 6 solution pattern
- `docs/knowledge/patterns/TERMINAL_CONTEXT_PERSISTENCE_FILES.md` — File structure, theory
- `spaceos-nexus/knowledge-service/src/contextPersistence.ts` — Implementation

---
