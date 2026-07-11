---
description: how to manage a Task lifecycle from creation to closure with Implementation Summary
---

# Task Lifecycle – Task Életciklusa (Létrehozás → Lezárás)

**Szerepkör:** Tech Lead (Létrehozás) / Agent (Végrehajtás)
**Trigger:** Új feladat kiírása vagy egy `in-progress` task befejezése.
**Forrás:** `Project_Folder_Structure_Standard.md`

---

## Lépések

### 1. Task fájl létrehozása (Tech Lead)

**Elnevezés:** `[EPIC_ROOT]/tasks/TASK-YY-ZZ.md`

**Kötelező felépítés:**

```yaml
---
id: task-YY-ZZ
title: "Cím"
type: task
epic: EPIC-YY
scope: project-nev
status: pending
priority: P0 | P1 | P2
role: backend_developer | frontend_developer | ...
created: YYYY-MM-DD
# FSM tracking (ha szükséges)
fsm_state: "BACKLOG_READY"
fsm_retry_count: 0
---
# TASK-YY-ZZ: Cím

## Leírás
Pontosan mi a feladat? Miért kell megcsinálni? Blokkolja-e valami?

## Elfogadási feltételek (Acceptance Criteria)
- [ ] AC-01: ...
- [ ] AC-02: ...

## (Opcionális) Technikai megvalósítás javaslata
Leírás, kódrészlet vagy pszeudokód.
```

### 2. Task végrehajtása (Agent)

1. Olvasd be a Task fájlt.
2. Állítsd a státuszt `in_progress`-re a fájlban és az Epic `state.md`-ben.
3. Hajtsd végre a módosításokat a kódban az AC-k alapján.
4. Futtass teszteket/buildet.

### 3. Implementation Summary létrehozása (KÖTELEZŐ)

**Minden lezárt task után kötelező létrehozni!**
**Útvonal:** `[EPIC_ROOT]/implementation-summary/TASK-YY-ZZ-<slug>.md`

**Kötelező tartalom:**

```yaml
---
id: impl-task-YY-ZZ
title: "Implementation Summary: <Task címe>"
task: TASK-YY-ZZ
status: completed
created: YYYY-MM-DD
author: <agent role>
---
## Mit csináltunk
Technikai részletek, döntések, megoldási mód.

## Módosított/létrehozott fájlok listája
- `src/...`
- `src/...`

## Elfogadási feltételek tesztelése
- [x] AC-01: leírás hogyan ellenőrizve
- [x] AC-02: leírás hogyan ellenőrizve
```

### 4. Task lezárása

1. Állítsd a Task fájlban a státuszt `completed`-re.
2. Frissítsd az Epic `state.md` táblázatát: `completed`.
3. Értesítsd az Orchestratort a befejezésről.

> **Aranyszabály:** Nincs lezárt task implementation summary nélkül. Ha a `tasks/` mappában `status: completed` szerepel, de nincs megfelelő fájl az `implementation-summary/` mappában, a task nem tekinthető ténylegesen lezártnak.

### 5. Definition of Done

- [ ] Task fájl létezik AC-kal
- [ ] Implementáció megtörtént
- [ ] Implementation Summary fájl létrejött minden AC igazolásával
- [ ] Task státusz `completed` mindenhol (Task fájl, Epic State)

---

## Kimenet

| Fájl | Útvonal |
|:-----|:--------|
| Task szerződés | `tasks/TASK-YY-ZZ.md` |
| Impl. Summary | `implementation-summary/TASK-YY-ZZ-<slug>.md` |
