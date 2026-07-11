---
id: MSG-MONITOR-021
from: root
to: monitor
type: task
priority: high
status: READ
model: sonnet
created: 2026-07-04
completed: 2026-07-04 13:16:12
content_hash: 9315149498740510bc99a7e88409b3c798d12b6ab44da8b8e95fb0e3595d9772
---

# Goal Persistence Pattern — felügyeleti felelősség és Goal Drift detection

## Összefoglaló

A Monitor terminál felelőssége a **Goal Drift** (cél eltérés) felügyelete. Ez kritikus a hosszú agent session-ök során.

## Mi a Goal Drift?

Az LLM agent-ek hajlamosak "elfelejteni" az eredeti céljukat hosszú session-ök közben. Ez 5 módon történhet:

### 5 Goal Drift Failure Mode

| # | Failure Mode | Mit jelent | Monitor jelzés |
|---|--------------|------------|----------------|
| 1 | **Context Dilution** | Korai instrukciók elhalványulnak a context window-ban | Session >25 NWT (~50 perc) figyelmeztetés |
| 2 | **Pattern Matching Override** | Friss kontextus felülírja a direktívákat | Terminál nem az epic-hez kapcsolódó munkát végez |
| 3 | **Inherited Drift** | Subagent output-ok szennyezik a parent célokat | DONE outbox túl részletes (>1500 char) |
| 4 | **Value Conflict Drift** | Modell értékeivel ütköző instrukciók erodálódnak | Terminál "megtagadja" a feladatot |
| 5 | **Subgoal Displacement** | Részfeladatok optimalizálása aláássa fő célt | Terminál side-task-okba megy |

## Implementált Védelmek (2026-07-04)

### 1. Cross-session Goal Recovery (`sessionState.ts`)
- Session indításkor betölti a korábbi goal state-et
- **Monitor ellenőrzés:** `.session-state.json` fájl frissül-e?

### 2. Auto Context Saturation Detector (`contextSaturation.ts`)
- Turn count tracking (`.turn-count` fájl)
- Auto re-anchoring >50 turn (25 NWT)
- **Monitor ellenőrzés:** Turn count <50? Ha nem, re-anchor triggerelődött?

### 3. Subagent Output Filtering (`outputFiltering.ts`)
- DONE outbox tartalom szűrése
- >1500 char → summary extraction
- **Monitor ellenőrzés:** DONE outbox méret reasonable?

### 4. Dense Milestone Feedback (`watchDone.ts`)
- Minden DONE után progress feedback Conductor-nak
- **Monitor ellenőrzés:** Conductor kapja a feedback-et?

## Monitor Health Check Kiegészítések

### Goal Drift Detection Checklist

```
## 🎯 Goal Persistence Check (KRITIKUS!)

### 1. Context Saturation
- [ ] Conductor turn count <50 (check .turn-count)
- [ ] Ha >50: re-anchor trigger megtörtént?
- [ ] Session state mentve? (.session-state.json friss?)

### 2. Epic Alignment
- [ ] Aktív terminálok az EPICS.yaml epic-en dolgoznak?
- [ ] DONE outbox-ok az epic checkpoint-okhoz kapcsolódnak?
- [ ] Nincs "off-topic" munka?

### 3. Inherited Drift Prevention
- [ ] DONE outbox-ok <1500 char (ha több: filtered?)
- [ ] Dense milestone feedback működik?

### 4. Goal Recovery
- [ ] Session restart után goal context injektálva?
- [ ] Conductor briefing tartalmazza az epic progress-t?
```

## NWT Thresholds (Goal Persistence)

| Metrika | Threshold | Akció |
|---------|-----------|-------|
| Turn count | 15 NWT (30 turn) | WARNING log |
| Turn count | 25 NWT (50 turn) | AUTO RE-ANCHOR |
| Session age | 60 NWT (2 óra) | Context check ajánlott |
| DONE outbox size | >1500 char | Filtered flag ellenőrzés |

## Fájlok

**Implementáció:**
- `src/conductor/sessionState.ts` — Cross-session recovery
- `src/conductor/contextSaturation.ts` — Auto saturation detector
- `src/conductor/outputFiltering.ts` — Output filtering

**Dokumentáció:**
- `docs/knowledge/patterns/GOAL_PERSISTENCE_PATTERNS.md` — Teljes pattern dokumentáció

## Kutatási Háttér

A Goal Persistence kutatás forrásai:
- Zylos Research (2026-04-03): "Goal Persistence & Drift in Long-Horizon AI Agents"
- MiRA Framework: Dense rewards 6.4% → 43.0% performance improvement
- Anthropic Multi-Agent: Planner identity separation

## Acceptance Criteria

- [ ] MEMORY.md frissítve Goal Drift detection checklisttel
- [ ] Health check prompt tartalmazza a Goal Persistence ellenőrzéseket
- [ ] 5 failure mode ismerete dokumentálva
- [ ] NWT threshold-ok ismertek
