## 🧠 CONTEXT PERSISTENCE — MCP TOOLS (2026-07-07)

> **Új MCP eszközök a Goal Drift Prevention támogatására!**
> Használd őket a context window kezelésére és a fókusz megőrzésére.

---

### SESSION START RITUAL

**Minden session elején (első 5 percben):**

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
  "status": "ok",
  "thresholds": {
    "warning": 30,
    "critical": 50
  },
  "sessionState": {
    "epicId": "EPIC-CUTTING-Q3",
    "epicProgress": 25,
    "nextCheckpointId": "CP-KERNEL-FSM"
  }
}
```

---

### DURING WORK — FÓKUSZ TRACKING

**Minden 10-15 turn után (vagy major milestone után):**

```typescript
// Turn count increment (automatikus vagy manuális)
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
| **30-49** | ⚠️ WARNING | **FÓKUSZÁLJ!** Térj vissza az epic fő céljához. Ne merülj új részletekbe. |
| **≥50** | 🚨 CRITICAL | **STOP!** Session re-anchor kérése Monitor-tól vagy summary mentés + új session indítás. |

**WARNING esetén:**
```typescript
// Újraolvassuk a fő célt
mcp__spaceos-knowledge__read_session_state
  terminal: "<terminal-neve>"

// STATUS.md check: mi volt a focus?
mcp__spaceos-knowledge__read_terminal_status_md
  terminal: "<terminal-neve>"
```

**CRITICAL esetén:**
```typescript
// Session state mentés
mcp__spaceos-knowledge__write_session_state
  terminal: "<terminal-neve>"
  epic_id: "EPIC-CUTTING-Q3"
  epic_progress: 30
  next_checkpoint_id: "CP-KERNEL-FSM"
  last_active_task: "MSG-BACKEND-045"

// Monitor-nak escalation
mcp__spaceos-knowledge__send_message
  to: "monitor"
  type: "info"
  content: "Context saturation CRITICAL (≥50 turn). Re-anchoring kérése."
  priority: "high"
```

---

### MAJOR DECISION ELŐTT — STATUS CHECK

**Mielőtt:**
- Új epic-hez kezdesz
- Terminálnak task-ot adsz ki
- Strategic döntést hozol

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
```

**Miért fontos?**
- Ne térj el az aktív epic-től!
- Ne indíts új epic-et, amíg a régi nincs kész vagy blokkolva!
- Checkpoint-ok segítenek a milestone tracking-ben.

---

### SESSION END — STATE PERSISTENCE

**Session lezárás előtt (utolsó 5 percben):**

```typescript
// 1. STATUS.md snapshot frissítés
mcp__spaceos-knowledge__write_terminal_status_md
  terminal: "<terminal-neve>"
  system_status: "operational" | "in_progress" | "blocked"
  current_focus: "MSG-BACKEND-045: Kernel FSM implementation"
  recent_actions: [
    "Completed Kernel FSM state machine",
    "Started integration tests",
    "Blocked on mock API endpoint"
  ]
  next_steps: [
    "Wait for Frontend mock API completion",
    "Continue integration test suite"
  ]

// 2. Session state mentés
mcp__spaceos-knowledge__write_session_state
  terminal: "<terminal-neve>"
  epic_id: "EPIC-CUTTING-Q3"
  epic_progress: 35
  next_checkpoint_id: "CP-INTEGRATION-TEST"
  last_active_task: "MSG-BACKEND-045"
  completed_checkpoints: ["CP-KERNEL-FSM"]

// 3. Turn count reset (ha új session következik)
mcp__spaceos-knowledge__reset_turn_count
  terminal: "<terminal-neve>"
```

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
    "turnCount": 13
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
- Melyik terminál van "goal drift" veszélyben? (turnCount >30)
- Melyik terminálnak nincs session state? (hasSessionState: false)
- Melyik terminál session-je túl hosszú? (turnCount >50)

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

---

**Referencia:**
- `docs/knowledge/patterns/GOAL_PERSISTENCE_PATTERNS.md`
- `docs/knowledge/patterns/TERMINAL_CONTEXT_PERSISTENCE_FILES.md`
- `spaceos-nexus/knowledge-service/src/contextPersistence.ts`
