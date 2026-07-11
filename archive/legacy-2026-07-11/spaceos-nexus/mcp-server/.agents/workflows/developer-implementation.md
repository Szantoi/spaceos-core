---
description: how to execute the Developer Implementation Loop for a single Task (Phase 3)
---

# Developer – Implementation Loop (Fázis 3)

**Szerepkör:** Backend Developer / Frontend Developer
**Trigger:** Orchestrator per-Task dispatch (P2: backend / P3: frontend)
**FSM Output:** Task: `Todo` → `IN_DEV` → `Ready for QA`
**Forrás:** `Operative_Process_Framework_Standard.md` — Fázis 3

> ⚠️ Ez a workflow **egyetlen Task-ra** vonatkozik. Az Orchestrator az összes `Todo` Task-ra dependency-sorrendben ismétli meg a dispatchet.

---

## Lépések

### 1. Task "szerződés" beolvasása

```
<EPIC_ROOT>/tasks/TASK-<N>-<M>.md    ← Kötelező beolvasni!
state.md                            ← Jelenlegi állapot
```

Ellenőrizd:
- [ ] Task státusza `Todo` (ha `Blocked`, NE kezdd el — eszkaláld az Orchestratornak)
- [ ] Függőségi Task-ok státusza `Done`

### 2. FSM frissítés: IN_DEV

- `state.md` frissítés: Task → `IN_DEV`
- Értesítsd az Orchestratort (ha Multi-Workspace mód)

### 3. Implementáció

Hajts végre minden Acceptance Criteriumot a Task fájlból. Tartsd be:
- A projekt kódolási szabványait (`src/agent-system/database/standards/`)
- Clean Architecture rétegeit (ha Backend: no cross-layer import)
- Az érintett ADR-ekben foglaltakat

### 4. Build ellenőrzés

// turbo
```powershell
# Backend
dotnet build

# Frontend (ha frontend task)
npm run build
```

Ha a build sikertelen: javítsd a hibákat mielőtt továbblépsz.

### 5. Unit tesztek írása

- Minden publikus üzleti logikai egységhez legalább 1 unit teszt
- Nincs skip/kommentálva kihagyott teszt

// turbo
```powershell
# Backend: tesztek futtatása
dotnet test

# Frontend: tesztek futtatása
npm test -- --watchAll=false
```

### 6. Implementation Summary elkészítése

Hozd létre a dokumentumot:

```
<EPIC_ROOT>/implementation-summary/TASK-<N>-<M>-<slug>.md
```

Sablon: `src/agent-system/database/roles/engineering/templates/implementation_report.template.md`

Kötelező tartalom:
- [ ] Mit implementáltál (mit és hogyan)
- [ ] Érintett fájlok listája
- [ ] Build és teszt eredmény (zöld)
- [ ] Ismert hiányosságok / technikai debt (ha van)

### 7. Definition of Done ellenőrzés

- [ ] Minden Acceptance Criterion teljesítve
- [ ] Build zöld
- [ ] Unit tesztek zöldek
- [ ] Implementation Summary létrehozva

### 8. FSM frissítés és Dispatch QA-nak

- `state.md` frissítés: Task → `Ready for QA` (→ `QA_WAITING`)
- Üzenet Orchestratornak: `messages/orchestrator/<timestamp>_from-developer_task-ready-for-qa.md`

---

## Kimenet

| Fájl | Útvonal |
|:-----|:--------|
| Implementation Summary | `<EPIC_ROOT>/implementation-summary/TASK-<N>-<M>-<slug>.md` |

## Következő fázis

→ **Fázis 4:** `qa-tester-testing.md`
