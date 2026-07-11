---
description: how to execute the Tech Lead Epic Review after all Tasks are Done (Phase 5)
---

# Tech Lead – Epic Review (Fázis 5)

**Szerepkör:** Tech Lead
**Trigger:** Orchestrator dispatch — minden Task státusza `Done`
**FSM Output:** Epic: `In Progress` → `Review`
**Forrás:** `Operative_Process_Framework_Standard.md` — Fázis 5

---

## Lépések

### 1. Input dokumentumok beolvasása

Olvasd be az Epic összes dokumentumát:

```
<EPIC_ROOT>/tasks/*.md                      ← Task szerződések
<EPIC_ROOT>/implementation-summary/*.md     ← Dev összefoglalók
<EPIC_ROOT>/qa/*.md                         ← QA eredmények
<EPIC_ROOT>/plan.md                         ← Architect terv
state.md                                     ← Jelenlegi állapot
```

Ellenőrizd, hogy **minden Task státusza `Done`** — ha valamelyik nem az, NE folytasd, értesítsd az Orchestratort.

### 2. Epic Review dokumentum elkészítése

Hozd létre:

```
<EPIC_ROOT>/epic_review.md
```

Sablon: `src/agent-system/database/roles/management/tech-lead/templates/epic_review.template.md`

Kötelező tartalom:

#### 2a. Task összesítő
- Minden Task: státusz, implementáló developer, QA eredmény, retry count

#### 2b. Technikai kihívások
- Mi volt nehezebb a vártnál?
- Milyen technikai adósság (tech debt) keletkezett?

#### 2c. Lessons Learned
- Mit tanultunk ezen az Epicon, amit a jövőben alkalmazni kell?

#### 2d. Metrikák
- Tervezett vs. tényleges effort
- QA rejection rate
- Build failure count

#### 2e. ⚠️ Calibration Instructions (kötelező!)
Ez a szekció a legfontosabb a rendszer evolúciója szempontjából. Dokumentáld:
- Milyen globális **skill**, **template** vagy **standard** frissítésre van szükség?
- Pl. "A `task.template.md`-be fel kell venni egy kötelező 'API Contract' szekciót"
- Pl. "Új skill kell a `qa-tester/skills/`-be: API mocking"

### 3. Definition of Done ellenőrzés

- [ ] Task összesítő minden Task-ra elkészítve
- [ ] Technikai kihívások dokumentálva
- [ ] Lessons Learned dokumentálva
- [ ] Metrikák kitöltve
- [ ] Calibration Instructions tartalmaz legalább 1 javaslatot
- [ ] `epic_review.md` létrehozva

### 4. FSM frissítés és Dispatch

- `state.md` frissítés: Epic → `Review`
- Orchestratornak visszajelzés: `messages/orchestrator/<timestamp>_from-tech-lead_epic-review-done.md`

---

## Kimenet

| Fájl | Útvonal |
|:-----|:--------|
| Epic Review | `<EPIC_ROOT>/epic_review.md` |

## Következő fázis

→ **Fázis 6:** `architect-signoff.md`
