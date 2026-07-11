---
description: how to execute the Tech Lead Task Planning phase (Phase 2)
---

# Tech Lead – Task Planning (Fázis 2)

**Szerepkör:** Tech Lead
**Trigger:** Orchestrator dispatch (P1: `tech_lead_epic_planning.message.md`)
**FSM Output:** Epic → `In Progress`, minden Task → `Todo`
**Forrás:** `Operative_Process_Framework_Standard.md` — Fázis 2

---

## Lépések

### 1. Input beolvasása

```
[Epic-Root]/plan.md    ← Architect által készített Epic Plan (az Epic gyökerében)
state.md                                  ← Jelenlegi FSM állapot
```

### 2. Task-ok azonosítása és felbontása

Az Epic Plan alapján bontsd le a munkát atomi, végrehajtható Task-okra. Minden Task:
- Egyetlen fejlesztői szerepkörnek (BE/FE) adható ki
- Elvégezhető egy iteráción belül (max. 1-2 nap)
- Mérhető elfogadási kritériumokkal rendelkezik

### 3. Task fájlok létrehozása

Minden Task-hoz hozz létre egy fájlt a sablon alapján:

```
[Epic-Root]/tasks/TASK-<EPIC_NR>-<TASK_NR>.md
```

Sablon: `src/agent-system/database/roles/engineering/templates/task.template.md`

Kötelező mezők minden Task fájlban:
- [ ] Task ID, Cím, Assignee Role
- [ ] Priority (P0/P1/P2/P3)
- [ ] Depends on (függőség lista)
- [ ] Skills szükségesek
- [ ] QA igény (Igen / Nem)
- [ ] Acceptance Criteria (mérhető, tesztelhetó)
- [ ] Definition of Done

### 4. Backlog fájl elkészítése

```
[Epic-Root]/backlog.md
```

Tartalom:
- Task-ok priorizált listája
- Függőségi sorrend (topológiai sorrend)
- Becsült effort összesítő

### 5. Opcionális: Devils Advocate Review

**Ha az Epic kockázatos vagy komplex** (sok integrációs pont, új technológia):
- Kérd a Devils Advocate szerepkör véleményét
- Irány: `messages/devils-advocate/<timestamp>_from-tech-lead_review-request.md`

### 6. Körkörös függőség ellenőrzése

Ellenőrizd, hogy a Task-ok között nincs circular dependency:

- Rajzold fel a függőségi gráfot mentálisan vagy a backlog.md-ben
- Ha körkörös függőség van: refaktoráld a Task-ok határait

### 7. Definition of Done ellenőrzés

- [ ] Minden Task-hoz role hozzárendelve
- [ ] Minden Task-hoz skills megadva
- [ ] Minden Task-hoz QA igény definiálva
- [ ] Függőségek definiálva, nincs circular dependency
- [ ] `backlog.md` elkészült

### 8. FSM frissítés és Dispatch

- `state.md` frissítés: Epic → `In Progress`, minden Task → `Todo`
- Orchestratornak visszajelzés: `messages/orchestrator/<timestamp>_from-tech-lead_tasks-ready.md`

---

## Kimenet

| Fájl | Útvonal |
|:-----|:--------|
| Task fájlok | `docs/<project>/epics/<EPIC_ID>/tasks/TASK-<N>-<M>.md` |
| Backlog | `docs/<project>/epics/<EPIC_ID>/backlog.md` |

## Következő fázis

→ **Fázis 3:** `developer-implementation.md`
