---
description: how to set up a new Epic within a Milestone, including state.md and tasks folder
---

# Epic Setup – Meglévő Milestone-on belüli Epic felállítása

**Szerepkör:** Tech Lead / Orchestrator
**Trigger:** Milestone terv (`plan.md`) alapján egy új modul fejlesztése kezdődik.
**Forrás:** `Project_Folder_Structure_Standard.md`

---

## Lépések

### 1. Epic mappa létrehozása

Hozd létre a mappát a Milestone-on belül:

```
[EPIC_ROOT]/
 ├── state.md                      ← Az Epic dashboard-ja
 ├── tasks/                        ← A feladatok (Tasks) fájljai
 └── implementation-summary/       ← KÖTELEZŐ összefoglalók mappája
 ```

 > **Megjegyzés:** Az `[EPIC_ROOT]` a `Project_Folder_Structure_Standard.md` alapján lehet közvetlenül a projekt gyökerében (Lean) vagy Milestone mappán belül (Enterprise).

### 2. `state.md` elkészítése

**Tartalom:**
- **Célkitűzés:** Mit ad hozzá a rendszerhez ez a modul?
- **Task lista táblázat**
- **FSM Tracking (opcionális, de ajánlott)**

**FSM Tracking YAML front-matter:**
```yaml
fsm_workflow_id: "agile-epic-lifecycle-v1"
fsm_state: "BACKLOG_READY"
fsm_retry_count: 0
related_projects: [] # Opcionális függőségek másik projektből
```

**Task táblázat formátum:**
| Típus | ID | Feladat | Státusz | Leírás |
|:-:|:---|:--------|:--------|:-------|
| `Logic` | T1-01 | ... | 📋 Planned | ... |

### 3. Task fájlok előkészítése

A `tasks/` mappában hozd létre az atomi feladatokat.
→ Menj a **`task-lifecycle.md`** workflow-ba a fájl felépítéséhez.

### 4. `implementation-summary/` mappa ellenőrzése

Győződj meg róla, hogy a mappa létezik. Ide fognak kerülni a lezárt taskok bizonyítékai.

### 5. Projekt szintű `state.md` frissítése

Frissítsd a `docs/<project>/state.md` fájlban az `Epic State Map` táblázatot az új Epichoz.

### 6. Definition of Done

- [ ] `epic_YY/` mappa létrehozva
- [ ] `state.md` kitöltve célkitűzéssel és task listával
- [ ] `tasks/` és `implementation-summary/` mappák létrehozva
- [ ] Projekt `state.md` frissítve az új Epichoz

---

## Kimenet

| Fájl | Útvonal |
|:-----|:--------|
| Epic Dashboard | `[EPIC_ROOT]/state.md` |
| Task feladatok | `[EPIC_ROOT]/tasks/` |

## Következő lépés

→ **Taskok kidolgozásához:** `task-lifecycle.md`
